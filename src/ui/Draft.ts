import { BOON_POOL, type Boon, type Effects } from "../run/effects";
import { sfx } from "../core/sfx";

// Post-fight boon draft (GAME_DESIGN §4): 3 choices, one boon per ability
// slot in the offer, reroll as mitigation. Resolves with the picked boon
// (or null on skip).

function offerBoons(effects: Effects): Boon[] {
  const available = BOON_POOL.filter((b) => !effects.has(b.id));
  // One boon per slot in the offer: group by slot, pick random slots
  const bySlot = new Map<string, Boon[]>();
  for (const b of available) {
    if (!bySlot.has(b.slot)) bySlot.set(b.slot, []);
    bySlot.get(b.slot)!.push(b);
  }
  const slots = [...bySlot.keys()].sort(() => Math.random() - 0.5);
  return slots.slice(0, 3).map((s) => {
    const group = bySlot.get(s)!;
    return group[Math.floor(Math.random() * group.length)];
  });
}

export function showDraft(
  effects: Effects,
  rerollsLeft: number,
  onReroll: () => void,
): Promise<Boon | null> {
  return new Promise((resolve) => {
    const root = document.createElement("div");
    root.id = "draft";

    const render = (rerolls: number) => {
      const boons = offerBoons(effects);
      root.innerHTML = `
        <style>
          #draft { position: absolute; inset: 0; background: #0a0a12dd;
            display: flex; flex-direction: column; align-items: center;
            justify-content: center; z-index: 10;
            font-family: "Segoe UI", system-ui, sans-serif; }
          #draft h2 { color: #fff; letter-spacing: 5px; margin: 0 0 4px;
            text-shadow: 0 0 18px #4a6cff; }
          #draft .sub { color: #55608a; font-size: 13px; margin-bottom: 24px; }
          #draft .choices { display: flex; gap: 16px; }
          #draft .boon { width: 230px; background: #141422; padding: 18px;
            border: 1px solid #333a5c; border-radius: 8px; cursor: pointer;
            transition: border-color .1s, transform .1s; }
          #draft .boon:hover { border-color: #33e0ff; transform: translateY(-3px); }
          #draft .slot { font-size: 10px; letter-spacing: 3px; color: #8894c4;
            text-transform: uppercase; }
          #draft .bname { color: #fff; font-weight: 700; font-size: 17px;
            margin: 6px 0; }
          #draft .bblurb { color: #98a4d4; font-size: 12.5px; line-height: 1.5; }
          #draft .actions { margin-top: 22px; display: flex; gap: 12px; }
          #draft button { padding: 8px 22px; background: #1b2438;
            color: #98a4d4; border: 1px solid #333a5c; border-radius: 4px;
            cursor: pointer; font-size: 13px; letter-spacing: 2px; }
          #draft button:hover:not(:disabled) { border-color: #5f7fff; color: #fff; }
          #draft button:disabled { opacity: 0.35; cursor: default; }
        </style>
        <h2>VICTORY</h2>
        <div class="sub">install one boon</div>
        <div class="choices">
          ${boons
            .map(
              (b, i) => `
            <div class="boon" data-i="${i}">
              <div class="slot">${b.slot}</div>
              <div class="bname">${b.name}</div>
              <div class="bblurb">${b.blurb}</div>
            </div>`,
            )
            .join("")}
        </div>
        <div class="actions">
          <button id="reroll" ${rerolls <= 0 ? "disabled" : ""}>REROLL (${rerolls})</button>
          <button id="skip">SKIP</button>
        </div>
      `;
      root.querySelectorAll(".boon").forEach((el) => {
        el.addEventListener("click", () => {
          const boon = boons[Number((el as HTMLElement).dataset.i)];
          sfx.draftPick();
          root.remove();
          resolve(boon);
        });
      });
      root.querySelector("#reroll")!.addEventListener("click", () => {
        if (rerolls > 0) {
          onReroll();
          render(rerolls - 1);
        }
      });
      root.querySelector("#skip")!.addEventListener("click", () => {
        root.remove();
        resolve(null);
      });
    };

    render(rerollsLeft);
    document.body.appendChild(root);
  });
}
