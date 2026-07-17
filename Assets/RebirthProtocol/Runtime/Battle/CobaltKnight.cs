using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Real rigged Bulwark chassis: the Cobalt Knight asset Codex built in
    // Blender (docs/KNIGHT_ROBOT_ASSET.md), loaded from the Resources copy
    // CobaltKnightResourceLinker made of KnightRobot_Body.prefab. No
    // authored animation exists yet — this is a static-mesh visual swap,
    // not a rigging pass; the model just stands in its rest pose.
    //
    // Socket coordinates below were read directly off the imported prefab
    // (Assets/RebirthProtocol/Editor/AssetHierarchyDumper, run once via
    // -executeMethod) rather than guessed — see docs/KNIGHT_ROBOT_ASSET.md's
    // socket list. The model's own forward is -Z (visor/muzzle sockets sit
    // at negative Z) where ours is +Z, and Socket_Gun/Socket_Hand_R sit on
    // the model's -X side where our right-arm convention is +X — a single
    // 180-degree Y rotation on the wrapper transform fixes both at once,
    // and every attachment below is parented under that same wrapper so it
    // inherits the flip automatically instead of needing hand-mirrored
    // coordinates.
    public static class CobaltKnight
    {
        private const string ResourcePath = "Mechs/CobaltKnightBody";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // Root sits ~2cm above true ground (Socket_Root world Y = 0.02) —
        // close enough to treat as ground level.
        private static readonly Vector3 SocketGun = new Vector3(-0.86f, 1.08f, -0.35f);
        private static readonly Vector3 SocketMuzzle = new Vector3(-0.86f, 1.08f, -1.20f);
        private static readonly Vector3 SocketHandL = new Vector3(0.91f, 0.98f, -0.23f);
        private static readonly Vector3 SocketHandR = new Vector3(-0.91f, 0.98f, -0.23f);
        private static readonly Vector3 SocketBomb = new Vector3(0.74f, 1.00f, -0.28f);
        private static readonly Vector3 SocketThrusterLegL = new Vector3(0.34f, 0.20f, 0.16f);
        private static readonly Vector3 SocketThrusterLegR = new Vector3(-0.34f, 0.20f, 0.16f);

        public static RoboVisual.Parts? Build(Transform tiltRoot, Loadout loadout, Color teamColor)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                return null;
            }

            var knightRoot = new GameObject("CobaltKnight").transform;
            knightRoot.SetParent(tiltRoot, false);
            knightRoot.localPosition = new Vector3(0f, -1f, 0f); // cancels tiltRoot's +1 (waist-height) offset
            knightRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var body = Object.Instantiate(prefab, knightRoot, false);
            body.name = "KnightBody";
            foreach (var collider in body.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider); // RoboAvatar's own CharacterController is the only collider
            }

            // KnightRobotAssetBuilder adds an empty Animator to every FBX
            // prefab it builds, for a future animation pass that hasn't
            // landed yet. Strip it for now — an Animator with no controller
            // assigned has no reason to be sitting on the visual, and this
            // avoids inheriting whatever default state it'd otherwise be in
            // once real clips do get wired up.
            foreach (var animator in body.GetComponentsInChildren<Animator>())
            {
                Object.Destroy(animator);
            }

            // Light team tint on top of the model's own baked cobalt/ivory/
            // aurum materials, via a MaterialPropertyBlock rather than
            // renderer.material — accessing .material does instantiate a
            // per-object copy as expected, but in the Editor/PlayMode-test
            // context that instantiation was observed leaving the SHARED
            // source .mat assets reserialized (dirtied) in git afterward.
            // A property block never touches the material asset at all.
            foreach (var renderer in body.GetComponentsInChildren<Renderer>())
            {
                var shared = renderer.sharedMaterial;
                if (shared == null)
                {
                    continue;
                }

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                if (shared.HasProperty(BaseColorId))
                {
                    block.SetColor(BaseColorId, Color.Lerp(shared.GetColor(BaseColorId), teamColor, 0.15f));
                }

                if (shared.HasProperty(ColorId))
                {
                    block.SetColor(ColorId, Color.Lerp(shared.GetColor(ColorId), teamColor, 0.15f));
                }

                renderer.SetPropertyBlock(block);
            }

            BuildRightArm(knightRoot, loadout, teamColor);
            BuildLeftArm(knightRoot, loadout, teamColor);
            BuildLegThrusters(knightRoot, teamColor);
            var blade = BuildBlade(knightRoot);

            // Shield-raise animation is skipped for this chassis for now —
            // RoboAvatar null-checks ShieldPlate before animating it, so
            // this is a clean, documented gap rather than a broken lerp
            // fighting the wrapper's own 180-degree rotation.
            return new RoboVisual.Parts { ShieldPlate = null, Blade = blade };
        }

        private static void BuildRightArm(Transform knightRoot, Loadout loadout, Color accent)
        {
            if (loadout.HasGun)
            {
                var barrelDir = (SocketMuzzle - SocketGun).normalized;
                var barrelLen = Vector3.Distance(SocketGun, SocketMuzzle);
                var barrel = Add(knightRoot, PrimitiveType.Cylinder,
                    Vector3.Lerp(SocketGun, SocketMuzzle, 0.5f), new Vector3(0.09f, barrelLen * 0.5f, 0.09f), accent);
                barrel.transform.localRotation = Quaternion.FromToRotation(Vector3.up, barrelDir);
            }
            else
            {
                // Melee weapon at rest, gripped at the hand socket.
                Add(knightRoot, PrimitiveType.Cube, SocketHandR + new Vector3(0f, 0f, -0.5f), new Vector3(0.06f, 0.12f, 0.95f), Color.Lerp(new Color(0.5f, 0.51f, 0.52f), accent, 0.2f));
            }
        }

        private static void BuildLeftArm(Transform knightRoot, Loadout loadout, Color accent)
        {
            if (loadout.HasBomb)
            {
                Add(knightRoot, PrimitiveType.Sphere, SocketBomb, Vector3.one * 0.3f, accent);
            }
            else
            {
                // Shield plate, static (no raise animation this pass) at
                // the left hand socket.
                Add(knightRoot, PrimitiveType.Cube, SocketHandL, new Vector3(0.08f, 0.75f, 0.6f), accent);
            }
        }

        private static void BuildLegThrusters(Transform knightRoot, Color accent)
        {
            Add(knightRoot, PrimitiveType.Cylinder, SocketThrusterLegL, new Vector3(0.09f, 0.12f, 0.09f), accent);
            Add(knightRoot, PrimitiveType.Cylinder, SocketThrusterLegR, new Vector3(0.09f, 0.12f, 0.09f), accent);
        }

        private static Transform BuildBlade(Transform knightRoot)
        {
            var blade = Add(knightRoot, PrimitiveType.Cube, SocketHandR + new Vector3(0f, 0.05f, -1.2f), new Vector3(0.15f, 0.5f, 2.2f), new Color(1f, 0.93f, 0.4f));
            blade.SetActive(false);
            return blade.transform;
        }

        private static GameObject Add(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = BattleMaterials.Lit(color);
            return part;
        }
    }
}
