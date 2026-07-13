import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { sfx } from "../core/sfx";
import type { MeleeWeaponPart } from "../parts/parts";

// Melee with a gap-closer (GAME_DESIGN §3.1): high commitment, punishable
// recovery on whiff. State machine: idle -> lunge -> swing -> recovery.
// Within closeRange the lunge is skipped and it's a direct swing.
// Combo strings: pressing melee again during a CONNECTED swing's recovery
// chains into swing 2, then a heavier finisher. Whiffs always end the
// string in full punishable recovery. Weapon numbers (damage, range,
// speed, recovery) come from the equipped MeleeWeaponPart -- only the
// gap-closer's own lunge mechanics (closeRange/lungeRange/etc.) are shared
// across all melee weapons.

type MeleeState = "idle" | "lunge" | "swing" | "recovery";

// Per-hit multipliers for the 3-hit string: opener, follow-up, finisher.
const COMBO_DAMAGE = [1, 0.85, 1.4];
const COMBO_ENDURANCE = [1, 0.8, 1.5];
const COMBO_KNOCKBACK = [1, 0.8, 1.8];

export class Melee {
  state: MeleeState = "idle";
  private timer = 0;
  private didHit = false;
  private comboIndex = 0; // 0-2 within the string
  private swingBlade: THREE.Mesh;

  constructor(
    private owner: Robo,
    scene: THREE.Object3D,
  ) {
    // Simple visual: a glowing blade that appears during the swing
    this.swingBlade = new THREE.Mesh(
      new THREE.BoxGeometry(0.15, 0.5, 2.2),
      new THREE.MeshBasicMaterial({ color: 0xffee66, transparent: true }),
    );
    this.swingBlade.visible = false;
    scene.add(this.swingBlade);
  }

  /** Only valid if the right arm is actually a melee weapon. */
  private get weapon(): MeleeWeaponPart | null {
    const rightArm = this.owner.loadout.rightArm;
    return rightArm.kind === "melee" ? rightArm.part : null;
  }

  /** True while the owner is mid-melee (used to suppress gun/moves). */
  get busy(): boolean {
    return this.state !== "idle";
  }

  /** True while this melee could clash (attacking, not recovering). */
  get attacking(): boolean {
    return this.state === "lunge" || this.state === "swing";
  }

  /** Melee clash (GAME_DESIGN §3.1): both attacks cancel into a short
   *  step-cancel window — whoever re-engages faster wins the exchange. */
  clashCancel(): void {
    this.reset();
    this.owner.actionLock = 0.25;
  }

  tryStart(target: Robo): void {
    if (this.state !== "idle") return;
    if (this.owner.controlLocked) return;
    if (!this.weapon) return; // right arm is a gun: no melee to swing

    const dist = this.owner.position.distanceTo(target.position);
    if (dist > TUNING.melee.lungeRange) return; // out of range: no-op

    this.didHit = false;
    this.comboIndex = 0;
    this.beginSwingOrLunge(target);
  }

  /** Chain into the next hit of the string. Only valid during the recovery
   *  of a swing that CONNECTED, and only up to the 3-hit finisher. Reuses
   *  the gap-closer: knockback from the previous hit routinely pushes the
   *  target just past melee range, so a follow-up with no re-approach
   *  would whiff almost every time. Returns true if the chain happened. */
  chain(target: Robo): boolean {
    if (this.state !== "recovery" || !this.didHit) return false;
    if (this.comboIndex >= COMBO_DAMAGE.length - 1) return false;
    this.comboIndex += 1;
    this.didHit = false;
    this.beginSwingOrLunge(target);
    return true;
  }

  private beginSwingOrLunge(target: Robo): void {
    const T = TUNING.melee;
    const dist = this.owner.position.distanceTo(target.position);
    if (dist <= T.closeRange) {
      this.enterSwing();
    } else {
      const toTarget = target.position.clone().sub(this.owner.position).setY(0);
      if (toTarget.lengthSq() > 1e-4) {
        this.owner.setFacing(Math.atan2(toTarget.x, toTarget.z));
      }
      this.state = "lunge";
      this.timer = T.lungeMaxDuration;
    }
  }

  private enterSwing(): void {
    this.state = "swing";
    this.timer = this.weapon?.swingActiveTime ?? 0.18;
    this.owner.externalMove = null;
    // Planted on the ground / falls in the air, but can't act
    this.owner.actionLock = 10; // held while swing+recovery run; reset clears
    sfx.meleeSwing();
  }

