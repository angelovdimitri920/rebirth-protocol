# The Task Ladder — Implementation Plan & Working Protocol

*The ordered build plan for turning the content canon into the game (2026-07-18). Consolidates `ARMORY_REFERENCE.md` §12, `COMBAT_DOCTRINE.md` §10, and `ARENA_ROSTER.md` §5 into one ladder. One task = one session = one branch = one PR.*

## Working protocol (every task)

1. **Branch** `feature/pass-<id>` off `main`.
2. **Implement** per the canon docs (specs live there — don't re-derive).
3. **Verify**: `unity-compile.ps1` → EditMode/PlayMode tests → built-player smoke with `-autodeploy` (grep the player log for exceptions). Never trust editor-only checks for visuals or shaders.
4. **Push the branch and open a PR** (`gh pr create`), then write the Codex review handoff citing the PR.
5. **Codex reviews** → fix findings on the branch → **user merges**. Never merge a base branch out from under a stacked PR (the #7/#8 lesson).
6. Log the pass in `GAME_DESIGN.md`; exclude Codex's concurrent asset paths (`SourceArt/**`, `Art/Mechs/**` untracked work, `scripts/asset_pipeline/*`) from every commit.

**Division of labor**: **Fable 5** carries the heavy work — domain systems, combat mechanics, the balance harness, AI framework, content waves, tuning. **Codex** reviews PRs and builds rigged assets in parallel. **The user** playtests feel (no gamepad in the dev environment — every pass ends with one specific feel question for a human) and merges.

## The ladder

Ordered so infrastructure precedes content, and every content wave lands on a validated capability. Tasks marked ⚑ are the highest-leverage "beefy" ones.

| # | Task | Scope | Canon spec |
|---|---|---|---|
| **B** | ~~Shield toll & raise behaviors~~ **done** — [PR #11](https://github.com/angelovdimitri920/rebirth-protocol/pull/11) | ~~`ShieldPart` toll + Root/March/Air-hold/Air-drop; toll UI like the bomb's; new shields **Targe, Kite Ward, Quiet Bell**~~ | ARMORY §2.3–2.4, §7 |
| **B2** | ~~Overload rule~~ **done** — [PR #13](https://github.com/angelovdimitri920/rebirth-protocol/pull/13) | ~~Knockdown wipes the downed pilot's in-flight gun rounds; groundwork flag for the scrapwright exemption~~ | DOCTRINE §4.3 |
| **C** | ~~Charge attacks~~ **done** — [PR #14](https://github.com/angelovdimitri920/rebirth-protocol/pull/14) | ~~Grounded X + lock = garniture charge; per-body charge data; i-frames during, vulnerability around; the 4 built bodies~~ | DOCTRINE §4.5, §11; ARMORY §3 |
| **D** ⚑ | ~~Balance harness~~ **done** — [PR #16](https://github.com/angelovdimitri920/rebirth-protocol/pull/16) | ~~Headless seeded AI-vs-AI batch runner; win-rate/TTK/knockdown reports with 40–60% and 60–120 s flags. First matrices = pillar-5 loadout shapes + the 4 built bodies (schools roster deferred to Pass M); `SimulationMode.Script` for bit-identical determinism~~ | DOCTRINE §13 |
| **E** | ~~Volley capability~~ **done** — [PR #18](https://github.com/angelovdimitri920/rebirth-protocol/pull/18) | ~~Multi-projectile/spread → **Trefoil, Palisade, Pincer Charge, Longglaive, Hydra Flail**~~ | ARMORY §4–§7 |
| **F** | Fetter status | Immobilize distinct from knockdown (+2 s fetter-immunity rule) → **Fetterlock, Rime Charge, Winterwatch, Knell Maul, Tocsin Mace, Hoarfrost Ward** | ARMORY; DOCTRINE §13 pillar 9 |
| **G** | Pulls & piercing | On-hit pull forces; guard-piercing → **Grapnel, Hookbill, Estoc, Auger, Sawtooth Espadon** | ARMORY |
| **H** | Scaling & delayed threats | Distance/lunge/speed damage scaling; hanging/delayed actives → **Pilgrim, Tilt Lance, Courser Saber, Vigil, Penitent Flail, Beacon, Crowbeak Pick** | ARMORY §13.1 |
| **I** | Trajectory suite | Arcing guns, vertical drops, curves, returns, mines → **Mangonel, Evenfall, Skysword, Steeplefall, Oxbow, Oubliette(s), Falconet, Volant Falx** | ARMORY |
| **J** | Arena geometry batch | **The Close, the Longfield, the Hushplain, the Thornfield, the Gallows Ell** — cheap variety; interleave anywhere the pace needs a light session | ARENA §5.1 |
| **K** | Shield wave + pair fill | **Argent Mirror, Bastille, Cenotaph, Pallium, Echo, Springald, Cheval, Testudo, Thorn, Canopy, Caltrop Wards** + remaining gun/melee pairs | ARMORY §7 |
| **L** | Pod & legs suite | Pod behaviors (course/ambush/vault/retreat/mark) → **Alaunt, Gargoyle, Lurcher, Ratter** etc.; legs fields (TurnMult, FallSpeedMult, hazard grip) → **Destrier, Thistledown, Heron** etc. | ARMORY §8–§9 |
| **M** ⚑ | Garnitures & new chassis | Field/War/Chase stat trims for the built 4; **Harrier, Freelance, Sunplume, Skyanvil, Cockatrice** stats (primitive meshes fine); multi-jump capability | ARMORY §3 |
| **N** | Tempers + Branded | Shared `ImpactType` through knockback/knockdown resolution; retrofit bombs/pods | ARMORY §2.5 |
| **O** ⚑ | The Casting, laurels, AI Classes | Fight-opening ritual; laurels scoring + mercy rule with the run layer; the Class C/B/A/S parameter framework on the AI | DOCTRINE §1, §7–§8, §12 |
| **P** | Scrapwright + Hushforged | The dependable line with perks; then the banned tier with drawbacks (own design review before build) | ARMORY §1.2, §16 |
| **Q** | Arena batches 2+ | Landmark Lists, overlays as true rolls, moving systems, terrain | ARENA §5.2–5.6 |
| **R** | Lasthold shell | The JRPG hub — last, once combat content deserves a world around it | GAME_DESIGN §26 |

## Session kickoff line

> Read `docs/TASK_LADDER.md` and run the next unfinished task on the ladder, following the working protocol.

Mark tasks done here (change the row to **~~struck~~ + PR link**) as they merge, so the ladder is always the single source of "what's next."
