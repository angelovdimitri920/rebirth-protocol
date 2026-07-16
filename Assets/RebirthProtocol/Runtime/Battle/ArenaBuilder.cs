using System.Collections.Generic;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Holosseum layouts (HOLOSSEUM_REFERENCE.md): same sealed base volume,
    // different cover/hazard overlays — exactly the prototype's Arena
    // architecture. Layouts rotate per launch/rematch alongside the enemy
    // build so every fight reads a little differently.
    public static class ArenaBuilder
    {
        public readonly struct LavaPool
        {
            public LavaPool(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }

            public Vector2 Center { get; }
            public float Radius { get; }
        }

        public sealed class Result
        {
            /// XZ rectangles where grounded movement is icy.
            public readonly List<Rect> IceRegions = new List<Rect>();

            /// Circular pools (XZ center + radius) that deal continuous
            /// environmental damage to anyone grounded inside them.
            public readonly List<LavaPool> LavaPools = new List<LavaPool>();
            public string Name;
        }

        public static Result Build(Transform parent, int layoutIndex)
        {
            var result = new Result();
            var size = CombatTuning.Arena.Size;
            var half = size * 0.5f;

            Block(parent, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(size, 1f, size), new Color(0.16f, 0.17f, 0.2f));

            var wallColor = new Color(0.3f, 0.32f, 0.38f);
            var visH = CombatTuning.Arena.VisibleWallHeight;
            Block(parent, "Wall N", new Vector3(0f, visH * 0.5f, half + 0.5f), new Vector3(size + 2f, visH, 1f), wallColor);
            Block(parent, "Wall S", new Vector3(0f, visH * 0.5f, -half - 0.5f), new Vector3(size + 2f, visH, 1f), wallColor);
            Block(parent, "Wall E", new Vector3(half + 0.5f, visH * 0.5f, 0f), new Vector3(1f, visH, size + 2f), wallColor);
            Block(parent, "Wall W", new Vector3(-half - 0.5f, visH * 0.5f, 0f), new Vector3(1f, visH, size + 2f), wallColor);

            var wallH = CombatTuning.Arena.WallHeight;
            Invisible(parent, "Bound N", new Vector3(0f, wallH * 0.5f, half + 0.5f), new Vector3(size + 2f, wallH, 1f));
            Invisible(parent, "Bound S", new Vector3(0f, wallH * 0.5f, -half - 0.5f), new Vector3(size + 2f, wallH, 1f));
            Invisible(parent, "Bound E", new Vector3(half + 0.5f, wallH * 0.5f, 0f), new Vector3(1f, wallH, size + 2f));
            Invisible(parent, "Bound W", new Vector3(-half - 0.5f, wallH * 0.5f, 0f), new Vector3(1f, wallH, size + 2f));
            Invisible(parent, "Ceiling", new Vector3(0f, wallH, 0f), new Vector3(size + 2f, 1f, size + 2f));

            switch (layoutIndex % 4)
            {
                case 1: // Colonnade: tall unbreakable pillars, sparse crates
                    result.Name = "Colonnade";
                    var pillarColor = new Color(0.36f, 0.35f, 0.42f);
                    foreach (var p in new[] { new Vector3(6f, 3f, 6f), new Vector3(-6f, 3f, 6f), new Vector3(6f, 3f, -6f), new Vector3(-6f, 3f, -6f) })
                    {
                        Block(parent, "Pillar", p, new Vector3(2f, 6f, 2f), pillarColor);
                    }

                    Crate(parent, new Vector3(0f, 0.8f, 11f));
                    Crate(parent, new Vector3(0f, 0.8f, -11f));
                    break;

                case 2: // Frostfield: central ice sheet, crates at the rim
                    result.Name = "Frostfield";
                    var ice = Block(parent, "Ice", new Vector3(0f, 0.01f, 0f), new Vector3(14f, 0.02f, 14f), new Color(0.62f, 0.78f, 0.92f));
                    Object.Destroy(ice.GetComponent<Collider>()); // visual only; the hazard is a movement rule
                    result.IceRegions.Add(new Rect(-7f, -7f, 14f, 14f));
                    Crate(parent, new Vector3(10f, 0.8f, 10f));
                    Crate(parent, new Vector3(-10f, 0.8f, -10f));
                    Crate(parent, new Vector3(-10f, 0.8f, 10f));
                    Crate(parent, new Vector3(10f, 0.8f, -10f));
                    break;

                case 3: // Cinderfield: lava pools deal continuous DoT (24 hp/s, 14 endurance/s, bypasses shields)
                    result.Name = "Cinderfield";
                    foreach (var (x, z, r) in new[] { (-8f, 3f, 3f), (8f, -3f, 3f), (0f, 10f, 2.4f) })
                    {
                        result.LavaPools.Add(new LavaPool(new Vector2(x, z), r));
                        var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pool.name = "Lava Pool";
                        pool.transform.SetParent(parent, false);
                        pool.transform.position = new Vector3(x, 0.02f, z);
                        pool.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
                        Object.Destroy(pool.GetComponent<Collider>()); // visual only; the hazard is a damage rule
                        pool.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(1f, 0.27f, 0f));
                    }

                    Crate(parent, new Vector3(-4f, 0.8f, -8f));
                    Crate(parent, new Vector3(6f, 0.8f, 6f));
                    break;

                default: // Depot: the classic crate field
                    result.Name = "Depot";
                    foreach (var p in new[]
                    {
                        new Vector3(5f, 0.8f, 5f), new Vector3(-5f, 0.8f, -5f), new Vector3(-7f, 0.8f, 6f),
                        new Vector3(7f, 0.8f, -5f), new Vector3(0f, 0.8f, 10f), new Vector3(-2f, 0.8f, -11f)
                    })
                    {
                        Crate(parent, p);
                    }

                    break;
            }

            return result;
        }

        private static void Crate(Transform parent, Vector3 pos)
        {
            var crate = Block(parent, "Crate", pos, new Vector3(1.6f, 1.6f, 1.6f), new Color(0.45f, 0.36f, 0.24f));
            crate.AddComponent<CrateHealth>();
        }

        private static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = BattleMaterials.Lit(color);
            return go;
        }

        private static void Invisible(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Renderer>());
        }
    }
}
