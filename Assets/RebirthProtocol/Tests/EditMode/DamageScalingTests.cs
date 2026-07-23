using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    // Pull/scaling domain curves (ARMORY §13.1, Pass H). Pure value types,
    // so the whole balance identity of Pilgrim/Beacon/Courser Saber/Tilt
    // Lance/Crowbeak Pick is testable without a scene.
    public sealed class DamageScalingTests
    {
        [Test]
        public void FlatRangeScalingIsAlwaysOne()
        {
            var flat = RangeScaling.Flat;
            Assert.That(flat.FactorAt(0f), Is.EqualTo(1f));
            Assert.That(flat.FactorAt(50f), Is.EqualTo(1f));
        }

        [Test]
        public void RangecraftGrowsWithDistanceThenHolds()
        {
            // Pilgrim: weak near, strong far. 0.5x point-blank -> 1.5x at 30m.
            var r = RangeScaling.Rangecraft(0.5f, 1.5f, 30f);
            Assert.That(r.FactorAt(0f), Is.EqualTo(0.5f).Within(0.001f), "weakest at the muzzle");
            Assert.That(r.FactorAt(15f), Is.EqualTo(1.0f).Within(0.001f), "reference at the midpoint");
            Assert.That(r.FactorAt(30f), Is.EqualTo(1.5f).Within(0.001f), "strongest at range");
            Assert.That(r.FactorAt(90f), Is.EqualTo(1.5f).Within(0.001f), "holds past the far distance, never overshoots");
            Assert.That(r.FactorAt(7.5f), Is.GreaterThan(r.FactorAt(0f)), "monotonic climb");
        }

        [Test]
        public void BurstPointPeaksOnlyInsideItsWindow()
        {
            // Beacon: 1.6x at 18m +/-3m, 0.4x everywhere else — space it or waste it.
            var b = RangeScaling.BurstPoint(1.6f, 0.4f, 18f, 3f);
            Assert.That(b.FactorAt(18f), Is.EqualTo(1.6f), "dead-on the bloom");
            Assert.That(b.FactorAt(15.5f), Is.EqualTo(1.6f), "inside the near edge of the window");
            Assert.That(b.FactorAt(20.5f), Is.EqualTo(1.6f), "inside the far edge");
            Assert.That(b.FactorAt(5f), Is.EqualTo(0.4f), "fired too close — wasted");
            Assert.That(b.FactorAt(30f), Is.EqualTo(0.4f), "flew past the bloom — wasted");
        }

        [Test]
        public void NoneMeleeScalingIsAlwaysOne()
        {
            var none = MeleeScaling.None;
            Assert.That(none.FactorAt(0f), Is.EqualTo(1f));
            Assert.That(none.FactorAt(100f), Is.EqualTo(1f));
        }

        [Test]
        public void MeleeRampClampsBothEnds()
        {
            // Tilt Lance shape: 0.4x with no charge, 1.6x at a 12m lunge.
            var s = MeleeScaling.Ramp(MeleeScaleMode.LungeDistance, 0.4f, 1.6f, 0f, 12f);
            Assert.That(s.FactorAt(0f), Is.EqualTo(0.4f).Within(0.001f), "standing still — minimum");
            Assert.That(s.FactorAt(6f), Is.EqualTo(1.0f).Within(0.001f), "half charge — midpoint");
            Assert.That(s.FactorAt(12f), Is.EqualTo(1.6f).Within(0.001f), "full charge — maximum");
            Assert.That(s.FactorAt(30f), Is.EqualTo(1.6f).Within(0.001f), "clamps, never runs away past the high input");
            Assert.That(s.FactorAt(-5f), Is.EqualTo(0.4f).Within(0.001f), "clamps below the low input too");
        }

        [Test]
        public void SpeedAndTipModesShareTheRampButKeyOffDifferentInputs()
        {
            // Both are just ramps — the avatar decides what live value feeds
            // them (velocity for Speed, distance-to-target for Tip).
            var speed = MeleeScaling.Ramp(MeleeScaleMode.Speed, 0.5f, 1.4f, 2f, 9f);
            Assert.That(speed.FactorAt(2f), Is.EqualTo(0.5f).Within(0.001f), "barely moving");
            Assert.That(speed.FactorAt(9f), Is.EqualTo(1.4f).Within(0.001f), "at full run");

            var tip = MeleeScaling.Ramp(MeleeScaleMode.Tip, 0.3f, 1.7f, 1.5f, 3.4f);
            Assert.That(tip.FactorAt(1.5f), Is.EqualTo(0.3f).Within(0.001f), "smothered up close");
            Assert.That(tip.FactorAt(3.4f), Is.EqualTo(1.7f).Within(0.001f), "all the power in the beak's tip");
        }
    }
}
