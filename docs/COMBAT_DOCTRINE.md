# Combat Doctrine — How Battles Work in the Lists v1

*Canonical combat-design reference (2026-07-18), distilled from the source material's mechanics/strategy texts (controls & tips FAQ, Grand Battle tournament guide) and refit to our world. This is the design contract for future combat passes: what is already built, what is directed, and what the source teaches about why each rule matters. Companions: [`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md) (parts), [`ARENA_ROSTER.md`](ARENA_ROSTER.md) (Lists), `GAME_DESIGN.md` (implementation log).*

Status tags: **[built]** in the Unity sim today · **[directed]** user-directed canon, to implement · **[source]** source-material rule recorded for adoption/adaptation · **[divergence]** where we deliberately differ.

---

## 1. The shape of a duel

A duel in the Lists runs: **the Casting → neutral → poise break → knockdown → rebirth → repeat until a harness falls.**

- **The Casting** [source, to design]: fights open with both harnesses cast into the List as sealed relic-cores by the Herald. The core tumbles, settles, and *unfurls* — and the first to unfurl moves first. Mashing speeds your unfurling; the aim of the cast steers where you land. This replaces "robos fade in at spawn points" with a ritual opening that is also a real minigame (first-move advantage, landing position). The source's advice — a fast unfurl into an immediate aimed attack can steal the first knockdown — is the intended skill expression.
- **Neutral** [built]: movement, boost economy, cover, lock-on, pod pressure.
- **Poise break → knockdown → rebirth** [built]: endurance/knockdown/rebirth-invincibility loop (`GAME_DESIGN.md` §2.2), the game's title mechanic.

## 2. The four ranges [source, adopted]

Combat texts should always speak in these bands, and weapon entries reference them:
- **Point-blank** — touching distance. Gauntlet/Cestus range.
- **Short** — Culverin range: a dash away.
- **Medium** — Pilgrim range: most of the fight happens here.
- **Long** — the full List. Longshrift, Firecrest, and swarm weapons live here.

Range identity is a balance axis, not flavor: short-range weapons carry the game's highest per-hit numbers *because* closing is paid for in risk.

## 3. Movement doctrine

