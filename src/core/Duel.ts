import * as THREE from "three";
import { Physics } from "../physics/Physics";
import { Arena, rollArena, type ArenaRoll } from "../arena/Arena";
import { Robo } from "../robo/Robo";
import { Input } from "./input";
import { Projectiles } from "../combat/Projectiles";
import { Gun } from "../combat/Gun";
import { Melee } from "../combat/Melee";
import { Bomb } from "../combat/Bomb";
import { Pod } from "../combat/Pod";
import { PlayerController } from "../player/PlayerController";
import { DummyAI } from "../ai/DummyAI";
import { Effects, ITEM_POOL, type Item } from "../run/effects";
import { enemyForFight, enemyPowerMult } from "../run/run";
import type { Loadout } from "../parts/parts";

// One fight: owns the physics world, arena, both robos, and all combat
// systems. Game builds a fresh Duel per fight and disposes the old one --
// everything visual lives under `root` so teardown is one scene.remove.

const ITEM_DROP_CHANCE = 0.3;

interface Pickup {
  mesh: THREE.Mesh;
  item: Item;
}

export type DuelResult = "ongoing" | "playerWon" | "playerLost";

export class Duel {
  root = new THREE.Group();
  physics: Physics;
  arena: Arena;
  player: Robo;
  enemy: Robo;
  playerController: PlayerController;
  playerBomb: Bomb;
  playerPod: Pod;
  private dummyAI: DummyAI;
  private enemyBomb: Bomb;
  private projectiles: Projectiles;
  private pickups: Pickup[] = [];
  private effects: Effects;
  /** Fires once when an item is collected (for HUD toasts). */
  onItemCollected: (item: Item) => void = () => {};

  constructor(
    scene: THREE.Scene,
    camera: THREE.PerspectiveCamera,
    input: Input,
    physics: Physics,
    playerLoadout: Loadout,
    effects: Effects,
    fightIndex: number,
    carriedHp: number | null,
    roll: ArenaRoll = rollArena(fightIndex),
  ) {
    this.physics = physics;
    this.effects = effects;
    scene.add(this.root);

    this.arena = new Arena(physics, this.root, roll);
    this.arena.onCrateDestroyed = (at) => this.maybeDropItem(at);

    this.player = new Robo(
      physics,
      this.root,
      "player",
      new THREE.Vector3(0, 0, -11),
      0x5577aa,
      0x33e0ff,
      playerLoadout,
    );
    this.enemy = new Robo(
      physics,
      this.root,
      "enemy",
      new THREE.Vector3(0, 0, 11),
      0x8a4444,
      0xff8833,
      enemyForFight(fightIndex),
      enemyPowerMult(fightIndex),
    );
    this.player.setFacing(0);
    this.enemy.setFacing(Math.PI);

    // Carry run HP: heal 15% of max between fights
    if (carriedHp !== null) {
      this.player.health.hp = Math.min(
        this.player.health.maxHp,
        carriedHp + this.player.health.maxHp * 0.15,
      );
    }

    this.projectiles = new Projectiles(physics, this.root, this.arena);
    const playerGun = new Gun(this.player, "player", this.projectiles);
    const enemyGun = new Gun(this.enemy, "enemy", this.projectiles);
    const playerMelee = new Melee(this.player, this.root);
    this.playerBomb = new Bomb(this.player, this.root, this.arena);
    this.enemyBomb = new Bomb(this.enemy, this.root, this.arena);
    this.playerPod = new Pod(
      this.player,
      "player",
      this.root,
      this.projectiles,
      0x33e0ff,
    );
    const enemyPod = new Pod(
      this.enemy,
      "enemy",
      this.root,
      this.projectiles,
      0xff8833,
    );

    // Bind run effects to this duel's actors
    this.player.effects = effects;
    effects.bind(this.player, this.enemy, this.root, this.projectiles);
    effects.resetGunCooldown = () => playerGun.resetCooldown();
    effects.resetBombCooldown = () => this.playerBomb.resetCooldown();

    this.playerController = new PlayerController(
      this.player,
      this.enemy,
      input,
      camera,
      playerGun,
      playerMelee,
      this.playerBomb,
      this.playerPod,
    );
    this.dummyAI = new DummyAI(
      this.enemy,
      this.player,
      enemyGun,
      this.enemyBomb,
      enemyPod,
    );
  }

  get result(): DuelResult {
    if (this.enemy.health.state === "dead") return "playerWon";
    if (this.player.health.state === "dead") return "playerLost";
    return "ongoing";
  }

  step(dt: number): void {
    this.playerController.update(dt);
    this.dummyAI.update(dt);
    this.player.update(dt);
    this.enemy.update(dt);
    this.arena.applyHazards(dt, [this.player, this.enemy]);
    this.physics.step(dt);
    this.projectiles.update(dt, this.player, this.enemy);
    this.playerBomb.update(dt, this.player, this.enemy);
    this.enemyBomb.update(dt, this.player, this.enemy);
    this.effects.update(dt);
    this.updatePickups(dt);
  }

  private maybeDropItem(at: THREE.Vector3): void {
    if (Math.random() > ITEM_DROP_CHANCE) return;
    const item = ITEM_POOL[Math.floor(Math.random() * ITEM_POOL.length)];
    const mesh = new THREE.Mesh(
      new THREE.BoxGeometry(0.5, 0.5, 0.5),
      new THREE.MeshStandardMaterial({
        color: 0x33ffcc,
        emissive: 0x22aa88,
        emissiveIntensity: 0.8,
      }),
    );
    mesh.position.set(at.x, 0.7, at.z);
    this.root.add(mesh);
    this.pickups.push({ mesh, item });
  }

  private updatePickups(dt: number): void {
    for (let i = this.pickups.length - 1; i >= 0; i--) {
      const p = this.pickups[i];
      p.mesh.rotation.y += 2.5 * dt;
      p.mesh.position.y = 0.7 + Math.sin(p.mesh.rotation.y) * 0.12;
      if (this.player.position.distanceTo(p.mesh.position) < 1.4) {
        this.effects.addItem(p.item);
        this.onItemCollected(p.item);
        this.root.remove(p.mesh);
        this.pickups.splice(i, 1);
      }
    }
  }

  dispose(scene: THREE.Scene): void {
    scene.remove(this.root);
    this.physics.dispose();
  }
}
