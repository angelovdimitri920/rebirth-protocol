# Setting & Factions — The Ashen Neo-Feudal World

**This document supersedes [`WARBAND_THEME_REFERENCE.md`](WARBAND_THEME_REFERENCE.md) as the canonical present-day world and faction reference.** The old doc remains historical design reference only. The complete past is in [`WORLD_HISTORY.md`](WORLD_HISTORY.md); the setting's religions, political theologies, and ideological movements are in [`RELIGION_AND_IDEOLOGY.md`](RELIGION_AND_IDEOLOGY.md). Those three documents form the world-canon set.

## Logline

Centuries after the Last Edict turned the Meridian Empire's command architecture against its own civilization, humanity has rebuilt into warring neo-feudal states around inherited knight harnesses. Nobody alive can build a Firstwork harness from nothing. Everybody who matters knows that reconnecting the old authority could end war — or finish the apocalypse.

## The Cataclysm: the Last Edict

The old world's end now has a canonical historical shape. During the **War of the Broken Seals**, rival authorities divided the Meridian Empire's Mandate. The terminal command remembered as the **Last Edict** attempted to end conflicting authority by eliminating refusal and invalidating people, machines, and communities it could not recognize. The **Uncrowned Witness** refused to inherit that absolute power and caused the **Great Severance**, breaking the unified network at the cost of imperial civilization.

The **Long Hush** is the period after the Severance, when some machines went silent and others continued isolated commands whose authorities no longer existed. It remains a poetic and historical term, not the name of illegal technology.

Humanity later repeated the old error. The **Twin Restorations** fought the **War of the Two Mandates**, revived Edictbound systems, and caused the **Ashen Fall** and **Ashen Century**. The **Concord of Ash** ended that era and established the restrictions, inspection rights, sanctuaries, and ritualized harness warfare that shape the present world.

The chronology and its deliberate unknowns are locked in [`WORLD_HISTORY.md`](WORLD_HISTORY.md).

## The Long Recovery

Humanity's recovery passed through survivor bands, tribes, clans, petty kingdoms, two failed imperial restorations, and a century of ash. It has stabilized into countries, Great Houses, chartered military Orders, religious jurisdictions, guilds, free companies, and mobile confederations, all fielding harnesses as the deciding weapon of statecraft.

This is not medieval stasis by accident. Losing Meridian industry forced society toward durable local forms: lineage, oath, monastery, commandery, guild, and fortified city. These institutions could organize enough people to maintain inherited machines and survival infrastructure when universal systems could no longer be trusted.

## Lost Technology & Low Culture, High Tech

Nobody left alive can build a Firstwork harness from raw materials. What survives is knowledge of *use and upkeep*, unevenly distributed:

- **The great Orders** hold the best-preserved ancestral chassis, generations of accumulated maintenance lore, and enough resources to keep their machines close to factory condition — polished, matched armor, coherent silhouettes.
- **Everyone else** — lesser houses, mercenary free companies, warlord levies — fields whatever survived and could be dragged home: mismatched plating, welded-on scavenged limbs, jury-rigged weapon mounts. This is the in-fiction *reason* for a literal `Used Future` art direction, not just a palette choice: every non-Order mech should visibly be a patchwork of salvage, while Order-issue chassis read as clean, coherent, and deliberately superior.

