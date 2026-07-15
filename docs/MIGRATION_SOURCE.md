# Migration Source

- Source project: `C:\Users\angel\claude_code\mecha-roguelite\mecha-roguelite`
- GitHub: `https://github.com/angelovdimitri920/rebirth-protocol`
- Source branch at setup: `main`
- Source commit at setup: `9e30725`
- Source status at setup: clean tracked files, with `3D_models/` untracked
- Unity project path: `C:\Users\angel\claude_code\rebirth-protocol-unity`
- Unity editor: `D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe`

## Rules

- Do not delete, move, rewrite, or reorganize the Three.js prototype.
- Do not do a line-by-line TypeScript-to-C# translation.
- Treat the prototype as the design and feel reference for timing, camera, movement, combat, drafting, run flow, UI, music, and SFX.
- Keep Unity gameplay logic separate from Unity presentation.
- Runtime state must not mutate ScriptableObject source assets.
- Use deterministic seeds for runs, arena rolls, and draft choices.
