# [Working Title]

A 3D arena mecha roguelite — Custom Robo's build-and-battle loop, crossed with Armored Core / Gundam-style weapon loadouts (melee, shields, funnels), built in Godot 4.

## Repo structure

- `docs/GAME_DESIGN.md` — the living design document. This is the source of truth for mechanics, differentiators, and the staged roadmap. Update it as decisions get made during development; don't let it go stale.
- `prompts/PHASE_1_KICKOFF.md` — the initial prompt used to kick off Stage 1 development with Claude Code / Fable 5.
- `CLAUDE.md` — project instructions Claude Code reads automatically at session start.
- `project/` — the actual Godot project (create this once Stage 1 begins).

## Status

Design phase complete for Stage 1 (core duel prototype). See `docs/GAME_DESIGN.md` §6 for the full staged roadmap and go/no-go criteria for each stage.

## Getting started (for the developer, not the agent)

```bash
git init
git add .
git commit -m "Initial design doc and project scaffold"
git branch -M main
git remote add origin <your-github-repo-url>
git push -u origin main
```
