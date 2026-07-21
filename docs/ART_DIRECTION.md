# Art Direction — Mecha Knights After the Last Edict v2

*Canonical art, animation, VFX, and audio direction for Rebirth Protocol (2026-07-18). Builds on [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md) (world), [`ROBOT_SHELL_DESIGN.md`](ROBOT_SHELL_DESIGN.md) (silhouette-first rule, still in force), and [`KNIGHT_ROBOT_ASSET.md`](KNIGHT_ROBOT_ASSET.md) (the Cobalt Knight, the calibration standard for everything here). Research basis: the Knights in Powered Armor trope family and a survey of how giant-knight media actually designs and fights its machines — Vision of Escaflowne (Guymelefs), Knight's & Magic (Silhouette Knights), Break Blade (Golems), Warhammer 40k Imperial Knights, BattleTech, Armored Core 6, Gundam Iron-Blooded Orphans.*

---

## 1. Scale and anatomy canon

- **A harness stands 10–14 meters.** This is Armored Core / Imperial Knight scale, deliberately *not* Custom Robo's tabletop-toy scale and not human-scale power armor: as a mounted knight towered over footmen, a harness towers over infantry. Cover walls in the Lists read roughly waist-to-shoulder height on a harness — which is exactly how our existing arena walls already read.
- **The pilot sits in the chest**, behind the thickest plate (behind the Cobalt Knight's cross, in-fiction). **The head is a sensor-helm, not a cockpit** — which is why helms can be sculptural (crests, brows, visor slits) instead of practical.
- **Proportions are heroic, not realistic**: ~1:6–1:7 head-to-height, longer legs than real armor allows, oversized forearms and greaves (they carry the parts). This matches both the Cobalt Knight as built and the isometric camera's need for readable limbs at distance.

## 2. What makes a machine read as a *knight* — the seven markers

Distilled from the survey (§3). A chassis needs most of these, always the first three:

1. **A faceless helm.** Visor slit or blank plate — never eyes, never a face. (Every source agrees; `ROBOT_SHELL_DESIGN.md` §2 already bans faces.)
2. **Pauldrons as the dominant upper mass.** Shoulder silhouette is the knight-read at a glance; Imperial Knights and Escaflowne both live on their shoulders.
3. **Carried weapons, not integrated ones.** A knight *bears* arms: guns are held like arquebus-cannons, blades are drawn from back/hip sheaths, shields strap to the forearm. Nothing grows out of the body. (Our `Socket_*` hand-anchor pipeline already enforces this — it is now doctrine, not convenience.)
4. **Skirts and hems.** Tassets, pteruges, mail-hem trim — armor layers that move. Already on all four built chassis; keep on every future one.
5. **Heraldry on flat panels.** Chest and pauldron faces stay uncluttered so livery and device read at distance (§4).
6. **Cloth and capes are earned, not default.** Only where the Order's identity says so (Duskmantle's cowl/cape). Escaflowne proves how much life a cape adds; 40k proves how special it stays when rare.
7. **Verticality of ornament.** Crests, banner-poles, steeple-line details that pull the eye *up* — machines built by a culture that builds cathedrals.

## 3. Survey: how giant-knight media designs and fights its machines

What each touchstone actually contributes, kept as one-line design lessons:

