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
import { music } from "./music";
import { audioCore } from "./audio";

const STEP = 1 / 60;

type GamePhase = "fight" | "interlude" | "over";

// Orchestrates a run: builds a fresh Duel per fight, pauses the sim for
// boon drafts between fights, and shows the run-end overlay.

export class Game {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private camera: THREE.OrthographicCamera;
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
  private paused = false;
  private pauseSelection = 0; // 0=resume, 1=restart fight, 2=back to hangar
  private pauseEl: HTMLElement | null = null;

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

    // Orthographic: no perspective convergence, reads as a true isometric-
    // style arena view rather than a perspective camera at a steep angle.
    const aspect = window.innerWidth / window.innerHeight;
    const fs = TUNING.camera.frustumSize;
    this.camera = new THREE.OrthographicCamera(
      (-fs * aspect) / 2,
      (fs * aspect) / 2,
      fs / 2,
      -fs / 2,
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
      const a = window.innerWidth / window.innerHeight;
      const size = TUNING.camera.frustumSize;
      this.camera.left = (-size * a) / 2;
      this.camera.right = (size * a) / 2;
      this.camera.top = size / 2;
      this.camera.bottom = -size / 2;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    });
    window.addEventListener("keydown", (e) => {
      if (e.code === "KeyR") location.reload(); // instant -> hangar
      if (e.code === "Tab") e.preventDefault();
      if (e.code === "KeyF" && !document.fullscreenElement) {
        document.documentElement.requestFullscreen?.().catch(() => {});
      }
      if (e.code === "KeyP" && !e.repeat && this.phase === "fight") this.togglePause();
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
    music.start("combat");
  }

  start(): void {
    this.lastTime = performance.now();
    this.renderer.setAnimationLoop(() => this.frame());
  }

  private frame(): void {
    const now = performance.now();
    const dt = Math.min((now - this.lastTime) / 1000, 0.1);
    this.lastTime = now;

    this.input.poll(); // gamepad state: once per rendered frame, not per sim step

    if (this.phase === "fight" && this.input.justPressed("KeyP")) {
      this.togglePause();
    }
    if (this.paused) {
      this.updatePauseNav();
      this.input.endFrame();
      this.renderer.render(this.scene, this.camera);
      return;
    }

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
    this.hud.setControllerConnected(this.input.gamepadConnected);
    this.renderer.render(this.scene, this.camera);
  }

  private togglePause(): void {
    this.paused = !this.paused;
    if (this.paused) {
      this.pauseSelection = 0;
      audioCore.ctx?.suspend();
      this.showPauseMenu();
    } else {
      audioCore.ctx?.resume();
      this.hidePauseMenu();
    }
  }

  /** D-pad/stick + A/Enter navigation within the 3-option pause menu,
   *  mirroring the hangar's nav pattern but simple enough not to need a
   *  full focus-grid -- just one vertical list. */
  private updatePauseNav(): void {
    const options = 3;
    if (this.input.justPressed("ArrowUp")) {
      this.pauseSelection = (this.pauseSelection + options - 1) % options;
      this.paintPauseSelection();
    } else if (this.input.justPressed("ArrowDown")) {
      this.pauseSelection = (this.pauseSelection + 1) % options;
      this.paintPauseSelection();
    } else if (this.input.justPressed("Space") || this.input.justPressed("Enter")) {
      this.activatePauseSelection();
    }
  }

  private paintPauseSelection(): void {
    const buttons = this.pauseEl?.querySelectorAll<HTMLElement>(".pause-btn");
    buttons?.forEach((el, i) =>
      el.classList.toggle("focused", i === this.pauseSelection),
    );
  }

  private activatePauseSelection(): void {
    if (this.pauseSelection === 0) this.togglePause(); // resume
    else if (this.pauseSelection === 1) this.restartFight();
    else location.reload(); // back to hangar
  }

  private showPauseMenu(): void {
    const el = document.createElement("div");
    el.innerHTML = `
      <style>
        #pausemenu { position: absolute; inset: 0; background: #0a0a12cc;
          backdrop-filter: blur(4px); display: flex; flex-direction: column;
          align-items: center; justify-content: center; z-index: 20;
          font-family: "Segoe UI", system-ui, sans-serif; }
        #pausemenu h1 { color: #fff; letter-spacing: 8px; font-size: 32px;
          margin: 0 0 24px; text-shadow: 0 0 20px #4a6cff; }
        #pausemenu .pause-btn { display: block; width: 260px;
          margin-bottom: 12px; padding: 12px 0; font-size: 15px;
          letter-spacing: 3px; text-align: center; color: #cdd6f4;
          background: #14142299; border: 1px solid #333a5c; border-radius: 4px;
          cursor: pointer; }
        #pausemenu .pause-btn:hover, #pausemenu .pause-btn.focused {
          border-color: #ffee66; color: #fff; background: #1b2438; }
      </style>
      <div id="pausemenu">
        <h1>PAUSED</h1>
        <button class="pause-btn" data-i="0">RESUME</button>
        <button class="pause-btn" data-i="1">RESTART FIGHT</button>
        <button class="pause-btn" data-i="2">BACK TO HANGAR</button>
      </div>
    `;
    el.querySelectorAll<HTMLElement>(".pause-btn").forEach((btn) => {
      btn.addEventListener("click", () => {
        sfx.uiClick();
        this.pauseSelection = Number(btn.dataset.i);
        this.activatePauseSelection();
      });
    });
    document.body.appendChild(el);
    this.pauseEl = el;
    this.paintPauseSelection();
  }

  private hidePauseMenu(): void {
    this.pauseEl?.remove();
    this.pauseEl = null;
  }

  private async restartFight(): Promise<void> {
    this.hidePauseMenu();
    this.paused = false;
    audioCore.ctx?.resume();
    this.duel.dispose(this.scene);
    this.run.carriedHp = null; // retry fresh, not at whatever HP you entered with
    await this.startFight(); // same fightIndex
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
    music.start("hangar"); // calm loop for the draft/run-end screens
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
    this.input.poll();
    for (let i = 0; i < steps; i++) {
      if (this.phase !== "fight" || this.paused) break;
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
    this.hud.setControllerConnected(this.input.gamepadConnected);
    this.renderer.render(this.scene, this.camera);
  }
}
