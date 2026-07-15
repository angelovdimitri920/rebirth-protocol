# Knight Robot Asset

Second-pass Cobalt Knight mech built in Blender for Rebirth Protocol.

This version uses original hard-surface geometry with broad shoulder drums, a head-hidden torso silhouette, cobalt/ivory/aurum armor language, long arms, heavy claw feet, and modular weapon parts.

Preview:

![Knight Robot](images/knight_robot_preview.png)

## Validation

- Status: `pass`
- Exportable mesh objects: `159`
- Projectile/effect meshes: `7`
- FBX exports: `6`

## Files

- `SourceArt/KnightRobot/KnightRobot_Loadout.blend`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_Loadout.fbx`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_Body.fbx`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicGun.fbx`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicBomb.fbx`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicLegs.fbx`
- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicPod.fbx`

## Production Notes

- Geometry is original and does not reuse Custom Robo source meshes.
- Rigging uses hard-surface rigid skinning: every mesh is assigned to one deform bone with full weight.
- The gun includes projectile reference meshes; the bomb includes an effect guide; the pod includes a projectile reference mesh.
- Sockets are included for root, camera target, hands, gun, muzzle, bomb, pod, and leg thrusters.
- Next art pass should add authored animation clips, final collision, LODs, and hand-authored texture maps.
