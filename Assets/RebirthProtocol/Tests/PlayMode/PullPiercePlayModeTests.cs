using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Pull & piercing capability (ARMORY_REFERENCE §4-5, Pass G): five new
    // parts across two new mechanics. Pulls (Grapnel gun, Hookbill/Sawtooth
    // Espadon melee, Auger gun) haul the victim TOWARD the attacker instead
    // of shoving away; guard-piercing (Estoc melee) ignores a fraction of a
    // raised shield's block. Every existing part's PullSpeed/GuardPierce
    // defaults to 0, so this pass changes no existing behavior — these tests
    // cover only the new parts and the two new mechanics.
    public sealed class PullPiercePlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("PullPierceTest");
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

        private static Loadout LoadoutWith(GunPart gun = null, MeleeWeaponPart melee = null,
            BombPart bomb = null, ShieldPart shield = null) => new Loadout
        {
            Body = PartsCatalog.Bodies[0],
            Gun = melee == null ? gun ?? PartsCatalog.Guns[0] : null,
            Melee = melee,
            Bomb = shield == null ? bomb ?? PartsCatalog.Bombs[0] : null,
            Shield = shield,
            Legs = PartsCatalog.Legs[0],
            Pod = PartsCatalog.Pods[0]
        };

        private static GunPart Gun(string id) => PartsCatalog.Guns.First(g => g.Id == id);
        private static MeleeWeaponPart Melee(string id) => PartsCatalog.MeleeWeapons.First(m => m.Id == id);
        private static ShieldPart Shield(string id) => PartsCatalog.Shields.First(s => s.Id == id);

        [UnityTest]
        public IEnumerator GrapnelHaulsTheVictimTowardTheShooter()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("grapnel")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 6f)); // stationary target, +Z of the shooter
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the barbed line must actually connect");

                // Let the haul play out (knockback decays over a handful of
                // frames) before measuring.
                for (var i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.FlatDistanceTo(duel.Enemy), Is.LessThan(startDist - 0.5f),
                    "Grapnel hauls the target toward the shooter, not away — the gap must shrink");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HookbillHaulsTheVictimTowardTheWielder()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("hookbill")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                // Within CloseRange (swings immediately, no gap-closing lunge
                // that would itself move the attacker) and within Hookbill's
                // 3.4 HitRange (connects).
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the billhook must actually connect");

                for (var i = 0; i < 20; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.FlatDistanceTo(duel.Enemy), Is.LessThan(startDist - 0.5f),
                    "Hookbill drags the foe in — the attacker stays put and the gap shrinks");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OathbladeShovesTheVictimAway_ThePullControl()
        {
            // The other side of the pull mechanic: an ordinary blade (no
            // PullSpeed) still knocks the target AWAY, so the Hookbill test
            // above is measuring a real reversal, not just any displacement.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("saber")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the swing must connect");

                for (var i = 0; i < 20; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.FlatDistanceTo(duel.Enemy), Is.GreaterThan(startDist + 0.1f),
                    "Oathblade shoves away — the gap must GROW, the opposite of a pull weapon");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GrapnelDoesNotHaulAGuardedTarget()
        {
            // Contract (Codex PR #22 finding): a raised shield defeats the
            // grab — a pull the guard intercepts hauls nothing, matching how
            // the ordinary hit-flinch is itself suppressed by a block.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("grapnel")), LoadoutWith(shield: Shield("kite-ward")));
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 6f));
                duel.Enemy.Intent = new RoboIntent { HasFaceYaw = true, FaceYaw = Mathf.PI };
                yield return null;
                duel.Enemy.Intent.ShieldHeld = true;
                yield return null; // TickShield raises the guard

                Assert.That(duel.Enemy.ShieldRaised, Is.True, "the guard must be up for the shot to be intercepted");
                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break; // chip landed on the guard
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the shot must connect on the guard (chip)");

                for (var i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.FlatDistanceTo(duel.Enemy), Is.GreaterThan(startDist - 0.3f),
                    "a raised guard defeats the grapnel's haul — the guarded target is not dragged in");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HookbillDoesNotHaulAGuardedTarget()
        {
            // The melee half of the same contract: a pull blade whose swing
            // the shield intercepts hauls nothing (the parry still punishes
            // the attacker). This is the deliberate divergence from a shove,
            // which still knocks a blocker back as it always has.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("hookbill")), LoadoutWith(shield: Shield("kite-ward")));
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3f));
                duel.Enemy.Intent = new RoboIntent { HasFaceYaw = true, FaceYaw = Mathf.PI };
                yield return null;
                duel.Enemy.Intent.ShieldHeld = true;
                yield return null; // TickShield raises the guard before the swing resolves

                Assert.That(duel.Enemy.ShieldRaised, Is.True);
                var startDist = duel.Player.FlatDistanceTo(duel.Enemy);
                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the swing must connect on the guard (chip)");

                for (var i = 0; i < 20; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.FlatDistanceTo(duel.Enemy), Is.GreaterThan(startDist - 0.3f),
                    "a raised guard defeats Hookbill's haul — the guarded target is not dragged in");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EstocPiercesMostOfARaisedGuard()
        {
            // Estoc's identity: "pierces 60% of a raised shield's GUARD." A
            // raised Kite Ward blocks 80% up front, so an unpierced hit lands
            // 20% of its damage as chip. Estoc's 60% pierce turns the guard
            // into an effective 32%, letting 68% through — comfortably above
            // half the unblocked damage. Measured as a fraction of Estoc's
            // OWN full unblocked hit so the assertion survives damage tuning.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                // Run 1: Estoc against a shieldless enemy — its full damage.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("estoc")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var unblockedBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var fullDamage = unblockedBefore - duel.Enemy.Health.Hp;
                Assert.That(fullDamage, Is.GreaterThan(0f), "the unblocked control hit must land");

                // Run 2: Estoc against a raised Kite Ward.
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("estoc")), LoadoutWith(shield: Shield("kite-ward")));
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f));
                duel.Enemy.Intent = new RoboIntent { HasFaceYaw = true, FaceYaw = Mathf.PI };
                yield return null;
                duel.Enemy.Intent.ShieldHeld = true;
                yield return null; // TickShield raises the guard before the swing resolves

                Assert.That(duel.Enemy.ShieldRaised, Is.True, "the guard must be up for the pierce to bite");
                var blockedBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var chip = blockedBefore - duel.Enemy.Health.Hp;
                Assert.That(chip, Is.GreaterThan(fullDamage * 0.5f),
                    "a 60% pierce lets well over half the hit through a raised guard (unpierced would be ~20%)");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OathbladeIsStoppedByTheSameRaisedGuard_ThePierceControl()
        {
            // The pierce control: an ordinary blade against the identical
            // raised Kite Ward lands only its ~20% chip — proving Estoc's
            // heavy chip is the pierce at work, not just melee-through-shield.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("saber")), PartsCatalog.DefaultLoadout());
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f));
                duel.Enemy.SetFacing(Mathf.PI);
                yield return null;

                var unblockedBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var fullDamage = unblockedBefore - duel.Enemy.Health.Hp;

                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("saber")), LoadoutWith(shield: Shield("kite-ward")));
                yield return null;
                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f));
                duel.Enemy.Intent = new RoboIntent { HasFaceYaw = true, FaceYaw = Mathf.PI };
                yield return null;
                duel.Enemy.Intent.ShieldHeld = true;
                yield return null;

                Assert.That(duel.Enemy.ShieldRaised, Is.True);
                var blockedBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                var chip = blockedBefore - duel.Enemy.Health.Hp;
                Assert.That(chip, Is.LessThan(fullDamage * 0.35f),
                    "an unpierced 80% guard lets only ~20% through — well under a third of the full hit");
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
