using System.Collections;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    public sealed class DuelManagerPlayModeTests
    {
        [UnityTest]
        public IEnumerator DuelBootsAndSimulatesWithoutErrors()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();

            // ~2 simulated seconds: AI orbits, fires, possibly melees.
            for (var i = 0; i < 120; i++)
            {
                yield return null;
            }

            Assert.That(duel.Player, Is.Not.Null);
            Assert.That(duel.Enemy, Is.Not.Null);
            Assert.That(duel.Player.Health.State, Is.Not.EqualTo(HealthState.Dead),
                "player cannot die to chip damage in two seconds");
            Assert.That(duel.IsOver, Is.False);

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KillingTheEnemyEndsTheDuelInVictory()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            yield return null;

            duel.Enemy.ReceiveHit(duel.Enemy.Health.MaxHp + 1f, 0f, Vector3.forward);
            yield return null;
            yield return null;

            Assert.That(duel.Enemy.Health.State, Is.EqualTo(HealthState.Dead));
            Assert.That(duel.IsOver, Is.True);
            Assert.That(duel.PlayerWon, Is.True);

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnduranceKnockdownRecoversThroughRebirth()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            yield return null;

            duel.Player.ReceiveHit(10f, duel.Player.Health.MaxEndurance + 1f, Vector3.forward);
            Assert.That(duel.Player.Health.State, Is.EqualTo(HealthState.KnockedDown));

            // Knockdown (<=2.2s) then rebirth (2.5s) then active: run 5.5s.
            var elapsed = 0f;
            var sawRebirth = false;
            while (elapsed < 5.5f)
            {
                if (duel.Player.Health.State == HealthState.Rebirth)
                {
                    sawRebirth = true;
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.That(sawRebirth, Is.True, "knockdown stands up into rebirth invincibility");
            Assert.That(duel.Player.Health.State, Is.EqualTo(HealthState.Active));

            Object.Destroy(go);
            yield return null;
        }
    }
}
