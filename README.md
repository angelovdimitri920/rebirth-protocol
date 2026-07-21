# REBIRTH PROTOCOL

*(working title — named for the knockdown → rebirth-invincibility comeback rhythm at the heart of the combat, and the roguelite die-and-retry loop around it)*

A 3D arena mecha roguelite — Custom Robo's build-and-battle loop, crossed with Armored Core/Gundam-style weapon loadouts (melee, shields, funnels). Built in Three.js + TypeScript.

## Repo structure

- `docs/GAME_DESIGN.md` — the living design document. Source of truth for mechanics, differentiators, tech stack, and the staged roadmap. Keep it updated as decisions get made; don't let it go stale.
- `docs/SETTING_AND_FACTIONS.md` — canonical present-day world, factions, Orders, characters, and lexicon.
- `docs/WORLD_HISTORY.md` — canonical Meridian Empire, Last Edict, Great Severance, Long Hush, Ashen history, Edictbound technology, Rebirth Protocol, and Riderless backstory.
- `docs/RELIGION_AND_IDEOLOGY.md` — canonical religions, religious orders, schisms, ideological movements, subfactions, and their relationship to Edict technology.
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
