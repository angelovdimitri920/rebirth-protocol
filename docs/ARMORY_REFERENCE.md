# The Armory — Canonical Parts Roster & Naming Reference v1

*This is the canonical naming, roster, and part-mechanics reference for Rebirth Protocol (2026-07-18). It supersedes the naming layer of [`WARBAND_THEME_REFERENCE.md`](WARBAND_THEME_REFERENCE.md) (historical-culture names) and builds directly on the archetype map in [`PARTS_AND_DAMAGE_REFERENCE.md`](PARTS_AND_DAMAGE_REFERENCE.md) — that doc identified the mechanical design space from the source material; this one names, themes, and stats it for the neo-feudal used-future world of [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md). Source basis: the user-supplied Parts FAQ (~150 parts), Weapon Damage Guide (damage formula + per-body multiplier tables), and controls/mechanics FAQ (stat-bar definitions, blast-type letters, downed-damage rule). Every name below is original; no source name or number is copied — everything is recalibrated to our own scale.*

---

## 1. Naming canon

### 1.1 The three tiers of parts

The world's Lost Technology premise (`SETTING_AND_FACTIONS.md`) gives us a natural three-tier catalog structure, which also maps cleanly onto the source material's normal / "illegal" / joke-tier split:

| Tier | In-fiction meaning | Source analogue | Game role |
|---|---|---|---|
| **Relic-pattern** | Ancestral parts maintained by the Orders for generations. Nobody can build one; everybody knows its pattern-name. | The normal legal roster | The standard hangar/draft catalog — everything in §4–§10 below |
| **Hushforged** | Parts touched by whatever caused the Long Hush — merged with something nobody understands. The Orders ban them; the banned things keep turning up. They glow *wrong*. | "Illegal" parts (the Rahu / Penumbra / Wyrm tier — "merged with an unknown living being") | Late-run rare drops and post-game unlocks: dramatically stronger, visibly corrupted, with a real drawback each. Future content, not in the current catalog |
| **Scrapwright** | New-made human imitations — the only parts anyone alive can actually *build*. Honest work; hopeless numbers. | The "Can" joke tier (Oil Can, Can gun/bomb/pod/legs) | Joke/challenge-run tier and NPC flavor. Codex's OilCan / GarbageBin / Binface rigged meshes slot here perfectly |

The scrapwright tier is quietly the most important lore beat in the catalog: **the gap between what humanity can make and what it inherited is the entire political order of the setting.** A scrapwright gun that fires at all is a life's masterwork, and it is still strictly worse than the worst relic in any Order armory.

### 1.2 Naming language rules

1. **No Earth-culture names.** The Long Hush erased the old world's nations from memory; a part named for a specific dead culture (Khopesh, Chu-Ko-Nu, Greek Fire, Numidian, Terracotta) breaks the fiction. This is the rule the Warband-era names violated and the reason for the §11 rename.
2. **Relic-pattern names are plain feudal armory words plus, at most, one epithet** — the vocabulary a surviving knightly culture would actually use: arbalest, bombard, pavise, misericorde, greaves. Terms from tournament culture (tilt, lists, unhorse), heraldry (fetterlock, trefoil, argent), liturgy (litany, vigil, censer, knell), and castle-craft (oubliette, palisade, mangonel) are all fair game — these are the institutions the survivors rebuilt.
3. **Slot vocabularies keep the catalog legible.** Each slot draws from its own register so a name alone hints at what a part is:
   - **Guns** — archery/siege/liturgy (Arbalest, Bombard, Litany)
   - **Melee** — knightly arms and votive words (Oathblade, Misericorde, Penitent Flail)
   - **Bombs** — siege charges and ritual objects (Censer, Oubliette, Anathema)
   - **Shields** — real historical shield types (Pavise, Targe, Kite Ward)
   - **Pods** — retainers, hounds, and falconry (Iron Squire, Kestrel, Alaunt)
   - **Legs** — always "<X> Greaves," where X is a mount, bird, or gait word (Courser, Gryphon, Thistledown)
   - **Bodies** — chassis pattern-names evoking their keeper Order (Bannerman, Cobalt Knight, Duskmantle)
