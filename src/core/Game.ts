import * as THREE from "three";
import { Physics } from "../physics/Physics";
import { Input } from "./input";
import { TUNING } from "./tuning";
import { Duel } from "./Duel";
import { Hud } from "../ui/Hud";
import { showDraft } from "../ui/Draft";
import { Effects } from "../run/effects";
import { RunState, FIGHTS_PER_RUN } from "../run/run";
import type { Loadout } from "../parts/parts";
import { sfx } from "./sfx";

const STEP = 1 / 60;

type GamePhase = "fight" | "interlude" | "over";

// Orchestrates a run: builds a fresh Duel per fight, pauses the sim for
// boon drafts between fights, and shows the run-end overlay.

export class Game {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private camera: THREE.PerspectiveCamera;
  private input: Input;
  private hud: Hud;
  private duel!: Duel;
  private effects = new Effects();
  private run = new RunState();
  private loadout: Loadout;
  private phase: GamePhase = "fight";
  private victoryDelay = 0;
  private accumulator = 0;
  private lastTime = 0;

  private constructor(canvas: HTMLCanvasElement, loadout: Loadout) {
    this.loadout = loadout;

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

    const sun = new THREE.DirectionalLight(0xffffff, 2.8);
    sun.position.set(20, 30, 10);
    sun.castShadow = true;
    sun.shadow.camera.left = -25;
    sun.shadow.camera.right = 25;
    sun.shadow.camera.top = 25;
    sun.shadow.camera.bottom = -25;
    sun.shadow.mapSize.set(2048, 2048);
    this.scene.add(sun);
    this.scene.add(new THREE.AmbientLight(0x99aacc, 1.1));
    this.scene.add(new THREE.HemisphereLight(0x6a8cff, 0x2a2a3a, 0.8));

    this.input = new Input(canvas);
    this.hud = new Hud(document.getElementById("hud")!);

    window.addEventListener("resize", () => {
      this.camera.aspect = window.innerWidth / window.innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    });
    window.addEventListener("keydown", (e) => {
      if (e.code === "KeyR") location.reload(); // abandon -> hangar
      if (e.code === "Tab") e.preventDefault();
    });
  }

  static async create(
    canvas: HTMLCanvasElement,
    playerLoadout: Loadout,
  ): Promise<Game> {
    const game = new Game(canvas, playerLoadout);
    await game.startFight();
    return game;
  }

  private async startFight(): Promise<void> {
    const physics = await Physics.create();
    this.duel = new Duel(
      this.scene,
      this.camera,
      this.input,
      physics,
      this.loadout,
      this.effects,
      this.run.fightIndex,
      this.run.carriedHp,
    );
    this.duel.onItemCollected = (item) => this.hud.toast(`+ ${item.name}`);
    this.hud.setRunInfo(
      this.run.fightIndex + 1,
      FIGHTS_PER_RUN,
      this.duel.arena.roll,
    );
    this.phase = "fight";
  }

  start(): void {
    this.lastTime = performance.now();
    this.renderer.setAnimationLoop(() => this.frame());
  }

  private frame(): void {
    const now = performance.now();
    const dt = Math.min((now - this.lastTime) / 1000, 0.1);
    this.lastTime = now;

    if (this.phase === "fight") {
      this.accumulator += dt;
      let stepped = false;
      while (this.accumulator >= STEP) {
        this.stepFight(STEP);
        this.accumulator -= STEP;
        stepped = true;
        if (this.phase !== "fight") break;
      }
      if (stepped) this.input.endFrame();
    }

    this.hud.update(
      this.duel.player,
      this.duel.enemy,
      this.duel.playerBomb,
      this.duel.playerPod,
      this.effects,
    );
    this.hud.updateReticle(
      this.duel.player,
      this.duel.enemy,
      this.camera,
      this.duel.playerController.lockedOn,
    );
    this.renderer.render(this.scene, this.camera);
  }

  private stepFight(dt: number): void {
    this.duel.step(dt);

    const result = this.duel.result;
    if (result === "playerLost") {
      this.phase = "over";
      this.showRunEnd(false);
    } else if (result === "playerWon") {
      // Let the kill land visually before the draft
      this.victoryDelay += dt;
      if (this.victoryDelay >= 1.4) {
        this.victoryDelay = 0;
        void this.advanceRun();
      }
    }
  }

  private async advanceRun(): Promise<void> {
    this.phase = "interlude";
    this.run.carriedHp = this.duel.player.health.hp;
    this.run.fightIndex += 1;

    if (this.run.fightIndex >= FIGHTS_PER_RUN) {
      this.phase = "over";
      this.showRunEnd(true);
      return;
    }

    const boon = await showDraft(this.effects, this.run.rerollsLeft, () => {
      this.run.rerollsLeft -= 1;
    });
    if (boon) this.effects.addBoon(boon);

    this.duel.dispose(this.scene);
    await this.startFight();
  }

  private showRunEnd(won: boolean): void {
    if (won) sfx.victory();
    else sfx.defeat();
    const el = document.createElement("div");
    el.innerHTML = `
      <style>
        #runend { position: absolute; inset: 0; background: #0a0a12dd;
          display: flex; flex-direction: column; align-items: center;
          justify-content: center; z-index: 10; color: #fff;
          font-family: "Segoe UI", system-ui, sans-serif; }
        #runend h1 { letter-spacing: 8px; font-size: 38px; margin: 0 0 8px;
          text-shadow: 0 0 24px ${won ? "#33e0ff" : "#d94a3a"}; }
        #runend .stats { color: #8894c4; margin-bottom: 26px; }
        #runend button { padding: 12px 48px; font-size: 16px;
          letter-spacing: 4px; background: #1b2438; color: #fff;
          border: 1px solid #5f7fff; border-radius: 4px; cursor: pointer; }
      </style>
      <div id="runend">
        <h1>${won ? "PROTOCOL COMPLETE" : "RUN TERMINATED"}</h1>
        <div class="stats">fights cleared: ${this.run.fightIndex}${
          won ? ` / ${FIGHTS_PER_RUN}` : ""
        } &nbsp;·&nbsp; boons: ${this.effects.boonList.map((b) => b.name).join(", ") || "none"}</div>
        <button onclick="location.reload()">RETURN TO HANGAR</button>
      </div>
    `;
    document.body.appendChild(el);
  }

  /** Console/testing hook: advance the sim N fixed steps, then render once.
   *  Lets the game be driven when rAF is suspended (hidden tab). */
  debugStep(steps = 1): void {
    for (let i = 0; i < steps; i++) {
      if (this.phase !== "fight") break;
      this.stepFight(STEP);
      this.input.endFrame();
    }
    this.hud.update(
      this.duel.player,
      this.duel.enemy,
      this.duel.playerBomb,
      this.duel.playerPod,
      this.effects,
    );
    this.hud.updateReticle(
      this.duel.player,
      this.duel.enemy,
      this.camera,
      this.duel.playerController.lockedOn,
    );
    this.renderer.render(this.scene, this.camera);
  }
}
