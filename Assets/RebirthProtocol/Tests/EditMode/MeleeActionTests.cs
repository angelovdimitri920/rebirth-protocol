using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class MeleeActionTests
    {
        [Test]
        public void CloseStartSkipsLungeAndSwingsDirectly()
        {
            var melee = new MeleeAction();

            Assert.That(melee.TryStart(3f), Is.True);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Swing));
        }

        [Test]
        public void FarStartLungesThenSwingsOnReach()
        {
            var melee = new MeleeAction();

            Assert.That(melee.TryStart(10f), Is.True);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Lunge));

            Assert.That(melee.Tick(0.1f, 8f), Is.EqualTo(MeleeTickEvent.LungeActive));
            Assert.That(melee.Tick(0.1f, 2.5f), Is.EqualTo(MeleeTickEvent.EnteredSwing));
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Swing));
        }

        [Test]
        public void OutOfRangeStartIsRejected()
        {
            var melee = new MeleeAction();
            Assert.That(melee.TryStart(16f), Is.False);
            Assert.That(melee.Busy, Is.False);
        }

        [Test]
        public void LungeTimeoutWhiffsIntoFullRecovery()
        {
            var melee = new MeleeAction();
            melee.TryStart(14f);

            Assert.That(melee.Tick(0.7f, 14f), Is.EqualTo(MeleeTickEvent.EnteredRecovery));
            // Whiff recovery is the long one (0.95s): still recovering at 0.9.
            Assert.That(melee.Tick(0.9f, 14f), Is.EqualTo(MeleeTickEvent.RecoveryActive));
            Assert.That(melee.Tick(0.1f, 14f), Is.EqualTo(MeleeTickEvent.Ended));
            Assert.That(melee.Busy, Is.False);
        }

        [Test]
        public void ConnectedSwingUsesShortRecovery()
        {
            var melee = new MeleeAction();
            melee.TryStart(2f);
            Assert.That(melee.TryRegisterHit(), Is.True);
            Assert.That(melee.TryRegisterHit(), Is.False, "one hit per swing");

            Assert.That(melee.Tick(0.2f, 2f), Is.EqualTo(MeleeTickEvent.EnteredRecovery));
            // Hit recovery is 0.45s: already over at 0.5.
            Assert.That(melee.Tick(0.5f, 2f), Is.EqualTo(MeleeTickEvent.Ended));
        }

        [Test]
        public void ChainOnlyAllowedAfterConnectedSwingUpToFinisher()
        {
            var melee = new MeleeAction();

            // Whiffed swing: no chain.
            melee.TryStart(2f);
            melee.Tick(0.2f, 2f);
            Assert.That(melee.TryChain(2f), Is.False);
            melee.Tick(1f, 2f); // recovery ends

            // Connected string chains twice, then caps.
            melee.TryStart(2f);
            Assert.That(melee.ComboDamageMult, Is.EqualTo(1f));
            melee.TryRegisterHit();
            melee.Tick(0.2f, 2f);

            Assert.That(melee.TryChain(2f), Is.True);
            Assert.That(melee.ComboDamageMult, Is.EqualTo(0.85f));
            melee.TryRegisterHit();
            melee.Tick(0.2f, 2f);

            Assert.That(melee.TryChain(2f), Is.True);
            Assert.That(melee.ComboDamageMult, Is.EqualTo(1.4f), "finisher multiplier");
            melee.TryRegisterHit();
            melee.Tick(0.2f, 2f);

            Assert.That(melee.TryChain(2f), Is.False, "string capped at three hits");
        }

        [Test]
        public void CancelResetsToIdle()
        {
            var melee = new MeleeAction();
            melee.TryStart(10f);
            melee.Cancel();
            Assert.That(melee.Busy, Is.False);
        }
    }

    public sealed class GunCycleTests
    {
        [Test]
        public void FireIsGatedByInterval()
        {
            var gun = new GunCycle();

            Assert.That(gun.TryFire(), Is.True);
            Assert.That(gun.TryFire(), Is.False, "cooldown active");

            gun.Tick(0.38f);
            Assert.That(gun.TryFire(), Is.True);
        }
    }
}
