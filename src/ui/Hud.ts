import { TUNING } from "../core/tuning";
import { Robo } from "../robo/Robo";

// HTML/CSS overlay HUD (per CLAUDE.md: no in-canvas UI).
// Player: HP, endurance, boost. Enemy: HP, endurance. Plus state callouts.

export class Hud {
  private playerHp: HTMLElement;
  private playerEnd: HTMLElement;
  private playerBoost: HTMLElement;
  private enemyHp: HTMLElement;
  private enemyEnd: HTMLElement;
  private callout: HTMLElement;
  private boostRow: HTMLElement;

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
        .boost .bar-fill { background: linear-gradient(#6fb7ff, #2f6fd9); }
        .boost.overheat .bar-fill { background: #d94a3a; }
        .bar.small { height: 9px; }
        #hud-callout { position: absolute; left: 50%; top: 38%;
          transform: translateX(-50%); color: #fff; font-size: 34px;
          font-weight: 700; letter-spacing: 6px; text-shadow: 0 0 18px #4a6cff;
          opacity: 0; transition: opacity 0.15s; }
        #hud-controls { position: absolute; left: 24px; top: 20px;
          color: #55608a; font-size: 12px; line-height: 1.7; }
      </style>
      <div id="hud-player" class="hud-corner">
        <div class="hud-label">HP</div>
        <div class="bar hp"><div class="bar-fill" id="p-hp"></div></div>
        <div class="hud-label">Endurance</div>
        <div class="bar end"><div class="bar-fill" id="p-end"></div></div>
        <div class="hud-label">Boost</div>
        <div class="bar boost" id="p-boost-row">
          <div class="bar-fill" id="p-boost"></div>
        </div>
      </div>
      <div id="hud-enemy" class="hud-corner">
        <div class="hud-label" style="text-align:right">Enemy</div>
        <div class="bar hp"><div class="bar-fill" id="e-hp"></div></div>
        <div class="bar end small"><div class="bar-fill" id="e-end"></div></div>
      </div>
      <div id="hud-callout"></div>
      <div id="hud-controls">
        WASD move &nbsp; SPACE jump/hover &nbsp; SHIFT dash<br>
        LMB gun &nbsp; RMB melee &nbsp; TAB lock-on &nbsp; click to capture mouse
      </div>
    `;
    this.playerHp = document.getElementById("p-hp")!;
    this.playerEnd = document.getElementById("p-end")!;
    this.playerBoost = document.getElementById("p-boost")!;
    this.boostRow = document.getElementById("p-boost-row")!;
    this.enemyHp = document.getElementById("e-hp")!;
    this.enemyEnd = document.getElementById("e-end")!;
    this.callout = document.getElementById("hud-callout")!;
  }

  update(player: Robo, enemy: Robo): void {
    const H = TUNING.health;
    this.playerHp.style.width = `${(player.health.hp / H.maxHp) * 100}%`;
    this.playerEnd.style.width = `${(player.health.endurance / H.maxEndurance) * 100}%`;
    this.playerBoost.style.width = `${(player.boost / TUNING.boost.max) * 100}%`;
    this.boostRow.classList.toggle("overheat", player.overheated);
    this.enemyHp.style.width = `${(enemy.health.hp / H.maxHp) * 100}%`;
    this.enemyEnd.style.width = `${(enemy.health.endurance / H.maxEndurance) * 100}%`;

    let text = "";
    if (player.health.state === "knockdown") text = "DOWN — MASH SPACE";
    else if (player.health.state === "rebirth") text = "REBIRTH";
    else if (player.health.state === "dead") text = "DESTROYED";
    else if (enemy.health.state === "dead") text = "TARGET ELIMINATED";
    else if (enemy.health.state === "knockdown") text = "ENEMY DOWN";
    this.callout.textContent = text;
    this.callout.style.opacity = text ? "1" : "0";
  }
}
