using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // SaveService's transient working set. Borrow removes its binding before any
    // mutation; only a successful independently read-back result can bind it again.
    // This never supplies validation, settlement authorization or release lineage.
    internal sealed class LivingDungeonForegroundExecutions
    {
        private Dictionary<int, RunExecution> executions = new Dictionary<int, RunExecution>();
        private Binding binding;
        private object epoch = new object();
        internal object Epoch => epoch;
        private sealed class Binding
        {
            internal DetachedCanonicalSaveSession Session;
            internal DetachedCurrentTargetValidationContext Context;
            internal LivingDungeonForegroundConfig Policy;
            internal LivingDungeonProfile Profile;
            internal RunSimulationConfig Configuration;
            internal ProductionSpatialContentSnapshot Production;
            internal LootConfig Loot;
            internal SaveSpatialMigrationLimitsProfile Limits;
        }
        internal int Count => executions.Count;
        internal void InvalidateUnlessBoundTo(DetachedCanonicalSaveSession session)
        {
            if (!ReferenceEquals(binding?.Session, session)) Clear();
        }
        internal void Clear()
        {
            // A reentrant lifecycle callback can invalidate a lent set while the
            // synchronous writer is in IO. Its eventual success cannot undo that.
            epoch = new object();
            var obsolete = executions;
            executions = new Dictionary<int, RunExecution>(); binding = null;
            foreach (var execution in obsolete.Values) execution.Abandon();
            obsolete.Clear();
        }
        internal Dictionary<int, RunExecution> Borrow(DetachedCanonicalSaveSession session,
            DetachedCompleteSaveValidationResult validated, DetachedCurrentTargetValidationContext context,
            LivingDungeonForegroundConfig policy, LivingDungeonProfile profile, RunSimulationConfig configuration,
            ProductionSpatialContentSnapshot production, LootConfig loot, SaveSpatialMigrationLimitsProfile limits)
        {
            var prior = binding;
            if (prior == null || !ReferenceEquals(prior.Session, session) || !ReferenceEquals(prior.Context, context) ||
                !ReferenceEquals(prior.Policy, policy) || !ReferenceEquals(prior.Profile, profile) ||
                !ReferenceEquals(prior.Configuration, configuration) || !ReferenceEquals(prior.Production, production) ||
                !ReferenceEquals(prior.Loot, loot) || !ReferenceEquals(prior.Limits, limits) ||
                !Eligible(session, validated, context, policy, profile)) {
                Clear(); return new Dictionary<int, RunExecution>();
            }
            var borrowed = executions;
            executions = new Dictionary<int, RunExecution>(); binding = null;
            foreach (int sequence in borrowed.Keys.OrderBy(n => n).ToArray()) {
                var party = validated.LivingDungeon.Parties.SingleOrDefault(p => p.Sequence == sequence);
                if (!Matches(borrowed[sequence], party, profile)) {
                    borrowed[sequence].Abandon(); borrowed.Remove(sequence);
                } else {
                    using var reuse = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.RetainedExecutionReuse,
                        ForegroundQualificationMetrics.Origin.Execution, sequence, party.NextOrdinal);
                }
            }
            return borrowed;
        }
        internal void Retain(DetachedCanonicalWriteResult durable, Dictionary<int, RunExecution> working,
            DetachedCurrentTargetValidationContext context, LivingDungeonForegroundConfig policy, LivingDungeonProfile profile,
            RunSimulationConfig configuration, ProductionSpatialContentSnapshot production, LootConfig loot,
            SaveSpatialMigrationLimitsProfile limits, object expectedEpoch)
        {
            bool stillEligible = ReferenceEquals(epoch, expectedEpoch);
            Clear();
            // An optimization failure after a confirmed commit must not turn that
            // commit into an apparent persistence failure. Simply reconstruct later.
            try {
                if (!stillEligible || durable?.IsSuccess != true || !Eligible(durable.Session, durable.Validation, context, policy, profile)) return;
                foreach (var party in durable.Validation.LivingDungeon.Parties.OrderBy(p => p.Sequence)) {
                    if (!working.TryGetValue(party.Sequence, out var execution) || !Matches(execution, party, profile)) continue;
                    executions.Add(party.Sequence, execution); working.Remove(party.Sequence);
                    using var rebound = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.RetainedExecutionRebind,
                        ForegroundQualificationMetrics.Origin.Readback, party.Sequence, party.NextOrdinal);
                }
                binding = new Binding { Session = durable.Session, Context = context, Policy = policy, Profile = profile,
                    Configuration = configuration, Production = production, Loot = loot, Limits = limits };
            } catch { Clear(); }
        }
        private static bool Eligible(DetachedCanonicalSaveSession session, DetachedCompleteSaveValidationResult validated,
            DetachedCurrentTargetValidationContext context, LivingDungeonForegroundConfig policy, LivingDungeonProfile profile)
        {
            var inputs = context?.RecoveryInputs;
            return session != null && validated?.CurrentTargetValidated == true && validated.IsValid &&
                inputs != null && inputs.ConsumedValuesStillBound(context.RoomContentOccupancy) && inputs.SupportsProfile(profile) &&
                (ReferenceEquals(policy, inputs.OneFloor) || ReferenceEquals(policy, inputs.TwoFloor)) &&
                validated.TimeProvenance?.Episode == null && validated.LivingDungeon?.IsClosed == true &&
                validated.LivingDungeon.Scheduler?.ProfileId == profile.Id &&
                validated.LivingDungeon.Parties.Count <= profile.MaximumActive &&
                session.GetCurrentBytes().SequenceEqual(validated.GetBytes());
        }
        private static bool Matches(RunExecution execution, LivingDungeonParty party, LivingDungeonProfile profile)
        {
            try {
                if (party == null || execution.IsTerminal || execution.OutcomeTaken || execution.TransferReleased ||
                    execution.NextOrdinal != party.NextOrdinal || execution.StartIdentity != party.StartIdentity ||
                    execution.Combat?.Kernel.RulesId != MonsterRecoveryRules.Executable ||
                    execution.TransferContract != (profile.Id == LivingDungeonForegroundConfig.TwoFloorProfileId) ||
                    !execution.TimingMatches(profile.QualificationTiming)) return false;
                var checkpoint = execution.ExportCheckpoint(profile.Checkpoints);
                return checkpoint.Identity == party.CheckpointIdentity && checkpoint.GetBytes().SequenceEqual(party.Checkpoint);
            } catch { return false; }
        }
    }
}
