#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.BuildTools
{
    // Opt-in technical build qualification, before Bootstrap can touch any save.
    // Inputs are detached test artifacts; this capability has no canonical writer.
    internal static class CombatCheckpointDevelopmentSmoke
    {
        [Serializable] internal sealed class Input
        { public int MaximumBytes; public long MaximumReplayEvents, MaximumCombatWork; public Row[] Rows; }
        [Serializable] internal sealed class Row
        { public string Bytes, Identity, StartIdentity, FinalBytes; public long Ordinal; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-b3b1CheckpointSmoke");
            if (index < 0) return;
            int exit = 1;
            try {
                if (index + 2 >= args.Length) throw new ArgumentException("b3b1.smoke.arguments");
                string source = Path.GetFullPath(args[index + 1]), destination = Path.GetFullPath(args[index + 2]);
                if (File.Exists(destination)) throw new IOException("b3b1.smoke.preserve_output");
                var input = JsonUtility.FromJson<Input>(File.ReadAllText(source));
                var limits = new RunCheckpointLimits(input.MaximumBytes, input.MaximumReplayEvents, input.MaximumCombatWork);
                int verified = 0;
                foreach (var row in input.Rows) {
                    var restored = CombatCheckpointCodec.RestoreSupported(Convert.FromBase64String(row.Bytes), row.Identity, row.StartIdentity, row.Ordinal, limits);
                    if (!restored.IsValid) throw new InvalidOperationException(restored.FailureReason);
                    var e = restored.Execution;
                    while (!e.IsTerminal) {
                        long due = e.NextDueTime().Value;
                        if (e.NextDueTime() != due) throw new InvalidOperationException("b3b1.smoke.due");
                        e.AdvanceThrough(due, 1, e.NextOrdinal);
                        var c = e.ExportCheckpoint(limits);
                        var reopened = CombatCheckpointCodec.RestoreSupported(c.GetBytes(), c.Identity, c.StartIdentity, c.NextOrdinal, limits);
                        if (!reopened.IsValid) throw new InvalidOperationException(reopened.FailureReason);
                        e.Abandon(); e = reopened.Execution;
                    }
                    e.TakeOutcome();
                    if (!e.ExportCheckpoint(limits).GetBytes().SequenceEqual(Convert.FromBase64String(row.FinalBytes)))
                        throw new InvalidOperationException("b3b1.smoke.outcome");
                    verified++;
                }
                using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write("{\"passed\":true,\"verified\":" + verified + "}");
                exit = 0;
            }
            catch (Exception error) { Debug.LogException(error); }
            // Immediate exit prevents subsequent Bootstrap initialization/save publication.
            Environment.Exit(exit);
        }
    }
}
#endif
