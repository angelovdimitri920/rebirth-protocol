# Project Instructions

This is a Three.js + TypeScript project (bundled with Vite, physics/character-controller via Rapier3D): a 3D arena mecha roguelite inspired by Custom Robo, with Armored Core/Gundam-style weapon loadouts.

**Before doing any work, read `docs/GAME_DESIGN.md` in full.** It is the source of truth for mechanics, tech stack, and the staged roadmap. Do not invent mechanics that contradict it; if a design decision is genuinely ambiguous or missing, make a reasonable call, note it, and keep moving rather than stalling on small things — but flag anything with a real gameplay-feel tradeoff.

## Stack specifics

- Three.js for rendering, Rapier3D (`@dimforge/rapier3d-compat`) for physics/movement, TypeScript throughout, Vite for dev/build.
- Prefer `KinematicCharacterController` (Rapier) for the player robo rather than a generic rigid body — the boost-economy movement needs precise grounded/airborne state control.
- UI/HUD as HTML/CSS overlay, not in-canvas rendering.

## Working style

- Do the simplest thing that implements the current stage well. Don't add configurability, abstractions, or systems for stages we haven't reached yet.
- No premature abstraction, no speculative future-proofing, no refactors or cleanup beyond what the current task needs.
- Validate inputs only at real boundaries (user input, network if we get there) — trust your own code elsewhere.
- Prefer small, testable increments — get one mechanic feeling right before moving to the next, rather than building a whole stage at once and tuning at the end.
- Commit to git as you complete meaningful milestones, with clear messages. The repo isn't pushed to GitHub yet — that happens manually later — so keep history clean enough to push as-is.
- Lead with the outcome when reporting back: what changed and whether it's ready to test, before the detailed explanation of how.
- Keep `docs/GAME_DESIGN.md` updated as decisions get made or mechanics get tuned differently than planned — it should stay the accurate source of truth, not a historical artifact.
