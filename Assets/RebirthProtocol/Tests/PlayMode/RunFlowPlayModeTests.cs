using System.Collections;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    public sealed class RunFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator VictoryOpensTheDraftAndResolvingItAdvancesTheRun()
        {
            DuelManager.RunSeedOverride = 12345;
            var go = new GameObject("RunTest");
            try
            {
                var duel = go.AddComponent<DuelManager>();
                duel.CloseHangar();
                duel.ForceArenaLayout(0); // pin Depot: exact HP assertions below
                duel.BrainsEnabled = false;
                yield return null;

                Assert.That(duel.FightNumber, Is.EqualTo(1));
                Assert.That(duel.Enemy.Health.MaxHp, Is.EqualTo(1000f), "fight 1 rival at power mult 1.0");

                // Wound the player so the HP carry is observable, then win.
                duel.Player.ReceiveHit(400f, 0f, Vector3.forward);
                duel.Enemy.ReceiveHit(duel.Enemy.Health.MaxHp + 1f, 0f, Vector3.forward);

                var elapsed = 0f;
                while (!duel.InDraft && elapsed < 4f)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                Assert.That(duel.InDraft, Is.True, "draft opens after the victory beat");
                Assert.That(duel.Draft, Is.Not.Null);

                // Pick the first boon exactly as Enter/A would.
                duel.Draft.Activate(0);
                duel.ForceArenaLayout(0); // next fight rolled a seeded arena; re-pin before HP checks

                Assert.That(duel.InDraft, Is.False);
                Assert.That(duel.Effects.BoonList.Count, Is.EqualTo(1), "picked boon installed");
                Assert.That(duel.FightNumber, Is.EqualTo(2));

                // Fight 2 rival: Skylance (0.8 HP mult) at power mult 1.12.
                Assert.That(duel.Enemy.Health.MaxHp, Is.EqualTo(Mathf.Round(1000f * 0.8f * 1.12f)));

                // HP carried: 600 at victory + 15% of max (1000) = 750.
                Assert.That(duel.Player.Health.Hp, Is.EqualTo(750f).Within(0.5f));
            }
            finally
            {
                DuelManager.RunSeedOverride = 0;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DroppedItemIsCollectedByWalkOverAndGrantsAStack()
        {
            DuelManager.RunSeedOverride = 777;
            var go = new GameObject("RunTest");
            try
            {
                var duel = go.AddComponent<DuelManager>();
                duel.CloseHangar();
                duel.ForceArenaLayout(0);
                duel.BrainsEnabled = false;
                yield return null;

                duel.DebugForceItemDrop(duel.Player.Position + new Vector3(0.5f, 0f, 0f));
                yield return null; // pickup tick: player is inside collect range

                var totalStacks = 0;
                foreach (var stack in duel.Effects.ItemStacks)
                {
                    totalStacks += stack.Value;
                }

                Assert.That(totalStacks, Is.EqualTo(1), "walk-over pickup granted exactly one stack");
            }
            finally
            {
                DuelManager.RunSeedOverride = 0;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDeathEndsTheRun()
        {
            DuelManager.RunSeedOverride = 555;
            var go = new GameObject("RunTest");
            try
            {
                var duel = go.AddComponent<DuelManager>();
                duel.CloseHangar();
                duel.ForceArenaLayout(0);
                duel.BrainsEnabled = false;
                yield return null;

                duel.Player.ReceiveHit(duel.Player.Health.MaxHp + 1f, 0f, Vector3.forward);
                yield return null;
                yield return null;

                Assert.That(duel.IsOver, Is.True);
                Assert.That(duel.RunOver, Is.True);
                Assert.That(duel.RunWon, Is.False);
                Assert.That(duel.InDraft, Is.False, "defeat never opens a draft");
            }
            finally
            {
                DuelManager.RunSeedOverride = 0;
                Object.Destroy(go);
            }

            yield return null;
        }
    }
}
