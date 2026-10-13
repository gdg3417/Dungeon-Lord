using System;
using System.IO;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Explicit retained input record. No iterator/object graph serialization.
    internal static class RunStartCapture
    {
        [Serializable] private sealed class Rooms { public MvpOrderedRouteRoom[] Values; }
        internal static byte[] Capture(RunSimulationConfig config, LootConfig loot, string table,
            StructureRuntimeState runtime, long tick, int sequence, string posture, MvpOrderedRouteRoom[] rooms,
            ActiveFloorRunSnapshot snapshot, RunLogicalTiming timing, long limit) => RunCheckpointCodec.Bytes(w => {
                w.Write(RunCheckpointCodec.Format); w.Write(sequence); w.Write(tick); w.Write(limit);
                RunCheckpointCodec.Text(w, posture); RunCheckpointCodec.Text(w, JsonUtility.ToJson(config));
                w.Write(config.MvpCompositionOutcomeTuning == null); w.Write(config.PhaseSix == null);
                RunCheckpointCodec.Text(w, loot == null ? null : JsonUtility.ToJson(loot)); RunCheckpointCodec.Text(w, table);
                RunCheckpointCodec.Text(w, JsonUtility.ToJson(runtime));
                w.Write(timing != null);
                if (timing != null) {
                    w.Write(timing.UnitsPerTileEdge); w.Write(timing.MonsterInteractionUnits); w.Write(timing.TrapInteractionUnits);
                    w.Write(timing.LootInteractionUnits); w.Write(timing.FloorTransferUnits);
                }
                w.Write(snapshot != null);
                if (snapshot != null) snapshot.WriteCheckpoint(w);
                else {
                    RunCheckpointCodec.Text(w, JsonUtility.ToJson(new Rooms { Values = rooms }));
                    w.Write(rooms.Length);
                    foreach (var room in rooms) { w.Write(room.Spatial == null); w.Write(room.Assignments == null); }
                }
            });

        internal static RunExecution Recreate(byte[] bytes)
        {
            using (var r = new BinaryReader(new MemoryStream(bytes), new System.Text.UTF8Encoding(false, true)))
            {
                if (r.ReadInt32() != RunCheckpointCodec.Format) throw new InvalidDataException("run.checkpoint.encoding");
                int sequence = r.ReadInt32(); long tick = r.ReadInt64(), limit = r.ReadInt64();
                string posture = RunCheckpointCodec.Text(r);
                var config = JsonUtility.FromJson<RunSimulationConfig>(RunCheckpointCodec.Text(r));
                if (RunCheckpointCodec.Flag(r)) config.MvpCompositionOutcomeTuning = null;
                if (RunCheckpointCodec.Flag(r)) config.PhaseSix = null;
                string lootJson = RunCheckpointCodec.Text(r), table = RunCheckpointCodec.Text(r);
                var loot = lootJson == null ? null : JsonUtility.FromJson<LootConfig>(lootJson);
                var runtime = JsonUtility.FromJson<StructureRuntimeState>(RunCheckpointCodec.Text(r));
                RunLogicalTiming timing = null;
                if (RunCheckpointCodec.Flag(r)) timing = new RunLogicalTiming(r.ReadInt64(), r.ReadInt64(), r.ReadInt64(), r.ReadInt64(), r.ReadInt64());
                ActiveFloorRunSnapshot snapshot = null; MvpOrderedRouteRoom[] rooms = null;
                if (RunCheckpointCodec.Flag(r)) snapshot = ActiveFloorRunSnapshot.ReadCheckpoint(r);
                else {
                    rooms = JsonUtility.FromJson<Rooms>(RunCheckpointCodec.Text(r)).Values;
                    int count = r.ReadInt32();
                    if (count != rooms.Length || count < 0 || count > (r.BaseStream.Length - r.BaseStream.Position) / 2)
                        throw new InvalidDataException("run.checkpoint.encoding");
                    foreach (var room in rooms) {
                        if (RunCheckpointCodec.Flag(r)) room.Spatial = null;
                        if (RunCheckpointCodec.Flag(r)) room.Assignments = null;
                    }
                }
                RunCheckpointCodec.End(r);
                var engine = new RunSimulationService(config, loot, table);
                var execution = snapshot == null ? engine.StartRoute(runtime, tick, sequence, posture, rooms, timing, limit) :
                    engine.StartSnapshot(sequence, snapshot, timing, limit);
                // Canonical capture must reproduce every byte, including compatibility discriminators.
                if (!System.Linq.Enumerable.SequenceEqual(bytes, execution.StartInputs)) {
                    execution.Abandon(); throw new InvalidDataException("run.checkpoint.encoding");
                }
                return execution;
            }
        }
    }
}
