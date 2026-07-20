using System.Collections;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Charge attacks (COMBAT_DOCTRINE §4.5): a committed grounded body-
    // strike — i-frames during the strike, vulnerability before and after,
    // ground-only, canceled by knockdown.
    public sealed class ChargePlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("ChargeTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // pin Depot: no hazards
            duel.BrainsEnabled = false; // nobody moves or fires on their own
            return duel;
        }

        private static void Teleport(RoboAvatar avatar, Vector3 to)
        {
            var cc = avatar.GetComponent<CharacterController>();
            cc.enabled = false;
            avatar.transform.position = to;
            cc.enabled = true;
        }

        [UnityTest]
        public IEnumerator ChargeConnectsWithIFramesDuringTheStrike()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                // Spawn gap is 16 m — more than the straight charge covers.
                // Close to 5 m so the strike reaches.
                Teleport(duel.Player, duel.Enemy.Position + Vector3.left * 5f);
                yield return null;

                duel.Player.TryCharge(duel.Enemy);
                Assert.That(duel.Player.Charge.Busy, Is.True, "grounded charge must start");
                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Windup));
                Assert.That(duel.Player.Intangible, Is.False, "windup is the vulnerable-before");

                var hpBefore = duel.Enemy.Health.Hp;
                var sawStrikeIFrames = false;
                var evadedDuringStrike = false;
                for (var i = 0; i < 240 && duel.Player.Charge.Busy; i++)
                {
                    if (duel.Player.Charge.Phase == ChargePhase.Strike && !sawStrikeIFrames)
                    {
                        sawStrikeIFrames = duel.Player.Intangible;
                        evadedDuringStrike =
                            duel.Player.ReceiveHit(1f, 1f, Vector3.forward) == ReceiveResult.Evaded;
                    }

                    yield return null;
                }

                Assert.That(sawStrikeIFrames, Is.True, "the strike window grants i-frames");
                Assert.That(evadedDuringStrike, Is.True, "attacks pass through the strike");
                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the body-strike landed");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator WhiffEndsInARootedVulnerableRecovery()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                // 16 m apart: the straight charge (22 m/s × 0.35 s) whiffs.
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryCharge(duel.Enemy);
                for (var i = 0; i < 240 && duel.Player.Charge.Phase != ChargePhase.Recovery; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Recovery), "the whiff must recover");
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore), "nothing landed");
                Assert.That(duel.Player.ControlLocked, Is.True, "recovery roots the charger");
                Assert.That(duel.Player.ReceiveHit(10f, 5f, Vector3.forward), Is.EqualTo(ReceiveResult.Hit),
                    "recovery is the vulnerable-after: hits land clean");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AirborneChargeIsRefusedAndKnockdownCancelsAWindup()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                // Thrust until airborne: charges are ground-only (§4.5).
                duel.Player.Intent = new RoboIntent { ThrustHeld = true };
                for (var i = 0; i < 60 && duel.Player.Grounded; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Grounded, Is.False, "thrust must lift off");
                duel.Player.TryCharge(duel.Enemy);
                Assert.That(duel.Player.Charge.Busy, Is.False, "air X stays dash — no charge aloft");

                // Land, then knock the charger down mid-windup: the charge dies.
                duel.Player.Intent = new RoboIntent();
                for (var i = 0; i < 240 && !duel.Player.Grounded; i++)
                {
                    yield return null;
                }

                for (var i = 0; i < 240 && duel.Player.ControlLocked; i++)
                {
                    yield return null; // wait out the landing recovery
                }

                duel.Player.TryCharge(duel.Enemy);
                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Windup));
                Assert.That(duel.Player.ReceiveHit(0f, 9999f, Vector3.forward),
                    Is.EqualTo(ReceiveResult.Knockdown), "windup has no i-frames");
                yield return null;
                Assert.That(duel.Player.Charge.Busy, Is.False, "a downed pilot is not mid-charge");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CobaltKnightsRisingStrikeClimbs()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                var bulwark = PartsCatalog.DefaultLoadout();
                bulwark.Body = PartsCatalog.Bodies[3]; // Cobalt Knight: Air-kind charge
                duel.RespawnWithLoadouts(bulwark, PartsCatalog.DefaultLoadout());
                yield return null;

                duel.Player.TryCharge(duel.Enemy);
                var peak = 0f;
                for (var i = 0; i < 360 && duel.Player.Charge.Busy; i++)
                {
                    peak = Mathf.Max(peak, duel.Player.Position.y);
                    yield return null;
                }

                Assert.That(peak, Is.GreaterThan(1.5f), "the rising strike must contest the sky");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }
    }
}