- **Circle strafing** [built, teachable]: orbiting a locked foe defeats straight fast bolts (they can't home hard enough to lead the arc). The AI must both use and be beaten by it.
- **The slide shot** [built]: firing while moving carries momentum — you slide through the volley instead of stopping. Ending a slide behind cover as the last round leaves is textbook form.
- **Short-jump firing** [source, to adopt]: a tapped hop + fire lets a harness shoot from above without committing to a full jump's vulnerability. Jump height already scales with button hold [built]; the hop-fire idiom needs tuning attention when arcing weapons land.
- **Landing is the vulnerable moment** [built]: landing recovery scales with boost spent; the whole aerial game orbits around baiting and punishing landings. Never add a mechanic that removes landing risk wholesale.
- **Standing start is slow** [built]: acceleration from rest is deliberately laggy — stopping is a decision, not a default.
- **Corners kill** [source, adopted as arena rule]: a cornered harness eats pod-and-bomb crossfire with no exit. Arena design keeps corner escapes expensive but real; AI should herd toward corners.
- **Low air dash** [built]: dashing immediately off the ground covers wall-to-wall gaps without surrendering cover height.

## 4. Offense doctrine

### 4.1 Wait first, strike later [source, core rhythm]
Every gun leaves its wielder briefly rooted after firing [built: fire-slide/air-halt]. The core exchange of the game is: bait the foe's volley, then answer during their recovery. The source states it plainly — fire *after* they fire. All AI archetypes must respect and exploit this rhythm.

### 4.2 Combos [source, adopted]
The canonical kill pattern: **displace, then punish** — a bomb or pod temper (Hoist/Sweep/Fetter) moves or holds the victim, the gun or blade collects. Aimed bombs + simultaneous gun fire ("two shots that look like one") is advanced form. Tempers exist to make displacement composable (`ARMORY_REFERENCE.md` §2.5).

### 4.3 The overload rule [source, adopted]
When a harness is knocked down, **its gun rounds still in flight are wiped** — a "system overload." Consequences the source demonstrates: slow buildup weapons (Evenfall, Thornswarm, Alembic) are *counter-punishable* by downing the wielder before the volley lands; guaranteed-knockdown tempers (Unhorse) do **not** trigger overload — only gunfire does, so K-bombs down without clearing the sky. This asymmetry is deliberate and worth porting exactly. **Scrapwright exception** [directed by tier design]: Matchlock rounds survive their wielder's knockdown — dependability made mechanical.
**[built 2026-07-19, Pass B2]** Entering knockdown from any cause — endurance break or guard break, whatever the damage source — wipes the downed pilot's own gun rounds (`HitSource.Gun`); bomb and pod shots stay live, and the opponent's sky is untouched. `GunPart.SurvivesKnockdown` carries the scrapwright exemption; no built gun sets it until Matchlock lands (Pass P). The Unhorse asymmetry needs no code today: only what is *wiped* is filtered (gunfire), not what *causes* the fall — a K-bomb downing you still clears your gun rounds but never its own kind, which is the ported behavior.

### 4.4 Aimed bombs [built + source nuance]
Holding the bomb opens the reticule and roots you (air: halts you) — you are a sitting target while aiming [built]. Source nuances to keep: different bombs *want* different aim times (Palisade demands placement; Censer barely needs any); a skilled hand aims in half a second; aiming is also *defense* (placing a blast on your own retreat line).

### 4.5 Charge attacks [built, Pass C — per-garniture]
Every garniture lists its charge (`ARMORY_REFERENCE.md` §3): a committed body-strike with **i-frames during, vulnerability before and after**. The four charge kinds: **Attack** (straight/diagonal strikes), **Air** (rising strikes that contest the sky), **Movement** (wall-clearing repositions), **Evasion** (drifts and feints). Charges answer point-blank pressure, punish downed-adjacent positioning, and give every body a no-ammo threat. Ground-only [source]; ours may relax this for Skyanvil-class charges later. Built for the four Field-garniture bodies (Attack/Air kinds only — Movement/Evasion arrive with the War/Chase garnitures, Pass M); `GAME_DESIGN.md` §30.

### 4.6 Pod placement [built + source doctrine]
Pods fire with **no vulnerability window** [source; ours matches] — they are the "free" action, and doctrine is to keep them always working: trap corners, seed retreat lines, wake them as landing cover. Triangle-set patterns (Gargoyle, Gonfalon Watch: three placements) reward deliberate spacing — too tight wastes overlap, too wide leaves lanes.

### 4.7 Melee and shield layers [built, ours]
Melee (gap-closer, strings, clash) and shields (directional block, parry, bash, toll) sit *on top of* the source's game — they are our differentiators and follow their own doctrine: melee's job is poise damage and punishing rooted moments (a foe mid-aim, mid-volley, post-charge); the shield's job is converting a read into tempo (block → riposte/bash → their toll of regret). The four loadout doctrines (gun/bomb, gun/shield, melee/bomb, melee/shield) each get an AI archetype.

## 5. Defense doctrine

- **Cover breaks homing and sightlines** [built]. Vault/through-wall weapons (Mangonel, Peal, Skysword) exist to tax turtling; their price is readable arcs.
- **Dodging homing rounds** [source, adopted]: slow homers are beaten by the *late* jump — let them commit, then move. Fast straight rounds are beaten by the circle. Teaching AI both dodges (and both failure modes) is a Stage goal.
- **Shield facing** [built]: block percentages are directional; getting behind a shield-bearer is the counter. Pallium exists to bend that rule.
- **Knockdown recovery** [built]: mash to rise faster.
- **Rebirth etiquette** [source observation, AI doctrine]: most fighters retreat during a foe's rebirth flare; predators like Roald the Close stalk it and strike the instant it gutters — the most hated (and legal) habit in the Lists. Both behaviors belong in the AI pool; punishing *during* invulnerability is wasted honor and ammunition.
- **Downed damage** [divergence, standing]: source reduces damage on downed targets to 30%; we keep **full invulnerability while down** (Stage-1 call, reaffirmed). Revisit only if juggle play becomes a goal.

## 6. Team and multi-harness battles [source, future modes]

Rules recorded for the Grand Passage modes (`SETTING_AND_FACTIONS.md`):
- **Shieldbrother (2v2)**: a partner's gun and collision cannot harm you; their **bombs and pods can** — friendly fire is selective, so blast placement stays a skill even among allies. Opposite-ends positioning doctrine: stand across the List from your partner so fire aimed at them never clips you, and you always hold ambush angles. Both allies' remaining vigor scores.
- **Twin Harness (tag)**: two loadouts, switch on the lock-on button; a benched harness below 150 vigor slowly mends back to 150. AI switches on thresholds (first at ~600, then per ~200 lost). Both harnesses' vigor counts for laurels.
- **Ordeal (1v2)**: the un-targeted foe fights timidly; your current target fights in earnest — target-switching is the real weapon. (Also exactly how our AI aggression should scale in any multi-fight.)
- **Melee (1v3 free-for-all)**: all four fight; aim discipline and third-party opportunism decide it. The source's counsel: pick your victim, ignore the rest, use the whole List.

## 7. The mercy rule [source, adopt for accessibility]
Repeated defeat against one foe unlocks lowering their vigor — 75%, then 50%, then 25% — at a laurel cost. This is the source's own difficulty valve and fits the fiction (the Herald may weaken a champion's charge to let a Passage proceed). Adopt as the run's retry accessibility option.

