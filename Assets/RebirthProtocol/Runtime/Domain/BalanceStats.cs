using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RebirthProtocol.Domain
{
    // Balance-harness aggregation and flagging (COMBAT_DOCTRINE §13):
    // seeded AI-vs-AI batch runs per matchup report win rate, TTK, and
    // knockdown count; any pairing outside 40–60% win rate or outside the
    // pacing band (60–120 s, 2–5 knockdowns, pillar 8) flags itself.
    // Plain C# so the flag logic is EditMode-testable — the Unity-coupled
    // fight loop lives in the harness test assembly.

    public enum FightOutcome
    {
        WinA,
        WinB,
        Draw // timeout at the cap, or a double KO (bomb trades both out)
    }

    [Serializable]
    public sealed class FightRecord
    {
        public int Seed;
        public int ArenaIndex;
        public bool SidesSwapped;
        public FightOutcome Outcome;
        public float DurationSeconds;
        public int KnockdownsA;
        public int KnockdownsB;
        public float EndHpA;
        public float EndHpB;
    }

    /// The flag bands. Doctrine values are the defaults; fields stay
    /// settable so a future pass can tighten them without a recompile.
    [Serializable]
    public sealed class BalanceBands
    {
        public float WinRateLow = 0.40f;
        public float WinRateHigh = 0.60f;
        public float TtkLowSeconds = 60f;
        public float TtkHighSeconds = 120f;
        public float KnockdownsLow = 2f;
        public float KnockdownsHigh = 5f;
    }

    [Serializable]
    public sealed class MatchupResult
    {
        public string BuildA;
        public string BuildB;
        public int Fights;
        public int WinsA;
        public int WinsB;
        public int Draws;
        public float WinRateA; // draws count half — a stalemate is not a loss
        public float MeanTtkSeconds;
        public float MeanKnockdowns; // both sides combined, per fight
        public List<string> Flags = new List<string>();
        public List<FightRecord> FightRecords = new List<FightRecord>();

        public bool IsMirror => BuildA == BuildB;
    }

    [Serializable]
    public sealed class MatrixResult
    {
        public string Name;
        public List<MatchupResult> Matchups = new List<MatchupResult>();
    }

    public static class BalanceStats
    {
        /// Aggregate one pairing's fight records into rates, means, and
        /// doctrine flags. A mirror matchup (A vs A) keeps the win-rate
        /// flag: with both sides driven by the same brain, a win rate
        /// outside the band there means the harness itself is biased
        /// (spawn side, tick order), which is exactly worth flagging.
        public static MatchupResult Aggregate(string buildA, string buildB,
            List<FightRecord> records, BalanceBands bands)
        {
            var result = new MatchupResult
            {
                BuildA = buildA,
                BuildB = buildB,
                Fights = records.Count,
                FightRecords = records
            };

            if (records.Count == 0)
            {
                result.Flags.Add("NO DATA — zero fights recorded");
                return result;
            }

            var ttkSum = 0f;
            var kdSum = 0f;
            foreach (var r in records)
            {
                switch (r.Outcome)
                {
                    case FightOutcome.WinA:
                        result.WinsA++;
                        break;
                    case FightOutcome.WinB:
                        result.WinsB++;
                        break;
                    default:
                        result.Draws++;
                        break;
                }

                ttkSum += r.DurationSeconds;
                kdSum += r.KnockdownsA + r.KnockdownsB;
            }

            result.WinRateA = (result.WinsA + 0.5f * result.Draws) / records.Count;
            result.MeanTtkSeconds = ttkSum / records.Count;
            result.MeanKnockdowns = kdSum / records.Count;

            if (result.WinRateA < bands.WinRateLow || result.WinRateA > bands.WinRateHigh)
            {
                result.Flags.Add(FormattableString.Invariant(
                    $"WINRATE {result.WinRateA:P0} outside {bands.WinRateLow:P0}–{bands.WinRateHigh:P0}{(result.IsMirror ? " (mirror: harness bias)" : "")}"));
            }

            if (result.MeanTtkSeconds < bands.TtkLowSeconds || result.MeanTtkSeconds > bands.TtkHighSeconds)
            {
                result.Flags.Add(FormattableString.Invariant(
                    $"TTK {result.MeanTtkSeconds:F0}s outside {bands.TtkLowSeconds:F0}–{bands.TtkHighSeconds:F0}s"));
            }

            if (result.MeanKnockdowns < bands.KnockdownsLow || result.MeanKnockdowns > bands.KnockdownsHigh)
            {
                result.Flags.Add(FormattableString.Invariant(
                    $"KNOCKDOWNS {result.MeanKnockdowns:F1} outside {bands.KnockdownsLow:F0}–{bands.KnockdownsHigh:F0}"));
            }

            if (result.Draws > 0)
            {
                result.Flags.Add(FormattableString.Invariant(
                    $"DRAWS {result.Draws}/{records.Count} (timeouts or double KOs)"));
            }

            return result;
        }

        /// Human-skimmable report: one table per matrix, flags inline, then
        /// a flag digest so a red run is readable from the bottom up.
        public static string RenderMarkdown(List<MatrixResult> matrices, BalanceBands bands, string header)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Balance Harness Report");
            sb.AppendLine();
            sb.AppendLine(header);
            sb.AppendLine();
            sb.AppendLine(FormattableString.Invariant(
                $"Bands: win rate {bands.WinRateLow:P0}–{bands.WinRateHigh:P0} · TTK {bands.TtkLowSeconds:F0}–{bands.TtkHighSeconds:F0} s · knockdowns {bands.KnockdownsLow:F0}–{bands.KnockdownsHigh:F0} (COMBAT_DOCTRINE §13, pillar 8)."));

            var flagged = new List<(string matrix, MatchupResult m)>();
            foreach (var matrix in matrices)
            {
                sb.AppendLine();
                sb.AppendLine($"## {matrix.Name}");
                sb.AppendLine();
                sb.AppendLine("| A | B | Fights | Win rate A | Mean TTK | Mean KDs | Flags |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (var m in matrix.Matchups)
                {
                    sb.AppendLine(FormattableString.Invariant(
                        $"| {m.BuildA} | {m.BuildB} | {m.Fights} | {m.WinRateA:P0} | {m.MeanTtkSeconds:F0} s | {m.MeanKnockdowns:F1} | {(m.Flags.Count == 0 ? "—" : string.Join("; ", m.Flags))} |"));
                    if (m.Flags.Count > 0)
                    {
                        flagged.Add((matrix.Name, m));
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Flag digest");
            sb.AppendLine();
            if (flagged.Count == 0)
            {
                sb.AppendLine("All pairings in band.");
            }
            else
            {
                foreach (var (matrixName, m) in flagged)
                {
                    sb.AppendLine($"- **{matrixName} / {m.BuildA} vs {m.BuildB}**: {string.Join("; ", m.Flags)}");
                }
            }

            return sb.ToString();
        }
    }
}
