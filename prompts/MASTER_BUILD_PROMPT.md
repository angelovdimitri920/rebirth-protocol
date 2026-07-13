I'm building [Working Title], a 3D arena mecha combat roguelite — inspired by Custom Robo: Battle Revolution (GameCube), crossed with Armored Core/Gundam-style weapon loadouts (melee, shields, funnels) and a real-time roguelike run structure. Full design context is in `docs/GAME_DESIGN.md` — read it in full before writing any code. `CLAUDE.md` has our tech stack and working style; follow it.

**Stack:** Three.js + TypeScript, bundled with Vite, physics/character-controller via Rapier3D (`@dimforge/rapier3d-compat`). Scaffold it with `npm create vite@latest . -- --template vanilla-ts` and add dependencies as needed.

**Scope for this engagement: build through the full staged roadmap in the design doc (§6, Stages 1–4), not just one piece of it.** Work through the stages in order:

1. **Core duel prototype** — boost-economy movement (jump/air-dash/hover spending a shared gauge that only refills on landing, with landing recovery scaling to spend), lock-on targeting, one gun weapon, one melee weapon with a punishable gap-closer, the HP/endurance/knockdown/rebirth loop, and one arena with basic cover.
2. **Full five-slot loadout system** (body/gun/bomb/pod/legs as swappable resources with real stat tradeoffs) plus one shield archetype, wired into the existing stagger/knockdown state rather than stacking a second free defense on top of it.
3. **The roguelite run structure** — a sequence of duels, boon/item drafting between fights (three choices, one boon per ability slot), and arena modifier rolls (hazard/layout variety per fight).
4. **Aerial identity and refinement** — homing dash, funnels/pods with their own independent energy pool, melee-clash/parry counterplay.

**Before moving from one stage to the next, stop and check in with me** using the go/no-go criteria already written in the design doc for that stage (for example: "does the movement-and-punish loop feel tense and good with zero upgrades, for 10+ minutes?"). Don't silently work through all four stages back-to-back — I want to actually play and react to each one before you build on top of it. Within a stage, where you hit a genuine judgment call the design doc doesn't resolve (see §7, Open Design Questions), make a reasonable decision, note what you chose and why, and keep moving rather than stopping to ask about every small thing.

**Working style:**
- Do the simplest thing that implements the current stage well. Don't build configurability, abstractions, or systems for a later stage before we're there.
- No premature abstraction, no speculative future-proofing, no refactors or cleanup beyond what the current task actually needs.
- Validate inputs only at real boundaries (user input, anything crossing a network if we get there) — trust your own code elsewhere.
- Commit to git as you complete meaningful milestones, with clear messages. I'll push to GitHub myself once I'm happy with where things stand, so keep the history clean enough that I can do that without squashing anything first.
- When you report back, lead with the outcome — what changed and whether it's ready for me to test — before the detailed explanation of how you built it.
- Use `npm run dev` to run and self-check your own work as you go: watch the browser console for errors, and tell me specifically what I should expect to see or try so I can confirm the feel myself.

Start with Stage 1. Before writing any code, give me a short plan for the project's file/module structure so I can sanity-check it before you build it out.
