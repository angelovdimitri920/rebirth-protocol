import argparse
import json
import math
import re
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--category", choices=["all", "arenas", "robos", "parts"], default="all")
    args = sys_argv_after_double_dash()
    return parser.parse_args(args)


def sys_argv_after_double_dash():
    import sys

    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def safe_name(name):
    cleaned = re.sub(r'[\\/:*?"<>|]', "-", name)
    cleaned = re.sub(r"\s+", " ", cleaned)
    return cleaned.strip()


PROJECTILE_KEYWORDS = ("bullet", "projectile", "shot", "round", "beam", "missile", "bomb", "explosion", "effect")


def is_projectile_reference(*names):
    combined = " ".join(str(name).lower() for name in names if name)
    return any(keyword in combined for keyword in PROJECTILE_KEYWORDS)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)


def import_obj(filepath):
    before = set(bpy.data.objects)
    if hasattr(bpy.ops.wm, "obj_import"):
        bpy.ops.wm.obj_import(filepath=str(filepath), forward_axis="NEGATIVE_Z", up_axis="Y")
    else:
        bpy.ops.import_scene.obj(filepath=str(filepath), axis_forward="-Z", axis_up="Y")
    after = set(bpy.data.objects)
    return [obj for obj in after - before if obj.type == "MESH"]


def mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def bounds_for(objects):
    coords = []
    for obj in objects:
        for corner in obj.bound_box:
            coords.append(obj.matrix_world @ Vector(corner))
    if not coords:
        return Vector((0, 0, 0)), Vector((0, 0, 0))
    min_v = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_v = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return min_v, max_v


def normalize_objects(objects, category):
    min_v, max_v = bounds_for(objects)
    size = max_v - min_v
    max_dim = max(size.x, size.y, size.z, 0.0001)
    target = {"arenas": 24.0, "robos": 2.2, "parts": 1.2}.get(category, 4.0)
    scale = target / max_dim
    center = (min_v + max_v) * 0.5

    for obj in objects:
        obj.location -= center
        obj.scale *= scale

    bpy.context.view_layer.update()
    min_v, _ = bounds_for(objects)
    for obj in objects:
        obj.location.z -= min_v.z
    bpy.context.view_layer.update()
    return scale


def ensure_collection(name):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collection)
    return collection


def move_to_collection(obj, collection):
    for existing in list(obj.users_collection):
        existing.objects.unlink(obj)
    collection.objects.link(obj)


def create_empty(name, location, display_type="PLAIN_AXES", size=0.35):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = display_type
    empty.empty_display_size = size
    empty.location = location
    bpy.context.scene.collection.objects.link(empty)
    return empty


def setup_scene_metadata(asset_name, category, source_obj, scale):
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene["asset_name"] = asset_name
    scene["category"] = category
    scene["source_obj"] = str(source_obj)
    scene["reference_only"] = True
    scene["normalization_scale"] = scale


def setup_arena(asset_name, objects):
    root = create_empty("Arena_Root", Vector((0, 0, 0)), "CUBE", 1.0)
    for obj in objects:
        obj.parent = root
        obj["unity_role"] = "arena_visual_reference"

    min_v, max_v = bounds_for(objects)
    center = (min_v + max_v) * 0.5
    extent = max_v - min_v
    create_empty("Arena_Center", center, "SPHERE", 0.5).parent = root
    create_empty("Spawn_Player", Vector((center.x - extent.x * 0.25, center.y, center.z + 0.05)), "ARROWS", 0.6).parent = root
    create_empty("Spawn_Enemy", Vector((center.x + extent.x * 0.25, center.y, center.z + 0.05)), "ARROWS", 0.6).parent = root
    create_empty("Camera_Target", Vector((center.x, center.y, max_v.z + max(extent.x, extent.y) * 0.35)), "SPHERE", 0.7).parent = root
    create_empty("Bounds_Min", min_v, "CUBE", 0.25).parent = root
    create_empty("Bounds_Max", max_v, "CUBE", 0.25).parent = root

    bpy.ops.mesh.primitive_cube_add(size=1, location=(center.x, center.y, min_v.z + 0.02))
    collider = bpy.context.object
    collider.name = "Arena_Bounds_Collider_Approx"
    collider.dimensions = (max(extent.x, 0.1), max(extent.y, 0.1), 0.04)
    collider.display_type = "WIRE"
    collider.hide_render = True
    collider["unity_role"] = "collision_guide_not_final"
    collider.parent = root
    bpy.context.view_layer.update()


