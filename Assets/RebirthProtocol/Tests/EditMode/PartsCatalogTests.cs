using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class PartsCatalogTests
    {
        [Test]
        public void ComputeStatsMultipliesBodyAndLegsIntoDerivedNumbers()
        {
            var loadout = new Loadout
            {
                Body = PartsCatalog.Bodies[3], // Crusader Knight: 1.45 hp, 0.8 speed, 1 dash
                Gun = PartsCatalog.Guns[0],
                Bomb = PartsCatalog.Bombs[0],
                Legs = PartsCatalog.Legs[1], // Numidian Boots: 1.3 speed, 0.85 jump, 1.1 recovery
                Pod = PartsCatalog.Pods[0]
            };

            var stats = PartsCatalog.ComputeStats(loadout);

            Assert.That(stats.MaxHp, Is.EqualTo(1450f));
            Assert.That(stats.DefMult, Is.EqualTo(0.75f));
            Assert.That(stats.RunSpeed, Is.EqualTo(9f * 0.8f * 1.3f).Within(0.0001f));
            Assert.That(stats.JumpThrust, Is.EqualTo(13f * 0.85f).Within(0.0001f));
            Assert.That(stats.DashCount, Is.EqualTo(1));
            Assert.That(stats.LandRecoveryMult, Is.EqualTo(1.1f));
        }

        [Test]
        public void LegsExtraDashesStackOnBodyDashCount()
        {
            var loadout = new Loadout
            {
                Body = PartsCatalog.Bodies[2], // Shinobi: 3 vanish dashes
                Melee = PartsCatalog.MeleeWeapons[2],
                Shield = PartsCatalog.Shields[0],
                Legs = PartsCatalog.Legs[2], // Winged Sandals: +1 dash
                Pod = PartsCatalog.Pods[1]
            };

            var stats = PartsCatalog.ComputeStats(loadout);

            Assert.That(stats.DashCount, Is.EqualTo(4));
            Assert.That(stats.DashType, Is.EqualTo(DashType.Vanish));
            Assert.That(loadout.HasMelee, Is.True);
            Assert.That(loadout.HasShield, Is.True);
            Assert.That(loadout.HasGun, Is.False);
            Assert.That(loadout.HasBomb, Is.False);
        }

        [Test]
        public void DashProfilesMatchTheArchetypeSpread()
        {
            var normal = DashProfile.For(DashType.Normal);
            var vanish = DashProfile.For(DashType.Vanish);
            var longDash = DashProfile.For(DashType.Long);

            Assert.That(normal.Cost, Is.EqualTo(28f));
            Assert.That(vanish.Duration, Is.LessThan(normal.Duration), "vanish is the shortest dash");
            Assert.That(longDash.Duration, Is.GreaterThan(normal.Duration), "long dash commits hardest");
        }
    }
}
