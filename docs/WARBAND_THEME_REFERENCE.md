# Warband Theme Reference v1

**SUPERSEDED (2026-07-15) — kept for historical record only.** The world/theme is now the neo-feudal used-future setting in [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md); read that doc first. The chassis silhouettes, mesh work, and part-name renames below are all still valid and don't need rebuilding — only the framing changes, from "this chassis IS a historical Earth culture" to "this chassis belongs to an in-fiction Order inspired by one." See the faction table in the new doc for the current Order name per chassis.

*Canonical art-direction and naming bible: every chassis, part, and Holosseum draws from real historical warrior cultures and armies, filtered through a Gundam/Armored Core mecha silhouette language. This is the layer that ties `ROBOT_SHELL_DESIGN.md`, `PARTS_AND_DAMAGE_REFERENCE.md`, and `HOLOSSEUM_REFERENCE.md`'s abstract archetypes into a concrete, final, original identity — nothing here is a copy of a named Custom Robo design; the source material's silhouette *proportions* (tall-and-sleek, short-and-armored, tallest-and-bulkiest, etc.) are the starting scaffold, the historical theme is what actually defines the final look.*

## 1. Why this direction

Two things pull in the same useful direction at once: an original theme is the *safest* way to differentiate from the source material (a Legionnaire is unambiguously not a copy of Ray 01, even if both happen to be "the balanced all-rounder"), and it's also just a stronger creative identity than "generic sci-fi robot with a color palette." Real historical arms and armor already did the silhouette-design work for us — a legionnaire's rectangular shield and layered lorica, a knight's towering plate and great helm, a shinobi's low hood and minimal profile are all instantly-readable shapes with thousands of years of cultural recognition behind them. That's exactly the "reads correctly as a silhouette, no surface detail needed" property `ROBOT_SHELL_DESIGN.md` §1 already established as the design bar.

**Art direction**: Gundam/Armored Core panel-line mecha language — articulated joints, layered armor plates instead of single smooth boxes, a visible "skeleton vs. armor" read, sharper silhouette breaks at the shoulders/knees/waist — applied *to* each civilization's actual visual vocabulary, not instead of it. A Legionnaire chassis should still read as a mecha first and a legionnaire second, the same way Gundam's own designs read as mecha first and knights/samurai second despite pulling heavily from both.

## 2. Chassis: civilization → archetype mapping

Every mapping below is chosen for a *mechanical* fit, not just an aesthetic one — the historical culture's real reputation matches the archetype's actual gameplay identity.

| Chassis (current name) | Mechanical identity | Civilization | Why it fits |
|---|---|---|---|
| **Vanguard** | Balanced 2-dash all-rounder, no weakness | **Roman Legionnaire** | History's most systematized, standardized soldier — drilled to be reliably competent at everything, the literal definition of "no glaring weakness, no standout edge." |
| **Bulwark** | Slow tank, huge HP, one dash | **Crusader Knight** | Full plate armor, immovable, the archetype "big and slow but you cannot easily put it down." |
| **Skylance** | Long single dash, glass cannon, hits hard and folds fast | **Valkyrie** | Norse myth's winged battlefield warriors — aggressive, airborne, high risk/reward, iconic wing silhouette potential. |
| **Wraith** | Three short vanish-dashes that phase through shots | **Shinobi** | Stealth and evasion *are* the shinobi's whole identity — vanish-dashing through incoming fire is a mecha-scale ninja teleport. |
| *(new)* **Corsair** | Hybrid balanced/flier, more air mobility than Vanguard, less commitment than Skylance | **Pirate / Privateer** | The name already means this — a corsair is a privateer. Swashbuckling, mobile, opportunistic; the "if you can't decide" pick, same as a pirate crew adapting to whatever fight shows up. |
| *(new)* **Halcyon** | Multiple short continuous jumps, never quite touches the ground | **Aztec Eagle Warrior** | The Aztec Empire's elite eagle-order warriors — agile, airborne-coded by name and iconography, an elite mobility specialist rather than a brute-force fighter. |
| *(new)* **Juggernaut** | Tank with genuinely good air mobility — heavy *and* fast | **Mongol Kheshig** | The Mongol imperial guard combined heavy armor with the era's most famous mobility doctrine — "heavy but not grounded" is the Mongol military's whole historical reputation. |

`Vanguard`/`Bulwark`/`Skylance`/`Wraith` are implemented today; `Corsair`/`Halcyon`/`Juggernaut` are documented in `PARTS_AND_DAMAGE_REFERENCE.md` §2 as the second-wave body archetypes and aren't built yet. This doc's mesh-detail work (§5) targets the four implemented chassis first.

