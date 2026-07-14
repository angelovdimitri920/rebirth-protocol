# Holosseum (Arena) Design Reference v1

*Reference basis for arena design, ensuring our own Holosseums are fun to fight in. Source: [Custom Robo GameCube Holosseums — Robopedia](https://customrobo.fandom.com/wiki/Category:Holosseums) (62-stage category, 14 stages surveyed in §2 below), plus Dragoonman's "Holosseum FAQ" (GameFAQs, v0.75), a player-written strategy guide covering all 35 named stages with in-game descriptions (§3 adds the patterns that survey missed). Inspiration for design language, not a licensed asset source — none of our stage names below are borrowed.*

## 1. What a Holosseum is, per the source material

> "A Holosseum is an artificial robo battle environment... composed of pure energy, but the environment created is physical. Most contain both breakable and unbreakable obstacles. Its invisible walls are impenetrable."

That last sentence is already how we describe our arenas in `GAME_DESIGN.md` §2.3 ("small, wall-bounded 3D arenas"). The rest of that quote is the operative design rule this doc exists to capture: **a good Holosseum layers breakable obstacles, unbreakable hazards, static walls, and (often) moving elements — not just one of these.**

## 2. Surveyed stages, by hazard pattern

### Breakable cover
- **Basic Stage** — rectangle, symmetric, regularly-shaped walls with wooden crates in the middle. Crates are destroyed by gunfire, don't respawn once destroyed for that battle. The wiki's own advice is telling: "if your set functions better in a clearer arena, focus on destroying them; if your set relies on cover, stay away from them." The crates aren't just scenery — they're a resource both players can choose to spend or protect. This is the baseline/neutral stage.

### Unbreakable terrain hazards
- **Frozen Field** — entirely ice-floored; footing itself is the hazard.
- **Magma Hole** — octagon; the center floor sinks and floods with magma if a robo stands on it, except for one small safe tower in the middle that never submerges. Positioning risk with one guaranteed-safe callout point.
- **Dead Line** — medium-large rectangle, cramped; conveyor belts feed continuously into a sea of magma. The hazard isn't the belt itself, it's what the belt does to you if you get immobilized on it.
- **Goliath Hazard** — a ring of magma plus hill-shaped obstacles in the center.

### Static walls / architecture
- **Crevice Court** — square, irregular jagged walls and gaps, wide-open central space. Described as producing "wild and unpredictable" fights — asymmetric cover reads differently from every angle.
- **Castle Citadel** — medium-large rectangle; one large stone lantern/wall structure in the center that the wiki calls "vital to victory." A single strong landmark, not a maze of cover.

### Moving / dynamic elements
- **Panic Cubes** — vertically oscillating block obstacles; danger concentrates at the center, forcing players outward.
- **Scramble Walls** — retracting walls that move (a variant of the also-cataloged "Panic Walls").
- **Sonic Circuit** — a rotating conveyor-belt ring around a stationary center island; players choose between the moving edge and the static middle.
- **Merry-Go-Round** — circular; six horses and two sleighs rotate counterclockwise around the arena. The wiki's tip — "fire between the horses" — describes a moving-cover timing check, not a hazard.
- **Loading Dock** — a single suspended platform that's always moving.
- **Nature Park** — tree stumps and fences as static cover, plus a large center bridge that moves up and down; noted as favoring long-range specialists because of the open sightlines around the moving centerpiece.

### Pure size/pacing levers
- **Sudden Death** — the smallest stage in the game, no notable hazards at all. Its entire design is "force close-range combat" through size alone. Proof that arena *size* is itself a legitimate design lever, independent of hazards or cover.

## 3. Additional patterns from the fuller 35-stage list

The §2 survey (14 stages, Fandom wiki) already captured every *recurring* hazard pattern well. Cross-referencing against the fuller 35-stage strategy-guide list turns up five genuinely new ideas that survey didn't have an example of, plus one structural confirmation of a system we'd already built.

### 3.1 Structural confirmation: layouts and hazards really are separable

Several stages in the fuller list are explicitly described as an *existing base layout* with a *hazard overlay dropped on top* — e.g. a magma-pool corner hazard applied to the same wall layout as the plain baseline stage, and again to a second, differently-shaped baseline layout. That's exactly the shape of our own `Arena.ts`/`rollArena()` system (layout roll × hazard roll, independent axes) — this isn't a new idea to adopt, it's confirmation the architecture we already built matches how the source material itself thinks about stage variety. No action needed here beyond noting it.

### 3.2 New lever: sloped/inclined terrain

One stage is explicitly built on an incline rather than a flat floor — everything the wiki survey covered (ice, magma, conveyors) is a flat-floor hazard; a *slope* is a different axis entirely, affecting movement speed, aim arcs, and where a knocked-back robo ends up. Worth prototyping as **the Basin**: a gently-sloped floor where downhill movement is faster and knockback carries farther, uphill is the opposite — a positioning hazard with no textures or physics tricks needed beyond an actual tilted collider.

### 3.3 New lever: a shrinking safe zone

Distinct from Magma Hole's *static* danger zone (§2, "Unbreakable terrain hazards"), one stage has its hazard ring slowly *close in* over the course of the fight — the safe area shrinks over time rather than being fixed. This is a battle-royale-style pacing tool we don't have yet: a fight that's spacious early and forced into close range late, without touching movement speed or weapon stats at all. Worth prototyping as **the Closing Vein** — could plug into the existing hazard-roll slot as a hazard whose radius is a function of elapsed fight time.

### 3.4 New lever: a floor that changes shape mid-fight

Distinct from moving *cover* (§2's Panic Cubes/Scramble Walls), one stage has the *ground itself* periodically split and shift, leaving gaps that can't be crossed. This punishes poor positioning through terrain removal rather than terrain addition — the opposite failure mode from every hazard already surveyed (which all threaten you for standing somewhere; this threatens you for *not being able to reach* somewhere). Worth prototyping as **the Sunder**.

### 3.5 New lever: a pure perception hazard

Every hazard surveyed so far is physical — it occupies space and can be walked into. One stage instead distorts the *camera/view itself*, making it hard to judge distance to the opponent, with no physical terrain hazard at all. This is a completely different design lever from everything else in this doc: it doesn't punish positioning, it punishes *reading* the fight. Interesting but higher-risk for us specifically, since our camera is already a moving, rotating system (`PlayerController.updateCamera`, `GAME_DESIGN.md` §11.5) doing real work to keep both fighters legible — deliberately degrading that legibility as a "hazard" could fight against a system we just spent real effort making readable. Flagging as a real idea from the source, not a recommendation to build soon.

### 3.6 New lever: wall density as its own axis, not just size

§2's "pure size/pacing levers" principle (Sudden Death: small forces close range) has a counterpart the survey didn't include: a stage with *no walls at all* — completely open, forcing a totally different read on any weapon whose behavior depends on cover (arcing shots around walls, homing that breaks on line-of-sight, etc.). Combined with Crevice Court's dense-and-irregular wall maze already in §2, this confirms wall density belongs on its own spectrum from "maze" to "zero," independent of arena size. Worth keeping in mind when Stage 3's modifier-roll system (`GAME_DESIGN.md` §3.4/§4) gets more layouts: a "the Open Void" no-walls layout is a legitimate extreme worth having in the pool alongside a dense-maze extreme, not just varying degrees of "medium."

## 4. Design principles to carry into our arenas

1. **Always bound the arena with a hard, invisible wall.** Already in `GAME_DESIGN.md` §2.3 — confirmed as the one universal constant across every stage surveyed.
2. **Mix at least one breakable element and one unbreakable one.** Breakable cover (crates, destructible walls) gives AoE/bomb-type weapons a clear job — clearing cover — that a pure gun build doesn't want to spend time on. Unbreakable hazards (lava, ice, sinkholes) punish poor positioning independent of what either player is holding. Relying on only one of the two makes every arena play the same regardless of loadout.
3. **Walls and hazards are different levers — use both.** Walls block homing lock and line of sight (a positioning/spacing tool). Hazards punish standing in the wrong place (a risk/reward tool). The strongest stages surveyed (Castle Citadel, Dead Line, Magma Hole) use one of each rather than doubling up on either.
4. **A moving/dynamic element is optional but valuable, and doesn't need to be complex.** Sonic Circuit and Merry-Go-Round get most of their identity from one single moving system (a belt, a rotating obstacle ring) rather than several. This is a good candidate to prototype early as a template for the Stage 3 arena-modifier system (`GAME_DESIGN.md` §3.4/§4) — a "moving hazard" slot that can be swapped or rolled per fight later.
5. **Vary size deliberately as a pacing tool.** Small forces close-range brawling (Sudden Death); large/open supports kiting and ranged spacing (Crevice Court). Arena size should be a conscious choice per stage, not incidental to the layout.
6. **Give each arena one memorable, callable landmark.** Castle Citadel's central lantern wall and Magma Hole's safe tower both give players a single position they can reference mid-fight ("get behind the lantern," "hold the tower"). This matters more for us than it did for Custom Robo's fixed-camera view — in a free 3D camera, a featureless box is much harder to read and callout during a fast fight.

## 5. Stage 1 recommendation

Build the single Stage-1 arena as a **Basic-Stage-style neutral duel space**: medium, roughly symmetric, flat bounded ground, a handful of destructible crates or crate-like cover in the middle (clear job for the gun to clear or the melee gap-closer to fight around), plus two or three unbreakable wall segments placed asymmetrically enough to block line-of-sight and homing lock without turning the arena into a maze. Skip moving elements and terrain hazards (magma, ice, conveyors) for Stage 1 — introduce those once there's more than one arena to give variety to (Stage 3's modifier rolls), so the Stage 1 space stays legible while the movement-and-punish loop itself is what's being judged at the go/no-go gate. This is a recommendation, not a mandate — flag if you'd rather Stage 1 include one hazard from day one.