def create_robo_armature(asset_name, objects):
    min_v, max_v = bounds_for(objects)
    size = max_v - min_v
    center_x = (min_v.x + max_v.x) * 0.5
    center_y = (min_v.y + max_v.y) * 0.5
    z0 = min_v.z
    z1 = max_v.z
    height = max(size.z, 0.1)

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    armature = bpy.context.object
    armature.name = f"{asset_name}_Rig"
    armature.data.name = f"{asset_name}_Armature"
    armature["rig_status"] = "auto scaffold; manual weight cleanup required"

    edit_bones = armature.data.edit_bones
    root = edit_bones[0]
    root.name = "root"
    root.head = (center_x, center_y, z0)
    root.tail = (center_x, center_y, z0 + height * 0.12)

    def bone(name, head, tail, parent=None):
        b = edit_bones.new(name)
        b.head = head
        b.tail = tail
        if parent is not None:
            b.parent = parent
        return b

    pelvis = bone("pelvis", (center_x, center_y, z0 + height * 0.12), (center_x, center_y, z0 + height * 0.35), root)
    spine = bone("spine", pelvis.tail, (center_x, center_y, z0 + height * 0.68), pelvis)
    head = bone("head", spine.tail, (center_x, center_y, z1), spine)

    side = max(size.x * 0.35, 0.25)
    depth = max(size.y * 0.15, 0.12)
    for suffix, sign in (("L", -1), ("R", 1)):
        shoulder = bone(f"upper_arm_{suffix.lower()}", spine.tail, (center_x + sign * side, center_y, z0 + height * 0.58), spine)
        forearm = bone(f"forearm_{suffix.lower()}", shoulder.tail, (center_x + sign * side * 1.25, center_y, z0 + height * 0.42), shoulder)
        bone(f"hand_{suffix.lower()}", forearm.tail, (center_x + sign * side * 1.35, center_y + depth, z0 + height * 0.36), forearm)
        thigh = bone(f"thigh_{suffix.lower()}", pelvis.head, (center_x + sign * side * 0.35, center_y, z0 + height * 0.16), pelvis)
        shin = bone(f"shin_{suffix.lower()}", thigh.tail, (center_x + sign * side * 0.35, center_y, z0 + height * 0.04), thigh)
        bone(f"foot_{suffix.lower()}", shin.tail, (center_x + sign * side * 0.35, center_y + depth, z0), shin)

    bpy.ops.object.mode_set(mode="OBJECT")

    for obj in objects:
        obj["rig_status"] = "armature modifier scaffold added; automatic weights not guaranteed"
        modifier = obj.modifiers.new("Robo_Rig", "ARMATURE")
        modifier.object = armature

    root_empty = create_empty("Robo_Root", Vector((center_x, center_y, z0)), "CUBE", 0.8)
    armature.parent = root_empty
    for obj in objects:
        obj.parent = root_empty

    sockets = {
        "Socket_Hand_L": Vector((center_x - side * 1.35, center_y + depth, z0 + height * 0.36)),
        "Socket_Hand_R": Vector((center_x + side * 1.35, center_y + depth, z0 + height * 0.36)),
        "Socket_Gun": Vector((center_x + side * 1.55, center_y + depth * 1.5, z0 + height * 0.38)),
        "Socket_Melee": Vector((center_x - side * 1.55, center_y + depth * 1.5, z0 + height * 0.38)),
        "Socket_Shield": Vector((center_x - side * 1.25, center_y - depth, z0 + height * 0.48)),
        "Socket_Pod_L": Vector((center_x - side * 0.6, center_y - depth, z0 + height * 0.7)),
        "Socket_Pod_R": Vector((center_x + side * 0.6, center_y - depth, z0 + height * 0.7)),
        "Socket_Thruster_Back": Vector((center_x, center_y - depth * 2, z0 + height * 0.5)),
        "Socket_Thruster_Leg_L": Vector((center_x - side * 0.35, center_y - depth, z0 + height * 0.08)),
        "Socket_Thruster_Leg_R": Vector((center_x + side * 0.35, center_y - depth, z0 + height * 0.08)),
        "Socket_Camera_Target": Vector((center_x, center_y, z1 + height * 0.3)),
    }
    for name, location in sockets.items():
        socket = create_empty(name, location, "SPHERE", 0.2)
        socket.parent = root_empty
        socket["unity_role"] = "socket"


def setup_part(asset_name, objects):
    root = create_empty("Part_Root", Vector((0, 0, 0)), "CUBE", 0.5)
    for obj in objects:
        is_projectile = is_projectile_reference(
            obj.name,
            obj.get("source_obj_name", ""),
            obj.get("source_obj", ""),
        )
        obj.parent = root
        obj["unity_role"] = "projectile_reference" if is_projectile else "part_visual_reference"

    min_v, max_v = bounds_for(objects)
    center = (min_v + max_v) * 0.5
    create_empty("Socket_Attach", center, "ARROWS", 0.25).parent = root
    create_empty("Socket_Muzzle_Or_Effect", Vector((center.x, max_v.y, center.z)), "SPHERE", 0.2).parent = root


