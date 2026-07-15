import argparse
import json
import sys
from pathlib import Path

import bpy


ARENA_REQUIRED_OBJECTS = {
    "Arena_Root",
    "Arena_Center",
    "Spawn_Player",
    "Spawn_Enemy",
    "Camera_Target",
    "Bounds_Min",
    "Bounds_Max",
    "Arena_Bounds_Collider_Approx",
}

ROBO_REQUIRED_OBJECTS = {
    "Robo_Root",
    "Socket_Hand_L",
    "Socket_Hand_R",
    "Socket_Gun",
    "Socket_Melee",
    "Socket_Shield",
    "Socket_Pod_L",
    "Socket_Pod_R",
    "Socket_Thruster_Back",
    "Socket_Thruster_Leg_L",
    "Socket_Thruster_Leg_R",
    "Socket_Camera_Target",
}

ROBO_REQUIRED_BONES = {
    "root",
    "pelvis",
    "spine",
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
}

PART_REQUIRED_OBJECTS = {
    "Part_Root",
    "Socket_Attach",
    "Socket_Muzzle_Or_Effect",
}


def sys_argv_after_double_dash():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    return parser.parse_args(sys_argv_after_double_dash())


def missing_objects(required):
    object_names = set(bpy.data.objects.keys())
    return sorted(required - object_names)


def mesh_objects():
    return [obj for obj in bpy.data.objects if obj.type == "MESH"]


def armature_objects():
    return [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]


def validate_arena(issues):
    missing = missing_objects(ARENA_REQUIRED_OBJECTS)
    if missing:
        issues.append(f"missing arena objects: {', '.join(missing)}")

    collider = bpy.data.objects.get("Arena_Bounds_Collider_Approx")
    if collider is not None and collider.get("unity_role") != "collision_guide_not_final":
        issues.append("arena bounds collider is missing collision guide metadata")


def validate_robo(issues):
    missing = missing_objects(ROBO_REQUIRED_OBJECTS)
    if missing:
        issues.append(f"missing robo sockets: {', '.join(missing)}")

    armatures = armature_objects()
    if not armatures:
        issues.append("missing robo armature")
        return

    bones = set(armatures[0].data.bones.keys())
    missing_bones = sorted(ROBO_REQUIRED_BONES - bones)
    if missing_bones:
        issues.append(f"missing robo bones: {', '.join(missing_bones)}")

    rigged_meshes = [
        obj for obj in mesh_objects()
        if any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if not rigged_meshes:
        issues.append("no mesh has an armature modifier")


def validate_part(item, issues):
    missing = missing_objects(PART_REQUIRED_OBJECTS)
    if missing:
        issues.append(f"missing part objects: {', '.join(missing)}")

    meshes = mesh_objects()
    meshes_without_roles = [obj.name for obj in meshes if not obj.get("unity_role")]
    if meshes_without_roles:
        issues.append(f"meshes missing unity_role metadata: {', '.join(meshes_without_roles[:5])}")

    projectile_count = sum(1 for obj in meshes if obj.get("unity_role") == "projectile_reference")
    asset = item.get("asset", "")
    if asset.startswith(("Bomb Parts", "Gun Parts", "Pod Parts")) and projectile_count == 0:
        issues.append("expected at least one projectile/effect object")

    return projectile_count


def validate_item(item):
    blend_path = Path(item.get("blend", ""))
    fbx_path = Path(item.get("fbx", ""))
    issues = []
    projectile_count = 0

    if not blend_path.exists():
        issues.append(f"missing blend file: {blend_path}")
    if not fbx_path.exists():
        issues.append(f"missing fbx file: {fbx_path}")

    if blend_path.exists():
        bpy.ops.wm.open_mainfile(filepath=str(blend_path))
        if not mesh_objects():
            issues.append("no mesh objects found")

        category = item.get("category")
        if category == "arenas":
            validate_arena(issues)
        elif category == "robos":
            validate_robo(issues)
        elif category == "parts":
            projectile_count = validate_part(item, issues)
        else:
            issues.append(f"unknown category: {category}")

    return {
        "category": item.get("category", ""),
        "asset": item.get("asset", ""),
        "blend": str(blend_path),
        "fbx": str(fbx_path),
        "status": "pass" if not issues else "fail",
        "issues": issues,
        "projectileObjectCount": projectile_count if item.get("category") == "parts" else item.get("projectileObjectCount", 0),
    }


def write_reports(project_root, results):
    report_dir = project_root / "ReferenceOnly" / "Reports"
    docs_dir = project_root / "docs"
    report_dir.mkdir(parents=True, exist_ok=True)
    docs_dir.mkdir(parents=True, exist_ok=True)

    (report_dir / "blender_validation_report.json").write_text(json.dumps(results, indent=2), encoding="utf-8")

    total = len(results)
    passed = sum(1 for item in results if item["status"] == "pass")
    failed = total - passed
    lines = [
        "# Blender Asset Validation",
        "",
        f"Validated assets: {total}",
        f"Passed: {passed}",
        f"Failed: {failed}",
        "",
        "| Category | Asset | Status | Issues |",
        "| --- | --- | --- | --- |",
    ]
    for item in results:
        issue_text = "<none>" if not item["issues"] else "; ".join(item["issues"])
        lines.append(f"| {item['category']} | {item['asset']} | {item['status']} | {issue_text} |")

    (docs_dir / "ASSET_VALIDATION.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    args = parse_args()
    project_root = Path(args.project_root)
    asset_report_path = project_root / "ReferenceOnly" / "Reports" / "blender_asset_report.json"
    items = json.loads(asset_report_path.read_text(encoding="utf-8"))
    results = [validate_item(item) for item in items]
    write_reports(project_root, results)

    failures = [item for item in results if item["status"] == "fail"]
    if failures:
        for failure in failures:
            print(f"{failure['category']} / {failure['asset']}: {'; '.join(failure['issues'])}")
        raise SystemExit(1)

    print(f"Validated {len(results)} Blender assets.")


if __name__ == "__main__":
    main()
