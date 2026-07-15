# Setting & Factions — Neo-Feudal Used Future

**This document supersedes [`WARBAND_THEME_REFERENCE.md`](WARBAND_THEME_REFERENCE.md) as the canonical world/theme reference (2026-07-15).** The old doc is kept for historical record only — its chassis mechanics and mesh work stay valid, but its framing (each chassis as a literal single Earth historical culture, spanning ancient Rome through the Viking Age) is replaced by the setting below.

## Logline

Centuries after a cataclysm nearly erased humanity, the survivors clawed their way back from scattered tribes to warring nations — and inherited their ancestors' war-mechs along with the ruins. Nobody alive remembers how to build one from nothing. Everybody who matters knows how to fight in one.

## The Cataclysm ("The Long Hush")

Something ended the old world hundreds — maybe thousands — of years ago. Nobody agrees on what. War, plague, the machines themselves turning, divine punishment for pride — every faction's temple, tribunal, and tavern has its own half-true version, and none of them can be fully confirmed (`Cataclysm Backstory`, `From Cataclysm to Myth`). What's certain is only the aftermath: cities became ruins, records became rumor, and the old world's Humongous Mecha survived intact far longer than any government that once fielded them. **Keep the true cause of the Long Hush unresolved by design** — it's a standing mystery hook for future story content, not a gap to fill in immediately.

## The Long Recovery

Humanity's path back followed the shape every `After the End` / `Standard Post-Apocalyptic Setting` society takes: survivor bands, then tribes, then clans, then petty kingdoms carving up the ruins — and, centuries on, that process has stabilized into a genuine `Feudal Future`: countries, Great Houses, chartered Knightly/Military Orders, and free mercenary companies, all fielding war-mechs as the deciding weapon of statecraft. This isn't Medieval Stasis by accident — losing the old world's industry *forced* a return to older social forms, because only inherited rank, oath, and commandery structure could organize enough people to keep a handful of ancestral machines running at all.

## Lost Technology & Low Culture, High Tech

Nobody left alive can build a war-mech from raw materials. What survives is knowledge of *use and upkeep*, unevenly distributed:

- **The great Orders** hold the best-preserved ancestral chassis, generations of accumulated maintenance lore, and enough resources to keep their machines close to factory condition — polished, matched armor, coherent silhouettes.
- **Everyone else** — lesser houses, mercenary free companies, warlord levies — fields whatever survived and could be dragged home: mismatched plating, welded-on scavenged limbs, jury-rigged weapon mounts. This is the in-fiction *reason* for a literal `Used Future` art direction, not just a palette choice: every non-Order mech should visibly be a patchwork of salvage, while Order-issue chassis read as clean, coherent, and deliberately superior.

Nobody — Order or otherwise — truly understands the deepest systems. That's the direct explanation for **"Rebirth Protocol" itself**: the knockdown → rebirth invincibility cycle (`GAME_DESIGN.md` §2.2) isn't a modern safety feature anyone designed. It's a failsafe subroutine baked into the ancestral chassis core, present in every surviving war-mech, that nobody currently alive knows how to disable, replicate, or fully explain — only trigger. The game's title and its core comeback mechanic are the same piece of half-understood Lost Technology.

## Factions & Chassis (draft — confirm before treating as final canon)

Existing chassis `.id`s and built geometry stay as-is (no re-export needed). What changes is which in-fiction Order each represents, and why. Per your framing ("military orders from the 1100–1500s, and other feudalistic societies/military regimes/orders/mercenary forces"), I nudged the two chassis that sat outside that window (ancient Rome, pre-1100 Viking Age) toward adjacent medieval institutions that keep the same silhouette identity already built. Flag anything here you'd rather keep closer to the original culture.

| `.id` | Old label (superseded) | Proposed Order | Real-world inspiration (not a copy) | Status |
|---|---|---|---|---|
| `bulwark` | Crusader Knight | **The Rust Cross Commandery** | Templar/Hospitaller/Teutonic crusading orders — "commandery" is the real term for a local chapter-house | Built (Cobalt Knight asset) |
| `vanguard` | Legionnaire (Rome) | **The Aureate Legion** | Byzantine heavy line-infantry — Rome's actual medieval continuation, keeps disciplined-formation identity inside the window | Built |
| `skylance` | Valkyrie (Norse) | **Order of the Winter Wing** | Baltic Crusades / Livonian sword-brother orders (1202–1500s) — Northern European, keeps the flier/valkyrie identity | Built |
| `wraith` | Shinobi (Japan) | **The Umbral Concordat** | Generic hidden mercenary/covert order, deliberately not tied to one real nation — keeps the "no glow, built not to be seen" identity | Built |
| `corsair` | Pirate | **The Drowned Compact** | Free Companies / routiers (14th-c. mercenary bands) crossed with coastal raider culture | Not built |
| `halcyon` | Aztec Eagle Warrior | **The Solarian Talon** | Aztec Eagle/Jaguar Warrior societies (1428–1521) — already a real historical *military order*, barely needs reframing | Not built |
| `juggernaut` | Mongol Kheshig | **The Kurultai Vanguard** | Mongol Kheshig imperial guard (1206–1368) — "kurultai" is the real term for a council of chieftains | Not built |

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

## What this changes for the Unity rebuild

- The already-built **Cobalt Knight** asset (`docs/KNIGHT_ROBOT_ASSET.md`) needs no rework — its heavy silhouette, chest-cross emblem, and cobalt/ivory/aurum "ancestral relic" material language already read as an elite Order's pristine ancestral-tech chassis under this theme. Only its in-fiction framing changes (Crusader Knight → **The Rust Cross Commandery**).
- Anything built from here on should default toward the Order names/table above rather than the old 1:1 historical-culture labels, and should distinguish Order-tier mechs (coherent, well-kept) from everyone-else-tier mechs (visibly patchwork/salvaged) as a deliberate visual language, not just incidental variety.
- `docs/GAME_DESIGN.md` §16 logs this pivot; `prompts/MASTER_BUILD_PROMPT_UNITY.md`'s required reading has been updated to point here.
