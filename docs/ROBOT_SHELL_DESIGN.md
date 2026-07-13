# Robot Shell Design Reference v1

*Reference basis for how robo bodies should look and read, before weapons/parts differentiate them further. Source: [Custom Robo Battle Revolution — Robopedia](https://customrobo.fandom.com/wiki/Custom_Robo_Battle_Revolution) and its per-class pages. This is inspiration for proportion and silhouette language, not a licensed asset source — no shell should be a direct copy of a named Custom Robo.*

## 1. Why silhouette-first

Custom Robo's roster works because every body class is identifiable by silhouette alone, at small size, mid-battle, before you can read any surface detail. Height, bulk, limb proportion, and head shape carry the read — color and ornamentation are secondary. This matters more for us than it did for Custom Robo: we're in full 3D with a free camera instead of their fixed 2.5D arena view, so a robo needs to read correctly from any angle, not just a canonical front-on shot. Every body archetype should be identifiable in a screenshot with all surface detail stripped to grey.

## 2. Reference archetypes

Custom Robo's classes map cleanly onto the four body archetypes already named in `GAME_DESIGN.md` §2.1 ("a balanced 2-dash all-rounder, a long-single-dash glass-cannon flier, a stealth/vanish-dash evader, a slow tank with high load capacity"). Use these as the visual basis for those four:

| Our archetype | Reference class | Silhouette | Build notes |
|---|---|---|---|
| Balanced 2-dash all-rounder | **Shining Fighter** | Average height, average build — the literal baseline other classes are measured against | No exaggerated proportions in any direction. Practical plating, nothing oversized. This is the "default" read — a player should be able to tell at a glance that every other archetype is a deviation from this one. |
| Long-single-dash glass-cannon flier | **Lightning Sky** | Above-average height, sleek and aerodynamic, bulkier than the all-rounder but built for streamlined motion | Wing-like or fin-like silhouette elements (shoulder/back/calf blades) that suggest flight without being literal wings. Lightning Sky robos transform toward an aircraft-like shape mid-dash — worth stealing as a *pose* idea (limbs tuck/sweep back during the boost dash) rather than a literal transformation. |
| Stealth/vanish-dash evader | **Strike Vanisher** | Slightly shorter than the all-rounder, fully armored with no organic-looking segments | Angular, matte, low-profile plating — knight/ninja/samurai proportions rather than bulky sci-fi armor. Read as "built to not be seen," not "built to tank hits." |
| Slow tank, high load capacity | **Metal Grappler** | Tallest and bulkiest class, several heads taller than the all-rounder | Oversized shoulders/limbs, wide stance, heavy plating. Large hurtbox is a legible tradeoff — the silhouette should make "this thing is easy to hit but hard to put down" obvious before a fight even starts. |

Two more classes are worth keeping on file for later body variety (Stage 2+, once we're past a single Stage-1 prototype robo) rather than designing now:
- **Trick Flyer** — tall and slender with a proportionally larger head; aerial zigzag mobility. A possible fifth archetype if we ever want a dedicated aerial-evasion body distinct from the Lightning Sky glass cannon.
- **Little Raider** — small, child-sized proportions, ground-speed specialist. Useful later if we want a genuine "small and fast" body distinct from all four above (none of our four current archetypes are actually *small*).

Not adopting from the source material: **Aerial Beauty** (female-coded) and **Shining Fighter**'s anime-protagonist faces, and **Funky Big Head**'s fused big-head shape — these are strong character-design choices for a story-driven RPG, but we have no cast of named pilots and no story beats riding on a robo's face. Keep faces minimal or absent; the shell is the character, not a portrait.

## 3. Shared design rules across all archetypes

- **Humanoid, bipedal, screen-legible.** No fully abstract or vehicle-form bodies — parts customization (gun/bomb/pod/melee mounts) needs a consistent humanoid frame to attach to.
- **Neutral base palette for now.** One primary hull color + one accent color per shell, kept generic/unbranded — this is a placeholder identity per the brief, not a final faction or character palette. Don't invest in lore-specific coloring yet.
- **Reserve visible mount points.** Even though Stage 1 only needs one robo with one gun and one melee weapon hardcoded, block out where parts will attach later so we're not retrofitting the rig in Stage 2: forearm mount (gun), back or hip mount (melee weapon, sheathed/stowed when not in use), shoulder or back mount (pod), hip or back mount (bomb). This is a rigging/attachment-point note, not a system to build now.
- **Distinguish "hit" from "armor" visually.** Because endurance/knockdown and (later) shields are core mechanics, the shell should have a clear state readable at a glance: standing, staggered/knocked-down, and rebirth-invincible should each be posable/distinguishable without needing to stare at the HUD.

## 4. Stage 1 recommendation

Build the single Stage-1 prototype robo as the **balanced all-rounder (Shining Fighter–style)** archetype: average proportions, no exaggerated silhouette in either direction. This is deliberate — Stage 1 is about proving the movement-and-punish loop feels good with zero upgrades, and a neutral body avoids any archetype-specific visual or animation bias sneaking into that judgment. The tank/flier/evader shells are Stage 2 work, once the loadout system exists to actually make their tradeoffs matter.
