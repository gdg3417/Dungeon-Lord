using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // An exact transition authorization, never a writable save owner. Player builds have
    // a narrowly scoped C2 foreground factory. Generic/manual history validation cannot opt into this mutation.
    internal sealed class DurableSettlementHistory
    {
        private readonly byte[] predecessor, retiredOwner;
        private readonly string expectedHistory;
        private readonly int maximum;
        private DurableSettlementHistory(byte[] before, byte[] owner, RunHistoryState history, int bound)
        {
            predecessor = (byte[])before.Clone(); retiredOwner = (byte[])owner.Clone();
            expectedHistory = JsonUtility.ToJson(history); maximum = bound;
        }
#if UNITY_EDITOR
        internal static DurableSettlementHistory Create(byte[] before, LivingDungeonState retired,
            RunHistoryState history, int maximum, SpatialSerializedInputLimits limits)
        {
            if (maximum <= 0) throw new InvalidOperationException("living.settlement.history_invalid");
            var writer = new ContractJsonWriter(limits); LivingDungeonContracts.Write(writer, retired);
            return new DurableSettlementHistory(before, writer.Finish(), history, maximum);
        }
#endif
        internal static DurableSettlementHistory CreateForeground(byte[] before, LivingDungeonState retired,
            RunHistoryState history, int maximum, SpatialSerializedInputLimits limits,
            DungeonBuilder.M0.Gameplay.RunSimulation.RunExecution terminal)
        {
            if (maximum <= 0 || terminal == null || !terminal.IsTerminal || !terminal.OutcomeTaken ||
                terminal.Combat?.Kernel.RulesId != MonsterRecoveryRules.Executable ||
                !LivingDungeonForegroundConfig.IsProduction(retired?.Scheduler?.ProfileId))
                throw new InvalidOperationException("living.foreground.provenance_required");
            var writer = new ContractJsonWriter(limits); LivingDungeonContracts.Write(writer, retired);
            return new DurableSettlementHistory(before, writer.Finish(), history, maximum);
        }
        internal bool Matches(byte[] before, LivingDungeonState retired, SpatialSerializedInputLimits limits)
        {
            if (retired == null || !predecessor.SequenceEqual(before)) return false;
            var writer = new ContractJsonWriter(limits); LivingDungeonContracts.Write(writer, retired);
            return retiredOwner.SequenceEqual(writer.Finish());
        }
        internal bool MatchesHistory(byte[] next) => expectedHistory == Encoding.UTF8.GetString(next);
        internal bool HasObjective(byte[] value)
        {
            var objectives = JsonUtility.FromJson<CompletedObjectiveState>(Encoding.UTF8.GetString(value));
            return objectives != null && ((objectives.ObjectiveIds ?? Array.Empty<string>()).Any(id => !string.IsNullOrEmpty(id)) ||
                !string.IsNullOrEmpty(objectives.LastCompletedObjectiveId));
        }

        internal byte[] MergeRecognized(string name, ContractJsonNode prior, byte[] nextBytes, SpatialSerializedInputLimits limits)
        {
            var writer = new ContractJsonWriter(limits);
            if (name != "structureRuntime" && name != "completedObjectives")
            { DetachedCompleteSaveContract.WriteCanonicalNode(writer, prior); return writer.Finish(); }
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(nextBytes, limits, issues, out var next))
                throw new InvalidOperationException("living.settlement.history_invalid");
            if (name == "completedObjectives")
            {
                var priorWriter = new ContractJsonWriter(limits);
                DetachedCompleteSaveContract.WriteCanonicalNode(priorWriter, prior);
                bool same = prior.Kind == ContractJsonKind.Null ? !HasObjective(nextBytes) :
                    JsonUtility.ToJson(JsonUtility.FromJson<CompletedObjectiveState>(Encoding.UTF8.GetString(priorWriter.Finish()))) ==
                    JsonUtility.ToJson(JsonUtility.FromJson<CompletedObjectiveState>(Encoding.UTF8.GetString(nextBytes)));
                if (same) { DetachedCompleteSaveContract.WriteCanonicalNode(writer, prior); return writer.Finish(); }
            }
            bool Changed(string key) => name == "structureRuntime" ? key == "Heat" :
                key == "ObjectiveIds" || key == "LastCompletedObjectiveId" || key == "LastCompletionRuleSourceId";
            if (prior.Kind != ContractJsonKind.Object || next.Kind != ContractJsonKind.Object)
            { DetachedCompleteSaveContract.WriteCanonicalNode(writer, next); return writer.Finish(); }
            writer.Node(); writer.Token("{"); bool first = true;
            foreach (var field in prior.Fields)
            {
                if (!first) writer.Token(","); first = false; writer.String(field.Key); writer.Token(":");
                var replacement = Changed(field.Key) ? next.Fields.SingleOrDefault(f => f.Key == field.Key).Value : null;
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, replacement ?? field.Value);
            }
            foreach (var field in next.Fields.Where(f => Changed(f.Key) && !prior.Fields.Any(p => p.Key == f.Key)))
            {
                if (!first) writer.Token(","); first = false; writer.String(field.Key); writer.Token(":");
                DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
            }
            writer.Token("}"); return writer.Finish();
        }

        internal byte[] Merge(ContractJsonNode prior, ContractJsonNode next, SpatialSerializedInputLimits limits)
        {
            // Retained outcomes can contain extensions/nulls unknown to Unity. Copy their
            // canonical nodes, trimming only the established oldest-first history window.
            var oldRecent = prior.Fields.SingleOrDefault(f => f.Key == "RecentOutcomes").Value;
            var latest = next.Fields.Single(f => f.Key == "LatestOutcome").Value;
            var retained = oldRecent?.Kind == ContractJsonKind.Array
                ? oldRecent.Items.Skip(Math.Max(0, oldRecent.Items.Count + 1 - maximum)).ToArray()
                : Array.Empty<ContractJsonNode>();
            var writer = new ContractJsonWriter(limits); writer.Node(); writer.Token("{");
            bool first = true, hasLatest = false, hasRecent = false;
            void Field(string key, ContractJsonNode value)
            {
                if (!first) writer.Token(","); first = false; writer.String(key); writer.Token(":");
                if (key == "RecentOutcomes")
                {
                    hasRecent = true; writer.Node(); writer.Token("["); bool start = true;
                    foreach (var item in retained.Concat(new[] { latest }))
                    {
                        if (!start) writer.Token(","); start = false;
                        DetachedCompleteSaveContract.WriteCanonicalNode(writer, item);
                    }
                    writer.Token("]");
                }
                else
                {
                    if (key == "LatestOutcome") { hasLatest = true; value = latest; }
                    DetachedCompleteSaveContract.WriteCanonicalNode(writer, value);
                }
            }
            foreach (var field in prior.Fields) Field(field.Key, field.Value);
            if (!hasLatest) Field("LatestOutcome", latest);
            if (!hasRecent) Field("RecentOutcomes", null);
            writer.Token("}"); return writer.Finish();
        }
    }
}
