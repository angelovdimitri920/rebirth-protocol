# The Lists — Canonical Arena Roster v2

*Feature-complete revision (2026-07-18): every one of the source material's 35 stages now maps to a named List or a hazard overlay, each with design stats. Builds on [`HOLOSSEUM_REFERENCE.md`](HOLOSSEUM_REFERENCE.md) (design levers; its coined prototypes are adopted here) and the source Map FAQs. All names original.*

## 1. What the Lists are, in fiction

Duels between armigers are fought in **the Lists**: consecrated dueling grounds, most cleared inside ancestral ruins. Some are dead ground; some are *not* — belt-lines that never stopped, defense grids still cycling, machines walking routes their makers forgot. The Orders didn't build the hazards; they **ritualized** them. Fighting among the ancestors' still-turning wheels is the highest form of the art.

## 2. Layouts × overlays

Confirmed by the source's own structure (`HOLOSSEUM_REFERENCE.md` §3.1) and matching our built roll system: a fight = **layout roll × overlay roll**. The source's "Cell" stages (same walls + magma corners) become four named **overlays** rather than separate Lists:

| Overlay | Source basis | Effect |
|---|---|---|
| **Emberpools** | the "Cell" stages | Magma pools in the corners; DoT bypasses shields |
| **Rimefloor** | Ice and Snow / Frozen Field | Ice footing: momentum carries, steering is a slow correction |
| **Beltworks** | Checkmate Foundry lanes | Conveyor lanes cross the floor |
| **The Closing Vein** | Magma Ruins | A hazard ring that tightens over the fight — spacious early, knife-range late |

Landmark Lists (marked ◆) are exempt from overlays so their identity stays clean. Fight 1 of any Passage stays overlay-free [built rule].

## 3. The roster — 21 Lists

Size S/M/L/XL · Walls none/sparse/dense/maze · Motion = moving elements. Status: **built** (in `ArenaBuilder`), **planned**, **story**.

| # | List | Source basis | Size | Walls | Motion | Signature | Home | Status |
|---|---|---|---|---|---|---|---|---|
| 1 | **The Tiltyard** | Basic Arena | M | sparse | — | Supply-cask cover (destructible); the neutral standard | Circuit | Built (was Depot) |
| 2 | **The Squires' Yard** | Practice Stage | M | sparse | — | Training List: target frames, forgiving sightlines | Circuit | Planned (training mode) |
| 3 | **The Thornfield** | Diamond Fences | M | dense | — | Peculiar diamond fence-walls; easy to get cornered | — | Planned |
| 4 | **The Broken Colonnade** | High-Rise Plaza | M | dense | — | High columns vs. low triangular stubs — two heights of cover | Aureate Legion | Built (was Colonnade) |
| 5 | **Crevice Court** → **The Shattered Close** | Crevice Court | M | maze | — | Haphazard jagged walls; wild, unpredictable fights | — | Planned |
| 6 | **The Gallows Ell** | L Formation | M | dense | — | L-shaped walls; every corner is a decision | — | Planned |
| 7 | **The Ancestors' Foundry** ◆ | Checkmate Foundry | M | sparse | Belts | The factory nobody can stop; fight the belts and lose | Wrightsguild | Planned |
| 8 | **Keep of the Rust Cross** ◆ | Castle Keep | L | maze | — | Sprawling curtain walls | Rust Cross | Planned |
| 9 | **The Reliquary Citadel** ◆ | Castle Citadel | M | dense | — | One central shrine-wall that decides the fight ("hold the shrine") | Rust Cross | Planned |
| 10 | **The Reliquary Garden** ◆ | Flower Garden / Nature Park | M | sparse | Center bridge rises/falls | Overgrown shrine garden; the arch-bridge commands all | Solarian Talon | Planned |
| 11 | **The Basin** | Chinese Bowl | M | sparse | — | A tilted bowl: downhill is fast, knockback carries far | — | Planned |
| 12 | **The Cloisters** | Robo's Room | S | maze | — | Cramped monastery corridors; movement itself is the puzzle | — | Planned |
| 13 | **The Pilgrim Road** ◆ | Little Locomotive | M | sparse | A walking ancestral crawler | The great cargo-crawler still walks its route: moving cover, rideable | Circuit | Planned |
| 14 | **The Reliquary Round** ◆ | Merry-Go-Round | M | sparse | Rotating procession | Saint-figures rotate the ring; fire between them | Circuit | Planned |
| 15 | **Cinderfield** | Magma Hole | M | sparse | Sinking center | Center floor sinks to magma underfoot; one tower never sinks | — | Built |
| 16 | **The Slagrun / Double Slagrun** | Dead Line / Double Dead Line | M | sparse | Belts → melt | Belts feed the melt; a down near the edge is the kill | Kurultai | Planned |
| 17 | **The Restless Vault** | Panic Cubes / Panic Walls / Scramble Walls | M | dense | Oscillating grid | The defense grid still cycles; learn its rhythm or be crushed against it | — | Planned |
| 18 | **The Orrery** ◆ | Battle Gear Station | M | sparse | Rotating cog-platforms | The great astronomical machine; ride the wheels | Winter Wing | Planned |
| 19 | **The Drowned Quay** ◆ | Loading Dock | M | sparse | Suspended platform | The cargo platform never stopped its route | Drowned Compact | Planned |
| 20 | **The Sunder** | Impact Craters | L | sparse | Floor splits | The ground divides in two, then four; gaps can't be crossed | — | Planned |
| 21 | **The Close / The Longfield / The Hushplain** | Sudden Death / Gigantix Sprawl / No Man's Land | S / XL / L | sparse / sparse / **none** | — | The three pure pacing levers: smallest, largest, wall-less | — | Planned (geometry-only batch) |

