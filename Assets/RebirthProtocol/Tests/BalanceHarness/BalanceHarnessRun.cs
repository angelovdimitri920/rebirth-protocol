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

            // The arena/side rotation (below) needs a full cycle — 2 side
            // assignments × arenas.Length arenas — to actually balance out;
            // a fightsPerPair that isn't a multiple of it truncates the
            // cycle and silently reintroduces a partial version of the same
            // confound the rotation exists to remove (Codex PR #16 finding).
            // A warning, not a hard failure: small ad-hoc smoke runs
            // (fightsPerPair=2, one arena) are still a legitimate use.
            var rotationPeriod = 2 * arenas.Length;
            if (fightsPerPair % rotationPeriod != 0)
            {
                Debug.LogWarning(FormattableString.Invariant(
                    $"[BalanceHarness] fightsPerPair={fightsPerPair} is not a multiple of 2*arenas.Length={rotationPeriod} — the arena/side rotation cycle is truncated, so results may retain a residual arena/side confound. Use a multiple of {rotationPeriod} for a clean run."));
            }

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

                            // Arena and side rotation must be INDEPENDENT: a
                            // naive `f % arenas.Length` for arena alongside
                            // `f % 2` for side ties them together whenever
                            // arenas.Length is even (with the default 4
                            // arenas, build i lands in slot A on arenas 0/2
                            // and slot B on arenas 1/3, EVERY pairing, EVERY
                            // run) — an asymmetric arena (Depot/Cinderfield's
                            // hazard layout isn't left-right symmetric) then
                            // leaks a positional advantage into what reads as
                            // loadout strength (Codex PR #16 finding). `f/2`
                            // runs both side assignments back-to-back on
                            // each arena before advancing, decoupling them.
                            var arena = arenas[(f / 2) % arenas.Length];

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

                            // The slot→build remap (outcome inversion + KD/HP
                            // swap) is a pure function, extracted to
                            // BalanceStats.RemapSlotResult so it has its own
                            // EditMode test coverage independent of this
                            // Unity-coupled loop (Codex PR #16 finding).
                            records.Add(BalanceStats.RemapSlotResult(fightSeed, arena, rolesSwapped,
                                duel.OutcomeNow(), duel.Elapsed, duel.KnockdownsA, duel.KnockdownsB,
                                duel.A.Health.Hp, duel.B.Health.Hp));

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
