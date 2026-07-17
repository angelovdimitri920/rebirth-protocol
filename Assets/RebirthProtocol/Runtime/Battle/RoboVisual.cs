using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Data-driven primitive visuals, ported from the prototype's RoboMesh:
    // every chassis/right-arm/left-arm/legs choice reads differently at a
    // glance, per-chassis hull palettes are tinted toward the team color so
    // player-vs-enemy stays readable even on matching chassis. This is the
    // bridge until the rigged Blender mechs (Cobalt Knight et al.) land
    // with the animation pass.
    public static class RoboVisual
    {
        public struct Parts
        {
            public Transform ShieldPlate; // raised/lowered by RoboAvatar
            public Transform Blade;
        }

        private static (Color hull, Color joint) Palette(string bodyId) => bodyId switch
        {
            "skylance" => (Rgb(0xdd, 0xe4, 0xf0), Rgb(0x3c, 0x46, 0x58)), // pale aerospace silver
            "wraith" => (Rgb(0x34, 0x2a, 0x44), Rgb(0x17, 0x13, 0x20)),   // dark violet-black, stealth
            "bulwark" => (Rgb(0x9a, 0x70, 0x38), Rgb(0x3e, 0x2c, 0x16)),  // bronze-olive, heavy armor
            _ => (Rgb(0x5a, 0x70, 0x99), Rgb(0x26, 0x2b, 0x38))           // steel blue-grey, all-rounder
        };

        private static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        public static Parts Build(Transform tiltRoot, Loadout loadout, Color teamColor)
        {
            // Bulwark is the one chassis with a real rigged asset (Cobalt
            // Knight) available; the other three stay primitive until their
            // own models land. No animation yet either way — this is a
            // static-mesh visual upgrade, not a rigging pass.
            if (loadout.Body.Id == "bulwark")
            {
                var knightParts = CobaltKnight.Build(tiltRoot, loadout, teamColor);
                if (knightParts.HasValue)
                {
                    return knightParts.Value;
                }
                // Resources copy missing (e.g. the linker step was never
                // run) — fall through to the primitive chassis so the game
                // still renders something instead of nothing.
            }

            var (hullBase, jointBase) = Palette(loadout.Body.Id);
            var hull = Color.Lerp(hullBase, teamColor, 0.22f);
            var joint = Color.Lerp(jointBase, teamColor, 0.15f);
            // Wraith identity: minimal glow, built to not be seen — hull
            // tones only, no bright accent panels.
            var accent = loadout.Body.Id == "wraith" ? joint : teamColor;

            BuildChassis(tiltRoot, loadout.Body.Id, hull, joint, accent);
            BuildRightArm(tiltRoot, loadout, hull, joint, accent);
            var shield = BuildLeftArm(tiltRoot, loadout, hull, joint, accent);
            BuildLegs(tiltRoot, loadout.Legs.Id, hull, joint, accent);

            var blade = Add(tiltRoot, PrimitiveType.Cube, new Vector3(0f, 0.2f, 1.4f), new Vector3(0.15f, 0.5f, 2.2f), new Color(1f, 0.93f, 0.4f));
            blade.SetActive(false);

            return new Parts { ShieldPlate = shield, Blade = blade.transform };
        }

        private static void BuildChassis(Transform root, string bodyId, Color hull, Color joint, Color accent)
        {
            switch (bodyId)
            {
                case "skylance": // glass-cannon flier: slim, tall, winged
                    Add(root, PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0f), new Vector3(0.78f, 1.08f, 0.78f), hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, -0.15f, 0f), new Vector3(0.5f, 0.16f, 0.42f), joint); // cinched waist
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.08f), new Vector3(0.4f, 0.3f, 0.42f), hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.88f, 0.3f), new Vector3(0.3f, 0.08f, 0.05f), accent); // visor
                    Add(root, PrimitiveType.Cube, new Vector3(-0.5f, 0.55f, -0.35f), new Vector3(0.1f, 0.7f, 0.35f), hull, new Vector3(0f, 25f, -30f)); // wing fins
                    Add(root, PrimitiveType.Cube, new Vector3(0.5f, 0.55f, -0.35f), new Vector3(0.1f, 0.7f, 0.35f), hull, new Vector3(0f, -25f, 30f));
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.4f), new Vector3(0.25f, 0.4f, 0.1f), accent); // chest crest
                    break;

                case "wraith": // evader: short, hooded, angular, no glow
                    Add(root, PrimitiveType.Capsule, new Vector3(0f, -0.1f, 0f), new Vector3(0.85f, 0.82f, 0.85f), hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.02f), new Vector3(0.52f, 0.3f, 0.55f), joint, new Vector3(-12f, 0f, 0f)); // hood/cowl
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.28f), new Vector3(0.3f, 0.06f, 0.06f), joint); // dark visor slit
                    Add(root, PrimitiveType.Cube, new Vector3(0.05f, 0.1f, 0.42f), new Vector3(0.5f, 0.12f, 0.06f), joint, new Vector3(0f, 0f, -28f)); // wrap-sash
                    Add(root, PrimitiveType.Cube, new Vector3(0f, -0.35f, -0.35f), new Vector3(0.4f, 0.6f, 0.08f), joint, new Vector3(18f, 0f, 0f)); // trailing cloth
                    break;

                case "bulwark": // tank: wide, towering pauldrons, heraldic cross
                    Add(root, PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(1.25f, 1.05f, 1.15f), hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.82f, 0.08f), new Vector3(0.55f, 0.38f, 0.52f), hull); // great helm
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.84f, 0.36f), new Vector3(0.34f, 0.07f, 0.05f), accent); // visor slit
                    Add(root, PrimitiveType.Cube, new Vector3(-0.78f, 0.42f, 0f), new Vector3(0.42f, 0.55f, 0.5f), joint); // heavy pauldrons
                    Add(root, PrimitiveType.Cube, new Vector3(0.78f, 0.42f, 0f), new Vector3(0.42f, 0.55f, 0.5f), joint);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0.56f), new Vector3(0.1f, 0.5f, 0.06f), accent); // heraldic cross
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.56f), new Vector3(0.32f, 0.1f, 0.06f), accent);
                    break;

                default: // vanguard: balanced all-rounder, banded plating
                    Add(root, PrimitiveType.Capsule, Vector3.zero, Vector3.one, hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.1f), new Vector3(0.5f, 0.35f, 0.5f), hull);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.32f), new Vector3(0.36f, 0.1f, 0.05f), accent); // visor
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.05f), new Vector3(0.08f, 0.16f, 0.4f), accent); // centurion crest
                    Add(root, PrimitiveType.Cube, new Vector3(-0.62f, 0.35f, 0f), new Vector3(0.3f, 0.45f, 0.35f), joint); // pauldrons
                    Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.35f, 0f), new Vector3(0.3f, 0.45f, 0.35f), joint);
                    Add(root, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.42f), new Vector3(0.4f, 0.5f, 0.12f), accent); // scutum chest panel
                    Add(root, PrimitiveType.Cube, new Vector3(0f, -0.25f, 0.38f), new Vector3(0.55f, 0.08f, 0.1f), joint); // banded plating
                    Add(root, PrimitiveType.Cube, new Vector3(0f, -0.42f, 0.36f), new Vector3(0.55f, 0.08f, 0.1f), joint);
                    break;
            }
        }

        private static void BuildRightArm(Transform root, Loadout loadout, Color hull, Color joint, Color accent)
        {
            if (loadout.HasGun)
            {
                switch (loadout.Gun.Id)
                {
                    case "needler": // twin thin barrels
                        Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.05f, 0.35f), new Vector3(0.18f, 0.18f, 0.5f), joint);
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.56f, 0.09f, 0.75f), new Vector3(0.06f, 0.3f, 0.06f), hull, new Vector3(90f, 0f, 0f));
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.68f, 0.01f, 0.75f), new Vector3(0.06f, 0.3f, 0.06f), hull, new Vector3(90f, 0f, 0f));
                        break;
                    case "ram-cannon": // one fat siege barrel
                        Add(root, PrimitiveType.Cube, new Vector3(0.66f, 0.05f, 0.25f), new Vector3(0.3f, 0.3f, 0.4f), joint);
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.66f, 0.05f, 0.8f), new Vector3(0.22f, 0.45f, 0.22f), hull, new Vector3(90f, 0f, 0f));
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.66f, 0.05f, 1.15f), new Vector3(0.26f, 0.06f, 0.26f), accent, new Vector3(90f, 0f, 0f)); // muzzle band
                        break;
                    case "blaster":
                        Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.05f, 0.3f), new Vector3(0.2f, 0.24f, 0.45f), joint);
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.62f, 0.08f, 0.75f), new Vector3(0.1f, 0.35f, 0.1f), hull, new Vector3(90f, 0f, 0f));
                        break;
                }
            }
            else
            {
                switch (loadout.Melee.Id)
                {
                    case "warhammer": // big head on a pole
                        Add(root, PrimitiveType.Cylinder, new Vector3(0.62f, 0.15f, 0.25f), new Vector3(0.06f, 0.55f, 0.06f), joint, new Vector3(35f, 0f, 0f));
                        Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.55f, 0.55f), new Vector3(0.3f, 0.22f, 0.22f), hull);
                        break;
                    case "twin-fang": // two short blades
                        Add(root, PrimitiveType.Cube, new Vector3(0.6f, -0.05f, 0.45f), new Vector3(0.05f, 0.1f, 0.6f), hull);
                        Add(root, PrimitiveType.Cube, new Vector3(-0.6f, -0.05f, 0.45f), new Vector3(0.05f, 0.1f, 0.6f), hull);
                        break;
                    case "saber": // one long blade at rest
                        Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.0f, 0.5f), new Vector3(0.06f, 0.12f, 0.95f), hull);
                        Add(root, PrimitiveType.Cube, new Vector3(0.62f, 0.0f, 0.05f), new Vector3(0.1f, 0.16f, 0.12f), accent); // hilt
                        break;
                }
            }
        }

        private static Transform BuildLeftArm(Transform root, Loadout loadout, Color hull, Color joint, Color accent)
        {
            if (loadout.HasShield)
            {
                var isBastion = loadout.Shield.Id == "bastion";
                var plate = Add(root, PrimitiveType.Cube,
                    new Vector3(-0.68f, 0.1f, 0.15f),
                    isBastion ? new Vector3(0.14f, 0.85f, 0.7f) : new Vector3(0.06f, 0.7f, 0.55f),
                    isBastion ? joint : accent);
                return plate.transform;
            }

            // Bomb: a pouch/charge on the left hip.
            var isQuake = loadout.Bomb.Id == "quake";
            Add(root, PrimitiveType.Sphere, new Vector3(-0.55f, -0.35f, 0.1f),
                Vector3.one * (isQuake ? 0.4f : 0.28f), isQuake ? joint : accent);
            return null;
        }

        private static void BuildLegs(Transform root, string legsId, Color hull, Color joint, Color accent)
        {
            switch (legsId)
            {
                case "cheetah": // fast and low: angled shin blades
                    Add(root, PrimitiveType.Cube, new Vector3(-0.28f, -0.85f, 0.1f), new Vector3(0.16f, 0.5f, 0.2f), joint, new Vector3(-20f, 0f, 0f));
                    Add(root, PrimitiveType.Cube, new Vector3(0.28f, -0.85f, 0.1f), new Vector3(0.16f, 0.5f, 0.2f), joint, new Vector3(-20f, 0f, 0f));
                    break;
                case "cricket": // sky rig: thruster cylinders
                    Add(root, PrimitiveType.Cylinder, new Vector3(-0.28f, -0.85f, -0.1f), new Vector3(0.14f, 0.28f, 0.14f), accent);
                    Add(root, PrimitiveType.Cylinder, new Vector3(0.28f, -0.85f, -0.1f), new Vector3(0.14f, 0.28f, 0.14f), accent);
                    break;
                default: // strider: plain greaves
                    Add(root, PrimitiveType.Cube, new Vector3(-0.28f, -0.85f, 0f), new Vector3(0.22f, 0.45f, 0.28f), joint);
                    Add(root, PrimitiveType.Cube, new Vector3(0.28f, -0.85f, 0f), new Vector3(0.22f, 0.45f, 0.28f), joint);
                    break;
            }
        }

        private static GameObject Add(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, Vector3 euler = default)
        {
            var part = GameObject.CreatePrimitive(type);
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.GetComponent<Renderer>().material = BattleMaterials.Lit(color);
            return part;
        }
    }
}