## 3. Parts: historical weapon vocabulary

Parts aren't tied to a single civilization's chassis — the loadout system is mix-and-match by design (a Shinobi can carry a Ballista, a Knight can carry a Chu-Ko-Nu), so part names are drawn broadly across history rather than bundled per-culture. Existing catalog entries (`src/parts/parts.ts`) get renamed; new entries from `PARTS_AND_DAMAGE_REFERENCE.md` get their final names here.

### 3.1 Guns

| New name | Real historical basis | Archetype (from `PARTS_AND_DAMAGE_REFERENCE.md` §3) | Status |
|---|---|---|---|
| **Longbow** | English longbow | Baseline, honest damage and tracking | Renamed from `Blaster` |
| **Chu-Ko-Nu** | Zhuge Nu, ancient Chinese repeating crossbow | Rapid weak stream | Renamed from `Needler` |
| **Ballista** | Roman/Greek heavy siege bolt-thrower | Slow heavy single-hit | Renamed from `Ram Cannon` |
| **Trident** | Retiarius gladiator weapon (already fits — kept) | Multi-stream spread | New |
| **Onager** | Roman torsion siege catapult | Vertical arc / anti-cover | New |
| **Bolas** | South American entangling throwing weapon | Delayed trap | New |
| **Grapnel Line** | Naval boarding grapple (Pirate tie-in) | Pull / disorient | New |
| **Composite Bow** | Mongol/Scythian composite bow, famous for range | Range-scaling | New |
| **Thunderclap Powder** | Early Chinese gunpowder flash weapons | Reliable stun | New |
| **Hwacha** | Korean multi-rocket launcher — a real historical swarm weapon | Homing swarm | New |

### 3.2 Melee weapons

| New name | Real historical basis | Status |
|---|---|---|
| **Saber** | Cavalry sword (already fits — kept) | Have |
| **Warhammer** | Medieval war hammer (already fits — kept) | Have |
| **Khopesh** | Ancient Egyptian sickle-sword — light, fast, low-commitment | Renamed from `Twin Fang` |

### 3.3 Bombs

| New name | Real historical basis | Archetype (`PARTS_AND_DAMAGE_REFERENCE.md` §4) | Status |
|---|---|---|---|
| **Greek Fire Pot** | Byzantine incendiary weapon | Standard tracking lob | Renamed from `Impact Bomb` |
| **Zhen Tian Lei** | "Sky-shaking thunder" — early Chinese gunpowder bomb | Heavy slow AoE | Renamed from `Quake Bomb` |
| **Grapeshot Charge** | Naval area-denial cannon ammunition (Pirate tie-in) | Wide cluster / area denial | New |
| **Boomerang Charge** | Aboriginal Australian returning throwing weapon | Curving side-approach | New |
| **Fougasse** | Historical improvised buried land mine | Delayed plant / trap | New |
| **Smoke Pot** | Blinding/disorienting smoke bombs (Chinese and Japanese warfare) — reframes the "freeze/status" archetype as blind/disorient rather than literal cold, since freezing has no clean ancient analog | Status / immobilize | New |

### 3.4 Pods

| New name | Real historical basis | Archetype (`PARTS_AND_DAMAGE_REFERENCE.md` §5) | Status |
|---|---|---|---|
| **Terracotta Sentinel** | China's Terracotta Army — an animated guardian statue | Direct combat, steady fire | Renamed from `Sentry Pod` |
| **War Kite** | Kites used for signaling/reconnaissance in ancient Chinese warfare | Direct combat, fast burst | Renamed from `Hornet Pod` |
| **War Hound** | Dogs used in warfare across many ancient armies | Roaming seeker | New |
| **Gorgon Idol** | Greek myth's petrifying gaze — reframes "freeze" as "turn to stone" | Stationary freeze turret | New |
| **Standard Bearer** | The soldier who carried a legion/regiment's banner, historically a rallying and formation point | Defensive screen | New |

### 3.5 Legs

| New name | Real historical basis | Archetype (`PARTS_AND_DAMAGE_REFERENCE.md` §6) | Status |
|---|---|---|---|
| **Traveler's Boots** | Generic — no gimmick, no tradeoff | Neutral | Renamed from `Strider Legs` |
| **Numidian Boots** | Numidian light cavalry, famous for speed | Ground-speed specialist | Renamed from `Cheetah Legs` |
| **Winged Sandals** | Hermes/Mercury's mythological winged sandals | Extra air mobility | Renamed from `Cricket Legs` |
| **Stilt Walkers** | Real historical stilt-walking traditions (ceremonial and utilitarian) | Pure vertical specialist | New |
| **Capoeira Steps** | Brazilian martial art/dance, famous for agility and evasive footwork | Tight-turn agility | New |
| **Phoenix Plume** | The phoenix's feathers — chosen over an Icarus/falling reference to keep the "float longer" mechanic framed as graceful, not doomed | Extended air time | New |