- **Vision of Escaflowne (Guymelefs)** — knight-mechs as *characters*: organic curves under hard plate, flowing capes, swords sheathed on the back, dragon-slaying knightly duels with fencing weight. Lesson: **grace over efficiency; the sword-draw is a ceremony.** ([Escaflowne design history](https://mechabay.com/about-escaflowne), [Guymelef reference](https://escaflowne.fandom.com/wiki/Guymelef))
- **Knight's & Magic (Silhouette Knights)** — a metal skeleton with crystal "muscle," pilots as a formal knightly institution, back-mounted auxiliary arms/weapons. Lesson: **the institution around the machine (orders, ranks, squires) is part of the design language.** ([Silhouette Knight reference](https://mmecha.fandom.com/wiki/Mecha_and_Monsters_from_Knights_and_Magic))
- **Break Blade (Golems)** — quartz-driven stone-age-tech mechs with enormous *inertia*: every swing has windup, footwork matters, shields and lances win wars. Lesson: **weight is choreography — commitment and follow-through sell mass better than detail does.**
- **Warhammer 40k Imperial Knights** — noble households, tilting plates, directional ion shields, heraldic carapaces, machine-worship. Lesson: **heraldry is load-bearing, and a *directional* shield you must face toward the threat is knightly by nature** — which is exactly our front-arc block system.
- **BattleTech (Succession Wars)** — Lostech scarcity: mechs as irreplaceable inheritance, patched over centuries. Lesson: **damage is history — weathering tells you who has an armory and who has a salvage pit** (our Order-pristine vs. patchwork split).
- **Armored Core 6** — stagger/punish rhythm, boost economy, shield-arms as active tools. Lesson: **mechanical honesty — every visual flourish must telegraph a real gameplay state.** (Already our combat skeleton.)
- **Gundam: Iron-Blooded Orphans** — no beam-spam: maces, lances, and momentum; brutal, low-tech-reading melee on high-tech frames. Lesson: **kinetic melee reads medieval all by itself when the animation carries momentum.**
- **Panzer World Galient (1984)** — the earliest full statement of our exact premise: legendary "panzers" preserved underground for millennia beneath a medieval world, excavated and ridden to war ([overview](https://en.wikipedia.org/wiki/Panzer_World_Galient)). Lesson: **the machine as excavated inheritance — presentation should always whisper "this was dug up, not built."**
- **Aura Battler Dunbine (1983)** — mecha in a medieval fantasy that feel *summoned* rather than engineered: organic shells, metaphysical wrongness ([overview](https://tvtropes.org/pmwiki/pmwiki.php/Anime/AuraBattlerDunbine)). Lesson: **reserve the organic/wrong register for one thing only — for us, the Edictbound tier and the Riderless.**
- **Imperial Knight heraldry, deepened** — house liveries vary per Knight but **no two liveries are identical**; battle honours and oaths are painted *onto* the machine ([heraldry reference](https://warhammer40k.fandom.com/wiki/Imperial_Knight_Heraldry)). **Freeblades** — knights who forsake their house — spurn house heraldry for a **single colour, a personal device, and a motto**, often with memento-mori marks ([Freeblade reference](https://warhammer40k.fandom.com/wiki/Freeblade)). Lessons: individual harnesses within an Order should carry small personal deltas (honour-marks, oath-scripts) on the shared livery; and the **Hedge Errantry's entire visual identity is the Freeblade rule** — one flat colour, one painted personal device, one motto, nothing else.
- **The Five Star Stories (Mortar Headds)** — knight-mecha as **heirloom works of art**: frescoed armor plates, machines that serve for *centuries* and pass from bearer to successor, "headliner" and "knight" as synonyms, dressed differently for parade and for war ([primer](https://www.mahq.net/fss-primer/), [Mortar Headd reference](http://www.gearsonline.net/series/fivestarstories/mh/)). Lessons: **celebrated individual harnesses carry proper names and recorded lineages of bearers** (the run's rivals should occasionally fly a *named* machine older than their Order's founding — a story hook and a spoils flex in one); and our garniture system doubles as FSS's parade-vs-war dressing, so tourney presentation is already in the design.
- **Code Geass (Knightmare Frames)** — the knightly *institution* layered onto agile mecha: Arthurian-named frames, a Round Table of personally-titled champion knights ([reference](https://codegeass.fandom.com/wiki/Knightmare_Frame)). Lesson: **rank as title, not number** — our Class C→S armiger ladder should surface in-fiction as spoken titles (squire-errant up to Paragon of the Passage), and champion rivals may bear singular titles the way the source's champions do.
- **Warmachine / Iron Kingdoms (warjacks)** — multi-ton coal-and-steam war machines, over-built and riveted, in a sword-and-gunpowder feudal world ([reference](https://ironkingdoms.fandom.com/wiki/Warjack)). Lesson: the **scrapwright register already has a whole genre behind it** — steam pressure, stack-venting, and boiler mass are the correct visual grammar for Wrightsguild machines, and they should *sound* like it too (chuff and hiss under the synth palette).

## 4. Order liveries and heraldry

Each Order gets a livery triplet — **field / metal / accent** — plus a device carried on chest and left pauldron. The Cobalt Knight's cobalt/ivory/aurum language generalizes into this table. Team-tint (player cyan / enemy orange on the accent glow) stays on top, per the built `CHASSIS_PALETTE` approach.

| Order | Field | Metal | Accent | Device |
|---|---|---|---|---|
| The Rust Cross Commandery | Cobalt | Ivory | Aurum (gold) | A rust-red cross, deliberately left unpolished |
| The Aureate Legion | Porphyry (dark purple) | Gold | White | A ringed sun-standard |
| Order of the Winter Wing | Bone white | Silver | Pale ice-blue | A single downswept wing |
| The Umbral Concordat | Matte black | Gunmetal | *None* (no glow — canon) | A blank circle: the device is the absence of one |
| The Drowned Compact | Sea-green | Tar black | Brass | An anchor fouled in chain |
| The Solarian Talon | Sun-gold | Jade | Scarlet | A raptor's talon grasping a sunburst |
| The Kurultai Vanguard | Lacquer red | Dark iron | Horsehair white | A horsetail standard |
| The Hedge Errantry | *One flat colour per knight* (never a table colour) | Bare steel | — | A personal device + painted motto, per knight |
| The Wrightsguild | Oxide grey | Riveted iron | Safety ochre | A hammer-and-compass stencil |
| The Litany Sisters | Slate | Pewter | Candle gold | A single unbroken ring of script |
| The Broken Choir | Ash black | Tarnished silver | Violet-black (Edictbound glow) | A shattered ring of script |

**Rules:** field colors never repeat across Orders; the Umbral Concordat is the *only* no-glow faction (already canon for the Duskmantle); non-Order/mercenary harnesses wear chipped, mismatched liveries with painted-over devices — you should be able to read a mech's *history of ownership* in its overpainting.

### Religious and ideological marks

These marks can cross Order liveries because belief and military allegiance overlap rather than map one-to-one:

- **Universal Communion:** Open Crown or Cleft Halo; circles and halos always retain a visible gap. Rust Cross investiture marks often place the open crown behind the commandery cross.
- **Court of Ten Thousand Banners:** lineage banners, saddle-rolls of bearer names, and component adoption ribbons. A captured part should visibly retain or overpaint the previous lineage's name.
- **Covenant of the Second Sun:** a radiant inner circle within a dark outer housing, most often on reactor, solar, greenhouse, or thermal-service plate rather than a war crest.
- **Rite of the Whole Crown:** closed circles, perfect command rings, symmetrical seals, and complete crowns. Its visual opposition to Communion gaps should read instantly.
- **Broken Choir:** concentric violet-black rings broken inward, as though the fracture is pulling toward a missing center.
- **Mortal Measure:** a human hand beneath a simple measuring line; moderate marks are painted workshop signs, while Ash Levelers cut or burn the mark into captured plate.
- **Cinderbound:** deliberately incomplete personal markings and obvious disposable Edictbound grafts; they do not expect the transformed body or harness to survive.

Religious symbols should not replace Order heraldry unless a character has renounced their military allegiance. Layer them as oath marks, cockpit seals, relic tags, pilgrim ribbons, inspection chalk, or battle honors.

## 5. Materials: the used-future split, formalized

Four material registers, in strict order of finish quality:

1. **Order-pristine** (relic-pattern in Order hands): clean coherent plate, livery enamel, gold trim, soft godlight glow in the seams. The Cobalt Knight is the standard.
2. **Free-company patchwork**: same ancestral bones, generations of mismatched replacement plates, welded seams, rope and strapping, hand-painted devices. Silhouette stays clean (gameplay legibility first); *surface* carries the poverty.
3. **Scrapwright work** (revised 2026-07-18 — *dependable, not comic*): visibly new-made and proud of it — thick uniform boiler-plate, exposed rivets in honest rows, stenciled Wrightsguild marks, oil-and-steam venting instead of godlight, everything over-built and square. It should read like beloved farm machinery or a working locomotive standing among cathedral relics: outclassed, unashamed, and clearly the only thing on the field its owner could fix with their own hands.
4. **Edictbound**: ancestral geometry gone *wrong* — asymmetric growths, seams that glow violet-black, surfaces that read slightly organic. Used sparingly; it should be alarming precisely because everything else obeys the rules.

## 6. Fight choreography

The combat sim is built; this is how presentation should read on top of it:

- **Weight and commitment.** Follow Break Blade / IBO: windup, follow-through, and recovery poses on every melee swing; the Dolorous Maul should pull its wielder half a step. Never snappy anime-lite swings on heavy weapons — the whiff-recovery stat *is* the animation brief.
- **The joust pass.** The gap-closer lunge (and the future Tilt Lance) is a camera moment: two harnesses closing and passing is the setting's signature image. Dash trails + a brief hit-pause on a landed lunge sell it.
- **Knockdown is a kneel; rebirth is an accolade.** A downed harness falls to one knee, head bowed — and the rebirth-invincibility flare is the moment it *rises*, godlight streaming from the seams: the Rebirth Protocol as visible rite. This single animation pair carries more of the game's identity than any other.
- **Shields are directional theater.** The plate visibly swings to face the threat (built); guard-break should shatter *outward* with the shield-arm thrown wide — the most readable "now punish" telegraph in the game. The new toll (`ARMORY_REFERENCE.md` §2.3) gets a visible state: a lowered shield on toll hangs dead at the side, glow extinguished, until it relights.
- **Vanish-dash is a cloak-sweep**, not a teleport blink: the Duskmantle wraps and is elsewhere.
- **Idle is ceremony.** Harnesses at rest stand at guard like soldiers on watch, not in game-idle sway.

## 7. VFX language

Hard constraint carried forward from §22 of the design log: **world-space effects are opaque, scale-animated primitives** (URP strips unreferenced transparent variants from player builds); screen-space transparency lives on the UI shader only.

- **Godlight (ancestral tech): warm gold-white.** Muzzle flashes, dash trails, rebirth flare, seam glows. Muzzle flashes bloom in a four-petal rose-window cross rather than a star.
- **Edictbound: violet-black**, reserved exclusively for that tier and the Riderless — the player should learn the color means *wrong*.
- **Explosions ring like bells**: keep the built layered explosion, add one expanding torus "toll ring" on big blasts (bomb detonations, guard-breaks) — an opaque ring primitive, build-safe.
- **Impact sparks read as forge-sparks** (brief orange-gold debris) — the world's violence is smithing, not electronics.
- **Fetter (when built) is a shackle ring** — a flat heraldic-fetterlock circle at the victim's feet.
- **Order accents stay diegetic**: the only saturated colors in a fight are livery, accents, godlight, and hazards — arenas stay desaturated so the knights carry the color.

## 8. Audio direction

All synthesized, extending the built `SynthClips`/`SfxPlayer`/`MusicSequencer` stack — no samples.

- **Sound palette: the forge, the chapel, the field.** Metal-on-metal (anvil rings) for melee; deep bell tolls for knockdown/guard-break; low choir-like pad swells (detuned saw stacks through a soft filter — buildable in `SynthClips`) for rebirth and victory; wind and creak beds for hangar ambience.
- **The Hush motif**: a four-note falling minor phrase (e.g. A–G–F–E over an open fifth) as the game's leitmotif — stinger form on run-start, defeat, and any future Riderless appearance. Cheap to bake, instantly ownable.
- **Music**: keep the 84 BPM hangar pad / 128 BPM combat sequencer architecture. Push the hangar pad toward *plainchant*: open fifths and octaves (drop the third from pad chords), slow bell strikes on bar lines. Combat keeps the drum grid but swaps toward deep floor-tom kicks and an anvil-clang backbeat; the three rotating progressions stay, preferring Dorian/Aeolian colors.
- **Per-Order stingers (future polish)**: a two-note fanfare variant per rival Order on fight-start cards — same instrument, different interval, so five rivals stay audibly distinct for almost no work.

## 9. What this changes about existing assets

Nothing built needs rework. The Cobalt Knight already satisfies §1–§5 (it defined half of them). The three primitive chassis remain placeholders pending rigged replacements judged against §2's seven markers and §4's livery table. The §7 muzzle-rose, toll-ring, and kneel/accolade items are the first VFX/animation tasks worth a dedicated pass, in that order.

*Research sources: [mechabay.com — About Escaflowne](https://mechabay.com/about-escaflowne), [escaflowne.fandom.com — Guymelef](https://escaflowne.fandom.com/wiki/Guymelef), [mmecha.fandom.com — Knight's & Magic mecha](https://mmecha.fandom.com/wiki/Mecha_and_Monsters_from_Knights_and_Magic); TV Tropes' Knights in Powered Armor page was requested as reference but blocks automated fetching — its trope framing (knightly codes + heraldry + powered armor as chivalric revival) is reflected via the surveyed works above.*
