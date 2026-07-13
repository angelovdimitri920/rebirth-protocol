import * as THREE from "three";
import { Physics } from "../physics/Physics";
import { Arena } from "../arena/Arena";
import { Robo } from "../robo/Robo";
import { Input } from "./input";
import { TUNING } from "./tuning";
import { Projectiles } from "../combat/Projectiles";
import { Gun } from "../combat/Gun";
import { Melee } from "../combat/Melee";
import { PlayerController } from "../player/PlayerController";
import { DummyAI } from "../ai/DummyAI";
import { Hud } from "../ui/Hud";

const STEP = 1 / 60;

export class Game {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private camera: THREE.PerspectiveCamera;
  private input: Input;
  private physics: Physics;
  private player: Robo;
  private enemy: Robo;
  private playerController: PlayerController;
  private dummyAI: DummyAI;
  private projectiles: Projectiles;
  private hud: Hud;
  private accumulator = 0;
  private lastTime = 0;

  private constructor(canvas: HTMLCanvasElement, physics: Physics) {
    this.physics = physics;

    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0a0a12);
    this.scene.fog = new THREE.Fog(0x0a0a12, 40, 90);

    this.camera = new THREE.PerspectiveCamera(
      TUNING.camera.fov,
      window.innerWidth / window.innerHeight,
      0.1,
      200,
    );
    this.camera.position.set(0, 6, -20);

    const sun = new THREE.DirectionalLight(0xffffff, 2.2);
    sun.position.set(20, 30, 10);
    sun.castShadow = true;
    sun.shadow.camera.left = -25;
    sun.shadow.camera.right = 25;
    sun.shadow.camera.top = 25;
    sun.shadow.camera.bottom = -25;
    sun.shadow.mapSize.set(2048, 2048);
    this.scene.add(sun);
    this.scene.add(new THREE.AmbientLight(0x8899bb, 0.55));
    const rim = new THREE.HemisphereLight(0x4a6cff, 0x1a1a2a, 0.4);
    this.scene.add(rim);

    const arena = new Arena(this.physics, this.scene);
    this.input = new Input(canvas);

    this.player = new Robo(
      this.physics,
      this.scene,
      "player",
      new THREE.Vector3(0, 0, -11),
      0x5577aa,
      0x33e0ff,
    );
    this.enemy = new Robo(
      this.physics,
      this.scene,
      "enemy",
      new THREE.Vector3(0, 0, 11),
      0x8a4444,
      0xff8833,
    );
    this.player.setFacing(0);
    this.enemy.setFacing(Math.PI);

    this.projectiles = new Projectiles(this.physics, this.scene, arena);
    const playerGun = new Gun(this.player, "player", this.projectiles);
    const enemyGun = new Gun(this.enemy, "enemy", this.projectiles);
    const playerMelee = new Melee(this.player, this.scene);

    this.playerController = new PlayerController(
      this.player,
      this.enemy,
      this.input,
      this.camera,
      playerGun,
      playerMelee,
    );
    this.dummyAI = new DummyAI(this.enemy, this.player, enemyGun);

    this.hud = new Hud(document.getElementById("hud")!);

    window.addEventListener("resize", () => {
      this.camera.aspect = window.innerWidth / window.innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    });
    window.addEventListener("keydown", (e) => {
      if (e.code === "KeyR") location.reload(); // quick restart for testing
      if (e.code === "Tab") e.preventDefault();
    });
  }

  static async create(canvas: HTMLCanvasElement): Promise<Game> {
    const physics = await Physics.create();
    return new Game(canvas, physics);
  }

  start(): void {
    this.lastTime = performance.now();
    this.renderer.setAnimationLoop(() => this.frame());
  }

  private frame(): void {
    const now = performance.now();
    const dt = Math.min((now - this.lastTime) / 1000, 0.1); // clamp hitches
    this.lastTime = now;

    this.accumulator += dt;
    let stepped = false;
    while (this.accumulator >= STEP) {
      this.step(STEP);
      this.accumulator -= STEP;
      stepped = true;
    }
    // Only clear edge-triggered input once a sim step has consumed it
    if (stepped) this.input.endFrame();

    this.hud.update(this.player, this.enemy);
    this.renderer.render(this.scene, this.camera);
  }

  private step(dt: number): void {
    this.playerController.update(dt);
    this.dummyAI.update(dt);
    this.player.update(dt);
    this.enemy.update(dt);
    this.physics.step(dt);
    this.projectiles.update(dt, this.player, this.enemy);
  }
}
