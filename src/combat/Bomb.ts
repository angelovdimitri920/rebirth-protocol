import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Arena } from "../arena/Arena";
import { sfx } from "../core/sfx";

// Bomb slot (GAME_DESIGN §2.1): slower AoE secondary on a cooldown, aimed
// with a hold-to-aim / release-to-throw reticule (HOLOSSEUM_REFERENCE.md).
// Holding the bomb input opens the reticule and keeps it live-tracking its
// default aim point every frame; releasing deploys the bomb wherever the
// reticule currently sits. The stick can additionally nudge the reticule
// away from its default point (steerAim), clamped so the total distance
// from the robo never exceeds the part's reticuleRange. Lobbed in an arc;
// detonates on arrival and applies AoE damage with falloff-free
// simplicity -- inside the radius is inside the blast.

interface LiveBomb {
  mesh: THREE.Mesh;
  start: THREE.Vector3;
  end: THREE.Vector3;
  t: number; // 0..1 along the arc
  flightTime: number;
  arcHeight: number;
}

export class Bomb {
  cooldownRemaining = 0;
  /** True while the reticule is open (button held). Movement halts hard
   *  while this is true -- you're rooted and vulnerable while aiming. */
  aiming = false;
  /** Current reticule world position while aiming, or null. */
  aimPoint: THREE.Vector3 | null = null;
  /** Player-steered nudge on top of the default aim point, persists for
   *  the duration of one aim session (reset on startAim/release). */
  private manualOffset = new THREE.Vector3();
  private live: LiveBomb[] = [];
  private blasts: { mesh: THREE.Mesh; timer: number }[] = [];

  constructor(
    private owner: Robo,
    private scene: THREE.Object3D,
    private arena: Arena,
  ) {}

  get ready(): boolean {
    return this.cooldownRemaining <= 0;
  }

  resetCooldown(): void {
    this.cooldownRemaining = 0;
  }

  /** Opens the reticule. Call once when the bomb input is first pressed. */
  startAim(target: Robo): boolean {
    if (!this.ready || this.owner.controlLocked) return false;
    if (this.owner.loadout.leftArm.kind !== "bomb") return false;
    this.aiming = true;
    this.manualOffset.set(0, 0, 0);
    this.updateAim(target);
    return true;
  }

  /** Call every frame the bomb input is held, to keep the reticule tracking
   *  its default aim point (the enemy, or a fixed point ahead of self),
   *  plus whatever manual offset steerAim has accumulated. */
  updateAim(target: Robo): void {
    if (!this.aiming) return;
    const leftArm = this.owner.loadout.leftArm;
    if (leftArm.kind !== "bomb") return;
    const part = leftArm.part;
    const start = this.owner.position;
    const groundY =
      part.reticuleAnchor === "target" && target.health.state !== "dead"
        ? target.groundY
        : this.owner.groundY;

    let base: THREE.Vector3;
    if (part.reticuleAnchor === "target" && target.health.state !== "dead") {
      const toTarget = target.position.clone().sub(start).setY(0);
      base =
        toTarget.lengthSq() > 1e-4
          ? start.clone().addScaledVector(toTarget.normalize(), Math.min(toTarget.length(), part.reticuleRange))
          : start.clone();
    } else {
      // Self-anchored (or target anchor with no valid target): fixed point
      // straight ahead, at reticuleRange -- can't be aimed further out.
      const f = this.owner.facingAngle;
      base = start
        .clone()
        .add(new THREE.Vector3(Math.sin(f) * part.reticuleRange, 0, Math.cos(f) * part.reticuleRange));
    }

    // Apply the manual offset, then clamp the TOTAL distance from the
    // robo to reticuleRange -- steering can't out-range the weapon.
    const point = base.clone().add(this.manualOffset).setY(0);
    const fromSelf = point.clone().sub(start).setY(0);
    if (fromSelf.length() > part.reticuleRange) {
      fromSelf.setLength(part.reticuleRange);
      point.copy(start).add(fromSelf);
    }
    this.aimPoint = point.setY(groundY + 0.2);
  }

  /** Call each frame the bomb input is held with stick deflection, to nudge
   *  the reticule's manual offset away from its default aim point. */
  steerAim(dir: THREE.Vector3, dt: number): void {
    if (!this.aiming || dir.lengthSq() < 1e-4) return;
    this.manualOffset.addScaledVector(dir, TUNING.aimSteer.bombOffsetSpeed * dt);
  }

