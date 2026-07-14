# Parts & Weapon Damage Design Reference v1

*Reference basis for expanding our part catalogs (`src/parts/parts.ts`) well beyond the current 2-4-per-slot roster. Sources: ThrawnFett's "Parts FAQ" (GameFAQs, v0.9) and Kamineko's "Weapon Damage Guide" (GameFAQs, v0.4) — player-written strategy guides for Custom Robo (GameCube) covering roughly 150 named parts across 5 slots and their numeric damage tables. The GameCube instruction manual was checked but its online listing is a PDF-download landing page with no extractable text; not used as a source here. A 3D-model-rip gallery of the original game's assets (Models Resource) was also checked for visual silhouette reference — it only exposed asset category counts (robo bodies, leg/gun/bomb/pod parts, Holosseums), no descriptions, so it's noted here as a pointer for future visual reference, not a content source. Inspiration for design space and balance shape, not a licensed asset or numbers source: every name below is original to us, and none of our numbers are copied — they're recalibrated to our own scale. This doc is planning only; nothing here is implemented yet.*

## 0. Scope and approach

The source material names something like 8 body classes (each with 3-6 tiered variants plus a "secret/illegal" bonus tier), ~50 guns, ~25 bombs (each with up to 11 selectable knockback-direction suffixes), ~25 pods, and ~12 leg types. Cataloging every one of those 1:1 would mean dozens of near-duplicate reskins (mirrored left/right variants, incremental tier-1/2/3 stat bumps on the same idea) — busywork that wouldn't actually widen the *design space* of our game. Instead, this doc identifies every genuinely distinct **mechanical archetype** the source material contains, gives each one an original name and a description in our own voice, and cross-references which of our *existing* parts (`Vanguard`, `Blaster`, `Impact Bomb`, etc.) already cover a given archetype versus which are net-new expansion targets. That's a comprehensive map of the design space, not a name-for-name port — closer in spirit to how `ROBOT_SHELL_DESIGN.md` already treats the source's 8 body classes as 4 archetypes-to-adopt-now plus 2 archetypes-to-keep-on-file.

Two source ideas are flagged as **future systems** rather than named parts, because they're structural, not catalog entries: a per-bomb selectable **impact type** (§4.3) and a **damage-reduction-while-downed** rule (§1.2). Both are real, valuable design ideas the source material demonstrates; neither requires new part names to prototype.

## 1. Damage philosophy

### 1.1 The formula already matches ours

The source's damage model is a simple multiplicative chain: *base weapon damage × attacker's body offense% × defender's body defense%*. That's exactly what `computeStats()` already does with `atkMult`/`defMult` per `BodyPart` (`src/parts/parts.ts`) — this is confirmation our existing approach is sound, not a change to make.

The source's per-body multiplier *spread* is useful calibration data even though we're not adopting its numbers directly: across the roster, offense multipliers cluster tightly (roughly 90%-110%, with one deliberate 150% outlier reserved for an ultra-rare unlock), while defense multipliers spread wider (roughly 85%-140% across the normal roster). In other words: **body choice should swing survivability more than it swings damage output** — a glass-cannon body earns its identity mainly by taking more damage, not by hitting dramatically harder. Our current spread (`defMult` 0.75-1.2, `atkMult` 0.9-1.25 across Vanguard/Skylance/Wraith/Bulwark) already follows this shape and doesn't need retuning; new bodies added later should keep offense multipliers closer together than defense multipliers.

### 1.2 Discovered mechanic worth adopting: damage reduction while downed

