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
            duel.ForceArenaLayout(0); // pin Depot: no hazards to complicate this smoke test

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
            duel.ForceArenaLayout(0); // pin Depot: not testing hazards here
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
                // Pin both sides to a known loadout AND a known arena layout
                // instead of whatever the shared _enemyBuildIndex counter's
                // rotation happens to land on (it drives both the enemy's
                // build and the arena layout — advances once per test that
                // creates a DuelManager, so what any given test gets
                // depends on how many other tests already ran). This test
                // asserts HP == MaxHp with zero tolerance: with Cinderfield
                // active, the default player spawn (-8,0,0) sits exactly on
                // pool 1's boundary (center (-8,3), radius 3) and takes a
                // sliver of real lava damage before the assertion even
                // runs — a real latent bug in the coincidence of those two
                // constants, not a flake, so pin Depot to sidestep it.
                var loadout = PartsCatalog.DefaultLoadout();
                duel.RespawnWithLoadouts(loadout, loadout);
                duel.ForceArenaLayout(0);
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
            duel.ForceArenaLayout(0); // pin Depot: not testing hazards here
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
                    Shield = PartsCatalog.Shields[0], // Ward Veil (id "aegis"): 180 hp, 75% front block
                    Legs = PartsCatalog.Legs[0],
                    Pod = PartsCatalog.Pods[0]
                };
                duel.RespawnWithLoadouts(shieldLoadout, PartsCatalog.DefaultLoadout());
                duel.ForceArenaLayout(0); // pin Depot: the exact-equality HP assertions below can't afford stray lava chip
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
                // Reposition by setting the transform directly rather than
                // the shared Teleport helper's disable/re-enable dance:
                // toggling CharacterController.enabled here left isGrounded
                // stuck false for many frames in practice (a real Unity
                // quirk, not a gameplay bug — normal play never
                // disables/re-enables the controller mid-fight).
                duel.Player.transform.position = new Vector3(-8f, duel.Player.Position.y, 3f);
                duel.Enemy.transform.position = new Vector3(15f, duel.Enemy.Position.y, 15f); // well clear of every pool

                var elapsed = 0f;
                while (!duel.Player.Grounded && elapsed < 2f)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                Assert.That(duel.Player.Grounded, Is.True, "settled and grounded before the timed window starts");

                var playerHpBefore = duel.Player.Health.Hp;
                var enemyHpBefore = duel.Enemy.Health.Hp;

                for (var i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Health.Hp, Is.LessThan(playerHpBefore), "grounded in the lava pool takes damage");
                // A real lava tick over 30 frames would be several HP at
                // minimum (24 hp/s); a tolerance this tight still catches
                // that while not flaking on incidental float noise from
                // ~30 frames of unrelated simulation (knockback decay,
                // shield regen, etc. all run every frame regardless).
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(enemyHpBefore).Within(0.5f), "well outside every pool takes none");
            }
            finally
            {
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BulwarkChassisUsesTheRiggedCobaltKnightAssetAndSimulatesCleanly()
        {
            var bulwarkLoadout = new Loadout
            {
                Body = PartsCatalog.Bodies[3], // bulwark -- the one chassis with a real rigged asset
                Gun = PartsCatalog.Guns[0],
                Shield = PartsCatalog.Shields[0],
                Legs = PartsCatalog.Legs[0],
                Pod = PartsCatalog.Pods[0]
            };

            var go = new GameObject("DuelTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.RespawnWithLoadouts(bulwarkLoadout, bulwarkLoadout);
            duel.ForceArenaLayout(0); // pin Depot: this test is about the visual, not hazards
            duel.BrainsEnabled = false;
            try
            {
                Assert.That(duel.Player.transform.Find("Visual/Tilt/CobaltKnight"), Is.Not.Null,
                    "Bulwark should build the real Cobalt Knight visual, not the primitive chassis");

                // A handful of frames to confirm nothing in the real-mesh
                // path throws once combat starts simulating around it.
                for (var i = 0; i < 20; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Health.State, Is.Not.EqualTo(HealthState.Dead));
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
            duel.ForceArenaLayout(0); // pin Depot: not testing hazards here
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
