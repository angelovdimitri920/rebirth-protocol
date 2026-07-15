using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class CombatantHealthTests
    {
        private static HealthTuning Tuning() => new HealthTuning
        {
            MaxHp = 1000f,
            MaxEndurance = 200f,
            EnduranceRegenPerSec = 35f,
            EnduranceRegenDelay = 1.8f,
            KnockdownDuration = 2.2f,
            KnockdownMashReduction = 0.12f,
            KnockdownMinDuration = 0.9f,
            RebirthDuration = 2.5f
        };

        [Test]
        public void HitDrainsBothPoolsAndStaysActive()
        {
            var health = new CombatantHealth(Tuning());

            var result = health.TakeHit(35f, 16f);

            Assert.That(result, Is.EqualTo(HitResult.Hit));
            Assert.That(health.Hp, Is.EqualTo(965f));
            Assert.That(health.Endurance, Is.EqualTo(184f));
            Assert.That(health.State, Is.EqualTo(HealthState.Active));
            Assert.That(health.CanAct, Is.True);
        }

        [Test]
        public void EnduranceEmptyingTriggersKnockdownWhileHpRemains()
        {
            var health = new CombatantHealth(Tuning());

            var result = health.TakeHit(50f, 200f);

            Assert.That(result, Is.EqualTo(HitResult.Knockdown));
            Assert.That(health.State, Is.EqualTo(HealthState.KnockedDown));
            Assert.That(health.Endurance, Is.Zero);
            Assert.That(health.Hp, Is.EqualTo(950f));
            Assert.That(health.CanAct, Is.False);
        }

        [Test]
        public void HpEmptyingKillsEvenWithEnduranceLeft()
        {
            var health = new CombatantHealth(Tuning());

            var result = health.TakeHit(1000f, 10f);

            Assert.That(result, Is.EqualTo(HitResult.Killed));
            Assert.That(health.State, Is.EqualTo(HealthState.Dead));
            Assert.That(health.Hp, Is.Zero);
        }

        [Test]
        public void HitsWhileDownedOrRebirthingAreInvulnerable()
        {
            var health = new CombatantHealth(Tuning());
            health.TakeHit(0f, 200f);
            Assert.That(health.State, Is.EqualTo(HealthState.KnockedDown));

            var hpBefore = health.Hp;
            Assert.That(health.TakeHit(500f, 100f), Is.EqualTo(HitResult.Invulnerable));
            Assert.That(health.Hp, Is.EqualTo(hpBefore));

            health.Tick(2.3f);
            Assert.That(health.State, Is.EqualTo(HealthState.Rebirth));
            Assert.That(health.TakeHit(500f, 100f), Is.EqualTo(HitResult.Invulnerable));
            Assert.That(health.Hp, Is.EqualTo(hpBefore));
        }

        [Test]
        public void StandingUpGrantsFullEnduranceThenReturnsToActive()
        {
            var health = new CombatantHealth(Tuning());
            health.TakeHit(0f, 200f);

            health.Tick(2.3f);
            Assert.That(health.State, Is.EqualTo(HealthState.Rebirth));
            Assert.That(health.Endurance, Is.EqualTo(200f));

            health.Tick(2.6f);
            Assert.That(health.State, Is.EqualTo(HealthState.Active));
        }

        [Test]
        public void RebirthIsUnlimitedAcrossRepeatedKnockdowns()
        {
            var health = new CombatantHealth(Tuning());

            for (var cycle = 0; cycle < 3; cycle++)
            {
                Assert.That(health.TakeHit(10f, 200f), Is.EqualTo(HitResult.Knockdown));
                health.Tick(2.3f);
                Assert.That(health.State, Is.EqualTo(HealthState.Rebirth));
                health.Tick(2.6f);
                Assert.That(health.State, Is.EqualTo(HealthState.Active));
            }
        }

        [Test]
        public void MashShavesKnockdownTimeButRespectsMinimumFloor()
        {
            var health = new CombatantHealth(Tuning());
            health.TakeHit(0f, 200f);

            health.Mash();
            Assert.That(health.StateTimer, Is.EqualTo(2.08f).Within(0.0001f));

            for (var i = 0; i < 100; i++)
            {
                health.Mash();
            }

            // Total downtime floor: elapsed (0 here) + remaining >= min duration.
            Assert.That(health.StateTimer, Is.EqualTo(0.9f).Within(0.0001f));
        }

        [Test]
        public void MashFloorAccountsForTimeAlreadySpentDown()
        {
            var health = new CombatantHealth(Tuning());
            health.TakeHit(0f, 200f);

            health.Tick(0.5f);
            for (var i = 0; i < 100; i++)
            {
                health.Mash();
            }

            // 0.5s already served, so remaining floors at 0.9 - 0.5 = 0.4:
            // total downtime still lands exactly on KnockdownMinDuration.
            Assert.That(health.StateTimer, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void MashOutsideKnockdownDoesNothing()
        {
            var health = new CombatantHealth(Tuning());
            health.Mash();
            Assert.That(health.State, Is.EqualTo(HealthState.Active));
            Assert.That(health.StateTimer, Is.Zero);
        }

        [Test]
        public void EnduranceRegeneratesOnlyAfterDelayAndCapsAtMax()
        {
            var health = new CombatantHealth(Tuning());
            health.TakeHit(10f, 100f);

            health.Tick(1.0f);
            Assert.That(health.Endurance, Is.EqualTo(100f), "no regen inside the delay window");

            health.Tick(1.0f);
            Assert.That(health.Endurance, Is.GreaterThan(100f), "regen after the delay elapses");

            health.Tick(60f);
            Assert.That(health.Endurance, Is.EqualTo(200f), "regen caps at max endurance");
        }

        [Test]
        public void DrainEnduranceTriggersKnockdownWhenEmptied()
        {
            var health = new CombatantHealth(Tuning());

            health.DrainEndurance(150f);
            Assert.That(health.State, Is.EqualTo(HealthState.Active));

            health.DrainEndurance(50f);
            Assert.That(health.State, Is.EqualTo(HealthState.KnockedDown));
        }

        [Test]
        public void ForceKnockdownOnlyActsFromActiveState()
        {
            var health = new CombatantHealth(Tuning());

            health.ForceKnockdown();
            Assert.That(health.State, Is.EqualTo(HealthState.KnockedDown));

            var timerBefore = health.StateTimer;
            health.ForceKnockdown();
            Assert.That(health.StateTimer, Is.EqualTo(timerBefore), "no timer reset while already down");
        }
    }
}
