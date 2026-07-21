using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass F):
    // a full immobilize distinct from knockdown, applied by six new parts
    // across every hit-source shape (gun, melee, bomb, pod, shield parry),
    // plus the fetter-immunity rule that bounds "Fetter chains" (the named
    // degenerate pattern). Every existing part's FetterSeconds/
    // ParryFetterSeconds/ProximityRange default to 0, so this pass changes
    // no existing behavior — these tests only cover the six new parts and
    // the core immobilize/immunity mechanic.
    public sealed class FetterPlayModeTests
    {
        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("FetterTest");
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

        private static Loadout LoadoutWith(GunPart gun = null, MeleeWeaponPart melee = null,
            BombPart bomb = null, ShieldPart shield = null, PodPart pod = null) => new Loadout
        {
            Body = PartsCatalog.Bodies[0],
            // The right arm is never empty in a real loadout (RoboVisual
            // assumes gun XOR melee) — default to the standard gun when a
            // test only cares about a different slot.
            Gun = melee == null ? gun ?? PartsCatalog.Guns[0] : null,
            Melee = melee,
            // The left arm is never empty either (bomb XOR shield).
            Bomb = shield == null ? bomb ?? PartsCatalog.Bombs[0] : null,
            Shield = shield,
            Legs = PartsCatalog.Legs[0],
            Pod = pod ?? PartsCatalog.Pods[0]
        };

        private static GunPart Gun(string id) => PartsCatalog.Guns.First(g => g.Id == id);
        private static MeleeWeaponPart Melee(string id) => PartsCatalog.MeleeWeapons.First(m => m.Id == id);
        private static BombPart Bomb(string id) => PartsCatalog.Bombs.First(b => b.Id == id);
        private static ShieldPart Shield(string id) => PartsCatalog.Shields.First(s => s.Id == id);
        private static PodPart Pod(string id) => PartsCatalog.Pods.First(p => p.Id == id);

        [UnityTest]
        public IEnumerator FetterlockAppliesFetterOnGunHit()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("fetterlock")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3f));
                yield return null;

                Assert.That(duel.Enemy.Fetter.IsFettered, Is.False);
                duel.Player.TickGun(1f / 60f, firing: true, duel.Enemy);

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 90; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the round must actually connect");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True, "Fetterlock's shackle-round carries Fetter");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator KnellMaulAppliesFetterOnMeleeHit()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("knell-maul")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f)); // within CloseRange: swings immediately, no lunge
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryMelee(duel.Enemy);
                for (var i = 0; i < 120 && duel.Player.Melee.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the swing must actually connect");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True, "Knell Maul tolls Fetter on every connecting hit");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RimeChargeAppliesFetterOnBombHit()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(bomb: Bomb("rime-charge")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 6f)); // within Rime Charge's 18m target range

                Assert.That(duel.Player.Grounded, Is.True);
                duel.PlayerBomb.StartAim(duel.Enemy);
                yield return null;
                duel.PlayerBomb.UpdateAim(duel.Enemy);
                duel.PlayerBomb.Release();

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the blast must actually connect");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True, "Rime Charge's real payload is the Fetter hold");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator WinterwatchFettersATargetWithinProximityRange()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(pod: Pod("winterwatch")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.PlayerPod.Toggle(); // deploy at Player's position
                yield return null;

                Teleport(duel.Enemy, new Vector3(3f, 0f, 0f)); // inside Winterwatch's tight 6m ProximityRange

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 240; i++)
                {
                    yield return null;
                    if (duel.Enemy.Health.Hp < hpBefore)
                    {
                        break;
                    }
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore), "the proximity shot must actually connect");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True, "\"fetters whoever comes near\"");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator WinterwatchStaysSilentBeyondItsProximityRange()
        {
            // Regression guard for the range-tightening itself: a target well
            // inside the NORMAL 22m pod FireRange but outside Winterwatch's
            // 6m ProximityRange must draw no fire at all — "patient" is a
            // real gate, not just flavor text on the same old range.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(pod: Pod("winterwatch")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.PlayerPod.Toggle();
                yield return null;

                Teleport(duel.Enemy, new Vector3(12f, 0f, 0f)); // inside 22m FireRange, outside 6m ProximityRange

                var hpBefore = duel.Enemy.Health.Hp;
                for (var i = 0; i < 180; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore), "12m is outside the tightened proximity range");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.False);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoarfrostWardFettersTheParriedMeleeAttacker()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(melee: Melee("saber")), LoadoutWith(shield: Shield("hoarfrost-ward")));
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 2f)); // within CloseRange
                duel.Enemy.Intent = new RoboIntent { HasFaceYaw = true, FaceYaw = Mathf.PI }; // face the attacker
                yield return null;
                duel.Enemy.Intent.ShieldHeld = true;
                yield return null; // TickShield raises it before this frame's melee resolves

                Assert.That(duel.Enemy.ShieldRaised, Is.True, "the parry needs a raised shield to punish against");
                Assert.That(duel.Player.Fetter.IsFettered, Is.False);
                var enduranceBefore = duel.Player.Health.Endurance;
                duel.Player.TryMelee(duel.Enemy);
                // Check right when the parry lands, not after the swing's
                // full recovery -- ParryFetterSeconds (0.6s) is deliberately
                // shorter than Oathblade's own swing+recovery cycle (~0.63s),
                // so waiting for Melee.Busy to clear would race the fetter's
                // own expiry.
                for (var i = 0; i < 120 && duel.Player.Health.Endurance == enduranceBefore; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Health.Endurance, Is.LessThan(enduranceBefore), "the parry must actually land");
                Assert.That(duel.Player.Fetter.IsFettered, Is.True,
                    "\"melee against it leaves the attacker rimed and slowed\" — the ATTACKER, not the shield-bearer");
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.False, "the shield-bearer itself is untouched");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FetteredRoboCannotMoveActOrRaiseShield()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), LoadoutWith(shield: Shield("kite-ward")));
                yield return null;

                var enemyStart = duel.Enemy.Position;
                duel.Enemy.ApplyFetter(1f);
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True);
                Assert.That(duel.Enemy.ControlLocked, Is.True);

                // Drive input for several frames: none of it should move the
                // avatar, fire its gun, or raise its shield while fettered.
                duel.Enemy.Intent = new RoboIntent { MoveDir = Vector3.forward, ShieldHeld = true };
                for (var i = 0; i < 20; i++)
                {
                    yield return null;
                    duel.Enemy.TickGun(1f / 60f, firing: true, duel.Player);
                }

                Assert.That(Vector3.Distance(duel.Enemy.Position, enemyStart), Is.LessThan(0.05f),
                    "immobilize means no drift from held movement input either");
                Assert.That(duel.Enemy.ShieldRaised, Is.False, "immobilize forces the shield down too");
                Assert.That(duel.Projectiles.CountLiveRounds(duel.Enemy, HitSource.Gun), Is.Zero,
                    "TickGun's own ControlLocked gate must have refused every fire attempt");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FetterEndingGrantsImmunityToAnImmediateRefetter()
        {
            // DOCTRINE §13 pillar 9's whole point: back-to-back Fetterlock
            // hits must not chain into a permalock. Two shots timed so the
            // second lands right as the first's hold would otherwise still
            // be running must NOT extend/refresh the fetter.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(gun: Gun("fetterlock")), PartsCatalog.DefaultLoadout());
                yield return null;

                Teleport(duel.Player, Vector3.zero);
                duel.Player.SetFacing(0f);
                Teleport(duel.Enemy, new Vector3(0f, 0f, 3f));
                yield return null;

                duel.Enemy.ApplyFetter(1f); // simulate the first shot's payload landing directly
                Assert.That(duel.Enemy.Fetter.IsFettered, Is.True);

                // Mid-fetter: a second application must be rejected outright,
                // not queued or refreshed.
                duel.Enemy.ApplyFetter(5f);
                for (var i = 0; i < 200 && duel.Enemy.Fetter.Phase != FetterPhase.Free; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Fetter.Phase, Is.EqualTo(FetterPhase.Free),
                    "the original ~1s fetter (plus its 2s immunity window) must have fully expired, not been extended to 5s+");
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
