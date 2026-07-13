import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Gun } from "../combat/Gun";
import { Bomb } from "../combat/Bomb";
import { Pod } from "../combat/Pod";
import { Melee } from "../combat/Melee";

// Basic pressure-test opponent, not a real AI: orbits the player at mid
// range, strafes, occasionally jumps or dashes, fires in bursts, lobs its
// bomb when it's off cooldown, keeps its pod deployed, and goes for melee
// when the player is close or landing-recovery vulnerable.

export class DummyAI {
  private strafeSign = 1;
  private decisionTimer = 0;
  private firing = false;
  private fireTimer = 0;
  private bombTimer = 3; // don't open with a bomb
  private meleeTimer = 2;

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
    this.melee.update(dt, this.player);

    // Fire gun in bursts
    this.fireTimer -= dt;
    if (this.fireTimer <= 0) {
      this.firing = !this.firing;
      this.fireTimer = this.firing ? 1.2 : A.fireInterval;
    }
    this.gun.update(
      dt,
      this.firing && playerAlive && !this.melee.busy,
      playerAlive ? this.player : null,
    );

    // Bomb when ready-ish, preferring a downed-adjacent or cornered player
    this.bombTimer -= dt;
    if (this.bombTimer <= 0 && playerAlive && this.bomb.ready && dist < 18) {
      this.bomb.tryThrow(this.player);
      this.bombTimer = 2 + Math.random() * 3;
    }

    // Keep the pod out
    if (!this.pod.deployed && this.robo.health.state === "active") {
      this.pod.toggle();
    }
    this.pod.update(dt, this.player);
  }
}
