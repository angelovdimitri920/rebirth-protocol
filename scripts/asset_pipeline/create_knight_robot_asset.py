import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ASSET_NAME = "KnightRobot"
FBX_OBJECT_TYPES = {"ARMATURE", "EMPTY", "MESH"}


def argv_after_double_dash():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    return parser.parse_args(argv_after_double_dash())


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)


def collection(name):
    existing = bpy.data.collections.get(name)
    if existing is not None:
        return existing
    new_collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(new_collection)
    return new_collection


def move_to_collection(obj, target):
    for existing in list(obj.users_collection):
        existing.objects.unlink(obj)
    target.objects.link(obj)


def create_material(name, color, metallic=0.0, roughness=0.42, emission=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if emission is not None:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength

    mat["unity_material_role"] = "original_knight_robot"
    return mat


def make_materials():
    return {
        "dark_iron": create_material("KR_Mat_DarkIron", (0.035, 0.04, 0.045, 1), 0.85, 0.32),
        "steel": create_material("KR_Mat_BurnishedSteel", (0.48, 0.51, 0.52, 1), 0.75, 0.27),
        "ivory": create_material("KR_Mat_IvoryCeramic", (0.82, 0.78, 0.66, 1), 0.18, 0.38),
        "cobalt": create_material("KR_Mat_CobaltArmor", (0.045, 0.13, 0.42, 1), 0.42, 0.34),
        "aurum": create_material("KR_Mat_AurumCore", (0.95, 0.63, 0.08, 1), 0.58, 0.27),
        "blue": create_material("KR_Mat_RoyalBlueGlow", (0.05, 0.22, 0.85, 1), 0.2, 0.18, (0.0, 0.28, 1.0, 1), 1.6),
        "ember": create_material("KR_Mat_EmberCore", (1.0, 0.28, 0.06, 1), 0.1, 0.24, (1.0, 0.22, 0.02, 1), 1.2),
        "joint": create_material("KR_Mat_RubberJoint", (0.012, 0.013, 0.014, 1), 0.2, 0.72),
        "copper": create_material("KR_Mat_CopperMuzzle", (0.86, 0.42, 0.18, 1), 0.65, 0.31),
        "teal": create_material("KR_Mat_PodTeal", (0.02, 0.55, 0.62, 1), 0.35, 0.24, (0.0, 0.55, 0.72, 1), 0.75),
    }


def set_active(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def tag(obj, role, bone=None, module=None):
    obj["rbp_asset"] = ASSET_NAME
    obj["rbp_role"] = role
    if bone:
        obj["rbp_rig_bone"] = bone
    if module:
        obj["rbp_module"] = module


def assign_material(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def add_bevel_and_normals(obj, width, segments=1):
    if width <= 0:
        return
    bevel = obj.modifiers.new("Production_Bevel", "BEVEL")
    bevel.width = width
    bevel.segments = segments
    bevel.affect = "EDGES"
    weighted = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True


def cube(name, loc, dims, mat, bone, target_collection, role="visual", module="body", bevel=0.025, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    set_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_material(obj, mat)
    add_bevel_and_normals(obj, bevel, 2 if bevel >= 0.03 else 1)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def cylinder(name, loc, radius, depth, mat, bone, target_collection, role="visual", module="body", vertices=24, bevel=0.0, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign_material(obj, mat)
    add_bevel_and_normals(obj, bevel, 1)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def cone(name, loc, radius1, radius2, depth, mat, bone, target_collection, role="visual", module="body", vertices=24, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign_material(obj, mat)
    add_bevel_and_normals(obj, 0.006, 1)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def torus(name, loc, major, minor, mat, bone, target_collection, role="visual", module="body", rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=32, minor_segments=8, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign_material(obj, mat)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def sphere(name, loc, scale, mat, bone, target_collection, role="visual", module="body", segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    set_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_material(obj, mat)
    add_bevel_and_normals(obj, 0.0, 1)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def prism_mesh(name, verts, faces, mat, bone, target_collection, role="visual", module="body", bevel=0.01):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    assign_material(obj, mat)
    add_bevel_and_normals(obj, bevel, 1)
    move_to_collection(obj, target_collection)
    tag(obj, role, bone, module)
    return obj


def chest_shield(name, mat, bone, target_collection):
    front_y = 0.235
    back_y = 0.185
    front = [(-0.38, front_y, 1.62), (0.38, front_y, 1.62), (0.32, front_y, 1.22), (0.0, front_y, 0.98), (-0.32, front_y, 1.22)]
    back = [(x, back_y, z) for x, _, z in front]
    verts = front + back
    faces = [
        (0, 1, 2, 3, 4),
        (9, 8, 7, 6, 5),
        (0, 5, 6, 1),
        (1, 6, 7, 2),
        (2, 7, 8, 3),
        (3, 8, 9, 4),
        (4, 9, 5, 0),
    ]
    return prism_mesh(name, verts, faces, mat, bone, target_collection, "armor_plate", "body", 0.018)


def add_rivet_row(objects, prefix, start_x, end_x, y, z, count, mat, bone, target_collection, module="body", radius=0.025):
    if count <= 1:
        xs = [start_x]
    else:
        xs = [start_x + (end_x - start_x) * index / (count - 1) for index in range(count)]

    for index, x in enumerate(xs, 1):
        objects.append(
            sphere(
                f"{prefix}_Rivet_{index:02d}",
                (x, y, z),
                (radius, radius * 0.45, radius),
                mat,
                bone,
                target_collection,
                "armor_rivet",
                module,
                12,
                6,
            )
        )


def add_heavy_knight_overarmor(materials, body, legs, gun, bomb, pod):
    objects = []

    # A much bulkier knight shell: squat, broad, horned, and layered.
    objects += [
        cube("KR_HeavyKnight_TorsoCobaltShell", (0, 0.405, 1.35), (1.16, 0.16, 0.62), materials["cobalt"], "chest", body, "heavy_knight_shell", "body", 0.06),
        cube("KR_HeavyKnight_TorsoLowerCobaltShell", (0, 0.39, 1.07), (0.86, 0.13, 0.24), materials["cobalt"], "chest", body, "heavy_knight_shell", "body", 0.04),
        cube("KR_HeavyKnight_LeftChestIvorySweep_A", (-0.33, 0.505, 1.38), (0.08, 0.028, 0.42), materials["ivory"], "chest", body, "painted_trim_geometry", "body", 0.006, (0, 0, math.radians(-18))),
        cube("KR_HeavyKnight_LeftChestIvorySweep_B", (-0.19, 0.51, 1.23), (0.07, 0.028, 0.32), materials["ivory"], "chest", body, "painted_trim_geometry", "body", 0.006, (0, 0, math.radians(24))),
        cube("KR_HeavyKnight_RightChestIvorySweep_A", (0.33, 0.505, 1.38), (0.08, 0.028, 0.42), materials["ivory"], "chest", body, "painted_trim_geometry", "body", 0.006, (0, 0, math.radians(18))),
        cube("KR_HeavyKnight_RightChestIvorySweep_B", (0.19, 0.51, 1.23), (0.07, 0.028, 0.32), materials["ivory"], "chest", body, "painted_trim_geometry", "body", 0.006, (0, 0, math.radians(-24))),
        cube("KR_HeavyKnight_AurumNeckCore", (0, 0.04, 1.79), (0.32, 0.36, 0.3), materials["aurum"], "neck", body, "neck_core", "body", 0.03),
        cube("KR_HeavyKnight_HeadlessCowling", (0, 0.18, 1.78), (0.68, 0.28, 0.18), materials["ivory"], "neck", body, "headless_cowling", "body", 0.025),
        cube("KR_CobaltKnight_BackMass", (0, -0.22, 1.33), (1.18, 0.28, 0.78), materials["dark_iron"], "chest", body, "rear_chassis", "body", 0.045),
        cube("KR_CobaltKnight_UpperCuirass", (0, 0.29, 1.47), (1.24, 0.22, 0.4), materials["steel"], "chest", body, "armor_plate", "body", 0.055),
        cube("KR_CobaltKnight_LowerCuirass", (0, 0.31, 1.17), (1.08, 0.2, 0.36), materials["dark_iron"], "chest", body, "armor_plate", "body", 0.045),
        chest_shield("KR_CobaltKnight_CrestShield", materials["ivory"], "chest", body),
        cylinder("KR_CobaltKnight_ChestReactor", (0, 0.43, 1.37), 0.145, 0.055, materials["blue"], "chest", body, "energy_core", "body", 32, 0.004, (math.pi / 2, 0, 0)),
        torus("KR_CobaltKnight_ReactorGuard", (0, 0.43, 1.37), 0.165, 0.014, materials["copper"], "chest", body, "armor_trim", "body", (math.pi / 2, 0, 0)),
        cube("KR_CobaltKnight_SternumBlade", (0, 0.455, 1.15), (0.12, 0.05, 0.45), materials["ivory"], "chest", body, "armor_spine", "body", 0.012),
        cube("KR_CobaltKnight_Crossbar", (0, 0.47, 1.43), (0.54, 0.045, 0.075), materials["ivory"], "chest", body, "armor_trim", "body", 0.01),
        cube("KR_CobaltKnight_NeckFortress", (0, 0.0, 1.71), (0.76, 0.48, 0.17), materials["dark_iron"], "neck", body, "armor_plate", "body", 0.03),
        cube("KR_CobaltKnight_HeavyHelmet", (0, 0.05, 1.93), (0.52, 0.42, 0.36), materials["dark_iron"], "head", body, "helmet", "body", 0.04),
        cube("KR_CobaltKnight_Faceplate", (0, 0.28, 1.88), (0.42, 0.055, 0.19), materials["steel"], "head", body, "faceplate", "body", 0.018),
        cube("KR_CobaltKnight_VisorBand", (0, 0.315, 1.96), (0.36, 0.026, 0.055), materials["blue"], "head", body, "visor", "body", 0.004),
        cube("KR_CobaltKnight_CrownPlate", (0, 0.02, 2.13), (0.32, 0.38, 0.12), materials["ivory"], "head", body, "armor_crest", "body", 0.02),
    ]

    # Horns and shoulder spikes give it a heavier crest-and-siege silhouette.
    for suffix, sign in (("L", -1), ("R", 1)):
        lower = suffix.lower()
        yrot = -sign * math.pi / 2
        objects += [
            cylinder(f"KR_HeavyKnight_ShoulderDrum_{suffix}", (sign * 0.76, 0.08, 1.57), 0.32, 0.58, materials["cobalt"], f"upper_arm_{lower}", body, "shoulder_drum", "body", 36, 0.006, (0, math.pi / 2, 0)),
            cylinder(f"KR_HeavyKnight_ShoulderEndCap_{suffix}", (sign * 1.07, 0.08, 1.57), 0.31, 0.055, materials["dark_iron"], f"upper_arm_{lower}", body, "shoulder_endcap", "body", 36, 0.004, (0, math.pi / 2, 0)),
            cube(f"KR_HeavyKnight_ShoulderIvoryCurl_A_{suffix}", (sign * 0.76, 0.395, 1.65), (0.24, 0.026, 0.055), materials["ivory"], f"upper_arm_{lower}", body, "painted_trim_geometry", "body", 0.005, (0, 0, sign * math.radians(20))),
            cube(f"KR_HeavyKnight_ShoulderIvoryCurl_B_{suffix}", (sign * 0.76, 0.395, 1.5), (0.27, 0.026, 0.055), materials["ivory"], f"upper_arm_{lower}", body, "painted_trim_geometry", "body", 0.005, (0, 0, sign * math.radians(-24))),
            cube(f"KR_HeavyKnight_UpperArmLongBlock_{suffix}", (sign * 0.98, 0.03, 1.23), (0.45, 0.24, 0.24), materials["dark_iron"], f"upper_arm_{lower}", body, "long_arm_chassis", "body", 0.025),
            cube(f"KR_HeavyKnight_WristAurumCoupler_{suffix}", (sign * 1.12, 0.19, 1.02), (0.15, 0.16, 0.18), materials["aurum"], f"hand_{lower}", body, "wrist_coupler", "body", 0.012),
            cylinder(f"KR_CobaltKnight_HornBase_{suffix}", (sign * 0.31, 0.12, 2.02), 0.095, 0.2, materials["ivory"], "head", body, "horn_base", "body", 20, 0.004, (0, math.pi / 2, 0)),
            cone(f"KR_CobaltKnight_HornMid_{suffix}", (sign * 0.47, 0.13, 2.03), 0.09, 0.045, 0.32, materials["ivory"], "head", body, "horn", "body", 24, (0, yrot, sign * math.radians(7))),
            cone(f"KR_CobaltKnight_HornTip_{suffix}", (sign * 0.67, 0.16, 2.08), 0.05, 0.0, 0.26, materials["ivory"], "head", body, "horn", "body", 20, (0, yrot, sign * math.radians(-12))),
            cube(f"KR_CobaltKnight_PauldronCore_{suffix}", (sign * 0.78, 0.0, 1.54), (0.62, 0.58, 0.34), materials["steel"], f"upper_arm_{lower}", body, "pauldron", "body", 0.06, (0, 0, sign * math.radians(9))),
            cube(f"KR_CobaltKnight_PauldronFrontPlate_{suffix}", (sign * 0.78, 0.31, 1.49), (0.5, 0.12, 0.22), materials["ivory"], f"upper_arm_{lower}", body, "pauldron_trim", "body", 0.025, (0, 0, sign * math.radians(9))),
            cone(f"KR_CobaltKnight_ShoulderHorn_{suffix}", (sign * 1.18, 0.07, 1.58), 0.17, 0.02, 0.42, materials["ivory"], f"upper_arm_{lower}", body, "shoulder_spike", "body", 24, (0, yrot, 0)),
            cylinder(f"KR_CobaltKnight_ElbowAxle_{suffix}", (sign * 0.84, 0.08, 1.22), 0.12, 0.24, materials["joint"], f"forearm_{lower}", body, "joint", "body", 18, 0.004, (0, math.pi / 2, 0)),
            cube(f"KR_CobaltKnight_ForearmShield_{suffix}", (sign * 0.93, 0.23, 1.12), (0.28, 0.28, 0.44), materials["ivory"], f"forearm_{lower}", body, "forearm_armor", "body", 0.035, (0, 0, sign * math.radians(5))),
            cube(f"KR_CobaltKnight_KnuckleBlock_{suffix}", (sign * 0.98, 0.38, 0.98), (0.24, 0.16, 0.13), materials["steel"], f"hand_{lower}", body, "hand_armor", "body", 0.018),
        ]
        for finger_index, offset in enumerate((-0.06, 0.0, 0.06), 1):
            objects.append(
                cube(
                    f"KR_CobaltKnight_Finger_{suffix}_{finger_index}",
                    (sign * (1.0 + abs(offset) * 0.2), 0.48, 0.93 + offset),
                    (0.045, 0.16, 0.04),
                    materials["dark_iron"],
                    f"hand_{lower}",
                    body,
                    "hand_claw",
                    "body",
                    0.006,
                )
            )

    add_rivet_row(objects, "KR_CobaltKnight_LeftChest", -0.5, -0.18, 0.47, 1.61, 4, materials["copper"], "chest", body)
    add_rivet_row(objects, "KR_CobaltKnight_RightChest", 0.18, 0.5, 0.47, 1.61, 4, materials["copper"], "chest", body)
    add_rivet_row(objects, "KR_CobaltKnight_Belly", -0.42, 0.42, 0.44, 1.05, 6, materials["copper"], "chest", body, radius=0.02)

    # Heavier "basic" legs now look like tanky siege knight legs instead of thin blocks.
    for suffix, sign in (("L", -1), ("R", 1)):
        lower = suffix.lower()
        objects += [
            cube(f"KR_HeavyKnight_CobaltThighCape_{suffix}", (sign * 0.36, 0.17, 0.68), (0.42, 0.18, 0.36), materials["cobalt"], f"thigh_{lower}", legs, "leg_shell", "basic_legs", 0.035),
            cube(f"KR_HeavyKnight_ThighIvoryMark_{suffix}", (sign * 0.36, 0.275, 0.7), (0.075, 0.026, 0.29), materials["ivory"], f"thigh_{lower}", legs, "painted_trim_geometry", "basic_legs", 0.005, (0, 0, sign * math.radians(-18))),
            cylinder(f"KR_HeavyKnight_KneeAurumPin_{suffix}", (sign * 0.55, 0.1, 0.48), 0.07, 0.08, materials["aurum"], f"shin_{lower}", legs, "knee_pin", "basic_legs", 18, 0.003, (0, math.pi / 2, 0)),
            cube(f"KR_CobaltKnight_HipSkirt_{suffix}", (sign * 0.34, 0.08, 0.88), (0.36, 0.28, 0.2), materials["dark_iron"], f"thigh_{lower}", legs, "hip_armor", "basic_legs", 0.028),
            cube(f"KR_CobaltKnight_ThighArmor_{suffix}", (sign * 0.33, 0.04, 0.58), (0.34, 0.34, 0.42), materials["steel"], f"thigh_{lower}", legs, "leg_armor", "basic_legs", 0.04),
            cube(f"KR_CobaltKnight_KneeRam_{suffix}", (sign * 0.36, 0.23, 0.41), (0.32, 0.17, 0.18), materials["ivory"], f"shin_{lower}", legs, "knee_armor", "basic_legs", 0.02),
            cone(f"KR_CobaltKnight_KneeSpike_{suffix}", (sign * 0.36, 0.37, 0.41), 0.08, 0.0, 0.16, materials["ivory"], f"shin_{lower}", legs, "knee_spike", "basic_legs", 16, (math.pi / 2, 0, 0)),
            cube(f"KR_CobaltKnight_ShinBulk_{suffix}", (sign * 0.36, 0.04, 0.24), (0.34, 0.3, 0.42), materials["ivory"], f"shin_{lower}", legs, "shin_armor", "basic_legs", 0.038),
            cube(f"KR_CobaltKnight_HeavyFoot_{suffix}", (sign * 0.36, 0.27, 0.07), (0.42, 0.55, 0.16), materials["dark_iron"], f"foot_{lower}", legs, "heavy_foot", "basic_legs", 0.035),
            cube(f"KR_CobaltKnight_ToeCap_{suffix}", (sign * 0.36, 0.56, 0.1), (0.38, 0.18, 0.12), materials["steel"], f"foot_{lower}", legs, "toe_armor", "basic_legs", 0.02),
            cube(f"KR_HeavyKnight_FootClawCenter_{suffix}", (sign * 0.36, 0.68, 0.07), (0.11, 0.2, 0.08), materials["ivory"], f"foot_{lower}", legs, "claw_foot", "basic_legs", 0.012),
            cube(f"KR_HeavyKnight_FootClawInner_{suffix}", (sign * 0.25, 0.63, 0.07), (0.09, 0.18, 0.075), materials["ivory"], f"foot_{lower}", legs, "claw_foot", "basic_legs", 0.012, (0, 0, sign * math.radians(-10))),
            cube(f"KR_HeavyKnight_FootClawOuter_{suffix}", (sign * 0.47, 0.63, 0.07), (0.09, 0.18, 0.075), materials["ivory"], f"foot_{lower}", legs, "claw_foot", "basic_legs", 0.012, (0, 0, sign * math.radians(10))),
            cylinder(f"KR_CobaltKnight_CalfThruster_{suffix}", (sign * 0.36, -0.18, 0.31), 0.075, 0.22, materials["blue"], f"shin_{lower}", legs, "thruster", "basic_legs", 18, 0.004, (math.pi / 2, 0, 0)),
        ]

    # The gun becomes a chunky lance cannon with a small shield guard and visible bolt projectile.
    objects += [
        cube("KR_CobaltKnight_GunReceiver", (0.92, 0.53, 1.08), (0.26, 0.42, 0.22), materials["dark_iron"], "weapon_gun", gun, "weapon_body", "basic_gun", 0.025),
        cube("KR_CobaltKnight_GunHandGuard", (0.92, 0.35, 1.08), (0.4, 0.08, 0.36), materials["ivory"], "weapon_gun", gun, "weapon_guard", "basic_gun", 0.018),
        cylinder("KR_CobaltKnight_GunUpperBarrel", (0.92, 0.92, 1.14), 0.045, 0.78, materials["steel"], "weapon_gun", gun, "weapon_barrel", "basic_gun", 24, 0.004, (math.pi / 2, 0, 0)),
        cylinder("KR_CobaltKnight_GunLowerBarrel", (0.92, 0.92, 1.02), 0.036, 0.68, materials["steel"], "weapon_gun", gun, "weapon_barrel", "basic_gun", 20, 0.004, (math.pi / 2, 0, 0)),
        torus("KR_CobaltKnight_GunMuzzleClamp", (0.92, 1.28, 1.08), 0.085, 0.012, materials["copper"], "muzzle", gun, "muzzle", "basic_gun", (math.pi / 2, 0, 0)),
        cone("KR_CobaltKnight_GunBoltTip", (0.92, 1.78, 1.12), 0.05, 0.0, 0.18, materials["blue"], "muzzle", gun, "projectile_reference", "basic_gun", 20, (math.pi / 2, 0, 0)),
    ]

    # Bomb reads as a heavy iron charge instead of a plain disk.
    objects += [
        sphere("KR_CobaltKnight_BombCore", (-0.8, 0.34, 0.98), (0.17, 0.17, 0.17), materials["dark_iron"], "weapon_bomb", bomb, "bomb_body", "basic_bomb", 24, 12),
        torus("KR_CobaltKnight_BombIronBand_A", (-0.8, 0.34, 0.98), 0.175, 0.014, materials["steel"], "weapon_bomb", bomb, "bomb_band", "basic_bomb", (math.pi / 2, 0, 0)),
        torus("KR_CobaltKnight_BombIronBand_B", (-0.8, 0.34, 0.98), 0.175, 0.014, materials["steel"], "weapon_bomb", bomb, "bomb_band", "basic_bomb", (0, math.pi / 2, 0)),
        cylinder("KR_CobaltKnight_BombFuseGlow", (-0.8, 0.54, 0.98), 0.052, 0.16, materials["ember"], "weapon_bomb", bomb, "effect_reference", "basic_bomb", 18, 0.003, (math.pi / 2, 0, 0)),
    ]

    # Back pod becomes a stocky siege launcher with twin tubes and fins.
    objects += [
        cube("KR_CobaltKnight_PodBackpack", (0, -0.45, 1.48), (0.62, 0.26, 0.4), materials["dark_iron"], "pod_back", pod, "pod_mount", "basic_pod", 0.03),
        cylinder("KR_CobaltKnight_PodTube_L", (-0.2, -0.72, 1.52), 0.075, 0.48, materials["teal"], "pod_back", pod, "pod_launcher", "basic_pod", 24, 0.004, (math.pi / 2, 0, 0)),
        cylinder("KR_CobaltKnight_PodTube_R", (0.2, -0.72, 1.52), 0.075, 0.48, materials["teal"], "pod_back", pod, "pod_launcher", "basic_pod", 24, 0.004, (math.pi / 2, 0, 0)),
        cube("KR_CobaltKnight_PodArmorCap_L", (-0.2, -0.48, 1.52), (0.2, 0.08, 0.2), materials["ivory"], "pod_back", pod, "pod_armor", "basic_pod", 0.014),
        cube("KR_CobaltKnight_PodArmorCap_R", (0.2, -0.48, 1.52), (0.2, 0.08, 0.2), materials["ivory"], "pod_back", pod, "pod_armor", "basic_pod", 0.014),
        sphere("KR_CobaltKnight_PodRound", (0, -1.02, 1.52), (0.09, 0.09, 0.09), materials["teal"], "pod_back", pod, "projectile_reference", "basic_pod", 20, 10),
    ]

    return objects


def create_armature():
    rig_collection = collection("Rig")
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    armature = bpy.context.object
    armature.name = "KR_Rig"
    armature.data.name = "KnightRobot_Armature"
    armature.data.display_type = "STICK"
    armature.show_in_front = True
    armature["rbp_asset"] = ASSET_NAME
    armature["rbp_rig_type"] = "hard_surface_rigid_weighted"
    move_to_collection(armature, rig_collection)

    edit_bones = armature.data.edit_bones
    root = edit_bones[0]
    root.name = "root"
    root.head = (0, 0, 0)
    root.tail = (0, 0, 0.2)

    def bone(name, head, tail, parent=None):
        b = edit_bones.new(name)
        b.head = head
        b.tail = tail
        if parent is not None:
            b.parent = parent
        return b

    pelvis = bone("pelvis", (0, 0, 0.74), (0, 0, 1.02), root)
    spine = bone("spine", pelvis.tail, (0, 0, 1.42), pelvis)
    chest = bone("chest", (0, 0, 1.28), (0, 0, 1.66), spine)
    neck = bone("neck", chest.tail, (0, 0, 1.78), chest)
    head = bone("head", neck.tail, (0, 0, 2.08), neck)
    pod = bone("pod_back", (0, -0.18, 1.35), (0, -0.48, 1.58), chest)
    weapon_bomb = bone("weapon_bomb", (-0.66, 0.08, 1.02), (-0.82, 0.22, 1.05), pelvis)

    for suffix, sign in (("l", -1), ("r", 1)):
        upper = bone(f"upper_arm_{suffix}", (sign * 0.47, 0, 1.52), (sign * 0.76, 0.02, 1.34), chest)
        forearm = bone(f"forearm_{suffix}", upper.tail, (sign * 0.9, 0.12, 1.08), upper)
        hand = bone(f"hand_{suffix}", forearm.tail, (sign * 0.93, 0.24, 0.98), forearm)
        thigh = bone(f"thigh_{suffix}", (sign * 0.24, 0, 0.86), (sign * 0.31, 0.02, 0.5), pelvis)
        shin = bone(f"shin_{suffix}", thigh.tail, (sign * 0.34, 0.04, 0.18), thigh)
        bone(f"foot_{suffix}", shin.tail, (sign * 0.34, 0.32, 0.1), shin)

    weapon_gun = bone("weapon_gun", (0.86, 0.14, 1.06), (0.86, 0.76, 1.08), edit_bones["hand_r"])
    bone("muzzle", weapon_gun.tail, (0.86, 1.02, 1.08), weapon_gun)
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def rigid_weight(obj, armature, bone_name):
    if obj.type != "MESH":
        return
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "ADD")
    modifier = obj.modifiers.new("KR_RigidSkin", "ARMATURE")
    modifier.object = armature
    modifier.use_vertex_groups = True
    obj.parent = armature


def socket(name, armature, bone_name, location, target_collection, size=0.12):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = "SPHERE"
    empty.empty_display_size = size
    empty.location = location
    empty.parent = armature
    empty["rbp_asset"] = ASSET_NAME
    empty["rbp_role"] = "unity_socket"
    empty["rbp_socket_bone"] = bone_name
    bpy.context.scene.collection.objects.link(empty)
    move_to_collection(empty, target_collection)
    return empty


def build_geometry(materials, armature):
    body = collection("KnightRobot_Body")
    legs = collection("Equipment_BasicLegs")
    gun = collection("Equipment_BasicGun")
    bomb = collection("Equipment_BasicBomb")
    pod = collection("Equipment_BasicPod")
    sockets = collection("Sockets")
    objects = []

    # Core knight body.
    objects += [
        cube("KR_Pelvis_Gimbal", (0, 0, 0.9), (0.66, 0.34, 0.28), materials["dark_iron"], "pelvis", body, "chassis", "body", 0.035),
        cube("KR_Torso_Cuirass", (0, 0, 1.32), (0.92, 0.44, 0.68), materials["steel"], "chest", body, "armor_plate", "body", 0.045),
        cube("KR_Torso_ReactorCore", (0, 0.255, 1.36), (0.18, 0.035, 0.24), materials["blue"], "chest", body, "energy_core", "body", 0.008),
        cube("KR_Collar_Ivory", (0, 0.02, 1.68), (0.58, 0.38, 0.12), materials["ivory"], "neck", body, "armor_plate", "body", 0.022),
        chest_shield("KR_Chest_KnightShield", materials["ivory"], "chest", body),
        cube("KR_Chest_Cross_Vertical", (0, 0.275, 1.33), (0.07, 0.03, 0.46), materials["steel"], "chest", body, "armor_trim", "body", 0.006),
        cube("KR_Chest_Cross_Horizontal", (0, 0.28, 1.43), (0.36, 0.03, 0.06), materials["steel"], "chest", body, "armor_trim", "body", 0.006),
        cube("KR_Helmet_Barbute", (0, 0, 1.81), (0.36, 0.3, 0.22), materials["dark_iron"], "head", body, "sunken_sensor_head", "body", 0.03),
        cube("KR_Helmet_VisorSlit", (0, 0.17, 1.84), (0.29, 0.025, 0.045), materials["blue"], "head", body, "visor", "body", 0.004),
        cube("KR_Helmet_Crest", (0, -0.02, 2.02), (0.1, 0.36, 0.13), materials["aurum"], "head", body, "armor_crest", "body", 0.012),
    ]

    for suffix, sign in (("L", -1), ("R", 1)):
        lower = suffix.lower()
        objects += [
            cube(f"KR_Pauldron_{suffix}", (sign * 0.58, -0.01, 1.51), (0.36, 0.42, 0.22), materials["ivory"], f"upper_arm_{lower}", body, "armor_plate", "body", 0.04, (0, 0, sign * math.radians(10))),
            cylinder(f"KR_UpperArm_Piston_{suffix}", (sign * 0.72, 0.01, 1.32), 0.075, 0.34, materials["joint"], f"upper_arm_{lower}", body, "joint", "body", 16, 0.004, (0, math.pi / 2, sign * 0.35)),
            cube(f"KR_Forearm_Gauntlet_{suffix}", (sign * 0.86, 0.08, 1.12), (0.2, 0.26, 0.34), materials["steel"], f"forearm_{lower}", body, "armor_plate", "body", 0.026, (0, 0, sign * math.radians(4))),
            cube(f"KR_Hand_Claw_{suffix}", (sign * 0.91, 0.23, 0.98), (0.18, 0.16, 0.16), materials["dark_iron"], f"hand_{lower}", body, "hand", "body", 0.018),
        ]

    # Basic legs are intentionally a named module.
    for suffix, sign in (("L", -1), ("R", 1)):
        lower = suffix.lower()
        objects += [
            cube(f"KR_BasicLegs_Thigh_{suffix}", (sign * 0.27, 0, 0.62), (0.22, 0.25, 0.38), materials["steel"], f"thigh_{lower}", legs, "leg_armor", "basic_legs", 0.028, (0, 0, sign * math.radians(3))),
            cube(f"KR_BasicLegs_KneeGuard_{suffix}", (sign * 0.32, 0.12, 0.42), (0.24, 0.1, 0.14), materials["ivory"], f"shin_{lower}", legs, "leg_armor", "basic_legs", 0.018),
            cube(f"KR_BasicLegs_ShinGreave_{suffix}", (sign * 0.34, 0.03, 0.27), (0.2, 0.22, 0.36), materials["ivory"], f"shin_{lower}", legs, "leg_armor", "basic_legs", 0.026),
            cube(f"KR_BasicLegs_Sabaton_{suffix}", (sign * 0.34, 0.21, 0.08), (0.24, 0.42, 0.14), materials["steel"], f"foot_{lower}", legs, "foot", "basic_legs", 0.025),
            cylinder(f"KR_BasicLegs_AnkleThruster_{suffix}", (sign * 0.34, -0.14, 0.2), 0.055, 0.18, materials["blue"], f"shin_{lower}", legs, "thruster", "basic_legs", 16, 0.003, (math.pi / 2, 0, 0)),
        ]

    # Basic gun: knight-lance silhouette, includes its projectile.
    objects += [
        cube("KR_BasicGun_Frame", (0.86, 0.42, 1.08), (0.16, 0.42, 0.14), materials["dark_iron"], "weapon_gun", gun, "weapon_body", "basic_gun", 0.018),
        cylinder("KR_BasicGun_Barrel", (0.86, 0.82, 1.08), 0.048, 0.72, materials["steel"], "weapon_gun", gun, "weapon_barrel", "basic_gun", 24, 0.004, (math.pi / 2, 0, 0)),
        cylinder("KR_BasicGun_MuzzleRing", (0.86, 1.18, 1.08), 0.065, 0.055, materials["copper"], "muzzle", gun, "muzzle", "basic_gun", 24, 0.002, (math.pi / 2, 0, 0)),
        cube("KR_BasicGun_SideMagazine", (0.99, 0.48, 1.0), (0.08, 0.2, 0.2), materials["ivory"], "weapon_gun", gun, "magazine", "basic_gun", 0.01),
        cylinder("KR_BasicGun_ProjectileBolt", (0.86, 1.48, 1.08), 0.028, 0.32, materials["blue"], "muzzle", gun, "projectile_reference", "basic_gun", 16, 0.002, (math.pi / 2, 0, 0)),
        cone("KR_BasicGun_ProjectileTip", (0.86, 1.66, 1.08), 0.04, 0.0, 0.12, materials["blue"], "muzzle", gun, "projectile_reference", "basic_gun", 16, (math.pi / 2, 0, 0)),
    ]

    # Basic bomb: readable casing plus effect guide.
    objects += [
        cylinder("KR_BasicBomb_Capsule", (-0.74, 0.28, 1.0), 0.115, 0.22, materials["dark_iron"], "weapon_bomb", bomb, "bomb_body", "basic_bomb", 24, 0.006, (math.pi / 2, 0, 0)),
        torus("KR_BasicBomb_ChargeRing", (-0.74, 0.28, 1.0), 0.13, 0.012, materials["ember"], "weapon_bomb", bomb, "bomb_charge", "basic_bomb", (math.pi / 2, 0, 0)),
        cube("KR_BasicBomb_GripMount", (-0.63, 0.16, 1.02), (0.08, 0.18, 0.12), materials["steel"], "weapon_bomb", bomb, "mount", "basic_bomb", 0.01),
        cylinder("KR_BasicBomb_ExplosionGuide", (-0.74, 0.7, 1.0), 0.18, 0.035, materials["ember"], "weapon_bomb", bomb, "effect_reference", "basic_bomb", 32, 0.002, (math.pi / 2, 0, 0)),
    ]

    # Basic pod: back launcher with separate pod round.
    objects += [
        cube("KR_BasicPod_BackDock", (0, -0.32, 1.44), (0.42, 0.16, 0.34), materials["dark_iron"], "pod_back", pod, "pod_mount", "basic_pod", 0.02),
        cylinder("KR_BasicPod_LaunchTube_L", (-0.14, -0.47, 1.46), 0.06, 0.38, materials["teal"], "pod_back", pod, "pod_launcher", "basic_pod", 24, 0.004, (math.pi / 2, 0, 0)),
        cylinder("KR_BasicPod_LaunchTube_R", (0.14, -0.47, 1.46), 0.06, 0.38, materials["teal"], "pod_back", pod, "pod_launcher", "basic_pod", 24, 0.004, (math.pi / 2, 0, 0)),
        cube("KR_BasicPod_StabilizerFin_L", (-0.28, -0.33, 1.52), (0.08, 0.08, 0.32), materials["ivory"], "pod_back", pod, "pod_fin", "basic_pod", 0.008, (0, 0, math.radians(12))),
        cube("KR_BasicPod_StabilizerFin_R", (0.28, -0.33, 1.52), (0.08, 0.08, 0.32), materials["ivory"], "pod_back", pod, "pod_fin", "basic_pod", 0.008, (0, 0, math.radians(-12))),
        cylinder("KR_BasicPod_ProjectileOrb", (0, -0.78, 1.46), 0.075, 0.12, materials["teal"], "pod_back", pod, "projectile_reference", "basic_pod", 24, 0.003, (math.pi / 2, 0, 0)),
    ]

    objects += add_heavy_knight_overarmor(materials, body, legs, gun, bomb, pod)

    for obj in objects:
        rigid_weight(obj, armature, obj["rbp_rig_bone"])

    socket_data = [
        ("Socket_Root", "root", (0, 0, 0.02), 0.16),
        ("Socket_Camera_Target", "head", (0, 0.08, 2.28), 0.12),
        ("Socket_Gun", "hand_r", (0.86, 0.35, 1.08), 0.11),
        ("Socket_Muzzle", "muzzle", (0.86, 1.2, 1.08), 0.08),
        ("Socket_Bomb", "weapon_bomb", (-0.74, 0.28, 1.0), 0.1),
        ("Socket_Pod", "pod_back", (0, -0.42, 1.48), 0.1),
        ("Socket_Hand_L", "hand_l", (-0.91, 0.23, 0.98), 0.1),
        ("Socket_Hand_R", "hand_r", (0.91, 0.23, 0.98), 0.1),
        ("Socket_Thruster_Leg_L", "shin_l", (-0.34, -0.16, 0.2), 0.08),
        ("Socket_Thruster_Leg_R", "shin_r", (0.34, -0.16, 0.2), 0.08),
    ]
    for name, bone, loc, size in socket_data:
        socket(name, armature, bone, loc, sockets, size)

    return objects


def setup_scene_metadata():
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene["asset_name"] = ASSET_NAME
    bpy.context.scene["asset_status"] = "original_production_track"
    bpy.context.scene["design_note"] = "Second-pass Cobalt Knight redesign with heavier original hard-surface massing."
    bpy.context.scene["license_note"] = "Original procedural mesh generated for Rebirth Protocol; no third-party source mesh is reused."
    bpy.context.scene["rig_note"] = "Hard-surface rigid skinning; every mesh is assigned to a single deform bone."


def setup_preview_scene(materials):
    preview = collection("Preview")
    bpy.ops.object.light_add(type="AREA", location=(0, -3.5, 4.2))
    key = bpy.context.object
    key.name = "Preview_KeyLight"
    key.data.energy = 600
    key.data.size = 4.5
    move_to_collection(key, preview)

    camera_location = Vector((2.8, 4.9, 2.45))
    target_location = Vector((0, 0.18, 1.2))
    camera_rotation = (target_location - camera_location).to_track_quat("-Z", "Y").to_euler()
    bpy.ops.object.camera_add(location=camera_location, rotation=camera_rotation)
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 40
    bpy.context.scene.camera = camera
    move_to_collection(camera, preview)

    cube("Preview_Floor", (0, 0, -0.035), (2.7, 2.7, 0.035), materials["dark_iron"], None, preview, "preview_only", "preview", 0.0)
    bpy.data.objects["Preview_Floor"]["rbp_ignore_export"] = True


def export_fbx(path, objects):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types=FBX_OBJECT_TYPES,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_custom_props=True,
        path_mode="AUTO",
    )


def render_preview(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 64
    scene.render.film_transparent = False
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def write_manifest(path, exports, mesh_objects):
    path.parent.mkdir(parents=True, exist_ok=True)
    modules = {}
    for obj in mesh_objects:
        module = obj.get("rbp_module", "unknown")
        modules.setdefault(module, 0)
        modules[module] += 1

    manifest = {
        "asset": ASSET_NAME,
        "status": "original_production_track",
        "design": "Cobalt Knight heavy mech redesign with original generated hard-surface geometry.",
        "scale": "meters",
        "rig": {
            "type": "hard_surface_rigid_skin",
            "deformation": "single-bone rigid weights per mesh",
            "animationRig": "Unity Generic",
        },
        "modules": modules,
        "exports": {name: str(path_value) for name, path_value in exports.items()},
        "notes": [
            "Original geometry and material language; no source meshes from Custom Robo are reused.",
            "Second pass intentionally adds broad shoulders, enlarged horns, layered cuirass armor, larger feet, rivets, and heavier weapon silhouettes.",
            "Includes basic gun projectile, basic bomb effect guide, and basic pod projectile reference meshes.",
            "Further polish should add animation clips, LODs, final collision, and hand-authored texture maps.",
        ],
    }
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def main():
    args = parse_args()
    project_root = Path(args.project_root)
    clear_scene()
    materials = make_materials()
    armature = create_armature()
    mesh_objects = build_geometry(materials, armature)
    setup_scene_metadata()
    setup_preview_scene(materials)

    source_dir = project_root / "SourceArt" / "KnightRobot"
    fbx_dir = project_root / "Assets" / "RebirthProtocol" / "Art" / "Mechs" / "KnightRobot" / "FBX"
    preview_dir = project_root / "docs" / "images"
    manifest_path = project_root / "Assets" / "RebirthProtocol" / "Art" / "Mechs" / "KnightRobot" / "knight_robot_manifest.json"

    source_dir.mkdir(parents=True, exist_ok=True)
    blend_path = source_dir / "KnightRobot_Loadout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    exportable = [
        obj for obj in bpy.context.scene.objects
        if not obj.get("rbp_ignore_export") and obj.type in FBX_OBJECT_TYPES
    ]
    body_modules = {"body", "basic_legs"}
    exports = {
        "loadout": fbx_dir / "KnightRobot_Loadout.fbx",
        "body": fbx_dir / "KnightRobot_Body.fbx",
        "basic_gun": fbx_dir / "KnightRobot_BasicGun.fbx",
        "basic_bomb": fbx_dir / "KnightRobot_BasicBomb.fbx",
        "basic_legs": fbx_dir / "KnightRobot_BasicLegs.fbx",
        "basic_pod": fbx_dir / "KnightRobot_BasicPod.fbx",
    }

    export_fbx(exports["loadout"], exportable)
    export_fbx(exports["body"], [armature] + [obj for obj in exportable if obj.type == "EMPTY" or obj.get("rbp_module") in body_modules])
    for module in ("basic_gun", "basic_bomb", "basic_legs", "basic_pod"):
        export_fbx(exports[module], [armature] + [obj for obj in exportable if obj.type == "EMPTY" or obj.get("rbp_module") == module])

    preview_path = preview_dir / "knight_robot_preview.png"
    render_preview(preview_path)
    write_manifest(manifest_path, exports | {"source_blend": blend_path, "preview": preview_path}, mesh_objects)
    print(f"Created {ASSET_NAME}: {blend_path}")
    print(f"Preview: {preview_path}")
    print(f"Exports: {fbx_dir}")


if __name__ == "__main__":
    main()
