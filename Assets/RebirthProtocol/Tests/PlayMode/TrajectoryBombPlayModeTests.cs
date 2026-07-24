using System.Collections;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Battle;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    // Trajectory bombs (ARMORY §6, Pass I2): the BombSystem half of Task I —
    // Steeplefall's extreme arc, Oxbow Charge's bow, and the two dwelling
    // Oubliettes. Every bomb before this pass is BombPath.Lob with
    // DwellSeconds 0, ContactRadius 0, MineCount 1, so these tests only
    // exercise the new parts (plus a Censer control in each shape test).
    //
    // Every test here asserts the SHAPE or the LIFECYCLE, never just "the
    // target took damage" — a plain lob eventually damages anything you can
    // aim at, so an outcome-only test would pass with none of this pass's
    // code present.
    public sealed class TrajectoryBombPlayModeTests
    {
        // A clean lane through the Depot crate field (ArenaBuilder.cs crates:
        // (±5,±5), (-7,6), (7,-5), (0,10), (-2,-11)): the thrower stands deep
        // at -Z and throws up the z-axis, so neither the flight, the bow's
        // widest point at (5,_,-8), nor any probe position sits inside a crate.
        private static readonly Vector3 ThrowerPos = new Vector3(0f, 0f, -14f);
        private static readonly Vector3 MarkPos = new Vector3(0f, 0f, -2f);
        private static readonly Vector3 ParkedFarAway = new Vector3(13f, 0f, 13f);

        private static DuelManager BootDuel(out GameObject go)
        {
            go = new GameObject("TrajectoryBombTest");
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

        private static Loadout LoadoutWith(BombPart bomb) => new Loadout
        {
            Body = PartsCatalog.Bodies[0],
            Gun = PartsCatalog.Guns[0],
            Bomb = bomb,
            Legs = PartsCatalog.Legs[0],
            Pod = PartsCatalog.Pods[0]
        };

        private static BombPart Bomb(string id) => PartsCatalog.Bombs.First(b => b.Id == id);

        /// Ground-plane distance from `pos` to the reticule mark every throw
        /// in this fixture is aimed at.
        private static float HorizontalDistanceToMark(Vector3 pos) =>
            Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(MarkPos.x, MarkPos.z));

        /// Stands the thrower in the clean lane, aims at the mark (the enemy
        /// stands on it so the Target-anchored reticule settles there), throws,
        /// and immediately parks the enemy far away so nothing perturbs the
        /// flight. Returns with exactly the thrown bomb(s) in the air.
        private static IEnumerator ThrowAtTheMark(DuelManager duel)
        {
            Teleport(duel.Player, ThrowerPos);
            duel.Player.SetFacing(0f); // FacingDir = +Z, up the lane
            Teleport(duel.Enemy, MarkPos);
            yield return null;

            Assert.That(duel.Player.Grounded, Is.True);
            duel.PlayerBomb.StartAim(duel.Enemy);
            yield return null;
            duel.PlayerBomb.UpdateAim(duel.Enemy);
            duel.PlayerBomb.Release();

            // Out of every blast's and every trigger's reach, so the enemy is
            // a probe the test places deliberately, never an accident.
            Teleport(duel.Enemy, ParkedFarAway);
        }

        [UnityTest]
        public IEnumerator SteeplefallClimbsFarHigherThanAPlainLob()
        {
            // "Climbs past steeple height." Measured against Censer's own
            // apex in the same throw, so this is a shape comparison and not a
            // magic number: the steeple must tower over the roster's lob.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("impact")), PartsCatalog.DefaultLoadout());
                yield return null;
                var lobApex = 0f;
                yield return ThrowAtTheMark(duel);
                while (duel.PlayerBomb.LiveBombCount > 0)
                {
                    lobApex = Mathf.Max(lobApex, duel.PlayerBomb.LiveBombPosition(0).y);
                    yield return null;
                }

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("steeplefall")), PartsCatalog.DefaultLoadout());
                yield return null;
                var steepleApex = 0f;
                yield return ThrowAtTheMark(duel);
                while (duel.PlayerBomb.LiveBombCount > 0)
                {
                    steepleApex = Mathf.Max(steepleApex, duel.PlayerBomb.LiveBombPosition(0).y);
                    yield return null;
                }

                Assert.That(lobApex, Is.LessThan(7f), "the baseline lob tops out around its 5m arc height");
                Assert.That(steepleApex, Is.GreaterThan(12f), "Steeplefall must climb past steeple height");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SteeplefallFinishesItsGroundTravelBeforeItFalls()
        {
            // "Falls straight down" is the half of the blurb a tall arc alone
            // cannot deliver: a symmetric lob still has HALF its ground travel
            // left at the apex and drifts in sideways. This measures the worst
            // (largest) horizontal distance still remaining at any point after
            // the bomb starts descending — near zero for a true vertical drop,
            // metres for any symmetric arc, including a very tall one.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("impact")), PartsCatalog.DefaultLoadout());
                yield return null;
                var lobDrift = 0f;
                yield return ThrowAtTheMark(duel);
                yield return MeasureDescentDrift(duel, d => lobDrift = d);

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("steeplefall")), PartsCatalog.DefaultLoadout());
                yield return null;
                var steepleDrift = 0f;
                yield return ThrowAtTheMark(duel);
                yield return MeasureDescentDrift(duel, d => steepleDrift = d);

                Assert.That(lobDrift, Is.GreaterThan(3f),
                    "a symmetric lob still has ground to cover while it descends");
                Assert.That(steepleDrift, Is.LessThan(1f),
                    "Steeplefall must be over the mark before it starts down, then fall straight");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        /// Flies the live bomb out, reporting the greatest horizontal distance
        /// it still had to travel at any frame after it began descending.
        private static IEnumerator MeasureDescentDrift(DuelManager duel, System.Action<float> report)
        {
            var apex = float.NegativeInfinity;
            var worst = 0f;
            var mark = new Vector2(MarkPos.x, MarkPos.z);
            while (duel.PlayerBomb.LiveBombCount > 0)
            {
                var pos = duel.PlayerBomb.LiveBombPosition(0);
                apex = Mathf.Max(apex, pos.y);
                // Descending, and clearly so: the 0.5m margin keeps a frame or
                // two of near-apex float from counting as the fall.
                if (pos.y < apex - 0.5f)
                {
                    worst = Mathf.Max(worst, Vector2.Distance(new Vector2(pos.x, pos.z), mark));
                }

                yield return null;
            }

            report(worst);
        }

        [UnityTest]
        public IEnumerator OxbowBowsWideToTheDexterFlankAndStillLandsOnTheMark()
        {
            // The route is the weapon, so the route is what gets asserted:
            // the bomb must leave the straight throw line by several metres,
            // do it on the NAMED (dexter/right) side rather than either side
            // at random, and still converge on the reticule mark — a bomb
            // that landed somewhere else would make the reticule a liar.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("impact")), PartsCatalog.DefaultLoadout());
                yield return null;
                var lobSwing = 0f;
                yield return ThrowAtTheMark(duel);
                yield return MeasureSignedSwing(duel, s => lobSwing = s);

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oxbow-charge")), PartsCatalog.DefaultLoadout());
                yield return null;
                var bowSwing = 0f;
                var landing = Vector3.zero;
                yield return ThrowAtTheMark(duel);
                yield return MeasureSignedSwing(duel, s => bowSwing = s, p => landing = p);

                Assert.That(Mathf.Abs(lobSwing), Is.LessThan(0.5f), "a plain lob flies the straight line");
                // Throw runs up +Z, so the thrower's right (dexter) is +X.
                Assert.That(bowSwing, Is.GreaterThan(3.5f),
                    "Oxbow must bow several metres wide, and to the dexter (+X) side");
                Assert.That(HorizontalDistanceToMark(landing), Is.LessThan(1f),
                    "and the bow must still converge on the reticule mark");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        /// Flies the live bomb out, reporting its greatest SIGNED deviation
        /// from the straight thrower→mark line (+X here, i.e. the thrower's
        /// right/dexter) and its last observed position before detonation.
        private static IEnumerator MeasureSignedSwing(DuelManager duel, System.Action<float> report,
            System.Action<Vector3> reportLanding = null)
        {
            var widest = 0f;
            var last = Vector3.zero;
            while (duel.PlayerBomb.LiveBombCount > 0)
            {
                last = duel.PlayerBomb.LiveBombPosition(0);
                if (Mathf.Abs(last.x) > Mathf.Abs(widest))
                {
                    widest = last.x; // the throw line is x = 0, so x IS the deviation
                }

                yield return null;
            }

            report(widest);
            reportLanding?.Invoke(last);
        }

        [UnityTest]
        public IEnumerator OxbowSweepsUpAFoeOnTheNamedFlankThatAPlainLobFliesPast()
        {
            // What the bow BUYS. The probe stands well off the throw line, at
            // the widest point of the dexter bow and far outside any blast
            // that lands on the mark — so the plain lob sails past and blows
            // harmlessly at the mark, while Oxbow meets the foe on the way.
            var probe = new Vector3(5f, 0f, -8f); // the bow's widest point, mid-lane
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("impact")), PartsCatalog.DefaultLoadout());
                yield return null;
                var lobDamage = 0f;
                yield return ThrowAtTheMark(duel);
                Teleport(duel.Enemy, probe);
                yield return MeasureDamageOverFlight(duel, d => lobDamage = d);

                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oxbow-charge")), PartsCatalog.DefaultLoadout());
                yield return null;
                var bowDamage = 0f;
                yield return ThrowAtTheMark(duel);
                Teleport(duel.Enemy, probe);
                yield return MeasureDamageOverFlight(duel, d => bowDamage = d);

                Assert.That(lobDamage, Is.EqualTo(0f),
                    "a plain lob blows on the mark, nowhere near a foe 5m off the throw line");
                Assert.That(bowDamage, Is.GreaterThan(0f),
                    "Oxbow's bow must sweep up the foe standing on the named flank");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        private static IEnumerator MeasureDamageOverFlight(DuelManager duel, System.Action<float> report,
            int windowFrames = 300)
        {
            var hpBefore = duel.Enemy.Health.Hp;
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

        [UnityTest]
        public IEnumerator OublietteMineLandsAndWaitsInsteadOfDetonating()
        {
            // The lifecycle a plain bomb has never had: fly → land → DWELL.
            // A bomb that still detonated on landing would leave nothing live
            // to inspect, so LiveBombCount alone catches the regression.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oubliette-mine")), PartsCatalog.DefaultLoadout());
                yield return null;

                yield return ThrowAtTheMark(duel);
                for (var i = 0; i < 180; i++) // 3s: several times the ~0.7s flight
                {
                    yield return null;
                }

                Assert.That(duel.PlayerBomb.LiveBombCount, Is.EqualTo(1),
                    "the mine must still exist seconds after it should have landed");
                Assert.That(duel.PlayerBomb.LiveBombDwelling(0), Is.True, "and it must be dwelling, not still flying");
                var restingAt = duel.PlayerBomb.LiveBombPosition(0);
                Assert.That(restingAt.y, Is.LessThan(0.5f), "a dwelling mine sits on the floor");
                Assert.That(HorizontalDistanceToMark(restingAt), Is.LessThan(0.5f),
                    "and it sits on the mark it was thrown at");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OublietteMineDetonatesWhenSomeoneStraysOntoIt()
        {
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oubliette-mine")), PartsCatalog.DefaultLoadout());
                yield return null;

                yield return ThrowAtTheMark(duel);
                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                }

                Assert.That(duel.PlayerBomb.LiveBombDwelling(0), Is.True, "the mine must be dwelling before the probe arrives");
                Assert.That(duel.Enemy.Health.Hp, Is.EqualTo(duel.Enemy.Health.MaxHp),
                    "and must not have touched anyone while it waited");

                // Walk onto it: the trigger, not the timer.
                Teleport(duel.Enemy, MarkPos);
                var damage = 0f;
                yield return MeasureDamageOverFlight(duel, d => damage = d, windowFrames: 60);

                Assert.That(damage, Is.GreaterThan(0f), "the mine must go off under the foot that found it");
                Assert.That(duel.PlayerBomb.LiveBombCount, Is.Zero, "and it is spent once it goes off");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OublietteMineEventuallyGoesOffOnItsOwn()
        {
            // "It remembers": patience runs out, the pit still blows. A mine
            // that quietly expired would be a dead throw whenever the foe
            // simply walked around it.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oubliette-mine")), PartsCatalog.DefaultLoadout());
                yield return null;

                yield return ThrowAtTheMark(duel);
                for (var i = 0; i < 300; i++) // 5s in: well past landing, inside the 8s wait
                {
                    yield return null;
                }

                Assert.That(duel.PlayerBomb.LiveBombCount, Is.EqualTo(1), "the mine holds its wait");
                Assert.That(duel.PlayerBomb.LiveBombDwelling(0), Is.True);

                for (var i = 0; i < 300 && duel.PlayerBomb.LiveBombCount > 0; i++) // out to 10s
                {
                    yield return null;
                }

                Assert.That(duel.PlayerBomb.LiveBombCount, Is.Zero,
                    "and once the wait runs out it goes off rather than expiring quietly");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.Destroy(go);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OublietteTwinPlantsTwoPitsThatTriggerIndependently()
        {
            // The discriminator against BlastPattern.Split (Pincer Charge),
            // which also puts two blasts on the ground: Split's two points go
            // off at the SAME INSTANT off one impact. Twin's two pits are
            // separate bombs with separate waits — setting one off must leave
            // the other sitting there.
            var duel = BootDuel(out var go);
            try
            {
                yield return null;
                Time.captureDeltaTime = 1f / 60f;
                duel.RespawnWithLoadouts(LoadoutWith(Bomb("oubliette-twin")), PartsCatalog.DefaultLoadout());
                yield return null;

                yield return ThrowAtTheMark(duel);
                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                }

                Assert.That(duel.PlayerBomb.LiveBombCount, Is.EqualTo(2), "one throw, two pits");
                Assert.That(duel.PlayerBomb.LiveBombDwelling(0) && duel.PlayerBomb.LiveBombDwelling(1), Is.True,
                    "both dwelling");
                var first = duel.PlayerBomb.LiveBombPosition(0);
                var second = duel.PlayerBomb.LiveBombPosition(1);
                Assert.That(Vector3.Distance(first, second), Is.GreaterThan(2f),
                    "the pits are planted apart, not stacked on one point");

                // Step on exactly one of them.
                Teleport(duel.Enemy, new Vector3(first.x, 0f, first.z));
                var damage = 0f;
                yield return MeasureDamageOverFlight(duel, d => damage = d, windowFrames: 60);

                Assert.That(damage, Is.GreaterThan(0f), "the pit that was stepped on goes off");
                Assert.That(duel.PlayerBomb.LiveBombCount, Is.EqualTo(1),
                    "and the OTHER pit is still waiting — two independent mines, not one two-point blast");
                Assert.That(duel.PlayerBomb.LiveBombDwelling(0), Is.True);
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
