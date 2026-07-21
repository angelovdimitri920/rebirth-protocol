# The Armory — Canonical Parts Roster & Naming Reference v2

*Feature-complete roster revision (2026-07-18), with world-canon terminology consolidated 2026-07-21. v1 mapped the source material to archetype families; **v2 is the full catalog**: every body, gun, bomb, pod, and leg part in the source lists (Parts FAQ, parts list w/ stat cards, Weapon Damage Guide) has a themed counterpart here with stats, and — per standing directive — **every gun has a melee counterpart and every bomb a shield counterpart**. Sources are inspiration only: every name is original, all numbers are recalibrated to our scale (which conveniently shares the source's 1000-vigor baseline). Companion docs: [`COMBAT_DOCTRINE.md`](COMBAT_DOCTRINE.md) (how battles work), [`ARENA_ROSTER.md`](ARENA_ROSTER.md) (the Lists), [`SETTING_AND_FACTIONS.md`](SETTING_AND_FACTIONS.md) (world), [`WORLD_HISTORY.md`](WORLD_HISTORY.md) (Edict technology), and [`ART_DIRECTION.md`](ART_DIRECTION.md) (look/sound).*

---

## 1. The three tiers (revised)

| Tier | In-fiction meaning | Source analogue | Game identity |
|---|---|---|---|
| **Relic-pattern** | Ancestral parts maintained by the Orders. Nobody can build one; every armory knows its pattern-name. | The legal roster | The standard catalog (§4–§10) |
| **Edictbound** | Firstwork retaining active command logic, interfaces, or propagation behavior from the Last Edict. Banned under the Concord of Ash; coveted anyway. It glows wrong and may alter permissions, memory, machinery, or the pilot. Common terms: **Edict-part**, **illegal part**. | "Illegal" parts (Rahu / Penumbra / Wyrm tier) | Endgame drops & unlocks: stronger, corrupted, each with a real drawback. **Allowed to break frame directives** (the 2-dash cap, etc.) — that is why they are banned. Using one halves your laurels (`COMBAT_DOCTRINE.md` §8) |
| **Scrapwright** | **New-made, human-designed, human-maintained.** The lowest tier — and the only one people truly *own*. Not a joke: scrapwright work is boring, honest, and dependable. It can't match a relic's edge or an Edictbound part's wrongness, but it takes abuse, repairs cheap, and fits any harness without a fuss. | The "Can" tier, reimagined | The dependable floor: high SOAK/POISE, modest MIGHT, zero gimmicks, and unique **dependability perks** (§1.2). Maintained by the Wrightsguild |

### 1.1 Naming language (unchanged from v1)

No Earth-culture names. Relic-patterns use plain feudal armory words plus at most one epithet, drawn from slot vocabularies: guns = archery/siege/liturgy; melee = knightly arms and votive words; bombs = siege charges and ritual objects; shields = historical shield types; pods = retainers, hounds, and falconry; legs = "<X> Greaves" (mounts, birds, gaits); bodies = pattern-names evoking their keeper Order. Mirrored left/right parts are single entries with **Dexter/Sinister** variants (the heraldic terms). Edictbound catalog names remain hushed, personal epithets (*the Grieving Wall*) because the part behaves less like a reproducible pattern and more like a singular affliction; scrapwright names are plain workshop words (*Matchlock*, *Boilerplate*).

### 1.2 Scrapwright design rules (new)

Every scrapwright part follows the same identity: **worse peaks, better floors.**
- Stats lean toward POISE/SOAK/reliability; MIGHT and refinement stats sit a notch below relic equivalents.
- No gimmick behaviors — straight shots, flat swings, square shields.
- Each carries one **dependability perk** (see entries): rounds that survive your knockdown, free full repair between fights, sure footing on hazards, an uninterruptible swing, a short toll. Dependability *is* the gimmick.
- **Fitting**: scrapwright parts have minimal Burden (§2.6) — they fit anything, instantly. Relic parts strain a harness; Edictbound parts torture one.

---

## 2. Combat-frame directives (canon)

Carried from v1, with two new systems:

1. **Arm pairings** (built): right arm gun XOR melee; left arm bomb XOR shield. Four doctrines — gun/bomb, gun/shield, melee/bomb, melee/shield — all must stay viable, hence counterpart parity throughout this doc.
2. **Air-dash cap** (applied): no relic body grants more than 2 air dashes; extra dashes come from legs only. Edictbound bodies may break this.
3. **Shield toll + raise behavior** (built, Pass B): lowering/breaking a shield starts its toll; raise behaviors are Root / March / Air-hold / Air-drop (§7).
4. **Damage formula** (built): weapon MIGHT × attacker offense mult × defender ward mult. Offense mults cluster (95–105%, Edictbound outliers to 150%); ward mults spread (85–138%). Per-garniture ward mults are in §4.
5. **Tempers v2** (to build): the source's 11 blast letters distill into **six directions plus one modifier**. Direction: **Sunder** (diagonal launch — default), **Sweep** (hard sideways), **Hoist** (straight up), **Hook** (pull toward owner), **Fetter** (stun in place), **Unhorse** (guaranteed knockdown). Modifier: **Branded** (the blast lingers as a standing blaze). A bomb/pod entry lists which tempers it may be fitted with; Branded may combine with Sunder/Sweep/Hoist.
6. **Burden & Fitting** (future system): every part has a Burden cost; every chassis a Fitting limit. Relic parts are moderate, Edictbound heavy, scrapwright near-zero. This is the mechanical home of "scrapwright is much easier to fit" and a future build-constraint lever — flagged, not scheduled.
7. **Garnitures** (new, from the source's Normal/Armor/Speed styles): a chassis pattern comes in up to three garnitures — real medieval term for a harness's exchangeable fittings. **Field** (the baseline), **War** (armored lean), **Chase** (swift/keen lean). One mesh per pattern; garnitures are stat trims + small cosmetic deltas (plate density, crest length), which is how 25 bodies stay affordable.

Stat bars are 0.5–5.0. Slot bar sets: Body **POISE/WARD/GAIT/WING/MIGHT** · Gun **MIGHT/BOLT/SEEK/CADENCE/REND** · Melee **MIGHT/REACH/TEMPO/GRACE/REND** · Bomb **MIGHT/LOFT/BREADTH/LINGER/REND** · Pod **MIGHT/HASTE/SEEK/BREADTH/LINGER** · Shield **GUARD/SOAK/MEND/TOLL/RIPOSTE**. "Volley" columns give total damage if every round of one trigger-pull lands, on the shared 1000-vigor scale — calibration targets, not final tuning.

---

## 3. Bodies — 9 patterns, 25 relic garnitures

Charge = the collision/charge attack (see `COMBAT_DOCTRINE.md` §4.5). Off% = offense mult; Ward% = incoming-damage mult (lower is tougher).

### Bannerman — The Aureate Legion *(built: `vanguard` = Field)*
The standard every pattern is measured against. 2 air dashes. Off 100%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 2.5 | 3.0 | 2.5 | 2.5 | 3.5 | 107% | Straight charge |
| War | 2.5 | 3.5 | 2.0 | 2.5 | 4.0 | 101% | Rising charge, clears walls |
| Chase | 2.5 | 2.5 | 3.0 | 2.5 | 4.0 | 111% | Diagonal rising strike |

### Sunplume — The Solarian Talon *(planned; was "Halcyon")*
Never quite touches the ground: two continuous jumps instead of dashes; charges clear walls. Off 95%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 2.0 | 2.0 | 3.0 | 3.5 | 2.5 | 117% | Short-hop charge |
| War | 2.0 | 2.5 | 2.5 | 4.0 | 2.5 | 111% | Diagonal rising strike |
| Chase | 2.0 | 1.5 | 3.0 | 3.5 | 3.0 | 122% | Hop-and-strike |

### Cobalt Knight — The Rust Cross Commandery *(built: `bulwark` = Field; rigged asset)*
The ancestral wall. 1 air dash. Off 105%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 5.0 | 4.0 | 1.5 | 1.5 | 2.0 | 95% | Diagonal rising strike |
| War | 5.0 | 4.5 | 1.0 | 1.5 | 5.0 | 90% | Devastating straight charge |
| Chase | 5.0 | 3.5 | 2.0 | 1.5 | 4.0 | 101% | Vertical rise then charge, clears walls |

### Harrier — The Hedge Errantry *(new pattern)*
The landless knights' pattern: extreme ground speed, three continuous jumps, folds to a stiff breeze. Off 95%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 0.5 | 1.0 | 4.5 | 2.5 | 1.5 | 132% | Short-hop charge, clears walls |
| War | 0.5 | 1.5 | 4.0 | 2.5 | 2.0 | 128% | Diagonal rising strike |
| Chase | 0.5 | 0.5 | 5.0 | 2.5 | 2.5 | 138% | Straight charge |

### Duskmantle — The Umbral Concordat *(built: `wraith` = Field)*
Vanish-dashes phase through shots; 2 dashes (cap). Slow afoot. Off 100%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 2.5 | 3.0 | 2.0 | 2.0 | 1.0 | 107% | Repeating short charges (no guard) |
| War | 2.5 | 3.5 | 1.5 | 2.0 | 2.0 | 101% | Feinting jump, drifts back |
| Chase | 2.5 | 2.5 | 2.5 | 2.0 | 4.0 | 111% | Straight charge |

### Freelance — The Drowned Compact *(planned; was "Corsair")*
The mercenary hybrid: strong air game on short elaborate dashes (2 — the cap trims the source's three), sluggish afoot. Off 100%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 2.5 | 2.5 | 1.5 | 4.0 | 3.0 | 111% | Charge, drifts up after impact |
| War | 2.5 | 3.0 | 1.5 | 3.5 | 4.0 | 107% | Vertical rise-and-return slam |
| Chase | 2.5 | 2.0 | 1.5 | 4.5 | 2.5 | 117% | Rising dive onto the foe below |

### Vesper — Order of the Winter Wing *(built: `skylance` = Field)*
One long steerable dash; strikes hard, folds fast. Off 100%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 1.5 | 1.5 | 1.5 | 4.0 | 4.5 | 128% | Slow gliding charge |
| War | 1.5 | 2.0 | 1.5 | 3.5 | 4.5 | 122% | Ascending charge |
| Chase | 1.5 | 1.0 | 1.5 | 4.5 | 4.5 | 132% | Slow rising approach |

### Skyanvil — The Kurultai Vanguard *(planned; was "Juggernaut")*
Heavy *and* airborne: two continuous jumps, superb air control, slow everything else. Off 100%.
| Garniture | POISE | WARD | GAIT | WING | MIGHT | Ward% | Charge |
|---|---|---|---|---|---|---|---|
| Field | 3.5 | 4.5 | 1.5 | 4.5 | 3.0 | 90% | Slow grinding advance |
| War | 3.5 | 5.0 | 1.5 | 3.5 | 3.0 | 85% | Diagonal rise, descends after impact |
| Chase | 3.5 | 4.0 | 1.5 | 5.0 | 4.0 | 95% | Pounce from on high, clears walls |

### Cockatrice — masterless pattern *(new; single garniture)*
A found relic no Order will claim: stealth-glide (one long cloaked glide-dash), superb jump, and a bouncing repeatable pounce charge. POISE 2.5 / WARD 2.5 / GAIT 1.5 / WING 3.5 / MIGHT 2.5 · Off 100% · Ward% 111.

### Edictbound bodies
| Name | Source basis | What it breaks | Drawback |
|---|---|---|---|
| **the Martyr** | Ray Legend | Off **150%** — nothing hits harder | Ward% **170** — nothing dies faster; its charge stumbles backward |
| **the Paragon** | Ray Warrior | POISE/WARD/MIGHT all 4.5+, Off 110% | Merely mortal mobility; the Orders hunt its bearers hardest |
| **the Manifold Shadow** | Rakensen | **5 vanish dashes** (cap broken) | Mediocre everything else; the pilot hears footsteps that aren't there |
| **the Grieving Wing** | Ruhiel | Vesper frame with real armor and air | It sings while it fights; pilots stop sleeping |
| **the Choir Aloft** | Athena | **6 continuous jumps** | Feather-fragile; twin examples exist and know each other |
| **Shard of the Riderless, First / Second / Third** | Rahu I / II / III | Escalating everything; the Third: near-max all bars, Ward% **22**, cannot be knocked down, rebirth is instant | It is a piece of the Riderless. It remembers being whole |

### Scrapwright body — **Plowshare**
The Wrightsguild's pattern: over-built, under-glamored. POISE 4.0 / WARD 3.5 / GAIT 2.0 / WING 1.0 / MIGHT 2.0 · Off 100% · Ward% 100 · 1 dash · Charge: honest straight shove. **Perk:** repairs between fights cost nothing; near-zero Burden.

---

## 4. Guns — 38 relic patterns (+10 Edictbound, +1 scrapwright)

Bars = MIGHT/BOLT/SEEK/CADENCE/REND. Volley = all-rounds-hit damage at medium range (melee-range or far values noted when the weapon's identity lives there). G/A = ground/air behavior differs.

| Name | Source | Bars | Range | Volley | Identity | Melee counterpart |
|---|---|---|---|---|---|---|
| **Arbalest** *(built)* | Basic | 3/2/1/2/2 | Med | 88 | Three honest bolts; the workhorse | Oathblade |
| **Trefoil** | 3-Way | 3/3/3/3/2 | Med-long | 67 | Three streams in a heraldic fan; better from afar | Longglaive |
| **Litany** *(built)* | Gatling | 2/3/1/3/3 | Med | 88 | Eight recited rounds; power grows with distance, accuracy shrinks | Misericorde |
| **Mangonel** | Vertical | 2/3/3/2/4 | Med | 76 | Two straight, two vaulting — the vault clears walls | Estoc |
| **Longshrift** | Sniper | 3/5/1/1/4 | Med-long | 84–105 | One fast round and a long vulnerable breath after | Tilt Lance |
| **Fetterlock** | Stun | 2/5/1/5/4 | Short | 53 | Two shackle-rounds; near-guaranteed down up close | Knell Maul |
| **Thornswarm** | Hornet | 3/2/4/2/3 | Med | 130 | Five chasing thorns; layer volleys to smother | Hydra Flail |
| **Pilgrim** | Flame | 3/3/2/3/4 | Med | 114 far | Six rounds that grow stronger the farther they travel | Courser Saber |
| **Dragoon** | Dragon | 4/2/3/1/4 | Med-long | 114 | One heavy tracking round; slow, patient, punishing | Wyrmtail |
| **Aspergill** | Splash | 1/2/1/5/2 | Sh-med | 19 | A sprinkling of stopping rounds; setup, not damage | Tocsin Mace |
| **Arcus (Dexter/Sinister)** | L/R Arc | 3/4/2/2/3 | Med-long | 73 | Two rounds curving in from the named side; reversed airborne | Backsword (D/S) |
| **Culverin** | Shotgun | 5/5/1/1/5 | Short | 142 pt-blank | Three-throat blast; downs anything, dies at range | Pollaxe |
| **Evenfall** | Rayfall | 3/3/4/2/2 | Long | 84 | Four rounds hang at even-fall, then descend homing (G only) | Pendulum Glaive |
| **Alembic** | Bubble | 2/1/2/3/4 | Sh-med | 84 | Fat slow alchemic orbs; two afoot, one aloft | Leaden Cudgel |
| **Gyrfalcon** | Eagle | 2/4/2/3/3 | Med-long | 33 G / 48 A | Straight afoot; aloft it pauses, then stoops | Stooping Talon |
| **Chevron** | V Laser | 3/4/1/3/4 | Med-long | 152 pt-blank / 76 | Fires a heraldic V afoot, a straight lance aloft | Scissor Glaive |
| **Petronel** | Magnum | 4/5/1/1/5 | Short | 126 | One brutal cavalry-shot; toothless past arm's reach | Rondel Dagger |
| **Versicle** | Needle | 2/3/1/4/4 | Med | 57 | Three vertical verses; hoists the victim skyward | Rising Falchion |
| **Splintered Star** | Starshot | 2/3/2/4/2 | Long | 155 | One round splits five — vertically afoot, laterally aloft | Shatterblade |
| **Falconet** | Glider | 3/2/5/3/1 | Long | 56 G / 113 A | Aloft, its rounds glide wide loops and return for a second pass | Volant Falx |
| **Rookery** *(planned)* | Homing Star | 3/3/4/2/2 | Med-long | 256 | One casting breaks into seven homing rooks | Iron Rosary |
| **Vigil** *(planned)* | Trap | 3/3/2/4/1 | Med-long | 228 | Rounds keep watch, near-invisible, then strike (G); straight aloft | Penitent Flail |
| **Auger** | Drill | 3/5/2/3/4 | Short | 107 | A grinding stream that drags the victim up its own thread | Sawtooth Espadon |
| **Goliath Shot** | Titan (gun) | 1/1/1/2/3 | Med | 42 | An enormous, slow, mostly-ceremonial round | Goliath Blade |
| **Grapnel** *(planned)* | Claw | 1/3/5/4/1 | Med | 19 | Barbed lines that haul the target off their aim | Hookbill |
| **Gauntlet** | Knuckle | 5/5/1/5/5 | Pt-blank | 152 | A fired fist; hoists afoot, hurls away aloft | Cestus |
| **Spur Volley** | Afterburner | 4/4/2/2/3 | Med-long | 126 | Rounds that gather speed; pushes afoot, draws in aloft | Cavalcade |
| **Quillon Bolt** | Blade | 1/3/1/5/1 | Sh-med | 20–42 | No recovery at all — fill the space between other threats | Fencer's Foil |
| **Sparrowstorm** | Meteor Storm | 2/3/2/3/2 | Sh-med | 310 far | Sixteen sparrows; a nuisance close, a storm at distance | Threshing Flail |
| **Portcullis** | Twin Fang (gun) | 4/4/3/3/2 | Short | 153 | Rounds rise like a gate and drop behind cover (G); straight aloft | Gatefall Maul |
| **Yoke** | Gravity | 4/3/1/4/4 | Med | 38 G / 95 A | Hangs black weights over the foe — the sky stops being safe | Yoke Hammer |
| **Firecrest** | Phoenix | 3/5/2/4/3 | Long | 114 | Fast burning birds; bell-curve afoot, level aloft | Brandiron |
| **Processional (D/S)** | L/R Pulse | 2/3/4/3/2 | Med | 99–107 | Eight rounds processing in a curve from the named side | Serpentine Blade (D/S) |
| **Skysword** | Sword Storm | 3/5/2/4/3 | Med-long | 95 | Blades cast heavenward that fall on the mark — cover is negotiable | Steeple Strike |
| **Wending Bolt** | Ion | 3/5/5/5/2 | Med | 86 | A round that turns twice to find its man; slow but faithful | Feinting Rapier |
| **Beacon** | Flare | 4/4/2/3/3 | Med | 124 at burst | Bursts at a set distance; time the blossom or waste the shot | Crowbeak Pick |
| **Cinquefoil (D/S)** | L/R 5-Way | 2/4/1/4/3 | Med-long | 158–273 | Five streams fanning from the named side; reversed aloft | Quintain Sweep (D/S) |
| **Annulet** | Halo | 3/3/4/3/3 | Med-long | 69–85 | A great ring that climbs, then hunts (G); halts before you aloft | Roundelay |

### Edictbound guns
| Name | Source | Identity | Drawback |
|---|---|---|---|
| **the Stilled Voice** | Wave Laser | Rounds that *still* a harness for long seconds | Almost no damage; the silence spreads to the wielder's vox |
| **the Burning Saltire** | X Laser | Crossed beams that curve in from both flanks; strikes hiders | No straight shot at all |
| **the Choir Unending** | Crystal Strike | An endless spammable stream of high-damage shards (260/volley) | Falls off hard against true dodgers; the choir doesn't stop when you do |
| **the Elder Wyrm** | Wyrm | Four homing wyrms afoot (227), one swift round aloft | Fast harnesses shed the wyrms; it eats pilots' patience |
| **the Twin Stoop** | Raptor | Twin raptors that pause, then stoop (93–95) | Ground fire is slow and stiff |
| **the Waxing Moon / the Waning Moon** | Waxing/Waning Arc | Four homing rounds curving from dexter/sinister (113–168) | Reversed aloft; the moons wax and wane on their own schedule |
| **Voice of the Riderless, First/Second/Third** | Rahu 1/2/3 | Blooming blast-clusters, escalating (127→285 at burst); the Third works point-blank or across the List (222) | Each Voice speaks a little more clearly to the pilot |

### Scrapwright gun — **Matchlock**
Bars 3/1/1/2/2 · Med · 88/volley but rounds shrink in flight (22 far). Three plain rounds, no gimmick. **Perk:** its rounds are never wiped by your knockdown — a Matchlock volley in the air *finishes the job* even if you're already down (`COMBAT_DOCTRINE.md` §4.3's overload rule doesn't apply).

---

## 5. Melee — 38 relic patterns (+10 Edictbound, +1 scrapwright)

Bars = MIGHT/REACH/TEMPO/GRACE/REND. Each is the designed counterpart of its gun (§4); the pairing rule keeps every ranged answer matched by a blade answer.

| Name | Counterpart | Bars | Identity |
|---|---|---|---|
| **Oathblade** *(built)* | Arbalest | 3/3/3/3/3 | The knight's standard; balanced in every line |
| **Longglaive** | Trefoil | 3/4/3/2/3 | Wide crescent sweep (140°) — punishes strafing |
| **Misericorde** *(built)* | Litany | 2/2/5/5/2 | The mercy-dagger; fast, light, barely punishable |
| **Estoc** | Mangonel | 3/3/3/3/2 | Narrow thrust that pierces 60% of a raised shield's GUARD |
| **Tilt Lance** | Longshrift | 5/4/2/1/4 | The joust: damage scales with lunge distance (60→190) |
| **Knell Maul** | Fetterlock | 2/3/3/3/5 | A bell-hammer that tolls through poise (REND 130) |
| **Hydra Flail** | Thornswarm | 3/3/3/2/3 | Five heads strike five angles at once; some always connect |
| **Courser Saber** | Pilgrim | 3/3/4/3/2 | Damage scales with your current speed — never swing standing still |
| **Wyrmtail** | Dragoon | 4/4/2/2/4 | A heavy sweep that re-tracks mid-lunge; slow, faithful |
| **Tocsin Mace** | Aspergill | 1/2/5/4/3 | Light stopping taps; rings a foe still for the follow-up |
| **Backsword (D/S)** | Arcus | 3/3/3/3/3 | Curving cut that reaches around a guard's named side |
| **Pollaxe** | Culverin | 5/3/2/1/5 | The unhorser: short, brutal, downs anything it touches |
| **Pendulum Glaive** | Evenfall | 3/4/2/2/3 | The first swing hangs; the pendulum falls a beat later |
| **Leaden Cudgel** | Alembic | 3/2/2/2/4 | Fat slow blows with absurd stagger |
| **Stooping Talon** | Gyrfalcon | 3/3/4/3/3 | Aloft it becomes a plunging dive-strike |
| **Scissor Glaive** | Chevron | 3/3/3/3/4 | Twin blades cross in a V — wide afoot, needle-narrow aloft |
| **Rondel Dagger** | Petronel | 4/1/4/2/5 | One armor-splitting thrust at grapple range |
| **Shatterblade** | Splintered Star | 2/3/3/3/2 | The swing splinters into a fan of five edges |
| **Volant Falx** | Falconet | 3/4/3/2/1 | A looping crescent wave that returns for a second pass |
| **Iron Rosary** | Rookery | 2/3/4/4/2 | A weighted chain told bead by bead — the longest string (4+ hits) |
| **Penitent Flail** | Vigil | 4/3/2/2/3 | The arc is unreadable and the timing is late |
| **Sawtooth Espadon** | Auger | 3/3/3/2/4 | A grinding hold that ticks damage and drags the foe up the blade |
| **Goliath Blade** | Goliath Shot | 1/5/1/2/3 | Enormous, slow, mostly ceremony — but the toll when it lands! |
| **Hookbill** | Grapnel | 2/4/3/3/1 | The billhook that dragged knights from horses; hauls them to you |
| **Cestus** | Gauntlet | 5/1/5/5/5 | The fired fist's twin: hoists afoot, hurls aloft |
| **Cavalcade** | Spur Volley | 4/3/2/2/3 | Chained accelerating lunges — each faster than the last |
| **Fencer's Foil** | Quillon Bolt | 1/3/5/5/1 | Zero-commitment pokes to fill the space between threats |
| **Threshing Flail** | Sparrowstorm | 2/4/3/3/2 | Many weak strikes, strongest at the tip of its wide arc |
| **Gatefall Maul** | Portcullis | 4/3/2/2/2 | An overhead drop-gate strike — the anti-air swing |
| **Yoke Hammer** | Yoke | 4/3/2/2/4 | Spikes an airborne foe straight into the ground |
| **Brandiron** | Firecrest | 3/3/3/3/3 | A burning arc that leaves a lingering brand ticking damage |
| **Serpentine Blade (D/S)** | Processional | 2/3/4/3/2 | An eight-touch winding combo curving from the named side |
| **Steeple Strike** | Skysword | 3/3/3/2/3 | A leaping plunge that clears low cover entirely |
| **Feinting Rapier** | Wending Bolt | 3/3/5/5/2 | Feints, re-aims mid-swing; nearly impossible to read |
| **Crowbeak Pick** | Beacon | 4/3/3/3/3 | All the power lives in the beak's tip — space it or waste it |
| **Quintain Sweep (D/S)** | Cinquefoil | 2/4/4/3/3 | A five-point fan combo sweeping from the named side |
| **Roundelay** | Annulet | 3/3/3/2/3 | A full-circle spin — the answer to being surrounded |

### Edictbound melee
**the Hushed Edge** (↔ Stilled Voice — wounds don't hurt until seconds later, and neither do its costs), **the Saltire's Kiss** (↔ Burning Saltire — twin crossed cuts that strike both flanks), **the Verse Eternal** (↔ Choir Unending — the combo string that does not end while stamina holds), **the Wyrm's Tooth** (↔ Elder Wyrm — bites re-track like living things), **the Twinned Talon** (↔ Twin Stoop — every swing is two), **the Horned Moon / the Hollow Moon** (↔ Waxing/Waning — mirrored crescent arts), **Hand of the Riderless, First/Second/Third** (↔ the Voices — the Third simply *takes* what it touches).

### Scrapwright melee — **Felling Axe**
Bars 3/3/2/3/3. A woodsman's arc, nothing more. **Perk:** the swing cannot be interrupted — poise damage taken mid-swing never cancels it.

---

## 6. Bombs — 18 relic shapes (+4 Edictbound, +1 scrapwright)

Bars = MIGHT/LOFT/BREADTH/LINGER/REND. Dmg = single-blast calibration (ground / air where the source differs). Tempers list what the shape may be fitted with (§2.5); **B+** marks Branded-compatible.

| Name | Source | Bars | Dmg | Tempers | Identity | Shield counterpart |
|---|---|---|---|---|---|---|
| **Censer** *(built)* | Standard | 4/3/3/3/3 | 71/55 | Sunder, Sweep, Fetter, Unhorse, B+ | The swung vessel; tracks the foe | Ward Veil |
| **Peal Charge (D/S/True)** | Wave, L/R Wave | 3/2/2/3/3 | 25–35 | Sweep | Three tolling blasts that pass *through* walls | Quiet Bell |
| **Quarrel Charge** | Straight | 2/4/2/2/3 | 53/36 | Hoist, Fetter, Hook | Fast and flat; no arc, no mercy | Targe |
| **Oxbow Charge (D/S)** | L/R Flank | 3/3/4/2/3 | 63/44 | Sweep | Bends around cover from the named side | Pallium |
| **Oubliette Mine** | Burrow | 4/3/4/5/3 | 79/55 | Sunder, Hoist, B+ | Lands, waits near-invisible, remembers | Caltrop Ward |
| **Oubliette Twin** | Double Mine | 2/3/3/3/1 | 32 ×2 | Sunder | Two forgotten pits for the price of one throw | Caltrop Ward |
| **Rime Charge** | Freeze | 1/3/2/4/1 | 8 | Fetter | Almost harmless; holds the foe for the real blow | Hoarfrost Ward |
| **Steeplefall** | Tomahawk | 3/2/4/4/3 | 64–66 | Sweep, Hoist, B+ | Climbs past steeple height, falls straight down | Canopy Ward |
| **Pincer Charge** | Gemini | 2/3/2/3/2 | 42 ×2 | Sweep, B+ | Splits to blast both sides at once — sides afoot, fore-and-aft aloft | Echo Ward |
| **Anathema Charge** *(built)* | Submarine | 5/1/4/5/3 | 88/55 | Sunder, Hoist, Unhorse, B+ | The great condemnation: slow, vast, lingering | Pavise |
| **Crescent Charge** | Crescent | 3/1/3/4/3 | 52–72 | Hoist, Unhorse, B+ | A tall slow crescent that forbids the sky | Kite Ward |
| **Antiphon Charge** | Dual | 3/3/3/4/3 | 33–39 ×2 | Sunder, Hoist | Call and response: one blast before the foe, one behind | Echo Ward |
| **Ascension Charge** | Acrobat | 0/5/4/2/1 | 0 | — | Detonates at your own feet and casts *you* skyward | Springald Ward |
| **Trine Snare** | Delta | 2/4/1/2/2 | 31/22 ×3 | Sweep | Three points around the foe — moving is wrong, staying is worse | Cheval Ward |
| **Palisade** *(planned)* | Wall | 4/4/3/4/3 | 79/55 | Sunder, B+ | A stake-wall of blasts before you; charges die on it | Bastille |
| **Belfry Burst** | Smash | 4/5/3/4/3 | ~55 | Sunder | Detonates directly overhead — the sky above you is yours | Testudo Ward |
| **Widening Gyre** | Geo Trap | 4/4/4/4/3 | 76 | Sunder, B+ | Lands small and *blooms* — always wider than it looks | Thorn Ward |
| **Goliath Charge** | Titan (bomb) | 1/1/5/3/5 | 19 | Unhorse | An enormous, slow, humiliating blast | Cenotaph |

### Edictbound bombs
**the Threefold Grief** (Treble — three grown blasts, corner a foe and there is no out), **the Wyvern's Egg** (Wyvern — a long-flying egg that hatches ruin), **Moontide, Waxing / Waning** (Waxing/Waning Arc — curving tides with grown blasts), **the Ruin Cross** (Grand Cross — four enormous blasts in a cross about you, 76 each; stand still when you loose it).

### Scrapwright bomb — **Powder Keg**
Bars 3/3/3/2/3 · 60. An honest keg of black powder. **Perk:** the shortest toll in the armory — it is always, dependably, nearly ready.

---

## 7. Shields — 17 relic patterns (+4 Edictbound, +1 scrapwright)

Every bomb shape has its shield counterpart (§6, last column). GUARD = front block% (back% second). TOLL = seconds after lowering/break. Raise: Root / March / Air-hold / Air-drop (§2).

| Name | GUARD | SOAK | MEND | TOLL | RIPOSTE | Raise | Identity |
|---|---|---|---|---|---|---|---|
| **Ward Veil** *(built)* | 75/25% | 180 | 25/s | 2.5 | 20 | Air-hold | Light energy veil; the flier's shield |
| **Pavise** *(built)* | 92/40% | 260 | 6/s | 6 | 32 | Root, Air-drop | The great standing wall-shield |
| **Targe** *(built)* | 60/15% | 110 | 30/s | 1.5 | 16 | **March** (40% walk) | The only shield you can advance behind |
| **Kite Ward** *(built)* | 80/30% | 200 | 14/s | 3.5 | 24 | Root | The knight's standard; balanced in every line |
| **Argent Mirror** | 70/20% | 140 | 18/s | 5 | 28 | Root | The first 0.25 s of the raise *reflects* projectiles |
| **Bastille** | 85/35% | 220 | 10/s | 5 | 40 | Root, Air-drop | The bash specialist — its riposte is an attack |
| **Cenotaph** | 95/50% | 420 | 0 (per-fight) | — | 30 | Root, Air-drop | The tomb-marker: vast, unmending; broken is broken until the next List |
| **Pallium** | 55/**55%** | 160 | 20/s | 3 | 18 | Air-hold | The mantle: uniquely strong *behind* — the kiter's cloak |
| **Quiet Bell** *(built)* | 65/35% | 150 | 16/s | 4 | 18 | Root | A dome of hush: briefly muffles blasts and through-wall harm from all sides |
| **Caltrop Ward** | 70/25% | 170 | 15/s | 4 | 20 | Root | Struck, it scatters caltrops at your feet — chasing you costs |
| **Hoarfrost Ward** | 70/25% | 170 | 15/s | 4 | 22 | Root | Melee against it leaves the attacker rimed and slowed |
| **Canopy Ward** | 65/25% (**90% above**) | 180 | 14/s | 4 | 20 | Root | An angled roof-plate: the answer to steeplefall and stoop |
| **Echo Ward** | 75/30% | 190 | 12/s | 4.5 | 26 | Root | A solid block rings an answering shock on both sides |
| **Springald Ward** | 65/20% | 140 | 18/s | 3 | 20 | Air-hold | A blocked hit winds the springald: refunds boost and flings you back a pace |
| **Cheval Ward** | 80/0% | 240 | 8/s | 7 | 22 | Root | Can be **planted** as standing cover and fought from behind |
| **Testudo Ward** | **100/100%** for 0.8 s | 120 | 10/s | 8 | 12 | Root, Air-drop | The full shell: a heartbeat of true invulnerability, then a long toll |
| **Thorn Ward** | 75/25% | 180 | 12/s | 4.5 | 24 | Root | Returns a fifth of blocked harm as a pulse of thorns |

### Edictbound shields
**the Grieving Wall** (blocks *everything*, and weeps your own vigor as payment), **the Eggshell** (absorbs one killing blow per List, then is gone), **the Moon Door** (cycles: perfectly there, then perfectly not), **the Ruin Aegis** (hoards blocked harm and returns it all as a cross of ruin).

### Scrapwright shield — **Boilerplate**
GUARD 80/30% · SOAK 300 · MEND 4/s · TOLL 5 · RIPOSTE 20 · Root. A riveted slab of honest plate. **Perk:** restored to full between fights at no cost, always.

---

## 8. Pods — 24 relic patterns (+6 Edictbound, +1 scrapwright)

Retainers, hounds, hawks, and wards on their own energy. Bars = MIGHT/HASTE/SEEK/BREADTH/LINGER. Tempers as marked.

| Name | Source | Bars | Identity |
|---|---|---|---|
| **Iron Squire** *(built)* | Standard | 3/3/3/3/3 | The loyal retainer: steady chip fire (Sweep temper available) |
| **Kestrel** *(built)* | *(ours)* | 2/5/2/2/2 | The cast hawk: stooping bursts, then an empty glove |
| **Alaunt** *(planned)* | Seeker | 2/1/5/3/3 | The war-hound: slowly courses the quarry, then closes (Sweep/Hoist) |
| **Lurcher** | Speed | 3/5/1/4/5 | Loosed flat and fast; its blast lingers like a held line (Sunder/Hoist, B+) |
| **Ratter** | Cockroach | 2/4/4/3/3 | Wanders idly — until it winds the quarry (Hoist/Sweep) |
| **Springer** | Dolphin | 3/4/2/3/3 | Bounds at the foe in leaping arcs; clears low walls (Hoist) |
| **Gargoyle** *(planned)* | Spider | 2/3/2/5/3 | Perches inert; the first to cross its ward is struck (Hoist), deploys three |
| **Winterwatch, Aloft / Afoot** | Sky/Ground Freeze | 1/2/3/4/4 | A patient rime-ward that fetters whoever comes near |
| **Mummer** | Feint | 2/4/4/4/3 | Halts *in front of* its mark and waits — a feint made flesh (Sweep/Hoist) |
| **Carrion Watch** | Float | 2/2/4/2/3 | Circles above the foe without striking; the sky closes |
| **Tumbler** | Jumping | 3/2/4/4/5 | Approaches low, vaults, and bursts overhead (Sweep/Hoist, B+) |
| **Sparrowhawk** | Diving | 2/3/4/3/3 | Rides high, marks the quarry, then dives past cover |
| **Bellman (& Twin)** | Wave / Double Wave | 2/4/2/2/3 | Tolling blasts that carry through walls; the Twin cries from both sides |
| **Gonfalon Watch** | Satellite | 3/1/3/3/3 | Three hanging banners that fall upon the airborne (Sweep available) |
| **Lymer** | Beast | 2/2/4/3/3 | The leashed hound: hangs back, then is loosed to the fore (Sweep) |
| **Brachet Trio** | Trio | 4/3/3/3/3 | Three small hounds, short of leash, jealous of ground (Sweep) |
| **Pavisers** | Wall (pod) | 3/5/1/3/4 | A rank of shield-bearers: three blasts in a line before you (Sunder) |
| **Palmer** | Reflection | 3/3/1/3/3 | The wandering pilgrim: roams long roads, chases no one |
| **Rearguard** | Caboose | 5/3/3/5/3 | Flies *opposite* your aim — covers the retreat (Hook/Hoist/Sunder) |
| **Outriders** | Twin Flank | 3/3/2/3/3 | One rides dexter, one sinister (Sweep/Hoist) |
| **Canopy Cast** | Umbrella | 3/4/2/4/1 | Three cast wide overhead, bursting in a canopy (Sunder) |
| **Slinger** | Throwing | 3/4/2/3/5 | A squire's high lobbed shot from behind cover (Sunder/Hoist, B+) |
| **Goliath Ward** | Titan (pod) | 1/1/1/5/5 | The enormous, slow, ceremonial blast, in retainer form |
| **Herald** *(ours)* | — | 1/3/4/1/3 | Circles the foe crying their position: marked targets take +10% |

### Edictbound pods
**the Coursing Shade** (Cheetah — three loosed at once, faster than sight, B+), **the Pale Weaver** (Wolf Spider — a leaping ambush-spinner), **the Drowned Chorister** (Orca — three far-ranging hunters that sing as they close), **Shadow of the Riderless, First/Second/Third** (Penumbra — tireless seekers; the Third looses three that do not stop).

### Scrapwright pod — **Watchdog**
Bars 2/2/3/2/5. Sits where you put it and barks steadily. **Perk:** never wanders, never idles — the highest uptime of any pod.

---

## 9. Legs — 12 relic patterns (+4 Edictbound, +1 scrapwright)

| Name | Source | Trait |
|---|---|---|
| **Wayfarer Greaves** *(built)* | Standard | Neutral gait; nothing gained, nothing owed |
| **Heron Greaves** | High Jump | Much higher jump apex; no descent aid |
| **Destrier Greaves** *(planned)* | Ground | Battle-trained pivots: sharp turns afoot and aloft, shorter jump, quicker jump interval |
| **Courser Greaves** *(built)* | Formula | Top ground speed; wide turns only |
| **Palfrey Greaves** | Stabilizer | Smooth-gaited acceleration; the "when in doubt" fit |
| **Curb Greaves** | Short Thrust | Shorter dash, but hard air turns — control over reach |
| **Hart Greaves** | Long Thrust | The leaping stag: much longer dash carry |
| **Hare Greaves** | Quick Jump | Fast, low, frequent jumps |
| **Thistledown Greaves** *(planned)* | Feather | Falls like seed-fluff; long hang, soft landings, sharp ground turns |
| **Longstride Greaves** | Wide Jump | Long flat jumps — midair travel without dash or double-jump |
| **Charger Greaves** | Booster | Dash builds to great speed from a slow start |
| **Gryphon Greaves** *(built)* | *(ours)* | **+1 air dash** — the only relic road past the 2-dash cap; clean landings, slow afoot |

### Edictbound legs
**the Swallow's Road** (ground speed half again over Courser), **the Raven's Step** (jump height *and* dash speed), **the Eclipse Gait** (sharp aerial turning that borders on wrong, high jumps), **the Stride of the Riderless** (Ultimate — everything, all of it, at once).

### Scrapwright legs — **Cartwright Greaves**
Neutral numbers, slightly slow. **Perk:** sure footing — ice barely slides them, conveyors barely carry them, hazard edges grip. The wagon does not tip.

---

## 10. Mapping to the built catalog

The 19 implemented parts (`Parts.cs`, ids frozen) sit in this roster as: Bannerman/Vesper/Duskmantle/Cobalt Knight **Field garnitures**; Arbalest, Litany, Bombard*, Oathblade, Dolorous Maul*, Misericorde; Censer, Anathema Charge; Ward Veil, Pavise; Iron Squire, Kestrel; Wayfarer/Courser/Gryphon Greaves.

*Two v1 names are superseded by v2's feature-complete mapping: v1's **Bombard** (`ram-cannon`) now reads as the **Longshrift** family's heavy cousin — keep the built part, treat it as its own entry (a short-list of 39 guns is fine); v1's **Dolorous Maul** likewise stands beside Pollaxe/Tilt Lance as the built heavy. No renames needed in code; both names remain canon.*

## 11. New sim capabilities (updated backlog)

Volley/multi-projectile · shield toll + raise behaviors + March/plant/reflect/bash/above-arc specials · Fetter status · arcing/hanging/delayed/returning projectile paths · pull forces · range/speed/lunge damage scaling · mine dwell + curved bombs · guard-piercing · extended combo strings · pod behavior suite (course/ambush/circle/vault/retreat/mark) · legs TurnMult/FallSpeedMult/hazard-grip · multi-jump bodies · garniture stat trims · tempers + Branded · charge attacks (per-garniture) · Burden/Fitting (future) · overload rule + Casting opening (`COMBAT_DOCTRINE.md`).

## 12. Rollout passes (revised)

- **A (done):** naming canon + renames + dash cap. **A2 (done):** feature-complete roster + doctrine doc.
- **B (done, 2026-07-18):** shield toll/raise on built shields; Targe, Kite Ward, Quiet Bell (`GAME_DESIGN.md` §27). **C:** volley tech → Trefoil, Palisade, Pincer, Longglaive, Hydra Flail. **D:** Fetter → Fetterlock, Rime, Winterwatch, Knell Maul, Tocsin Mace, Hoarfrost Ward. **E:** pulls/piercing → Grapnel, Hookbill, Estoc, Auger, Sawtooth Espadon. **F:** scaling/delay → Pilgrim, Tilt Lance, Courser Saber, Vigil, Penitent Flail, Beacon, Crowbeak Pick. **G:** trajectory suite → Mangonel, Evenfall, Skysword, Steeplefall, Oxbow, Oubliette(s), Falconet, Volant Falx. **H:** remaining guns/melee pairs + shields (Argent Mirror, Bastille, Cenotaph, Pallium, Echo, Springald, Cheval, Testudo, Thorn, Canopy, Caltrop). **I:** pod suite + legs wave + garnitures + charges + Casting. **J:** scrapwright line (Plowshare through Cartwright) + dependability perks. **K:** the three planned chassis + Harrier + Cockatrice. **L:** tempers + Branded. **M:** Edictbound tier (own design pass: drawbacks first).

Each pass: EditMode tests for domain logic, built-player smoke check, and a `GAME_DESIGN.md` log entry.

---

## 13. The damage model (codified from the Weapon Damage Guide)

The full chain: **Final damage = weapon base (range band, stance) × attacker offense% × defender ward%**, on the shared 1000-vigor scale. Poise (REND) damage runs down the same chain in parallel against the endurance bar.

### 13.1 Guns vary by range band and stance
Gun base damage is a function of **four range bands** — point-blank / short / medium / long (`COMBAT_DOCTRINE.md` §2) — and of **stance** (fired afoot vs. aloft). Every gun belongs to one **range profile**, and the profile is the balance identity:

| Profile | Behavior | Patterns |
|---|---|---|
| **Flat** | Same base in every band — dependable, never spectacular | Arbalest, Mangonel, Dragoon, Versicle, Goliath Shot, Grapnel, Annulet, Wending Bolt, Yoke (afoot), Matchlock |
| **Falloff** | Brutal close, dead at range | Culverin (142→0), Petronel (126→38), Gauntlet (pt-blank only), Auger, Portcullis, Trefoil (huge pt-blank spike, modest beyond), Chevron (152 pt-blank → 76), Longshrift (mild: 105→73), Fetterlock, Aspergill |
| **Rangecraft** | Grows with distance flown — reward for keeping the long field | Litany (62→109), Pilgrim (53→114), Sparrowstorm (47→540 headline), Cinquefoil (52→273), Quillon Bolt (20→42 within its short reach) |
| **Burst-point** | Peak damage at a detonation distance; weak early/late | Beacon (124 at burst, 47 past it), the Voices of the Riderless (127–285 at bloom) |
| **Stance-split** | Ground and air fire are different weapons | Falconet (56 G / 113 A), Gyrfalcon (33 G / 48 A), Alembic (84 G / 48 A), Evenfall, Vigil (228 G / 74 A), Firecrest (endpoints only afoot), Yoke (95 aloft), Splintered Star |

Rules of thumb ported intact from the source tables: multi-round volleys quote all-hit totals (per-round = volley ÷ hits; real connect rates against movers are far below 100% — see `COMBAT_DOCTRINE.md` §13 balance pillar 3); swarm/indirect weapons post the biggest headlines precisely because they land the least.

### 13.2 Bombs are flat; tempers and stance modify
Bomb base damage **does not vary with throw distance** — a Censer at arm's length and across the List hit identically (source rule, kept). Two modifiers only: **stance** (bombs loosed airborne deal ~75% of grounded values — the source's ground/air split, recalibrated) and **temper** (direction of displacement; **Branded** blasts linger and can re-hit on the victim's landing).

### 13.3 Pods are pressure, not payload
Pod hits use flat bomb-style damage at deliberately low values; their real budget is **uptime** (own energy pool, no firing vulnerability) and **LINGER** (a lingering blast that catches landings — the source's speed-pod trick, kept for Lurcher). A pod should never out-damage a gun; it should make every other weapon land more.

### 13.4 The standing exceptions
Downed harnesses are fully invulnerable (our divergence from the source's 30% rule — reaffirmed). Knockdown wipes the downed pilot's in-flight gun rounds (**overload**, `COMBAT_DOCTRINE.md` §4.3) — except scrapwright rounds. Shield block applies GUARD% *before* the damage chain; chip always passes.

---

## 14. The four schools of build (customization doctrine)

The source's build guide (Balanced / Tank / Air / Ground) refits as the four **schools** taught across the Lists. Doctrine for the hangar, the draft AI, and the tutorial voice:

| School | Frame | Kit shape | Teaching |
|---|---|---|---|
| **The Line School** (balanced) | Bannerman, Freelance | Flat-profile gun + Censer + line pods (Iron Squire, Pavisers) + Wayfarer | "Learn everything here. The Line has no answer missing and no answer sharpened." |
| **The Bastion School** (tank) | Cobalt Knight, Skyanvil | The 1–2 punch: heavy gun + heavy bomb (Dragoon + Anathema), wall shield, **legs chosen to patch the frame's flaw** (Hart/Gryphon for reach) | "Trade motion for weight, then buy the motion back with your greaves." |
| **The Sky School** (air) | Vesper, Sunplume, Skyanvil Chase | Stance-split weapons (Falconet, Evenfall, Gyrfalcon), sky-denial pods (Gonfalon, Winterwatch), Thistledown | "Fight from the stance the weapon loves. Land only where you chose in advance." |
| **The Hedge School** (hit-and-fade) | Harrier | Falloff guns (Culverin, Petronel), arcing/planted bombs, roaming pods (Lurcher, Ratter), Destrier/Courser | "You are already in trouble when you are hit. So don't be." |

**Named recipes** (the source's combo section, themed — seed content for tips, AI builds, and challenges): **the Wyrmrider's Descent** (Vesper · Steeplefall-Sweep · Dragoon — sweep them into the lingering fall, ride the wyrm in; 150+), **the Closing Vise** (Falconet · Pincer Charge — blasts fore and aft while the loops close from the flanks), **the Anchorite's Perch** (Duskmantle · Skysword · Gonfalon Watch · Quarrel Charge — perch, rain blades, ward the sky, quarrel whoever climbs), **the Headsman's Arithmetic** (Cobalt War's 105% × Dragoon — the two-knockdown fight). Hangar rule ported from the source's Test Mode: the **test yard** (Squires' Yard) is reachable from the customize screen before any Passage.

---

## 15. Stewardship — every pattern has a keeper

Any armiger may *field* any part (loadout freedom is untouchable). But every pattern is **stewarded** by one faction — the house that maintains its lore, teaches its art, biases its spoils and shops, and whose armigers favor it. Melee patterns share their gun-counterpart's steward; shields share their bomb-counterpart's steward (the pairing is taught as one art). Full assignment:

- **The Aureate Legion** (line discipline): Bannerman · guns Arbalest, Trefoil, Arcus (D/S), Chevron, Cinquefoil (D/S) (+ their melee) · bombs Censer, Quarrel Charge, Palisade (+ shields Ward Veil, Targe, Bastille) · pods Iron Squire, Pavisers, Gonfalon Watch · legs Palfrey, Curb.
- **The Rust Cross Commandery** (walls and sieges): Cobalt Knight · guns Bombard, Longshrift, Mangonel, Portcullis, Goliath Shot (+ melee incl. Dolorous Maul) · bombs Anathema, Steeplefall, Belfry Burst, Goliath Charge (+ shields Pavise, Canopy Ward, Testudo Ward, Cenotaph) · pods Gargoyle, Goliath Ward · legs Gryphon.
- **Order of the Winter Wing** (winter and the wing): Vesper · guns Evenfall, Gyrfalcon, Splintered Star (+ melee) · bombs Rime Charge, Crescent Charge (+ shields Hoarfrost Ward, Kite Ward) · pods Kestrel, Winterwatch (both), Sparrowhawk · legs Heron, Hart.
- **The Solarian Talon** (sun and plumage): Sunplume · guns Thornswarm, Firecrest, Skysword, Beacon, Annulet (+ melee) · bombs Ascension Charge (+ shield Springald Ward) · pods Canopy Cast, Carrion Watch · legs Thistledown.
- **The Kurultai Vanguard** (horse and momentum): Skyanvil · guns Dragoon, Spur Volley, Yoke, Gauntlet (+ melee — the Tilt Lance rides with them; the joust is theirs) · bombs Oxbow Charge (+ shield Pallium) · pods Lymer, Rearguard, Outriders · legs Destrier, Courser, Charger.
- **The Drowned Compact** (salt and salvage): Freelance · guns Culverin, Petronel, Falconet, Grapnel, Alembic (+ melee) · bombs Pincer Charge, Widening Gyre (+ shields Echo Ward, Thorn Ward) · pods Springer, Slinger · legs Longstride.
- **The Umbral Concordat** (patience and certainty): Duskmantle · guns Fetterlock, Vigil, Rookery, Wending Bolt (+ melee) · bombs Oubliette Mine & Twin, Trine Snare (+ shields Caltrop Ward, Cheval Ward) · pods Mummer, Palmer · legs — none; they walk borrowed roads.
- **The Hedge Errantry** (the road): Harrier · guns Pilgrim, Quillon Bolt, Sparrowstorm (+ melee) · bombs — none owned; they throw what they win · pods Lurcher, Ratter, Tumbler, Brachet Trio · legs Wayfarer, Hare.
- **The Litany Sisters**: guns Litany, Aspergill, Versicle, Processional (D/S) (+ melee) · bombs Peal Charge (all), Antiphon Charge (+ shield Quiet Bell) · pods Bellman & Twin.
- **The Wrightsguild**: the entire scrapwright line (Plowshare, Matchlock, Felling Axe, Powder Keg, Boilerplate, Watchdog, Cartwright) · gun Auger (+ Sawtooth Espadon) — the one relic art they were ever granted.
- **The Circuit itself**: the Herald pod (the Herald's own device); the Cockatrice and the Alaunt run masterless — stewarded by no one, coveted by everyone.
- **The Broken Choir**: stewards nothing; *traffics* everything Edictbound (§16).

---

## 16. Edictbound interactions (how the banned tier touches everything)

- **Nature**: these are not simply overpowered relics. Each retains active command logic, interfaces, or propagation behavior from the Last Edict and may seek authority, rewrite permissions, alter memory, or resist shutdown (`WORLD_HISTORY.md` §10).
- **Acquisition**: never sold or drafted normally. Found in **sealed reliquary caches** hidden in the world (the source's hidden-pickup pattern — behind the column, inside the broken carriage), claimed from Broken Choir masters, stolen from Veiled Restoration laboratories, taken from Ash Leveler cells, or granted at story beats. Collection is tracked in-fiction as **the Choir's Ledger**.
- **Laurels**: fielding even one Edictbound part halves that fight's laurels (`COMBAT_DOCTRINE.md` §8). Vow events bar them outright; Choir gauntlets *expect* them.
- **Balance position**: budgeted ~115–130% of relic effectiveness, always with a real drawback in the kit itself (the Martyr's ward, the Stilled Voice's damage, the Moon Door's absences) — the laurel economy prices them, the drawback disciplines them.
- **Frame rules**: Edictbound parts may break frame directives (dash caps, mend rules) — that is *why* they're banned, and they are the only tier allowed to.
- **Overload**: Edictbound guns obey the overload rule like any relic (the wrongness does not protect a volley — the Choir Unending is famously overloadable). Only scrapwright rounds persist.
- **AI usage**: only Broken Choir fighters, empowered Paragons of the Golden Passage, the First Armiger, Riderless, and authored story encounters involving the Whole Crown's Veiled Restoration or Ash Leveler Cinderbound ever field them. A common armiger with an Edictbound arm is a story event, never a random roll.
- **Faction distinction**: Whole Crown researchers want to master Edict-parts discreetly; the Broken Choir openly seeks, implants, and awakens them; Ash Levelers use them as disposable terror weapons they intend to destroy with themselves. These motives should change dialogue, drawbacks, and presentation even when two encounters use the same mechanical tier.
- **Presentation**: violet-black glow, organic wrongness, a whisper-line per part (`ART_DIRECTION.md` §5/§7).

---

## Appendix A. Coverage ledger — every source part, mapped

Verification table: every named part in the source lists and its counterpart here. *(Bodies map per-garniture; blast letters map to tempers.)*

**Bodies** — Ray 01→Bannerman Field · Splendor→Bannerman War · Glory→Bannerman Chase · Milky Way→Sunplume Field · Earth→Sunplume War · Sol→Sunplume Chase · Metal Ape→Cobalt Field · Metal Bear→Cobalt War · Metal Ox→Cobalt Chase · Swift→Harrier Field · Shrike→Harrier War · Peregrine→Harrier Chase · Javelin→Duskmantle Field · Glaive→Duskmantle War · Halberd→Duskmantle Chase · Criminal→Freelance Field · Buggy→Freelance War · Juggler→Freelance Chase · Defender→Vesper Field · Seeker→Vesper War · Breaker→Vesper Chase · Seal Head→Skyanvil Field · Dour Head→Skyanvil War · Tank Head→Skyanvil Chase · Chickenheart→Cockatrice · Ray Legend→the Martyr · Ray Warrior→the Paragon · Rakensen→the Manifold Shadow · Ruhiel→the Grieving Wing · Athena→the Choir Aloft · Rahu I/II/III→Shards of the Riderless · Oil Can→(lineage of) Plowshare, the scrapwright frame.

**Guns** — Basic→Arbalest · 3-Way→Trefoil · Gatling→Litany · Vertical→Mangonel · Sniper→Longshrift · Stun→Fetterlock · Hornet→Thornswarm · Flame→Pilgrim · Dragon→Dragoon · Splash→Aspergill · Left/Right Arc→Arcus Sinister/Dexter · Shotgun→Culverin · Rayfall→Evenfall · Bubble→Alembic · Eagle→Gyrfalcon · V Laser→Chevron · Magnum→Petronel · Needle→Versicle · Starshot→Splintered Star · Glider→Falconet · Homing Star→Rookery · Trap→Vigil · Drill→Auger · Titan→Goliath Shot · Claw→Grapnel · Knuckle→Gauntlet · Afterburner→Spur Volley · Blade→Quillon Bolt · Meteor Storm→Sparrowstorm · Twin Fang→Portcullis · Gravity→Yoke · Phoenix→Firecrest · Left/Right Pulse→Processional S/D · Sword Storm→Skysword · Ion→Wending Bolt · Flare→Beacon · Left/Right 5-Way→Cinquefoil S/D · Halo→Annulet · Wave Laser→the Stilled Voice · X Laser→the Burning Saltire · Crystal Strike→the Choir Unending · Wyrm→the Elder Wyrm · Raptor→the Twin Stoop · Waxing/Waning Arc→the Waxing/Waning Moon · Rahu 1/2/3→Voices of the Riderless · Can→Matchlock.

**Bombs** — Standard (N/F/K/S/X)→Censer (Sunder/Sweep/Unhorse/Fetter tempers) · Wave→Peal Charge · L/R Wave→Peal S/D · Straight (G/S/T)→Quarrel Charge (Hoist/Fetter/Hook) · L/R Flank H→Oxbow S/D · Burrow (D/P)→Oubliette Mine (Branded Sunder/Hoist) · Double Mine→Oubliette Twin · Freeze→Rime Charge · Tomahawk (B/G)→Steeplefall (Branded Sweep / Hoist) · Gemini (B/P)→Pincer Charge · Submarine (D/P/K)→Anathema (Branded Sunder/Hoist, Unhorse) · Crescent (P/C/K)→Crescent Charge (Branded Hoist, gentle Hoist, Unhorse) · Dual (N/C)→Antiphon Charge · Acrobat→Ascension Charge · Delta→Trine Snare · Wall→Palisade · Smash→Belfry Burst · Geo Trap→Widening Gyre · Titan→Goliath Charge · Treble→the Threefold Grief · Wyvern→the Wyvern's Egg · Waxing/Waning Arc→Moontide · Grand Cross→the Ruin Cross · Can→Powder Keg.

**Pods** — Standard (N/F)→Iron Squire · Seeker (F/G)→Alaunt · Speed (D/P)→Lurcher · Cockroach (G/H)→Ratter · Dolphin (N/G)→Springer · Spider (N/G)→Gargoyle · Sky Freeze→Winterwatch Aloft · Ground Freeze→Winterwatch Afoot · Feint (F/G)→Mummer · Float (F)→Carrion Watch · Jumping (B/G)→Tumbler · Diving→Sparrowhawk · Wave→Bellman · Double Wave→Bellman Twin · Satellite (N/H)→Gonfalon Watch · Beast (F)→Lymer · Trio (H)→Brachet Trio · Wall→Pavisers · Reflection→Palmer · Caboose (C/T/X)→Rearguard (gentle Hoist/Hook/Sunder) · Twin Flank (F/G)→Outriders · Umbrella→Canopy Cast · Throwing (D/P)→Slinger · Titan→Goliath Ward · Cheetah→the Coursing Shade · Wolf Spider→the Pale Weaver · Orca→the Drowned Chorister · Penumbra I/II/III→Shadows of the Riderless · Can→Watchdog.

**Legs** — Standard→Wayfarer · High Jump→Heron · Ground→Destrier · Formula→Courser · Stabilizer→Palfrey · Short Thrust→Curb · Long Thrust→Hart · Quick Jump→Hare · Feather→Thistledown · Wide Jump→Longstride · Booster→Charger · Swallow→the Swallow's Road · Raven→the Raven's Step · Eclipse→the Eclipse Gait · Ultimate→the Stride of the Riderless · Can→Cartwright.

**Blast letters → tempers** — F (sideways)→Sweep · H (slow sideways)→Sweep (gentle) · G (upward)→Hoist · C (slow upward)→Hoist (gentle) · K (always down)→Unhorse · S (stun)→Fetter · T (pull)→Hook · X (diagonal)→Sunder · B (sideways, lingering)→Branded Sweep · D (diagonal, lingering)→Branded Sunder · P (upward, lingering)→Branded Hoist. All eleven letters covered; "gentle" is a strength grade, not a seventh temper.

*Ledger verified against the Parts FAQ, the stat-card parts list, and the Weapon Damage Guide, 2026-07-18: no source part is unmapped. Our own additions beyond the source (Bombard, Dolorous Maul, Kestrel, Gryphon Greaves, Herald, the Edictbound shields, and the scrapwright line) stand as native patterns.*