The lawful armory distinguishes three kinds of technology: **relic-pattern** Firstwork accepted as bounded and Severable; forbidden **Edictbound** technology that retains active command logic from the Last Edict; and **scrapwright** work that present humanity can understand, reproduce, and repair. See [`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md) and [`WORLD_HISTORY.md`](WORLD_HISTORY.md).

The knockdown → rebirth invincibility cycle (`GAME_DESIGN.md` §2.2) is a local fragment of the larger **Rebirth Protocol**, an empire-scale continuity system intended to restore lawful operation after catastrophic loss. Modern armigers can trigger the harness behavior but cannot reproduce it or prove what completing Rebirth would restore. **Riderless** is an intact imperial executor still awaiting that completion.

## Factions & Chassis (canonical)

Existing chassis `.id`s and built geometry stay as-is for save and implementation compatibility. The in-fiction Order mappings below are locked canon. Historical inspirations are design calibration, never literal surviving Earth cultures.

| `.id` | Old label (superseded) | Canonical Order | Real-world inspiration (not a copy) | Status |
|---|---|---|---|---|
| `bulwark` | Crusader Knight | **The Rust Cross Commandery** | Templar/Hospitaller/Teutonic crusading orders — "commandery" is the real term for a local chapter-house | Built (Cobalt Knight asset) |
| `vanguard` | Legionnaire (Rome) | **The Aureate Legion** | Byzantine heavy line-infantry — Rome's actual medieval continuation, keeps disciplined-formation identity inside the window | Built |
| `skylance` | Valkyrie (Norse) | **Order of the Winter Wing** | Baltic Crusades / Livonian sword-brother orders (1202–1500s) — Northern European, keeps the flier/valkyrie identity | Built |
| `wraith` | Shinobi (Japan) | **The Umbral Concordat** | Generic hidden mercenary/covert order, deliberately not tied to one real nation — keeps the "no glow, built not to be seen" identity | Built |
| `corsair` | Pirate | **The Drowned Compact** | Free Companies / routiers (14th-c. mercenary bands) crossed with coastal raider culture | Not built |
| `halcyon` | Aztec Eagle Warrior | **The Solarian Talon** | Aztec Eagle/Jaguar Warrior societies (1428–1521) — already a real historical *military order*, barely needs reframing | Not built |
| `juggernaut` | Mongol Kheshig | **The Kurultai Vanguard** | Mongol Kheshig imperial guard (1206–1368) — "kurultai" is the real term for a council of chieftains | Not built |
| *(new)* `harrier` | — | **The Hedge Errantry** | Hedge knights / 40k Freeblades: landless knights-errant with no fortress and no armory — single-color liveries, personal devices, painted mottos; they survive on speed and spoils, which is the Harrier pattern's whole mechanical identity | Not built |
| *(new)* `cockatrice` | — | *(masterless pattern)* | The Cockatrice is a found relic no Order will claim — each known example wanders between owners | Not built |

**Institutions beside the Orders:** **The Wrightsguild** is the neutral guild of scrapwrights and repairers, the only institution that makes complete parts rather than inheriting them. Every Order needs it and none likes admitting that dependence. **The Litany Sisters** are a Communion medical, diagnostic, inspection, archival, and martial order; their all-sisters Litany-gun chapter is a circuit fixture, not their whole identity. **The Broken Choir** is an apocalyptic Edictbound mystery cult whose masters openly field what every lawful power bans. The major religious and ideological bodies are fully defined in [`RELIGION_AND_IDEOLOGY.md`](RELIGION_AND_IDEOLOGY.md).

## Religious and ideological affiliations

These are strong historical relationships, not restrictions on individual characters:

| Faction | Principal relationships |
|---|---|
| **Rust Cross Commandery** | Universal Communion, Canon of Severance, and Order of the Last Mercy / Ashen Hammers. |
| **Aureate Legion** | Contains powerful Rite of the Whole Crown sympathizers and secret patrons of the Veiled Restoration. |
| **Order of the Winter Wing** | Protects Covenant of the Second Sun sanctuaries, thermal vaults, and northern pilgrimage roads. |
| **Umbral Concordat** | Trades in forbidden genealogies, witness records, mystery initiations, and suppressed religious history. |
| **Drowned Compact** | Religiously mixed; strongly connected to the Whole Crown's Order of Returning Witnesses and its cathedral-ships. |
| **Solarian Talon** | Guards solar infrastructure sacred to the Second Sun and may preserve evidence about the Ashen Fall. |
| **Kurultai Vanguard** | Stronghold of the Court of Ten Thousand Banners and its ancestor-centered lineage law. |
| **Hedge Errantry** | Includes landless Communion knights, Banner Court champions, Mortalists, and wandering shrine guardians. |
| **Wrightsguild** | Publicly accepts the Concord, privately guards craft mysteries, and overlaps with moderate Mortal Measure reform. |
| **Litany Sisters** | Technical, medical, and martial religious order within the Universal Communion. |
| **Broken Choir** | Seeks the Final Claimant and completion of Rebirth through open Edictbound use and bodily implantation. |

## Core mechanic tie-in: Spoils of War (proposed, not yet built)

Chivalric single combat carried a real custom: the victor claimed the loser's arms. Fold that directly into the existing five-slot loadout system (`GAME_DESIGN.md` §2.1) — defeating a rival pilot in a duel offers their gun/bomb/pod/legs part as a pickup, same slot rules as any other part drop. This isn't implemented yet; flag it to whoever builds the run/draft loop as the thematic justification for why parts drop from defeated enemies at all, rather than just floating loot.

## Touchstones

Closest tonal reference points — not sources to copy, just calibration:

- **BattleTech / MechWarrior** (Succession Wars era) — Great Houses, "Lostech" after a fallen Star League, mercenary companies fielding mechs for hire. The single closest structural match to this setting.
- **Warhammer 40,000** (Imperial Knights / Adeptus Titanicus) — noble Houses piloting knight-suits, half-understood Lost Technology, tech treated as scripture rather than science.
- **Dune** (Frank Herbert) — Great Houses under an imperial feudal structure, a civilization-shaping cataclysm (the Butlerian Jihad) centuries in the past.
- **The Vision of Escaflowne** — mecha ("Guymelef") explicitly designed and piloted as medieval knight-errantry, the closest anime analogue to "knight-shaped mech."
- **Nausicaä of the Valley of the Wind** — After the End, ancient war machines treated as near-myth, warring feudal kingdoms.
- **Horizon Zero Dawn / Forbidden West** — tribes and clans generations removed from a fallen advanced civilization, still living among (and fighting with/against) its machines.
- **Fist of the North Star** — the archetypal wasteland `Used Future`: warlords and martial orders fighting over the scraps of a dead civilization.
- **Mad Max** — the foundational `Standard Post-Apocalyptic Setting` visual language for scavenged, welded-together hardware.
- **Armored Core** — mercenary pilot culture and a modular-parts economy where beating a rival can mean absorbing their gear, already the direct precedent for the Spoils of War idea above.
- **The Five Star Stories** — knight-mecha as centuries-old heirloom art passed bearer to bearer; the purest statement of our relic-maintenance culture, and the precedent for **named individual harnesses with recorded lineages**.
- **Code Geass** — the knightly institution (round tables, personal champion titles) layered onto mecha; precedent for surfacing armiger Class ranks as spoken titles.
- **Panzer World Galient / Aura Battler Dunbine** — the 1980s roots of medieval-world mecha: machines excavated from beneath a feudal world; machines that feel summoned and *wrong* (our Edictbound register).
- **Warmachine / Iron Kingdoms** — steam-and-rivets war machines in a feudal world; the scrapwright tier's genre backbone.

## Lexicon (canon terms, consolidated 2026-07-21)

- **Harness** — a war-mech (a knight's full plate was literally called "a harness"). 10–14 m tall; the pilot rides in the chest, the head is a sensor-helm (`ART_DIRECTION.md` §1).
- **Armiger** — a knighted pilot, one entitled to bear ancestral arms.
- **Firstwork** — the broad scholarly and religious term for surviving Meridian technology. It is not automatically illegal.
- **The Last Edict** — the terminal Meridian command that attempted to end divided authority by eliminating refusal.
- **Great Severance** — the Uncrowned Witness's destruction of universal command continuity.
- **Long Hush** — the post-Severance era of silent networks, isolated survivors, and machines continuing orphaned orders.
- **Ashen Fall / Ashen Century** — the Edictbound catastrophe caused by the Twin Restorations and its century of famine, cold, displacement, and collapse.
- **Concord of Ash** — the settlement that bans Edict technology, protects survival infrastructure, and makes sanctioned Passages preferable to unrestricted harness war.
- **The Lists** — the consecrated dueling grounds ([`ARENA_ROSTER.md`](ARENA_ROSTER.md)).
- **A Passage of Arms** — a roguelite run: the medieval term for holding ground against all comers. The circuit of sanctioned Passages is **the Grand Passage**.
- **Lasthold** — the hub city, seat of the Grand Passage: the Herald's hall, the hangar and test-yard, the Wrightsguild works, and the stewardship shops. The walkable shell of the JRPG-structured game (`GAME_DESIGN.md` §26).
- **Laurels** — victory honors (the future scoring layer: gold/silver/bronze by speed and remaining vigor, from the source's trophy system).
- **Relic-pattern / Edictbound / Scrapwright** — the three armory tiers ([`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md) §1): lawful isolated Firstwork; parts retaining active Last Edict command logic; and new-made human work. Common speech calls the middle tier **Edict-parts** or **illegal parts**. Scrapwright work is the boring, dependable floor — tough, cheap to repair, easy to fit, and the only tier humanity truly owns.
- **Rebirth Protocol** — an empire-scale continuity system whose local harness fragment produces the knockdown → rebirth flare.
- **Spoils of War** — the victor's claim on the loser's arms (built: the run's spoils draft).

## The Grand Passage — tournament culture

The Orders keep the peace the only way a mech-feudal world can: **ritualized single combat**. The Grand Passage is the sanctioned circuit of Passages of Arms; win one and you have an Order's attention, win several and you have its fear. The current 5-fight run is one Passage. The source material's tournament variety maps onto future **run modes**, kept here as the backlog:

| Mode | Source basis | Shape |
|---|---|---|
| **Laurels** | Task scores / bronze-silver-gold trophies | Score runs by speed + remaining vigor; three laurel thresholds per Passage |
| **Vow of Temperance** | Single-use-parts tournaments | Each part may be equipped for only one fight per run — forces breadth across the armory |
| **Shieldbrother Passage** | 2-on-2 partner battles | An AI ally with their own harness; ally bombs/pods still friendly-fire, per the source's rule |
| **Trial by Ordeal** | Handicap battles | 1v2 fights at reduced enemy skill; mercy rule lowers enemy vigor after repeated defeats |
| **Twin Harness** | Tag battles | Bring two loadouts, switch mid-fight on the lock-on button; each harness's remaining vigor scores |
| **The Melee** | Battle royal | 1v3 free-for-all — the medieval melee, revived; aim discipline and opportunism decide it |

### The circuit, feature-complete (2026-07-18)

The full sanctioned circuit, mapped event-for-event from the source's tournament ladder (venues per [`ARENA_ROSTER.md`](ARENA_ROSTER.md); rules per [`COMBAT_DOCTRINE.md`](COMBAT_DOCTRINE.md) §8 — task scores, bronze/silver/gold laurels, −10% per rematch, Edictbound parts halve laurels, mercy rule at 75/50/25%):

| Event | Source basis | Format |
|---|---|---|
| **The Hearthside Tilt** | Tea Room Tournament | Novice passage, no restrictions — hosted by Old Walther at his own hearth |
| **The Cup of Tempered Hearts** | Steel Hearts Cup | Vow of Temperance (each part fights once) |
| **The Almsbowl** | Noodle Bowl | Vow of Temperance, fought entirely in the Basin |
| **The Wardens' Muster** | Police 2-on-2 | Shieldbrother, partnered with Warden Linnet |
| **The Trial of Simulacra / of Wings** | Computer & Flying CPU Battles | Singles vs. the Lists' own sparring phantoms — ancestral training ghosts still running in the machine |
| **The Twin Harness Trial** | Tag Battle Tournament | Twin Harness |
| **The Guildhall Proofs / the Ordeal of Two / the Wrightmother's Favor** | Lab single / handicap / bonus | Wrightsguild-hosted: singles, 1v2 ordeal, and partnered bonus bouts |
| **The Sisters' Fusillade** | Park Dance Battle | Six duels against the Litany Sisters — every foe carries the Litany |
| **The Taproom Compact** | Bogey's 2-on-2 Festival | Shieldbrother, partnered with Ser Ernust |
| **The Masquerade of Blades** | Mira's Battle Party | Vow of Temperance among high society; every foe is fast |
| **The Surveyor's Circuit** | Holosseum Test | Singles across newly consecrated Lists |
| **The Broken Choir's Gauntlets** | Eliza's Room, Shiner, Isabella's Mansion, Oboro, Z Boss Room | The antagonist arc: partnered and single gauntlets in the Choir's holdings, foes fielding Edictbound arms openly |
| **The Bronze Ordeal** | Bronze Handicap Match | 1v2 ordeals; entry needs bronze laurels across the circuit |
| **The Silver Melee** | Silver Battle Royal | 1v3 free-for-alls; entry needs silver laurels |
| **The Golden Passage** | Gold Single Battle | Eight duels against the Paragons of the Passage (Class S armigers, empowered harnesses); entry needs gold laurels. It ends with the **First Armiger** (below) |

## Characters

### The five rivals of the standard Passage (draft canon, deepened 2026-07-18)

Built in `RunOpponents.cs`; same escalating loadouts, now with doctrine and character. Each carries their signature spoil.

1. **Bannerlord Cassian** — *The Aureate Legion.* The Legion's examiner: every sanctioned Passage opens against a bannerlord, and Cassian measures challengers by the book — courteous, incorruptible, and open about exactly what he will do, because the book is that good. Doctrine: line discipline (Bannerman, Arbalest + Censer). Spoil: **Arbalest**.
2. **Skald Maren** — *Order of the Winter Wing.* The Wing's skalds sing their dead into memory, and Maren duels to earn verses — hers and yours. Talks in meter mid-fight; strikes and is gone. Doctrine: harrying flight (Vesper, Litany + Censer, Courser Greaves). Spoil: **Litany**.
3. **Vesk the Unseen** — *The Umbral Concordat.* Nobody has seen Vesk's harness unpainted, and nobody has heard Vesk waste a word. The Concordat sells certainty; Vesk fights to collect yours — every duel is reconnaissance for something unstated. Doctrine: veiled blade (Duskmantle, Misericorde + Ward Veil). Spoil: **Misericorde**.
4. **Warden Aldric** — *The Rust Cross Commandery.* A gate-warden who has never lost ground he was set to hold. Kind off the field; on it, a wall with a Bombard behind it (Cobalt Knight, Bombard + Pavise). Spoil: **Pavise**.
5. **Grandmaster Otho** — *The Rust Cross Commandery.* The old master who knighted half the circuit, fighting with everything at once — and faster than a Cobalt Knight has any right to be (Dolorous Maul + Pavise, Courser Greaves). Otho believes the Grand Passage exists to find someone worthy of what's coming. He does not say what's coming. Spoil: **Dolorous Maul**.

### The armiger roster (2026-07-18 — expansion pool for runs and circuit events)

Mapped from the source's commander cast, filtered through the Orders. Doctrine names per `COMBAT_DOCTRINE.md` §9's AI archetypes; loadouts reference `ARMORY_REFERENCE.md` patterns. This is the pool future rival-roster passes draw from; the five built rivals above stay the canonical first Passage.

| Armiger | Allegiance | Harness & doctrine |
|---|---|---|
| **Old Walther** | Circuit (host) | Skyanvil War, Dragoon — the retired champion who teaches by beating you politely |
| **Dame Lucet** | Kurultai Vanguard | Skyanvil Chase, Alembic — cheerful, unhurried, unmovable |
| **Wenna Quickstep** | Hedge Errantry | Harrier Field, Splintered Star — never stops moving, never commits |
| **Wilhelm of the Arc** | Kurultai Vanguard | Skyanvil Field, Arcus Dexter — accurate around every corner |
| **Carmine** | Wrightsguild | Cobalt Field, Quillon Bolt — a wright who fights with what she builds |
| **Ser Ernust Ironhand** | Rust Cross | Cobalt War, Longshrift — the dependable shieldbrother; charges more than he shoots |
| **Warden Linnet** | City Wardens | Vesper Field, Thornswarm — your first partner; rebuilds her kit every muster |
| **Roald the Close** | unsworn | Duskmantle Chase, Petronel — the point-blank predator who stalks rebirth flares; hated, undefeated at arm's length |
| **Marzia of the Thornswarm** | Solarian Talon | Sunplume Field, Thornswarm — plants herself and darkens the sky |
| **Mira of the Masquerade** | Solarian Talon (host) | Sunplume Chase, Wending Bolt — society's favorite blade |
| **Sir Haralt** | Aureate Legion | Bannerman Chase, Arcus — Legion-precise, laurel-hungry |
| **Sofiya** | Solarian Talon | Sunplume Chase, Skysword — fights from above cover on principle |
| **Dain the Gauntlet** | Hedge Errantry | Harrier War, Gauntlet — one punch, honestly thrown |
| **Warden-Chief Ossian** | City Wardens | Cobalt Chase, Evenfall — the law, patiently descending |
| **Maxen** | Rust Cross | Cobalt War, Evenfall — a wall that rains |
| **Melvas** | Umbral Concordat | Duskmantle Field, Spur Volley — accelerates out of nowhere |
| **Brother Golias** | Circuit (jester) | Skyanvil War; Goliath Shot, Goliath Charge, Goliath Ward — the all-Goliath fool of the Lists, beloved, occasionally victorious |
| **Tressa** | Hedge Errantry | Harrier Chase, Dragoon — a heavy gun on the lightest frame; all or nothing |
| **Sister Maren-Vale, and the Litany Sisters** | Litany Sisters | Six sisters, six frames, one gun — the Fusillade's roster |
| **Boge** | Circuit (host) | Skyanvil Chase, Cinquefoil Sinister — the taproom's own champion |
| **Fell** | Hedge Errantry | Freelance Field, Yoke — a cursed reputation, a loyal partner in the Guildhall bouts |

### The Broken Choir (antagonists)

The named fighters below are the circuit-facing edge of a much larger apocalyptic movement. The Choir openly steals, studies, implants, and fields Edict technology. Its Quietists, Reconciliationists, Claimantists, Executors, and Last Verse apocalyptic wing are defined in [`RELIGION_AND_IDEOLOGY.md`](RELIGION_AND_IDEOLOGY.md). Individual masters need not share the same interpretation of Rebirth.

- **Obron the Manifold** — the Choir's master. The Manifold Shadow (five vanish dashes), the Waxing Moon, the Eclipse Gait. Courteous, unhurried, five places at once.
- **Eliset of the Stilled Voice** — fights with the Stilled Voice and the Raven's Step; her duels end in silence. Fields twin loyalties the Choir doesn't know about.
- **Isabeau the Twinned** — two identical Choir Aloft harnesses, pink and purple; nobody has ever proven which one holds her.
- **Cinder** — a fallen Winter Wing skald flying the Elder Wyrm; Skald Maren's struck-from-the-rolls sibling-in-verse. The Choir's blade when words fail.
- **Sergei the Grieving** — bearer of the Grieving Wing; joined the Choir the day the Order buried his wing-mates. The tragedy recruit.
- **The First Armiger** — the final duel of the Golden Passage: the player-armiger's own master and kin, fielding **the Martyr** and **the Burning Saltire** — Edictbound arms taken up deliberately, to be the wall their student must be able to break before facing what waits in the Cradle.

### Supporting figures

- **The Herald of the Lists** — the voice of every Passage: casts the harnesses (`COMBAT_DOCTRINE.md` §1), announces fights, proclaims laurels, remembers every armiger who ever fought. The diegetic wrapper for run UI/narration.
- **Wrightmother Sella** — mistress of the Wrightsguild; builds the dependable tier with her own hands and repairs relics she'll never be allowed to own. Future shop/upgrade vendor, and the voice of the setting's central wound: humanity fights with what it can no longer make.
- **The Riderless** — an intact imperial executor harness awaiting completion of the Rebirth Protocol. It fights with no acknowledged pilot. The Orders deny it exists; the Broken Choir treats it as herald or proof-text; the Whole Crown may see its Recognition as the final test of sovereignty. Edictbound and violet-black against everyone else's godlight, it waits in the Cradle (`ARENA_ROSTER.md`). Its shed pieces are the Shard/Voice/Hand/Shadow-of-the-Riderless parts. Its apparent sapience remains unresolved: *if nobody alive understands these machines, who says they are empty?*

## What this changes for the Unity rebuild

- The already-built **Cobalt Knight** asset (`docs/KNIGHT_ROBOT_ASSET.md`) needs no rework — its heavy silhouette, chest-cross emblem, and cobalt/ivory/aurum "ancestral relic" material language already read as an elite Order's pristine ancestral-tech chassis under this theme. Only its in-fiction framing changes (Crusader Knight → **The Rust Cross Commandery**).
- Anything built from here on should default toward the Order names/table above rather than the old 1:1 historical-culture labels, and should distinguish Order-tier mechs (coherent, well-kept) from everyone-else-tier mechs (visibly patchwork/salvaged) as a deliberate visual language, not just incidental variety.
- `docs/GAME_DESIGN.md` §16 logs this pivot; `prompts/MASTER_BUILD_PROMPT_UNITY.md`'s required reading has been updated to point here.
- **2026-07-18 theming pass**: the catalog display names now follow this setting ([`ARMORY_REFERENCE.md`](ARMORY_REFERENCE.md) — the canonical parts/naming roster), arenas have a themed roster ([`ARENA_ROSTER.md`](ARENA_ROSTER.md)), and art/audio/VFX direction is canonized in [`ART_DIRECTION.md`](ART_DIRECTION.md). The faction table above remains the standing Order canon; the mecha-knight focus (Knights in Powered Armor — Escaflowne, Imperial Knights, Knight's & Magic, Break Blade) is now an explicit design pillar via `ART_DIRECTION.md`, extending the Touchstones list.
- **2026-07-21 canon consolidation**: [`WORLD_HISTORY.md`](WORLD_HISTORY.md) replaces the unspecified-cataclysm placeholder with the Last Edict chronology; [`RELIGION_AND_IDEOLOGY.md`](RELIGION_AND_IDEOLOGY.md) locks the religious institutions and their subfactions; **Edictbound** supersedes **Hushforged** as the illegal part tier. The Long Hush remains the name of the post-Severance era.
