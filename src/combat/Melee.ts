import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";

// Melee with a gap-closer (GAME_DESIGN §3.1): high commitment, punishable
// recovery on whiff. State machine: idle -> lunge -> swing -> recovery.
// Within closeRange the lunge is skipped and it's a direct swing.
// Stage 1 keeps a single swing; combo strings come later.

type MeleeState = "idle" | "lunge" | "swing" | "recovery";

export class Melee {
  state: MeleeState = "idle";
  private timer = 0;
  private didHit = false;
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

    const T = TUNING.melee;
    const dist = this.owner.position.distanceTo(target.position);
    if (dist > T.lungeRange) return; // out of range: no-op

    this.didHit = false;
    if (dist <= T.closeRange) {
      this.enterSwing();
    } else {
      this.state = "lunge";
      this.timer = T.lungeMaxDuration;
    }
  }

  private enterSwing(): void {
    this.state = "swing";
    this.timer = TUNING.melee.swingActiveTime;
    this.owner.externalMove = null;
    // Planted on the ground / falls in the air, but can't act
    this.owner.actionLock = 10; // held while swing+recovery run; reset clears
  }

  private enterRecovery(duration: number): void {
    this.state = "recovery";
    this.timer = duration;
    this.owner.externalMove = null;
    this.owner.actionLock = 10;
  }

  update(dt: number, target: Robo): void {
    const T = TUNING.melee;

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
          this.enterRecovery(T.whiffRecovery);
        }
        break;
      }

      case "swing": {
        if (!this.didHit) this.checkHit(target);
        this.updateBladeVisual();
        this.timer -= dt;
        if (this.timer <= 0) {
          this.enterRecovery(this.didHit ? T.hitRecovery : T.whiffRecovery);
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

  private checkHit(target: Robo): void {
    const T = TUNING.melee;
    const toTarget = target.position.clone().sub(this.owner.position);
    toTarget.y = 0;
    const dist = toTarget.length();
    if (dist > T.hitRange) return;

    const angleTo = Math.atan2(toTarget.x, toTarget.z);
    let diff = angleTo - this.owner.facingAngle;
    while (diff > Math.PI) diff -= 2 * Math.PI;
    while (diff < -Math.PI) diff += 2 * Math.PI;
    if (Math.abs(diff) > (T.hitArcDegrees * Math.PI) / 360) return;

    this.didHit = true;
    const dir = toTarget.normalize();
    const fx = this.owner.effects;
    const result = target.receiveHit(
      (T.damage * this.owner.stats.atkMult + (fx?.flatDamageBonus() ?? 0)) *
        (fx?.meleeDamageMult() ?? 1),
      T.enduranceDamage,
      dir,
      { shieldDamageMult: fx?.meleeShieldMult() ?? 1 },
    );
    if (result !== "invulnerable" && result !== "evaded") {
      target.applyKnockback(dir, T.knockbackSpeed);
      if (fx) {
        fx.onHit("melee", target.position.clone());
        if (result === "knockdown" || result === "guardbreak") {
          fx.onKnockdown();
        }
      }
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
