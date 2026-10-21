#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    // Explicit local qualification command, not part of gameplay or normal tests.
    // Attest new-format-1 bytes with the protected original PR #245 executable.
    public static class LivingDungeonHistoricalCheckpointCapture
    {
        [Serializable] private sealed class Golden
        {
            public string Source, SourceSha256, ReaderModule, StartIdentity, CheckpointIdentity, Base64;
            public int Format, VerifiedPrefixes;
            public long NextOrdinal;
        }
        public static void CaptureFormatOne()
        {
            string original = Path.GetFullPath("Builds/Development/Windows-living-dungeon-b3b2-20261019-live-marker-rendered-final/Dungeon Lord_Data/Managed/Assembly-CSharp.dll");
            string output = Path.GetFullPath("Tools/Presentation/Fixtures/historical-b1-format1.json");
            if (!Application.productName.Contains("B3B2 Smoke Two Floor") || File.Exists(output))
                throw new InvalidOperationException("historical.capture.isolation_required");
            byte[] dll = File.ReadAllBytes(original);
            using var hash = SHA256.Create();
            string identity = BitConverter.ToString(hash.ComputeHash(dll)).Replace("-", "").ToLowerInvariant();
            if (identity != "577293ec07d0aadf7dfa281f5d9da60eaf684f363f61ce56cc695abbb03eb860")
                throw new InvalidOperationException("historical.capture.original_binary_mismatch");
            var old = Assembly.Load(dll);
            if (old == typeof(RunExecution).Assembly || old.ManifestModule.ModuleVersionId == typeof(RunExecution).Assembly.ManifestModule.ModuleVersionId)
                throw new InvalidOperationException("historical.capture.reader_not_independent");
            var limitType = old.GetType("DungeonBuilder.M0.Gameplay.RunSimulation.RunCheckpointLimits", true);
            var oldLimits = Activator.CreateInstance(limitType, 4000000, 10000L, 1000000L); // historical test envelope
            var restore = old.GetType("DungeonBuilder.M0.Gameplay.RunSimulation.CombatCheckpointCodec", true)
                .GetMethod("RestoreSupported", BindingFlags.Static | BindingFlags.NonPublic);
            var currentLimits = new RunCheckpointLimits(4000000, 10000, 1000000);
            var current = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB1Tests.CreateForPlayMode();
            Golden selected = null; int verified = 0;
            try {
                while (true) {
                    var cp = current.ExportCheckpoint(currentLimits); var bytes = cp.GetBytes();
                    var result = restore.Invoke(null, new object[] { bytes, cp.Identity, cp.StartIdentity, cp.NextOrdinal, oldLimits, false });
                    var resultType = result.GetType();
                    if (!(bool)resultType.GetProperty("IsValid").GetValue(result)) {
                        // Preserve the underlying old-reader failure instead of
                        // mistaking a load-context/fixture limitation for a byte change.
                        var start = (byte[])typeof(RunExecution).GetField("StartInputs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(current);
                        try {
                            old.GetType("DungeonBuilder.M0.Gameplay.RunSimulation.RunStartCapture", true)
                                .GetMethod("Recreate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { start });
                        } catch (TargetInvocationException error) { Debug.LogException(error.InnerException); }
                        throw new InvalidOperationException("historical.capture.old_reader_rejected: " + resultType.GetProperty("FailureReason").GetValue(result));
                    }
                    var execution = resultType.GetProperty("Execution").GetValue(result);
                    try {
                        var checkpoint = execution.GetType().GetMethod("ExportCheckpoint").Invoke(execution, new[] { oldLimits });
                        var oldBytes = (byte[])checkpoint.GetType().GetMethod("GetBytes").Invoke(checkpoint, null);
                        if (!bytes.SequenceEqual(oldBytes)) throw new InvalidOperationException("historical.capture.original_bytes_changed");
                        if (cp.NextOrdinal == 7) selected = new Golden { Source = original, SourceSha256 = identity,
                            ReaderModule = old.ManifestModule.ModuleVersionId.ToString(), Format = 1, NextOrdinal = cp.NextOrdinal,
                            StartIdentity = cp.StartIdentity, CheckpointIdentity = cp.Identity, Base64 = Convert.ToBase64String(oldBytes) };
                    } finally { execution.GetType().GetMethod("Abandon", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(execution, null); }
                    verified++;
                    if (current.IsTerminal) break;
                    current.Advance(1, current.NextOrdinal);
                }
            } finally { current.Abandon(); }
            if (selected == null) throw new InvalidOperationException("historical.capture.prefix_missing");
            selected.VerifiedPrefixes = verified;
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(selected, true));
            Debug.Log("historical.capture.original_reader_verified_prefixes=" + verified);
        }
    }
}
#endif
