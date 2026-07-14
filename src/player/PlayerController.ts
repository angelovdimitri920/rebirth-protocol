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
// the Custom Robo-style camera: an elevated isometric view that rotates to
// keep both fighters framed (HOLOSSEUM_REFERENCE.md), so WASD maps to
// screen directions recomputed from the camera's actual orientation every
// frame -- rotation can't desync movement.
// The robo faces where it's moving; it only squares up to the enemy while
// actually attacking (firing, meleeing, aiming a bomb, or shielding).
// Controls: WASD move, Space jump/hover (mash during knockdown, double-tap
// while airborne also air-dashes), Shift dash, LMB gun, RMB melee, Q hold
// to aim bomb/raise shield (WASD steers the reticule while held), E pod
// deploy/recall (WASD steers its launch direction while held), Tab toggle
// lock-on.

export class PlayerController {
  lockedOn = true; // targeting for homing/melee -- not camera or facing
  private cameraViewDir = new THREE.Vector3(1, 0, 0); // matches the fixed Z-axis spawn line

  constructor(
    private robo: Robo,
    private enemy: Robo,
    private input: Input,
    private camera: THREE.Camera,
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
    // A+A: tapping jump again while airborne triggers the same chassis-
    // specific air-dash/mobility move as the dedicated dash button.
    const airDashTap = !this.robo.grounded && input.wasDoubleTapped("Space");
    this.robo.intent.dashRequested = input.justPressed("ShiftLeft") || airDashTap;
    this.robo.intent.mashPressed = input.justPressed("Space");

    // Face movement direction by default; square up while attacking,
    // holding the shield up, or aiming a bomb (you want to face what
    // you're blocking or throwing at)
    const shieldWillBeHeld =
      this.robo.loadout.leftArm.kind === "shield" && input.held("KeyQ");
    const attacking =
      input.fireHeld || this.melee.busy || shieldWillBeHeld || this.bomb.aiming;
    this.robo.intent.faceAngle =
      attacking && target
        ? Math.atan2(
            this.enemy.position.x - this.robo.position.x,
            this.enemy.position.z - this.robo.position.z,
          )
        : null;

    // Homing dash: while locked on, dashes curve toward the target
    this.robo.intent.dashHomingPoint = target ? target.position : null;

    // --- Right arm: gun (LMB/B, held) or melee (RMB/B, pressed) --
    // whichever isn't equipped silently no-ops, so both buttons are always
    // safe to read regardless of loadout. ---
    if (input.meleePressed && enemyAlive) {
      if (!this.melee.busy) this.melee.tryStart(this.enemy);
      else this.melee.chain(this.enemy); // combo string follow-up
    }
    this.melee.update(dt, this.enemy);
    const gunFiring =
      this.robo.loadout.rightArm.kind === "gun" && input.fireHeld && !this.melee.busy;
    this.robo.intent.firingGun = gunFiring;
    this.gun.update(dt, gunFiring, target);

    // --- Left arm: bomb (Q/R, hold to aim -- stick steers the reticule,
    // release to throw) or shield (Q/R, HELD) -- Q is context-sensitive on
    // which part is equipped. Movement is already frozen while aiming
    // (leftArmActive), so reusing `move` for reticule-steering is free. ---
    if (this.robo.loadout.leftArm.kind === "shield") {
      this.robo.intent.shieldHeld = input.held("KeyQ") && !this.melee.busy;
      this.robo.intent.leftArmActive = this.robo.intent.shieldHeld;
    } else {
      this.robo.intent.shieldHeld = false;
      const aimHeld = input.held("KeyQ") && enemyAlive && !this.melee.busy;
      if (aimHeld && !this.bomb.aiming) this.bomb.startAim(this.enemy);
      else if (aimHeld && this.bomb.aiming) {
        this.bomb.updateAim(this.enemy);
        this.bomb.steerAim(move, dt);
      } else if (!aimHeld && this.bomb.aiming) this.bomb.release(this.enemy);
      this.robo.intent.leftArmActive = this.bomb.aiming;
    }

    // --- Pod: E deploys/recalls on press; holding E steers its launch
    // direction with the stick instead of moving (one stick, one job at a
    // time -- releasing E falls back to auto-aiming at the enemy). ---
    if (input.justPressed("KeyE")) {
      this.pod.toggle();
    }
    if (input.held("KeyE") && this.pod.deployed) {
      this.pod.steerAim(move, dt);
      this.robo.intent.moveDir.set(0, 0, 0);
    } else {
      this.pod.clearAim();
    }
    this.pod.update(dt, this.enemy);

    this.updateCamera(dt);
  }

  /** Isometric arena view that rotates to keep both fighters in frame
   *  (HOLOSSEUM_REFERENCE.md "Normal View"): looks at a point biased
   *  toward the player but not centered on them, from a direction kept
   *  perpendicular to the line between the two robos so that line reads
   *  left-right on screen no matter which way the fight drifts. Zooms out
   *  (orthographic frustum, not camera distance -- distance does nothing
   *  for an orthographic projection) as the fighters separate. */
  private updateCamera(dt: number): void {
    const C = TUNING.camera;
    const playerPos = this.robo.position;
    const enemyAlive = this.enemy.health.state !== "dead";

    let midpoint: THREE.Vector3;
    let desiredViewDir: THREE.Vector3;
    let sepDist = 0;

    if (enemyAlive) {
      const enemyPos = this.enemy.position;
      midpoint = playerPos.clone().lerp(enemyPos, C.targetBias);
      const sep = enemyPos.clone().sub(playerPos).setY(0);
      sepDist = sep.length();
      if (sepDist > 0.5) {
        // Perpendicular to the separation line, picking whichever of the
        // two valid perpendiculars is closer to the current direction so
        // the camera eases instead of flipping sides.
        desiredViewDir = new THREE.Vector3(-sep.z, 0, sep.x).normalize();
        if (desiredViewDir.dot(this.cameraViewDir) < 0) desiredViewDir.negate();
      } else {
        desiredViewDir = this.cameraViewDir.clone();
      }
    } else {
      midpoint = playerPos.clone();
      desiredViewDir = new THREE.Vector3(
        Math.sin(this.robo.facingAngle),
        0,
        Math.cos(this.robo.facingAngle),
      );
    }

    const k = Math.min(1, C.followLerp * dt);
    this.cameraViewDir.lerp(desiredViewDir, Math.min(1, C.rotateLerp * dt));
    if (this.cameraViewDir.lengthSq() > 1e-6) this.cameraViewDir.normalize();

    const desiredPos = midpoint
      .clone()
      .addScaledVector(this.cameraViewDir, -C.back)
      .setY(C.height);
    const lookAt = midpoint.clone().setY(1);

    this.camera.position.lerp(desiredPos, k);
    this.camera.lookAt(lookAt);

    if (this.camera instanceof THREE.OrthographicCamera) {
      const zoomT = THREE.MathUtils.clamp(
        (sepDist - C.zoomStartDistance) / C.zoomRange,
        0,
        1,
      );
      const targetSize = C.frustumSize * (1 + zoomT * C.zoomMax);
      const aspect =
        (this.camera.right - this.camera.left) / (this.camera.top - this.camera.bottom);
      const currentSize = this.camera.top - this.camera.bottom;
      const size = currentSize + (targetSize - currentSize) * k;
      this.camera.top = size / 2;
      this.camera.bottom = -size / 2;
      this.camera.left = (-size * aspect) / 2;
      this.camera.right = (size * aspect) / 2;
      this.camera.updateProjectionMatrix();
    }
  }
}
