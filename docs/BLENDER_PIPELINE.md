# Blender And Asset Pipeline

Reference archive location:

`C:\Users\angel\claude_code\rebirth-protocol-unity\3D_models`

Initial inventory:

- `arenas`: 4 ZIP archives
- `parts`: 17 ZIP archives
- `robos`: 22 ZIP archives
- Total: 43 ZIP archives

These archives are confirmed free/licensed reference assets, not Custom Robo IP — but they stay reference-only and gitignored regardless: do not import them directly into `Assets/RebirthProtocol`. Use them only for proportion/silhouette/socket study; production geometry must be original.

## Current Pipeline

Run from `C:\Users\angel\claude_code\rebirth-protocol-unity`:

```powershell
.\scripts\asset_pipeline\extract-reference-archives.ps1
& "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe" --background --python ".\scripts\asset_pipeline\blender_prepare_assets.py" -- --project-root "C:\Users\angel\claude_code\rebirth-protocol-unity" --category all
& "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe" --background --python ".\scripts\asset_pipeline\blender_validate_assets.py" -- --project-root "C:\Users\angel\claude_code\rebirth-protocol-unity"
```

Generated working files stay under `ReferenceOnly`, which is ignored by Git:

- `ReferenceOnly\Extracted`: expanded archive working copies.
- `ReferenceOnly\Blender`: organized `.blend` scenes.
- `ReferenceOnly\UnityReferenceFBX`: FBX exports for Unity prototype/reference import.
- `ReferenceOnly\Reports`: machine-readable JSON inventory, processing, and validation reports.

Human-readable reports:

- `docs\ASSET_REFERENCE_INVENTORY.md`
- `docs\ASSET_BLENDER_PROCESSING.md`
- `docs\ASSET_VALIDATION.md`

## Working Rules

- Preserve source ZIP files unchanged.
- Extract study copies only into a clearly marked `ReferenceOnly` directory outside production Unity assets.
- Record mesh names, pivots, materials, textures, topology notes, and separable components.
- Use the reference material for proportion, silhouette, pivot, and socket study only.
- Build original modular robot bodies, weapons, legs, pods, shields, thrusters, impact effects, and arena forms for production.
- Treat the auto-generated robo armatures as rig scaffolds. Production animation still needs manual weights, deformation cleanup, and original-art redesign.

## Production Socket Plan

- `root`
- `pelvis`
- `spine`
- `head`
- `hand_l`
- `hand_r`
- `weapon_gun`
- `weapon_melee`
- `shield`
- `pod_l`
- `pod_r`
- `thruster_back`
- `thruster_leg_l`
- `thruster_leg_r`
- `muzzle`
- `camera_target`

The first art milestone should validate one original Blender robot, one mechanical skeleton, idle/move/combat animation clips, modular weapon sockets, URP materials, and Unity prefab assembly.
