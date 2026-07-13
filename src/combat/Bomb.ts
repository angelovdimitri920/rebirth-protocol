import * as THREE from "three";
import { Robo } from "../robo/Robo";
import { Arena } from "../arena/Arena";
import { sfx } from "../core/sfx";

// Bomb slot (GAME_DESIGN §2.1): slower AoE secondary on a cooldown.
// Lobbed in an arc to the target's position at launch; detonates on
// arrival (or floor contact) and applies AoE damage with falloff-free
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

  tryThrow(target: Robo): boolean {
    if (!this.ready || this.owner.controlLocked) return false;
    const leftArm = this.owner.loadout.leftArm;
    if (leftArm.kind !== "bomb") return false; // left arm is a shield: no bomb
    const part = leftArm.part;
    this.cooldownRemaining = part.cooldown;

    const start = this.owner.position.clone().add(new THREE.Vector3(0, 0.8, 0));
    // Lead the throw: aim where the target will be when the bomb lands.
    // Two passes: estimate flight time from current position, then re-aim.
    let end = target.position.clone().setY(target.groundY + 0.2);
    for (let pass = 0; pass < 2; pass++) {
      const flight = Math.max(0.5, start.distanceTo(end) / 18);
      end = target.position
        .clone()
        .addScaledVector(target.horizontalVelocity, flight)
        .setY(target.groundY + 0.2);
    }
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

  update(dt: number, player: Robo, enemy: Robo): void {
    this.cooldownRemaining -= dt;

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
