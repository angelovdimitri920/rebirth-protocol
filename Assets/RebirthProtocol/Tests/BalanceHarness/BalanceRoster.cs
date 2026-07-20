using System;
using System.Collections.Generic;
using System.Linq;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.BalanceHarness
{
    public sealed class NamedBuild
    {
        public string Name;
        public Loadout Loadout;
    }

    public sealed class MatrixSpec
    {
        public string Name;
        public List<NamedBuild> Builds;
    }

    // What the harness actually fights, until Pass M builds the literal
    // four-schools roster (Freelance/Skyanvil/Sunplume/Harrier are not in
    // the catalog yet). Scoping call, flagged in the Pass D log: the first
    // matrices cover what doctrine already names must-stay-viable with
    // built parts — pillar 5's four loadout shapes (gun/bomb, gun/shield,
    // melee/bomb, melee/shield) and the four built bodies. Two 4×4 default
    // matrices rather than one confounded 16×16: each axis varies alone
    // (shapes on the neutral body; bodies on the neutral kit), so a flag
    // points at a shape or a body, never an ambiguous combination. The
    // 16×16 cross and a future schools roster are just other MatrixSpecs —
    // Pass M slots in here without touching the runner.
    public static class BalanceRoster
    {
        // The neutral kit: the "knight's standard" of every slot.
        private static GunPart Gun => PartsCatalog.Guns.First(g => g.Id == "blaster"); // Arbalest
        private static MeleeWeaponPart Melee => PartsCatalog.MeleeWeapons.First(m => m.Id == "saber"); // Oathblade
        private static BombPart Bomb => PartsCatalog.Bombs.First(b => b.Id == "impact"); // Censer
        private static ShieldPart Shield => PartsCatalog.Shields.First(s => s.Id == "kite-ward");
        private static LegsPart Legs => PartsCatalog.Legs.First(l => l.Id == "strider"); // Wayfarer
        private static PodPart Pod => PartsCatalog.Pods.First(p => p.Id == "sentry"); // Iron Squire

        private static BodyPart NeutralBody => PartsCatalog.Bodies.First(b => b.Id == "vanguard"); // Bannerman

        private static Loadout Build(BodyPart body, bool gunArm, bool bombArm) => new Loadout
        {
            Body = body,
            Gun = gunArm ? Gun : null,
            Melee = gunArm ? null : Melee,
            Bomb = bombArm ? Bomb : null,
            Shield = bombArm ? null : Shield,
            Legs = Legs,
            Pod = Pod
        };

        private static readonly (string Name, bool GunArm, bool BombArm)[] Shapes =
        {
            ("gun-bomb", true, true),
            ("gun-shield", true, false),
            ("melee-bomb", false, true),
            ("melee-shield", false, false)
        };

        /// Pillar 5's four loadout shapes, all on Bannerman + neutral kit.
        public static MatrixSpec ShapesMatrix() => new MatrixSpec
        {
            Name = "loadout-shapes (on Bannerman)",
            Builds = Shapes
                .Select(s => new NamedBuild { Name = s.Name, Loadout = Build(NeutralBody, s.GunArm, s.BombArm) })
                .ToList()
        };

        /// The four built bodies, all on the neutral gun/bomb kit.
        public static MatrixSpec BodiesMatrix() => new MatrixSpec
        {
            Name = "bodies (gun/bomb kit)",
            Builds = PartsCatalog.Bodies
                .Select(b => new NamedBuild { Name = b.Name, Loadout = Build(b, true, true) })
                .ToList()
        };

        /// The full 16-build sweep: every shape on every body. 136 pairings —
        /// run deliberately, not as the default.
        public static MatrixSpec CrossMatrix() => new MatrixSpec
        {
            Name = "shapes × bodies cross",
            Builds = PartsCatalog.Bodies
                .SelectMany(b => Shapes.Select(s => new NamedBuild
                {
                    Name = $"{b.Name}/{s.Name}",
                    Loadout = Build(b, s.GunArm, s.BombArm)
                }))
                .ToList()
        };

        public static List<MatrixSpec> Select(string roster) => roster switch
        {
            "default" => new List<MatrixSpec> { ShapesMatrix(), BodiesMatrix() },
            "shapes" => new List<MatrixSpec> { ShapesMatrix() },
            "bodies" => new List<MatrixSpec> { BodiesMatrix() },
            "cross" => new List<MatrixSpec> { CrossMatrix() },
            _ => throw new ArgumentException($"Unknown roster '{roster}' (default|shapes|bodies|cross).")
        };
    }
}