def process_asset(project_root, category, asset_dir, reports):
    asset_name = safe_name(asset_dir.name)
    obj_files = sorted(asset_dir.rglob("*.obj"))
    if not obj_files:
        reports.append({
            "category": category,
            "asset": asset_name,
            "status": "skipped_no_obj",
            "source": str(asset_dir),
        })
        return

    clear_scene()
    all_objects = []
    for obj_file in obj_files:
        imported = import_obj(obj_file)
        for obj in imported:
            obj["source_obj"] = str(obj_file)
            obj["source_obj_name"] = obj_file.stem
        all_objects.extend(imported)

    objects = mesh_objects()
    if not objects:
        reports.append({
            "category": category,
            "asset": asset_name,
            "status": "failed_no_mesh_objects",
            "source": str(asset_dir),
        })
        return

    mesh_collection = ensure_collection("Imported_Meshes")
    for obj in objects:
        move_to_collection(obj, mesh_collection)
        obj.name = safe_name(obj.name)

    scale = normalize_objects(objects, category)
    setup_scene_metadata(asset_name, category, obj_files[0], scale)

    projectile_count = 0
    if category == "arenas":
        setup_arena(asset_name, objects)
        status = "arena_blend_and_fbx_created"
    elif category == "robos":
        create_robo_armature(asset_name, objects)
        status = "robo_rig_scaffold_created"
    else:
        setup_part(asset_name, objects)
        projectile_count = sum(1 for obj in objects if obj.get("unity_role") == "projectile_reference")
        status = "part_scene_created"

    blend_dir = project_root / "ReferenceOnly" / "Blender" / category / asset_name
    fbx_dir = project_root / "ReferenceOnly" / "UnityReferenceFBX" / category
    blend_dir.mkdir(parents=True, exist_ok=True)
    fbx_dir.mkdir(parents=True, exist_ok=True)
    blend_path = blend_dir / f"{asset_name}.blend"
    fbx_path = fbx_dir / f"{asset_name}.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=False,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        object_types={"ARMATURE", "EMPTY", "MESH"},
    )

    min_v, max_v = bounds_for(objects)
    reports.append({
        "category": category,
        "asset": asset_name,
        "status": status,
        "source": str(asset_dir),
        "objCount": len(obj_files),
        "meshObjectCount": len(objects),
        "blend": str(blend_path),
        "fbx": str(fbx_path),
        "bounds": {
            "min": [round(min_v.x, 4), round(min_v.y, 4), round(min_v.z, 4)],
            "max": [round(max_v.x, 4), round(max_v.y, 4), round(max_v.z, 4)],
        },
        "normalizationScale": scale,
        "projectileObjectCount": projectile_count,
    })


def write_reports(project_root, reports):
    report_dir = project_root / "ReferenceOnly" / "Reports"
    docs_dir = project_root / "docs"
    report_dir.mkdir(parents=True, exist_ok=True)
    docs_dir.mkdir(parents=True, exist_ok=True)

    json_path = report_dir / "blender_asset_report.json"
    json_path.write_text(json.dumps(reports, indent=2), encoding="utf-8")

    lines = [
        "# Blender Asset Processing Report",
        "",
        "All processed assets are reference-only unless replaced by original production art.",
        "",
        "| Category | Asset | Status | Mesh Objects | Projectile/Effect Objects | Blend | FBX |",
        "| --- | --- | --- | ---: | ---: | --- | --- |",
    ]
    for item in reports:
        lines.append(
            f"| {item.get('category', '')} | {item.get('asset', '')} | {item.get('status', '')} | "
            f"{item.get('meshObjectCount', 0)} | {item.get('projectileObjectCount', 0)} | "
            f"`{item.get('blend', '')}` | `{item.get('fbx', '')}` |"
        )
    lines += [
        "",
        "Rig note: robo scenes include a standardized armature and sockets so they are rig-ready/manipulable in Blender. Automatic production-quality skin weights, deformation cleanup, and original-art redesign still require manual art work.",
        "",
        "Arena note: arena scenes include center/spawn/camera markers and an approximate bounds collider guide for Unity prototyping.",
        "",
        "Parts note: projectile/bullet/effect sub-objects are preserved when the ZIP exposes them as separate OBJ files. Imported objects keep `source_obj` and `source_obj_name` metadata so companion meshes remain identifiable in Blender/FBX workflows.",
    ]
    (docs_dir / "ASSET_BLENDER_PROCESSING.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    args = parse_args()
    project_root = Path(args.project_root)
    extracted_root = project_root / "ReferenceOnly" / "Extracted"
    categories = ["arenas", "robos", "parts"] if args.category == "all" else [args.category]
    reports = []
    for category in categories:
        category_dir = extracted_root / category
        if not category_dir.exists():
            continue
        for asset_dir in sorted([p for p in category_dir.iterdir() if p.is_dir()]):
            process_asset(project_root, category, asset_dir, reports)
    write_reports(project_root, reports)


if __name__ == "__main__":
    main()
