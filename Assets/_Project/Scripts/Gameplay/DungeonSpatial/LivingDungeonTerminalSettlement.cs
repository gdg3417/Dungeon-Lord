#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class LivingDungeonTerminalSettlement
    {
        internal const string Invalid = "living.settlement.invalid";
        internal const string Ineligible = "living.settlement.foreground_provenance_required";
        internal const string Stale = "living.settlement.stale_authorization";
        internal const string NoTerminal = "living.settlement.no_terminal";
        internal const string Recovery = "living.settlement.recovery_required";

        // Selection precedes eligibility. Every party was already bounded/validated by
        // the complete-save reader; independently prove the actual terminal boundary.
        internal static LivingDungeonParty Select(LivingDungeonState owner, LivingDungeonProfile profile)
        {
            if (owner?.Scheduler == null || !owner.IsClosed) throw new InvalidOperationException(LivingDungeonScheduling.Boundary);
            LivingDungeonParty selected = null; long earliest = 0, terminalOrdinal = 0;
            foreach (var party in owner.Parties)
            {
                var execution = LivingDungeonContracts.Restore(party, profile);
                try
                {
                    if (execution.Combat != null) throw new InvalidOperationException(Ineligible);
                    if (!execution.IsTerminal) continue;
                    var view = execution.Inspect(); var last = view.LastStep;
                    if (execution.OutcomeTaken || !last.HasValue || last.Value.Kind != RunStepKind.Terminal ||
                        last.Value.RunId != "run-" + party.Sequence.ToString(CultureInfo.InvariantCulture) ||
                        view.RunId != last.Value.RunId || checked(last.Value.Ordinal + 1) != party.NextOrdinal ||
                        view.NextOrdinal != party.NextOrdinal || last.Value.LogicalTime != view.LogicalTime)
                        throw new InvalidOperationException(Invalid);
                    long time = checked(party.AdmittedAt + view.LogicalTime);
                    if (time > owner.Scheduler.ClosedThrough) throw new InvalidOperationException(Invalid);
                    if (selected == null || time < earliest || time == earliest &&
                        (party.Sequence < selected.Sequence || party.Sequence == selected.Sequence && last.Value.Ordinal < terminalOrdinal))
                    { selected = party; earliest = time; terminalOrdinal = last.Value.Ordinal; }
                }
                finally { execution.Abandon(); }
            }
            return selected;
        }

        internal static void Apply(SaveData candidate, DetachedCompleteSaveValidationResult owned,
            RunExecution execution, RunOutcomeRecord outcome, RunSimulationConfig currentConfig,
            ProductionSpatialContentSnapshot production, CanonicalSpatialSerializationLimits limits)
        {
            if (execution.Combat != null || CombatC0Diagnostics.IsCombatC2(outcome))
                throw new InvalidOperationException(Invalid);
            LivingDungeonOutcomeConsequences.Apply(candidate, owned, execution, outcome, currentConfig, production, limits);
        }
    }

    public sealed partial class DetachedCanonicalWriteAuthority
    {
        // Entire capability/entry point is excluded from player compilation. There is no
        // foreground boolean, SaveService/GameRoot adapter, persistent evidence or UI.
        internal sealed class SyntheticForegroundQualification
        {
            internal sealed class Request
            {
                internal readonly int Sequence;
                internal readonly string Checkpoint;
                internal Request(int sequence, string checkpoint) { Sequence = sequence; Checkpoint = checkpoint; }
            }
            private readonly DetachedCanonicalWriteAuthority authority;
            private byte[] predecessor;
            private readonly Dictionary<int, string> qualified = new Dictionary<int, string>();
            private Request pending;
            private bool recoveryRequired;
            internal SyntheticForegroundQualification(DetachedCanonicalWriteAuthority authority, DetachedCanonicalSaveSession session)
            {
                this.authority = authority; predecessor = session.GetCurrentBytes();
                if (authority.context?.LivingDungeonProfiles == null || authority.ValidateSession(session)?.CurrentTargetValidated != true)
                    throw new InvalidOperationException(LivingDungeonTerminalSettlement.Ineligible);
                // Existing parties are deliberately not adopted, even terminal ones.
            }
            private void Require(DetachedCanonicalSaveSession session)
            {
                if (recoveryRequired) throw new InvalidOperationException(LivingDungeonTerminalSettlement.Recovery);
                if (session == null || !predecessor.SequenceEqual(session.GetCurrentBytes()))
                    throw new InvalidOperationException(LivingDungeonTerminalSettlement.Stale);
            }
            private DetachedCanonicalWriteResult Track(DetachedCanonicalSaveSession session,
                Func<DetachedCanonicalWriteResult> operation, bool progression = false)
            {
                try
                {
                    Require(session);
                    var before = authority.ValidateSession(session).LivingDungeon;
                    var result = operation();
                    if (result.Reason == RecoveryRequiredReason) recoveryRequired = true;
                    if (!result.IsSuccess || result.IsNoOp) return result;
                    var after = result.Validation.LivingDungeon;
                    if (progression)
                        foreach (var party in after.Parties)
                            if (qualified.ContainsKey(party.Sequence) || !before.Parties.Any(p => p.Sequence == party.Sequence))
                                qualified[party.Sequence] = party.CheckpointIdentity;
                    foreach (int sequence in qualified.Keys.Where(sequence => !after.Parties.Any(p => p.Sequence == sequence)).ToArray())
                        qualified.Remove(sequence);
                    // Every successful operation advances the exact session binding. An
                    // unobserved write invalidates this scope instead of blessing a suffix.
                    predecessor = result.Session.GetCurrentBytes(); pending = null;
                    return result;
                }
                catch (Exception e) { return Failure(Reason(e)); }
            }
            internal DetachedCanonicalWriteResult Configure(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, string profile, long spacing) =>
                Track(session, () => authority.ConfigureLivingDungeon(path, fs, session, profile, spacing));
            internal DetachedCanonicalWriteResult Install(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, long ordinal, long time) =>
                Track(session, () => authority.InstallLivingDungeonOpportunity(path, fs, session, ordinal, time));
            internal DetachedCanonicalWriteResult Accept(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, long horizon) =>
                Track(session, () => authority.AcceptLivingDungeonHorizon(path, fs, session, horizon));
            internal DetachedCanonicalWriteResult Advance(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, int budget) =>
                Track(session, () => authority.AdvanceLivingDungeon(path, fs, session, budget), true);
            internal DetachedCanonicalWriteResult Save(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, SaveData runtime) =>
                Track(session, () => authority.SaveRecognizedState(path, fs, session, runtime));
            internal DetachedCanonicalWriteResult Edit(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, SaveData runtime, DetachedCanonicalMutationRequest mutation) =>
                Track(session, () => authority.Execute(path, fs, session, runtime, mutation));
            internal DetachedCanonicalWriteResult Manual(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, SaveData runtime, string posture, long savedUtc) =>
                Track(session, () => authority.CommitPhaseFiveBRun(path, fs, session, runtime,
                    new RunSimulationService(authority.configuration, authority.runLoot), posture, savedUtc));
            internal Request Prepare(DetachedCanonicalSaveSession session)
            {
                Require(session);
                var owned = authority.ValidateSession(session);
                if (owned?.IsValid != true) throw new InvalidOperationException(LivingDungeonTerminalSettlement.Invalid);
                var selected = LivingDungeonTerminalSettlement.Select(owned.LivingDungeon,
                    LivingDungeonContracts.Profile(owned.LivingDungeon, authority.context.LivingDungeonProfiles));
                if (selected == null) throw new InvalidOperationException(LivingDungeonTerminalSettlement.NoTerminal);
                if (!qualified.TryGetValue(selected.Sequence, out string checkpoint) || checkpoint != selected.CheckpointIdentity)
                    throw new InvalidOperationException(LivingDungeonTerminalSettlement.Ineligible);
                return pending = new Request(selected.Sequence, checkpoint);
            }
            internal DetachedCanonicalWriteResult Settle(string path, ISpatialMigrationFileSystem fs,
                DetachedCanonicalSaveSession session, Request request) => Track(session,
                    () => authority.SettleSyntheticTerminal(path, fs, session, this, request));
            internal bool Authorizes(DetachedCanonicalWriteAuthority writer, DetachedCanonicalSaveSession session,
                Request request, LivingDungeonParty selected)
            {
                Require(session);
                return ReferenceEquals(writer, authority) && request != null && ReferenceEquals(request, pending) &&
                    request.Sequence == selected.Sequence && request.Checkpoint == selected.CheckpointIdentity &&
                    qualified.TryGetValue(selected.Sequence, out string checkpoint) && checkpoint == selected.CheckpointIdentity;
            }
        }
        private static string Reason(Exception error) => error.Message.StartsWith("living.", StringComparison.Ordinal)
            ? error.Message : LivingDungeonTerminalSettlement.Invalid;

        private DetachedCanonicalWriteResult SettleSyntheticTerminal(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, SyntheticForegroundQualification qualification,
            SyntheticForegroundQualification.Request request)
        {
            RunExecution execution = null;
            try
            {
                if (session == null || fs == null || context?.LivingDungeonProfiles == null)
                    return Failure(LivingDungeonTerminalSettlement.Ineligible);
                if (!session.GetCurrentBytes().SequenceEqual(fs.ReadAllBytes(path)))
                    return Failure(LivingDungeonTerminalSettlement.Stale);
                var owned = ValidateSession(session);
                if (owned?.CurrentTargetValidated != true || !owned.IsValid ||
                    !CanonicalMvpRouteProjection.TryPublishValidated(owned, production, out var candidate, out _))
                    return Failure(LivingDungeonTerminalSettlement.Invalid);
                var profile = LivingDungeonContracts.Profile(owned.LivingDungeon, context.LivingDungeonProfiles);
                var selected = LivingDungeonTerminalSettlement.Select(owned.LivingDungeon, profile);
                if (selected == null) return Failure(LivingDungeonTerminalSettlement.NoTerminal);
                if (qualification == null || !qualification.Authorizes(this, session, request, selected))
                    return Failure(LivingDungeonTerminalSettlement.Stale);
                execution = LivingDungeonContracts.Restore(selected, profile);
                string runId = execution.Inspect().RunId;
                if (candidate.runHistory.LatestOutcome?.RunId == runId ||
                    (candidate.runHistory.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>()).Any(o => o?.RunId == runId))
                    return Failure(LivingDungeonTerminalSettlement.Invalid);
                var outcome = execution.TakeOutcome();
                LivingDungeonTerminalSettlement.Apply(candidate, owned, execution, outcome, configuration, production, limits.Canonical);
                // Single-settlement rule: one removal + its consequences is one checked
                // semantic revision. Scheduler and every other checkpoint remain exact.
                owned.LivingDungeon.Parties.Remove(selected);
                owned.LivingDungeon.Revision = checked(owned.LivingDungeon.Revision + 1);
                var history = DurableSettlementHistory.Create(session.GetCurrentBytes(), owned.LivingDungeon,
                    candidate.runHistory, configuration.MaxRunHistoryEntries, limits.Canonical.Serialized);
                var captured = DetachedRecognizedSaveStateSnapshot.Capture(candidate, limits);
                return PrepareAndPersist(path, fs, session, session.PrepareDurableSettlementReplacement(captured,
                    owned, owned.LivingDungeon, candidate.sharedBranchKnowledge, candidate.sharedFloorKnowledge, history), false, recover: true);
            }
            catch (Exception error) { return Failure(Reason(error)); }
            finally { execution?.Abandon(); }
        }
    }
}
#endif
