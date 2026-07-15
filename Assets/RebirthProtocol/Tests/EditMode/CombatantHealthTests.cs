using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    public sealed class CombatantHealthTests
    {
        [Test]
        public void DamageCanStaggerWithoutKnockingDown()
        {
            var health = new CombatantHealth(100f, 30f, 1);

            health.ApplyDamage(new DamageEvent(10f, 30f));

            Assert.That(health.HitPoints, Is.EqualTo(90f));
            Assert.That(health.State, Is.EqualTo(CombatLifeState.Staggered));
            Assert.That(health.CanAct, Is.True);
        }

        [Test]
        public void KnockdownCanSpendRebirthCharge()
        {
            var health = new CombatantHealth(100f, 30f, 1);

            health.ApplyDamage(new DamageEvent(125f, 0f));

            Assert.That(health.State, Is.EqualTo(CombatLifeState.KnockedDown));
            Assert.That(health.TryStartRebirth(), Is.True);
            health.CompleteRebirth();

            Assert.That(health.State, Is.EqualTo(CombatLifeState.Alive));
            Assert.That(health.HitPoints, Is.EqualTo(50f));
            Assert.That(health.RebirthCharges, Is.Zero);
        }
    }
}
