using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class BoostGaugeTests
    {
        [Test]
        public void ThrustDrainsGaugeAndFullDrainOverheats()
        {
            var boost = new BoostGauge();

            boost.SpendThrust(1f); // 45/s
            Assert.That(boost.Value, Is.EqualTo(55f));
            Assert.That(boost.Overheated, Is.False);

            boost.SpendThrust(2f);
            Assert.That(boost.Value, Is.Zero);
            Assert.That(boost.Overheated, Is.True);
            Assert.That(boost.CanBoost, Is.False);
        }

        [Test]
        public void AirDashSpendsCostAndIsLimitedByDashCount()
        {
            var boost = new BoostGauge();

            Assert.That(boost.TrySpendAirDash(), Is.True);
            Assert.That(boost.Value, Is.EqualTo(72f));
            Assert.That(boost.TrySpendAirDash(), Is.True);
            Assert.That(boost.TrySpendAirDash(), Is.False, "third dash blocked by MaxAirDashes");
        }

        [Test]
        public void AirDashBlockedWhenGaugeBelowCost()
        {
            var boost = new BoostGauge();
            boost.SpendThrust(1.7f); // 76.5 spent, 23.5 left < 28 cost

            Assert.That(boost.TrySpendAirDash(), Is.False);
            Assert.That(boost.AirDashesUsed, Is.Zero);
        }

        [Test]
        public void LandingRefillsAndScalesRecoveryWithSpend()
        {
            var boost = new BoostGauge();
            boost.NotifyLanded();
            var freshRecovery = boost.LandingRecovery;
            Assert.That(freshRecovery, Is.EqualTo(0.1f).Within(0.0001f), "base recovery at zero spend");

            var spent = new BoostGauge();
            spent.SpendThrust(1f); // 45% spent
            spent.NotifyLanded();
            Assert.That(spent.LandingRecovery, Is.EqualTo(0.1f + 0.55f * 0.45f).Within(0.0001f));
            Assert.That(spent.Value, Is.EqualTo(100f), "gauge refilled on landing");
            Assert.That(spent.AirDashesUsed, Is.Zero);
        }

        [Test]
        public void OverheatAddsExtraRecoveryAndClearsOnLanding()
        {
            var boost = new BoostGauge();
            boost.SpendThrust(3f); // fully drained -> overheat

            boost.NotifyLanded();
            Assert.That(boost.LandingRecovery, Is.EqualTo(0.1f + 0.55f + 0.5f).Within(0.0001f));
            Assert.That(boost.Overheated, Is.False);

            boost.Tick(2f);
            Assert.That(boost.CanBoost, Is.True, "recovery elapses and boost is usable again");
        }
    }
}
