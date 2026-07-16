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
            duel.CloseHangar();

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
            duel.CloseHangar();
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
            duel.CloseHangar();
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
            duel.CloseHangar();
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

        [UnityTest]
        public IEnumerator ShieldBlocksFrontHitsAndGuardBreaksIntoKnockdown()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.BrainsEnabled = false;
            try
            {
                var shieldLoadout = new Loadout
                {
                    Body = PartsCatalog.Bodies[0], // defMult 1.0 keeps numbers plain
                    Gun = PartsCatalog.Guns[0],
                    Shield = PartsCatalog.Shields[0], // Aegis: 180 hp, 75% front block
                    Legs = PartsCatalog.Legs[0],
                    Pod = PartsCatalog.Pods[0]
                };
                duel.RespawnWithLoadouts(shieldLoadout, PartsCatalog.DefaultLoadout());
                yield return null;

                var player = duel.Player;
                player.Intent = new RoboIntent { ShieldHeld = true, LeftArmActive = true };

                // Player spawns facing +x toward the enemy; an attack
                // traveling -x hits the front arc.
                var result = player.ReceiveHit(100f, 40f, new Vector3(-1f, 0f, 0f));

                Assert.That(result, Is.EqualTo(ReceiveResult.Shielded));
                Assert.That(player.Health.Hp, Is.EqualTo(player.Health.MaxHp - 25f), "25% chips through");
                Assert.That(player.Health.Endurance, Is.EqualTo(player.Health.MaxEndurance - 10f));
                Assert.That(player.ShieldHp, Is.EqualTo(105f), "blocked 75 drains the shield pool");

                // Blocked portion exceeding remaining shield hp: guard break
                // feeds the SAME knockdown state — no second defense layer.
                result = player.ReceiveHit(200f, 0f, new Vector3(-1f, 0f, 0f));

                Assert.That(result, Is.EqualTo(ReceiveResult.GuardBreak));
                Assert.That(player.ShieldHp, Is.Zero);
                Assert.That(player.Health.State, Is.EqualTo(HealthState.KnockedDown));
            }
            finally
            {
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LavaPoolDamagesGroundedRoboButNotOneOutside()
        {
            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.BrainsEnabled = false;
            try
            {
                duel.ForceArenaLayout(3); // Cinderfield
                yield return null;

                // One of Cinderfield's pools is centered at (-8, 3) radius 3.
                Teleport(duel.Player, new Vector3(-8f, duel.Player.Position.y, 3f));
                Teleport(duel.Enemy, new Vector3(15f, duel.Enemy.Position.y, 15f)); // well clear of every pool
                yield return null; // let CharacterController settle/ground

                var playerHpBefore = duel.Player.Health.Hp;
                var enemyHpBefore = duel.Enemy.Health.Hp;

                for (var i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Health.Hp, Is.LessThan(playerHpBefore), "grounded in the lava pool takes damage");
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(enemyHpBefore), "well outside every pool takes none");
            }
            finally
            {
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
            duel.CloseHangar();
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