## 8. Laurels — honor scoring [source, adopt with the run]

- Score = **speed of victory + remaining vigor** (both harnesses' vigor in team modes), against a posted **task score** per event; thresholds award **bronze / silver / gold laurels**.
- **−10% per defeat-and-rematch** on that fight.
- **Hushforged penalty**: fielding even one Hushforged part **halves the fight's laurels** — victory, but dishonored. (The economy that makes the banned tier a real choice.)
- Laurels gate the circuit's crown events (Bronze Ordeal → Silver Melee → Golden Passage) and feed armiger **Class ranks** (C through S — Class S are the Paragons of the Passage).

## 9. AI design notes [source observations → build targets]

The tournament guide is effectively an AI-archetype catalog; these patterns are the build list:
1. **The Bookman** (Cassian): fights the textbook — waits, punishes recovery, never overextends.
2. **The Spammer**: anchors in place layering slow volleys (Evenfall/Thornswarm types); loses to overload pressure — must actually stop firing when downed.
3. **The Point-Blank Predator** (Roald): relentless approach, punishes downed and rebirthing foes, X-charges at arm's length; loses to Fetter zones and range discipline.
4. **The Skirmisher**: fast harness, strikes on your landings, disengages on your terms being met.
5. **The Turtle**: wall-hugger behind shield/cover; vault weapons and Estoc exist for them.
6. **The Kiter-Flier**: circles at altitude on bombing runs; loses corners, wins open sky.
7. **The Trapper**: seeds Oubliettes/Gargoyles and herds you across them.
8. **The Titan-Fool** (Brother Golias): all-Goliath meme build — deliberately keep one absurd-but-legal archetype; the Lists have jesters too.
AI must visibly **use** doctrine (circle-strafe, slide-shot, corner-herd, temper-combo) *and* be beatable through its named blind spot. One blind spot per archetype, always.

## 10. What this doc feeds

Near-term implementation order (slots into `ARMORY_REFERENCE.md` §12's passes): overload rule → charge attacks (garniture data exists) → the Casting opening → mercy rule + laurels with the run layer → team modes last (they need the AI framework of §12 first).

## 11. Controls — implemented mapping and source-verb coverage

The canonical controller layout **[built]** (keyboard is a 1:1 mirror; controller feel still needs a physical-pad playtest, per the standing environment constraint):

| Input | Action | Keyboard |
|---|---|---|
| Left stick | Move / steer aim while holding pod or bomb | WASD |
| A | Jump/hover · mash to recover · **double-tap airborne = air dash** · (planned) mash to unfurl at the Casting | Space |
| X | Dash airborne · **grounded X = the garniture's charge attack** *(built, Pass C — the lock gate is moot in a duel: lock is always on, so ground dash retires from X while locked, exactly the source's trade)* | Shift |
| RT | Right arm: gun (held) / melee (pressed) | J |
| LT | Left arm: bomb (hold to aim, release to throw) / shield (held) | Q |
| B | Pod: deploy/recall; hold + stick steers launch heading | E |
| Y | Lock-on / switch targets (real in Ordeal/Melee/Shieldbrother modes) | L |
| RB | Lock-on / switch targets (Y duplicate — the thumb never leaves the right stick) | — |
| LB | X duplicate (dash aloft, charge afoot) — kept as a mirror in Pass C; a *dedicated* charge trigger stays an option if grounded-X-as-charge doesn't survive playtest | — |
| Start | Pause menu (resume / customization / title / quit) | P |

**Source-verb coverage audit** — every control concept in the source FAQs, and where it lives here: joystick move ✓ · jump & air-dash ✓ (A, double-tap) · fire gun / use melee ✓ (RT, one trigger per the arm mutex) · fire bomb / use shield ✓ (LT) · fire pod ✓ (B, no-vulnerability rule kept) · switch targets ✓ (Y — a duel no-op until multi-foe modes) · **collision/charge** ✓ (grounded X, Pass C — ground-only per the source, so air X stays dash) · aimed bomb (hold + stick, sitting-duck rule) ✓ · slide shot ✓ (fire-while-moving momentum) · short-jump fire ✓ (tap-A + B — needs a tuning check when arcing weapons land) · jump height by hold ✓ · mash to rise ✓ · Casting mash [planned] · pause/help/rules screens [hub UI, planned with the shell].

## 12. Designing opponent AI — the layered framework

Three layers compose every opponent: **Doctrine** (an archetype from §9 — what it tries to do), **Class** (C/B/A/S — how well it executes), **Personality** (a rival's signature quirks). This is the build spec for all AI work.

### 12.1 The parameter set
Every archetype is expressed through the same tunable parameters, so Class scaling is uniform:

| Parameter | Meaning |
|---|---|
| Reaction delay | Time from a player action to the AI's answer |
| Aim lead error | Deliberate error in predicting movement |
| Band discipline | How strictly it keeps its weapons' preferred range band |
| Dodge-check rate | How often it rolls to answer incoming fire (late-jump vs. circle chosen by projectile type — §5) |
| Combo rate | Chance a displacement (temper hit) is followed by the punish |
| Overload awareness | Whether it stops committing volleys when at knockdown risk (§4.3) |
| Punish-downed propensity | 0 for most; high only for predator personalities |
| Rebirth etiquette | Retreat during the foe's flare vs. stalk it |
| Corner craft | Herds the player toward corners; avoids its own cornering |
| Temper usage | Picks bomb/pod tempers situationally (Sweep near hazards, Fetter before heavy volleys) |
| Target logic | Multi-foe: focus, switch triggers, passivity when untargeted (§6) |
| Mercy of the machine | A floor of deliberate imperfection — no Class is frame-perfect, ever |

### 12.2 Class scaling (armiger ranks C → S)
| Class | Feel | Reaction | Combos | Doctrine execution |
|---|---|---|---|---|
| **C** | Squires: telegraphs, forgets its pod | ~500 ms | Rare | One doctrine move at a time |
| **B** | Journeyed: keeps its band, uses cover | ~350 ms | Sometimes | Doctrine plus one habit |
| **A** | Circuit regulars: dodges by projectile type, punishes landings | ~250 ms | Often | Full doctrine, readable rhythm |
| **S** | The Paragons: feints, baits, adapts once mid-fight | ~180 ms | Reliable | Full doctrine plus a counter-adaptation (e.g., stops circling when you fit an Arcus) |

Class maps to the circuit ladder (`SETTING_AND_FACTIONS.md`): early events run C/B, the Golden Passage runs S with empowered harnesses (the run's existing ×(1+0.12·fight) power mult is the "empowered" lever — reuse it).

### 12.3 Composition rules
- **One blind spot per archetype, always** (§9) — Class raises execution, never removes the blind spot; S-Class merely makes you *earn* it.
- **Rivals = archetype + Class + signature-part bias + home List + personality quirk** (e.g., Roald: Predator doctrine, Class A, Petronel bias, stalk-rebirth etiquette, punishes downed foes — the one legal cruelty).
- **Observed source behaviors as acceptance tests**: the Spammer roots itself mid-volley and must be overloadable; the teleporter panic-vanishes when pressured from two threats at once (Duskmantle AI dumps both dashes under multi-threat); tag AI switches at its thresholds; untargeted Ordeal foes fight timidly; nobody attacks *through* a rebirth flare except stalk-etiquette personalities, who wait at arm's length for it to gutter.
- **AI never drafts boons** [built rule, kept]; enemy strength scales by loadout, Class, and the power mult.
- **Hushforged AI**: Choir and endgame only (`ARMORY_REFERENCE.md` §16).

## 13. The balance framework

Ten pillars, applied at design time and validated in simulation:

1. **Commitment buys damage.** Recovery, toll, range-band narrowness, and aim time are the currencies that purchase MIGHT. Nothing gets a headline number without a payment (`ARMORY_REFERENCE.md` §13 profiles are the ledger).
2. **Bodies swing survivability, not damage.** Offense mults cluster 95–105% (Hushforged excepted); ward mults spread 85–138%. Garnitures re-trim within the pattern, never past the class envelope.
3. **Volley truth.** Balance to *realistic connect rates*, not all-hit totals — Sparrowstorm's 540 is fiction against anything moving; its real number is a third of that. Sim harness measures actual landed damage per archetype matchup.
4. **Counterpart parity.** Each gun and its melee twin must clear comparable effectiveness *under their own doctrines* (Longshrift at long band ≈ Tilt Lance landing its lunge). Audit pairwise whenever either is tuned.
5. **Doctrine viability.** The four loadout shapes (gun/bomb, gun/shield, melee/bomb, melee/shield) and four schools each keep a top-tier build at all times. If a patch orphans a school, the patch is wrong.
6. **Tier economy.** Scrapwright ≈ 85% relic effectiveness + dependability perks (the floor is playable, never optimal); relic = 100%; Hushforged ≈ 115–130% minus a real drawback minus half laurels. The three tiers must *stay* ordered — a relic outclassed by a scrapwright part is a bug.
7. **Temper budget.** A temper is worth ~10–15% effective damage; Unhorse and Fetter are the expensive ones and pay in MIGHT or toll. Branded pays in REND.
8. **Pacing targets.** A duel between equal builds lands in **60–120 s** with **2–5 knockdowns**. REND tuning holds the knockdown count; MIGHT tuning holds the clock. Fights outside the band flag themselves in the sim harness.
9. **The degenerate watchlist** (checked every content pass): Fetter chains (rule: 2 s fetter-immunity after a fetter ends) · Yoke + Culverin grounding loops · Rearguard-kite forever (pod energy pacing is the leash) · Cenotaph turtling (chip + toll answer it) · Testudo cycling (the 8 s toll is load-bearing — never shorten it casually) · tag heal-floor abuse (the 150 floor never rises) · Ascension Charge escape loops (boost cost on use).
10. **Laurels are soft balance.** Anything legal-but-degenerate costs style before it costs errata: rematch decay, Hushforged halving, and task scores tuned so honor and optimality mostly point the same way.

**Validation harness** (to build alongside the AI framework): seeded AI-vs-AI batch runs across a matchup matrix — 4 schools × 4 schools × arena sample — reporting win rate, TTK, knockdown count. Flag any pairing outside 40–60% win rate or outside the pacing band. Deterministic seeds make regressions bisectable; this is the balance CI for every parts pass in `ARMORY_REFERENCE.md` §12.

## 14. The shape of a session

The battle system lives inside a **JRPG-shaped shell with a western soul** (structure directive in `GAME_DESIGN.md` §26): a hub city, named armigers to talk to, a circuit ladder to climb, and the Passage (roguelite run) as the battle loop entered through events. A typical session: arrive in the hub → talk and shop (Wrightsguild repairs, stewardship shops, rumors of caches) → customize in the hangar and prove it in the test yard → enter an event → fight its Passage under its rules → take laurels, spoils, and a story beat → repeat. Twenty to forty minutes per event; a full circuit tier per sitting for a long one.
