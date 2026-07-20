using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.BalanceHarness
{
    // The balance CI (COMBAT_DOCTRINE §13): seeded AI-vs-AI batch runs
    // across a matchup matrix × arena sample, reporting win rate / TTK /
    // knockdowns and flagging pairings outside the doctrine bands. Hosted
    // as a [UnityTest] because the battle systems need play mode (deferred
    // Destroy), and the test runner is the project's proven batchmode
    // entry point — but it lives in its own assembly so the regular
    // PlayMode suite never pays for it; scripts/run-balance-harness.ps1 is
    // the front door. The test passes when every scheduled fight produced
    // a record and the report was written; balance flags are DATA in the
    // report, not failures — a content pass reads them, it isn't blocked
    // mid-run by them.
    public sealed class BalanceHarnessRun
    {
        private const float Dt = 1f / 60f;

        [Serializable]
        private sealed class ReportRoot
        {
            public string Config;
            public BalanceBands Bands;
            public List<MatrixResult> Matrices;
        }

        [UnityTest]
        [Timeout(21600000)] // wall-clock cap: 6 h, real runs are minutes
        public IEnumerator RunBalanceMatrix()
        {
            var roster = Arg("-balanceRoster", "default");
            var fightsPerPair = int.Parse(Arg("-balanceFights", "24"), CultureInfo.InvariantCulture);
            var baseSeed = int.Parse(Arg("-balanceSeed", "101"), CultureInfo.InvariantCulture);
            var arenas = Arg("-balanceArenas", "0,1,2,3")
                .Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            var maxFightSeconds = float.Parse(Arg("-balanceMaxFightSeconds", "240"), CultureInfo.InvariantCulture);
            var stepsPerFrame = int.Parse(Arg("-balanceStepsPerFrame", "600"), CultureInfo.InvariantCulture);

            var config = FormattableString.Invariant(
                $"roster={roster} fightsPerPair={fightsPerPair} baseSeed={baseSeed} arenas=[{string.Join(",", arenas)}] maxFightSeconds={maxFightSeconds:F0} dt={Dt:F4}");
            Debug.Log($"[BalanceHarness] {config}");

            var matrices = BalanceRoster.Select(roster);
            var bands = new BalanceBands();
            var results = new List<MatrixResult>();
            var started = DateTime.UtcNow;

            var pairIndex = 0;
            foreach (var matrix in matrices)
            {
                var matrixResult = new MatrixResult { Name = matrix.Name };
                results.Add(matrixResult);

                for (var i = 0; i < matrix.Builds.Count; i++)
                {
                    for (var j = i; j < matrix.Builds.Count; j++)
                    {
                        var buildA = matrix.Builds[i];
                        var buildB = matrix.Builds[j];
                        var records = new List<FightRecord>();

                        for (var f = 0; f < fightsPerPair; f++)
                        {
                            // Deterministic and bisectable: the seed is a
                            // pure function of (baseSeed, pair, fight).
                            var fightSeed = baseSeed * 1000003 + pairIndex * 8191 + f * 2;
                            var arena = arenas[f % arenas.Length];

                            // Alternate which build takes slot A (the -x
                            // spawn, ticked first, seeded fightSeed). Slot A
                            // carries a small structural edge — first mover on
                            // simultaneous frames — so build i must occupy it
                            // in exactly half a pairing's fights, or its win
                            // rate inherits the edge. Seeds stay tied to the
                            // SLOT, so build i samples both across the pair.
                            // Every recorded stat is remapped back to
                            // build i = "A", build j = "B".
                            var rolesSwapped = f % 2 == 1;
                            var loadoutSlotA = rolesSwapped ? buildB.Loadout : buildA.Loadout;
                            var loadoutSlotB = rolesSwapped ? buildA.Loadout : buildB.Loadout;

                            var duel = new HeadlessDuel(loadoutSlotA, loadoutSlotB,
                                arena, fightSeed, fightSeed + 1);

                            // One frame before stepping: robo-visual and
                            // arena-visual primitives strip their colliders
                            // with DEFERRED Destroys (RoboVisual/ArenaBuilder)
                            // — step batches must not raycast against them.
                            yield return null;

                            var steps = 0;
                            while (!duel.IsOver && duel.Elapsed < maxFightSeconds)
                            {
                                duel.Step(Dt);
                                if (++steps % stepsPerFrame == 0)
                                {
                                    yield return null;
                                }
                            }

                            var rawOutcome = duel.OutcomeNow();
                            var outcome = !rolesSwapped ? rawOutcome
                                : rawOutcome == FightOutcome.WinA ? FightOutcome.WinB
                                : rawOutcome == FightOutcome.WinB ? FightOutcome.WinA
                                : FightOutcome.Draw;

                            records.Add(new FightRecord
                            {
                                Seed = fightSeed,
                                ArenaIndex = arena,
                                SidesSwapped = rolesSwapped,
                                Outcome = outcome,
                                DurationSeconds = duel.Elapsed,
                                KnockdownsA = rolesSwapped ? duel.KnockdownsB : duel.KnockdownsA,
                                KnockdownsB = rolesSwapped ? duel.KnockdownsA : duel.KnockdownsB,
                                EndHpA = (rolesSwapped ? duel.B : duel.A).Health.Hp,
                                EndHpB = (rolesSwapped ? duel.A : duel.B).Health.Hp
                            });

                            duel.Dispose();
                            yield return null; // flush deferred Destroys before the next arena goes up
                        }

                        var aggregated = BalanceStats.Aggregate(buildA.Name, buildB.Name, records, bands);
                        matrixResult.Matchups.Add(aggregated);
                        Debug.Log(FormattableString.Invariant(
                            $"[BalanceHarness] {matrix.Name}: {buildA.Name} vs {buildB.Name} — winA {aggregated.WinRateA:P0}, ttk {aggregated.MeanTtkSeconds:F0}s, kd {aggregated.MeanKnockdowns:F1}{(aggregated.Flags.Count > 0 ? " ⚑ " + string.Join("; ", aggregated.Flags) : "")}"));
                        pairIndex++;
                    }
                }
            }

            var elapsed = DateTime.UtcNow - started;
            var header = FormattableString.Invariant(
        $"{config}\n\nRun {DateTime.Now:yyyy-MM-dd HH:mm} · {results.Sum(m => m.Matchups.Count)} pairings · {results.Sum(m => m.Matchups.Sum(p => p.Fights))} fights · wall {elapsed.TotalMinutes:F1} min.");

            var outDir = Path.Combine(Application.dataPath, "..", "TestResults");
            Directory.CreateDirectory(outDir);
            var mdPath = Path.GetFullPath(Path.Combine(outDir, "balance-report.md"));
            var jsonPath = Path.GetFullPath(Path.Combine(outDir, "balance-report.json"));
            File.WriteAllText(mdPath, BalanceStats.RenderMarkdown(results, bands, header));
            File.WriteAllText(jsonPath, JsonUtility.ToJson(
                new ReportRoot { Config = config, Bands = bands, Matrices = results }, prettyPrint: true));

            var flagCount = results.Sum(m => m.Matchups.Count(p => p.Flags.Count > 0));
            Debug.Log($"[BalanceHarness] Done: {flagCount} flagged pairing(s). Report: {mdPath}");

            // The harness's own health, not the game's balance: every
            // scheduled fight must have produced a record.
            foreach (var matrix in results)
            {
                foreach (var pairing in matrix.Matchups)
                {
                    Assert.That(pairing.Fights, Is.EqualTo(fightsPerPair),
                        $"{matrix.Name}: {pairing.BuildA} vs {pairing.BuildB} lost fights");
                }
            }

            Assert.That(File.Exists(mdPath), Is.True);
            Assert.That(File.Exists(jsonPath), Is.True);
        }

        private static string Arg(string flag, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
