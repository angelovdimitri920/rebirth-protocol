# Gameplay, Tournament, and Progression

*Authoritative high-level gameplay and campaign-progression contract. Consolidated baseline: 2026-07-23.*

## 1. Scope and status

This document owns:

- The complete player loop.
- Combat pillars and battle rhythm.
- Harness customization.
- Passage structure.
- Grand Passage events and tournament formats.
- Laurels, spoils, run rewards, and persistent progression.
- The relationship between the campaign shell and battle systems.

Detailed rules remain in:

- [`COMBAT_DOCTRINE.md`](COMBAT_DOCTRINE.md)
- [`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md)
- [`ARENA_ROSTER.md`](ARENA_ROSTER.md)

Implementation history remains in [`GAME_DESIGN.md`](GAME_DESIGN.md) until extracted.

Sections labeled **Built** describe the current Unity implementation. Sections labeled **Confirmed canon** describe the intended final structure even when not built. Progression conflicts in Section 14 remain **Undeveloped**.

## 2. Player experience

The intended final loop is:

> **Talk and shop in Lasthold → customize and test the harness → enter an event → fight its Passage → receive laurels, spoils, and a story development → return to a changed world**

Each event should support a twenty-to-forty-minute session. A long sitting can complete a circuit tier.

The current build implements a title screen, hangar, five-fight run, drafts, and return flow. The walkable Lasthold shell is confirmed but planned for a later production stage.

## 3. Core design pillars

1. **Build expression:** Every part or boon changes how the player approaches combat.
2. **Readable, skill-based combat:** Openings come from spacing, recovery, cover, and opponent knowledge.
3. **One more Passage:** Events create escalating power and meaningful choices without making builds unreadable.
4. **Campaign consequence:** Tournament success changes relationships, political attention, access, and story.

The project must preserve the combination of:

- Modular pre-battle construction.
- Real-time 3D arena combat.
- Roguelite event progression.
- Persistent story-driven advancement.

## 4. Harness loadout

The current loadout has five functional slots:

| Slot | Choice |
|---|---|
| Body | Chassis pattern and garniture; establishes movement, ward, poise, and charge identity |
| Right arm | Gun **or** melee |
| Left arm | Bomb **or** shield |
| Pod | Deployable pressure, zoning, or support system |
| Legs | Ground speed, jump, dash, turn, landing, or hazard behavior |

The arm mutexes are structural:

- Gun and melee are mutually exclusive.
- Bomb and shield are mutually exclusive.
- Pods remain independent so they can provide constant pressure.

Any lawful part can fit any compatible body. Stewardship influences access, teaching, shop bias, and story meaning, not hard faction locks.

Chassis must be combat archetypes, not stat sticks. Parts must create tactical identities rather than straight upgrade ladders.

## 5. Battle rhythm

The canonical duel sequence is:

> **Casting → neutral → poise break → knockdown → rebirth → renewed neutral**

### The Casting

**Confirmed canon, not built:** Harnesses enter the List as sealed relic-cores, tumble into the arena, and unfurl. Mashing speeds deployment and the initial cast influences landing position.

### Neutral

Neutral play uses:

- Movement and boost economy.
- Lock-on and range.
- Cover that breaks sightlines and homing.
- Pod pressure.
- Attack recovery and landing recovery.
- Baiting and punishing commitment.

### Knockdown and Rebirth

**Built:**

- Health and endurance are separate.
- Damage drains endurance as well as health.
- Empty endurance causes knockdown.
- The downed harness is fully invulnerable.
- Mashing speeds recovery.
- Rising creates brief rebirth invulnerability.
- Endurance regenerates when the harness is not taking damage.

Presentation must read as kneeling followed by an accolade-like rise.

## 6. Tactical roles

| Tool | Primary job |
|---|---|
| Gun | Sustained, ranged, or profile-specific damage |
| Melee | Poise pressure, gap closing, and punishment of rooted actions |
| Bomb | Aimed displacement, area denial, cover pressure, and burst |
| Shield | Directional defense that converts a read into tempo through block, parry, bash, and toll |
| Pod | Continuous low-risk pressure, traps, landing coverage, and route control |
| Body charge | No-ammo committed threat with invulnerability during the strike and vulnerability around it |
| Legs | Movement identity and correction of chassis weaknesses |

The four primary arm doctrines must remain viable:

- Gun and bomb.
- Gun and shield.
- Melee and bomb.
- Melee and shield.

## 7. Combat contract

### Confirmed and built or directed

- Commitment buys damage.
- Point-blank damage is earned through approach risk.
- Landing remains a vulnerable moment.
- Firing creates a punishable commitment.
- Cover breaks homing and sightlines.
- Bomb aiming creates vulnerability.
- Pods act without a firing vulnerability window.
- Shields are directional and pay a toll after lowering or breaking.
- Melee strings require hit confirmation to continue.
- Simultaneous melee can clash into a short reset.
- Grounded charge attacks have readable windup and recovery.
- Knockdown wipes the fallen pilot's own gun rounds; bombs and pods remain.
- Downed harnesses remain fully invulnerable.

### Pacing targets

- Equal duels: **60-to-120 seconds**.
- Expected knockdowns: **2-to-5**.
- Matchup balance target: **40-to-60 percent** win rate in deterministic AI-vs-AI validation.

Current balance-harness findings show several built matchups below the target time and identify the Cobalt Knight as unusually dominant. These are implementation-tuning findings, not changes to the target.

## 8. Arena structure

A fight is designed as:

> **Layout × overlay**

Confirmed arena principles:

- Hard-bounded duel spaces.
- Cover and sightline management.
- Breakable and unbreakable elements.
- Size as a deliberate pacing lever.
- Optional moving systems.
- One memorable landmark per List.
- Expensive but real corner escape.
- Fight one of a Passage remains overlay-free.

The canonical roster contains twenty-one Lists, four overlays, and two story or optional sites. See [`ARENA_ROSTER.md`](ARENA_ROSTER.md).

## 9. Current Passage implementation

**Built:**

- Five escalating duels.
- Named rival sequence.
- Player health persists between fights.
- Fifteen percent maximum-health recovery between fights.
- Endurance, shield, and boost reset between fights.
- Death ends the run.
- Three-boon draft choices from different ability slots.
- One reroll per run.
- Skip option.
- Mid-fight item drops from destructible cover.
- Hyperbolic stacking for percentage effects.
- Run-scoped rival spoils as a fourth draft choice.
- Deterministic seeds for arenas, drafts, and drops.
- Enemies do not draft boons.

The current rival order is:

1. Bannerlord Cassian.
2. Skald Maren.
3. Vesk the Unseen.
4. Warden Aldric.
5. Grandmaster Otho.

This five-fight Passage is one event inside the intended larger campaign, not the complete Grand Passage.

## 10. Grand Passage

The Grand Passage is the sanctioned circuit through which armigers earn public legitimacy, Order attention, and access to greater challenges.

### Confirmed event framework

| Event | Format or story role |
|---|---|
| Hearthside Tilt | Novice Passage without restrictions |
| Cup of Tempered Hearts | Vow of Temperance |
| Almsbowl | Vow of Temperance in the Basin |
| Wardens' Muster | Shieldbrother event with Warden Linnet |
| Trial of Simulacra / Trial of Wings | Battles against training phantoms |
| Twin Harness Trial | Tag event |
| Guildhall Proofs / Ordeal of Two / Wrightmother's Favor | Wrightsguild singles, handicap, and partner events |
| Sisters' Fusillade | Six Litany Sister duels |
| Taproom Compact | Shieldbrother event with Ser Ernust |
| Masquerade of Blades | High-society Vow event |
| Surveyor's Circuit | Singles across newly consecrated Lists |
| Broken Choir's Gauntlets | Antagonist sequence using Edictbound equipment |
| Bronze Ordeal | One-versus-two events gated by bronze laurels |
| Silver Melee | One-versus-three free-for-all gated by silver laurels |
| Golden Passage | Eight duels against Paragons; concludes with the First Armiger |

The exact campaign ordering, unlock requirements, and number of mandatory events remain **Draft**.

## 11. Tournament formats

| Format | Rule |
|---|---|
| **Standard Passage** | Sequence of single duels with drafts between fights |
| **Vow of Temperance** | Each part can be used for only one fight in the event |
| **Shieldbrother** | Two-versus-two with selective friendly fire |
| **Trial by Ordeal** | One-versus-two with target-based aggression and mercy options |
| **Twin Harness** | Two loadouts with mid-fight switching and limited bench recovery |
| **The Melee** | One-versus-three free-for-all |

Team and multi-target formats are confirmed design but not current core implementation.

## 12. Laurels and ranks

**Confirmed canon, not fully built:**

- Laurels combine victory speed and remaining vigor against a posted task score.
- Bronze, silver, and gold thresholds gate later events.
- Rematching after defeat reduces the available score.
- Equipping any Edictbound part halves laurels for that fight.
- Mercy settings lower enemy vigor at an honor cost.
- Armiger competence runs from Class C through Class S.
- Class S armigers are the **Paragons of the Passage**.

Laurels should make honor and mechanical optimization point mostly in the same direction without functioning as moral purity points.

## 13. Equipment progression

### Run-scoped progression

**Built:**

- Boons.
- Stacking items.
- Rival spoils.
- Healing between fights.
- Enemy power escalation.

Spoils allow mid-Passage adaptation and respect the right-arm and left-arm mutexes. They currently do not alter the saved hangar loadout.

### Persistent progression

**Undeveloped:**

- How the player permanently earns parts.
- Whether captured parts require legal, religious, or guild processing.
- Whether all hangar parts begin unlocked.
- What shops sell.
- How stewardship affects access.
- Whether named relics are unique.
- How Revival and scrapwright gear enter the player's permanent armory.
- Whether Edictbound equipment can be stored safely.
- Whether relationships or factions unlock equipment.

### Existing principle

Meta-progression should primarily unlock options rather than flat statistics. The campaign fantasy also promises meaningful acquisition of stronger and more prestigious equipment. These goals require a deliberate hybrid model rather than an accidental contradiction.

## 14. Progression decisions that must be resolved

| Decision | Current conflict |
|---|---|
| Permanent parts | Current rival spoils are run-scoped, but campaign advancement implies a growing armory |
| Power curve | "Unlock options, not raw power" conflicts with the fantasy of acquiring stronger relics |
| Starting catalog | Current hangar exposes all implemented parts, which removes discovery from the campaign |
| Unique relics | Named historical machines and parts imply scarcity, but unrestricted loadout experimentation requires access |
| Edictbound collection | The Choir's Ledger and cache acquisition exist in canon, but permanent contamination rules do not |
| Currency and shops | Lasthold requires an economy, but no persistent currencies or prices are approved |
| Failure | Current death ends the run; story consequences and retries within campaign events are not defined |
| Laurels | Scoring is confirmed, but its exact relationship to story gates, rank, shops, and replay is not built |

## 15. Recommended progression shape

**Draft for later approval:**

- Permanent progression unlocks parts, institutions, relationships, and build possibilities.
- Raw stat growth remains narrow.
- Stronger relics bring requirements, burden, specialization, or social consequences rather than universal superiority.
- Scrapwright and Revival equipment provide dependable early and political progression.
- Named relics enter through story, stewardship, adoption, or rival relationships.
- Run boons and items provide the dramatic temporary power curve.
- Edictbound equipment provides exceptional capability with legal, laurel, narrative, and personal cost.

This preserves campaign acquisition without turning the game into a numerical grind.

## 16. AI and opponent identity

Every rival combines:

- Doctrine.
- Class.
- Signature-part bias.
- Home List preference.
- Personality.
- One readable blind spot.

Canonical AI archetypes include:

- Bookman.
- Spammer.
- Point-Blank Predator.
- Skirmisher.
- Turtle.
- Kiter-Flier.
- Trapper.
- Titan-Fool.

Higher Class improves execution without removing the archetype's counterplay. No opponent becomes frame-perfect.

## 17. Controls authority

The current canonical controller mapping is maintained in [`COMBAT_DOCTRINE.md`](COMBAT_DOCTRINE.md) Section 11 and the implementation.

High-level principles:

- Controller-first.
- Left stick remains dedicated to movement and aimed-placement steering.
- Right stick remains unbound while the automatic duel camera works.
- Right trigger operates the equipped right arm.
- Left trigger operates the equipped left arm.
- Pod has its own face button.
- Jump, dash or charge, lock-on or target switch, and pause remain distinct.

Do not copy older control tables from the implementation log without checking the specialist authority.

## 18. Gameplay non-negotiables

- Combat cannot become button-mashing.
- Movement, spacing, and recovery remain central.
- Every legal loadout shape remains viable.
- Parts change behavior and doctrine.
- Chassis remain movement archetypes.
- Cover and arena geometry remain mechanically meaningful.
- Edictbound power must have a real drawback.
- Downed and rebirth states remain readable.
- Equal fights target the established duration and knockdown band.
- High-rank AI retains a designed blind spot.
- The campaign shell must eventually replace the interim menu-only structure.