The source rule: **a knocked-down target only takes 30% of normal damage** until it recovers. This directly counters a "downed = free follow-up combo that kills instantly" spiral, without needing a separate invincibility window — it keeps knockdown punishing (still lost tempo, still can't act) while capping how much a single opening can snowball into a full stock loss. We don't currently have this in `Health.ts`. Worth prototyping later: reduce `takeHit()`'s damage (not endurance) by ~70% while `state === "knockdown"`.

### 1.3 Damage tiers, paraphrased from the source's numbers

Not reproduced as a table (that's the guide's own compiled data), but the shape is useful as a sanity check for new parts: single hits mostly land in a **20-150** range depending on weapon commitment (fast/spammy hits low, slow/heavy hits high); a handful of extreme multi-projectile "spam" weapons can post 300-500+ *if every projectile connects*, which in practice almost never happens against a moving target — the headline total isn't the realistic average. Bomb damage in the source doesn't fall off with range at all (unlike guns, which the source splits into melee/close/medium/far bands) — bombs hit the same regardless of throw distance. Our simpler model (flat gun damage, no range falloff) already matches the bomb side of this; we don't currently vary gun damage by range either, which is a reasonable simplification to keep rather than adopt the added complexity.

## 2. Bodies

Our four existing archetypes (`Vanguard`/`Skylance`/`Wraith`/`Bulwark`) already map to the source's four most load-bearing classes per `ROBOT_SHELL_DESIGN.md` §2 (balanced all-rounder, long-dash glass-cannon flier, vanish-dash evader, slow tank). Three more classes from the source are distinct enough to be worth adding as a second wave, plus one structural idea:

| New archetype | Source basis | Identity |
|---|---|---|
| **Halcyon** | Aerial Beauty | Multiple *short* continuous jumps instead of one long dash — an air-mobility specialist distinct from Skylance's single-big-commitment dash. Reads as "never quite touches the ground." |
| **Corsair** | Trick Flyer | A hybrid between Vanguard and Skylance: more air mobility than the balanced body, less commitment/fragility than the glass-cannon flier. The "if you can't decide" pick. |
| **Juggernaut** | Funky Big Head | A second tank archetype that trades some of Bulwark's raw bulk for real air mobility — "heavy but not grounded," a tank that can still contest the air instead of being purely a ground brawler. |

**Future system, not a body:** the source reserves a small "illegal/secret" tier of drastically stronger bodies (one gets +50% outgoing damage and takes only 20% incoming damage, at the cost of a crippled charge attack) as unlockable prestige rewards, not something available from the start. This maps naturally onto a **meta-progression unlock** later (Stage 3+ territory) — an "Apex-class" chassis variant earned through run completions rather than picked in the hangar from day one — rather than a fifth hangar-visible body archetype. Flagging as a future system, not a Stage-1/2 catalog entry.

## 3. Guns

Ten distinct families cover the source's ~50 named guns. We already have three (`Blaster`, `Needler`, `Ram Cannon`); seven are new:

| Family | Source basis | Mechanical identity | Status |
|---|---|---|---|
| Baseline | Basic | No gimmick, honest numbers, the "learn on this" gun | **Have** — `Blaster` |
| Rapid weak stream | Gatling, Needle | Fast weak shots that reward sustained pressure over single big hits | **Have** — `Needler` |
| Slow heavy single-hit | Sniper, Dragon, Magnum | Big commitment per shot, punishing to whiff, rewards patience | **Have** — `Ram Cannon` |
| Multi-stream spread | 3-Way, Left/Right 5-Way, Arc, Pulse | Several simultaneous projectiles in a fan — hard to dodge, forgiving of imprecise aim | **New — `Trident`** |
| Vertical arc / anti-cover | Vertical, Twin Fang | Shots arc up and back down, clearing low walls and hitting robos hiding behind short cover | **New — `Arcjet`** |
| Delayed trap | Trap, Flare | Telegraphed, low-threat-looking shot that becomes dangerous after a beat — rewards prediction over reaction, brutal on whiff | **New — `Snare Beam`** |
| Pull / disorient | Claw, Drill | Doesn't just damage — yanks the target off their aim line, disrupting whatever they were about to fire | **New — `Undertow`** |
| Range-scaling | Flame | Damage increases the farther the shot travels — rewards keeping distance, weak in someone's face | **New — `Farlight`** |
| Reliable stun | Stun | Short range, wide hitbox, near-guaranteed knockdown in exchange for having to get close | **New — `Jolt Caster`** |
| Homing swarm | Hornet, Homing Star, Starshot | Multiple slow-homing projectiles that linger and stack, applying constant pressure rather than one clean hit | **New — `Locust Swarm`** |

## 4. Bombs

Six families beyond our existing two (`Impact Bomb`, `Quake Bomb`), plus a structural idea worth prototyping later.

### 4.1 New bomb archetypes

| Family | Source basis | Mechanical identity | Status |
|---|---|---|---|
| Standard tracking lob | Standard | Arcs toward the target, moderate everything | **Have** — `Impact Bomb` |
| Heavy slow AoE | Submarine | Big radius, big endurance damage, long rearm — the "one good throw wins the exchange" bomb | **Have** — `Quake Bomb` |
| Wide cluster / area denial | Wall, Delta, Grand Cross | Multiple explosions grouped or spread close together — doesn't need to land precisely, just needs the opponent standing anywhere in the cluster | **New — `Barricade Charge`** |
| Curving side-approach | Left/Right Flank, Left/Right Wave | Doesn't fly straight — curves in from an angle, reaching around cover a straight-line bomb can't | **New — `Sidewinder`** |
| Delayed plant / trap | Burrow, Double Mine, Geo Trap | Lands and *waits*, nearly invisible, detonating only once the enemy walks close — a positional trap, not a direct throw | **New — `Sapper Mine`** |
| Status / immobilize | Freeze | Minimal direct damage, but locks the target in place for a beat — a setup tool for a follow-up hit, not a finisher | **New — `Cryo Canister`** |

### 4.2 Not adopted as a distinct bomb (already covered by our reticule system)

The source's Dual/Gemini family (hits both in front of *and* behind the target simultaneously, punishing a stationary opponent) and its Tomahawk/Crescent/Smash vertical-drop family (arcs high, drops straight down onto hidden targets) are neat ideas, but both are really just different **reticule-anchor behaviors** on top of a bomb we could already build with the existing `reticuleAnchor`/`reticuleRange` fields (`src/parts/parts.ts`) rather than new mechanical primitives — worth remembering as *tuning* choices for a future bomb (e.g., a bomb that plants its reticule directly overhead instead of ahead-of-self) rather than new fields.

### 4.3 Future system: selectable impact type

The source lets many bombs (and some pods) pick from up to 11 lettered knockback/status variants of the *same* underlying bomb — sideways shove, slow upward pop, stun/immobilize, pull-toward, diagonal launch, etc. — as a modifier independent of the bomb's base shape. That's a genuinely reusable idea: instead of (or alongside) new bomb *shapes*, a future pass could add a shared `impactType: "shove" | "launch" | "pull" | "stagger"` trait that any bomb (or even the shield's guard-break) could carry, decoupling "how the blast looks/travels" from "what it does to the victim on hit." Flagging as a structural idea, not committing to it now — it would touch `Health.ts`'s knockback/knockdown handling, not just `parts.ts`.

## 5. Pods

Our two existing pods (`Sentry Pod`, `Hornet Pod`) are both "sits near you and fires steadily" — a simpler, more direct-combat-focused take than the source's pod roster, which leans heavily on movement gimmicks over raw fire rate. Three new families fill that gap:

| Family | Source basis | Mechanical identity | Status |
|---|---|---|---|
| Direct combat, steady fire | (our own invention — no close source equivalent) | Deployed, holds position near you, fires on a timer | **Have** — `Sentry Pod`, `Hornet Pod` |
| Roaming seeker | Seeker, Cockroach, Dolphin | Wanders the arena on its own, rushing in and self-destructing only once it gets close to the enemy — a delayed, positional threat rather than an instant one | **New — `Skitter Pod`** |
| Stationary freeze turret | Sky Freeze, Ground Freeze, Spider | Parks in one spot (or hovers) and does nothing until the enemy wanders near, at which point it locks them down for a follow-up | **New — `Glacier Pod`** |
| Defensive screen | Float, Umbrella | Doesn't chase or lock down — creates a zone the enemy has to path around, denying air approach or a direct line of attack rather than dealing damage itself | **New — `Veil Pod`** |

## 6. Legs

Our three existing leg parts (`Strider`, `Cheetah`, `Cricket`) already cover neutral/balanced, ground-speed specialist, and extra-air-mobility. Three source families aren't yet represented:

| Family | Source basis | Mechanical identity | Status |
|---|---|---|---|
| Neutral | Standard | No tradeoff, no edge | **Have** — `Strider Legs` |
| Ground-speed specialist | Formula | Faster running, worse turning | **Have** — `Cheetah Legs` |
| Extra air mobility | Long Thrust | An added dash charge, better landings | **Have** — `Cricket Legs` |
| Pure vertical specialist | High Jump | Much higher jump apex than any other leg part — the answer to arenas with tall cover or vertical hazards, at no real ground-speed cost | **New — `Stilt Legs`** |
| Tight-turn agility | Ground | Sharper turning radius rather than raw speed — favors robos that juke and reposition constantly over ones that just outrun | **New — `Vector Legs`** |
| Extended air time | Feather | Falls slower without jumping any higher or farther — more time airborne to line up an air-specific attack, at the cost of being predictable while hanging | **New — `Plume Legs`** |

## 7. What this doc is and isn't

This is a **content-expansion map**, not an implementation plan — none of the new parts above have stats, meshes, or catalog entries yet. When any of these get built, they should follow the same pattern as the existing catalog: a short blurb, numbers calibrated against `TUNING`'s existing ranges (not the source's raw numbers, which run on a completely different scale), and — per `ROBOT_SHELL_DESIGN.md`'s standing rule — a silhouette or mesh distinct enough to read at a glance, never a reskin of an existing part.