  private enterRecovery(duration: number): void {
    this.state = "recovery";
    this.timer = duration;
    this.owner.externalMove = null;
    this.owner.actionLock = 10;
  }

  update(dt: number, target: Robo): void {
    const T = TUNING.melee;
    const weapon = this.weapon;
    if (!weapon) {
      this.reset();
      return;
    }

    // Knocked down mid-melee: cancel everything
    if (
      this.owner.health.state === "knockdown" ||
      this.owner.health.state === "dead"
    ) {
      this.reset();
      return;
    }

    switch (this.state) {
      case "idle":
        return;

      case "lunge": {
        this.timer -= dt;
        const toTarget = target.position.clone().sub(this.owner.position);
        toTarget.y = 0;
        const dist = toTarget.length();
        this.owner.setFacing(Math.atan2(toTarget.x, toTarget.z));
        this.owner.externalMove = toTarget
          .normalize()
          .multiplyScalar(T.lungeSpeed);

        if (dist <= T.lungeReachDistance) {
          this.enterSwing();
        } else if (this.timer <= 0) {
          // Lunge expired without reaching: whiff recovery
          this.enterRecovery(weapon.whiffRecovery);
        }
        break;
      }

      case "swing": {
        if (!this.didHit) this.checkHit(target, weapon);
        this.updateBladeVisual();
        this.timer -= dt;
        if (this.timer <= 0) {
          this.enterRecovery(this.didHit ? weapon.hitRecovery : weapon.whiffRecovery);
          this.swingBlade.visible = false;
        }
        break;
      }

      case "recovery": {
        this.timer -= dt;
        if (this.timer <= 0) this.reset();
        break;
      }
    }
  }

  private checkHit(target: Robo, weapon: MeleeWeaponPart): void {
    const toTarget = target.position.clone().sub(this.owner.position);
    toTarget.y = 0;
    const dist = toTarget.length();
    if (dist > weapon.hitRange) return;

    const angleTo = Math.atan2(toTarget.x, toTarget.z);
    let diff = angleTo - this.owner.facingAngle;
    while (diff > Math.PI) diff -= 2 * Math.PI;
    while (diff < -Math.PI) diff += 2 * Math.PI;
    if (Math.abs(diff) > (weapon.hitArcDegrees * Math.PI) / 360) return;

    this.didHit = true;
    const dir = toTarget.normalize();
    const fx = this.owner.effects;
    const combo = this.comboIndex;
    const result = target.receiveHit(
      (weapon.damage * COMBO_DAMAGE[combo] * this.owner.stats.atkMult +
        (fx?.flatDamageBonus() ?? 0)) *
        (fx?.meleeDamageMult() ?? 1),
      weapon.enduranceDamage * COMBO_ENDURANCE[combo],
      dir,
      { shieldDamageMult: fx?.meleeShieldMult() ?? 1 },
    );
    if (result !== "invulnerable" && result !== "evaded") {
      sfx.meleeHit();
      target.applyKnockback(dir, weapon.knockbackSpeed * COMBO_KNOCKBACK[combo]);
      if (fx) {
        fx.onHit("melee", target.position.clone());
        if (result === "knockdown" || result === "guardbreak") {
          fx.onKnockdown();
        }
      }
    }
    // Shield parry: a melee hit that gets blocked by an ENGAGED shield
    // drains the attacker's own endurance (GAME_DESIGN §3.2).
    if (
      (result === "shielded" || result === "guardbreak") &&
      target.intent.shieldHeld &&
      target.loadout.leftArm.kind === "shield"
    ) {
      const parryDamage = target.loadout.leftArm.part.meleeParryEnduranceDamage;
      this.owner.health.drainEndurance(parryDamage);
      sfx.guardBreak(); // sharp counter cue distinct from the normal block tone
    }
  }

  private updateBladeVisual(): void {
    this.swingBlade.visible = true;
    const f = this.owner.facingAngle;
    this.swingBlade.position
      .copy(this.owner.position)
      .add(new THREE.Vector3(Math.sin(f) * 1.4, 0.2, Math.cos(f) * 1.4));
    this.swingBlade.rotation.y = f;
  }

  private reset(): void {
    this.state = "idle";
    this.swingBlade.visible = false;
    this.owner.externalMove = null;
    this.owner.actionLock = 0;
  }
}
