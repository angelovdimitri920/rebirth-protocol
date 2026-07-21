using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class FetterStateTests
    {
        [Test]
        public void ApplyFromFreeTakesHold()
        {
            var fetter = new FetterState();
            Assert.That(fetter.TryApply(1f), Is.True);
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Fettered));
            Assert.That(fetter.IsFettered, Is.True);
        }

        [Test]
        public void FetteredEventFiresOnlyOnSuccessfulApply()
        {
            var fetter = new FetterState();
            var fired = 0;
            fetter.Fettered += () => fired++;

            Assert.That(fetter.TryApply(1f), Is.True);
            Assert.That(fired, Is.EqualTo(1));

            Assert.That(fetter.TryApply(1f), Is.False, "already fettered");
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void ZeroOrNegativeDurationIsRejected()
        {
            var fetter = new FetterState();
            Assert.That(fetter.TryApply(0f), Is.False);
            Assert.That(fetter.TryApply(-1f), Is.False);
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
        }

        [Test]
        public void ReapplyMidFetterIsIgnoredNotRefreshed()
        {
            var fetter = new FetterState();
            fetter.TryApply(1f);
            fetter.Tick(0.9f);
            Assert.That(fetter.TryApply(5f), Is.False, "no chaining -- would defeat the immunity rule");
            fetter.Tick(0.2f); // total 1.1s: the ORIGINAL 1s duration has elapsed
            Assert.That(fetter.IsFettered, Is.False);
        }

        [Test]
        public void FetterEndsIntoImmunityWindow()
        {
            var fetter = new FetterState();
            fetter.TryApply(1f);
            fetter.Tick(1f);
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Immune));
            Assert.That(fetter.IsFettered, Is.False);
            Assert.That(fetter.IsImmune, Is.True);
        }

        [Test]
        public void ApplyDuringImmunityWindowIsRejected()
        {
            var fetter = new FetterState();
            fetter.TryApply(1f);
            fetter.Tick(1f); // -> immune
            Assert.That(fetter.TryApply(1f), Is.False, "the pillar-9 fetter-immunity rule");
        }

        [Test]
        public void ImmunityWindowIsDoctrinesTwoSeconds()
        {
            var fetter = new FetterState();
            fetter.TryApply(0.5f);
            fetter.Tick(0.5f); // -> immune, 2s remaining
            fetter.Tick(1.9f);
            Assert.That(fetter.IsImmune, Is.True, "still inside the 2s window");
            fetter.Tick(0.2f); // total 2.1s of immunity
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
        }

        [Test]
        public void FreeAfterImmunityAcceptsANewFetter()
        {
            var fetter = new FetterState();
            fetter.TryApply(0.5f);
            // Each Tick call resolves at most one phase transition (the
            // codebase's established convention -- CombatantHealth.Tick
            // does the same): a tick that overshoots a phase boundary does
            // NOT carry the remainder into the next phase's countdown, so
            // fettered -> immune -> free needs two ticks, not one giant one.
            fetter.Tick(0.5f); // -> immune (2s window starts fresh)
            fetter.Tick(2.01f); // -> free
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
            Assert.That(fetter.TryApply(1f), Is.True);
        }

        [Test]
        public void CancelReturnsToFreeWithNoImmunityGranted()
        {
            var fetter = new FetterState();
            fetter.TryApply(1f);
            fetter.Cancel();
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
            Assert.That(fetter.TryApply(1f), Is.True, "cancel must not leave a stray immunity window");
        }

        [Test]
        public void TickOnFreeIsANoOp()
        {
            var fetter = new FetterState();
            fetter.Tick(10f);
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
        }

        [Test]
        public void CustomImmunityTuningIsRespected()
        {
            var fetter = new FetterState(new FetterTuning { ImmunitySeconds = 0.5f });
            fetter.TryApply(1f);
            fetter.Tick(1f); // -> immune
            fetter.Tick(0.4f);
            Assert.That(fetter.IsImmune, Is.True);
            fetter.Tick(0.2f);
            Assert.That(fetter.Phase, Is.EqualTo(FetterPhase.Free));
        }
    }
}
