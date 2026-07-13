import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Gun } from "../combat/Gun";

// Basic pressure-test opponent, not a real AI: orbits the player at mid
// range, strafes, occasionally jumps or dashes, fires in bursts. Enough to
// judge whether the movement-and-punish loop feels tense (Stage 1 go/no-go).

export class DummyAI {
  private strafeSign = 1;
  private decisionTimer = 0;
  private firing = false;
  private fireTimer = 0;

  constructor(
    private robo: Robo,
    private player: Robo,
    private gun: Gun,
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

    // Fire in bursts when player is alive
    this.fireTimer -= dt;
    if (this.fireTimer <= 0) {
      this.firing = !this.firing;
      this.fireTimer = this.firing ? 1.2 : TUNING.ai.fireInterval;
    }
    const playerAlive = this.player.health.state !== "dead";
    this.gun.update(dt, this.firing && playerAlive, playerAlive ? this.player : null);
  }
}
