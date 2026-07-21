using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Volley capability (ARMORY §4-6, Pass E, DOCTRINE §13 pillar 3 "volley
    // truth"): spread guns, multi-angle melee, and multi-point bombs. Every
    // existing built part's behavior is unchanged (ProjectileCount=1,
    // ProngAngles=null, Pattern=Single are all no-ops) — these tests only
    // cover the five new parts.
    public sealed class VolleyPlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("VolleyTest");
            var duel = go.AddComponent<DuelManager>();
            duel.CloseHangar();
            duel.ForceArenaLayout(0); // Depot: no hazards
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

        private static Loadout LoadoutWith(GunPart gun = null, MeleeWeaponPart melee = null, BombPart bomb = null) => new Loadout
        {
            Body = PartsCatalog.Bodies[0],
            // The right arm is never empty in a real loadout (RoboVisual
            // assumes gun XOR melee) — default to the standard gun when a
            // test only cares about the bomb slot.
            Gun = melee == null ? gun ?? PartsCatalog.Guns[0] : null,
            Melee = melee,
            Bomb = bomb ?? PartsCatalog.Bombs[0],
            Legs = PartsCatalog.Legs[0],
            Pod = PartsCatalog.Pods[0]
        };

        private static GunPart Gun(string id) => PartsCatalog.Guns.First(g => g.Id == id);
        private static MeleeWeaponPart Melee(string id) => PartsCatalog.MeleeWeapons.First(m => m.Id == id);
        private static BombPart Bomb(string id) => PartsCatalog.Bombs.First(b => b.Id == id);

        [UnityTest]
        public IEnumerator TrefoilFiresThreeIndependentStreamsPerTriggerPull()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("trefoil")), PartsCatalog.DefaultLoadout());
                yield return null;

                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.Zero);
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.EqualTo(3),
                    "one trigger-pull spawns all 3 streams at once, not 1");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ArbalestStillFiresExactlyOneStream()
        {
            // Regression guard: ProjectileCount=1 (every gun before Trefoil)
            // must still spawn exactly one round per trigger-pull.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                Assert.That(duel.Projectiles.CountLiveRounds(duel.Player, HitSource.Gun), Is.EqualTo(1));
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LongglaiveConnectsAtAWideAngleAStandardBladeWouldMiss()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("longglaive")), PartsCatalog.DefaultLoadout());
                yield return null;

                // Oathblade's arc is 70° (±35°); Longglaive's is 140° (±70°).
                // 55° off dead-ahead is a miss for the standard blade but a
                // connect for Longglaive.
                PositionAtAngle(duel.Player, duel.Enemy, 55f, 3f);
                duel.Player.SetFacing(0f);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "140° arc reaches 55° off-facing");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HydraFlailConnectsAtAnAngleLongglaiveCannotReach()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("hydra-flail")), PartsCatalog.DefaultLoadout());
                yield return null;

                // 75° is outside even Longglaive's ±70° arc, but inside
                // Hydra Flail's +60° prong (covers [40°, 80°] at a 40°-wide
                // half-arc of 20°) — proof this is genuinely multi-prong
                // coverage, not just "a very wide single arc" reskinned.
                PositionAtAngle(duel.Player, duel.Enemy, 75f, 3f);
                duel.Player.SetFacing(0f);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the +60° prong covers 75°");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HydraFlailMissesBeyondItsTotalProngSpan()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("hydra-flail")), PartsCatalog.DefaultLoadout());
                yield return null;

                // Prongs span -80..+80 total (±60 centers, ±20 half-arc
                // each); 100° is outside every prong -- proves the check is
                // still a real gate, not an accept-anything fallback.
                PositionAtAngle(duel.Player, duel.Enemy, 100f, 3f);
                duel.Player.SetFacing(0f);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore), "100° is outside every prong");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PalisadeReachesFartherThanASinglePointBlastWould()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(bomb: Bomb("palisade")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, 0f));
                // Face -Z: Depot's crate field (ArenaBuilder.cs) has a crate
                // at (0, 0.8, 10) sitting right in the +Z line this test
                // needs; -Z has no crate within reach of any probe point.
                duel.Player.SetFacing(Mathf.PI); // FacingDir = -Z
                // Keep Enemy well clear during aim/throw -- Depot's arena
                // geometry can nudge a parked CharacterController out of a
                // crate overlap over several frames, and Palisade is Self-
                // anchored so it never reads Enemy's position for aiming
                // anyway (unlike Pincer Charge).
                Teleport(duel.Enemy, new Vector3(14f, 0f, -14f)); // clear of every Depot crate and the walls (half-size 16)
                yield return null;

                Assert.That(duel.Player.Grounded, Is.True);
                duel.PlayerBomb.StartAim(duel.Enemy);
                yield return null;
                duel.PlayerBomb.UpdateAim(duel.Enemy);
                duel.PlayerBomb.Release();

                // Self-anchored at ReticuleRange 5.5 along facing -> nominal
                // impact ~-5.5 on Z. A single-point blast (radius 2.6 + the
                // 0.5 hit-buffer BombSystem uses) reaches to ~-8.6. The Line
                // pattern's outer point sits 2.4 beyond that (~-7.9),
                // reaching to ~-11.0 -- so a target at -10.5 is a MISS for a
                // hypothetical single blast but a HIT for the line. Placed
                // AFTER Release (matching the Pincer Charge test) so the
                // final probe position is never disturbed by anything
                // between placement and the bomb landing a few frames later.
                Teleport(duel.Enemy, new Vector3(0f, 0f, -10.5f));

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "the Line pattern's outer point reaches past a single blast's radius");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PincerChargeSplitsLaterallyWhenThrownGrounded()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(bomb: Bomb("pincer-charge")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, 0f));
                duel.Player.SetFacing(0f); // forward = +Z
                Teleport(duel.Enemy, new Vector3(0f, 0f, 6f)); // within the 18m target range

                Assert.That(duel.Player.Grounded, Is.True);
                duel.PlayerBomb.StartAim(duel.Enemy);
                yield return null;
                duel.PlayerBomb.UpdateAim(duel.Enemy);
                duel.PlayerBomb.Release(); // captures impact ~ (0,_,6), GroundedAtRelease = true

                // Grounded split axis is PERPENDICULAR to the throw line
                // (forward = +Z), i.e. lateral along +X/-X. Move the probe
                // to one split point (spacing 3.2) — outside a single
                // blast's reach (radius 2.4 + 0.5 buffer = 2.9 < 3.2) but
                // exactly on the Split pattern's own point.
                Teleport(duel.Enemy, new Vector3(3.2f, 0f, 6f));

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "grounded release splits laterally (perpendicular to the throw line)");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PalisadeKeepsItsThrowOrientationEvenIfTheOwnerMovesAfterRelease()
        {
            // Codex PR #18 finding: the original BlastPoints re-derived the
            // throw direction from _owner.Position AT DETONATION, not at
            // release. Release() un-roots the thrower immediately and
            // flight is >=0.5s, so strafing after the throw silently
            // rotated the Line pattern. This test moves Player sideways
            // right after Release() and confirms the Line's outer point
            // STILL lands where the ORIGINAL throw would have put it.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(bomb: Bomb("palisade")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, 0f));
                duel.Player.SetFacing(Mathf.PI); // FacingDir = -Z, clear of Depot's crate at (0,0.8,10)
                Teleport(duel.Enemy, new Vector3(14f, 0f, -14f)); // parked clear during aim/throw
                yield return null;

                duel.PlayerBomb.StartAim(duel.Enemy);
                yield return null;
                duel.PlayerBomb.UpdateAim(duel.Enemy);
                duel.PlayerBomb.Release(); // captures End ~(0,_,-5.5) and ForwardAtRelease = -Z

                // The critical step: move the THROWER sideways right after
                // release, well before the ~0.5s flight lands. If the bug
                // were present, BlastPoints would recompute "forward" from
                // Player's NEW position toward the still-fixed impact
                // point, producing a very different (diagonal) orientation
                // that would miss the probe below.
                Teleport(duel.Player, new Vector3(8f, 0f, 0f));

                // Same probe as the release-time-orientation test: only
                // reachable via the Line pattern's outer point if "forward"
                // stayed -Z (the ORIGINAL throw), not whatever direction
                // Player's new position at (8,0,0) would imply.
                Teleport(duel.Enemy, new Vector3(0f, 0f, -10.5f));

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "the Line pattern must keep the orientation captured at release, not the thrower's post-throw position");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PincerChargeSplitsForeAndAftWhenThrownAirborne()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(bomb: Bomb("pincer-charge")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, new Vector3(0f, 0f, 0f));
                duel.Player.SetFacing(Mathf.PI); // forward = -Z, clear of every Depot crate
                Teleport(duel.Enemy, new Vector3(0f, 0f, -4f)); // within the 18m target range

                // Thrust until airborne -- ARMORY §6: "sides afoot, fore-
                // and-aft aloft," so the split axis must follow the
                // thrower's stance AT RELEASE, not just default to grounded.
                duel.Player.Intent = new RoboIntent { ThrustHeld = true };
                for (var i = 0; i < 60 && duel.Player.Grounded; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Grounded, Is.False, "thrust must lift off before release");
                duel.PlayerBomb.StartAim(duel.Enemy);
                yield return null;
                duel.PlayerBomb.UpdateAim(duel.Enemy);
                duel.PlayerBomb.Release(); // captures GroundedAtRelease = false, impact ~(0,_,-4)

                // Airborne split axis is ALONG the throw line (fore/aft),
                // not perpendicular. The far point sits at -4 + (-3.2) =
                // -7.2 -- clear of every Depot crate, and far enough from
                // the (0,0,-4) impact and the (0,0,0) origin that a LATERAL
                // (grounded-branch) split would clearly miss it too, so
                // this is a genuine fore/aft-vs-lateral discriminator, not
                // just a distance check.
                Teleport(duel.Enemy, new Vector3(0f, 0f, -7.2f));

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "an airborne release splits fore-and-aft (along the throw line)");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        /// Positions `target` at `distance` from `origin`, offset `angleDeg`
        /// from origin's own +Z (world) so a caller who then calls
        /// `origin.SetFacing(0f)` gets a known angle-off-facing.
        private static void PositionAtAngle(RoboAvatar origin, RoboAvatar target, float angleDeg, float distance)
        {
            Teleport(origin, Vector3.zero);
            var rad = angleDeg * Mathf.Deg2Rad;
            Teleport(target, new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance);
        }
    }
}
