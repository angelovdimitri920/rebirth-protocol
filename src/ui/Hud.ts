import * as THREE from "three";
import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";
import { Bomb } from "../combat/Bomb";
import { Pod } from "../combat/Pod";
import type { Effects } from "../run/effects";
import type { ArenaRoll } from "../arena/Arena";

// HTML/CSS overlay HUD (per CLAUDE.md: no in-canvas UI).
// Player: HP, endurance, shield, boost, bomb cooldown, pod energy.
// Enemy: HP, endurance, shield. Plus state callouts.

export class Hud {
  private playerHp: HTMLElement;
  private playerEnd: HTMLElement;
  private playerShield: HTMLElement;
  private playerShieldRow: HTMLElement;
  private playerBoost: HTMLElement;
  private boostRow: HTMLElement;
  private bombFill: HTMLElement;
  private podFill: HTMLElement;
  private enemyHp: HTMLElement;
  private enemyEnd: HTMLElement;
  private enemyShield: HTMLElement;
  private enemyShieldRow: HTMLElement;
  private callout: HTMLElement;

  constructor(hudRoot: HTMLElement) {
    hudRoot.innerHTML = `
      <style>
        .hud-corner { position: absolute; width: 320px; }
        #hud-player { left: 24px; bottom: 24px; }
        #hud-enemy { right: 24px; top: 24px; }
        .hud-label { color: #aab4d4; font-size: 12px; letter-spacing: 2px;
          margin-bottom: 2px; text-transform: uppercase; }
        .bar { height: 14px; background: #10101c; border: 1px solid #333a5c;
          border-radius: 3px; overflow: hidden; margin-bottom: 6px; }
        .bar-fill { height: 100%; width: 100%;
          transition: width 0.08s linear; }
        .hp .bar-fill { background: linear-gradient(#7fe07f, #3aa53a); }
        .end .bar-fill { background: linear-gradient(#ffd75e, #d9a520); }
        .shield .bar-fill { background: linear-gradient(#7fd7ff, #2f8fd9); }
        .boost .bar-fill { background: linear-gradient(#6fb7ff, #2f6fd9); }
        .boost.overheat .bar-fill { background: #d94a3a; }
        .bar.small { height: 9px; }
        .ability-row { display: flex; gap: 10px; margin-top: 4px; }
        .ability { flex: 1; }
        .ability .hud-label { font-size: 10px; }
        .ability .bar { height: 8px; }
        .bomb .bar-fill { background: linear-gradient(#ff9a5e, #d9622a); }
        .pod .bar-fill { background: linear-gradient(#c49aff, #7a4ad9); }
        #hud-callout { position: absolute; left: 50%; top: 38%;
          transform: translateX(-50%); color: #fff; font-size: 34px;
          font-weight: 700; letter-spacing: 6px; text-shadow: 0 0 18px #4a6cff;
          opacity: 0; transition: opacity 0.15s; }
        #hud-controls { position: absolute; left: 24px; top: 20px;
          color: #55608a; font-size: 12px; line-height: 1.7; }
        #hud-run { position: absolute; left: 50%; top: 18px;
          transform: translateX(-50%); text-align: center; color: #aab4d4;
          font-size: 15px; letter-spacing: 4px; }
        #hud-run .mods { font-size: 11px; color: #55608a; letter-spacing: 2px;
          margin-top: 2px; text-transform: uppercase; }
        #hud-build { position: absolute; right: 24px; bottom: 24px;
          text-align: right; color: #8894c4; font-size: 11.5px;
          line-height: 1.7; max-width: 260px; }
        #hud-build .boon { color: #c4b47f; }
        #hud-build .item { color: #7fc4a4; }
        #hud-toast { position: absolute; left: 50%; bottom: 130px;
          transform: translateX(-50%); color: #7fc4a4; font-size: 16px;
          letter-spacing: 2px; opacity: 0; transition: opacity 0.25s; }
        #hud-reticle { position: absolute; width: 44px; height: 44px;
          margin: -22px 0 0 -22px; border: 2px solid #ff4444;
          border-radius: 50%; opacity: 0.9;
          box-shadow: 0 0 10px #ff444488, inset 0 0 6px #ff444455; }
        #hud-reticle::after { content: ""; position: absolute; inset: 16px;
          border-radius: 50%; background: currentColor; opacity: 0.55; }
        #hud-reticle.green { border-color: #44dd88;
          box-shadow: 0 0 10px #44dd8866, inset 0 0 6px #44dd8844; }
        #hud-reticle { color: #ff4444; }
        #hud-reticle.green { color: #44dd88; }
      </style>
      <div id="hud-player" class="hud-corner">
        <div class="hud-label">HP</div>
        <div class="bar hp"><div class="bar-fill" id="p-hp"></div></div>
        <div class="hud-label">Endurance</div>
        <div class="bar end"><div class="bar-fill" id="p-end"></div></div>
        <div id="p-shield-row">
          <div class="hud-label">Shield</div>
          <div class="bar shield"><div class="bar-fill" id="p-shield"></div></div>
        </div>
        <div class="hud-label">Boost</div>
        <div class="bar boost" id="p-boost-row">
          <div class="bar-fill" id="p-boost"></div>
        </div>
        <div class="ability-row">
          <div class="ability bomb">
            <div class="hud-label">Bomb [Q]</div>
            <div class="bar"><div class="bar-fill" id="p-bomb"></div></div>
          </div>
          <div class="ability pod">
            <div class="hud-label">Pod [E]</div>
            <div class="bar"><div class="bar-fill" id="p-pod"></div></div>
          </div>
        </div>
      </div>
      <div id="hud-enemy" class="hud-corner">
        <div class="hud-label" style="text-align:right">Enemy</div>
        <div class="bar hp"><div class="bar-fill" id="e-hp"></div></div>
        <div class="bar end small"><div class="bar-fill" id="e-end"></div></div>
        <div class="bar shield small" id="e-shield-row">
          <div class="bar-fill" id="e-shield"></div>
        </div>
      </div>
      <div id="hud-reticle" style="display:none"></div>
      <div id="hud-callout"></div>
      <div id="hud-run"></div>
      <div id="hud-build"></div>
      <div id="hud-toast"></div>
      <div id="hud-controls">
        WASD move &nbsp; SPACE jump/hover &nbsp; SHIFT dash<br>
        LMB gun &nbsp; RMB melee &nbsp; Q bomb &nbsp; E pod &nbsp; R rebuild
      </div>
    `;
    this.playerHp = document.getElementById("p-hp")!;
    this.playerEnd = document.getElementById("p-end")!;
    this.playerShield = document.getElementById("p-shield")!;
    this.playerShieldRow = document.getElementById("p-shield-row")!;
    this.playerBoost = document.getElementById("p-boost")!;
    this.boostRow = document.getElementById("p-boost-row")!;
    this.bombFill = document.getElementById("p-bomb")!;
    this.podFill = document.getElementById("p-pod")!;
    this.enemyHp = document.getElementById("e-hp")!;
    this.enemyEnd = document.getElementById("e-end")!;
    this.enemyShield = document.getElementById("e-shield")!;
    this.enemyShieldRow = document.getElementById("e-shield-row")!;
    this.callout = document.getElementById("hud-callout")!;
  }

