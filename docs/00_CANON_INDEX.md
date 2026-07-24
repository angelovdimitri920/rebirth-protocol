# Rebirth Protocol Canon Index

*Documentation authority map and change-control guide. Consolidated baseline: 2026-07-23.*

## 1. Purpose

This index defines where every creative fact in **Rebirth Protocol** belongs. The numbered documents are the top-level canon structure. Specialist files preserve the detailed combat, armory, arena, art, religion, and production work that would make the numbered documents too large to use.

The governing rule is:

> **Every fact has one authoritative home. Other documents summarize it and link to that home.**

The current files were written at different stages and sometimes mix canon, implementation history, research, and proposals. This index does not silently promote those proposals. It records what is confirmed, what remains draft, and what still needs a decision.

## 2. Canon-status vocabulary

Every numbered document uses the following statuses:

| Status | Meaning |
|---|---|
| **Confirmed canon** | Approved creative fact. Later work must preserve it unless a deliberate canon change is recorded. |
| **Draft** | Developed enough to evaluate, but not yet approved as binding canon. |
| **Intentional mystery** | The project deliberately withholds a definitive answer. Writers may develop evidence and interpretations, but cannot settle the answer without approval. |
| **Undeveloped** | Required material that does not yet have a substantive answer. |
| **Superseded** | Replaced material retained only for history, research, or migration context. |

Implementation state is separate from canon state. A rule can be **confirmed canon but not built**, or **built but still awaiting a feel decision**. Specialist gameplay documents may use implementation tags such as `built`, `directed`, `planned`, and `reference`.

## 3. The authoritative hierarchy

| # | Document | Primary authority |
|---|---|---|
| 00 | [`00_CANON_INDEX.md`](00_CANON_INDEX.md) | Document ownership, status rules, source classification, and merge sequence |
| 01 | [`01_THEME_AND_PREMISE.md`](01_THEME_AND_PREMISE.md) | Creative identity, premise, player fantasy, themes, tone, story promise, and non-negotiables |
| 02 | [`02_HISTORICAL_FOUNDATION.md`](02_HISTORICAL_FOUNDATION.md) | Meridian history, Final Edict chronology, post-Edict eras, Concord of Ash, and historical unknowns |
| 03 | [`03_TECHNOLOGY_AND_SETTING_RULES.md`](03_TECHNOLOGY_AND_SETTING_RULES.md) | Technology taxonomy, Severance, Edictbound rules, harnesses, pilots, Rebirth, and machine-personhood boundaries |
| 04 | [`04_PRESENT_DAY_WORLD_AND_ATLAS.md`](04_PRESENT_DAY_WORLD_AND_ATLAS.md) | Present political order, geography, named places, countries, travel, economy, and atlas development |
| 05 | [`05_POWERS_FAITHS_AND_FACTIONS.md`](05_POWERS_FAITHS_AND_FACTIONS.md) | Governments, Houses, Orders, faiths, guilds, ideological movements, and relationships among them |
| 06 | [`06_CHARACTERS_AND_RELATIONSHIPS.md`](06_CHARACTERS_AND_RELATIONSHIPS.md) | Protagonist, rivals, allies, antagonists, personal arcs, and relationship continuity |
| 07 | [`07_CAMPAIGN_AND_NARRATIVE.md`](07_CAMPAIGN_AND_NARRATIVE.md) | Campaign spine, narrative structure, revelations, quests, choices, endings, and story delivery |
| 08 | [`08_GAMEPLAY_TOURNAMENT_AND_PROGRESSION.md`](08_GAMEPLAY_TOURNAMENT_AND_PROGRESSION.md) | Player loop, combat contract, Passages, Grand Passage, rewards, progression, and campaign-gameplay integration |
| 09 | [`09_CANON_LEDGER_AND_BACKLOG.md`](09_CANON_LEDGER_AND_BACKLOG.md) | Locked decisions, drafts, intentional mysteries, contradictions, unresolved questions, and development order |

When two numbered documents touch the same subject, the more specific document owns the fact. For example:

- Section 01 defines why Edictbound power matters thematically.
- Section 03 defines how Edictbound infection works.
- Section 05 defines what each faction wants from it.
- Section 07 defines how it enters the campaign.
- Section 08 defines its gameplay cost.

## 4. Specialist documents

The following files remain useful and canonical within their stated scope. The numbered documents summarize them rather than duplicating their full catalogs.

