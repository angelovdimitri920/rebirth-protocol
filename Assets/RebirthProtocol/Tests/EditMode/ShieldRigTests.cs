using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class ShieldRigTests
    {
        private static ShieldPart TestShield(float toll = 2f, float muffle = 0f) => new ShieldPart
        {
            Id = "test-shield",
            Name = "Test Shield",
            ShieldHp = 100f,
            RegenPerSec = 10f,
            RegenDelay = 1f,
            FrontBlockPercent = 0.8f,
            BackBlockPercent = 0.3f,
            MeleeParryEnduranceDamage = 20f,
            TollSeconds = toll,
            GroundRaise = ShieldGroundRaise.Root,
            AirRaise = ShieldAirRaise.None,
            BlastMuffleSeconds = muffle
        };

        [Test]
        public void RaisesOnHeldIntentAndLoweringStartsTheToll()
        {
            var rig = new ShieldRig(TestShield(toll: 2f));

            rig.Tick(0.016f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.True);
            Assert.That(rig.TollRemaining, Is.Zero, "no toll while the shield stays up");

            rig.Tick(0.016f, wantRaised: false, canAct: true);
            Assert.That(rig.Raised, Is.False);
            Assert.That(rig.TollRemaining, Is.EqualTo(2f), "lowering starts the toll");

            // Held input during the toll must NOT raise (and must not root
            // the holder -- the avatar only roots on Raised).
            rig.Tick(1f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.False, "still tolling");

            // Holding through the toll re-raises the moment it expires.
            rig.Tick(1.1f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.True);
        }

        [Test]
        public void GuardBreakForcesTheShieldDownAndStartsTheToll()
        {
            var rig = new ShieldRig(TestShield(toll: 3f));
            rig.Tick(0.016f, wantRaised: true, canAct: true);

            rig.NotifyBlockedHit();
            var broke = rig.Drain(150f);

            Assert.That(broke, Is.True);
            Assert.That(rig.Hp, Is.Zero);
            Assert.That(rig.Raised, Is.False);
            Assert.That(rig.TollRemaining, Is.EqualTo(3f), "a break tolls like a lowering");
        }

        [Test]
        public void PartialDrainDoesNotBreakOrLower()
        {
            var rig = new ShieldRig(TestShield());
            rig.Tick(0.016f, wantRaised: true, canAct: true);

            rig.NotifyBlockedHit();
            var broke = rig.Drain(40f);

            Assert.That(broke, Is.False);
            Assert.That(rig.Hp, Is.EqualTo(60f));
            Assert.That(rig.Raised, Is.True);
            Assert.That(rig.TollRemaining, Is.Zero);
        }

        [Test]
        public void EmptyShieldCannotBeRaisedUntilItMendsAboveZero()
        {
            var rig = new ShieldRig(TestShield(toll: 0.5f));
            rig.Tick(0.016f, wantRaised: true, canAct: true);
            rig.NotifyBlockedHit();
            rig.Drain(150f);

            // Toll expires quickly, but the pool is empty AND the mend delay
            // hasn't passed: still not ready.
            rig.Tick(0.9f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.False, "empty pool cannot be raised");

            // After the mend delay, hp ticks back above zero and the shield
            // can come up again.
            rig.Tick(0.5f, wantRaised: true, canAct: true);
            Assert.That(rig.Hp, Is.GreaterThan(0f), "mend has resumed");
            rig.Tick(0.016f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.True);
        }

        [Test]
        public void MendPausesWhileRecentlyStruckAndStopsWhileDowned()
        {
            var rig = new ShieldRig(TestShield());
            rig.NotifyBlockedHit();
            rig.Drain(50f);

            rig.Tick(0.5f, wantRaised: false, canAct: true);
            Assert.That(rig.Hp, Is.EqualTo(50f), "inside the mend delay: no regen");

            rig.Tick(1f, wantRaised: false, canAct: false);
            Assert.That(rig.Hp, Is.EqualTo(50f), "downed: no regen even past the delay");

            rig.Tick(1f, wantRaised: false, canAct: true);
            Assert.That(rig.Hp, Is.EqualTo(60f).Within(0.001f), "past the delay and active: 10/s mend");
        }

        [Test]
        public void KnockdownWhileRaisedLowersAndStartsTheToll()
        {
            var rig = new ShieldRig(TestShield(toll: 2f));
            rig.Tick(0.016f, wantRaised: true, canAct: true);
            Assert.That(rig.Raised, Is.True);

            rig.Tick(0.016f, wantRaised: true, canAct: false);

            Assert.That(rig.Raised, Is.False);
            Assert.That(rig.TollRemaining, Is.EqualTo(2f));
        }

        [Test]
        public void MuffleWindowMeetsBlastsWithFullFrontGuardFromAllSides()
        {
            var rig = new ShieldRig(TestShield(muffle: 1.5f));
            rig.Tick(0.016f, wantRaised: true, canAct: true);

            Assert.That(rig.MuffleRemaining, Is.GreaterThan(0f));
            Assert.That(rig.BlockPercent(isFront: false, isBlast: true), Is.EqualTo(0.8f),
                "muffled blast from behind blocks at the front percent");
            Assert.That(rig.BlockPercent(isFront: false, isBlast: false), Is.EqualTo(0.3f),
                "direct fire from behind is NOT muffled");

            // Window expires while raised; blasts fall back to facing rules.
            rig.Tick(1.6f, wantRaised: true, canAct: true);
            Assert.That(rig.MuffleRemaining, Is.Zero);
            Assert.That(rig.BlockPercent(isFront: false, isBlast: true), Is.EqualTo(0.3f));
        }

        [Test]
        public void MuffleWindowReopensOnEachRaise()
        {
            var rig = new ShieldRig(TestShield(toll: 0.2f, muffle: 1.5f));
            rig.Tick(0.016f, wantRaised: true, canAct: true);
            rig.Tick(1.6f, wantRaised: true, canAct: true);
            Assert.That(rig.MuffleRemaining, Is.Zero);

            rig.Tick(0.016f, wantRaised: false, canAct: true); // lower: toll starts
            rig.Tick(0.3f, wantRaised: true, canAct: true); // toll expired: re-raise

            Assert.That(rig.Raised, Is.True);
            Assert.That(rig.MuffleRemaining, Is.EqualTo(1.5f).Within(0.001f));
        }

        // --- Catalog rows (ARMORY_REFERENCE §7) ---

        [Test]
        public void CatalogCarriesTheFiveShieldsWithTheirTollsAndRaises()
        {
            var shields = PartsCatalog.Shields;
            Assert.That(shields.Length, Is.EqualTo(5));

            var wardVeil = shields[0];
            Assert.That(wardVeil.Id, Is.EqualTo("aegis")); // frozen pre-canon id
            Assert.That(wardVeil.TollSeconds, Is.EqualTo(2.5f));
            Assert.That(wardVeil.AirRaise, Is.EqualTo(ShieldAirRaise.Hold));

            var pavise = shields[1];
            Assert.That(pavise.Id, Is.EqualTo("bastion")); // frozen pre-canon id
            Assert.That(pavise.TollSeconds, Is.EqualTo(6f));
            Assert.That(pavise.GroundRaise, Is.EqualTo(ShieldGroundRaise.Root));
            Assert.That(pavise.AirRaise, Is.EqualTo(ShieldAirRaise.Drop));

            var targe = shields[2];
            Assert.That(targe.Id, Is.EqualTo("targe"));
            Assert.That(targe.ShieldHp, Is.EqualTo(110f));
            Assert.That(targe.RegenPerSec, Is.EqualTo(30f));
            Assert.That(targe.FrontBlockPercent, Is.EqualTo(0.60f));
            Assert.That(targe.BackBlockPercent, Is.EqualTo(0.15f));
            Assert.That(targe.MeleeParryEnduranceDamage, Is.EqualTo(16f));
            Assert.That(targe.TollSeconds, Is.EqualTo(1.5f));
            Assert.That(targe.GroundRaise, Is.EqualTo(ShieldGroundRaise.March));
            Assert.That(targe.MarchSpeedMult, Is.EqualTo(0.4f));

            var kiteWard = shields[3];
            Assert.That(kiteWard.Id, Is.EqualTo("kite-ward"));
            Assert.That(kiteWard.ShieldHp, Is.EqualTo(200f));
            Assert.That(kiteWard.RegenPerSec, Is.EqualTo(14f));
            Assert.That(kiteWard.FrontBlockPercent, Is.EqualTo(0.80f));
            Assert.That(kiteWard.BackBlockPercent, Is.EqualTo(0.30f));
            Assert.That(kiteWard.MeleeParryEnduranceDamage, Is.EqualTo(24f));
            Assert.That(kiteWard.TollSeconds, Is.EqualTo(3.5f));
            Assert.That(kiteWard.GroundRaise, Is.EqualTo(ShieldGroundRaise.Root));

            var quietBell = shields[4];
            Assert.That(quietBell.Id, Is.EqualTo("quiet-bell"));
            Assert.That(quietBell.ShieldHp, Is.EqualTo(150f));
            Assert.That(quietBell.RegenPerSec, Is.EqualTo(16f));
            Assert.That(quietBell.FrontBlockPercent, Is.EqualTo(0.65f));
            Assert.That(quietBell.BackBlockPercent, Is.EqualTo(0.35f));
            Assert.That(quietBell.MeleeParryEnduranceDamage, Is.EqualTo(18f));
            Assert.That(quietBell.TollSeconds, Is.EqualTo(4f));
            Assert.That(quietBell.BlastMuffleSeconds, Is.GreaterThan(0f), "the Quiet Bell's dome is its special");

            foreach (var shield in shields)
            {
                Assert.That(shield.TollSeconds, Is.GreaterThan(0f), $"{shield.Name}: every shield pays a toll");
                if (shield.GroundRaise == ShieldGroundRaise.March)
                {
                    Assert.That(shield.MarchSpeedMult, Is.GreaterThan(0f).And.LessThan(1f),
                        $"{shield.Name}: March needs a reduced-but-positive walk speed");
                }
            }
        }

        [Test]
        public void TargeIsTheOnlyMarchShield()
        {
            var marchers = 0;
            foreach (var shield in PartsCatalog.Shields)
            {
                if (shield.GroundRaise == ShieldGroundRaise.March)
                {
                    marchers++;
                    Assert.That(shield.Id, Is.EqualTo("targe"));
                }
            }

            Assert.That(marchers, Is.EqualTo(1));
        }
    }
}