4. **Hushforged names are hushed epithets** — spoken like something you shouldn't name directly: *the Grieving Lance*, *the Choir Unending*, *Mother-of-Rust*. Future content should follow that register.
5. **Scrapwright names are plain and a little self-deprecating** — *Tinker's Bow*, *Second-Best Blade*, *Honest Plate*. The humor is affectionate, not contemptuous.

### 1.3 In-fiction lexicon used throughout

- A war-mech is a **harness** (a knight's full plate armor was literally called "a harness").
- A duel arena is a **List**; the roster of them lives in [`ARENA_ROSTER.md`](ARENA_ROSTER.md).
- A roguelite run is a **Passage of Arms** (the real medieval term for holding ground against all comers).

---

## 2. Combat-frame directives (canon as of 2026-07-18)

These are standing design rules from the user, recorded here as canon. The first two are already built; the rest are directives for upcoming passes.

### 2.1 Arm pairings (built)
Right arm carries **gun XOR melee**; left arm carries **bomb XOR shield**. The four legal loadout shapes — gun/bomb, gun/shield, melee/bomb, melee/shield — are the game's four broad fighting doctrines, and content must keep all four viable: **melee and shield rosters stay at numeric parity with gun and bomb rosters** (10 guns ↔ 10 melee; 8 bombs ↔ 8 shields).

### 2.2 Air-dash cap (applied this pass)
**No body grants more than 2 air dashes.** Extra dashes come only from legs (`ExtraDashes`). The Duskmantle (wraith) dropped from 3 vanish dashes to 2 — its evasion identity now rests on the vanish i-frames and short cooldown-free commitment, not on raw dash count. Gryphon Greaves (+1) remain the way to build a 3-dash harness.

### 2.3 Shield toll (new mechanic — to implement)
Shields gain a **cooldown ("toll"), like bombs**: lowering the shield, or having it broken, starts its toll; it cannot be raised again until the toll passes. This kills turtle-flicker blocking and makes raising a shield a *decision* with the same weight as throwing a bomb. Numbers per shield in §8.

### 2.4 Shield raise behavior (new mechanic — to implement)
Each shield declares what raising it does to your movement:

| Behavior | Meaning |
|---|---|
| **Root** | Halts you in place (the current behavior — stays default for mid shields) |
| **March** | You can still walk at reduced speed while it's up (light shields only) |
| **Air-hold** | Raising it midair halts you and holds you hovering in place |
| **Air-drop** | Raising it midair slams you straight to the ground — a liability, or an aggressive plummet, depending on who's underneath |

### 2.5 Damage formula & stat model (confirmation, not change)
The source's formula — *weapon damage × attacker body offense × defender body defense* — is exactly what `PartsCatalog.ComputeStats` already does with `AtkMult`/`DefMult`. Its calibration rule stays canon: **offense multipliers cluster tight (±10%); defense multipliers spread wide (±25%+)** — body choice swings survivability more than damage. Downed robos stay **fully invulnerable** (our Stage-1 judgment call) rather than the source's 30%-damage rule; revisit only if juggle/oki play ever becomes a design goal.

### 2.6 Impact types ("tempers") — future system
The source's eleven letter-suffix blast variants collapse into six named **tempers**, a modifier any bomb (and eventually pod blasts and shield bashes) can carry independent of its flight shape:

| Temper | Effect on the victim |
|---|---|
| **Sunder** | Launched diagonally up and away (the default) |
| **Sweep** | Shoved hard sideways, low — into hazards, off conveyors |
| **Unhorse** | Always knocks down, regardless of remaining poise |
| **Fetter** | Stunned in place for a beat; no launch |
| **Pyre** | Launched straight up; the blast lingers as a burning column |
| **Hook** | Yanked *toward* the part's owner — melee setup |

Implementation shape per `PARTS_AND_DAMAGE_REFERENCE.md` §4.3: a shared `ImpactType` field consumed by the knockback/knockdown resolution, not per-part bespoke code.

---

## 3. Stat language

Display stats are 0.5–5.0 bars (the source's own presentation, which players read instantly), skinned in the world's vocabulary. Each bar maps onto real sim fields — the bars are *presentation*, the sim numbers stay the balance truth.

| Slot | Bars | Sim mapping |
|---|---|---|
| Body | **MIGHT / WARD / POISE / GAIT / WING** | AtkMult / DefMult / endurance+knockdown resistance / SpeedMult / dash+air profile |
| Gun | **MIGHT / BOLT / SEEK / CADENCE / REND** | Damage / ProjectileSpeed / HomingTurnRate / FireInterval / EnduranceDamage |
| Melee | **MIGHT / REACH / TEMPO / GRACE / REND** | Damage / HitRange / SwingActiveTime / recoveries / EnduranceDamage |
| Bomb | **MIGHT / LOFT / BREADTH / LINGER / REND** | Damage / flight speed / BlastRadius / blast duration / EnduranceDamage |
| Pod | **MIGHT / HASTE / SEEK / BREADTH / LINGER** | Damage / FireInterval+energy / homing / blast size / uptime |
| Shield | **GUARD / SOAK / MEND / TOLL / RIPOSTE** | FrontBlockPercent / ShieldHp / RegenPerSec / cooldown / MeleeParryEnduranceDamage + bash |
| Legs | trait card | SpeedMult / JumpMult / ExtraDashes / LandRecoveryMult / (new) TurnMult, FallSpeedMult |

---

## 4. Bodies — 7 chassis patterns

Each chassis pattern is kept by one Order (`SETTING_AND_FACTIONS.md` faction table). `.id`s and built geometry unchanged.

| `.id` | Pattern name | Keeper Order | Identity | Status |
|---|---|---|---|---|
| `vanguard` | **Bannerman** | The Aureate Legion | The standard every other pattern is measured against. 2 dashes, no edges. | Built |
| `skylance` | **Vesper** | Order of the Winter Wing | Glass-cannon flier; one long committed dash. Named for the evening star — seen at dusk, gone by dark. | Built |
| `wraith` | **Duskmantle** | The Umbral Concordat | Evader; short vanish-dashes that phase through shots (now 2, per §2.2). The cowl-and-cape silhouette is literal. | Built |
| `bulwark` | **Cobalt Knight** | The Rust Cross Commandery | Slow tank; the one finished rigged asset, now name-aligned with it. | Built (rigged) |
| `halcyon` | **Sunplume** | The Solarian Talon | Multiple short continuous jumps — never quite touches the ground. | Planned |
| `corsair` | **Freelance** | The Drowned Compact | The hybrid — literally a "free lance," the mercenary chassis for pilots who won't commit to one doctrine. | Planned |
| `juggernaut` | **Skyanvil** | The Kurultai Vanguard | Heavy *and* airborne — a tank that contests the sky. The name is the mechanical identity. | Planned |

Proposed stat spread for the three planned chassis (calibrated to the built four — HpMult 0.8–1.45, DefMult 0.75–1.2, AtkMult 0.9–1.25, per §2.5's tight-offense/wide-defense rule):

| Pattern | HpMult | DefMult | AtkMult | Dash | SpeedMult | Notes |
|---|---|---|---|---|---|---|
| Sunplume | 0.85 | 1.15 | 0.95 | Normal ×2 (short, quick-recharge) | 1.0 | Needs a multi-jump capability (see §12) |
| Freelance | 1.0 | 1.05 | 1.05 | Normal ×2 | 1.05 | Deliberately almost-Bannerman with a lean toward speed |
| Skyanvil | 1.3 | 0.85 | 1.05 | Long ×1 | 0.8 | High WING despite weight: strong hover, cheap air-dash |

---

## 5. Guns — 10 relic patterns

Three built, seven planned (families per `PARTS_AND_DAMAGE_REFERENCE.md` §3). Sim proposals calibrated to the built range (Damage 14–90, FireInterval 0.13–1.15, speed 26–36, homing 0.6–3.4).

| `.id` | Name | Family | Identity | Proposed sim | Status |
|---|---|---|---|---|---|
| `blaster` | **Arbalest** | Baseline | The workhorse. Honest damage, honest tracking. | (as built: 35 dmg / 0.38 int) | Built |
| `needler` | **Litany** | Rapid stream | A recited pressure of weak, hard-curving darts — death by repetition. | (as built: 14 / 0.13) | Built |
| `ram-cannon` | **Bombard** | Heavy single | Slow, straight, brutal siege-shot. One hit shreds poise. | (as built: 90 / 1.15) | Built |
| `trident` | **Trefoil** | Spread | Three simultaneous streams in a heraldic three-lobe fan. Forgiving of aim, hard to dodge laterally. | 3×16 dmg, int 0.5, spread 18° | Planned |
| `arcjet` | **Mangonel** | Arcing anti-cover | Lobs shot over walls onto hiders — the siege engine's whole job. | 40 dmg, int 0.9, ballistic arc, no homing | Planned |
| `snare-beam` | **Vigil** | Delayed trap | Rounds hang in the air, near-invisible, keeping watch — then strike. Rewards prediction, brutal on whiff. | 55 dmg, int 0.75, 0.8 s hang | Planned |
| `undertow` | **Grapnel** | Pull/disorient | Barbed shot that yanks the target off their aim line — and toward you. | 18 dmg, pull 4 m/s × 0.4 s | Planned |
| `farlight` | **Pilgrim** | Range-scaling | The round grows stronger the farther it travels. Weak in someone's face; devastating from across the List. | 20→70 dmg over 4→24 m | Planned |
| `jolt-caster` | **Fetterlock** | Reliable stun | Short-ranged, wide, near-guaranteed knockdown — named for the heraldic shackle. | 25 dmg / 40 REND, max range 8 m, 1.2 s fetter | Planned |
| `locust-swarm` | **Rookery** | Homing swarm | Releases a flock of slow, circling, stacking seekers. Constant pressure, never a clean single hit. | 5×9 dmg, homing 4.0, int 1.0 | Planned |

---

## 6. Melee — 10 relic patterns (parity with guns)

Each melee pattern is a deliberate counterpart to a gun family (§2.1's four-doctrine rule: a melee main should have an answer wherever a gun main has one). Three built, seven new. Calibration range from the built three: Damage 85–210, HitRange 2.6–3.4, WhiffRecovery 0.6–1.4.

| `.id` | Name | Gun counterpart | Identity | Proposed sim | Status |
|---|---|---|---|---|---|
| `saber` | **Oathblade** | Arbalest | The knight's standard. Balanced in every line. | (as built: 130 dmg) | Built |
| `twin-fang` | **Misericorde** | Litany | The mercy-dagger: fast, light, barely punishable. Finishes what poise-loss starts. | (as built: 85 dmg) | Built |
| `warhammer` | **Dolorous Maul** | Bombard | The dolorous stroke: enormous damage and knockback, ruinous to whiff. | (as built: 210 dmg) | Built |
| `longglaive` | **Longglaive** | Trefoil | Wide crescent sweep — hits a whole arc, not a line. The anti-strafe swing. | 110 dmg, range 3.6, arc 140° | New |
| `estoc` | **Estoc** | Mangonel | The armor-piercer: narrow thrust that ignores most of a raised shield's GUARD, as Mangonel ignores walls. | 95 dmg, arc 30°, pierces 60% of block | New |
| `flail` | **Penitent Flail** | Vigil | The arc is unreadable and the timing is late — the threat that lands after you thought it missed. | 140 dmg, delayed active window (0.25 s) | New |
| `hookbill` | **Hookbill** | Grapnel | The billhook that dragged knights off horses: a landed hit hauls the target into your face. | 75 dmg, pulls target to melee range | New |
| `tilt-lance` | **Tilt Lance** | Pilgrim | The joust: damage scales with the distance of the closing lunge. Ridden from across the List, it is the hardest single hit in the armory. | 60→190 dmg by lunge distance, arc 40° | New |
| `knell-maul` | **Knell Maul** | Fetterlock | A bell-hammer that barely bruises hull but tolls straight through poise — the stagger specialist. | 70 dmg / 130 REND | New |
| `iron-rosary` | **Iron Rosary** | Rookery | A weighted chain told like beads: low damage per bead, the longest combo string in the armory. | 4-hit string, 55 dmg each, 0.2 s hit-recovery | New |

---

## 7. Bombs — 8 relic patterns

Two built, six new (families per `PARTS_AND_DAMAGE_REFERENCE.md` §4, plus two the source's reticule-tuning note anticipated). Calibration: Damage 80–120, Cooldown 5–9 s, BlastRadius 3.2–4.5.

| `.id` | Name | Family | Identity | Proposed sim | Status |
|---|---|---|---|---|---|
| `impact` | **Censer** | Standard lob | The swung vessel of fire. Tracks the enemy; hold to aim. | (as built: 80 dmg / 5 s) | Built |
| `quake` | **Anathema Charge** | Heavy close AoE | The great condemnation: huge blast fixed just ahead of you, long rearm. | (as built: 120 / 9 s) | Built |
| `barricade` | **Palisade** | Cluster denial | Three grouped blasts in a stake-wall line. Doesn't need to land clean — needs them *near* it. | 3×45 dmg, cd 7, r 2.2 each | New |
| `sidewinder` | **Oxbow Charge** | Curving | Bends around cover in a long river-meander arc. | 70 dmg, cd 5.5, curved path | New |
| `sapper` | **Oubliette Mine** | Planted trap | The forgotten pit: lands, waits near-invisible, remembers. | 85 dmg, cd 6, 12 s dwell | New |
| `cryo` | **Rime Charge** | Immobilize | Almost no damage; fetters the target for the follow-up. A setup tool, never a finisher. | 15 dmg, cd 6.5, 1.6 s fetter | New |
| `steeplefall` | **Steeplefall** | Vertical drop | Climbs past steeple height, drops nearly straight down — cover means nothing. | 90 dmg, cd 6, ArcHeight ~14 | New |
| `pincer` | **Pincer Charge** | Twin split | Splits to blast both sides of the target at once. Standing still is the wrong answer; moving is also the wrong answer. | 2×40 dmg, cd 6 | New |

---

## 8. Shields — 8 relic patterns (parity with bombs)

Two built, six new. Every shield now carries a **TOLL** (§2.3) and a **raise behavior** (§2.4). Calibration: ShieldHp 110–420, front block 55–95%.

| `.id` | Name | Identity | GUARD front/back | SOAK | MEND | TOLL | Raise | Status |
|---|---|---|---|---|---|---|---|---|
| `aegis` | **Ward Veil** | Light energy veil; the flier's shield. | 75% / 25% | 180 | 25/s | 2.5 s | Air-hold | Built (add toll) |
| `bastion` | **Pavise** | The great standing wall-shield. | 92% / 40% | 260 | 6/s | 6 s | Root + Air-drop | Built (add toll) |
| `targe` | **Targe** | Small and quick: the only shield you can walk behind. | 60% / 15% | 110 | 30/s | 1.5 s | March (40% speed) | New |
| `kite-ward` | **Kite Ward** | The knight's standard shield; balanced in every line. | 80% / 30% | 200 | 14/s | 3.5 s | Root | New |
| `argent-mirror` | **Argent Mirror** | Timed art: the first instant of the raise *reflects* projectiles. Late, it's a mediocre wall. | 70% / 20% (reflect window 0.25 s) | 140 | 18/s | 5 s | Root | New |
| `bastille` | **Bastille** | The prison door swings both ways: the bash specialist — its RIPOSTE is an attack, not a deterrent. | 85% / 35% | 220 | 10/s | 5 s | Root + Air-drop | New |
| `cenotaph` | **Cenotaph** | The tomb-marker: a vast pool that does not mend. Broken, it is gone for the fight — refilled between Lists. | 95% / 50% | 420 | 0 (per-fight) | — | Root + Air-drop | New |
| `pallium` | **Pallium** | The mantle: modest ahead, uniquely strong *behind* — the kiter's cloak against pursuit. | 55% / 55% | 160 | 20/s | 3 s | Air-hold | New |

---

## 9. Pods — 6 relic patterns

Pods are retainers: squires, hounds, hawks, and wards that fight beside a harness on their own energy. Two built, four new.

| `.id` | Name | Family | Identity | Status |
|---|---|---|---|---|
| `sentry` | **Iron Squire** | Steady fire | The loyal retainer: steady chip fire while you reposition. | Built |
| `hornet` | **Kestrel** | Burst | The cast hawk: fast stooping bursts, then an empty glove. | Built |
| `alaunt` | **Alaunt** | Roaming hunter | The war-hound: roams the List, rushes and detonates when it winds the quarry. | New |
| `gargoyle` | **Gargoyle** | Stationary ambush | Perches inert; the first thing to cross its ward gets fettered. | New |
| `roodscreen` | **Roodscreen** | Zone denial | Projects a standing screen that must be flown around, not through. | New |
| `herald` | **Herald** | Mark/support | Circles the foe crying their position: marked targets take +10% damage while it flies. | New |

---

## 10. Legs — 6 relic patterns

Always Greaves. Three built, three new (needs two new sim fields: `TurnMult`, `FallSpeedMult` — §12).

| `.id` | Name | Identity | Proposed sim | Status |
|---|---|---|---|---|
| `strider` | **Wayfarer Greaves** | Neutral gait. Nothing gained, nothing owed. | (as built) | Built |
| `cheetah` | **Courser Greaves** | The running horse: ground speed up, jump suffers. | (as built) | Built |
| `cricket` | **Gryphon Greaves** | The sky rig: +1 air dash, clean landings, sluggish on foot. The only path past the 2-dash cap (§2.2). | (as built) | Built |
| `heron` | **Heron Greaves** | The wading bird: much higher jump apex, everything else honest. | speed 0.95, jump 1.45 | New |
| `destrier` | **Destrier Greaves** | The battle-trained mount: pivots sharply where others carve wide. | turn ×1.6, landRec 0.9 | New |
| `thistledown` | **Thistledown Greaves** | Falls like seed-fluff: slow descent, long hang-time for air-fired weapons. | fall ×0.6, landRec 0.85 | New |

---

## 11. Renames applied to the built catalog (2026-07-18)

Display `Name`/`Blurb` only; every `.id` untouched (save compatibility). Applied in `Assets/RebirthProtocol/Runtime/Domain/Parts.cs`.

| `.id` | Old (Warband) | New (canonical) |
|---|---|---|
| `vanguard` | Legionnaire | **Bannerman** |
| `skylance` | Valkyrie | **Vesper** |
| `wraith` | Shinobi | **Duskmantle** |
| `bulwark` | Crusader Knight | **Cobalt Knight** |
| `blaster` | Longbow | **Arbalest** |
| `needler` | Chu-Ko-Nu | **Litany** |
| `ram-cannon` | Ballista | **Bombard** |
| `saber` | Saber | **Oathblade** |
| `warhammer` | Warhammer | **Dolorous Maul** |
| `twin-fang` | Khopesh | **Misericorde** |
| `impact` | Greek Fire Pot | **Censer** |
| `quake` | Zhen Tian Lei | **Anathema Charge** |
| `aegis` | Aegis Barrier | **Ward Veil** |
| `bastion` | Bastion Plate | **Pavise** |
| `sentry` | Terracotta Sentinel | **Iron Squire** |
| `hornet` | War Kite | **Kestrel** |
| `strider` | Traveler's Boots | **Wayfarer Greaves** |
| `cheetah` | Numidian Boots | **Courser Greaves** |
| `cricket` | Winged Sandals | **Gryphon Greaves** |

The run-layer catalog (`RunCatalog.cs`) got the same treatment — boons read as rites and marks of favor, items as relic trinkets (Scrap Plating kept: it was already perfect used-future). The five rival pilots keep their names; their characterization deepens in `SETTING_AND_FACTIONS.md`.

---

## 12. New sim capabilities the planned roster requires

Each is one bounded system; parts above reference them. This is the implementation backlog, roughly ordered by how many parts each unlocks:

1. **Multi-projectile volley** (`ProjectileCount` + `SpreadDegrees`) — Trefoil, Rookery, Palisade, Pincer Charge.
2. **Shield toll + raise behavior** (§2.3–2.4 fields on `ShieldPart`; toll UI like the bomb's) — all 8 shields.
3. **Status: Fetter** (brief immobilize, distinct from knockdown) — Fetterlock, Rime Charge, Gargoyle; prerequisite for tempers (§2.6).
4. **Ballistic/arcing gun trajectory** — Mangonel (bombs already arc; guns don't).
5. **Delayed/hanging projectiles** — Vigil; Penitent Flail's late active window is the melee twin.
6. **Pull forces** (on-hit velocity toward owner) — Grapnel, Hookbill; later the Hook temper.
7. **Range/charge damage scaling** — Pilgrim (distance flown), Tilt Lance (lunge distance).
8. **Mine dwell + curved bomb paths** — Oubliette, Oxbow.
9. **Guard-piercing melee** — Estoc (fraction of block ignored).
10. **Extended combo strings** (per-weapon string length > 3) — Iron Rosary.
11. **Roaming/ambush pod AI + zone denial + target mark** — Alaunt, Gargoyle, Roodscreen, Herald.
12. **Legs fields** `TurnMult`, `FallSpeedMult`; **multi-jump body capability** — Destrier, Thistledown; Sunplume.
13. **Tempers** (`ImpactType` on bombs) — the §2.6 system, after Fetter exists.

---

## 13. Rollout plan (one pass per task, per CLAUDE.md's small-increments rule)

- **Pass A (done, this session):** naming canon, full roster design, display renames, air-dash cap.
- **Pass B:** shield toll + raise behavior on the two built shields; Targe + Kite Ward (no new capabilities beyond #2).
- **Pass C:** multi-projectile volley → Trefoil gun + Palisade bomb; Longglaive (pure numbers, no new capability).
- **Pass D:** Fetter status → Fetterlock, Rime Charge; then Knell Maul (numbers only).
- **Pass E:** pulls + guard-piercing → Grapnel, Hookbill, Estoc.
- **Pass F:** scaling damage → Pilgrim, Tilt Lance; delayed threats → Vigil, Penitent Flail.
- **Pass G:** remaining bombs/shields (Oxbow, Oubliette, Steeplefall, Pincer; Argent Mirror, Bastille, Cenotaph, Pallium), Iron Rosary, Rookery.
- **Pass H:** pod AI wave (Alaunt, Gargoyle, Roodscreen, Herald); legs wave (Heron, Destrier, Thistledown).
- **Pass I:** the three planned chassis (Sunplume needs multi-jump; Freelance is numbers-only and can ship any time a mesh exists; Skyanvil).
- **Pass J:** tempers; then the Hushforged tier design pass (needs its own doc — drawback design is the hard part).

Each pass ends with EditMode tests for new domain logic and a built-player smoke check, per the standing verification rules.
