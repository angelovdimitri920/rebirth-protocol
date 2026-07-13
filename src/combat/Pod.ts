import * as THREE from "three";
import { Robo } from "../robo/Robo";
import { Projectiles } from "./Projectiles";
import { sfx } from "../core/sfx";

// Pod slot (GAME_DESIGN §2.1): deployable drone with its OWN energy pool,
// independent of gun/bomb, so it's always-on pressure rather than a burst
// opportunity cost. Deploy drops it at your position; it hovers there and
// fires weak homing shots at the enemy while its cell holds charge.

const HOVER_HEIGHT = 2.4;
const FIRE_RANGE = 22;

export class Pod {
  deployed = false;
  energy: number;
  private fireCooldown = 0;
  private mesh: THREE.Group;
  private bobTime = 0;

  constructor(
    private owner: Robo,
    private ownerTag: "player" | "enemy",
    scene: THREE.Object3D,
    private projectiles: Projectiles,
    accentColor: number,
  ) {
    this.energy = owner.loadout.pod.energyMax;

    this.mesh = new THREE.Group();
    const shell = new THREE.Mesh(
      new THREE.OctahedronGeometry(0.4),
      new THREE.MeshStandardMaterial({
        color: 0x333344,
        metalness: 0.6,
        roughness: 0.4,
      }),
    );
    const eye = new THREE.Mesh(
      new THREE.SphereGeometry(0.14, 8, 8),
      new THREE.MeshBasicMaterial({ color: accentColor }),
    );
    eye.position.z = 0.3;
    this.mesh.add(shell, eye);
    this.mesh.visible = false;
    scene.add(this.mesh);
  }

  /** Deploy at the owner's position, or recall if already out. */
  toggle(): void {
    if (this.owner.controlLocked) return;
    if (this.deployed) {
      this.deployed = false;
      this.mesh.visible = false;
      sfx.podToggle(false);
      return;
    }
    this.deployed = true;
    this.mesh.visible = true;
    this.mesh.position
      .copy(this.owner.position)
      .setY(this.owner.groundY + HOVER_HEIGHT);
    sfx.podToggle(true);
  }

  update(dt: number, target: Robo): void {
    const part = this.owner.loadout.pod;

    // Energy regenerates whether deployed or not (its own pool)
    const fx = this.owner.effects;
    this.energy = Math.min(
      part.energyMax,
      this.energy + part.energyRegenPerSec * (fx?.podRegenMult() ?? 1) * dt,
    );

    if (!this.deployed) return;

    // Owner down or dead: pod powers off
    if (this.owner.health.state === "dead") {
      this.deployed = false;
      this.mesh.visible = false;
      return;
    }

    this.bobTime += dt;
    this.mesh.position.y += Math.sin(this.bobTime * 2.5) * 0.003;
    this.mesh.rotation.y += dt * 1.5;

    this.fireCooldown -= dt;
    const targetAlive = target.health.state !== "dead";
    const inRange =
      this.mesh.position.distanceTo(target.position) <= FIRE_RANGE;

    if (
      targetAlive &&
      inRange &&
      this.fireCooldown <= 0 &&
      this.energy >= part.energyPerShot
    ) {
      this.fireCooldown = part.fireInterval * (fx?.podFireIntervalMult() ?? 1);
      this.energy -= part.energyPerShot;
      sfx.podShot();
      const aim = target.position.clone().setY(target.groundY + 1.0);
      this.projectiles.spawn(this.mesh.position.clone(), aim, this.ownerTag, {
        damage:
          part.damage * this.owner.stats.atkMult + (fx?.flatDamageBonus() ?? 0),
        enduranceDamage: part.enduranceDamage,
        speed: 26,
        homingTurnRate: 1.6,
        source: "pod",
      });
    }
  }
}
