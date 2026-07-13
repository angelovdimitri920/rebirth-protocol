# REBIRTH PROTOCOL

*(working title — named for the knockdown → rebirth-invincibility comeback rhythm at the heart of the combat, and the roguelite die-and-retry loop around it)*

A 3D arena mecha roguelite — Custom Robo's build-and-battle loop, crossed with Armored Core/Gundam-style weapon loadouts (melee, shields, funnels). Built in Three.js + TypeScript.

## Repo structure

- `docs/GAME_DESIGN.md` — the living design document. Source of truth for mechanics, differentiators, tech stack, and the staged roadmap. Keep it updated as decisions get made; don't let it go stale.
- `prompts/MASTER_BUILD_PROMPT.md` — the prompt used to kick off full development with Claude Code / Fable 5.
- `CLAUDE.md` — project instructions Claude Code reads automatically at session start.
- `src/` — the actual Three.js project (created once development begins: `npm create vite@latest . -- --template vanilla-ts`).

## Status

Stage 1 (core duel prototype) in development. See `docs/GAME_DESIGN.md` §6 for the full staged roadmap and go/no-go criteria for each stage.

## Running

```bash
npm install
npm run dev
```

## GitHub

Hosted at [github.com/angelovdimitri920/rebirth-protocol](https://github.com/angelovdimitri920/rebirth-protocol).
