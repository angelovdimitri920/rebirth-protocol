import * as THREE from "three";
import RAPIER from "@dimforge/rapier3d-compat";
import { Physics } from "../physics/Physics";
import { TUNING } from "../core/tuning";
import { Health } from "../combat/Health";
import { buildRoboMesh, type RoboMeshParts } from "./RoboMesh";

// One robo: kinematic character controller + boost economy + health.
// Player and dummy both use this; a controller writes `intent` each step.
// Boost economy (GAME_DESIGN §3.3): jump/hover/air-dash share one gauge that
// only refills on landing; landing recovery scales with how much was spent;
// fully draining it adds an overheat penalty.

export interface RoboIntent {
  moveDir: THREE.Vector3; // world-space, horizontal, normalized or zero
  thrustHeld: boolean; // hold to jump / hover
  dashRequested: boolean;
  faceAngle: number | null; // yaw to face, or null to face moveDir
  mashPressed: boolean; // knockdown recovery mashing
}

const CAPSULE_HALF = 0.5;
const CAPSULE_RADIUS = 0.5;
const CENTER_Y = CAPSULE_HALF + CAPSULE_RADIUS; // capsule center at ground

export class Robo {
  health = new Health();
  mesh: RoboMeshParts;
  body: RAPIER.RigidBody;
  collider: RAPIER.Collider;
  private controller: RAPIER.KinematicCharacterController;

  // Boost state
  boost = TUNING.boost.max;
  grounded = true;
  landingRecovery = 0; // s remaining of post-landing lockout
  overheated = false;

  // Motion state
  private velocity = new THREE.Vector3();
  private dashTimer = 0;
  private dashDir = new THREE.Vector3();
  private knockback = new THREE.Vector3();
  /** While set, an external system (melee lunge) owns horizontal velocity. */
  externalMove: THREE.Vector3 | null = null;
  /** While > 0, control inputs are ignored but gravity still applies
   *  (melee swing/recovery: planted on the ground, falls in the air). */
  actionLock = 0;

  intent: RoboIntent = {
    moveDir: new THREE.Vector3(),
    thrustHeld: false,
    dashRequested: false,
    faceAngle: null,
    mashPressed: false,
  };

  private facing = 0;
  private flashTime = 0;

  constructor(
    physics: Physics,
    scene: THREE.Scene,
    tag: "player" | "enemy",
    spawn: THREE.Vector3,
    hullColor: number,
    accentColor: number,
  ) {
    this.mesh = buildRoboMesh(hullColor, accentColor);
    this.mesh.root.position.copy(spawn).setY(CENTER_Y);
    scene.add(this.mesh.root);

    this.body = physics.world.createRigidBody(
      RAPIER.RigidBodyDesc.kinematicPositionBased().setTranslation(
        spawn.x,
        CENTER_Y,
        spawn.z,
      ),
    );
    this.collider = physics.world.createCollider(
      RAPIER.ColliderDesc.capsule(CAPSULE_HALF, CAPSULE_RADIUS),
      this.body,
    );
    physics.tag(this.collider, { kind: "robo", robo: tag });

    this.controller = physics.world.createCharacterController(0.08);
    this.controller.enableAutostep(0.5, 0.2, true);
    this.controller.enableSnapToGround(0.4);
  }

  get position(): THREE.Vector3 {
    const t = this.body.translation();
    return new THREE.Vector3(t.x, t.y, t.z);
  }

  /** Feet-level position (for ground checks / AI). */
  get groundY(): number {
    return this.body.translation().y - CENTER_Y;
  }

  get controlLocked(): boolean {
    return (
      this.landingRecovery > 0 ||
      this.actionLock > 0 ||
      this.health.state === "knockdown" ||
      this.health.state === "dead" ||
      this.externalMove !== null
    );
  }

