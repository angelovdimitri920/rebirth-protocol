import argparse
import json
import sys
from pathlib import Path

import bpy


REQUIRED_BONES = {
    "root",
    "pelvis",
    "spine",
    "chest",
    "neck",
    "head",
    "upper_arm_l",
    "forearm_l",
    "hand_l",
    "upper_arm_r",
    "forearm_r",
    "hand_r",
    "thigh_l",
    "shin_l",
    "foot_l",
    "thigh_r",
    "shin_r",
    "foot_r",
    "weapon_gun",
    "weapon_bomb",
    "pod_back",
    "muzzle",
}

REQUIRED_MODULES = {
    "body",
    "basic_gun",
    "basic_bomb",
    "basic_legs",
    "basic_pod",
}

REQUIRED_SOCKETS = {
    "Socket_Root",
    "Socket_Camera_Target",
    "Socket_Gun",
    "Socket_Muzzle",
    "Socket_Bomb",
    "Socket_Pod",
    "Socket_Hand_L",
    "Socket_Hand_R",
    "Socket_Thruster_Leg_L",
    "Socket_Thruster_Leg_R",
}

REQUIRED_EXPORTS = {
    "KnightRobot_Loadout.fbx",
    "KnightRobot_Body.fbx",
    "KnightRobot_BasicGun.fbx",
    "KnightRobot_BasicBomb.fbx",
    "KnightRobot_BasicLegs.fbx",
    "KnightRobot_BasicPod.fbx",
}


def argv_after_double_dash():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    return parser.parse_args(argv_after_double_dash())


def mesh_objects():
    return [
        obj for obj in bpy.data.objects
        if obj.type == "MESH" and not obj.get("rbp_ignore_export")
    ]


def validate_blend(project_root):
    source_blend = project_root / "SourceArt" / "KnightRobot" / "KnightRobot_Loadout.blend"
    issues = []
    if not source_blend.exists():
        return {"status": "fail", "issues": [f"missing source blend: {source_blend}"]}

    bpy.ops.wm.open_mainfile(filepath=str(source_blend))

    armature = bpy.data.objects.get("KR_Rig")
    if armature is None or armature.type != "ARMATURE":
        issues.append("missing KR_Rig armature")
    else:
        missing_bones = sorted(REQUIRED_BONES - set(armature.data.bones.keys()))
        if missing_bones:
            issues.append(f"missing bones: {', '.join(missing_bones)}")

    missing_sockets = sorted(REQUIRED_SOCKETS - set(bpy.data.objects.keys()))
    if missing_sockets:
        issues.append(f"missing sockets: {', '.join(missing_sockets)}")

    meshes = mesh_objects()
    if len(meshes) < 35:
        issues.append(f"expected at least 35 exportable mesh objects, found {len(meshes)}")

    module_counts = {}
    projectile_count = 0
    unweighted = []
    for obj in meshes:
        module = obj.get("rbp_module")
        module_counts[module] = module_counts.get(module, 0) + 1
        if obj.get("rbp_role") in {"projectile_reference", "effect_reference"}:
            projectile_count += 1

        bone = obj.get("rbp_rig_bone")
        has_group = obj.vertex_groups.get(bone) is not None if bone else False
        has_armature_modifier = any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
        if not bone or not has_group or not has_armature_modifier:
            unweighted.append(obj.name)

    missing_modules = sorted(REQUIRED_MODULES - set(module_counts.keys()))
    if missing_modules:
        issues.append(f"missing modules: {', '.join(missing_modules)}")
    if unweighted:
        issues.append(f"meshes missing rigid skin setup: {', '.join(unweighted[:8])}")
    if projectile_count < 3:
        issues.append(f"expected at least 3 projectile/effect meshes, found {projectile_count}")

    return {
        "status": "pass" if not issues else "fail",
        "issues": issues,
        "meshObjectCount": len(meshes),
        "moduleCounts": module_counts,
        "projectileOrEffectCount": projectile_count,
    }


def validate_exports(project_root):
    fbx_dir = project_root / "Assets" / "RebirthProtocol" / "Art" / "Mechs" / "KnightRobot" / "FBX"
    existing = {path.name for path in fbx_dir.glob("*.fbx")}
    missing = sorted(REQUIRED_EXPORTS - existing)
    return {
        "status": "pass" if not missing else "fail",
        "issues": [f"missing FBX exports: {', '.join(missing)}"] if missing else [],
        "fbxCount": len(existing),
        "fbxDir": str(fbx_dir),
    }


def write_report(project_root, blend_result, export_result):
    report = {
        "asset": "KnightRobot",
        "status": "pass" if blend_result["status"] == "pass" and export_result["status"] == "pass" else "fail",
        "blend": blend_result,
        "exports": export_result,
    }
    report_path = project_root / "Assets" / "RebirthProtocol" / "Art" / "Mechs" / "KnightRobot" / "knight_robot_validation.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")

    docs_path = project_root / "docs" / "KNIGHT_ROBOT_ASSET.md"
    lines = [
        "# Knight Robot Asset",
        "",
        "Second-pass Cobalt Knight mech built in Blender for Rebirth Protocol.",
        "",
        "This version uses original hard-surface geometry with broad shoulder drums, a head-hidden torso silhouette, cobalt/ivory/aurum armor language, long arms, heavy claw feet, and modular weapon parts.",
        "",
        "Preview:",
        "",
        "![Knight Robot](images/knight_robot_preview.png)",
        "",
        "## Validation",
        "",
        f"- Status: `{report['status']}`",
        f"- Exportable mesh objects: `{blend_result.get('meshObjectCount', 0)}`",
        f"- Projectile/effect meshes: `{blend_result.get('projectileOrEffectCount', 0)}`",
        f"- FBX exports: `{export_result.get('fbxCount', 0)}`",
        "",
        "## Files",
        "",
        "- `SourceArt/KnightRobot/KnightRobot_Loadout.blend`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_Loadout.fbx`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_Body.fbx`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicGun.fbx`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicBomb.fbx`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicLegs.fbx`",
        "- `Assets/RebirthProtocol/Art/Mechs/KnightRobot/FBX/KnightRobot_BasicPod.fbx`",
        "",
        "## Production Notes",
        "",
        "- Geometry is original and does not reuse Custom Robo source meshes.",
        "- Rigging uses hard-surface rigid skinning: every mesh is assigned to one deform bone with full weight.",
        "- The gun includes projectile reference meshes; the bomb includes an effect guide; the pod includes a projectile reference mesh.",
        "- Sockets are included for root, camera target, hands, gun, muzzle, bomb, pod, and leg thrusters.",
        "- Next art pass should add authored animation clips, final collision, LODs, and hand-authored texture maps.",
    ]
    docs_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return report


def main():
    args = parse_args()
    project_root = Path(args.project_root)
    blend_result = validate_blend(project_root)
    export_result = validate_exports(project_root)
    report = write_report(project_root, blend_result, export_result)
    if report["status"] != "pass":
        print(json.dumps(report, indent=2))
        raise SystemExit(1)
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
