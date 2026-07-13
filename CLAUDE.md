# Project Instructions

This is a Godot 4 (GDScript) project: a 3D arena mecha roguelite inspired by Custom Robo, with Armored Core/Gundam-style weapon loadouts.

**Before doing any work, read `docs/GAME_DESIGN.md` in full.** It is the source of truth for mechanics, scope, and the staged roadmap. Do not invent mechanics that contradict it; if a design decision is genuinely ambiguous or missing, ask rather than guessing.

## Current scope

We are in **Stage 1** (see design doc §6): a single arena, single robo, boost-economy movement (jump/air-dash/hover spending a shared gauge that only refills on landing), lock-on targeting, one gun weapon and one melee weapon with a gap-closer, and the HP/endurance/knockdown/rebirth loop.

**Do not build Stage 2+ systems yet** (full five-slot loadout swapping, shields, roguelite run structure, boons/items, additional arenas) unless explicitly asked. Stage 1 is scoped deliberately narrow so the core combat feel can be validated before anything else is layered on.

## Working style

- Do the simplest thing that implements the current stage well. Don't add configurability, abstractions, or systems for stages we haven't reached yet.
- Prefer small, testable increments — get one mechanic feeling right before moving to the next, rather than building all of Stage 1 at once and tuning at the end.
- If something in the design doc turns out not to feel good in practice, say so and propose a specific alternative rather than silently deviating from the doc.
- Keep `docs/GAME_DESIGN.md` updated as decisions get made or mechanics get tuned differently than originally planned — it should stay the accurate source of truth, not a historical artifact.
