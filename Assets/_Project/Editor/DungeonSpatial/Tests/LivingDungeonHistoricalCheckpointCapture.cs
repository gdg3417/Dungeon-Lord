#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    // Explicit local qualification. The failed side-by-side player DLL attempt
    // remains evidence, and is not used as a compatibility attestation.
    public static class LivingDungeonHistoricalCheckpointCapture
    {
        [Serializable] private sealed class Golden
        {
            public string Source, SourceSha256, StartCaptureSourceSha256, StartIdentity, CheckpointIdentity, Base64;
            public int Format, VerifiedPrefixes;
            public long NextOrdinal;
        }
        private static string Identity(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static string SourceIdentity(string name) => Identity(Encoding.UTF8.GetBytes(
            File.ReadAllText("Assets/_Project/Scripts/Gameplay/RunSimulation/" + name).Replace("\r\n", "\n")));
        public static void CaptureFormatOne()
        {
            string output = Path.GetFullPath("Tools/Presentation/Fixtures/historical-b1-format1.json");
            if (!Application.productName.Contains("B3B2 Smoke Two Floor") || File.Exists(output))
                throw new InvalidOperationException("historical.capture.isolation_required");
            // Exact hashes from verified main. The format-1 codec/start sources
            // have no feature edits; this golden pins that historical contract.
            string codec = SourceIdentity("RunCheckpoint.cs"), startSource = SourceIdentity("RunStartCapture.cs");
            if (codec != "3663a6c7d65ed34c65ecdc6e1af3956cf70080d739b4dfeaaa0e1b75660e7db7" ||
                startSource != "88bb167baece2d4a559db78c1d3bd18edd16e6186d8da7e212a197df9aae15ef")
                throw new InvalidOperationException("historical.capture.frozen_source_changed");
            var limits = new RunCheckpointLimits(4000000, 10000, 1000000); // historical test envelope
            var current = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB1Tests.CreateForPlayMode();
            Golden selected = null; int verified = 0;
            try {
                while (true) {
                    var cp = current.ExportCheckpoint(limits); var bytes = cp.GetBytes();
                    if (BitConverter.ToInt32(bytes, 0) != 1 || current.Combat != null || current.TransferContract)
                        throw new InvalidOperationException("historical.capture.wrong_executable");
                    var restored = CombatCheckpointCodec.RestoreSupported(bytes, cp.Identity, cp.StartIdentity, cp.NextOrdinal, limits);
                    if (!restored.IsValid) throw new InvalidOperationException(restored.FailureReason);
                    try {
                        if (!bytes.SequenceEqual(restored.Execution.ExportCheckpoint(limits).GetBytes()))
                            throw new InvalidOperationException("historical.capture.roundtrip_changed");
                        if (cp.NextOrdinal == 7) selected = new Golden {
                            Source = "main@76f78c5c2d17d3aab916f297aecb5c25cf146d76 frozen format-1 sources",
                            SourceSha256 = codec, StartCaptureSourceSha256 = startSource, Format = 1, NextOrdinal = cp.NextOrdinal,
                            StartIdentity = cp.StartIdentity, CheckpointIdentity = cp.Identity, Base64 = Convert.ToBase64String(bytes) };
                    } finally { restored.Execution.Abandon(); }
                    verified++;
                    if (current.IsTerminal) break;
                    current.Advance(1, current.NextOrdinal);
                }
            } finally { current.Abandon(); }
            if (selected == null) throw new InvalidOperationException("historical.capture.prefix_missing");
            selected.VerifiedPrefixes = verified;
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(selected, true));
            Debug.Log("historical.capture.frozen_source_verified_prefixes=" + verified);
        }
    }
}
#endif
