using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal sealed class ResearchTimingRule
    {
        internal const long MaximumExactUnits = (1L << 53) - 1;
        internal string Reference { get; }
        internal string ProjectId { get; }
        internal string SlotId { get; }
        internal long ManaCost { get; }
        internal long RequiredUnits { get; }

        internal ResearchTimingRule(string reference, string projectId, string slotId, long manaCost, long requiredUnits)
        {
            Reference = reference;
            ProjectId = projectId;
            SlotId = slotId;
            ManaCost = manaCost;
            RequiredUnits = requiredUnits;
        }

        internal long EarnedThrough(long start, long closed, long tickWidth) =>
            Math.Min(RequiredUnits, Math.Max(0, closed / tickWidth * tickWidth - start));
    }

    // Release entries and enabled selections are independent. A saved reference
    // resolves by key and exact byte hash, regardless of the current selection.
    internal sealed class ResearchTimingRules
    {
        internal const string Invalid = "research.timing.rule_invalid";
        internal const string Missing = "research.timing.rule_missing";
        internal const string Mismatch = "research.timing.rule_mismatch";
        internal const string Disabled = "research.timing.start_disabled";
        internal const string StateInvalid = "research.timing.state_invalid";

        private static readonly string[] RuleFields =
            { "formatVersion", "releaseKey", "projectId", "slotId", "manaCost", "requiredEligibleSeconds" };
        private static readonly Regex ReleaseKey = new Regex(
            @"\Aresearch\.timing\.[a-z][a-z0-9_]{0,31}\.v[1-9][0-9]{0,9}\z", RegexOptions.CultureInvariant);
        private readonly Dictionary<string, ResearchTimingRule> archive = new Dictionary<string, ResearchTimingRule>(StringComparer.Ordinal);
        private readonly Dictionary<string, ResearchTimingRule> selections = new Dictionary<string, ResearchTimingRule>(StringComparer.Ordinal);

        internal static bool IsPaidReference(string reference) =>
            reference?.StartsWith("research.timing.", StringComparison.Ordinal) == true;

        internal ResearchTimingRules(IEnumerable<byte[]> releasedRules, IEnumerable<string> enabledReferences,
            int unitsPerSecond, SpatialSerializedInputLimits limits)
        {
            if (unitsPerSecond <= 0) throw new ArgumentException(Invalid);
            foreach (var source in releasedRules) {
                byte[] bytes = source == null ? null : (byte[])source.Clone();
                var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits, issues, out var node) ||
                    !ContractJson.ValidateShape(node, RuleFields, issues) ||
                    !RecoveryEpisode.TryInteger(node.Fields[0].Value, out long format) || format != 1 ||
                    !ContractJson.String(node.Fields[1].Value, out string key) || !ValidKey(key) ||
                    !ContractJson.String(node.Fields[2].Value, out string project) || string.IsNullOrWhiteSpace(project) ||
                    !ContractJson.String(node.Fields[3].Value, out string slot) || string.IsNullOrWhiteSpace(slot) ||
                    !RecoveryEpisode.TryInteger(node.Fields[4].Value, out long cost) || cost <= 0 ||
                    cost > ResearchTimingRule.MaximumExactUnits ||
                    !RecoveryEpisode.TryInteger(node.Fields[5].Value, out long seconds) || seconds <= 0 ||
                    seconds > ResearchTimingRule.MaximumExactUnits / unitsPerSecond)
                    throw new ArgumentException(Invalid);
                var writer = new ContractJsonWriter(limits);
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, node);
                if (!bytes.AsSpan().SequenceEqual(writer.Finish()) || archive.ContainsKey(key))
                    throw new ArgumentException(Invalid);
                archive.Add(key, new ResearchTimingRule(key + "." + SpatialContractSha256.Compute(bytes),
                    project, slot, cost, seconds * unitsPerSecond));
            }
            foreach (string reference in enabledReferences) {
                string reason = Resolve(reference, out var rule);
                if (reason != null) throw new ArgumentException(reason);
                if (selections.ContainsKey(rule.ProjectId)) throw new ArgumentException(Invalid);
                selections.Add(rule.ProjectId, rule);
            }
        }

        private static bool ValidKey(string key) => key != null && key.Length <= 60 &&
            ReleaseKey.IsMatch(key) && int.TryParse(key.Substring(key.LastIndexOf('.') + 2), out _);

        internal string Resolve(string reference, out ResearchTimingRule rule)
        {
            rule = null;
            if (reference == null || reference.Length < 65 || reference.Length > 125) return Invalid;
            int separator = reference.Length - 65;
            string key = reference.Substring(0, separator);
            if (reference[separator] != '.' || !ValidKey(key) ||
                !SpatialContractSha256.IsCanonical(reference.Substring(separator + 1))) return Invalid;
            if (!archive.TryGetValue(key, out var archived)) return Missing;
            if (reference != archived.Reference) return Mismatch;
            rule = archived;
            return null;
        }

        internal string Select(string project, out ResearchTimingRule rule)
        {
            rule = null;
            return project != null && selections.TryGetValue(project, out rule) ? null : Disabled;
        }
    }
}
