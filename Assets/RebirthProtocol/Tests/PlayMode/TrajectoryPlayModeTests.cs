using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Trajectory suite (ARMORY §4-5, Pass I1): arcing/vaulting/dropping/
    // looping gun rounds (Mangonel, Skysword, Evenfall, Falconet) and the
    // first melee that casts a projectile (Volant Falx). Every existing part
    // uses ProjectilePath.Direct, so these tests only exercise the new parts.
    public sealed class TrajectoryPlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("TrajectoryTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // Depot: crate field, one crate dead on the z-axis at (0, 0.8, 10)
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

        private const float LaneX = 3f; // clean lane off the z-axis crate, per ScalingPlayModeTests

        /// Fire one trigger-pull from a stationary shooter at `shooterPos`
        /// (facing +z) at a stationary target at `targetPos`, and report the
        /// damage the target took over the flight window (0 if none connected).
        private static IEnumerator FireAndMeasure(DuelManager duel, Vector3 shooterPos, Vector3 targetPos,
            System.Action<float> report, int settleFrames = 8, int windowFrames = 240)
        {
            Teleport(duel.Player, shooterPos);
            duel.Player.SetFacing(0f);
            Teleport(duel.Enemy, targetPos);
            duel.Enemy.SetFacing(Mathf.PI);
            duel.Player.Gun.ResetCooldown();
            for (var i = 0; i < settleFrames; i++)
            {
                yield return null;
            }

            var hpBefore = duel.Enemy.Health.Hp;
            duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
            for (var i = 0; i < windowFrames; i++)
            {
                yield return null;
                if (duel.Enemy.Health.Hp < hpBefore)
                {
                    break;
                }
            }

            report(hpBefore - duel.Enemy.Health.Hp);
        }

        /// The highest world-Y reached by any live projectile this frame (0 if
        /// none exist) — the trajectory-shape fingerprint a straight shot can't
        /// fake.
        private static float MaxProjectileY(DuelManager duel)
        {
            var t = duel.Projectiles.transform;
            var max = 0f;
            for (var i = 0; i < t.childCount; i++)
            {
                max = Mathf.Max(max, t.GetChild(i).position.y);
            }

            return max;
        }

        [UnityTest]
        public IEnumerator MangonelVaultsOverCoverAPlainShotCannotClear()
        {
            // Two signatures together, so neither a crate-destroying leak nor a
            // flat shot can pass: a plain round is stopped by the z-axis crate
            // between shooter and target, while Mangonel both strikes the mark
            // AND arcs a vaulting round well above head height on the way (a
            // straight shot never leaves muzzle height).
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("blaster")), PartsCatalog.DefaultLoadout());
                yield return null;
                var plain = 0f;
                yield return FireAndMeasure(duel, new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 14f), d => plain = d);

                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("mangonel")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, new Vector3(0f, 0f, 4f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 14f));
                duel.Enemy.SetFacing(Mathf.PI);
                duel.Player.Gun.ResetCooldown();
                for (var i = 0; i < 8; i++)
                {
                    yield return null;
                }

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
                var apex = 0f;
                var vault = 0f;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    apex = Mathf.Max(apex, MaxProjectileY(duel));
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        vault = hpBefore - duel.Enemy.Health.Hp;
                        break;
                    }
                }

                Assert.That(plain, Is.EqualTo(0f), "a plain straight round must be stopped by the crate");
                Assert.That(vault, Is.GreaterThan(0f), "Mangonel must clear the crate and strike the mark");
                Assert.That(apex, Is.GreaterThan(3f), "and a vaulting round must arc high over the cover, not fly flat");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SkyswordFallsThroughCoverOntoTheMark()
        {
            // Cover is negotiable: the crate between shooter and target stops a
            // plain shot, but Skysword's blades materialize high above the mark
            // and fall straight down, so the horizontal wall is irrelevant. The
            // spawn-height signature (a straight shot leaves the muzzle, never
            // the sky) is what a crate-destroying leak cannot fake.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("blaster")), PartsCatalog.DefaultLoadout());
                yield return null;
                var plain = 0f;
                yield return FireAndMeasure(duel, new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 14f), d => plain = d);

                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("skysword")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, new Vector3(0f, 0f, 4f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 14f));
                duel.Enemy.SetFacing(Mathf.PI);
                duel.Player.Gun.ResetCooldown();
                for (var i = 0; i < 8; i++)
                {
                    yield return null;
                }

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
                yield return null;

                Assert.That(MaxProjectileY(duel), Is.GreaterThan(8f),
                    "Skysword's blades must be cast from high above the mark, not fired from the muzzle");

                var sky = 0f;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        sky = hpBefore - duel.Enemy.Health.Hp;
                        break;
                    }
                }

                Assert.That(plain, Is.EqualTo(0f), "a plain straight round must be stopped by the crate");
                Assert.That(sky, Is.GreaterThan(0f), "Skysword must drop on the mark despite the cover between");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EvenfallHangsAtHeightThenDescendsAndStrikes()
        {
            // Fired grounded, the rounds materialize above the mark, hang a
            // beat at even-fall, then descend homing — still live well after a
            // level shot would have crossed the gap, and coming from above.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("evenfall")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(LaneX, 0f, -3f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(LaneX, 0f, 6f));
                duel.Enemy.SetFacing(Mathf.PI);
                for (var i = 0; i < 10; i++)
                {
                    yield return null; // settle grounded: the drop-hang only arms when fired grounded
                }

                Assert.That(duel.Player.Grounded, Is.True, "Evenfall's even-fall only arms when fired grounded");

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
                yield return null;

                // The round is born above the mark, not at the muzzle.
                Assert.That(duel.Projectiles.transform.childCount, Is.GreaterThan(0), "the rounds must exist");
                Assert.That(duel.Projectiles.transform.GetChild(0).position.y, Is.GreaterThan(5f),
                    "Evenfall's rounds hang high above the mark, not at muzzle height");

                var stillLiveMidHang = false;
                var connected = false;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (i == 24 && duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun) > 0
                        && duel.Enemy.Health.Hp == hpBefore)
                    {
                        stillLiveMidHang = true; // ~0.4s in: hanging, not yet descended
                    }

                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        connected = true;
                        break;
                    }
                }

                Assert.That(stillLiveMidHang, Is.True, "the rounds must hang at even-fall, not fall at once");
                Assert.That(connected, Is.True, "and then descend and strike");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FalconetLoopsAloftButFliesStraightGrounded()
        {
            // Stance-split: grounded the rounds fly a plain straight line;
            // aloft they bend into a wide loop. Fired with no locked target so
            // homing is off and only the path shapes the flight — measured as
            // lateral drift off the launch lane.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("falconet")), PartsCatalog.DefaultLoadout());
                yield return null;

                // Grounded: settle on the floor, fire with no target.
                Teleport(duel.Player, new Vector3(LaneX, 0f, -3f));
                duel.Player.SetFacing(0f);
                for (var i = 0; i < 10; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Grounded, Is.True);
                duel.Player.Gun.ResetCooldown();
                duel.Player.TickGun(1f / 60f, firing: true, target: null);
                var groundedDrift = 0f;
                for (var i = 0; i < 18; i++)
                {
                    yield return null;
                    if (duel.Projectiles.transform.childCount > 0)
                    {
                        groundedDrift = Mathf.Max(groundedDrift,
                            Mathf.Abs(duel.Projectiles.transform.GetChild(0).position.x - LaneX));
                    }
                }

                duel.Projectiles.Clear();

                // Aloft: teleport up and fire before landing.
                Teleport(duel.Player, new Vector3(LaneX, 8f, -3f));
                duel.Player.SetFacing(0f);
                yield return null;
                Assert.That(duel.Player.Grounded, Is.False, "must be airborne to glide the loop");
                duel.Player.Gun.ResetCooldown();
                duel.Player.TickGun(1f / 60f, firing: true, target: null);
                var aloftDrift = 0f;
                for (var i = 0; i < 18; i++)
                {
                    yield return null;
                    if (duel.Projectiles.transform.childCount > 0)
                    {
                        aloftDrift = Mathf.Max(aloftDrift,
                            Mathf.Abs(duel.Projectiles.transform.GetChild(0).position.x - LaneX));
                    }
                }

                Assert.That(groundedDrift, Is.LessThan(0.5f), "grounded, Falconet flies a straight lane");
                Assert.That(aloftDrift, Is.GreaterThan(1.5f), "aloft, its rounds glide a wide curving loop");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator VolantFalxCastsAWaveThatStrikesPastAWhiffedContact()
        {
            // The first melee that spawns a projectile: at a distance just
            // outside the blade's own reach (so the contact swing whiffs) the
            // looping wave still flies out and strikes — where a plain blade,
            // which casts nothing, leaves the target untouched.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                // Oathblade control: contact whiffs at 3.8m (HitRange 3.0) and
                // there is no wave, so the target takes nothing.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("saber")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3.8f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var plainHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 180 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var plainDamage = plainHp - duel.Enemy.Health.Hp;

                // Volant Falx: contact whiffs too (HitRange 3.6 < 3.8), but the
                // cast wave reaches the target.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("volant-falx")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3.8f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;
                var waveHp = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                var sawMeleeProjectile = false;
                for (var i = 0; i < 180; i++)
                {
                    yield return null;
                    if (duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Melee) > 0)
                    {
                        sawMeleeProjectile = true;
                    }

                    if (!duel.Player.Melee.Busy && duel.Enemy.Health.Hp < waveHp)
                    {
                        break;
                    }
                }

                var waveDamage = waveHp - duel.Enemy.Health.Hp;

                Assert.That(plainDamage, Is.EqualTo(0f), "a plain blade whiffs at this range and casts no wave");
                Assert.That(sawMeleeProjectile, Is.True, "Volant Falx must cast a melee-source projectile");
                Assert.That(waveDamage, Is.GreaterThan(0f), "and that wave must strike past the whiffed contact");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // --- Codex PR #24 review regression tests ---

        [UnityTest]
        public IEnumerator FalconetLoopSurvivesAndReturnsForItsSecondPass()
        {
            // Codex PR #24 P1: the shared 2.0s lifetime is shorter than a
            // Falconet revolution (2.6 rad/s -> 2.42s), so the loop despawned
            // mid-circle and never returned. With a period-scaled lifetime the
            // round must outlive the old cap AND swing wide, then come back to
            // its launch lane (a pure circle re-crosses only its start point,
            // so the honest mechanical read of "second pass" is this return).
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("falconet")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(LaneX, 8f, -3f));
                duel.Player.SetFacing(0f);
                yield return null;
                Assert.That(duel.Player.Grounded, Is.False, "must be airborne to glide the loop");
                duel.Player.Gun.ResetCooldown();
                duel.Player.TickGun(1f / 60f, firing: true, target: null); // no lock: pure geometric loop
                yield return null;
                Assert.That(duel.Projectiles.transform.childCount, Is.GreaterThan(0), "the loop rounds must exist");

                var round = duel.Projectiles.transform.GetChild(0);
                var launch = round.position;
                launch.y = 0f;
                var maxDist = 0f;
                var hasLeft = false;
                var returnMin = 999f;
                var aliveLate = false;
                for (var i = 0; i < 200; i++)
                {
                    yield return null;
                    if (round == null)
                    {
                        break; // despawned
                    }

                    var pos = round.position;
                    pos.y = 0f;
                    var d = Vector3.Distance(pos, launch);
                    maxDist = Mathf.Max(maxDist, d);
                    if (d > 6f)
                    {
                        hasLeft = true;
                    }

                    if (hasLeft)
                    {
                        returnMin = Mathf.Min(returnMin, d);
                    }

                    if (i >= 126)
                    {
                        aliveLate = true; // still in flight past the old 2.0s (~120-frame) cap
                    }
                }

                Assert.That(maxDist, Is.GreaterThan(8f), "the loop must swing wide before returning");
                Assert.That(aliveLate, Is.True, "the loop round must outlive the old 2.0s cap to complete its circle");
                Assert.That(returnMin, Is.LessThan(4f), "and return to its launch lane for the second pass");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator VolantWaveIsWipedByAMeleeClash()
        {
            // Codex PR #24 P1: the wave is cast on swing-entry (during the brain
            // tick) before CheckMeleeClash runs, so a clash cancelled the Melee
            // action but the already-cast wave still flew. Two Volant Falx
            // fighters swinging simultaneously in clash range must knock apart
            // AND take no damage — contact is cancelled by the clash, and the
            // waves are now wiped too, so nothing lands.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("volant-falx")),
                    LoadoutWith(melee: Melee("volant-falx")));
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, -1.5f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 1.5f)); // 3.0m apart: inside clashRange 3.5
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var pHp = duel.Player.Health.Hp;
                var eHp = duel.Enemy.Health.Hp;
                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                duel.Player.TryMelee(duel.Enemy);
                duel.Enemy.TryMelee(duel.Player);

                var clashed = false;
                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                    if (duel.Player.FlatDistanceTo(duel.Enemy) > startDist + 0.5f)
                    {
                        clashed = true; // the clash knockback pushed them apart
                    }
                }

                Assert.That(clashed, Is.True, "simultaneous close swings must clash and knock both apart");
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(eHp),
                    "the clash must wipe the cast wave, not just the contact — the enemy takes nothing");
                Assert.That(duel.Player.Health.Hp, Is.EqualTo(pHp), "and neither does the player");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MangonelVaultLandsOnTheMarkAtACoarseTimestep()
        {
            // Codex PR #24 P1: semi-implicit Euler landed the arc ~vaultRise*dt
            // low (2.6m at dt=0.2), turning a hit into a miss. With analytic
            // integration the vault lands on the mark regardless of step size.
            // Straddling the z-axis crate blocks the two straight streams, so
            // only the vault can reach — isolating the arc's landing accuracy.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 0.2f; // ~5 fps: coarse enough that semi-implicit Euler would miss
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("mangonel")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, 4f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 14f));
                duel.Enemy.SetFacing(Mathf.PI);
                for (var i = 0; i < 5; i++)
                {
                    yield return null; // settle grounded at the coarse step
                }

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
                var connected = false;
                for (var i = 0; i < 60; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        connected = true;
                        break;
                    }
                }

                Assert.That(connected, Is.True,
                    "the vault must still land on the mark at a coarse timestep, not fall short below it");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        /// A stack of `count` non-damageable box colliders evenly placed from
        /// `from` to `to`, each `scale` in size — plain cover the cover-ignoring
        /// paths must see past. Returned container is the caller's to destroy.
        private static GameObject SpawnColliderWall(int count, Vector3 from, Vector3 to, Vector3 scale)
        {
            var container = new GameObject("ColliderWall");
            for (var i = 0; i < count; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); // carries a BoxCollider
                cube.transform.SetParent(container.transform, false);
                var t = count > 1 ? i / (count - 1f) : 0f;
                cube.transform.position = Vector3.Lerp(from, to, t);
                cube.transform.localScale = scale;
            }

            return container;
        }

        [UnityTest]
        public IEnumerator CoverIgnoringDropHitsThroughAColliderWallThatSaturatesTheBuffer()
        {
            // Codex PR #24 P2: RaycastNonAlloc returns an unordered subset when
            // there are more hits than the buffer holds, so the target behind a
            // dense wall of cover could be dropped and a cover-ignoring round
            // pass through it too. With the grown buffer + RaycastAll fallback,
            // Skysword's straight-down drop (no curve to complicate the geometry)
            // must still reach the target under a saturating vertical stack of
            // cover.
            var duel = BootDuel(out var go);
            GameObject wall = null;
            try
            {
                yield return null;
                Time.captureDeltaTime = 0.2f; // coarse: one descent step spans the lower stack + the target
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("skysword")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(LaneX, 0f, 0f));
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(LaneX, 0f, 8f)); // clean lane, clear of the Depot crates
                duel.Enemy.SetFacing(Mathf.PI);
                for (var i = 0; i < 5; i++)
                {
                    yield return null;
                }

                // 30 colliders (well past the 16-entry buffer) stacked in the
                // air directly above the target, clear of its capsule — the
                // centre blade descends straight through them onto the mark.
                wall = SpawnColliderWall(30, new Vector3(LaneX, 2.4f, 8f), new Vector3(LaneX, 7f, 8f),
                    new Vector3(1.2f, 0.03f, 1.2f));
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);
                var connected = false;
                for (var i = 0; i < 60; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        connected = true;
                        break;
                    }
                }

                Assert.That(connected, Is.True,
                    "a cover-ignoring drop must still reach the mark past a buffer-saturating stack of cover");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                if (wall != null)
                {
                    Object.Destroy(wall);
                }

                Object.Destroy(go);
            }

            yield return null;
        }
    }
}