  /** Deploys the bomb at the current reticule position and closes it.
   *  Call when the bomb input is released. No-ops if not currently aiming. */
  release(target: Robo): boolean {
    if (!this.aiming) return false;
    this.aiming = false;
    const leftArm = this.owner.loadout.leftArm;
    if (leftArm.kind !== "bomb") return false;
    const part = leftArm.part;
    this.cooldownRemaining = part.cooldown;

    const start = this.owner.position.clone().add(new THREE.Vector3(0, 0.8, 0));
    const end = (this.aimPoint ?? target.position.clone().setY(target.groundY + 0.2)).clone();
    this.aimPoint = null;
    const dist = start.distanceTo(end);
    const mesh = new THREE.Mesh(
      new THREE.SphereGeometry(0.28, 10, 10),
      new THREE.MeshStandardMaterial({
        color: 0x222228,
        emissive: 0xff5522,
        emissiveIntensity: 0.7,
      }),
    );
    mesh.position.copy(start);
    this.scene.add(mesh);
    this.live.push({
      mesh,
      start,
      end,
      t: 0,
      flightTime: Math.max(0.5, dist / 18), // faster lob at range
      arcHeight: part.arcHeight,
    });
    sfx.bombThrow();
    return true;
  }

  /** Closes the reticule without throwing (knockdown/death mid-aim). */
  cancelAim(): void {
    this.aiming = false;
    this.aimPoint = null;
  }

  update(dt: number, player: Robo, enemy: Robo): void {
    this.cooldownRemaining -= dt;
    if (
      this.aiming &&
      (this.owner.health.state === "knockdown" || this.owner.health.state === "dead")
    ) {
      this.cancelAim();
    }

    // Delayed cluster mini-blasts (sim-time, not wall-clock)
    for (let i = this.pendingClusters.length - 1; i >= 0; i--) {
      const c = this.pendingClusters[i];
      c.timer -= dt;
      if (c.timer <= 0) {
        this.pendingClusters.splice(i, 1);
        c.fire();
      }
    }

    for (let i = this.live.length - 1; i >= 0; i--) {
      const b = this.live[i];
      b.t += dt / b.flightTime;
      if (b.t >= 1) {
        this.detonate(b.end, player, enemy);
        this.scene.remove(b.mesh);
        this.live.splice(i, 1);
        continue;
      }
      // Parabolic arc: lerp + sine bump
      b.mesh.position.lerpVectors(b.start, b.end, b.t);
      b.mesh.position.y += Math.sin(b.t * Math.PI) * b.arcHeight;
    }

    // Blast visuals fade out
    for (let i = this.blasts.length - 1; i >= 0; i--) {
      const blast = this.blasts[i];
      blast.timer -= dt;
      blast.mesh.scale.multiplyScalar(1 + 4 * dt);
      (blast.mesh.material as THREE.MeshBasicMaterial).opacity = Math.max(
        0,
        blast.timer / 0.35,
      );
      if (blast.timer <= 0) {
        this.scene.remove(blast.mesh);
        this.blasts.splice(i, 1);
      }
    }
  }

  private detonate(
    at: THREE.Vector3,
    player: Robo,
    enemy: Robo,
    isCluster = false,
  ): void {
    const leftArm = this.owner.loadout.leftArm;
    if (leftArm.kind !== "bomb") return; // can't happen: tryThrow already gated this
    const part = leftArm.part;
    const fx = this.owner.effects;
    const scale = isCluster ? 0.6 : 1;

    const blastMesh = new THREE.Mesh(
      new THREE.SphereGeometry(part.blastRadius * 0.5 * scale, 16, 16),
      new THREE.MeshBasicMaterial({
        color: 0xffaa44,
        transparent: true,
        opacity: 0.8,
      }),
    );
    blastMesh.position.copy(at);
    this.scene.add(blastMesh);
    this.blasts.push({ mesh: blastMesh, timer: 0.35 });
    sfx.explosion();

    // AoE hits BOTH robos -- your own bomb can knock you down. Skill issue.
    for (const robo of [player, enemy]) {
      const toRobo = robo.position.clone().sub(at);
      if (toRobo.length() <= part.blastRadius * scale + 0.5) {
        const result = robo.receiveHit(
          (part.damage * this.owner.stats.atkMult +
            (fx?.flatDamageBonus() ?? 0)) *
            scale,
          part.enduranceDamage * scale,
          toRobo.setY(0).normalize(),
        );
        if (fx && robo !== this.owner && result !== "invulnerable") {
          fx.onHit("bomb", robo.position.clone());
          if (result === "knockdown" || result === "guardbreak") {
            fx.onKnockdown();
          }
        }
      }
    }
    // Crates inside the blast are destroyed outright
    this.arena.damageCratesInRadius(at, part.blastRadius * scale);

    // Cluster Shell boon: follow-up mini-blasts
    if (!isCluster && fx) {
      for (const offset of fx.clusterOffsets()) {
        const miniAt = at.clone().add(offset);
        this.pendingClusters.push({
          timer: 0.3,
          fire: () => this.detonate(miniAt, player, enemy, true),
        });
      }
    }
  }

  private pendingClusters: { timer: number; fire: () => void }[] = [];
}
