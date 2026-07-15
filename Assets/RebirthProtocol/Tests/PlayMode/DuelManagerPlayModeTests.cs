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
        public IEnumerator SimultaneousMeleeInRangeClashesInsteadOfTrading()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            duel.BrainsEnabled = false; // deterministic: nobody moves or fires
            try
            {
                yield return null;

                // Both in melee hit range: without clash resolution these
                // swings would trade damage in player-first tick order.
                Teleport(duel.Enemy, duel.Player.Position + new Vector3(2f, 0f, 0f));

                // Start both swings on the domain state machines in the same
                // C# turn — no frames pass, so this is order-proof: the next
                // Update must resolve the clash BEFORE either melee ticks.
                Assert.That(duel.Player.Melee.TryStart(2f), Is.True);
                Assert.That(duel.Enemy.Melee.TryStart(2f), Is.True);
                Assert.That(duel.Player.Melee.Attacking, Is.True);
                Assert.That(duel.Enemy.Melee.Attacking, Is.True);

                yield return null;

                Assert.That(duel.Player.Melee.Busy, Is.False, "player attack canceled by clash");
                Assert.That(duel.Enemy.Melee.Busy, Is.False, "enemy attack canceled by clash");
                Assert.That(duel.Player.Health.Hp, Is.EqualTo(duel.Player.Health.MaxHp), "no damage traded");
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(duel.Enemy.Health.MaxHp), "no damage traded");
            }
            finally
            {
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HitchFrameSwingStillLandsItsHit()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            duel.BrainsEnabled = false;
            try
            {
                yield return null;
                yield return null;

                Teleport(duel.Enemy, duel.Player.Position + new Vector3(2f, 0f, 0f));

                // Force frames longer than SwingActiveTime (0.18s): the swing
                // must land before its timer expires into recovery.
                Time.captureDeltaTime = 0.2f;
                yield return null;

                // Start on the domain state machine directly (avatar control
                // locks like phantom landing recovery are out of scope here).
                var hpBefore = duel.Enemy.Health.Hp;
                Assert.That(duel.Player.Melee.TryStart(2f), Is.True);
                Assert.That(duel.Player.Melee.Phase, Is.EqualTo(Domain.MeleePhase.Swing));

                yield return null;
                yield return null;

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "swing connects even when one frame consumes the whole active window");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        private static void Teleport(RoboAvatar avatar, Vector3 position)
        {
            var cc = avatar.GetComponent<CharacterController>();
            cc.enabled = false;
            avatar.transform.position = position;
            cc.enabled = true;
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
