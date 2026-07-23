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
        public void LateStrikeWindowIsNotSkippedByACoarseStep()
        {
            // Codex PR #23: Penitent Flail's late-strike window (the final
            // 40% of a 0.4s swing = _timer <= 0.16) must not be stepped over
            // by a coarse frame. With the hit checked before Tick decrements,
            // two 0.2s steps sample _timer at 0.4 then 0.2 — both above 0.16 —
            // so a pre-step gate would expire the swing unstruck. The gate
            // projects past the step (_timer - dt), so the second frame lands.
            var tuning = new MeleeTuning
            {
                SwingActiveTime = 0.4f,
                StrikeDelayFraction = 0.6f,
                HitRecovery = 0.1f,
                WhiffRecovery = 0.1f
            };
            var melee = new MeleeAction(tuning);
            Assert.That(melee.TryStart(1f), Is.True); // within CloseRange: swings directly
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Swing));

            var landed = false;
            // Mirror RoboAvatar.TickMelee's order: hit check, THEN Tick.
            for (var i = 0; i < 3 && melee.Phase == MeleePhase.Swing; i++)
            {
                if (melee.TryRegisterHit(0.2f))
                {
                    landed = true;
                    break;
                }

                melee.Tick(0.2f, 1f);
            }

            Assert.That(landed, Is.True, "a coarse frame must not skip the late-strike window");
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
