I'm starting a new Godot 4 project: a 3D arena mecha combat game (working title TBD). Read `docs/GAME_DESIGN.md` in full before doing anything else — it's the design doc, and CLAUDE.md has our current scope and working style.

For this session, I want to build the Stage 1 core duel prototype only (design doc §6): one arena, one robo, and the core movement/combat feel, with no loadout swapping, shields, or roguelite systems yet.

Please set up the Godot project structure, then implement, in this order:

1. **Movement & the boost economy**: ground movement, jump, and an air-dash/hover that spends a shared "boost" gauge. The gauge only refills when grounded, and landing recovery time scales with how much boost was spent since the last landing (fully draining it should impose a larger recovery penalty). Get this feeling responsive on its own before adding combat.
2. **Lock-on targeting**: a simple lock-on onto a target dummy/AI robo, with a camera that keeps both combatants in frame.
3. **One ranged weapon** (a basic gun, fired on its own button) and **one melee weapon** (on a separate button, with a dash-in gap-closer that has real whiff recovery if it misses).
4. **HP + endurance + knockdown + rebirth**: a main HP pool, a separate endurance bar that drains on hit, a knockdown state when endurance empties, and a few seconds of rebirth invincibility on standing back up.
5. **One arena**: a small wall-bounded space with at least one piece of cover, so line-of-sight and positioning already matter.

Build this incrementally and let me test/give feedback between each numbered step rather than implementing everything and tuning at the end — I want to validate the movement and combat feel is actually fun before we add anything else. Use the Godot MCP tooling to run the project and check for errors as you go rather than asking me to manually verify each change.
