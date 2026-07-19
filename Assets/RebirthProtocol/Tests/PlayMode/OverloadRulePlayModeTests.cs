using System.Collections;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // The overload rule (COMBAT_DOCTRINE §4.3): going down wipes the downed
    // pilot's own gun rounds in flight — never bomb/pod shots, never the
    // opponent's rounds, never scrapwright rounds flagged SurvivesKnockdown.
    public sealed class OverloadRulePlayModeTests
    {
        [UnityTest]
        public IEnumerator KnockdownWipesOwnGunRoundsButSparesPodsFoesAndExemptRounds()
        {
            var go = new GameObject("OverloadTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // pin Depot: no hazards
            duel.BrainsEnabled = false; // nobody moves or fires on their own
            try
            {
                yield return null;

                // Park slow test rounds high above the arena, aimed at open
                // sky, so nothing collides while the rule resolves.
                var sky = duel.Player.Center + Vector3.up * 6f;
                var up = sky + Vector3.up * 20f;
                duel.Projectiles.Spawn(duel.Player, null, sky, up, 1f, 1f, 1f, 0f, HitSource.Gun);
                duel.Projectiles.Spawn(duel.Player, null, sky + Vector3.right, up, 1f, 1f, 1f, 0f,
                    HitSource.Gun, survivesKnockdown: true);
                duel.Projectiles.Spawn(duel.Player, null, sky + Vector3.left, up, 1f, 1f, 1f, 0f, HitSource.Pod);
                duel.Projectiles.Spawn(duel.Enemy, null, sky + Vector3.forward, up, 1f, 1f, 1f, 0f, HitSource.Gun);

                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.EqualTo(2));

                var result = duel.Player.ReceiveHit(0f, 9999f, Vector3.forward);
                Assert.That(result, Is.EqualTo(ReceiveResult.Knockdown));

                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.EqualTo(1),
                    "own gun rounds wiped; the SurvivesKnockdown round rides it out");
                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Pod), Is.EqualTo(1),
                    "pod shots stay live — only gunfire overloads");
                Assert.That(duel.Projectiles.CountLiveRounds(duel.Enemy, HitSource.Gun), Is.EqualTo(1),
                    "the opponent's sky is untouched");

                // Let the cull pass run: wiped rounds must despawn cleanly.
                yield return null;
                yield return null;
                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Pod), Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ProjectileCausedKnockdownWipesSafelyMidTick()
        {
            // The wipe fires reentrantly from inside ProjectileSystem.Tick
            // when a round's own hit causes the knockdown — the counter-punish
            // the doctrine demands (down the wielder before the volley lands).
            var go = new GameObject("OverloadTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // pin Depot: no hazards
            duel.BrainsEnabled = false;
            try
            {
                yield return null;

                // Batchmode frames are not 60 fps — pin the step so 120
                // frames is a known two simulated seconds of flight time.
                Time.captureDeltaTime = 1f / 60f;
                yield return null;

                var sky = duel.Player.Center + Vector3.up * 6f;
                duel.Projectiles.Spawn(duel.Player, null, sky, sky + Vector3.up * 20f, 1f, 1f, 1f, 0f, HitSource.Gun);
                duel.Projectiles.Spawn(duel.Player, null, sky + Vector3.right, sky + Vector3.up * 20f,
                    1f, 1f, 1f, 0f, HitSource.Gun);

                // Enemy round that downs the player on contact.
                duel.Projectiles.Spawn(duel.Enemy, duel.Player, duel.Player.Center + Vector3.right * 3f,
                    duel.Player.Center, 0f, 9999f, 30f, 4f, HitSource.Gun);

                for (var i = 0; i < 120 && duel.Player.Health.State != HealthState.KnockedDown; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Health.State, Is.EqualTo(HealthState.KnockedDown),
                    "the endurance-shredding round must land — enemy rounds still live: "
                    + duel.Projectiles.CountLiveRounds(duel.Enemy, HitSource.Gun)
                    + ", player endurance: " + duel.Player.Health.Endurance);
                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.EqualTo(0),
                    "the volley in flight died with its wielder's fall");
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
