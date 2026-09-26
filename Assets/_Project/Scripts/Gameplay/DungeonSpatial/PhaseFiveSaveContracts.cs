using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class CorridorContentAssignment
    {
        public string AssignmentId;
        public string CategoryId;
        public string OptionId;
        public long Sequence;
        public string FloorInstanceId;
        public string OptionalBranchId;
        public string EdgeId;
        public TileCoordinate Tile;
    }

    [Serializable]
    public sealed class CorridorContentAuthority
    {
        public CorridorContentAssignment[] Assignments = Array.Empty<CorridorContentAssignment>();
    }

    [Serializable]
    public sealed class BranchKnowledgeRecord
    {
        public string FloorInstanceId;
        public string OptionalBranchId;
        public string EdgeId;
        public string TopologyFingerprint;
        public bool TopologyKnown;
        public bool IncentiveKnown;
        public double PerceivedIncentive;
        public bool DangerKnown;
        public double PerceivedDanger;
        public bool ConfidenceKnown;
        public double Confidence;
        public bool HasLastConfirmedRun;
        public string LastConfirmedRunId;
    }

    [Serializable]
    public sealed class SharedBranchKnowledgeAuthority
    {
        public BranchKnowledgeRecord[] Records = Array.Empty<BranchKnowledgeRecord>();
    }

    public static class BranchTopologyFingerprint
    {
        public static bool TryCompute(DetachedCanonicalSpatialSaveState state, string floorInstanceId,
            string optionalBranchId, out string fingerprint)
        {
            fingerprint = null;
            try
            {
                SavedSpatialFloor floor = (state?.Floors ?? Array.Empty<SavedSpatialFloor>()).SingleOrDefault(
                    value => value != null && value.FloorInstanceId == floorInstanceId);
                FloorRouteEdge edge = (floor?.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).SingleOrDefault(
                    value => value != null && value.Classification == RouteClassification.Optional &&
                    value.OptionalBranchId == optionalBranchId);
                FloorRouteNode destination = (floor?.Layout?.Nodes ?? Array.Empty<FloorRouteNode>()).SingleOrDefault(
                    value => value != null && value.NodeId == edge?.DestinationNodeId);
                if (edge == null || destination?.Kind != FloorRouteNodeKind.DeadEnd ||
                    edge.ConnectionKind != FloorRouteConnectionKind.PhysicalCorridor) return false;
                string tiles = string.Join(";", (edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>())
                    .OrderBy(value => value).Select(value => value.X.ToString(CultureInfo.InvariantCulture) +
                    "," + value.Y.ToString(CultureInfo.InvariantCulture)));
                string evidence = floor.FloorInstanceId + "\n" + edge.OptionalBranchId + "\n" + edge.EdgeId +
                    "\n" + edge.SourceNodeId + "\n" + edge.DestinationNodeId + "\n" +
                    edge.CorridorDefinitionId + "\n" + ((int)edge.Classification).ToString(CultureInfo.InvariantCulture) +
                    "\n" + ((int)edge.ConnectionKind).ToString(CultureInfo.InvariantCulture) + "\n" + tiles;
                fingerprint = SpatialContractSha256.Compute(System.Text.Encoding.UTF8.GetBytes(evidence));
                return true;
            }
            catch { return false; }
        }

        public static bool IsApplicable(BranchKnowledgeRecord record,
            DetachedCanonicalSpatialSaveState state) => record != null &&
            SpatialContractSha256.IsCanonical(record.TopologyFingerprint) &&
            TryCompute(state, record.FloorInstanceId, record.OptionalBranchId, out string current) &&
            string.Equals(current, record.TopologyFingerprint, StringComparison.Ordinal);
    }

    internal static class PhaseFiveSaveContracts
    {
        internal const string CorridorOwnerName = "corridorContent";
        internal const string KnowledgeOwnerName = "sharedBranchKnowledge";

        internal static CorridorContentAuthority EmptyCorridor() => new CorridorContentAuthority();
        internal static SharedBranchKnowledgeAuthority EmptyKnowledge() => new SharedBranchKnowledgeAuthority();

        internal static bool TryReadCorridor(ContractJsonNode node, out CorridorContentAuthority value)
        {
            value = null;
            if (!Members(node, "Assignments") || node.Fields[0].Value.Kind != ContractJsonKind.Array)
                return false;
            var records = new List<CorridorContentAssignment>();
            foreach (ContractJsonNode item in node.Fields[0].Value.Items)
            {
                if (!Members(item, "AssignmentId", "CategoryId", "OptionId", "Sequence",
                        "FloorInstanceId", "OptionalBranchId", "EdgeId", "Tile") ||
                    !String(item, 0) || !String(item, 1) || !String(item, 2) ||
                    !Long(item, 3, out long sequence) || !String(item, 4) || !String(item, 5) ||
                    !String(item, 6) || !Tile(item.Fields[7].Value, out TileCoordinate tile)) return false;
                records.Add(new CorridorContentAssignment
                {
                    AssignmentId = item.Fields[0].Value.Text,
                    CategoryId = item.Fields[1].Value.Text,
                    OptionId = item.Fields[2].Value.Text,
                    Sequence = sequence,
                    FloorInstanceId = item.Fields[4].Value.Text,
                    OptionalBranchId = item.Fields[5].Value.Text,
                    EdgeId = item.Fields[6].Value.Text,
                    Tile = tile
                });
            }
            value = Canonicalize(new CorridorContentAuthority { Assignments = records.ToArray() });
            CorridorContentAuthority parsed = value;
            return CanonicalCorridor(parsed) && SameNode(node, writer => Write(writer, parsed));
        }

        internal static bool TryReadKnowledge(ContractJsonNode node,
            out SharedBranchKnowledgeAuthority value)
        {
            value = null;
            if (!Members(node, "Records") || node.Fields[0].Value.Kind != ContractJsonKind.Array)
                return false;
            var records = new List<BranchKnowledgeRecord>();
            foreach (ContractJsonNode item in node.Fields[0].Value.Items)
            {
                if (!Members(item, "FloorInstanceId", "OptionalBranchId", "EdgeId",
                        "TopologyFingerprint", "TopologyKnown", "IncentiveKnown",
                        "PerceivedIncentive", "DangerKnown", "PerceivedDanger",
                        "ConfidenceKnown", "Confidence", "HasLastConfirmedRun",
                        "LastConfirmedRunId") || !String(item, 0) || !String(item, 1) ||
                    !String(item, 2) || !String(item, 3) || !Boolean(item, 4, out bool topology) ||
                    !Boolean(item, 5, out bool incentiveKnown) || !Number(item, 6, out double incentive) ||
                    !Boolean(item, 7, out bool dangerKnown) || !Number(item, 8, out double danger) ||
                    !Boolean(item, 9, out bool confidenceKnown) || !Number(item, 10, out double confidence) ||
                    !Boolean(item, 11, out bool hasRun) || !NullableString(item, 12)) return false;
                records.Add(new BranchKnowledgeRecord
                {
                    FloorInstanceId = item.Fields[0].Value.Text,
                    OptionalBranchId = item.Fields[1].Value.Text,
                    EdgeId = item.Fields[2].Value.Text,
                    TopologyFingerprint = item.Fields[3].Value.Text,
                    TopologyKnown = topology,
                    IncentiveKnown = incentiveKnown,
                    PerceivedIncentive = incentive,
                    DangerKnown = dangerKnown,
                    PerceivedDanger = danger,
                    ConfidenceKnown = confidenceKnown,
                    Confidence = confidence,
                    HasLastConfirmedRun = hasRun,
                    LastConfirmedRunId = item.Fields[12].Value.Kind == ContractJsonKind.Null
                        ? null : item.Fields[12].Value.Text
                });
            }
            value = Canonicalize(new SharedBranchKnowledgeAuthority { Records = records.ToArray() });
            SharedBranchKnowledgeAuthority parsed = value;
            return CanonicalKnowledge(parsed) && SameNode(node, writer => Write(writer, parsed));
        }

        internal static bool Validate(CorridorContentAuthority corridor,
            SharedBranchKnowledgeAuthority knowledge, DetachedCanonicalSpatialSaveState spatial,
            ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            CanonicalSpatialSaveWorkloadLimits limits)
        {
            if (corridor == null || knowledge == null || spatial == null || production == null ||
                configuration == null || !limits.IsValid || !CanonicalCorridor(corridor) ||
                !CanonicalKnowledge(knowledge)) return false;
            if (!TryCountRecords(spatial, corridor, knowledge, limits.MaximumRecords)) return false;
            var activeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoomContentAssignment item in (spatial.Floors ?? Array.Empty<SavedSpatialFloor>())
                .Where(floor => floor != null).SelectMany(floor => floor.RoomContents?.Assignments ??
                    Array.Empty<RoomContentAssignment>()))
                if (item == null || !activeIds.Add(item.AssignmentId)) return false;
            foreach (ReturnedStructuralContent item in spatial.LifecycleAndOwnership?.ReturnedContents ??
                Array.Empty<ReturnedStructuralContent>())
                if (item == null || !activeIds.Add(item.AssignmentId)) return false;

            var occupied = new HashSet<string>(StringComparer.Ordinal);
            var configured = new HashSet<string>((configuration.MvpPlacementEffects ??
                Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null)
                .Select(value => value.OptionId), StringComparer.Ordinal);
            foreach (CorridorContentAssignment item in corridor.Assignments)
            {
                if (item == null || !Persistent(item.AssignmentId) || !activeIds.Add(item.AssignmentId) ||
                    item.Sequence < 0 || !MvpDungeonPlacementIds.TryGetCategoryForOption(item.OptionId,
                        out string category) || category != item.CategoryId ||
                    (category != CanonicalSpatialSaveContracts.TrapCategoryId &&
                     category != CanonicalSpatialSaveContracts.LootNodeCategoryId) ||
                    !configured.Contains(item.OptionId)) return false;
                SavedSpatialFloor floor = spatial.Floors.SingleOrDefault(value => value != null &&
                    value.FloorInstanceId == item.FloorInstanceId);
                FloorRouteEdge edge = (floor?.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).SingleOrDefault(
                    value => value != null && value.EdgeId == item.EdgeId &&
                    value.OptionalBranchId == item.OptionalBranchId);
                if (edge == null || edge.Classification != RouteClassification.Optional ||
                    edge.ConnectionKind != FloorRouteConnectionKind.PhysicalCorridor ||
                    !(edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>()).Contains(item.Tile))
                    return false;
                CorridorSpatialDefinition definition = (production.Catalog.Corridors ??
                    Array.Empty<CorridorSpatialDefinition>()).SingleOrDefault(value => value != null &&
                    value.CorridorDefinitionId == edge.CorridorDefinitionId);
                if (definition == null) return false;
                string tileKey = floor.FloorInstanceId + "\0" + edge.EdgeId + "\0" + item.Tile.X + "\0" + item.Tile.Y;
                if (!occupied.Add(tileKey)) return false;
                int count = corridor.Assignments.Count(value => value != null && value.EdgeId == edge.EdgeId &&
                    value.CategoryId == category);
                int capacity = category == CanonicalSpatialSaveContracts.TrapCategoryId
                    ? definition.TrapCapacity : definition.LootCapacity;
                if (count > capacity) return false;
                if (!OptionalBranchGeometry.TryResolveSourceTile(floor, edge, production.Catalog,
                        out TileCoordinate sourceTile)) return false;
                if (category == CanonicalSpatialSaveContracts.LootNodeCategoryId &&
                    !item.Tile.Equals(TerminalTile(edge, sourceTile))) return false;
            }
            var branchKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BranchKnowledgeRecord record in knowledge.Records)
            {
                if (record == null || !Persistent(record.FloorInstanceId) ||
                    !Persistent(record.OptionalBranchId) || !Persistent(record.EdgeId) ||
                    !SpatialContractSha256.IsCanonical(record.TopologyFingerprint) ||
                    !branchKeys.Add(record.FloorInstanceId + "\0" + record.OptionalBranchId) ||
                    !Bounded(record.PerceivedIncentive) || !Bounded(record.PerceivedDanger) ||
                    !Bounded(record.Confidence) || !record.IncentiveKnown && record.PerceivedIncentive != 0d ||
                    !record.DangerKnown && record.PerceivedDanger != 0d ||
                    !record.ConfidenceKnown && record.Confidence != 0d ||
                    record.HasLastConfirmedRun != !string.IsNullOrEmpty(record.LastConfirmedRunId) ||
                    record.HasLastConfirmedRun && !Persistent(record.LastConfirmedRunId)) return false;
                SavedSpatialFloor floor = spatial.Floors.SingleOrDefault(value => value != null &&
                    value.FloorInstanceId == record.FloorInstanceId);
                FloorRouteEdge edge = (floor?.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).SingleOrDefault(
                    value => value != null && value.EdgeId == record.EdgeId &&
                    value.OptionalBranchId == record.OptionalBranchId &&
                    value.Classification == RouteClassification.Optional);
                if (edge == null) return false;
            }
            return true;
        }

        internal static CorridorContentAuthority Canonicalize(CorridorContentAuthority source) =>
            new CorridorContentAuthority
            {
                Assignments = (source?.Assignments ?? Array.Empty<CorridorContentAssignment>())
                    .Select(Copy).OrderBy(value => value?.FloorInstanceId, StringComparer.Ordinal)
                    .ThenBy(value => value?.OptionalBranchId, StringComparer.Ordinal)
                    .ThenBy(value => value?.EdgeId, StringComparer.Ordinal)
                    .ThenBy(value => value?.Tile).ThenBy(value => Rank(value?.CategoryId))
                    .ThenBy(value => value?.Sequence ?? 0L)
                    .ThenBy(value => value?.AssignmentId, StringComparer.Ordinal).ToArray()
            };

        internal static SharedBranchKnowledgeAuthority Canonicalize(
            SharedBranchKnowledgeAuthority source) => new SharedBranchKnowledgeAuthority
            {
                Records = (source?.Records ?? Array.Empty<BranchKnowledgeRecord>()).Select(Copy)
                    .OrderBy(value => value?.FloorInstanceId, StringComparer.Ordinal)
                    .ThenBy(value => value?.OptionalBranchId, StringComparer.Ordinal)
                    .ThenBy(value => value?.EdgeId, StringComparer.Ordinal).ToArray()
            };

        internal static void Write(ContractJsonWriter writer, CorridorContentAuthority source)
        {
            CorridorContentAuthority value = Canonicalize(source);
            writer.Node(); writer.Token("{\"Assignments\":[");
            for (int index = 0; index < value.Assignments.Length; index++)
            {
                if (index != 0) writer.Token(",");
                CorridorContentAssignment item = value.Assignments[index]; writer.Node(); writer.Token("{");
                Property(writer, "AssignmentId", item.AssignmentId, true);
                Property(writer, "CategoryId", item.CategoryId); Property(writer, "OptionId", item.OptionId);
                NumberProperty(writer, "Sequence", item.Sequence); Property(writer, "FloorInstanceId", item.FloorInstanceId);
                Property(writer, "OptionalBranchId", item.OptionalBranchId); Property(writer, "EdgeId", item.EdgeId);
                writer.Token(",\"Tile\":{\"X\":"); writer.Token(item.Tile.X.ToString(CultureInfo.InvariantCulture));
                writer.Token(",\"Y\":"); writer.Token(item.Tile.Y.ToString(CultureInfo.InvariantCulture)); writer.Token("}}");
            }
            writer.Token("]}");
        }

        internal static void Write(ContractJsonWriter writer, SharedBranchKnowledgeAuthority source)
        {
            SharedBranchKnowledgeAuthority value = Canonicalize(source);
            writer.Node(); writer.Token("{\"Records\":[");
            for (int index = 0; index < value.Records.Length; index++)
            {
                if (index != 0) writer.Token(",");
                BranchKnowledgeRecord item = value.Records[index]; writer.Node(); writer.Token("{");
                Property(writer, "FloorInstanceId", item.FloorInstanceId, true);
                Property(writer, "OptionalBranchId", item.OptionalBranchId); Property(writer, "EdgeId", item.EdgeId);
                Property(writer, "TopologyFingerprint", item.TopologyFingerprint);
                BoolProperty(writer, "TopologyKnown", item.TopologyKnown); BoolProperty(writer, "IncentiveKnown", item.IncentiveKnown);
                DoubleProperty(writer, "PerceivedIncentive", item.PerceivedIncentive);
                BoolProperty(writer, "DangerKnown", item.DangerKnown); DoubleProperty(writer, "PerceivedDanger", item.PerceivedDanger);
                BoolProperty(writer, "ConfidenceKnown", item.ConfidenceKnown); DoubleProperty(writer, "Confidence", item.Confidence);
                BoolProperty(writer, "HasLastConfirmedRun", item.HasLastConfirmedRun);
                writer.Token(",\"LastConfirmedRunId\":"); if (item.LastConfirmedRunId == null) writer.Token("null"); else writer.String(item.LastConfirmedRunId);
                writer.Token("}");
            }
            writer.Token("]}");
        }

        private static bool CanonicalCorridor(CorridorContentAuthority value)
        {
            CorridorContentAssignment[] canonical = Canonicalize(value).Assignments;
            return value?.Assignments != null && value.Assignments.Length == canonical.Length &&
                value.Assignments.Select(Key).SequenceEqual(canonical.Select(Key), StringComparer.Ordinal);
        }

        private static bool CanonicalKnowledge(SharedBranchKnowledgeAuthority value)
        {
            BranchKnowledgeRecord[] canonical = Canonicalize(value).Records;
            return value?.Records != null && value.Records.Length == canonical.Length &&
                value.Records.Select(Key).SequenceEqual(canonical.Select(Key), StringComparer.Ordinal);
        }

        private static bool TryCountRecords(DetachedCanonicalSpatialSaveState spatial,
            CorridorContentAuthority corridor, SharedBranchKnowledgeAuthority knowledge, int maximum)
        {
            long records = (corridor.Assignments?.LongLength ?? 0L) + (knowledge.Records?.LongLength ?? 0L) +
                (spatial.LifecycleAndOwnership?.Floors?.LongLength ?? 0L) +
                (spatial.LifecycleAndOwnership?.ReturnedContents?.LongLength ?? 0L);
            foreach (SavedSpatialFloor floor in spatial.Floors ?? Array.Empty<SavedSpatialFloor>())
                records += floor == null ? 1L : 1L + (floor.Layout?.Rooms?.LongLength ?? 0L) +
                    (floor.Layout?.Nodes?.LongLength ?? 0L) + (floor.Layout?.Edges?.LongLength ?? 0L) +
                    (floor.FixedStructures?.LongLength ?? 0L) +
                    (floor.RoomContents?.Assignments?.LongLength ?? 0L) +
                    (floor.RoomContents?.RoomSemantics?.LongLength ?? 0L);
            return records >= 0L && records <= maximum;
        }

        private static TileCoordinate TerminalTile(FloorRouteEdge edge, TileCoordinate sourceTile) =>
            (edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>()).OrderBy(value =>
                Manhattan(value, sourceTile)).Last();

        private static int Manhattan(TileCoordinate a, TileCoordinate b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        private static CorridorContentAssignment Copy(CorridorContentAssignment value) => value == null ? null :
            new CorridorContentAssignment { AssignmentId = value.AssignmentId, CategoryId = value.CategoryId,
                OptionId = value.OptionId, Sequence = value.Sequence, FloorInstanceId = value.FloorInstanceId,
                OptionalBranchId = value.OptionalBranchId, EdgeId = value.EdgeId, Tile = value.Tile };
        private static BranchKnowledgeRecord Copy(BranchKnowledgeRecord value) => value == null ? null :
            new BranchKnowledgeRecord { FloorInstanceId = value.FloorInstanceId, OptionalBranchId = value.OptionalBranchId,
                EdgeId = value.EdgeId, TopologyFingerprint = value.TopologyFingerprint, TopologyKnown = value.TopologyKnown,
                IncentiveKnown = value.IncentiveKnown, PerceivedIncentive = value.PerceivedIncentive,
                DangerKnown = value.DangerKnown, PerceivedDanger = value.PerceivedDanger,
                ConfidenceKnown = value.ConfidenceKnown, Confidence = value.Confidence,
                HasLastConfirmedRun = value.HasLastConfirmedRun, LastConfirmedRunId = value.LastConfirmedRunId };
        private static string Key(CorridorContentAssignment value) => value == null ? string.Empty :
            value.FloorInstanceId + "\0" + value.OptionalBranchId + "\0" + value.EdgeId + "\0" +
            value.Tile.X.ToString("D11", CultureInfo.InvariantCulture) + "\0" +
            value.Tile.Y.ToString("D11", CultureInfo.InvariantCulture) + "\0" + Rank(value.CategoryId) + "\0" +
            value.Sequence.ToString("D20", CultureInfo.InvariantCulture) + "\0" + value.AssignmentId;
        private static string Key(BranchKnowledgeRecord value) => value == null ? string.Empty :
            value.FloorInstanceId + "\0" + value.OptionalBranchId + "\0" + value.EdgeId;
        private static int Rank(string category) => category == CanonicalSpatialSaveContracts.TrapCategoryId ? 0 : 1;
        private static bool Bounded(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d && value <= 1d;
        private static bool Persistent(string value)
        {
            if (string.IsNullOrEmpty(value)) return false; bool separator = true;
            foreach (char character in value)
            {
                bool atom = character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
                if (atom) { separator = false; continue; }
                if ((character == '.' || character == '_' || character == '-') && !separator)
                { separator = true; continue; }
                return false;
            }
            return !separator;
        }
        private static bool Members(ContractJsonNode node, params string[] names) => node?.Kind == ContractJsonKind.Object &&
            node.Fields.Select(value => value.Key).SequenceEqual(names);
        private static bool String(ContractJsonNode node, int index) => node.Fields[index].Value.Kind == ContractJsonKind.String;
        private static bool NullableString(ContractJsonNode node, int index) => String(node, index) || node.Fields[index].Value.Kind == ContractJsonKind.Null;
        private static bool Long(ContractJsonNode node, int index, out long value)
        {
            value = 0L;
            return node.Fields[index].Value.Kind == ContractJsonKind.Number &&
                long.TryParse(node.Fields[index].Value.Text, NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out value);
        }
        private static bool Number(ContractJsonNode node, int index, out double value)
        {
            value = 0d;
            return node.Fields[index].Value.Kind == ContractJsonKind.Number &&
                double.TryParse(node.Fields[index].Value.Text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out value) &&
                !double.IsNaN(value) && !double.IsInfinity(value);
        }
        private static bool Boolean(ContractJsonNode node, int index, out bool value) => bool.TryParse(
            node.Fields[index].Value.Text, out value) && node.Fields[index].Value.Kind == ContractJsonKind.Boolean;
        private static bool Tile(ContractJsonNode node, out TileCoordinate value)
        {
            value = default; if (!Members(node, "X", "Y") || !Long(node, 0, out long x) ||
                !Long(node, 1, out long y) || x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) return false;
            value = new TileCoordinate((int)x, (int)y); return true;
        }
        private static bool SameNode(ContractJsonNode node, Action<ContractJsonWriter> write)
        {
            var a = new ContractJsonWriter(new SpatialSerializedInputLimits(int.MaxValue, int.MaxValue,
                int.MaxValue, int.MaxValue, 64)); DetachedCompleteSaveContract.WriteCanonicalNode(a, node);
            var b = new ContractJsonWriter(new SpatialSerializedInputLimits(int.MaxValue, int.MaxValue,
                int.MaxValue, int.MaxValue, 64)); write(b); return a.Finish().SequenceEqual(b.Finish());
        }
        private static void Property(ContractJsonWriter writer, string name, string value, bool first = false)
        { if (!first) writer.Token(","); writer.String(name); writer.Token(":"); writer.String(value); }
        private static void NumberProperty(ContractJsonWriter writer, string name, long value)
        { writer.Token(","); writer.String(name); writer.Token(":"); writer.Token(value.ToString(CultureInfo.InvariantCulture)); }
        private static void DoubleProperty(ContractJsonWriter writer, string name, double value)
        { writer.Token(","); writer.String(name); writer.Token(":"); writer.Token(value.ToString("R", CultureInfo.InvariantCulture)); }
        private static void BoolProperty(ContractJsonWriter writer, string name, bool value)
        { writer.Token(","); writer.String(name); writer.Token(":"); writer.Token(value ? "true" : "false"); }
    }
}