Story/optional beyond the roll pool: **The Veil** (Dark Star — perception hazard; opt-in only, never randomly rolled, per the §11.5 camera-legibility concern) and **The Cradle** (Lost World — the half-organic boss chamber where the Riderless waits).

Winterfield (built) = the Tiltyard layout under a full-floor **Rimefloor** overlay and stands as the Winter Wing's snow List; it stays in rotation as-is.

## 4. Design notes

- **Source coverage is complete**: 35 source stages → 21 Lists + 4 overlays + 2 story stages. The "Cell" trio and the ice pair fold into overlays; the three Panic/Scramble stages are difficulty settings of the Restless Vault's one grid system; Basic Arena's hidden "frame-only" variant becomes the Squires' Yard training mode.
- **The Pilgrim Road and the Reliquary Round** rehabilitate the two "toy" stages by transposing scale: a child's train set becomes a walking ancestral freight-crawler; a carousel becomes a rotating procession of saint-figures. Same mechanics (moving rideable cover; rotating occluders), right register.
- **The Slagrun doctrine** (from the tournament guide): belt + melt-edge arenas make Fetter/Unhorse/Sweep tempers into kill conditions — a downed harness drifts. This is the List where displacement builds headline.
- **Corner rule** (`COMBAT_DOCTRINE.md` §3): every List keeps corner escapes expensive but real; the Thornfield is deliberately the worst offender and teaches the lesson.
- **Home Lists**: when a rival's Order has a home List, bias (don't force) their fight there. Circuit events (`SETTING_AND_FACTIONS.md`) name their venues from this table.

## 5. Rollout order

1. **Geometry-only batch**: the Close, the Longfield, the Hushplain, the Thornfield, the Gallows Ell (five Lists of pure layout work).
2. **Landmark batch**: Keep of the Rust Cross, the Reliquary Citadel, the Reliquary Garden, the Shattered Close, the Cloisters.
3. **Overlay formalization**: Emberpools / Rimefloor / Beltworks as true overlay rolls on any non-◆ layout; the Closing Vein as a time-scaled hazard.
4. **Hazard-mechanics batch**: the Ancestors' Foundry, the Slagrun(s), Cinderfield's sinking center.
5. **Moving-system batch**: the Restless Vault, the Orrery, the Drowned Quay, the Pilgrim Road, the Reliquary Round.
6. **Terrain batch**: the Basin (sloped collider), the Sunder (splitting floor).
7. **Story/optional**: the Veil, the Cradle; the Squires' Yard with a training mode.
