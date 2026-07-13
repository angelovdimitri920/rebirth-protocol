import * as THREE from "three";
import { Input } from "../core/input";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Gun } from "../combat/Gun";
import { Melee } from "../combat/Melee";

// Reads input, writes the player robo's intent, drives gun/melee, and owns
// the third-person camera + lock-on state.
// Controls: WASD move, Space jump/hover (mash during knockdown), Shift dash,
// LMB gun, RMB melee, Tab toggle lock-on.

export class PlayerController {
  lockedOn = true; // Stage 1 default: locked to the only enemy
  private freeYaw = 0;
  private freePitch = 0.35;

  constructor(
    private robo: Robo,
    private enemy: Robo,
    private input: Input,
    private camera: THREE.PerspectiveCamera,
    public gun: Gun,
    public melee: Melee,
  ) {}

  update(dt: number): void {
    const input = this.input;

    if (input.justPressed("Tab")) this.lockedOn = !this.lockedOn;
    const enemyAlive = this.enemy.health.state !== "dead";
    const target = this.lockedOn && enemyAlive ? this.enemy : null;

    // --- Camera yaw basis for movement ---
    let camYaw: number;
    if (target) {
      const toEnemy = this.enemy.position.clone().sub(this.robo.position);
      camYaw = Math.atan2(toEnemy.x, toEnemy.z);
    } else {
      this.freeYaw -= input.mouseDx * 0.003;
      this.freePitch = THREE.MathUtils.clamp(
        this.freePitch + input.mouseDy * 0.003,
        -0.2,
        1.2,
      );
      camYaw = this.freeYaw;
    }

    // --- Movement intent (camera-relative WASD) ---
    const move = new THREE.Vector3();
    if (input.held("KeyW")) move.z += 1;
    if (input.held("KeyS")) move.z -= 1;
    if (input.held("KeyA")) move.x += 1;
    if (input.held("KeyD")) move.x -= 1;
    if (move.lengthSq() > 0) {
      move.normalize();
      const sin = Math.sin(camYaw);
      const cos = Math.cos(camYaw);
      move.set(move.x * cos + move.z * sin, 0, -move.x * sin + move.z * cos);
    }

    this.robo.intent.moveDir.copy(move);
    this.robo.intent.thrustHeld = input.held("Space");
    this.robo.intent.dashRequested = input.justPressed("ShiftLeft");
    this.robo.intent.mashPressed = input.justPressed("Space");
    this.robo.intent.faceAngle = target
      ? Math.atan2(
          this.enemy.position.x - this.robo.position.x,
          this.enemy.position.z - this.robo.position.z,
        )
      : null;

    // --- Weapons ---
    if (input.meleePressed && !this.melee.busy && enemyAlive) {
      this.melee.tryStart(this.enemy);
    }
    this.melee.update(dt, this.enemy);
    this.gun.update(dt, input.fireHeld && !this.melee.busy, target);

    this.updateCamera(dt, camYaw, target);
  }

  private updateCamera(
    dt: number,
    camYaw: number,
    target: Robo | null,
  ): void {
    const C = TUNING.camera;
    const playerPos = this.robo.position;

    const pitch = target ? 0.3 : this.freePitch;
    const back = new THREE.Vector3(
      -Math.sin(camYaw) * Math.cos(pitch),
      Math.sin(pitch),
      -Math.cos(camYaw) * Math.cos(pitch),
    );
    const desiredPos = playerPos
      .clone()
      .addScaledVector(back, C.distance)
      .add(new THREE.Vector3(0, C.height * 0.4, 0));
    if (desiredPos.y < 0.5) desiredPos.y = 0.5;

    const lookAt = playerPos.clone().setY(playerPos.y + C.lookAtHeight);
    if (target) {
      lookAt.lerp(
        target.position.clone().setY(target.groundY + 1),
        C.targetBias,
      );
    }

    const k = Math.min(1, C.followLerp * dt);
    this.camera.position.lerp(desiredPos, k);
    this.camera.lookAt(lookAt);
  }
}