| Specialist file | Role | Parent section |
|---|---|---|
| [`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md) | Detailed bodies, weapons, tiers, stewardship, damage profiles, and Edictbound equipment rules | 03 and 08 |
| [`COMBAT_DOCTRINE.md`](COMBAT_DOCTRINE.md) | Detailed battle rhythm, ranges, AI doctrine, controls, pacing, and balance contract | 08 |
| [`ARENA_ROSTER.md`](ARENA_ROSTER.md) | The Lists, layouts, overlays, home arenas, and arena rollout | 04 and 08 |
| [`ART_DIRECTION.md`](ART_DIRECTION.md) | Harness silhouettes, materials, heraldry, animation, VFX, and audio | 01 and 03 |
| [`RELIGION_AND_IDEOLOGY.md`](RELIGION_AND_IDEOLOGY.md) | Detailed doctrines, rites, orders, subfactions, symbols, and religious practices | 05 |
| [`ROBOT_SHELL_DESIGN.md`](ROBOT_SHELL_DESIGN.md) | Early silhouette and body-archetype reference that remains valid where it does not conflict with `ART_DIRECTION.md` | 01 and 03 |

If a specialist document conflicts with a numbered document after this structure is adopted, record the conflict in Section 09. Do not silently choose one.

## 5. Mixed legacy documents

These files supplied much of the material now consolidated into Sections 01 through 09. They remain in place during the migration so no detail or implementation history is lost.

| File | Current treatment |
|---|---|
| [`GAME_DESIGN.md`](GAME_DESIGN.md) | Mixed original design, current mechanics, implementation log, and roadmap. Section 08 owns the high-level gameplay contract; this file remains the detailed implementation history until it is decomposed. |
| [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md) | Mixed present-world, factions, terminology, tournament, and characters. Its material is now indexed across Sections 01, 04, 05, 06, 07, and 08. |
| [`WORLD_HISTORY.md`](WORLD_HISTORY.md) | Detailed source for Section 02 and portions of Section 03. Retain until the historical migration is approved and checked line by line. |

No mixed legacy file should be deleted or archived until every unique fact has been assigned an authoritative home and all links have been checked.

## 6. Research, production, migration, and archive classification

### Research references

These informed the design but do not override current canon:

- [`PARTS_AND_DAMAGE_REFERENCE.md`](PARTS_AND_DAMAGE_REFERENCE.md)
- [`HOLOSSEUM_REFERENCE.md`](HOLOSSEUM_REFERENCE.md)

### Production and technical references

These remain outside the nine creative sections:

- [`BLENDER_PIPELINE.md`](BLENDER_PIPELINE.md)
- [`KNIGHT_ROBOT_ASSET.md`](KNIGHT_ROBOT_ASSET.md)
- [`TASK_LADDER.md`](TASK_LADDER.md)
- [`UNITY_SETUP.md`](UNITY_SETUP.md)

### Migration and historical records

- [`MIGRATION_SOURCE.md`](MIGRATION_SOURCE.md)
- [`DESIGN_HANDOFF_FROM_THREEJS.md`](DESIGN_HANDOFF_FROM_THREEJS.md)
- [`NEXT_BATCH.md`](NEXT_BATCH.md)

`NEXT_BATCH.md` is an early implementation checklist and is superseded for current task ordering by `TASK_LADDER.md`.

### Superseded creative reference

- [`WARBAND_THEME_REFERENCE.md`](WARBAND_THEME_REFERENCE.md) is **Superseded**. Its literal Earth-culture framing is not current canon. Valid silhouette work survives only where later documents explicitly retained it.

## 7. Intended folder organization

The long-term supporting structure is:

```text
docs/
├── 00_CANON_INDEX.md
├── 01_THEME_AND_PREMISE.md
├── 02_HISTORICAL_FOUNDATION.md
├── 03_TECHNOLOGY_AND_SETTING_RULES.md
├── 04_PRESENT_DAY_WORLD_AND_ATLAS.md
├── 05_POWERS_FAITHS_AND_FACTIONS.md
├── 06_CHARACTERS_AND_RELATIONSHIPS.md
├── 07_CAMPAIGN_AND_NARRATIVE.md
├── 08_GAMEPLAY_TOURNAMENT_AND_PROGRESSION.md
├── 09_CANON_LEDGER_AND_BACKLOG.md
├── gameplay/
├── art/
├── production/
├── reference/
└── archive/
```

The current consolidation leaves supporting files at their existing paths. Physical moves should happen only when the integration pass can update every repository link in one verified change. The target classification is:

- `gameplay/`: `ARMORY_REFERENCE.md`, `COMBAT_DOCTRINE.md`, `ARENA_ROSTER.md`
- `art/`: `ART_DIRECTION.md`, `ROBOT_SHELL_DESIGN.md`
- `production/`: `BLENDER_PIPELINE.md`, `KNIGHT_ROBOT_ASSET.md`, `TASK_LADDER.md`, `UNITY_SETUP.md`
- `reference/`: `PARTS_AND_DAMAGE_REFERENCE.md`, `HOLOSSEUM_REFERENCE.md`
- `archive/`: `WARBAND_THEME_REFERENCE.md`, `MIGRATION_SOURCE.md`, `DESIGN_HANDOFF_FROM_THREEJS.md`, `NEXT_BATCH.md`, and the eventual extracted development log

This staged approach keeps the first canon merge reviewable and avoids obscuring substantive content changes beneath file-renaming noise.

## 8. Change-control rules

1. Check Section 09 before changing a canon fact.
2. Identify the fact's authoritative home using this index.
3. Mark new material with the correct canon status.
4. Update summaries and links, not duplicate full passages.
5. Add unresolved contradictions to Section 09.
6. Preserve intentional mysteries unless the user explicitly approves an answer.
7. Record deliberate retcons in the superseded ledger.
8. Keep implementation logs separate from fictional chronology.
9. Never infer world geography, protagonist history, or campaign endings from aesthetic inspiration alone.
10. Historical cultures are design references, not surviving Earth nations in the setting.

## 9. Baseline audit coverage

The consolidated baseline was checked against the complete Rebirth Protocol documentation set available in `docs/`.

### Core creative and design sources

- `GAME_DESIGN.md`
- `SETTING_AND_FACTIONS.md`
- `WORLD_HISTORY.md`
- `RELIGION_AND_IDEOLOGY.md`
- `ART_DIRECTION.md`
- `COMBAT_DOCTRINE.md`
- `ARMORY_REFERENCE.md`
- `ARENA_ROSTER.md`
- `ROBOT_SHELL_DESIGN.md`

### Research, migration, production, and implementation sources

- `PARTS_AND_DAMAGE_REFERENCE.md`
- `HOLOSSEUM_REFERENCE.md`
- `BLENDER_PIPELINE.md`
- `KNIGHT_ROBOT_ASSET.md`
- `UNITY_SETUP.md`
- `TASK_LADDER.md`
- `MIGRATION_SOURCE.md`
- `DESIGN_HANDOFF_FROM_THREEJS.md`
- `NEXT_BATCH.md`
- `WARBAND_THEME_REFERENCE.md`

The repository `README.md`, `CLAUDE.md`, and Unity master build prompt were also checked for current project framing, source-of-truth instructions, and migration conflicts. Their outdated authority and engine statements are recorded in Section 09 rather than silently rewritten during baseline creation.

Relevant user-approved conversation decisions were included when they postdated or clarified the files, especially:

- Renaming Section 01 to **Theme and Premise**.
- Separating documentation merges into the `CANON-##` series.
- Preserving the campaign spine and unresolved mystery boundaries.
- Recording the approved headless, handless knight-harness visual concept without making it a universal chassis rule.

Only the Rebirth Protocol repository and its project history are valid sources for this structure. External or unrelated project documents are outside scope.

This audit does not make every baseline statement final. Drafts, undeveloped areas, contradictions, and intentional mysteries retain their explicit statuses and receive section-by-section review in CANON-01 through CANON-09.

## 10. Canon merge series

The documentation series is separate from numbered game-development merge requests:

| Internal series | Scope |
|---|---|
| **CANON-00** | Documentation structure, canon index, and initial consolidated baseline for Sections 01 through 09 |
| **CANON-01** | Theme and Premise |
| **CANON-02** | Historical Foundation |
| **CANON-03** | Technology and Setting Rules |
| **CANON-04** | Present-Day World and Atlas |
| **CANON-05** | Powers, Faiths, and Factions |
| **CANON-06** | Characters and Relationships |
| **CANON-07** | Campaign and Narrative |
| **CANON-08** | Gameplay, Tournament, and Progression |
| **CANON-09** | Canon Ledger and Development Backlog |

CANON-00 imports the complete baseline so dependencies, existing information, gaps, and contradictions are visible immediately. Each later CANON merge can review, deepen, revise, and approve one section without mixing it with ordinary game-code work.
