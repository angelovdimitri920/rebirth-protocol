using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Scaling & delayed threats (ARMORY §13.1, Pass H): distance/speed/lunge
    // damage scaling (Pilgrim, Beacon, Courser Saber, Tilt Lance, Crowbeak
    // Pick) and delayed/hanging actives (Vigil trap, Penitent Flail late
    // strike). Every existing part uses the neutral (Flat / None / no-hang /
    // no-delay) variant, so these tests only exercise the seven new parts.
    public sealed class ScalingPlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("ScalingTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // Tiltyard: no hazards
            duel.BrainsEnabled = false;
            return duel;
        }

        private static void Teleport(RoboAvatar avatar, Vector3 to)
        {
            var cc = avatar.GetComponent<CharacterController>();
            cc.enabled = false;
            avatar.transform.position = to;
            cc.enabled = true;
        }

        private static Loadout LoadoutWith(GunPart gun = null, MeleeWeaponPart melee = null) => new Loadout
        {
            Body = PartsCatalog.Bodies[0],
            Gun = melee == null ? gun ?? PartsCatalog.Guns[0] : null,
            Melee = melee,
            Bomb = PartsCatalog.Bombs[0],
            Legs = PartsCatalog.Legs[0],
            Pod = PartsCatalog.Pods[0]
        };

        private static GunPart Gun(string id) => PartsCatalog.Guns.First(g => g.Id == id);
        private static MeleeWeaponPart Melee(string id) => PartsCatalog.MeleeWeapons.First(m => m.Id == id);

        // The Depot layout (ForceArenaLayout 0) is a crate field with one
        // crate dead on the z-axis at (0, 0.8, 10) — it would block any shot
        // or lunge fired straight down z. x=3 is a clean lane (nearest crates
        // are at x=5 and x=0), and centring the pair around the origin keeps
        // even an 18m gap comfortably inside the 32m arena.
        private const float LaneX = 3f;

        /// Fire one gun round from a stationary shooter at a stationary target
        /// `gap` metres away and return the damage the target took (0 if it
        /// never connected within the window).
        private static IEnumerator FireOnceAndMeasure(DuelManager duel, float gap,
            System.Action<float> report)
        {
            Teleport(duel.Player, new Vector3(LaneX, 0f, -gap * 0.5f));
            duel.Player.SetFacing(0f);
            Teleport(duel.Enemy, new Vector3(LaneX, 0f, gap * 0.5f));
            duel.Enemy.SetFacing(Mathf.PI);
            // The gun cooldown persists across calls on a reused duel (the
            // brains that would tick it are disabled), so a second shot would
            // silently fail to fire — reset it so every measurement fires.
            duel.Player.Gun.ResetCooldown();
            yield return null;

            var hpBefore = duel.Enemy.Health.Hp;
            duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
            for (var i = 0; i < 240; i++)
            {
                yield return null;
                if (duel.Enemy.Health.Hp < hpBefore)
                {
                    break;
                }
            }

            report(hpBefore - duel.Enemy.Health.Hp);
        }

        [UnityTest]
        public IEnumerator PilgrimHitsHarderTheFartherItFlies()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("pilgrim")), PartsCatalog.DefaultLoadout());
                yield return null;

                var near = 0f;
                yield return FireOnceAndMeasure(duel, 4f, d => near = d);
                var far = 0f;
                yield return FireOnceAndMeasure(duel, 22f, d => far = d);

                Assert.That(near, Is.GreaterThan(0f), "the near shot must connect");
                Assert.That(far, Is.GreaterThan(0f), "the far shot must connect");
                Assert.That(far, Is.GreaterThan(near * 1.5f),
                    "Rangecraft: a round that flew far must hit markedly harder than a point-blank one");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BeaconPeaksAtItsBurstDistanceAndWastesElsewhere()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("beacon")), PartsCatalog.DefaultLoadout());
                yield return null;

                var atBurst = 0f;
                yield return FireOnceAndMeasure(duel, 18f, d => atBurst = d); // the bloom (18m +/-3.5)
                var tooClose = 0f;
                yield return FireOnceAndMeasure(duel, 5f, d => tooClose = d); // fired too early

                Assert.That(atBurst, Is.GreaterThan(0f), "the burst shot must connect");
                Assert.That(tooClose, Is.GreaterThan(0f), "the close shot must connect");
                Assert.That(atBurst, Is.GreaterThan(tooClose * 1.8f),
                    "Burst-point: dead-on the bloom must far outdamage a shot fired too close");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BeaconBurstWindowIsFrameRateIndependentAtTheEdge()
        {
            // Codex PR #23: a raycast hit scaled off the pre-step distance
            // made the burst-window boundary frame-rate dependent. Fired just
            // PAST the window (18m +/-3.5 → 21.5m edge; ~22m impact) under a
            // COARSE dt (a ~2.3m step), the round must still read as
            // off-window (low damage) — with the bug the undercredited
            // distance would fall back inside the window and read peak.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 15f; // coarse: ~2.3m per step at 34 m/s
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("beacon")), PartsCatalog.DefaultLoadout());
                yield return null;

                var pastWindow = 0f;
                yield return FireOnceAndMeasure(duel, 23f, d => pastWindow = d);

                Assert.That(pastWindow, Is.GreaterThan(0f), "the shot must connect");
                // Off-window is 0.6 x 75 = 45; peak is 1.65 x 75 = 124. A
                // reading below the midpoint proves the distance credited at
                // impact was the true (past-the-edge) one, not an
                // undercredited in-window value.
                Assert.That(pastWindow, Is.LessThan(80f),
                    "past the burst window the round must read off-window regardless of step size");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator VigilRoundHangsThenStrikes()
        {
            // The trap identity: fired grounded, the round should still be
            // alive and in flight/hovering well after a normal shot would
            // have crossed the same gap — then it connects.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("vigil")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(LaneX, 0f, -6f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(LaneX, 0f, 6f));
                duel.Enemy.SetFacing(Mathf.PI);
                // Let the shooter settle onto the floor: the trap only arms
                // when fired grounded (a teleport leaves Grounded stale for a
                // frame or two until the next CharacterController.Move lands).
                for (var i = 0; i < 10; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Grounded, Is.True, "the trap only arms when fired grounded");

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                // Around the hang window (~1.1s after flying ~9m) the round
                // must still be live — a non-hanging shot at 28 m/s would have
                // crossed 12m in <0.5s and already resolved.
                var stillLiveMidHang = false;
                var connected = false;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (i == 60 && duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun) > 0)
                    {
                        stillLiveMidHang = true; // ~1s in and not yet resolved: it hung
                    }

                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        connected = true;
                        break;
                    }
                }

                Assert.That(stillLiveMidHang, Is.True, "the round must keep watch, not cross instantly");
                Assert.That(connected, Is.True, "and then strike");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CourserSaberHitsHarderAtSpeedThanFromAStandstill()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                // Standing swing: target within CloseRange so there is no
                // gap-closing lunge, and the wielder carries no speed in.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("courser-saber")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2.5f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var standingHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var standingDamage = standingHp - duel.Enemy.Health.Hp;

                // Running swing: drive the wielder at full run into the target,
                // so it commits the swing carrying speed.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("courser-saber")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 6f));
                duel.Enemy.SetFacing(Mathf.PI);
                duel.Player.Intent = new RoboIntent { MoveDir = Vector3.forward };
                // Let it accelerate to run speed before the swing commits.
                for (var i = 0; i < 30; i++)
                {
                    duel.Player.Intent = new RoboIntent { MoveDir = Vector3.forward };
                    yield return null;
                    if (duel.Player.FlatDistanceTo(duel.Enemy) < 3.5f)
                    {
                        break;
                    }
                }

                var runningHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var runningDamage = runningHp - duel.Enemy.Health.Hp;

                Assert.That(standingDamage, Is.GreaterThan(0f), "the standing swing must connect");
                Assert.That(runningDamage, Is.GreaterThan(0f), "the running swing must connect");
                Assert.That(runningDamage, Is.GreaterThan(standingDamage * 1.3f),
                    "Courser Saber must hit harder carrying speed than from a standstill");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TiltLanceHitsHarderOffALongLungeThanAStandingThrust()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                // Standing thrust: within CloseRange, no lunge charged.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("tilt-lance")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, new Vector3(LaneX, 0f, -1.5f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(LaneX, 0f, 1.5f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var standingHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var standingDamage = standingHp - duel.Enemy.Health.Hp;

                // Long joust: start well beyond CloseRange so the gap-closer
                // charges a real distance before the strike lands. On the
                // clean x=3 lane so the lunge doesn't run into the z-axis
                // crate at (0, 0.8, 10).
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("tilt-lance")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, new Vector3(LaneX, 0f, -6f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(LaneX, 0f, 6f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var joustHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 180 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var joustDamage = joustHp - duel.Enemy.Health.Hp;

                Assert.That(standingDamage, Is.GreaterThan(0f), "the standing thrust must connect");
                Assert.That(joustDamage, Is.GreaterThan(0f), "the joust must connect");
                Assert.That(joustDamage, Is.GreaterThan(standingDamage * 1.4f),
                    "Tilt Lance: a long charge must land far harder than a standing thrust");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CrowbeakPickHitsHarderAtTheTipThanUpClose()
        {
            // Space it or waste it: a hit taken at the far edge of reach must
            // far exceed one smothered up close. Both are direct swings (no
            // lunge) at a fixed distance, so only the tip-scaling differs.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("crowbeak-pick")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 1.5f)); // smothered
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var closeHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var closeDamage = closeHp - duel.Enemy.Health.Hp;

                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("crowbeak-pick")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3.1f)); // at the tip (HitRange 3.2)
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var tipHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var tipDamage = tipHp - duel.Enemy.Health.Hp;

                Assert.That(closeDamage, Is.GreaterThan(0f), "the close swing must connect");
                Assert.That(tipDamage, Is.GreaterThan(0f), "the tip swing must connect");
                Assert.That(tipDamage, Is.GreaterThan(closeDamage * 1.6f),
                    "Crowbeak Pick: all the power lives in the tip");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PenitentFlailStrikesLateInItsSwing()
        {
            // The late-strike: the hit must not register on the first active
            // frame the way an ordinary swing does — it lands only after the
            // swing has entered its late window.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("penitent-flail")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2.5f)); // in range, immediate swing
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);

                // One frame in, the swing is active but must NOT have landed
                // yet (an ordinary weapon would already have connected).
                yield return null;
                yield return null;
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore),
                    "the hit must not register on the opening frames — the timing is late");

                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "but it must still land before the swing ends");
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
