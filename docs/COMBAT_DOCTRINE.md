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

### 4.3 The overload rule [source, to adopt]
When a harness is knocked down, **its gun rounds still in flight are wiped** — a "system overload." Consequences the source demonstrates: slow buildup weapons (Evenfall, Thornswarm, Alembic) are *counter-punishable* by downing the wielder before the volley lands; guaranteed-knockdown tempers (Unhorse) do **not** trigger overload — only gunfire does, so K-bombs down without clearing the sky. This asymmetry is deliberate and worth porting exactly. **Scrapwright exception** [directed by tier design]: Matchlock rounds survive their wielder's knockdown — dependability made mechanical.
**[divergence]** Our sim doesn't yet wipe projectiles on knockdown; adopt with the rule above.

### 4.4 Aimed bombs [built + source nuance]
Holding the bomb opens the reticule and roots you (air: halts you) — you are a sitting target while aiming [built]. Source nuances to keep: different bombs *want* different aim times (Palisade demands placement; Censer barely needs any); a skilled hand aims in half a second; aiming is also *defense* (placing a blast on your own retreat line).

### 4.5 Charge attacks [source, to build — per-garniture]
Every garniture lists its charge (`ARMORY_REFERENCE.md` §3): a committed body-strike with **i-frames during, vulnerability before and after**. The four charge kinds: **Attack** (straight/diagonal strikes), **Air** (rising strikes that contest the sky), **Movement** (wall-clearing repositions), **Evasion** (drifts and feints). Charges answer point-blank pressure, punish downed-adjacent positioning, and give every body a no-ammo threat. Ground-only [source]; ours may relax this for Skyanvil-class charges later.

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

Near-term implementation order (slots into `ARMORY_REFERENCE.md` §12's passes): overload rule → charge attacks (garniture data exists) → the Casting opening → mercy rule + laurels with the run layer → team modes last (they need AI archetypes 1–7 first).
