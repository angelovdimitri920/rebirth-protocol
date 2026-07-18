# The Lists — Canonical Arena Roster v1

*Canonical named-arena roster for Rebirth Protocol (2026-07-18). Builds on [`HOLOSSEUM_REFERENCE.md`](HOLOSSEUM_REFERENCE.md), which extracted the design levers from the source material's 35 stages (its coined prototypes — the Basin, the Closing Vein, the Sunder, the Open Void — are adopted and named here); this doc turns those levers into a themed roster for the world of [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md). Source basis: the user-supplied Map FAQ / Holosseum lists (35 stages with descriptions and hazard notes). All names original.*

## 1. What the Lists are, in fiction

Duels between armigers are fought in **the Lists**: consecrated dueling grounds, most of them cleared inside ancestral ruins. Some are dead ground. Some are *not* — conveyor lines that never stopped running, defense grids that still cycle, machinery whose purpose died with the old world. The Orders didn't build the hazards; they **ritualized** them. Fighting among the ancestors' still-turning wheels is considered the highest form of the art.

This reframes every "weird mechanical stage" from the source as world-building: a conveyor arena isn't a factory theme, it's *a factory nobody can turn off*.

## 2. The roster

Status: **built** = exists in `ArenaBuilder` today (renamed from its working label); **planned** = designed here, unbuilt; **story** = tied to future narrative content.

| # | List | Source basis | Core lever | Landmark / hazard | Status |
|---|---|---|---|---|---|
| 1 | **The Tiltyard** | Basic Arena | Neutral baseline | Supply-cask cover (destructible crates) | Built (was "Depot") |
| 2 | **The Broken Colonnade** | High-Rise Plaza | Tall cover vs. low cover | High pillars + low triangular stubs | Built (was "Colonnade") |
| 3 | **Winterfield** | Ice and Snow / Frozen Field | Footing | Central ice sheet; momentum carries | Built (was "Frostfield") |
| 4 | **Cinderfield** | Basic Cell | Corner hazards | Three magma pools, DoT bypasses shields | Built |
| 5 | **The Ancestors' Foundry** | Checkmate Foundry | Conveyors | Belt lanes that never stopped; fighting against the belt makes you a slow target | Planned |
| 6 | **The Slagrun** | Dead Line / Double Dead Line | Conveyor + fatal edge | Belts feed a molten channel; immobilize/knockdown near the edge is the win condition | Planned |
| 7 | **Keep of the Rust Cross** | Castle Keep / Castle Citadel | Sprawling walls + one landmark | The central reliquary-shrine wall — "hold the shrine" is the callout | Planned |
| 8 | **The Closing Vein** | Magma Ruins | Shrinking safe zone | A magma ring that tightens over the fight — spacious early, knife-range late | Planned |
| 9 | **The Basin** | Chinese Bowl | Sloped terrain | A tilted bowl: downhill is fast and knockback carries; uphill is neither | Planned |
| 10 | **The Sunder** | Impact Craters | Terrain removal | The floor splits and shifts; gaps can't be crossed | Planned |
| 11 | **The Hushplain** | No Man's Land | Zero walls | Dead-flat scoured ground; homing and spread weapons change entirely | Planned |
| 12 | **The Restless Vault** | Panic Cubes / Panic Walls / Scramble Walls | Moving cover | An ancestral defense grid still cycling its blocks/walls on a rhythm you can learn | Planned |
| 13 | **The Orrery** | Battle Gear Station / Merry-Go-Round | Rotating platforms | A vast astronomical machine; ride the wheels, fire between the spokes | Planned |
| 14 | **The Drowned Quay** | Loading Dock | Moving platform | A suspended cargo platform that never stopped its route — Drowned Compact home List | Planned |
| 15 | **The Reliquary Garden** | Flower Garden / Nature Park | Central arch + mixed cover | An overgrown shrine garden; the arch-bridge commands the whole List | Planned |
| 16 | **The Close** | Sudden Death | Minimum size | The smallest List: no room, no mercy | Planned |
| 17 | **The Longfield** | Gigantix Sprawl | Maximum size | The largest List: spacing *is* the fight | Planned |
| 18 | **The Veil** | Dark Star | Perception | Distance and bearing lie here. High-risk per `HOLOSSEUM_REFERENCE.md` §3.5 — endgame/optional only | Planned (flagged) |
| 19 | **The Cradle** | Lost World | Boss stage | A strange, half-organic chamber; where the Riderless waits | Story |

Not carried forward as distinct Lists: the source's "X Cell" magma-corner variants (that's a hazard overlay our layout×hazard roll system already expresses — confirmed by `HOLOSSEUM_REFERENCE.md` §3.1), its lavatory/play-room novelty stages (wrong register for this world), and its practice stage (the hangar's future training mode, not a List).

## 3. Per-List design notes (planned set)

- **The Ancestors' Foundry / The Slagrun** are a pair: Foundry teaches belts safely; Slagrun weaponizes them with a fatal edge. Slagrun is where Sweep/Unhorse tempers and Fetter effects (`ARMORY_REFERENCE.md` §2.6) become build-around tools — port the source's lesson that immobilization near a hazard is the real kill condition.
- **Keep of the Rust Cross** should be the first List with real doctrine identity: sprawling wall maze rewarding Mangonel/Steeplefall arcs and Vigil traps, punishing straight-line Bombard sightlines.
- **The Closing Vein** plugs into the hazard-roll slot as a time-scaled radius — the battle-royale pacing lever, cheap to build against the existing lava-DoT code.
- **The Restless Vault / The Orrery** are the two "learn the machine's rhythm" Lists; each needs exactly one moving system (per `HOLOSSEUM_REFERENCE.md` §4.4) — oscillating blocks for the Vault, one rotating ring for the Orrery.
- **The Hushplain vs. The Close vs. The Longfield** are pure pacing levers with near-zero build cost — geometry-only variants that triple perceived arena variety early. Recommended as the first planned batch for exactly that reason.
- **The Veil** stays out of the standard roll pool whenever it's built: it degrades the camera legibility work of §11.5, so it must be opt-in (a challenge modifier or story beat), never a random roll.
- **The Cradle** ships only with the Riderless (see `SETTING_AND_FACTIONS.md` §Characters).

## 4. Fold into the run system

Current run rolls pick from the 4 built layouts with fight 1 hazard-free. As planned Lists land, adopt the full layout×hazard matrix the domain code already supports: layout roll (Tiltyard / Colonnade / Keep / Basin / …) × hazard roll (none / magma pools / ice / conveyor / closing vein), with landmark-bearing Lists (Keep, Orrery, Quay) exempt from hazard overlays so their identity stays clean. Fight-1-safe rule stays. Home-List flavor: when a rival's Order has a home List (Winterfield for the Winter Wing, the Drowned Quay for the Compact), bias — don't force — their fight toward it; it makes rivals feel *from somewhere* for one line of code.

## 5. Rollout order

1. **Geometry-only batch:** the Close, the Longfield, the Hushplain (three Lists for one afternoon of layout work).
2. **Landmark batch:** Keep of the Rust Cross, the Reliquary Garden, the Broken Colonnade variants.
3. **Hazard-mechanics batch:** the Ancestors' Foundry (belts exist as a hazard already — needs lane layout), the Slagrun (belts + fatal edge), the Closing Vein (time-scaled hazard radius).
4. **Moving-system batch:** the Restless Vault, the Orrery, the Drowned Quay.
5. **Terrain batch:** the Basin (sloped collider), the Sunder (split-floor system).
6. **Story/optional:** the Veil, the Cradle.
