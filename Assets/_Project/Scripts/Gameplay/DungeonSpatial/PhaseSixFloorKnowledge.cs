using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class FloorKnowledgeRecord
    {
        public string FloorInstanceId;
        public string ApplicabilityFingerprint;
        public bool RewardKnown;
        public double PerceivedRewardScore;
        public bool DangerKnown;
        public double PerceivedDangerScore;
        public bool ConfidenceKnown;
        public double Confidence;
        public bool HasLastConfirmedRun;
        public string LastConfirmedRunId;
    }

    [Serializable]
    public sealed class SharedFloorKnowledgeAuthority
    {
        public FloorKnowledgeRecord[] Records = Array.Empty<FloorKnowledgeRecord>();
    }

    public static class FloorKnowledgeApplicability
    {
        public static bool TryCompute(DetachedCanonicalSpatialSaveState state,
            CorridorContentAuthority corridor, CanonicalSpatialSerializationLimits limits,
            string floorInstanceId, out string fingerprint)
        {
            fingerprint = null;
            try
            {
                var serialized = CanonicalSpatialSaveSerializer.SerializeMembers(state, limits);
                if (!serialized.IsValid || corridor?.Assignments == null) return false;
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(serialized.Value.Floors, limits.Serialized, issues,
                    out ContractJsonNode floors) || floors.Kind != ContractJsonKind.Array) return false;
                ContractJsonNode floor = floors.Items.SingleOrDefault(item => item.Kind == ContractJsonKind.Object &&
                    item.Fields[0].Key == "FloorInstanceId" && item.Fields[0].Value.Text == floorInstanceId);
                if (floor == null) return false;
                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{"); bool first = true;
                foreach (var field in floor.Fields)
                {
                    if (field.Key == nameof(SavedSpatialFloor.ActivationState)) continue;
                    if (!first) writer.Token(","); first = false;
                    writer.String(field.Key); writer.Token(":");
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token(",\"CorridorContent\":");
                PhaseFiveSaveContracts.Write(writer, PhaseFiveSaveContracts.Canonicalize(
                    new CorridorContentAuthority { Assignments = corridor.Assignments.Where(
                        item => item != null && item.FloorInstanceId == floorInstanceId).ToArray() }));
                writer.Token("}");
                fingerprint = SpatialContractSha256.Compute(writer.Finish());
                return true;
            }
            catch { return false; }
        }

        public static bool IsApplicable(FloorKnowledgeRecord record,
            DetachedCanonicalSpatialSaveState state, CorridorContentAuthority corridor,
            CanonicalSpatialSerializationLimits limits) => record != null &&
            SpatialContractSha256.IsCanonical(record.ApplicabilityFingerprint) &&
            TryCompute(state, corridor, limits, record.FloorInstanceId, out string current) &&
            string.Equals(current, record.ApplicabilityFingerprint, StringComparison.Ordinal);
    }

    internal static class PhaseSixFloorKnowledge
    {
        internal const string OwnerName = "sharedFloorKnowledge";
        internal static SharedFloorKnowledgeAuthority Empty() => new SharedFloorKnowledgeAuthority();

        internal static SharedFloorKnowledgeAuthority Canonicalize(SharedFloorKnowledgeAuthority source) =>
            new SharedFloorKnowledgeAuthority { Records = (source?.Records ?? Array.Empty<FloorKnowledgeRecord>())
                .Select(Copy).OrderBy(item => item?.FloorInstanceId, StringComparer.Ordinal).ToArray() };

        internal static bool Validate(SharedFloorKnowledgeAuthority authority,
            DetachedCanonicalSpatialSaveState spatial, int maximumFloors)
        {
            if (authority?.Records == null || spatial?.Floors == null ||
                authority.Records.Length > maximumFloors || authority.Records.Length > spatial.Floors.Length)
                return false;
            string previous = null;
            foreach (FloorKnowledgeRecord record in authority.Records)
            {
                if (record == null || !Persistent(record.FloorInstanceId) ||
                    previous != null && string.CompareOrdinal(previous, record.FloorInstanceId) >= 0 ||
                    !SpatialContractSha256.IsCanonical(record.ApplicabilityFingerprint) ||
                    !spatial.Floors.Any(floor => floor != null && floor.FloorInstanceId == record.FloorInstanceId) ||
                    !Score(record.PerceivedRewardScore) || !Score(record.PerceivedDangerScore) ||
                    !record.RewardKnown && record.PerceivedRewardScore != 0d ||
                    !record.DangerKnown && record.PerceivedDangerScore != 0d ||
                    !Unit(record.Confidence) || !record.ConfidenceKnown && record.Confidence != 0d ||
                    record.ConfidenceKnown != (record.RewardKnown || record.DangerKnown) ||
                    record.ConfidenceKnown && record.Confidence <= 0d ||
                    record.HasLastConfirmedRun != record.ConfidenceKnown ||
                    record.HasLastConfirmedRun != !string.IsNullOrEmpty(record.LastConfirmedRunId) ||
                    record.HasLastConfirmedRun && !Persistent(record.LastConfirmedRunId)) return false;
                previous = record.FloorInstanceId;
            }
            return true;
        }

        internal static bool TryRead(ContractJsonNode node, out SharedFloorKnowledgeAuthority authority)
        {
            authority = null;
            if (node?.Kind != ContractJsonKind.Object || node.Fields.Count != 1 ||
                node.Fields[0].Key != "Records" || node.Fields[0].Value.Kind != ContractJsonKind.Array)
                return false;
            if (node.Fields[0].Value.Items.Count >
                DungeonBuilder.M0.Gameplay.RunSimulation.PhaseSixRunConfigValidation.MaximumSupportedActiveFloors)
                return false;
            var records = new List<FloorKnowledgeRecord>();
            foreach (ContractJsonNode item in node.Fields[0].Value.Items)
            {
                string[] names = { "FloorInstanceId", "ApplicabilityFingerprint", "RewardKnown",
                    "PerceivedRewardScore", "DangerKnown", "PerceivedDangerScore", "ConfidenceKnown",
                    "Confidence", "HasLastConfirmedRun", "LastConfirmedRunId" };
                if (item.Kind != ContractJsonKind.Object || item.Fields.Count != names.Length ||
                    !item.Fields.Select(field => field.Key).SequenceEqual(names) ||
                    item.Fields[0].Value.Kind != ContractJsonKind.String ||
                    item.Fields[1].Value.Kind != ContractJsonKind.String ||
                    !Bool(item, 2, out bool rewardKnown) || !Number(item, 3, out double reward) ||
                    !Bool(item, 4, out bool dangerKnown) || !Number(item, 5, out double danger) ||
                    !Bool(item, 6, out bool confidenceKnown) || !Number(item, 7, out double confidence) ||
                    !Bool(item, 8, out bool hasRun) ||
                    item.Fields[9].Value.Kind != ContractJsonKind.Null &&
                    item.Fields[9].Value.Kind != ContractJsonKind.String) return false;
                records.Add(new FloorKnowledgeRecord { FloorInstanceId = item.Fields[0].Value.Text,
                    ApplicabilityFingerprint = item.Fields[1].Value.Text, RewardKnown = rewardKnown,
                    PerceivedRewardScore = reward, DangerKnown = dangerKnown,
                    PerceivedDangerScore = danger, ConfidenceKnown = confidenceKnown,
                    Confidence = confidence, HasLastConfirmedRun = hasRun,
                    LastConfirmedRunId = item.Fields[9].Value.Kind == ContractJsonKind.Null ? null : item.Fields[9].Value.Text });
            }
            authority = new SharedFloorKnowledgeAuthority { Records = records.ToArray() };
            var writer = new ContractJsonWriter(new SpatialSerializedInputLimits(int.MaxValue,
                int.MaxValue, int.MaxValue, int.MaxValue, 64));
            Write(writer, authority);
            var source = new ContractJsonWriter(new SpatialSerializedInputLimits(int.MaxValue,
                int.MaxValue, int.MaxValue, int.MaxValue, 64));
            DetachedCompleteSaveContract.WriteCanonicalNode(source, node);
            return source.Finish().SequenceEqual(writer.Finish());
        }

        internal static void Write(ContractJsonWriter writer, SharedFloorKnowledgeAuthority source)
        {
            if (source?.Records == null || source.Records.Length >
                DungeonBuilder.M0.Gameplay.RunSimulation.PhaseSixRunConfigValidation.MaximumSupportedActiveFloors)
                throw new ArgumentException("floor.knowledge.invalid_state");
            writer.Node(); writer.Token("{\"Records\":[");
            FloorKnowledgeRecord[] records = Canonicalize(source).Records;
            for (int index = 0; index < records.Length; index++)
            {
                if (index != 0) writer.Token(",");
                FloorKnowledgeRecord item = records[index];
                writer.Node(); writer.Token("{");
                Property(writer, "FloorInstanceId", item.FloorInstanceId, true);
                Property(writer, "ApplicabilityFingerprint", item.ApplicabilityFingerprint);
                Boolean(writer, "RewardKnown", item.RewardKnown);
                Double(writer, "PerceivedRewardScore", item.PerceivedRewardScore);
                Boolean(writer, "DangerKnown", item.DangerKnown);
                Double(writer, "PerceivedDangerScore", item.PerceivedDangerScore);
                Boolean(writer, "ConfidenceKnown", item.ConfidenceKnown);
                Double(writer, "Confidence", item.Confidence);
                Boolean(writer, "HasLastConfirmedRun", item.HasLastConfirmedRun);
                writer.Token(",\"LastConfirmedRunId\":");
                if (item.LastConfirmedRunId == null) writer.Token("null"); else writer.String(item.LastConfirmedRunId);
                writer.Token("}");
            }
            writer.Token("]}");
        }

        internal static FloorKnowledgeRecord Copy(FloorKnowledgeRecord record) => record == null ? null :
            new FloorKnowledgeRecord { FloorInstanceId = record.FloorInstanceId,
                ApplicabilityFingerprint = record.ApplicabilityFingerprint, RewardKnown = record.RewardKnown,
                PerceivedRewardScore = record.PerceivedRewardScore, DangerKnown = record.DangerKnown,
                PerceivedDangerScore = record.PerceivedDangerScore, ConfidenceKnown = record.ConfidenceKnown,
                Confidence = record.Confidence, HasLastConfirmedRun = record.HasLastConfirmedRun,
                LastConfirmedRunId = record.LastConfirmedRunId };

        private static bool Score(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        private static bool Unit(double value) => Score(value) && value <= 1d;
        private static bool Persistent(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                if (character >= 'a' && character <= 'z' || character >= '0' && character <= '9')
                { separator = false; continue; }
                if ((character == '.' || character == '_' || character == '-') && !separator)
                { separator = true; continue; }
                return false;
            }
            return !separator;
        }
        private static bool Bool(ContractJsonNode node, int index, out bool result)
        {
            result = false;
            return node.Fields[index].Value.Kind == ContractJsonKind.Boolean &&
                bool.TryParse(node.Fields[index].Value.Text, out result);
        }
        private static bool Number(ContractJsonNode node, int index, out double result)
        {
            result = 0d;
            return node.Fields[index].Value.Kind == ContractJsonKind.Number &&
                double.TryParse(node.Fields[index].Value.Text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out result) && !double.IsNaN(result) && !double.IsInfinity(result);
        }
        private static void Property(ContractJsonWriter writer, string name, string value, bool first = false)
        { if (!first) writer.Token(","); writer.String(name); writer.Token(":"); writer.String(value); }
        private static void Boolean(ContractJsonWriter writer, string name, bool value)
        { writer.Token(","); writer.String(name); writer.Token(":"); writer.Token(value ? "true" : "false"); }
        private static void Double(ContractJsonWriter writer, string name, double value)
        { writer.Token(","); writer.String(name); writer.Token(":"); writer.Token(value.ToString("R", CultureInfo.InvariantCulture)); }
    }
}
