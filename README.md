# REBIRTH PROTOCOL

*(working title — named for the knockdown → rebirth-invincibility comeback rhythm at the heart of the combat, and the roguelite die-and-retry loop around it)*

A 3D arena mecha roguelite — Custom Robo's build-and-battle loop, crossed with Armored Core/Gundam-style weapon loadouts (melee, shields, funnels). Built in Three.js + TypeScript.

## Repo structure

- `docs/00_CANON_INDEX.md` — the creative-documentation authority map, canon-status vocabulary, and change-control guide.
- `docs/01_THEME_AND_PREMISE.md` through `docs/09_CANON_LEDGER_AND_BACKLOG.md` — the numbered creative authorities for theme, history, setting rules, the present world, factions, characters, campaign, gameplay, and unresolved canon.
- `docs/GAME_DESIGN.md` — detailed mechanics, implementation history, and the staged roadmap; Section 08 owns the high-level gameplay contract.
- `docs/SETTING_AND_FACTIONS.md` — detailed legacy support for the present world, factions, Orders, characters, and lexicon, under the numbered authority map.
- `docs/WORLD_HISTORY.md` — detailed historical support for the Meridian Ascendancy, BFE/AFE timeline, Final Edict, Great Severance, Long Hush, Ashen Century, Present Age, Edictbound infection, Rebirth Protocol, and Riderless backstory.
- `docs/RELIGION_AND_IDEOLOGY.md` — detailed specialist reference for religions, orders, schisms, ideological movements, subfactions, and their relationship to Edictbound technology.
- `prompts/MASTER_BUILD_PROMPT.md` — the prompt used to kick off full development with Claude Code / Fable 5.
- `CLAUDE.md` — project instructions Claude Code reads automatically at session start.
- `src/` — the actual Three.js project (created once development begins: `npm create vite@latest . -- --template vanilla-ts`).
- `Assets/`, `Packages/`, `ProjectSettings/` — Unity 6000.3.19f1 project foundation for the production track.
- `SourceArt/KnightRobot/` — Blender source for the original Cobalt Knight robot asset.
- `docs/UNITY_SETUP.md` and `docs/KNIGHT_ROBOT_ASSET.md` — Unity setup notes and current robot asset validation report.

## Status

Stage 1 (core duel prototype) in development. See `docs/GAME_DESIGN.md` §6 for the full staged roadmap and go/no-go criteria for each stage.

## Running

```bash
npm install
npm run dev
```

Unity commands are run from PowerShell with Unity 6000.3.19f1 installed:

```powershell
.\scripts\unity-compile.ps1
.\scripts\run-editmode-tests.ps1
.\scripts\run-playmode-tests.ps1
.\scripts\build-windows-dev.ps1
```

## GitHub

Hosted at [github.com/angelovdimitri920/rebirth-protocol](https://github.com/angelovdimitri920/rebirth-protocol).
