using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class ChargeActionTests
    {
        private static ChargeSpec SingleStrike() => new ChargeSpec
        {
            Kind = ChargeKind.Attack,
            Damage = 100f,
            EnduranceDamage = 40f,
            Speed = 20f,
            StrikeTime = 0.4f,
            WindupTime = 0.25f,
            RecoveryTime = 0.8f,
            KnockbackSpeed = 10f,
            HitRange = 2f
        };

        private static ChargeSpec Repeating(bool iframes = false) => new ChargeSpec
        {
            Kind = ChargeKind.Attack,
            Damage = 40f,
            EnduranceDamage = 15f,
            Speed = 26f,
            StrikeTime = 0.15f,
            Strikes = 3,
            WindupTime = 0.2f,
            RecoveryTime = 0.9f,
            GrantsIFrames = iframes,
            KnockbackSpeed = 5f,
            HitRange = 2f
        };

        [Test]
        public void StartWindsUpThenStrikesThenRecoversThenEnds()
        {
            var charge = new ChargeAction(SingleStrike());

            Assert.That(charge.TryStart(), Is.True);
            Assert.That(charge.Phase, Is.EqualTo(ChargePhase.Windup));

            Assert.That(charge.Tick(0.1f), Is.EqualTo(ChargeTickEvent.WindupActive));
            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.EnteredStrike));
            Assert.That(charge.Phase, Is.EqualTo(ChargePhase.Strike));

            Assert.That(charge.Tick(0.3f), Is.EqualTo(ChargeTickEvent.StrikeActive));
            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.EnteredRecovery));
            Assert.That(charge.Phase, Is.EqualTo(ChargePhase.Recovery));

            // Recovery is 0.8s: still vulnerable-and-rooted at 0.7.
            Assert.That(charge.Tick(0.7f), Is.EqualTo(ChargeTickEvent.RecoveryActive));
            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.Ended));
            Assert.That(charge.Busy, Is.False);
        }

        [Test]
        public void BusyStartIsRejected()
        {
            var charge = new ChargeAction(SingleStrike());
            charge.TryStart();
            Assert.That(charge.TryStart(), Is.False);
        }

        [Test]
        public void RepeatingChargeChainsAllStrikesBeforeRecovery()
        {
            var charge = new ChargeAction(Repeating());
            charge.TryStart();
            Assert.That(charge.Tick(0.25f), Is.EqualTo(ChargeTickEvent.EnteredStrike));
            Assert.That(charge.StrikeIndex, Is.EqualTo(0));

            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.EnteredStrike));
            Assert.That(charge.StrikeIndex, Is.EqualTo(1));
            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.EnteredStrike));
            Assert.That(charge.StrikeIndex, Is.EqualTo(2));

            // Third strike is the last: it expires into recovery, not a 4th.
            Assert.That(charge.Tick(0.2f), Is.EqualTo(ChargeTickEvent.EnteredRecovery));
        }

        [Test]
        public void EachStrikeRegistersAtMostOneHit()
        {
            var charge = new ChargeAction(Repeating());
            charge.TryStart();
            charge.Tick(0.25f); // -> strike 0

            Assert.That(charge.TryRegisterHit(), Is.True);
            Assert.That(charge.TryRegisterHit(), Is.False, "one hit per strike");

            charge.Tick(0.2f); // -> strike 1: fresh hit budget
            Assert.That(charge.TryRegisterHit(), Is.True);
        }

        [Test]
        public void HitRegistrationOnlyDuringStrike()
        {
            var charge = new ChargeAction(SingleStrike());
            Assert.That(charge.TryRegisterHit(), Is.False, "idle");
            charge.TryStart();
            Assert.That(charge.TryRegisterHit(), Is.False, "windup");
            charge.Tick(0.3f); // -> strike
            Assert.That(charge.TryRegisterHit(), Is.True);
            charge.Tick(0.5f); // -> recovery
            Assert.That(charge.TryRegisterHit(), Is.False, "recovery");
        }

        [Test]
        public void IFramesCoverOnlyTheStrikeWindow()
        {
            var charge = new ChargeAction(SingleStrike());
            Assert.That(charge.IFramesActive, Is.False, "idle");
            charge.TryStart();
            Assert.That(charge.IFramesActive, Is.False, "windup is the vulnerable-before");
            charge.Tick(0.3f); // -> strike
            Assert.That(charge.IFramesActive, Is.True);
            charge.Tick(0.5f); // -> recovery
            Assert.That(charge.IFramesActive, Is.False, "recovery is the vulnerable-after");
        }

        [Test]
        public void NoGuardChargeNeverGetsIFrames()
        {
            var charge = new ChargeAction(Repeating(iframes: false));
            charge.TryStart();
            charge.Tick(0.25f); // -> strike
            Assert.That(charge.Phase, Is.EqualTo(ChargePhase.Strike));
            Assert.That(charge.IFramesActive, Is.False, "the source's \"no guard\"");
        }

        [Test]
        public void CancelReturnsToIdleFromAnyPhase()
        {
            var charge = new ChargeAction(SingleStrike());
            charge.TryStart();
            charge.Tick(0.3f); // -> strike
            charge.Cancel();
            Assert.That(charge.Busy, Is.False);
            Assert.That(charge.Tick(0.1f), Is.EqualTo(ChargeTickEvent.None));
        }
    }
}
