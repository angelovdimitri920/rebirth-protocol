import * as THREE from "three";
import { Robo } from "../robo/Robo";
import { Projectiles } from "./Projectiles";
import { sfx } from "../core/sfx";

// The equipped gun part drives all stats; body ATK multiplier scales damage.

export class Gun {
  private cooldown = 0;

  resetCooldown(): void {
    this.cooldown = 0;
  }

  constructor(
    private owner: Robo,
    private ownerTag: "player" | "enemy",
    private projectiles: Projectiles,
  ) {}

  update(dt: number, firing: boolean, target: Robo | null): void {
    this.cooldown -= dt;
    if (!firing || this.cooldown > 0) return;
    if (this.owner.controlLocked || this.owner.health.state === "knockdown")
      return;
    const rightArm = this.owner.loadout.rightArm;
    if (rightArm.kind !== "gun") return; // right arm is melee: no gun to fire

    const part = rightArm.part;
    this.cooldown = part.fireInterval;
    sfx.shot();

    const muzzle = new THREE.Vector3();
    this.owner.mesh.gunMuzzle.getWorldPosition(muzzle);

    let aimPoint: THREE.Vector3;
    if (target && target.health.state !== "dead") {
      aimPoint = target.position.clone();
      aimPoint.y = target.groundY + 1.0;
    } else {
      const f = this.owner.facingAngle;
      aimPoint = this.owner.position
        .clone()
        .add(new THREE.Vector3(Math.sin(f) * 10, 0, Math.cos(f) * 10));
    }

    const fx = this.owner.effects;
    this.projectiles.spawn(muzzle, aimPoint, this.ownerTag, {
      damage:
        (part.damage * this.owner.stats.atkMult + (fx?.flatDamageBonus() ?? 0)) *
        (fx?.gunDamageMult() ?? 1),
      enduranceDamage: part.enduranceDamage,
      speed: part.projectileSpeed,
      homingTurnRate: target !== null ? part.homingTurnRate : 0,
      source: "gun",
    });
  }
}
