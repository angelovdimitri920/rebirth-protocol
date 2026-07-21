# Project Instructions

This is a Unity 6000.3.19f1 project (URP, Input System, Cinemachine, Animation Rigging, uGUI, Unity Test Framework; C# throughout): a 3D arena mecha roguelite inspired by Custom Robo, with Armored Core/Gundam-style weapon loadouts, set in a post-apocalyptic neo-feudal used-future world.

This repo also contains the original Three.js prototype (`src/`, `package.json`, `index.html`) that the game was first built and playtested in. **It is a frozen design/feel reference only** — never move, rewrite, delete, or reorganize it, and never port it line-by-line to C# (see `docs/MIGRATION_SOURCE.md` for the full rules).

**Before doing any work, read `docs/GAME_DESIGN.md` in full.** For lore, theme, naming, factions, characters, or story work, also read the complete world-canon set: `docs/SETTING_AND_FACTIONS.md` (present world), `docs/WORLD_HISTORY.md` (Meridian Ascendancy, BFE/AFE chronology, Final Edict, Great Severance, Ashen Century, Edictbound infection, pilot synchronization, Rebirth, Riderless), and `docs/RELIGION_AND_IDEOLOGY.md` (religions, orders, subfactions, and political theology). These supersede `WARBAND_THEME_REFERENCE.md`'s old literal-culture framing. **Edictbound** is the only canonical term for the forbidden technological condition. `GAME_DESIGN.md` remains the source of truth for mechanics and the staged roadmap. Do not invent mechanics that contradict it; if a design decision is genuinely ambiguous or missing, make a reasonable call, note it, and keep moving rather than stalling on small things — but flag anything with a real gameplay-feel tradeoff.

## Stack specifics

- Unity Editor: `D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe` (override with `REBIRTH_UNITY_EXE` on other machines).
- Assembly layout: `RebirthProtocol.Runtime` (gameplay/domain logic — plain C#, unit-testable, no presentation coupling), `RebirthProtocol.Editor` (asset/build tooling), `RebirthProtocol.EditMode.Tests` / `RebirthProtocol.PlayMode.Tests`.
- MonoBehaviours are thin presentation/glue over the plain-C# domain. Runtime state must never mutate ScriptableObject source assets.
- Use deterministic seeds for runs, arena rolls, and draft choices.
- Command-line verification from the repo root: `.\scripts\unity-compile.ps1`, `.\scripts\run-editmode-tests.ps1`, `.\scripts\run-playmode-tests.ps1`, `.\scripts\build-windows-dev.ps1`.
- Art pipeline: original assets are built in Blender via `scripts/asset_pipeline/` and land under `Assets/RebirthProtocol/Art/`; `3D_models/` and `ReferenceOnly/` are git-ignored reference material for proportion/silhouette study only — never import them into `Assets/`.

## Working style

- Do the simplest thing that implements the current stage well. Don't add configurability, abstractions, or systems for stages we haven't reached yet.
- No premature abstraction, no speculative future-proofing, no refactors or cleanup beyond what the current task needs.
- Validate inputs only at real boundaries (user input) — trust your own code elsewhere.
- Prefer small, testable increments — get one mechanic feeling right before moving to the next, rather than building a whole stage at once and tuning at the end.
- Domain logic gets EditMode tests before scenes/MonoBehaviours get built on top of it. Self-check with the compile/test scripts before reporting work as done.
- Work on feature branches off `main`; open a PR when a stage is ready for review rather than pushing straight to `main`. Commit meaningful milestones with clear messages.
- Lead with the outcome when reporting back: what changed and whether it's ready to test, before the detailed explanation of how.
- Keep `docs/GAME_DESIGN.md` updated as decisions get made or mechanics get tuned differently than planned — it should stay the accurate source of truth, not a historical artifact.
