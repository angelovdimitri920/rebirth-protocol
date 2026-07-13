# REBIRTH PROTOCOL — Design Document v1

*A 3D arena mecha roguelite inspired by Custom Robo: Battle Revolution (GameCube), with weapon-loadout depth from Armored Core / Gundam and a real-time roguelike run structure. "REBIRTH PROTOCOL" is a working title, named for the knockdown → rebirth comeback rhythm (§2.2) and the roguelite die-and-retry loop.*

---

## 1. Concept & Pillars

Build a robo from modular parts before a run, fight through a sequence of real-time arena duels, and get stronger *during* the run by picking up boons and items mid-fight — Custom Robo's beloved build-crafting, restructured as a roguelite.

**Pillars:**
1. **Build expression** — every part/boon changes *how you play*, not just your numbers.
2. **Readable, skill-based combat** — no button-mashing; openings are earned, not given.
3. **One more run** — clear direction, real (not fake) choices, escalating power that stays legible.

**Competitive positioning:** No shipped game currently combines (a) a modular pre-battle parts loadout, (b) real-time 3D arena duels, and (c) roguelike run-based progression. Custom Robo's own spiritual successor, *Synaptic Drive* (2020, by series creator Koji Kenjo), has (a) and (b) but explicitly shipped without a roguelite layer — just an arcade score-attack mode. Every existing "mech roguelite" (Wolfstride, Mechabellum, MECHBORN, Mech Armada) is turn-based, an auto-battler, or a deckbuilder. This is open ground.

---

## 2. Core Loop — Inherited from Custom Robo

### 2.1 The five-slot loadout
Before a run (and adjusted between fights), assemble a robo from:
- **Body** — base HP, DEF multiplier, ATK multiplier, air-dash class/identity, charge-attack animation (i-frames during it).
- **Gun** — primary ranged weapon (ATK, bullet speed, homing strength, rate of fire, knockdown power).
- **Bomb** — secondary, slower/AoE, on a cooldown.
- **Pod** — deployable drone/mine for zoning; in our version, pods/funnels get their **own energy pool**, independent of guns/bombs, so they're an always-on pressure tool rather than a burst-damage opportunity cost (Gundam Breaker 4's mistake was tying funnels to the same meter as big attacks, which made them feel unusable — avoid that).
- **Legs** — ground speed, jump height, air-dash count/type, turning, landing recovery.

Any part fits any body. Each body should be a genuine *archetype* (not just a stat-stick) defined by its air-dash behavior — e.g., a balanced 2-dash all-rounder, a long-single-dash glass-cannon flier, a stealth/vanish-dash evader, a slow tank with high load capacity.

Visual/silhouette design basis for these archetypes: see [`ROBOT_SHELL_DESIGN.md`](ROBOT_SHELL_DESIGN.md).

### 2.2 HP, endurance, knockdown, rebirth
Twin-bar system: a large HP pool plus a separate **endurance** bar. Taking hits drains endurance; when it empties, the robo is knocked down (briefly helpless, mash to recover faster), then enters a few seconds of **rebirth invincibility** on standing. This is the source of Custom Robo's signature comeback rhythm — pressure to force a knockdown, but respect the rebirth window. Endurance regenerates over time when unhit.

### 2.3 Arenas ("Holosseums")
Small, wall-bounded 3D arenas with cover geometry (walls, platforms), multi-tier verticality, and environmental hazards (lava, ice, conveyors). Cover matters — you duck behind walls to break homing lock, and arc-firing weapons exist specifically to dig enemies out of cover.

Arena design basis and Stage 1 recommendation: see [`HOLOSSEUM_REFERENCE.md`](HOLOSSEUM_REFERENCE.md).

---

## 3. Differentiators — What Makes This Not Just Custom Robo

