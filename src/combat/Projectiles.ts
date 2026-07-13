import * as THREE from "three";
import RAPIER from "@dimforge/rapier3d-compat";
import { Physics } from "../physics/Physics";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Arena } from "../arena/Arena";

// All live projectiles, both robos'. Projectiles are physicsless tracers:
// each step they advance and raycast their swept path, so nothing tunnels.

interface Projectile {
  mesh: THREE.Mesh;
  pos: THREE.Vector3;
  dir: THREE.Vector3; // unit
  owner: "player" | "enemy";
  life: number;
  homing: boolean;
}

const AIM_HEIGHT = 1.0; // aim at chest, not feet

export class Projectiles {
  private live: Projectile[] = [];
  private geo = new THREE.SphereGeometry(0.12, 8, 8);
  private playerMat = new THREE.MeshBasicMaterial({ color: 0x66ccff });
  private enemyMat = new THREE.MeshBasicMaterial({ color: 0xff6655 });

  constructor(
    private physics: Physics,
    private scene: THREE.Scene,
    private arena: Arena,
  ) {}

  spawn(
    from: THREE.Vector3,
    towards: THREE.Vector3,
    owner: "player" | "enemy",
    homing: boolean,
  ): void {
    const mesh = new THREE.Mesh(
      this.geo,
      owner === "player" ? this.playerMat : this.enemyMat,
    );
    mesh.position.copy(from);
    this.scene.add(mesh);
    this.live.push({
      mesh,
      pos: from.clone(),
      dir: towards.clone().sub(from).normalize(),
      owner,
      life: TUNING.gun.projectileLifetime,
      homing,
    });
  }

  update(dt: number, player: Robo, enemy: Robo): void {
    const T = TUNING.gun;
    for (let i = this.live.length - 1; i >= 0; i--) {
      const p = this.live[i];
      p.life -= dt;
      if (p.life <= 0) {
        this.remove(i);
        continue;
      }

      // Homing: limited turn rate toward the target robo, within range
      const target = p.owner === "player" ? enemy : player;
      if (p.homing && target.health.state !== "dead") {
        const targetPos = target.position.clone();
        targetPos.y = target.groundY + AIM_HEIGHT;
        if (p.pos.distanceTo(targetPos) <= T.homingRange) {
          const desired = targetPos.sub(p.pos).normalize();
          rotateTowards(p.dir, desired, T.homingTurnRate * dt);
        }
      }

      // Swept raycast for this step's travel
      const stepLen = T.projectileSpeed * dt;
      const ray = new RAPIER.Ray(p.pos, p.dir);
      const ownerCollider = (p.owner === "player" ? player : enemy).collider;
      const hit = this.physics.world.castRay(
        ray,
        stepLen,
        true,
        undefined,
        undefined,
        ownerCollider,
      );

      if (hit) {
        const tag = this.physics.tagOf(hit.collider);
        if (tag?.kind === "crate") {
          this.arena.damageCrate(tag.id);
        } else if (tag?.kind === "robo") {
          const victim = tag.robo === "player" ? player : enemy;
          const result = victim.health.takeHit(T.damage, T.enduranceDamage);
          if (result !== "invulnerable") {
            victim.applyKnockback(p.dir, 2);
          }
        }
        this.remove(i);
        continue;
      }

      p.pos.addScaledVector(p.dir, stepLen);
      p.mesh.position.copy(p.pos);
    }
  }

  private remove(i: number): void {
    this.scene.remove(this.live[i].mesh);
    this.live.splice(i, 1);
  }
}

function rotateTowards(
  current: THREE.Vector3,
  desired: THREE.Vector3,
  maxAngle: number,
): void {
  const angle = current.angleTo(desired);
  if (angle < 1e-4) return;
  const t = Math.min(1, maxAngle / angle);
  current.lerp(desired, t).normalize();
}