  update(dt: number): void {
    this.health.update(dt);
    const T = TUNING;

    if (this.intent.mashPressed) this.health.mash();

    const downed =
      this.health.state === "knockdown" || this.health.state === "dead";

    // --- Landing recovery / action lock timers ---
    if (this.landingRecovery > 0) this.landingRecovery -= dt;
    if (this.actionLock > 0) this.actionLock -= dt;

    // --- Horizontal movement ---
    const horiz = new THREE.Vector3();
    if (this.externalMove) {
      // Melee lunge (or similar) owns movement this step
      horiz.copy(this.externalMove);
      this.velocity.y = 0;
    } else if (downed || this.landingRecovery > 0 || this.actionLock > 0) {
      // No control: drift to stop
      this.velocity.x *= 0.85;
      this.velocity.z *= 0.85;
      horiz.set(this.velocity.x, 0, this.velocity.z);
    } else if (this.dashTimer > 0) {
      this.dashTimer -= dt;
      horiz.copy(this.dashDir).multiplyScalar(T.boost.airDashSpeed);
      this.velocity.y = 0; // dashes are horizontal; gravity suspended
      this.velocity.x = horiz.x;
      this.velocity.z = horiz.z;
    } else if (this.grounded) {
      horiz.copy(this.intent.moveDir).multiplyScalar(T.move.runSpeed);
      this.velocity.x = horiz.x;
      this.velocity.z = horiz.z;
    } else {
      // Air steering: accelerate toward desired air velocity
      const desired = this.intent.moveDir
        .clone()
        .multiplyScalar(T.move.airControlSpeed);
      this.velocity.x += (desired.x - this.velocity.x) * Math.min(1, 4 * dt);
      this.velocity.z += (desired.z - this.velocity.z) * Math.min(1, 4 * dt);
      horiz.set(this.velocity.x, 0, this.velocity.z);
    }

    // --- Knockback decay (applies on top of movement) ---
    horiz.add(this.knockback);
    this.knockback.multiplyScalar(Math.max(0, 1 - 6 * dt));

    // --- Boost: thrust / dash ---
    const canBoost =
      !downed &&
      this.landingRecovery <= 0 &&
      this.actionLock <= 0 &&
      !this.overheated &&
      this.boost > 0 &&
      this.externalMove === null;

    if (canBoost && this.intent.thrustHeld && this.dashTimer <= 0) {
      this.velocity.y = T.boost.jumpThrust;
      this.spendBoost(T.boost.thrustDrainPerSec * dt);
    }
    if (
      canBoost &&
      this.intent.dashRequested &&
      this.dashTimer <= 0 &&
      this.boost >= T.boost.airDashCost
    ) {
      const dir =
        this.intent.moveDir.lengthSq() > 0.01
          ? this.intent.moveDir.clone()
          : new THREE.Vector3(Math.sin(this.facing), 0, Math.cos(this.facing));
      this.dashDir.copy(dir.normalize());
      this.dashTimer = T.boost.airDashDuration;
      this.spendBoost(T.boost.airDashCost);
      if (this.grounded) this.velocity.y = 3; // ground dash lifts into a hop
    }

    // --- Gravity ---
    if (this.dashTimer <= 0 && this.externalMove === null) {
      this.velocity.y += T.move.gravity * dt;
      if (this.grounded && this.velocity.y < 0) this.velocity.y = -2; // stick
    }

    // --- Move via character controller ---
    const move = new THREE.Vector3(
      horiz.x * dt,
      this.velocity.y * dt,
      horiz.z * dt,
    );
    this.controller.computeColliderMovement(this.collider, move);
    const corrected = this.controller.computedMovement();
    const pos = this.body.translation();
    this.body.setNextKinematicTranslation({
      x: pos.x + corrected.x,
      y: pos.y + corrected.y,
      z: pos.z + corrected.z,
    });

    const wasGrounded = this.grounded;
    this.grounded = this.controller.computedGrounded();
    if (this.grounded && this.velocity.y < 0) this.velocity.y = 0;

    // --- Landing: recovery scales with boost spent, then gauge refills ---
    if (!wasGrounded && this.grounded) {
      const spentFraction = 1 - this.boost / T.boost.max;
      this.landingRecovery =
        T.boost.landRecoveryBase +
        T.boost.landRecoveryScale * spentFraction +
        (this.overheated ? T.boost.overheatExtraRecovery : 0);
      this.boost = T.boost.max;
      this.overheated = false;
      this.dashTimer = 0;
    }

    // --- Facing (frozen mid-swing/recovery: commitment is punishable) ---
    if (!downed && this.externalMove === null && this.actionLock <= 0) {
      const target =
        this.intent.faceAngle !== null
          ? this.intent.faceAngle
          : this.intent.moveDir.lengthSq() > 0.01
            ? Math.atan2(this.intent.moveDir.x, this.intent.moveDir.z)
            : this.facing;
      this.facing = dampAngle(this.facing, target, T.move.turnRate, dt);
    }

    this.syncVisual(dt);
  }

  private spendBoost(amount: number): void {
    this.boost -= amount;
    if (this.boost <= 0) {
      this.boost = 0;
      this.overheated = true;
    }
  }

  /** Face this world yaw immediately (used by melee lunge). */
  setFacing(angle: number): void {
    this.facing = angle;
  }

  get facingAngle(): number {
    return this.facing;
  }

  applyKnockback(dir: THREE.Vector3, speed: number): void {
    this.knockback.copy(dir).setY(0).normalize().multiplyScalar(speed);
  }

  private syncVisual(dt: number): void {
    const t = this.body.translation();
    this.mesh.root.position.set(t.x, t.y, t.z);
    this.mesh.root.rotation.y = this.facing;

    // Knockdown: fall over backward. Otherwise stand.
    const targetTilt = this.health.state === "knockdown" ? -Math.PI / 2 : 0;
    this.mesh.body.rotation.x +=
      (targetTilt - this.mesh.body.rotation.x) * Math.min(1, 10 * dt);

    // Rebirth / invulnerable: flash
    this.flashTime += dt;
    const flashing =
      this.health.state === "rebirth" || this.health.state === "knockdown";
    const flash = flashing ? (Math.sin(this.flashTime * 25) > 0 ? 1 : 0.25) : 1;
    for (const m of this.mesh.materials) {
      m.opacity = flash;
      m.transparent = flashing;
    }

    // Dead: sink and fade (simple Stage 1 end state)
    if (this.health.state === "dead") {
      this.mesh.root.scale.multiplyScalar(Math.max(0, 1 - 1.5 * dt));
    }
  }
}

function dampAngle(from: number, to: number, rate: number, dt: number): number {
  let diff = to - from;
  while (diff > Math.PI) diff -= 2 * Math.PI;
  while (diff < -Math.PI) diff += 2 * Math.PI;
  return from + diff * Math.min(1, rate * dt);
}
