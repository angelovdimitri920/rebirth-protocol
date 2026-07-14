import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Gun } from "../combat/Gun";
import { Bomb } from "../combat/Bomb";
import { Pod } from "../combat/Pod";
import { Melee } from "../combat/Melee";

// Basic pressure-test opponent, not a real AI: orbits the player at mid
// range, strafes, occasionally jumps or dashes, fires in bursts, lobs its
// bomb when it's off cooldown (or raises its shield periodically at close
// range if that's its left arm instead), keeps its pod deployed, and goes
// for melee when the player is close or landing-recovery vulnerable.

export class DummyAI {
  private strafeSign = 1;
  private decisionTimer = 0;
  private firing = false;
  private fireTimer = 0;
  private bombTimer = 3; // don't open with a bomb
  private bombAiming = false;
  private bombAimTimer = 0;
  private meleeTimer = 2;
  private shieldTimer = 1;
  private shieldEngaged = false;

  constructor(
    private robo: Robo,
    private player: Robo,
    private gun: Gun,
    private bomb: Bomb,
    private pod: Pod,
    private melee: Melee,
  ) {}

  update(dt: number): void {
    const A = TUNING.ai;
    const toPlayer = this.player.position.clone().sub(this.robo.position);
    toPlayer.y = 0;
    const dist = toPlayer.length();
    const dirToPlayer = toPlayer.clone().normalize();

    // Rethink strafe direction periodically
    this.decisionTimer -= dt;
    if (this.decisionTimer <= 0) {
      this.decisionTimer = A.decisionInterval * (0.6 + Math.random() * 0.8);
      if (Math.random() < 0.4) this.strafeSign *= -1;
    }

    // Movement: keep orbit distance band, strafe around player
    const move = new THREE.Vector3();
    const strafe = new THREE.Vector3(
      -dirToPlayer.z * this.strafeSign,
      0,
      dirToPlayer.x * this.strafeSign,
    );
    if (dist > A.orbitRadiusMax) move.add(dirToPlayer);
    else if (dist < A.orbitRadiusMin) move.addScaledVector(dirToPlayer, -1);
    move.add(strafe).normalize();

    this.robo.intent.moveDir.copy(move);
    this.robo.intent.faceAngle = Math.atan2(dirToPlayer.x, dirToPlayer.z);
    this.robo.intent.mashPressed = Math.random() < 8 * dt; // ~8 mash/s

    // Occasional jump / dash to be a harder target
    this.robo.intent.thrustHeld =
      this.robo.intent.thrustHeld && this.robo.boost > 20
        ? Math.random() > 0.1 // keep short hops short
        : Math.random() < A.jumpChancePerSec * dt;
    this.robo.intent.dashRequested = Math.random() < A.dashChancePerSec * dt;

    const playerAlive = this.player.health.state !== "dead";

    // Melee: punish a close player, especially during landing recovery
    this.meleeTimer -= dt;
    if (
      playerAlive &&
      !this.melee.busy &&
      this.meleeTimer <= 0 &&
      dist < 10 &&
      (this.player.landingRecovery > 0 || Math.random() < 0.5)
    ) {
      this.melee.tryStart(this.player);
      this.meleeTimer = 2.5 + Math.random() * 2;
    }
    // Sometimes press the string through (~7 attempts/s while in recovery)
    if (this.melee.busy && Math.random() < 7 * dt) {
      this.melee.chain(this.player);
    }
    this.melee.update(dt, this.player);

    // Fire gun in bursts
    this.fireTimer -= dt;
    if (this.fireTimer <= 0) {
      this.firing = !this.firing;
      this.fireTimer = this.firing ? 1.2 : A.fireInterval;
    }
    const gunFiring =
      this.robo.loadout.rightArm.kind === "gun" &&
      this.firing &&
      playerAlive &&
      !this.melee.busy;
    this.robo.intent.firingGun = gunFiring;
    this.gun.update(dt, gunFiring, playerAlive ? this.player : null);

    // Left arm: bomb OR shield, whichever this build actually has
    if (this.robo.loadout.leftArm.kind === "shield") {
      this.shieldTimer -= dt;
      if (this.shieldTimer <= 0) {
        this.shieldEngaged = !this.shieldEngaged;
        this.shieldTimer = this.shieldEngaged
          ? 1.0 + Math.random() // hold it up for a beat
          : 0.6 + Math.random() * 0.8; // then rest -- rooted isn't free
      }
      this.robo.intent.shieldHeld =
        this.shieldEngaged && dist < 10 && !this.melee.busy;
      this.robo.intent.leftArmActive = this.robo.intent.shieldHeld;
    } else {
      this.robo.intent.shieldHeld = false;
      this.bombTimer -= dt;
      if (
        !this.bombAiming &&
        this.bombTimer <= 0 &&
        playerAlive &&
        this.bomb.ready &&
        dist < 18
      ) {
        this.bomb.startAim(this.player);
        this.bombAiming = true;
        this.bombAimTimer = 0.25 + Math.random() * 0.3; // hold, then release
      }
      if (this.bombAiming) {
        this.bomb.updateAim(this.player);
        this.bombAimTimer -= dt;
        if (this.bombAimTimer <= 0 || !playerAlive) {
          this.bomb.release(this.player);
          this.bombAiming = false;
          this.bombTimer = 2 + Math.random() * 3;
        }
      }
      this.robo.intent.leftArmActive = this.bombAiming;
    }

    // Keep the pod out
    if (!this.pod.deployed && this.robo.health.state === "active") {
      this.pod.toggle();
    }
    this.pod.update(dt, this.player);
  }
}
