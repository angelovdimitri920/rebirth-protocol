import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Projectiles } from "./Projectiles";

// One basic gun per robo: hold to fire, fixed interval, shots home toward
// the locked target (homing strength/range live in tuning).

export class Gun {
  private cooldown = 0;

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

    this.cooldown = TUNING.gun.fireInterval;

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

    this.projectiles.spawn(muzzle, aimPoint, this.ownerTag, target !== null);
  }
}
