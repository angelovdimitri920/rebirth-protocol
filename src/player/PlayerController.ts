import * as THREE from "three";
import { Input } from "../core/input";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Gun } from "../combat/Gun";
import { Melee } from "../combat/Melee";
import { Bomb } from "../combat/Bomb";
import { Pod } from "../combat/Pod";
import { sfx } from "../core/sfx";

// Reads input, writes the player robo's intent, drives weapons, and owns
// the Custom Robo-style camera: an elevated view from the player's side of
// the arena that never rotates, so WASD maps directly to screen directions.
// The robo faces where it's moving; it only squares up to the enemy while
// actually attacking (firing or melee).
// Controls: WASD move, Space jump/hover (mash during knockdown), Shift dash,
// LMB gun, RMB melee, Q bomb, E pod deploy/recall, Tab toggle lock-on.

export class PlayerController {
  lockedOn = true; // targeting for homing/melee -- not camera or facing

  constructor(
    private robo: Robo,
    private enemy: Robo,
    private input: Input,
    private camera: THREE.PerspectiveCamera,
    public gun: Gun,
    public melee: Melee,
    public bomb: Bomb,
    public pod: Pod,
  ) {}

  update(dt: number): void {
    const input = this.input;

    if (input.justPressed("Tab")) {
      this.lockedOn = !this.lockedOn;
      sfx.lockToggle(this.lockedOn);
    }
    const enemyAlive = this.enemy.health.state !== "dead";
    const target = this.lockedOn && enemyAlive ? this.enemy : null;

    // --- Movement intent: derived from the camera's actual orientation,
    // not a hardcoded world axis. This is deliberate: a fixed camera still
    // has a specific facing, and hand-picking which world axis is "screen
    // right" is exactly what produced the inverted-controls bug twice in a
    // row. Reading it straight off the camera's quaternion can't drift out
    // of sync with whatever the camera is actually doing. ---
    const screenRight = new THREE.Vector3(1, 0, 0)
      .applyQuaternion(this.camera.quaternion)
      .setY(0)
      .normalize();
    const screenForward = new THREE.Vector3(0, 0, -1)
      .applyQuaternion(this.camera.quaternion)
      .setY(0)
      .normalize();

    const move = new THREE.Vector3();
    if (input.stickMoveDir) {
      // Analog stick: direction only (the sim has no analog-speed model),
      // magnitude already <=1 since it's read straight off the pad's axes.
      move
        .addScaledVector(screenForward, input.stickMoveDir.z)
        .addScaledVector(screenRight, input.stickMoveDir.x);
      if (move.lengthSq() > 1) move.normalize();
    } else {
      if (input.held("KeyW")) move.add(screenForward);
      if (input.held("KeyS")) move.sub(screenForward);
      if (input.held("KeyD")) move.add(screenRight);
      if (input.held("KeyA")) move.sub(screenRight);
      if (move.lengthSq() > 0) move.normalize();
    }

    this.robo.intent.moveDir.copy(move);
    this.robo.intent.thrustHeld = input.held("Space");
    this.robo.intent.dashRequested = input.justPressed("ShiftLeft");
    this.robo.intent.mashPressed = input.justPressed("Space");

    // Face movement direction by default; square up only while attacking
    const attacking = input.fireHeld || this.melee.busy;
    this.robo.intent.faceAngle =
      attacking && target
        ? Math.atan2(
            this.enemy.position.x - this.robo.position.x,
            this.enemy.position.z - this.robo.position.z,
          )
        : null;

    // Homing dash: while locked on, dashes curve toward the target
    this.robo.intent.dashHomingPoint = target ? target.position : null;

    // --- Weapons ---
    if (input.meleePressed && enemyAlive) {
      if (!this.melee.busy) this.melee.tryStart(this.enemy);
      else this.melee.chain(this.enemy); // combo string follow-up
    }
    this.melee.update(dt, this.enemy);
    this.gun.update(dt, input.fireHeld && !this.melee.busy, target);
    if (input.justPressed("KeyQ") && enemyAlive && !this.melee.busy) {
      this.bomb.tryThrow(this.enemy);
    }
    if (input.justPressed("KeyE")) {
      this.pod.toggle();
    }
    this.pod.update(dt, this.enemy);

    this.updateCamera(dt);
  }

  private updateCamera(dt: number): void {
    const C = TUNING.camera;
    const playerPos = this.robo.position;

    const desiredPos = new THREE.Vector3(
      playerPos.x,
      C.height,
      playerPos.z - C.back,
    );

    const lookAt = new THREE.Vector3(
      playerPos.x,
      1,
      playerPos.z + C.lookAhead,
    );
    if (this.enemy.health.state !== "dead") {
      lookAt.lerp(
        this.enemy.position.clone().setY(this.enemy.groundY + 1),
        C.targetBias,
      );
    }

    const k = Math.min(1, C.followLerp * dt);
    this.camera.position.lerp(desiredPos, k);
    this.camera.lookAt(lookAt);
  }
}