  setRunInfo(fight: number, total: number, roll: ArenaRoll): void {
    const runEl = document.getElementById("hud-run")!;
    const hazardLabel = roll.hazard === "none" ? "" : ` · ${roll.hazard}`;
    runEl.innerHTML = `FIGHT ${fight} / ${total}<div class="mods">${roll.layout}${hazardLabel}</div>`;
  }

  toast(message: string): void {
    const el = document.getElementById("hud-toast")!;
    el.textContent = message;
    el.style.opacity = "1";
    setTimeout(() => (el.style.opacity = "0"), 1800);
  }

  /** Red lock = inside gun homing range (shots track); green = outside
   *  (shots fly straight). GAME_DESIGN §3.3's range-gated lock-on. */
  updateReticle(
    player: Robo,
    enemy: Robo,
    camera: THREE.PerspectiveCamera,
    lockedOn: boolean,
  ): void {
    const el = document.getElementById("hud-reticle")!;
    if (!lockedOn || enemy.health.state === "dead") {
      el.style.display = "none";
      return;
    }
    const pos = enemy.position.clone().setY(enemy.groundY + 1.2);
    pos.project(camera);
    if (pos.z > 1) {
      el.style.display = "none"; // behind the camera
      return;
    }
    el.style.display = "";
    el.style.left = `${((pos.x + 1) / 2) * window.innerWidth}px`;
    el.style.top = `${((1 - pos.y) / 2) * window.innerHeight}px`;
    const inRange =
      player.position.distanceTo(enemy.position) <= TUNING.gun.homingRange;
    el.classList.toggle("green", !inRange);
  }

  update(
    player: Robo,
    enemy: Robo,
    bomb: Bomb,
    pod: Pod,
    effects?: Effects,
  ): void {
    const H = TUNING.health;
    this.playerHp.style.width = `${(player.health.hp / player.health.maxHp) * 100}%`;
    this.playerEnd.style.width = `${(player.health.endurance / H.maxEndurance) * 100}%`;
    this.playerBoost.style.width = `${(player.boost / TUNING.boost.max) * 100}%`;
    this.boostRow.classList.toggle("overheat", player.overheated);
    this.enemyHp.style.width = `${(enemy.health.hp / enemy.health.maxHp) * 100}%`;
    this.enemyEnd.style.width = `${(enemy.health.endurance / H.maxEndurance) * 100}%`;

    // Shields: hide the row entirely for shieldless builds
    const pMax = player.loadout.shield.shieldHp;
    this.playerShieldRow.style.display = pMax > 0 ? "" : "none";
    if (pMax > 0)
      this.playerShield.style.width = `${(player.shieldHp / pMax) * 100}%`;
    const eMax = enemy.loadout.shield.shieldHp;
    this.enemyShieldRow.style.display = eMax > 0 ? "" : "none";
    if (eMax > 0)
      this.enemyShield.style.width = `${(enemy.shieldHp / eMax) * 100}%`;

    // Bomb: fills up as it comes off cooldown
    const bombPart = player.loadout.bomb;
    this.bombFill.style.width = `${
      (1 - Math.max(0, bomb.cooldownRemaining) / bombPart.cooldown) * 100
    }%`;
    this.podFill.style.width = `${(pod.energy / player.loadout.pod.energyMax) * 100}%`;

    let text = "";
    if (player.health.state === "knockdown") text = "DOWN — MASH SPACE";
    else if (player.health.state === "rebirth") text = "REBIRTH";
    else if (player.health.state === "dead") text = "DESTROYED";
    else if (enemy.health.state === "dead") text = "TARGET ELIMINATED";
    else if (enemy.health.state === "knockdown") text = "ENEMY DOWN";
    this.callout.textContent = text;
    this.callout.style.opacity = text ? "1" : "0";

    // Build strip: boons + item stacks
    if (effects) {
      const buildEl = document.getElementById("hud-build")!;
      const boons = effects.boonList
        .map((b) => `<div class="boon">${b.name}</div>`)
        .join("");
      const items = [...effects.itemStacks.entries()]
        .map(([id, n]) => `<div class="item">${id} ×${n}</div>`)
        .join("");
      buildEl.innerHTML = boons + items;
    }
  }
}