### 3.1 Melee weapons (new to the formula)
Custom Robo has no melee combat — this is the biggest single differentiator. Design menu, drawn from Armored Core 6 and Devil May Cry:
- **Separate melee button** from gun/bomb/pod (Devil May Cry's foundational lesson — melee and ranged on different inputs let players weave both rather than choosing one).
- **Gap-closer dash-attack** (Stinger/AC6-kick style): high commitment, punishable recovery if it whiffs, but closes distance on kiting ranged opponents.
- **Combo strings**: 2–3 hit branching chains (stab → sweep → launcher), similar to Gundam Vs beam saber strings.
- **Stagger/poise meter** (AC6's ACS system): melee and high-impact hits build a separate "stagger" gauge; filling it stuns the target for a big punish window. This gives melee a clear job (staggering) distinct from ranged's job (sustained damage), so both archetypes matter.
- **Melee clash**: when two melee attacks connect simultaneously, resolve it via a fast step-cancel (a la Gundam Vs's "Rainbow Step") rather than a rock-paper-scissors minigame — whoever re-engages faster wins the exchange.

### 3.2 Shields (new layer on top of endurance)
Keep HP/endurance as the core Custom Robo system, and add shields as a genuinely distinct layer — don't let it double up with rebirth invincibility (a broken shield should feed into the *same* stagger/knockdown state, not stack a second free defense on top). Two shield archetypes to consider (pick one for v1, both are viable stretch goals):
- **Regenerating energy shield** (Overwatch model): its own bar, absorbs damage, stops regenerating when recently hit, only blocks from the front, regenerates after ~2–3s without damage.
- **Consumable/physical shield** (Gundam beam-shield model): finite, depletes permanently once broken this fight, doesn't regenerate mid-fight (refills between arenas as a run resource), guard-breaks into a stagger when it fails.
- **Shield bash**: turn a successful block into an offensive stagger option (Brigitte/Gundam Breaker precedent) so shields aren't purely passive.

### 3.3 Expanded air combat & flanking
Layer these on top of Custom Robo's existing jump/air-dash:
- **Boost economy**: movement (jump, air-dash, hover) spends a shared gauge that only refills on landing; landing recovery scales with how much you spent, and fully draining it ("overheating") adds an extra penalty. This single rule (borrowed from Gundam Vs) creates most of the aerial skill ceiling — positioning, baiting an opponent's landing, and managing your own gauge.
- **Homing dash** (Zone of the Enders model): a dash that curves toward the locked target — becomes a melee lunge up close, or a homing ranged option at range.
- **Momentum-building movement** (Titanfall 2 principle): if you add wall-contact or slide mechanics, have them *build* speed rather than just repositioning, so chaining movement feels rewarding.
- **Range-gated lock-on** ("red lock / green lock" from Gundam Vs): homing tracking only works within an effective range band; outside it, shots fly straight. Gives ranged weapons meaningful falloff and rewards spacing.
- **Landing-recovery flanking**: reward players who punish an opponent's post-landing vulnerability window — this is the core skill expression of Gundam Vs's aerial neutral game.

### 3.4 Arena variety
Go beyond Custom Robo's relatively flat Holosseums:
- **Destructible cover** and throwable hazards (Power Stone).
- **Dynamic mid-fight events**: randomly-triggered hazards that reshape the arena or force engagements (Anarchy Reigns' tsunamis/black holes/sawblades model).
- **Roguelite modifier rolls**: treat each arena as a roll of {layout, hazard set, environmental modifier}, the same way Hades rolls encounter modifiers and Dead Cells varies biome hazards — keeps repeat runs feeling fresh without needing hundreds of hand-built arenas.

---

## 4. Roguelike Structure

- **Pre-run base kit**: the five-slot loadout is your Hades-style "chosen weapon" — a deliberate, skill-expressing starting identity, not randomized.
- **In-run boons**: offered as **3 choices** after cleared fights, one per ability slot (gun/bomb/pod/melee/dash) — enforce Hades's "one boon per slot" discipline to bound complexity. Boons should change *behavior* ("your dash now leaves a damaging afterimage"), not just add flat stats.
- **Stacking items**: smaller passive pickups that stack linearly for additive effects; use **hyperbolic scaling** (`1 − 1/(1+a·x)`) for any %-chance effect so it approaches but never trivially hits 100% (Risk of Rain 2's model). Hook all items into a small set of universal trigger verbs (on-hit, on-kill, on-knockdown, on-dash) so synergies emerge combinatorially rather than needing hand-authored pairs.
- **Real choices, not fake ones**: every reward option should open new possibilities rather than being an obvious best-pick or an obvious trap. Offer mitigation (rerolls, banish, shop) so a rough early run stays recoverable.
- **Meta-progression unlocks options, not raw power**: new parts/boons/archetypes to draft from, not flat stat increases — keeps runs varied at hour 50, not just hour 5.
- **Fix Custom Robo's own known weakness**: player/critic consensus on the GameCube game is that bombs and pods were often skippable next to a good gun. Make sure early boon/item design gives bombs and pods (and now melee, shields, and funnels) their own clear win conditions so no single tool dominates every build.

---

## 5. Engine & Tech Stack

**Decision: Three.js + TypeScript, bundled with Vite.**

This supersedes an earlier Godot recommendation in this project's research. Worth recording honestly: this trades away Godot's more mature AI-agent tooling (dedicated MCP servers with in-editor run/debug feedback loops) and its built-in editor, physics, and character controller — all of that now gets assembled from libraries instead of coming for free. The upside: Three.js is *just* JavaScript/TypeScript end-to-end, so an AI coding agent can read, write, run, and test it with a normal dev server and no engine-specific tooling required at all — and it opens genuine browser-native distribution (itch.io, web demos) alongside Steam.

**Core stack:**
- **Three.js** (r16x+) for rendering — scene graph, camera, lighting, `GLTFLoader` for mecha models.
- **Rapier3D** (`@dimforge/rapier3d-compat`, Rust compiled to WASM) for physics and the character controller. It ships a built-in `KinematicCharacterController`, which is the right primitive for the boost-economy movement in §3.3 — you want precise control over grounded/airborne state and velocity, not a generic rigid-body simulation. It's a more actively maintained, better-performing choice than Cannon-es for a project with real movement/collision demands.
- **TypeScript**, not plain JS — worth the small setup cost at this scope; it catches a whole class of bugs an agent can otherwise introduce silently across files.
- **Vite** for the dev server and bundler — fast hot-reload matters a lot when tuning movement feel.
- **Howler.js** or native Web Audio for sound; an HTML/CSS overlay (not in-canvas rendering) for HUD/menus — far faster to iterate on than drawing UI in WebGL.

**Distribution:** Three.js runs natively as a browser game, so itch.io (HTML5) is a zero-friction release target alongside Steam. For Steam specifically, wrap the build in **Tauri** (Rust-based, much smaller binary than Electron, the current recommendation for new projects) or Electron if you hit a Tauri/WebGL compatibility snag. Steamworks integration then goes through the wrapper's native layer, not the browser.

**Verification loop:** Since this is a normal web app, an AI coding agent can run `npm run dev` and check the dev console for errors directly; if you have a browser-automation MCP connector wired into your Claude Code setup, that gives it a visual self-check loop roughly analogous to Godot's in-editor screenshot tooling, though it isn't wired up by default the way the Godot MCP servers are.

---

## 6. Staged Development Roadmap

**Stage 1 — Core duel prototype (weeks 1–6).** One arena, one robo, the boost economy (jump/air-dash/hover spending a gauge that only refills on landing), lock-on, one gun + one melee weapon with a gap-closer, and the HP/endurance/knockdown/rebirth loop. *Go/no-go: does the movement-and-punish loop feel tense and good with zero upgrades, against a dummy or basic AI, for 10+ minutes?* If not, stop and fix this before adding anything else.

**Stage 2 — Full loadout + one shield (weeks 6–12).** All five part slots as swappable resources with real stat tradeoffs; one shield archetype wired into the existing stagger/knockdown state. *Go/no-go: do two different builds (e.g., melee-rush vs. funnel-kiter) feel meaningfully different and counter each other?*

**Stage 3 — Roguelite loop + arena modifiers (weeks 12–20).** Run structure across a sequence of duels; boon/item drafting between fights; arena modifier rolls. *Go/no-go: do playtesters describe their build, not just whether they won?*

**Stage 4 — Aerial identity + funnels + melee clash (weeks 20+).** Remote drones with their own energy pool, homing dash, melee-clash/parry counterplay, momentum movement if maps support it.

**Thresholds that should change the plan:** if projectile/particle counts approach bullet-hell scale, look at instanced rendering and Three.js's WebGPU renderer path before assuming a rewrite is needed; if online PvP becomes a core pillar rather than a later add-on, plan a WebSocket/WebRTC layer early — browser-native networking is one area where Three.js is actually at an advantage over a native engine.

---

## 7. Open Design Questions (resolve during prototyping, not up front)

- Exact stagger-meter numbers (fill rate per weapon type, decay rate, stun duration).
- Whether shields are a Stage 2 feature or should wait until Stage 3 once the core loop is proven.
- Whether funnels/pods deploy as persistent companions or single-use consumables.
- How many arena layouts are needed at ship vs. how much variety comes from modifiers alone.
- Whether multiplayer (local or online) is ever in scope, and if so, when to start planning netcode.

---

## 8. Implementation Log — Stage 1 Judgment Calls

Decisions made during Stage 1 prototyping where the design doc was ambiguous or silent. Revisit any of these if playtesting disagrees.

- **Downed robos are fully invulnerable**, not damage-reduced (source games reduce damage). Keeps the comeback rhythm clean in a 1v1; revisit if juggling/oki play ever matters.
- **Melee is a single swing** — no 2–3 hit combo strings yet. §3.1's combo strings are treated as a later addition on top of the proven gap-closer core, not a Stage 1 requirement.
- **No separate stagger meter yet.** Endurance fills that role for now; the dedicated stagger gauge (§3.1) arrives with shields in Stage 2, since a broken shield needs it to feed into.
- **Dash from the ground is allowed** (converts to a small hop + air-dash, costs boost). The design only specifies air-dash; a grounded robo with zero dash options felt wrong.
- **Lock-on is default-on and arena-wide**; the red-lock/green-lock range gate (§3.3) is deferred to Stage 4 as planned, but gun homing already stops beyond 24 m as a soft preview of it.
- **Controls (KB+M):** WASD move (camera-relative), Space jump/hover + mash-to-recover, Shift air-dash, LMB gun, RMB melee, Tab lock-on toggle, R restart.
- **Boost numbers** (gauge 100): hover drain 45/s, air-dash 28, landing recovery 0.1 s + 0.55 s × fraction spent, overheat +0.5 s. All in `src/core/tuning.ts`.
- **Working title: REBIRTH PROTOCOL** (named for §2.2's rebirth window + the roguelite loop).

### Stage 2 judgment calls

- **Shields resolved as a Stage 2 feature** (§7 question closed): the regenerating front-arc energy shield ("Aegis Barrier") shipped first. Guard break feeds *directly* into the existing knockdown state — no separate stagger meter yet, endurance still fills that role.
- **Shield is a sixth slot** (None / Aegis), not folded into body parts — keeps body archetypes about movement identity.
- **Pods are persistent deployables** (§7 question closed): toggle deploy/recall at your position; the drone fires homing chip shots on its own regenerating energy pool, per §2.1's independent-pool rule.
- **Bombs lead their target** (aim at predicted landing-time position) — without this a strafing opponent made bombs literally unhittable, the exact §4 "skippable" trap.
- **Bomb AoE is friendly-fire**: your own bomb can guard-break or knock *you* down.
- **Vanish dash (Wraith) grants projectile i-frames only** — melee still connects, so the evader body doesn't hard-counter melee builds.
- **No charge attacks yet**: body identity = dash type + stat profile for now; charge attacks (§2.1) deferred.
- **Enemy preset build**: Bulwark / Ram Cannon / Quake Bomb / Sentry Pod / Strider / Aegis — a tanky bruiser that contrasts with most player builds for the Stage 2 go/no-go ("do two different builds feel meaningfully different?").
- **Known polish gap**: no camera collision — cover walls can briefly occlude the third-person camera.

### Stage 3 judgment calls

- **Run shape**: 5 duels per run against escalating enemy presets (all-rounder → harasser → shielded skirmisher → tank → kitchen-sink), each with a flat ×(1 + 0.12·fight) power multiplier on HP and ATK.
- **Player HP persists across fights** (+15% max-HP heal between fights); endurance/shield/boost reset per fight. Death ends the run.
- **Boons apply to the player only** — the AI doesn't draft. Drafts offer 3 boons from 3 *different* ability slots (§4's one-boon-per-slot discipline); 1 reroll per run; skipping is allowed.
- **Items drop mid-fight from destroyed crates** (30% chance) as walk-over pickups, rather than from a post-fight shop — makes destructible cover double as an economy. %-chance items use hyperbolic stacking as specified.
- **Arena modifier rolls**: {layout: crossfire/bastion/scatter} × {hazard: none/lava/ice/conveyor}; fight 1 is always hazard-free so run openings stay readable. Hazard damage bypasses shields (environmental, not directional).
- **Mitigation shipped**: reroll only; banish and shops deferred until there's a currency to spend.
- **Meta-progression not started** — §4's "unlock options, not power" layer is still open.

### First playtest feedback (2026-07-13) — controls & camera revision

- **Camera is now the Custom Robo GameCube view**: elevated, fixed-yaw, anchored to the player's side of the arena. It never rotates, so WASD maps 1:1 to screen directions — this also eliminated the "inverted A/D" feel, which was really the old orbiting lock-on camera constantly re-basing what "left" meant.
- **Free facing**: the robo faces its movement direction; it only squares up to the enemy while actually attacking (gun held / melee active). Lock-on (Tab) still governs homing, melee targeting, and homing dash — it no longer steers the camera or facing.
- **No pointer lock / mouse-look anymore** — mouse is buttons only (LMB gun, RMB melee).
- **Arena walls are real**: connected visible perimeter walls with glowing trim enclose the Holosseum; colliders extend invisibly above the visible wall so nothing boosts out. Replaces the old invisible boundaries + floating rim strips.
- Camera-occlusion polish gap from Stage 2 is largely mooted by the high camera angle.

### Second playtest pass (2026-07-13) — control fix + top-down camera + full containment

- **Root-caused the "inverted" controls**: it wasn't the key mapping, it was a sign error in the camera math. The fixed camera looks down world +Z, and in Three.js's right-handed convention that flips which world axis is screen-right (confirmed by the rotation math: a camera facing +Z has local "right" pointing at world −X, not +X). I'd hand-picked the wrong axis twice in a row. Fixed properly this time: movement is now derived every frame straight from `camera.quaternion` (`screenRight`/`screenForward` vectors), so WASD is correct by construction and can't drift out of sync again if the camera is retuned later.
- **Camera pitched much steeper** — confirmed via the wiki's own framing of the series ("frantic action battles in confined 3D arenas, similar to Virtual On") that an elevated, near-overhead read is the right genre reference. Went from ~43° to ~65° (`height: 20, back: 9` in `tuning.ts`), with a full-arena screenshot confirming both robos, cover, and the pickup all read clearly at a glance.
- **Deliberate divergence from the literal GameCube camera**: the original rotates to track the player's facing (Virtual On-style lock-on chase cam). We're keeping a fully fixed-yaw camera instead, because the user explicitly wants free movement without forced facing — a rotating camera would reintroduce the same "what does WASD mean right now" problem this pass just fixed. Worth revisiting if a future pass wants full camera fidelity.
- **Arena is now a sealed volume, not just walled**: added an invisible ceiling collider matching the wall footprint, and raised the invisible wall height from 6 to 34 (verified: a fully-drained max-duration hover peaks at 31.67 before falling back, comfortably under the ceiling). This was a real exploit, not just a request — the boost economy's hover mechanic can carry a robo ~29m up, which cleared the old 6-unit wall collider entirely.

### Stage 4 slice (partial)

- **Homing dash**: while locked on, active dashes curve toward the target at a capped turn rate (ZoE model) — works as approach and as escape-denial.
- **Enemy melee**: the AI now goes for melee up close, preferring to punish the player's landing recovery — landing-recovery flanking (§3.3) now cuts both ways.
- **Melee clash**: simultaneous melee attacks within range cancel both into a 0.25 s step-cancel with mutual knockback — whoever re-engages faster wins the exchange (§3.1's Rainbow Step resolution, no RPS minigame).
- **Still open from Stage 4**: strict red-lock/green-lock UI feedback (the range gate itself exists on gun homing), momentum-building wall movement, funnel behaviors beyond the static pod.
- **Verification (2026-07-13)**: all live-verified in the sim — homing-dash curves ~3 rad/s toward the locked target mid-dash; melee clash cancels both attackers into 0.25 s step-cancel locks; lava pools apply continuous HP+endurance DoT; conveyors drift standing robos; ice preserves slide momentum after input release. Only organic AI melee initiation (timer/range branch) hasn't been observed end-to-end — it shares the verified tryStart path.
