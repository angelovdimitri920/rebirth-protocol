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

        // Volley capability (ARMORY §4-6, Pass E, DOCTRINE §13 pillar 3
        // "volley truth"): each new spread part swapped one at a time
        // against its plain counterpart, everything else held at the
        // neutral kit — a flag here points straight at the new part, not
        // an ambiguous shape/body confound.
        private static GunPart TrefoilGun => PartsCatalog.Guns.First(g => g.Id == "trefoil");
        private static MeleeWeaponPart LongglaiveMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "longglaive");
        private static MeleeWeaponPart HydraFlailMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "hydra-flail");
        private static BombPart PalisadeBomb => PartsCatalog.Bombs.First(b => b.Id == "palisade");
        private static BombPart PincerChargeBomb => PartsCatalog.Bombs.First(b => b.Id == "pincer-charge");

        private static Loadout WithGun(GunPart gun) => new Loadout { Body = NeutralBody, Gun = gun, Bomb = Bomb, Legs = Legs, Pod = Pod };
        private static Loadout WithMelee(MeleeWeaponPart melee) => new Loadout { Body = NeutralBody, Melee = melee, Bomb = Bomb, Legs = Legs, Pod = Pod };
        private static Loadout WithBomb(BombPart bomb) => new Loadout { Body = NeutralBody, Gun = Gun, Bomb = bomb, Legs = Legs, Pod = Pod };

        public static MatrixSpec VolleyMatrix() => new MatrixSpec
        {
            Name = "volley capability (on Bannerman, neutral kit otherwise)",
            Builds = new List<NamedBuild>
            {
                new NamedBuild { Name = "arbalest (baseline gun)", Loadout = WithGun(Gun) },
                new NamedBuild { Name = "trefoil", Loadout = WithGun(TrefoilGun) },
                new NamedBuild { Name = "oathblade (baseline melee)", Loadout = WithMelee(Melee) },
                new NamedBuild { Name = "longglaive", Loadout = WithMelee(LongglaiveMelee) },
                new NamedBuild { Name = "hydra-flail", Loadout = WithMelee(HydraFlailMelee) },
                new NamedBuild { Name = "censer (baseline bomb)", Loadout = WithBomb(Bomb) },
                new NamedBuild { Name = "palisade", Loadout = WithBomb(PalisadeBomb) },
                new NamedBuild { Name = "pincer-charge", Loadout = WithBomb(PincerChargeBomb) }
            }
        };

        // Fetter capability (ARMORY §4-9, Pass F, DOCTRINE §13 pillar 9
        // "Fetter chains" watchlist entry): each new Fetter-carrying part
        // swapped one at a time against its plain counterpart, same
        // one-axis-at-a-time shape as VolleyMatrix — a flag here points
        // straight at the new part (or, for a mirror match, at the fetter-
        // immunity rule itself if a lock ever proved degenerate).
        private static GunPart FetterlockGun => PartsCatalog.Guns.First(g => g.Id == "fetterlock");
        private static MeleeWeaponPart KnellMaulMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "knell-maul");
        private static MeleeWeaponPart TocsinMaceMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "tocsin-mace");
        private static BombPart RimeChargeBomb => PartsCatalog.Bombs.First(b => b.Id == "rime-charge");
        private static ShieldPart HoarfrostWardShield => PartsCatalog.Shields.First(s => s.Id == "hoarfrost-ward");
        private static PodPart WinterwatchPod => PartsCatalog.Pods.First(p => p.Id == "winterwatch");

        private static Loadout WithShield(ShieldPart shield) => new Loadout { Body = NeutralBody, Gun = Gun, Shield = shield, Legs = Legs, Pod = Pod };
        private static Loadout WithPod(PodPart pod) => new Loadout { Body = NeutralBody, Gun = Gun, Bomb = Bomb, Legs = Legs, Pod = pod };

        public static MatrixSpec FetterMatrix() => new MatrixSpec
        {
            Name = "fetter capability (on Bannerman, neutral kit otherwise)",
            Builds = new List<NamedBuild>
            {
                new NamedBuild { Name = "arbalest (baseline gun)", Loadout = WithGun(Gun) },
                new NamedBuild { Name = "fetterlock", Loadout = WithGun(FetterlockGun) },
                new NamedBuild { Name = "oathblade (baseline melee)", Loadout = WithMelee(Melee) },
                new NamedBuild { Name = "knell-maul", Loadout = WithMelee(KnellMaulMelee) },
                new NamedBuild { Name = "tocsin-mace", Loadout = WithMelee(TocsinMaceMelee) },
                new NamedBuild { Name = "censer (baseline bomb)", Loadout = WithBomb(Bomb) },
                new NamedBuild { Name = "rime-charge", Loadout = WithBomb(RimeChargeBomb) },
                new NamedBuild { Name = "kite-ward (baseline shield)", Loadout = WithShield(Shield) },
                new NamedBuild { Name = "hoarfrost-ward", Loadout = WithShield(HoarfrostWardShield) },
                new NamedBuild { Name = "iron-squire (baseline pod)", Loadout = WithPod(Pod) },
                new NamedBuild { Name = "winterwatch", Loadout = WithPod(WinterwatchPod) }
            }
        };

        // Pull/pierce capability (ARMORY §4-5, Pass G): each new part
        // swapped one at a time against its plain counterpart, same
        // one-axis-at-a-time shape as Volley/Fetter. Pulls interact with
        // knockback and positioning, exactly the kind of displacement that
        // could produce a degenerate approach/lock loop the harness would
        // catch — Estoc's guard-pierce is measured on the gun/SHIELD shape
        // (Grapnel is the neutral bomb-arm counterpart's opponent) so a
        // raised shield actually exists for it to pierce.
        private static GunPart GrapnelGun => PartsCatalog.Guns.First(g => g.Id == "grapnel");
        private static GunPart AugerGun => PartsCatalog.Guns.First(g => g.Id == "auger");
        private static MeleeWeaponPart HookbillMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "hookbill");
        private static MeleeWeaponPart EstocMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "estoc");
        private static MeleeWeaponPart SawtoothEspadonMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "sawtooth-espadon");

        // A melee-vs-shield shape so Estoc's guard-pierce has a raised
        // shield to bite: attacker melee-arm, defender kept on the same kit
        // via the runner's mirror pairing. The runner fights every build
        // against every other, so pairing a melee-shield loadout in surfaces
        // the pierce against a live shield-user.
        private static Loadout WithMeleeAndShield(MeleeWeaponPart melee) =>
            new Loadout { Body = NeutralBody, Melee = melee, Shield = Shield, Legs = Legs, Pod = Pod };

        public static MatrixSpec PullsMatrix() => new MatrixSpec
        {
            Name = "pull/pierce capability (on Bannerman, neutral kit otherwise)",
            Builds = new List<NamedBuild>
            {
                new NamedBuild { Name = "arbalest (baseline gun)", Loadout = WithGun(Gun) },
                new NamedBuild { Name = "grapnel", Loadout = WithGun(GrapnelGun) },
                new NamedBuild { Name = "auger", Loadout = WithGun(AugerGun) },
                new NamedBuild { Name = "oathblade (baseline melee)", Loadout = WithMelee(Melee) },
                new NamedBuild { Name = "hookbill", Loadout = WithMelee(HookbillMelee) },
                new NamedBuild { Name = "sawtooth-espadon", Loadout = WithMelee(SawtoothEspadonMelee) },
                // Estoc + its counterpart shape carry shields so the pierce
                // is actually exercised against a raised guard.
                new NamedBuild { Name = "oathblade/shield (baseline pierce ctrl)", Loadout = WithMeleeAndShield(Melee) },
                new NamedBuild { Name = "estoc/shield", Loadout = WithMeleeAndShield(EstocMelee) }
            }
        };

        // Scaling & delayed threats (ARMORY §13.1, Pass H): each new part
        // swapped one at a time against its plain counterpart, same
        // one-axis shape as Volley/Fetter/Pulls. These parts' whole point is
        // that damage varies with position/speed/timing, so a static AI that
        // never keeps the long field (Pilgrim), spaces a burst (Beacon), or
        // charges a joust (Tilt Lance) will UNDER-read them — that under-read
        // is itself the headline Pass O signal, not a tuning error to chase.
        private static GunPart PilgrimGun => PartsCatalog.Guns.First(g => g.Id == "pilgrim");
        private static GunPart VigilGun => PartsCatalog.Guns.First(g => g.Id == "vigil");
        private static GunPart BeaconGun => PartsCatalog.Guns.First(g => g.Id == "beacon");
        private static MeleeWeaponPart CourserSaberMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "courser-saber");
        private static MeleeWeaponPart TiltLanceMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "tilt-lance");
        private static MeleeWeaponPart PenitentFlailMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "penitent-flail");
        private static MeleeWeaponPart CrowbeakPickMelee => PartsCatalog.MeleeWeapons.First(m => m.Id == "crowbeak-pick");

        public static MatrixSpec ScalingMatrix() => new MatrixSpec
        {
            Name = "scaling & delayed threats (on Bannerman, neutral kit otherwise)",
            Builds = new List<NamedBuild>
            {
                new NamedBuild { Name = "arbalest (baseline gun)", Loadout = WithGun(Gun) },
                new NamedBuild { Name = "pilgrim", Loadout = WithGun(PilgrimGun) },
                new NamedBuild { Name = "vigil", Loadout = WithGun(VigilGun) },
                new NamedBuild { Name = "beacon", Loadout = WithGun(BeaconGun) },
                new NamedBuild { Name = "oathblade (baseline melee)", Loadout = WithMelee(Melee) },
                new NamedBuild { Name = "courser-saber", Loadout = WithMelee(CourserSaberMelee) },
                new NamedBuild { Name = "tilt-lance", Loadout = WithMelee(TiltLanceMelee) },
                new NamedBuild { Name = "penitent-flail", Loadout = WithMelee(PenitentFlailMelee) },
                new NamedBuild { Name = "crowbeak-pick", Loadout = WithMelee(CrowbeakPickMelee) }
            }
        };

        public static List<MatrixSpec> Select(string roster) => roster switch
        {
            "default" => new List<MatrixSpec> { ShapesMatrix(), BodiesMatrix() },
            "shapes" => new List<MatrixSpec> { ShapesMatrix() },
            "bodies" => new List<MatrixSpec> { BodiesMatrix() },
            "cross" => new List<MatrixSpec> { CrossMatrix() },
            "volley" => new List<MatrixSpec> { VolleyMatrix() },
            "fetter" => new List<MatrixSpec> { FetterMatrix() },
            "pulls" => new List<MatrixSpec> { PullsMatrix() },
            "scaling" => new List<MatrixSpec> { ScalingMatrix() },
            _ => throw new ArgumentException($"Unknown roster '{roster}' (default|shapes|bodies|cross|volley|fetter|pulls|scaling).")
        };
    }
}
