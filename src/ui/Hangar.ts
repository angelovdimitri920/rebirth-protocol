import {
  BODIES,
  LEGS,
  PODS,
  RIGHT_ARM_CATALOG,
  LEFT_ARM_CATALOG,
  loadLoadout,
  saveLoadout,
  type Loadout,
  type RightArm,
  type LeftArm,
  type BodyPart,
  type LegsPart,
  type PodPart,
} from "../parts/parts";
import { sfx } from "../core/sfx";
import { music } from "../core/music";
import { HangarPreview } from "./HangarPreview";

// Pre-fight loadout screen: live 3D preview on the left (rebuilds as you
// swap parts, using the exact same mesh builder the real game uses), part
// cards on the right. Right Arm and Left Arm each have two sub-tabs (gun
// vs melee, bomb vs shield) that are mutually exclusive by construction --
// picking a card in one tab silently deselects whatever was in the other.
// Fully navigable by keyboard (arrows + Enter), mouse, and gamepad (D-pad/
// stick + A) -- see moveFocus/activateFocused below.

export function showHangar(): Promise<Loadout> {
  return new Promise((resolve) => {
    const loadout = loadLoadout();
    let rightTab: "gun" | "melee" = loadout.rightArm.kind;
    let leftTab: "bomb" | "shield" = loadout.leftArm.kind;

    const root = document.createElement("div");
    root.id = "hangar";
    root.innerHTML = `
      <style>
        #hangar { position: absolute; inset: 0; background: #0a0a12ee;
          font-family: "Segoe UI", system-ui, sans-serif; color: #cdd6f4;
          display: flex; z-index: 10; }
        #hangar h1 { margin: 18px 0 0; font-size: 26px; letter-spacing: 7px;
          color: #fff; text-shadow: 0 0 24px #4a6cff; text-align: center; }
        #hangar-left { width: 34%; min-width: 320px; display: flex;
          flex-direction: column; align-items: center; padding-bottom: 20px;
          border-right: 1px solid #262c4a; }
        #hangar-canvas { display: block; width: 100%; flex: 1; min-height: 0; }
        #hangar-summary { text-align: center; padding: 0 20px; }
        #hangar-summary .build-name { font-size: 15px; letter-spacing: 3px;
          color: #7fc4a4; margin-bottom: 6px; text-transform: uppercase; }
        #hangar-summary .part-line { font-size: 12px; color: #8894c4;
          line-height: 1.7; }
        #hangar-right { flex: 1; overflow-y: auto; padding: 20px 28px 40px; }
        #hangar-right .sub { color: #55608a; font-size: 12px;
          margin-bottom: 18px; }
        .slot-section { margin-bottom: 18px; }
        .slot-name { font-size: 12px; letter-spacing: 3px; color: #8894c4;
          text-transform: uppercase; margin-bottom: 6px; }
        .tabs { display: flex; gap: 6px; margin-bottom: 8px; }
        .tab-btn { padding: 5px 16px; font-size: 11px; letter-spacing: 2px;
          text-transform: uppercase; background: #14142299;
          border: 1px solid #333a5c; border-radius: 4px; color: #8894c4;
          cursor: pointer; }
        .tab-btn.tab-active { border-color: #33e0ff; color: #fff;
          background: #1b2438; }
        .cards { display: flex; gap: 10px; flex-wrap: wrap; }
        .card { flex: 1 1 200px; max-width: 240px; background: #14142299;
          border: 1px solid #333a5c; border-radius: 6px; padding: 10px 12px;
          cursor: pointer; transition: border-color .1s, background .1s; }
        .card:hover { border-color: #5f7fff; }
        .card.selected { border-color: #33e0ff; background: #1b2438; }
        .card.focused { outline: 2px solid #ffee66; outline-offset: 2px; }
        .card .pname { font-weight: 600; font-size: 14px; color: #fff; }
        .card .pblurb { font-size: 11.5px; color: #8894c4; margin-top: 4px;
          line-height: 1.45; }
        #deploy-row { margin-top: 8px; }
        #deploy { padding: 12px 64px; font-size: 18px;
          letter-spacing: 6px; font-weight: 700; color: #fff;
          background: linear-gradient(#2f6fd9, #1b3f8f);
          border: 1px solid #5f7fff; border-radius: 4px; cursor: pointer; }
        #deploy:hover { background: linear-gradient(#3f7fe9, #2b4f9f); }
        #deploy.focused { outline: 2px solid #ffee66; outline-offset: 3px; }
        #hangar-nav-hint { color: #55608a; font-size: 11px;
          text-align: center; margin-top: 10px; }
      </style>
      <div id="hangar-left">
        <h1>REBIRTH PROTOCOL</h1>
        <canvas id="hangar-canvas"></canvas>
        <div id="hangar-summary"></div>
        <div id="hangar-nav-hint">
          Arrows/D-pad move &nbsp; Enter/A select &nbsp; Mouse click also works
        </div>
      </div>
      <div id="hangar-right">
        <div class="sub">assemble your robo — every slot is a tradeoff</div>
        <div class="slot-section">
          <div class="slot-name">Chassis</div>
          <div class="cards" data-row="0" id="cards-body"></div>
        </div>
        <div class="slot-section">
          <div class="slot-name">Right Arm</div>
          <div class="tabs" data-row="1" id="tabs-right"></div>
          <div class="cards" data-row="2" id="cards-right"></div>
        </div>
        <div class="slot-section">
          <div class="slot-name">Left Arm</div>
          <div class="tabs" data-row="3" id="tabs-left"></div>
          <div class="cards" data-row="4" id="cards-left"></div>
        </div>
        <div class="slot-section">
          <div class="slot-name">Legs</div>
          <div class="cards" data-row="5" id="cards-legs"></div>
        </div>
        <div class="slot-section">
          <div class="slot-name">Pod</div>
          <div class="cards" data-row="6" id="cards-pod"></div>
        </div>
        <div id="deploy-row" data-row="7">
          <button id="deploy" class="focusable">DEPLOY</button>
        </div>
      </div>
    `;
    document.body.appendChild(root);

    const preview = new HangarPreview(
      document.getElementById("hangar-canvas") as HTMLCanvasElement,
    );
    preview.start();
    // Debug handle for console-driven testing (harmless in production;
    // mirrors the window.game hook the in-fight Game class already has).
    (window as unknown as { hangarPreview: HangarPreview }).hangarPreview = preview;

    function refreshPreview(): void {
      preview.rebuild(loadout);
      const summary = document.getElementById("hangar-summary")!;
      summary.innerHTML = `
        <div class="build-name">${loadout.body.name}</div>
        <div class="part-line">${loadout.rightArm.part.name} (R) · ${loadout.leftArm.part.name} (L)</div>
        <div class="part-line">${loadout.legs.name} · ${loadout.pod.name}</div>
      `;
    }

    // --- Card builders ---
    function makeCard(
      name: string,
      blurb: string,
      selected: boolean,
      onPick: () => void,
    ): HTMLElement {
      const card = document.createElement("div");
      card.className = "card focusable" + (selected ? " selected" : "");
      card.innerHTML = `<div class="pname">${name}</div><div class="pblurb">${blurb}</div>`;
      card.addEventListener("click", () => {
        sfx.ensure();
        music.start("hangar");
        sfx.uiClick();
        onPick();
        renderAll();
      });
      return card;
    }

    function renderBodyCards(): void {
      const el = document.getElementById("cards-body")!;
      el.innerHTML = "";
      for (const part of BODIES) {
        el.appendChild(
          makeCard(part.name, part.blurb, loadout.body.id === part.id, () => {
            loadout.body = part as BodyPart;
          }),
        );
      }
    }

    function renderRightArmTabs(): void {
      const el = document.getElementById("tabs-right")!;
      el.innerHTML = "";
      for (const tab of ["gun", "melee"] as const) {
        const btn = document.createElement("button");
        btn.className =
          "tab-btn focusable" + (rightTab === tab ? " tab-active" : "");
        btn.textContent = tab === "gun" ? "Guns" : "Melee Weapons";
        btn.addEventListener("click", () => {
          sfx.ensure();
          sfx.uiClick();
          rightTab = tab;
          renderAll();
        });
        el.appendChild(btn);
      }
    }

    function renderLeftArmTabs(): void {
      const el = document.getElementById("tabs-left")!;
      el.innerHTML = "";
      for (const tab of ["bomb", "shield"] as const) {
        const btn = document.createElement("button");
        btn.className =
          "tab-btn focusable" + (leftTab === tab ? " tab-active" : "");
        btn.textContent = tab === "bomb" ? "Bombs" : "Shields";
        btn.addEventListener("click", () => {
          sfx.ensure();
          sfx.uiClick();
          leftTab = tab;
          renderAll();
        });
        el.appendChild(btn);
      }
    }

    function renderRightArmCards(): void {
      const el = document.getElementById("cards-right")!;
      el.innerHTML = "";
      const options = RIGHT_ARM_CATALOG.filter((o) => o.kind === rightTab);
      for (const option of options) {
        const selected =
          loadout.rightArm.kind === option.kind &&
          loadout.rightArm.part.id === option.part.id;
        el.appendChild(
          makeCard(option.part.name, option.part.blurb, selected, () => {
            loadout.rightArm = option as RightArm;
          }),
        );
      }
    }

    function renderLeftArmCards(): void {
      const el = document.getElementById("cards-left")!;
      el.innerHTML = "";
      const options = LEFT_ARM_CATALOG.filter((o) => o.kind === leftTab);
      for (const option of options) {
        const selected =
          loadout.leftArm.kind === option.kind &&
          loadout.leftArm.part.id === option.part.id;
        el.appendChild(
          makeCard(option.part.name, option.part.blurb, selected, () => {
            loadout.leftArm = option as LeftArm;
          }),
        );
      }
    }

    function renderLegsCards(): void {
      const el = document.getElementById("cards-legs")!;
      el.innerHTML = "";
      for (const part of LEGS) {
        el.appendChild(
          makeCard(part.name, part.blurb, loadout.legs.id === part.id, () => {
            loadout.legs = part as LegsPart;
          }),
        );
      }
    }

    function renderPodCards(): void {
      const el = document.getElementById("cards-pod")!;
      el.innerHTML = "";
      for (const part of PODS) {
        el.appendChild(
          makeCard(part.name, part.blurb, loadout.pod.id === part.id, () => {
            loadout.pod = part as PodPart;
          }),
        );
      }
    }

    function renderAll(): void {
      renderBodyCards();
      renderRightArmTabs();
      renderRightArmCards();
      renderLeftArmTabs();
      renderLeftArmCards();
      renderLegsCards();
      renderPodCards();
      refreshPreview();
      refreshFocus();
    }

    // --- Keyboard/gamepad grid navigation ---
    let curRow = 0;
    let curCol = 0;

    function getRows(): HTMLElement[][] {
      const rowEls = [...root.querySelectorAll<HTMLElement>("[data-row]")];
      rowEls.sort((a, b) => Number(a.dataset.row) - Number(b.dataset.row));
      return rowEls.map((rowEl) => [
        ...rowEl.querySelectorAll<HTMLElement>(".focusable"),
      ]);
    }

    function refreshFocus(): void {
      const rows = getRows();
      if (rows.length === 0) return;
      curRow = Math.max(0, Math.min(rows.length - 1, curRow));
      const row = rows[curRow];
      curCol = Math.max(0, Math.min(row.length - 1, curCol));
      root
        .querySelectorAll(".focusable.focused")
        .forEach((el) => el.classList.remove("focused"));
      const el = row[curCol];
      if (el) {
        el.classList.add("focused");
        el.scrollIntoView({ block: "nearest", behavior: "smooth" });
      }
    }

    function moveFocus(dRow: number, dCol: number): void {
      const rows = getRows();
      if (rows.length === 0) return;
      if (dRow !== 0) {
        curRow = Math.max(0, Math.min(rows.length - 1, curRow + dRow));
        curCol = 0;
      } else if (dCol !== 0) {
        const row = rows[curRow] ?? [];
        curCol = Math.max(0, Math.min(row.length - 1, curCol + dCol));
      }
      refreshFocus();
    }

    function activateFocused(): void {
      const rows = getRows();
      rows[curRow]?.[curCol]?.click();
    }

    window.addEventListener("keydown", onKeydown);
    function onKeydown(e: KeyboardEvent): void {
      if (e.code === "ArrowUp") moveFocus(-1, 0);
      else if (e.code === "ArrowDown") moveFocus(1, 0);
      else if (e.code === "ArrowLeft") moveFocus(0, -1);
      else if (e.code === "ArrowRight") moveFocus(0, 1);
      else if (e.code === "Enter" || e.code === "Space") {
        e.preventDefault();
        activateFocused();
      } else return;
      e.preventDefault();
    }

    // Gamepad nav: separate small poll loop (Game/Input don't exist yet
    // during hangar) -- D-pad or stick to move, A to activate, with a
    // repeat delay so a held direction doesn't scroll every frame.
    let padRepeatTimer = 0;
    let padPrevA = false;
    let navRafId: number | null = null;
    let lastTime = performance.now();
    function pollNav(): void {
      const now = performance.now();
      const dt = (now - lastTime) / 1000;
      lastTime = now;

      const pads = navigator.getGamepads ? navigator.getGamepads() : [];
      const gp = pads[0];
      if (gp) {
        padRepeatTimer -= dt;
        const ly = gp.axes[1] ?? 0;
        const lx = gp.axes[0] ?? 0;
        const up = !!gp.buttons[12]?.pressed || ly < -0.5;
        const down = !!gp.buttons[13]?.pressed || ly > 0.5;
        const left = !!gp.buttons[14]?.pressed || lx < -0.5;
        const right = !!gp.buttons[15]?.pressed || lx > 0.5;
        if (padRepeatTimer <= 0) {
          if (up) {
            moveFocus(-1, 0);
            padRepeatTimer = 0.22;
          } else if (down) {
            moveFocus(1, 0);
            padRepeatTimer = 0.22;
          } else if (left) {
            moveFocus(0, -1);
            padRepeatTimer = 0.22;
          } else if (right) {
            moveFocus(0, 1);
            padRepeatTimer = 0.22;
          }
        }
        const aDown = !!gp.buttons[0]?.pressed;
        if (aDown && !padPrevA) {
          sfx.ensure();
          activateFocused();
        }
        padPrevA = aDown;
      }
      navRafId = requestAnimationFrame(pollNav);
    }
    pollNav();

    renderAll();

    document.getElementById("deploy")!.addEventListener("click", () => {
      sfx.ensure(); // user gesture: safe point to start the AudioContext
      sfx.draftPick();
      // Fullscreen requires a direct user-gesture call; fine to fail
      // silently (blocked by browser policy, already fullscreen, etc.) --
      // the game is fully playable windowed too.
      document.documentElement.requestFullscreen?.().catch(() => {});
      saveLoadout(loadout);
      window.removeEventListener("keydown", onKeydown);
      if (navRafId !== null) cancelAnimationFrame(navRafId);
      preview.stop();
      root.remove();
      resolve(loadout);
    });
  });
}
