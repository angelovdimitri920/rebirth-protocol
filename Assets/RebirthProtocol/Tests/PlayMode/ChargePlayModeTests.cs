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

        // Codex PR #14 finding #2: contact was a flattened (y=0) check, so a
        // ground charge could hit something far overhead. Fixed to a real
        // 3D center-to-center distance (facing stays flat).
        [UnityTest]
        public IEnumerator GroundChargeMissesATargetDirectlyOverhead()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                // Same X/Z, 10 m straight up: horizontal distance is 0 (well
                // within the old flattened check's range) but the real 3D
                // gap is far outside HitRange (2.2 for the straight charge).
                Teleport(duel.Enemy, duel.Player.Position + Vector3.up * 10f);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryCharge(duel.Enemy);
                for (var i = 0; i < 240 && duel.Player.Charge.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore), "directly overhead must not connect");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RisingChargeConnectsOnlyOnceItHasClimbedToAltitude()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                var bulwark = PartsCatalog.DefaultLoadout();
                bulwark.Body = PartsCatalog.Bodies[3]; // Cobalt Knight: Air-kind, Speed 15, RiseSpeed 9
                duel.RespawnWithLoadouts(bulwark, PartsCatalog.DefaultLoadout());
                yield return null;

                // Cobalt's strike travels a straight diagonal ray: Speed
                // (15 m/s) horizontal, RiseSpeed (9 m/s) vertical. Placing
                // the target 4.5 m out and 2.7 m up puts it on that ray at
                // t=0.3s into the 0.5 s strike — reachable only after real
                // altitude gain, not at strike start. The target is a live
                // avatar (gravity acts on it too), so it's re-teleported
                // back to that fixed point every frame — otherwise it just
                // falls out of the intercept before the strike gets there.
                var pinnedTarget = duel.Player.Position + Vector3.right * 4.5f + Vector3.up * 2.7f;
                Teleport(duel.Enemy, pinnedTarget);
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryCharge(duel.Enemy);
                for (var i = 0; i < 60 && duel.Player.Charge.Phase != ChargePhase.Strike; i++)
                {
                    Teleport(duel.Enemy, pinnedTarget);
                    yield return null;
                }

                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Strike));

                // ~0.15 s into the 0.5 s strike (well before the t=0.3s
                // intercept): still nowhere near the target's altitude.
                for (var i = 0; i < 9 && duel.Player.Charge.Busy; i++)
                {
                    Teleport(duel.Enemy, pinnedTarget);
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(hpBefore),
                    "the strike just launched — it hasn't climbed to the target's height yet");

                for (var i = 0; i < 60 && duel.Player.Charge.Busy; i++)
                {
                    Teleport(duel.Enemy, pinnedTarget);
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "once the climb reaches the target's altitude, the strike connects");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 finding #1: EnemyBrain's shield timer didn't know
        // about Charge.Busy, so an AI could raise its shield mid-charge and
        // defeat the doctrine's required vulnerable windows. Fixed
        // centrally in RoboAvatar.TickShield rather than per-brain.
        [UnityTest]
        public IEnumerator ShieldCannotRaiseWhileAChargeIsInFlight()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                var shielded = PartsCatalog.DefaultLoadout();
                shielded.Bomb = null;
                shielded.Shield = PartsCatalog.Shields[0];
                duel.RespawnWithLoadouts(shielded, PartsCatalog.DefaultLoadout());
                yield return null;

                duel.Player.TryCharge(duel.Enemy);
                Assert.That(duel.Player.Charge.Busy, Is.True);

                for (var i = 0; i < 120 && duel.Player.Charge.Busy; i++)
                {
                    duel.Player.Intent = new RoboIntent { ShieldHeld = true };
                    yield return null;
                    Assert.That(duel.Player.ShieldRaised, Is.False,
                        $"shield must not raise mid-charge (phase {duel.Player.Charge.Phase})");
                }
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 finding #3: a same-frame knockdown from a path
        // outside TickMelee/TickCharge (e.g. ApplyLava, a shield-parry
        // endurance drain) left _externalMove set for one more frame,
        // moving/launching a fallen pilot before the next TickCharge poll.
        // Fixed via a Health.KnockedDown subscription that cancels
        // synchronously, wherever knockdown comes from.
        [UnityTest]
        public IEnumerator OutOfBandKnockdownCancelsAnInFlightChargeSynchronously()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                duel.Player.TryCharge(duel.Enemy);
                for (var i = 0; i < 60 && duel.Player.Charge.Phase != ChargePhase.Strike; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Strike));
                var posBefore = duel.Player.Position;

                // Simulate a knockdown arriving from OUTSIDE TickCharge's own
                // poll, exactly ApplyLava's shape: Health.TakeHit called
                // directly, mid-frame, between TickCharge and TickMotor —
                // and bypassing ReceiveHit means it bypasses Intangible too,
                // same as lava does to a mid-strike i-framed charger.
                duel.Player.Health.TakeHit(0f, 9999f);
                Assert.That(duel.Player.Charge.Busy, Is.False,
                    "the KnockedDown event must cancel the charge the instant it fires");

                yield return null; // let TickMotor run once more
                Assert.That(Vector3.Distance(duel.Player.Position, posBefore), Is.LessThan(1f),
                    "a downed pilot must not still be launched by a stale charge move");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 finding #4: TryCharge was called from inside
        // PlayerBrain.Tick, BEFORE DuelManager's TickShield resolved this
        // frame's shield intent — releasing a raised shield and pressing
        // charge on the same frame read the stale (still-raised) state and
        // silently dropped the charge input. Fixed by deferring the actual
        // TryCharge call to DuelManager, right after TickShield.
        [UnityTest]
        public IEnumerator ReleasingARaisedShieldFreesUpAChargeOnTheSameFrame()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                var shielded = PartsCatalog.DefaultLoadout();
                shielded.Bomb = null;
                shielded.Shield = PartsCatalog.Shields[0];
                duel.RespawnWithLoadouts(shielded, PartsCatalog.DefaultLoadout());
                yield return null;

                // Raise the shield and let it settle up.
                duel.Player.Intent = new RoboIntent { ShieldHeld = true };
                for (var i = 0; i < 30 && !duel.Player.ShieldRaised; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.ShieldRaised, Is.True, "setup: shield must actually be up");

                // Same frame: release the shield AND request a charge.
                duel.Player.Intent = new RoboIntent { ShieldHeld = false, ChargeRequested = true };
                yield return null;

                Assert.That(duel.Player.ShieldRaised, Is.False, "the release must land this frame");
                Assert.That(duel.Player.Charge.Busy, Is.True,
                    "the charge request must not be refused on last frame's raised flag");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 second-pass finding #1: a same-frame charge request
        // didn't reserve the frame from other actions -- a gun could still
        // fire (or melee/shield/bomb could still start) on the very frame a
        // charge began, racing the charge's still-deferred TryCharge call.
        // Runs EnemyBrain for real (deterministic, seeded) across many
        // simulated seconds and asserts no gun round ever appears on the
        // frame a charge starts.
        [UnityTest]
        public IEnumerator NoGunRoundFiresOnTheSameFrameAChargeStarts()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                var loadout = PartsCatalog.DefaultLoadout(); // has a gun
                duel.RespawnWithLoadouts(loadout, loadout);
                duel.BrainsEnabled = true; // EnemyBrain must make its own decisions
                yield return null;

                // The per-frame ResetCooldown below (see the loop) forces
                // Enemy's gun into a machine-gun rate to remove per-shot
                // timing as a SECOND layer of randomness on top of the
                // charge roll. At that rate it would otherwise kill Player
                // in well under a second, which permanently blocks
                // EnemyBrain's charge decision (it requires playerAlive) --
                // Player is made unkillable purely so the fight keeps
                // running long enough to observe the actual thing under
                // test. Player's own gun never fires (no input device is
                // connected in batch mode), so this one-sided boost is
                // inert for everything else in the test.
                duel.Player.Health.IncreaseMaxHp(1_000_000f);
                duel.Player.Health.SetHp(1_000_000f);

                // Force the gun to always be cooldown-ready: without this,
                // whether a round actually spawns on any given firing-mode
                // frame is itself a ~1-in-20 shot (Arbalest's own fire
                // interval), which nearly hides the bug behind a SECOND
                // layer of randomness having nothing to do with what this
                // test checks. Resetting the cooldown every frame removes
                // that layer, so "the AI happens to be in a firing burst"
                // becomes the only remaining gate on the coincidence.
                var chargeStarts = 0;
                var wasBusy = false;
                for (var i = 0; i < 3600 && chargeStarts < 10; i++)
                {
                    duel.Enemy.Gun.ResetCooldown();
                    var roundsBefore = duel.Projectiles.CountLiveRounds(duel.Enemy, HitSource.Gun);
                    yield return null;
                    var roundsAfter = duel.Projectiles.CountLiveRounds(duel.Enemy, HitSource.Gun);

                    var isBusy = duel.Enemy.Charge.Busy;
                    if (isBusy && !wasBusy)
                    {
                        chargeStarts++;
                        Assert.That(roundsAfter, Is.LessThanOrEqualTo(roundsBefore),
                            "no new gun round may appear on the frame a charge starts");
                    }

                    wasBusy = isBusy;
                }

                Assert.That(chargeStarts, Is.GreaterThan(0),
                    "setup: the AI must actually charge at least once in this window");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 second-pass finding #2: Atan2(0, 0) resolves to
        // world yaw zero, so a target directly overhead/underfoot passed
        // the facing check only when the charger happened to face north —
        // identical vertical contact, facing-dependent outcome. Facing
        // south is the case that would have missed under the old code.
        [UnityTest]
        public IEnumerator OverheadContactConnectsRegardlessOfFacing()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                // 1 m directly overhead: well within the straight charge's
                // 2.2 m HitRange, same X/Z so the flattened facing vector
                // is exactly zero.
                Teleport(duel.Enemy, duel.Player.Position + Vector3.up * 1f);
                duel.Player.SetFacing(Mathf.PI); // south -- FaceFlatToward can't override this (zero flat offset)
                yield return null;

                var hpBefore = duel.Enemy.Health.Hp;
                duel.Player.TryCharge(duel.Enemy);
                Assert.That(duel.Player.Charge.Busy, Is.True);

                for (var i = 0; i < 240 && duel.Player.Charge.Busy; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Enemy.Health.Hp, Is.LessThan(hpBefore),
                    "directly-overhead contact must connect regardless of which way the charger faces");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        // Codex PR #14 second-pass finding #3: CombatantHealth.TakeHit goes
        // straight to Dead on lethal damage without ever firing
        // KnockedDown, so the Init-time event subscription (which cancels
        // Melee/Charge synchronously) never fires for a killing blow. The
        // gap is specifically a death landing BETWEEN TickCharge and
        // TickMotor in the SAME frame -- exactly ApplyLava's position in
        // DuelManager's tick order -- so this test uses a real lava pool
        // rather than calling Health.TakeHit from the test coroutine
        // itself (which lands safely BETWEEN frames, already caught by the
        // next frame's own TickCharge poll, and doesn't exercise the gap
        // at all). The backstop lives in TickMotor: downed is checked and
        // _externalMove cleared BEFORE the movement branch reads it, in
        // the same call the death happens in.
        [UnityTest]
        public IEnumerator OutOfBandDeathCancelsAnInFlightChargeBeforeItCanMove()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.ForceArenaLayout(3); // Cinderfield: lava pools
                duel.RespawnWithLoadouts(PartsCatalog.DefaultLoadout(), PartsCatalog.DefaultLoadout());
                yield return null;

                duel.Player.TryCharge(duel.Enemy);
                for (var i = 0; i < 60 && duel.Player.Charge.Phase != ChargePhase.Strike; i++)
                {
                    yield return null;
                }

                Assert.That(duel.Player.Charge.Phase, Is.EqualTo(ChargePhase.Strike));

                // Drop Player onto a lava pool's exact center (-8, 3), r=3
                // (ArenaBuilder.cs case 3) with 0.1 hp -- the very next
                // ApplyLava tick (24 hp/s DoT) is guaranteed lethal, and
                // ApplyLava runs after TickCharge but before TickMotor,
                // reproducing the exact ordering Codex's finding describes.
                // SetHp floors at 1 (by design, for run-carryover healing),
                // so TakeHit -- which has no such floor -- gets HP down to
                // the edge instead.
                var criticalFramePos = new Vector3(-8f, 0f, 3f);
                var cc = duel.Player.GetComponent<CharacterController>();
                cc.enabled = false;
                duel.Player.transform.position = criticalFramePos;
                cc.enabled = true;
                duel.Player.Health.TakeHit(duel.Player.Health.Hp - 0.1f, 0f);

                yield return null; // the critical frame: TickCharge (still alive) -> ApplyLava (kills) -> TickMotor
                Debug.Log($"[DIAG] after: grounded={duel.Player.Grounded} hp={duel.Player.Health.Hp} state={duel.Player.Health.State}");

                Assert.That(duel.Player.Health.State, Is.EqualTo(HealthState.Dead), "setup: the lava tick must kill");
                Assert.That(duel.Player.Charge.Busy, Is.False,
                    "TickMotor must cancel the charge the instant it sees the downed state, same frame");
                Assert.That(Vector3.Distance(duel.Player.Position, criticalFramePos), Is.LessThan(0.05f),
                    "a dead pilot must not still take one charge-speed step in the frame that killed it");
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