### 3.6 Shields

`Aegis Barrier` (Greek myth's shield of Zeus/Athena) and `Bastion Plate` (a real historical fortification term) already fit the theme perfectly — no renaming needed.

## 4. Holosseums: renamed arena concepts

Pulling from `HOLOSSEUM_REFERENCE.md`'s hazard-pattern catalog (§2-3), each pattern gets a civilization-themed name. These are naming/flavor proposals for arenas that already have a design *pattern* documented — none are built yet beyond the single Stage-1 arena.

| Pattern (from `HOLOSSEUM_REFERENCE.md`) | Themed name | Why |
|---|---|---|
| Baseline neutral duel space (§2, Basic Stage) | **The Colosseum** | History's single most iconic neutral gladiatorial arena — the obvious pick for our own baseline stage. |
| Unbreakable terrain hazard, ice (§2, Frozen Field) | **Viking Fjord** | Norse warriors fighting on frozen coastal ground. |
| Unbreakable terrain hazard, lava/magma (§2, Magma Hole) | **Pompeii Ruins** | Roman volcanic disaster — a ruined city with an active, dangerous ground hazard baked into the premise. |
| Single strong landmark (§2, Castle Citadel) | **Crusader Keep** | A single defensible central structure, matching the Knight/Bulwark chassis's own theme. |
| Moving platform (§2, Loading Dock) | **Pirate Cove** | Ship decks are a natural "always-moving platform," and ties to the Corsair chassis. |
| Sloped/inclined terrain (§3.2, new lever) | **Temple Steps** | Aztec stepped-pyramid architecture is a natural, thematically-grounded excuse for an inclined arena floor. |
| Shrinking safe zone (§3.3, new lever) | **Thermopylae Pass** | history's most famous narrowing-battlefield-under-pressure — a last-stand chokepoint is exactly what a closing safe zone should feel like. |
| Pure perception/camera hazard (§3.5, new lever, flagged higher-risk) | **Mist Garden** | A shinobi-coded disorientation hazard — fits the Wraith/Shinobi chassis's stealth theme if this lever ever gets built. |
| Zero-wall open extreme (§3.6, new lever) | **The Steppe** | The Mongol Kheshig's home terrain — open, wall-less, favors mobility and range over cover play. |

## 5. Mesh redesign direction — done, all four chassis (2026-07-14)

`RoboMesh.ts` originally built every chassis from a handful of plain `THREE.BoxGeometry` slabs — functional for silhouette-testing, but flat and un-articulated. All four have since gotten a real Gundam/Armored-Core panel-language detail pass (`GAME_DESIGN.md` §14-15), informed by private reference model studies the user provided for Legionnaire, Crusader Knight, Valkyrie, and Shinobi — used only for extracting proportion/color data locally, never imported or embedded (§1's "no copy of a named design" rule held for actual assets too, not just web references).

- **Legionnaire (Vanguard) — done.** Layered torso plating suggesting *lorica segmentata*'s banded strips; a rectangular chest panel echoing a scutum shield; a helmet browline + transverse centurion crest; hanging pteruges strips; stepped pauldron caps. 9→19 primitives.
- **Crusader Knight (Bulwark) — done.** A two-piece heraldic cross emblem on the chest (reads clearly at a glance); a great-helm brow band; a mail-hem trim at the waist; stepped pauldron caps. The knight reference study confirmed this chassis should read taller *and* bulkier than the others, not just wider — it was the tallest of the four reference profiles even excluding its T-pose arm-spread.
- **Valkyrie (Skylance) — done.** A cinched waist belt, a chest crest trim, a head crest spike, and a second smaller wing-blade layered behind the existing sweptback fins. The Seeker reference showed a genuinely narrow waist band low down before widening toward the shoulders, which is what justified the cinch rather than a uniform torso.
- **Shinobi (Wraith) — done.** A hood/cowl over the crown instead of a boxy head; a diagonal wrap-sash; a longer/lower trailing cloth panel. Kept strictly `hull`/`joint` only, no `accent` — preserves the original "minimal glow, built to not be seen" identity. The Shrike reference turned out to be the *shortest* of the four models with the widest h/w ratio — a low, compact silhouette, not the tall-willowy read a "ninja" name might suggest on its own, which confirmed keeping Wraith's already-shorter proportions rather than second-guessing them.

Not yet done: individual weapon/pod/leg mesh redesigns (as opposed to chassis).
