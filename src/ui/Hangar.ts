import {
  CATALOG,
  loadLoadout,
  saveLoadout,
  type Loadout,
} from "../parts/parts";
import { sfx } from "../core/sfx";

// Pre-fight loadout screen (Stage 2). Pure DOM overlay; resolves with the
// chosen loadout when DEPLOY is clicked. Selection persists in localStorage.

const SLOT_LABELS: Record<keyof Loadout, string> = {
  body: "Body",
  gun: "Gun",
  bomb: "Bomb",
  pod: "Pod",
  legs: "Legs",
  shield: "Shield",
};

export function showHangar(): Promise<Loadout> {
  return new Promise((resolve) => {
    const loadout = loadLoadout();

    const root = document.createElement("div");
    root.id = "hangar";
    root.innerHTML = `
      <style>
        #hangar { position: absolute; inset: 0; background: #0a0a12ee;
          font-family: "Segoe UI", system-ui, sans-serif; color: #cdd6f4;
          display: flex; flex-direction: column; align-items: center;
          overflow-y: auto; padding: 28px 0 40px; z-index: 10; }
        #hangar h1 { margin: 0 0 2px; font-size: 30px; letter-spacing: 8px;
          color: #fff; text-shadow: 0 0 24px #4a6cff; }
        #hangar .sub { color: #55608a; font-size: 13px; margin-bottom: 22px; }
        .slot-row { width: min(980px, 94vw); margin-bottom: 14px; }
        .slot-name { font-size: 12px; letter-spacing: 3px; color: #8894c4;
          text-transform: uppercase; margin-bottom: 6px; }
        .cards { display: flex; gap: 10px; flex-wrap: wrap; }
        .card { flex: 1 1 200px; max-width: 240px; background: #14142299;
          border: 1px solid #333a5c; border-radius: 6px; padding: 10px 12px;
          cursor: pointer; transition: border-color .1s, background .1s; }
        .card:hover { border-color: #5f7fff; }
        .card.selected { border-color: #33e0ff; background: #1b2438; }
        .card .pname { font-weight: 600; font-size: 14px; color: #fff; }
        .card .pblurb { font-size: 11.5px; color: #8894c4; margin-top: 4px;
          line-height: 1.45; }
        #deploy { margin-top: 18px; padding: 12px 64px; font-size: 18px;
          letter-spacing: 6px; font-weight: 700; color: #fff;
          background: linear-gradient(#2f6fd9, #1b3f8f);
          border: 1px solid #5f7fff; border-radius: 4px; cursor: pointer; }
        #deploy:hover { background: linear-gradient(#3f7fe9, #2b4f9f); }
      </style>
      <h1>REBIRTH PROTOCOL</h1>
      <div class="sub">assemble your robo — every slot is a tradeoff</div>
      <div id="slots"></div>
      <button id="deploy">DEPLOY</button>
    `;

    const slotsEl = root.querySelector("#slots")!;
    for (const slot of Object.keys(CATALOG) as (keyof Loadout)[]) {
      const row = document.createElement("div");
      row.className = "slot-row";
      row.innerHTML = `<div class="slot-name">${SLOT_LABELS[slot]}</div>`;
      const cards = document.createElement("div");
      cards.className = "cards";
      for (const part of CATALOG[slot]) {
        const card = document.createElement("div");
        card.className = "card" + (loadout[slot].id === part.id ? " selected" : "");
        card.innerHTML = `<div class="pname">${part.name}</div><div class="pblurb">${part.blurb}</div>`;
        card.addEventListener("click", () => {
          (loadout[slot] as typeof part) = part;
          cards
            .querySelectorAll(".card")
            .forEach((c) => c.classList.remove("selected"));
          card.classList.add("selected");
        });
        cards.appendChild(card);
      }
      row.appendChild(cards);
      slotsEl.appendChild(row);
    }

    root.querySelector("#deploy")!.addEventListener("click", () => {
      sfx.ensure(); // user gesture: safe point to start the AudioContext
      sfx.draftPick();
      saveLoadout(loadout);
      root.remove();
      resolve(loadout);
    });

    document.body.appendChild(root);
  });
}
