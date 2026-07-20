using System.Collections.Generic;
using NUnit.Framework;
using RebirthProtocol.Domain;

namespace RebirthProtocol.Tests.EditMode
{
    // The harness's own flagging logic (COMBAT_DOCTRINE §13): a balance CI
    // that fails to flag an out-of-band pairing is worse than no CI, so
    // every band edge gets a test on both sides.
    public sealed class BalanceStatsTests
    {
        private static readonly BalanceBands Bands = new BalanceBands();

        private static FightRecord Fight(FightOutcome outcome, float duration, int kdA, int kdB)
            => new FightRecord { Outcome = outcome, DurationSeconds = duration, KnockdownsA = kdA, KnockdownsB = kdB };

        /// n fights of an in-band shape: alternating wins, 90 s, 3 knockdowns.
        private static List<FightRecord> InBandFights(int n)
        {
            var list = new List<FightRecord>();
            for (var i = 0; i < n; i++)
            {
                list.Add(Fight(i % 2 == 0 ? FightOutcome.WinA : FightOutcome.WinB, 90f, 2, 1));
            }

            return list;
        }

        [Test]
        public void InBandPairingCarriesNoFlags()
        {
            var m = BalanceStats.Aggregate("a", "b", InBandFights(20), Bands);

            Assert.That(m.WinRateA, Is.EqualTo(0.5f));
            Assert.That(m.MeanTtkSeconds, Is.EqualTo(90f));
            Assert.That(m.MeanKnockdowns, Is.EqualTo(3f));
            Assert.That(m.Flags, Is.Empty);
        }

        [Test]
        public void LopsidedWinRateFlagsBothDirections()
        {
            var high = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                high.Add(Fight(i < 7 ? FightOutcome.WinA : FightOutcome.WinB, 90f, 2, 1));
            }

            var m = BalanceStats.Aggregate("a", "b", high, Bands);
            Assert.That(m.WinRateA, Is.EqualTo(0.7f));
            Assert.That(m.Flags, Has.Some.Contains("WINRATE"));

            var low = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                low.Add(Fight(i < 7 ? FightOutcome.WinB : FightOutcome.WinA, 90f, 2, 1));
            }

