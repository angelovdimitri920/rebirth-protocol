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
                Body = PartsCatalog.Bodies[3], // Cobalt Knight: 1.45 hp, 0.8 speed, 1 dash
                Gun = PartsCatalog.Guns[0],
                Bomb = PartsCatalog.Bombs[0],
                Legs = PartsCatalog.Legs[1], // Courser Greaves: 1.3 speed, 0.85 jump, 1.1 recovery
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
                Body = PartsCatalog.Bodies[2], // Duskmantle: 2 vanish dashes (bodies cap at 2, ARMORY_REFERENCE §2.2)
                Melee = PartsCatalog.MeleeWeapons[2],
                Shield = PartsCatalog.Shields[0],
                Legs = PartsCatalog.Legs[2], // Gryphon Greaves: +1 dash
                Pod = PartsCatalog.Pods[1]
            };

            var stats = PartsCatalog.ComputeStats(loadout);

            Assert.That(stats.DashCount, Is.EqualTo(3));
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

        [Test]
        public void EveryBodyCarriesItsArmoryChargeRow()
        {
            // ARMORY_REFERENCE §3, Field garnitures: Bannerman straight,
            // Vesper slow gliding, Duskmantle repeating short (no guard),
            // Cobalt Knight diagonal rising. And the doctrine's shape holds
            // for all of them: vulnerability before AND after (§4.5).
            foreach (var body in PartsCatalog.Bodies)
            {
                var c = body.Charge;
                Assert.That(c, Is.Not.Null, $"{body.Name} has no charge");
                Assert.That(c.Damage, Is.GreaterThan(0f));
                Assert.That(c.Speed, Is.GreaterThan(0f));
                Assert.That(c.StrikeTime, Is.GreaterThan(0f));
                Assert.That(c.WindupTime, Is.GreaterThan(0f), $"{body.Name}: no vulnerable-before");
                Assert.That(c.RecoveryTime, Is.GreaterThan(0f), $"{body.Name}: no vulnerable-after");
            }

            var vanguard = PartsCatalog.Bodies[0].Charge;
            Assert.That(vanguard.Kind, Is.EqualTo(ChargeKind.Attack));
            Assert.That(vanguard.Strikes, Is.EqualTo(1));
            Assert.That(vanguard.GrantsIFrames, Is.True);

            var skylance = PartsCatalog.Bodies[1].Charge;
            Assert.That(skylance.Kind, Is.EqualTo(ChargeKind.Attack));
            Assert.That(skylance.Speed, Is.LessThan(vanguard.Speed), "Vesper glides slow");
            Assert.That(skylance.Damage, Is.GreaterThan(vanguard.Damage), "and hits hardest");
            Assert.That(skylance.RecoveryTime, Is.GreaterThan(vanguard.RecoveryTime), "and whiffs worst");

            var wraith = PartsCatalog.Bodies[2].Charge;
            Assert.That(wraith.Strikes, Is.EqualTo(3), "repeating short charges");
            Assert.That(wraith.GrantsIFrames, Is.False, "the source's \"no guard\"");

            var bulwark = PartsCatalog.Bodies[3].Charge;
            Assert.That(bulwark.Kind, Is.EqualTo(ChargeKind.Air), "rising strike contests the sky");
            Assert.That(bulwark.RiseSpeed, Is.GreaterThan(0f));
        }

        [Test]
        public void NoBuiltGunIsExemptFromTheOverloadRule()
        {
            // The SurvivesKnockdown exemption is reserved for the scrapwright
            // line (DOCTRINE §4.3 — Matchlock, Pass P). Until then every gun's
            // in-flight rounds must be wiped by its wielder's knockdown.
            foreach (var gun in PartsCatalog.Guns)
            {
                Assert.That(gun.SurvivesKnockdown, Is.False,
                    $"{gun.Name} must not claim the scrapwright exemption");
            }
        }

        [Test]
        public void EveryMeleeWeaponsLungeStopsWithinItsOwnHitRange()
        {
            // Codex PR #21 finding: MeleeTuning's shared default
            // LungeReachDistance (2.6) predates every weapon's own
            // HitRange and can exceed a shorter blade's real reach
            // (Tocsin Mace: HitRange 2.4) -- the gap-closer would decide
            // "close enough to stop" at a distance the swing's own hit-
            // range check then rejected, a repeatable whiff. This is the
            // general invariant MeleeWeaponPart.ToTuning() must uphold for
            // every current AND future weapon, not just a Tocsin-specific
            // regression check.
            foreach (var melee in PartsCatalog.MeleeWeapons)
            {
                var tuning = melee.ToTuning();
                Assert.That(tuning.LungeReachDistance, Is.LessThan(melee.HitRange),
                    $"{melee.Name}: the lunge must stop strictly inside its own HitRange, with room to spare");
            }
        }
    }
}
