#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Tests.EditMode;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools
{
    // One-shot, explicitly invoked qualification before production integration. Never a
    // test that regenerates frozen expected data; no GameRoot or disk save writer.
    public static class CombatC0B1GoldenCapture
    {
        [Serializable] public sealed class Entry
        {
            public string Scenario, File, Identity, StartIdentity, Inspection;
            public long Ordinal;
            public bool Consumed;
        }
        [Serializable] public sealed class Capture
        {
            public string Source = "c0dba584f1d1b9f10ccc50bf51fc26c831c5f81e";
            public string UnityVersion;
            public Entry[] Entries;
        }
        public static void CaptureBeforeIntegration()
        {
            string output = "TestResults/combat-c0-b1-golden";
            if (Directory.Exists(output)) throw new InvalidOperationException("combat.c0.golden.already_exists");
            Directory.CreateDirectory(output);
            var entries = new List<Entry>();
            foreach (string scenario in new[] { "clear", "retreat", "wipe" })
            {
                var config = LivingDungeonPhaseABaselineTests.Configuration(scenario);
                var state = new RunSimulationService(config, PhaseSixA4Tests.Loot()).StartRoute(
                    new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 }, 42, 1,
                    RunPostureResolver.BalancedId, new[] { LivingDungeonPhaseABaselineTests.Room(0, true,
                        LivingDungeonPhaseABaselineTests.Content()), LivingDungeonPhaseABaselineTests.Room(1, true,
                        LivingDungeonPhaseABaselineTests.Content()) }, LivingDungeonPhaseAExecutionTests.Timing());
                Save(scenario, state, output, entries);
            }
            foreach (bool twoFloors in new[] { false, true })
            {
                // Reuse the exact qualified B1 canonical fixture, including real floor transfer.
                var arguments = new object[] { twoFloors, null };
                var snapshot = (ActiveFloorRunSnapshot)typeof(LivingDungeonPhaseB1Tests).GetMethod("Canonical",
                    BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
                var state = new RunSimulationService(LivingDungeonPhaseABaselineTests.Configuration("clear"),
                    PhaseSixA4Tests.Loot()).StartSnapshot(1, snapshot, LivingDungeonPhaseAExecutionTests.Timing());
                Save(twoFloors ? "canonical-two-floors" : "canonical-branch", state, output, entries);
                ((Action)arguments[1])();
            }
            File.WriteAllText(Path.Combine(output, "manifest.json"), JsonUtility.ToJson(new Capture {
                UnityVersion = Application.unityVersion, Entries = entries.ToArray() }, true));
            File.WriteAllText(Path.Combine(output, "historical-config.json"), JsonUtility.ToJson(PhaseFiveBTestConfig.Production()));
        }
        private static void Save(string scenario, RunExecution state, string output, List<Entry> entries)
        {
            var limits = new RunCheckpointLimits(1000000, 10000); // explicit test-only envelope
            do
            {
                var checkpoint = state.ExportCheckpoint(limits);
                string file = scenario + "-" + state.NextOrdinal + ".bin";
                File.WriteAllBytes(Path.Combine(output, file), checkpoint.GetBytes());
                entries.Add(new Entry { Scenario = scenario, File = file, Identity = checkpoint.Identity,
                    StartIdentity = checkpoint.StartIdentity, Ordinal = checkpoint.NextOrdinal,
                    Inspection = LivingDungeonPhaseABaselineTests.Structured(state.Inspect()) });
                if (state.IsTerminal) break;
                state.Advance(1, state.NextOrdinal);
            } while (true);
            File.WriteAllText(Path.Combine(output, scenario + "-outcome.txt"),
                LivingDungeonPhaseABaselineTests.Structured(state.TakeOutcome()));
            var consumed = state.ExportCheckpoint(limits);
            string consumedFile = scenario + "-consumed.bin";
            File.WriteAllBytes(Path.Combine(output, consumedFile), consumed.GetBytes());
            entries.Add(new Entry { Scenario = scenario, File = consumedFile, Identity = consumed.Identity,
                StartIdentity = consumed.StartIdentity, Ordinal = consumed.NextOrdinal, Consumed = true,
                Inspection = LivingDungeonPhaseABaselineTests.Structured(state.Inspect()) });
        }
    }
}
#endif