            m = BalanceStats.Aggregate("a", "b", low, Bands);
            Assert.That(m.WinRateA, Is.EqualTo(0.3f));
            Assert.That(m.Flags, Has.Some.Contains("WINRATE"));
        }

        [Test]
        public void WinRateAtTheBandEdgesDoesNotFlag()
        {
            // Exactly 40% and exactly 60% are IN band ("outside 40–60%").
            var forty = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                forty.Add(Fight(i < 4 ? FightOutcome.WinA : FightOutcome.WinB, 90f, 2, 1));
            }

            Assert.That(BalanceStats.Aggregate("a", "b", forty, Bands).Flags, Is.Empty);

            var sixty = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                sixty.Add(Fight(i < 6 ? FightOutcome.WinA : FightOutcome.WinB, 90f, 2, 1));
            }

            Assert.That(BalanceStats.Aggregate("a", "b", sixty, Bands).Flags, Is.Empty);
        }

        [Test]
        public void PacingBandFlagsFastAndSlowFights()
        {
            var fast = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 30f, 2, 1), Fight(FightOutcome.WinB, 40f, 1, 2) },
                Bands);
            Assert.That(fast.Flags, Has.Some.Contains("TTK"));

            var slow = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 150f, 2, 1), Fight(FightOutcome.WinB, 130f, 1, 2) },
                Bands);
            Assert.That(slow.Flags, Has.Some.Contains("TTK"));
        }

        [Test]
        public void KnockdownBandFlagsTooFewAndTooMany()
        {
            var few = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 90f, 1, 0), Fight(FightOutcome.WinB, 90f, 0, 1) },
                Bands);
            Assert.That(few.Flags, Has.Some.Contains("KNOCKDOWNS"));

            var many = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 90f, 4, 3), Fight(FightOutcome.WinB, 90f, 3, 4) },
                Bands);
            Assert.That(many.Flags, Has.Some.Contains("KNOCKDOWNS"));
        }

        [Test]
        public void TtkAtTheBandEdgesDoesNotFlag()
        {
            // Exactly 60s and exactly 120s are IN band ("outside 60–120s").
            var sixty = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 60f, 2, 1), Fight(FightOutcome.WinB, 60f, 1, 2) },
                Bands);
            Assert.That(sixty.Flags, Has.None.Contains("TTK"));

            var oneTwenty = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 120f, 2, 1), Fight(FightOutcome.WinB, 120f, 1, 2) },
                Bands);
            Assert.That(oneTwenty.Flags, Has.None.Contains("TTK"));
        }

        [Test]
        public void KnockdownsAtTheBandEdgesDoesNotFlag()
        {
            // Exactly 2.0 and exactly 5.0 mean knockdowns are IN band
            // ("outside 2–5"). Combined per-fight KD total must average to
            // the edge exactly: 2 fights summing to 4 (mean 2) and to 10
            // (mean 5).
            var low = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 90f, 1, 1), Fight(FightOutcome.WinB, 90f, 1, 1) },
                Bands);
            Assert.That(low.MeanKnockdowns, Is.EqualTo(2f));
            Assert.That(low.Flags, Has.None.Contains("KNOCKDOWNS"));

            var high = BalanceStats.Aggregate("a", "b",
                new List<FightRecord> { Fight(FightOutcome.WinA, 90f, 3, 2), Fight(FightOutcome.WinB, 90f, 3, 2) },
                Bands);
            Assert.That(high.MeanKnockdowns, Is.EqualTo(5f));
            Assert.That(high.Flags, Has.None.Contains("KNOCKDOWNS"));
        }

        [Test]
        public void RemapSlotResultPassesThroughWhenRolesNotSwapped()
        {
            var r = BalanceStats.RemapSlotResult(seed: 7, arenaIndex: 2, rolesSwapped: false,
                slotOutcome: FightOutcome.WinA, durationSeconds: 42f,
                knockdownsSlotA: 3, knockdownsSlotB: 1, endHpSlotA: 500f, endHpSlotB: 0f);

            Assert.That(r.Seed, Is.EqualTo(7));
            Assert.That(r.ArenaIndex, Is.EqualTo(2));
            Assert.That(r.SidesSwapped, Is.False);
            Assert.That(r.Outcome, Is.EqualTo(FightOutcome.WinA));
            Assert.That(r.DurationSeconds, Is.EqualTo(42f));
            Assert.That(r.KnockdownsA, Is.EqualTo(3));
            Assert.That(r.KnockdownsB, Is.EqualTo(1));
            Assert.That(r.EndHpA, Is.EqualTo(500f));
            Assert.That(r.EndHpB, Is.EqualTo(0f));
        }

        [Test]
        public void RemapSlotResultInvertsOutcomeAndSwapsKdHpWhenRolesSwapped()
        {
            // Slot A won (WinA) but slot A was occupied by build j this
            // fight (rolesSwapped) — so the remapped record must credit
            // build i (still labeled "A" in the record) with a LOSS, and
            // every per-slot stat must swap sides with it.
            var r = BalanceStats.RemapSlotResult(seed: 9, arenaIndex: 0, rolesSwapped: true,
                slotOutcome: FightOutcome.WinA, durationSeconds: 55f,
                knockdownsSlotA: 3, knockdownsSlotB: 1, endHpSlotA: 500f, endHpSlotB: 0f);

            Assert.That(r.SidesSwapped, Is.True);
            Assert.That(r.Outcome, Is.EqualTo(FightOutcome.WinB), "slot A's win belongs to build j, i.e. build i (recorded as A) lost");
            Assert.That(r.KnockdownsA, Is.EqualTo(1), "slot B's knockdowns become build i's (A)");
            Assert.That(r.KnockdownsB, Is.EqualTo(3));
            Assert.That(r.EndHpA, Is.EqualTo(0f));
            Assert.That(r.EndHpB, Is.EqualTo(500f));
        }

        [Test]
        public void RemapSlotResultInvertsWinBToWinAWhenSwapped()
        {
            var r = BalanceStats.RemapSlotResult(seed: 1, arenaIndex: 0, rolesSwapped: true,
                slotOutcome: FightOutcome.WinB, durationSeconds: 10f,
                knockdownsSlotA: 0, knockdownsSlotB: 0, endHpSlotA: 0f, endHpSlotB: 0f);

            Assert.That(r.Outcome, Is.EqualTo(FightOutcome.WinA));
        }

        [Test]
        public void RemapSlotResultKeepsDrawAsDrawWhenSwapped()
        {
            var r = BalanceStats.RemapSlotResult(seed: 1, arenaIndex: 0, rolesSwapped: true,
                slotOutcome: FightOutcome.Draw, durationSeconds: 240f,
                knockdownsSlotA: 0, knockdownsSlotB: 0, endHpSlotA: 10f, endHpSlotB: 20f);

            Assert.That(r.Outcome, Is.EqualTo(FightOutcome.Draw));
            Assert.That(r.EndHpA, Is.EqualTo(20f));
            Assert.That(r.EndHpB, Is.EqualTo(10f));
        }

        [Test]
        public void DrawsCountHalfTowardWinRateAndAreReported()
        {
            // 5 A wins + 5 draws over 10: win rate 0.75 — flagged — and the
            // draw count itself is surfaced.
            var records = new List<FightRecord>();
            for (var i = 0; i < 5; i++)
            {
                records.Add(Fight(FightOutcome.WinA, 90f, 2, 1));
                records.Add(Fight(FightOutcome.Draw, 90f, 2, 1));
            }

            var m = BalanceStats.Aggregate("a", "b", records, Bands);
            Assert.That(m.WinRateA, Is.EqualTo(0.75f));
            Assert.That(m.Draws, Is.EqualTo(5));
            Assert.That(m.Flags, Has.Some.Contains("WINRATE"));
            Assert.That(m.Flags, Has.Some.Contains("DRAWS"));
        }

        [Test]
        public void MirrorMatchupLabelsWinRateFlagAsHarnessBias()
        {
            var records = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                records.Add(Fight(i < 8 ? FightOutcome.WinA : FightOutcome.WinB, 90f, 2, 1));
            }

            var m = BalanceStats.Aggregate("same", "same", records, Bands);
            Assert.That(m.IsMirror, Is.True);
            Assert.That(m.Flags, Has.Some.Contains("harness bias"));
        }

        [Test]
        public void ZeroFightsIsItselfAFlag()
        {
            var m = BalanceStats.Aggregate("a", "b", new List<FightRecord>(), Bands);
            Assert.That(m.Flags, Has.Some.Contains("NO DATA"));
        }

        [Test]
        public void MarkdownReportListsFlaggedPairingsInTheDigest()
        {
            var inBand = BalanceStats.Aggregate("line", "bastion", InBandFights(20), Bands);
            var records = new List<FightRecord>();
            for (var i = 0; i < 10; i++)
            {
                records.Add(Fight(FightOutcome.WinA, 90f, 2, 1)); // 100% A
            }

            var flagged = BalanceStats.Aggregate("sky", "hedge", records, Bands);
            var md = BalanceStats.RenderMarkdown(
                new List<MatrixResult>
                {
                    new MatrixResult { Name = "test-matrix", Matchups = new List<MatchupResult> { inBand, flagged } }
                },
                Bands, "header line");

            Assert.That(md, Does.Contain("| line | bastion |"));
            Assert.That(md, Does.Contain("test-matrix / sky vs hedge"));
            Assert.That(md, Does.Contain("WINRATE"));
        }
    }
}
