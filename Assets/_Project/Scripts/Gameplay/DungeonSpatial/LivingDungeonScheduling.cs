using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class LivingDungeonScheduling
    {
        internal const string Invalid = "living.scheduler.invalid_state";
        internal const string Boundary = "living.scheduler.closed_boundary_required";
        internal static void Change(LivingDungeonState state) => state.Revision = checked(state.Revision + 1);
        internal static void Activate(LivingDungeonState state, LivingDungeonProfile profile, long spacing, RunHistoryState history)
        {
            if (!state.IsDormant || spacing <= 0 || !AllocatorValid(history, state, true)) throw new InvalidOperationException(Invalid);
            state.Scheduler = new LivingDungeonScheduler { ProfileId = profile.Id, MinimumOpportunitySpacingUnits = spacing };
            Change(state);
        }
        internal static void UpgradeProfile(LivingDungeonState state, LivingDungeonProfile before, LivingDungeonProfile after)
        {
            if (!state.IsClosed || after.MaximumActive < before.MaximumActive || after.MaximumRetained < before.MaximumRetained ||
                after.MaximumCheckpointBytes < before.MaximumCheckpointBytes || after.MaximumPartyReplay < before.MaximumPartyReplay ||
                after.MaximumTotalReplay < before.MaximumTotalReplay || after.MaximumBaseSaveBytes < before.MaximumBaseSaveBytes ||
                after.MaximumOwnerBytes < before.MaximumOwnerBytes || after.MaximumCombatWork < before.MaximumCombatWork)
                throw new InvalidOperationException(Invalid);
            if (before.Id == after.Id) return;
            state.Scheduler.ProfileId = after.Id; Change(state);
        }
        internal static void Install(LivingDungeonState state, long ordinal, long time)
        {
            var s = state.Scheduler ?? throw new InvalidOperationException(Invalid);
            if (s.NextOpportunityOrdinal == ordinal && s.NextOpportunityTime == time) return;
            if (!state.IsClosed) throw new InvalidOperationException(Boundary);
            if (s.NextOpportunityTime.HasValue || ordinal != s.NextOpportunityOrdinal || ordinal == long.MaxValue ||
                time < 0 || time <= s.ClosedThrough || (s.LastOpportunityTime.HasValue &&
                time < checked(s.LastOpportunityTime.Value + s.MinimumOpportunitySpacingUnits))) throw new InvalidOperationException(Invalid);
            s.NextOpportunityTime = time; Change(state);
        }
        internal static void Accept(LivingDungeonState state, long horizon)
        {
            var s = state.Scheduler ?? throw new InvalidOperationException(Invalid);
            if (horizon < 0 || horizon < s.AcceptedThrough) throw new InvalidOperationException(Invalid);
            if (horizon == s.AcceptedThrough) return;
            if (!state.IsClosed) throw new InvalidOperationException(Boundary);
            s.AcceptedThrough = horizon; Change(state);
        }
        // Allocation validity is deliberately gated on activation, preserving dormant historical behavior.
        internal static bool HistorySequence(ContractJsonNode history, out int sequence)
        {
            sequence = 0;
            if (history?.Kind != ContractJsonKind.Object || history.Fields.Count(f =>
                string.Equals(f.Key, "NextRunSequence", StringComparison.OrdinalIgnoreCase)) != 1 ||
                history.Fields.GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1)) return false;
            foreach (var item in history.Fields)
            {
                bool latest = string.Equals(item.Key, "LatestOutcome", StringComparison.OrdinalIgnoreCase);
                bool recent = string.Equals(item.Key, "RecentOutcomes", StringComparison.OrdinalIgnoreCase);
                if ((latest && item.Key != "LatestOutcome") || (recent && item.Key != "RecentOutcomes")) return false;
                if (latest && item.Value.Kind != ContractJsonKind.Null && !HistoryIdentityShape(item.Value, false)) return false;
                if (recent && item.Value.Kind != ContractJsonKind.Null &&
                    (item.Value.Kind != ContractJsonKind.Array || item.Value.Items.Any(record => !HistoryIdentityShape(record, true)))) return false;
            }
            var field = history.Fields.Single(f => string.Equals(f.Key, "NextRunSequence", StringComparison.OrdinalIgnoreCase));
            return field.Key == "NextRunSequence" && ContractJson.Int(field.Value, out sequence) && sequence > 0 &&
                field.Value.Text == sequence.ToString(CultureInfo.InvariantCulture);
        }
        private static bool HistoryIdentityShape(ContractJsonNode record, bool required)
        {
            if (record.Kind != ContractJsonKind.Object || record.Fields.GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1)) return false;
            var fields = record.Fields.Where(f => string.Equals(f.Key, "RunId", StringComparison.OrdinalIgnoreCase)).ToArray();
            return fields.Length == 0 ? !required : fields.Length == 1 && fields[0].Key == "RunId" && fields[0].Value.Kind == ContractJsonKind.String;
        }
        internal static bool AllocatorValid(RunHistoryState history, LivingDungeonState state, bool requireAvailable)
        {
            if (history == null || history.NextRunSequence <= 0 ||
                (requireAvailable && history.NextRunSequence == int.MaxValue)) return false;
            var recent = history.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>();
            if (recent.Length != 0 && history.LatestOutcome == null) return false;
            var ids = new HashSet<int>();
            foreach (var r in recent)
                if (r == null || !Sequence(r.RunId, out int sequence) || sequence >= history.NextRunSequence || !ids.Add(sequence)) return false;
            // Unity's historical null-class encoding writes an exact default record for an empty latest slot.
            // Interpret only that byte-equivalent default with empty recent history as absence; preserve its bytes.
            bool emptyLatest = (history.RecentOutcomes?.Length ?? 0) == 0 && history.LatestOutcome != null &&
                UnityEngine.JsonUtility.ToJson(history.LatestOutcome) == UnityEngine.JsonUtility.ToJson(new RunOutcomeRecord());
            if (history.LatestOutcome != null && !emptyLatest)
            {
                if (!Sequence(history.LatestOutcome.RunId, out int latest) || latest >= history.NextRunSequence) return false;
                // Latest is an alias only for the final recent record; contradictory aliases are rejected.
                if (recent.Length != 0 && (recent[recent.Length - 1].RunId != history.LatestOutcome.RunId ||
                    UnityEngine.JsonUtility.ToJson(recent[recent.Length - 1]) != UnityEngine.JsonUtility.ToJson(history.LatestOutcome))) return false;
                ids.Add(latest);
            }
            foreach (var p in state.Parties)
                if (p.Sequence >= history.NextRunSequence || !ids.Add(p.Sequence)) return false;
            return true;
        }
        private static bool Sequence(string id, out int sequence)
        {
            sequence = 0;
            return id != null && id.StartsWith("run-", StringComparison.Ordinal) &&
                int.TryParse(id.Substring(4), NumberStyles.None, CultureInfo.InvariantCulture, out sequence) && sequence > 0 &&
                id == "run-" + sequence.ToString(CultureInfo.InvariantCulture);
        }
        internal static void Pin(LivingDungeonParty party, RunExecution execution, LivingDungeonProfile profile)
        {
            var checkpoint = execution.ExportCheckpoint(profile.Checkpoints);
            if (party.StartIdentity != null && party.StartIdentity != checkpoint.StartIdentity) throw new InvalidOperationException(Invalid);
            party.StartIdentity = checkpoint.StartIdentity; party.CheckpointIdentity = checkpoint.Identity;
            party.NextOrdinal = checkpoint.NextOrdinal; party.Checkpoint = checkpoint.GetBytes();
        }
        private static bool FitsLifetimeOwner(int count, LivingDungeonProfile profile)
        {
            // Full per-record reservation prevents a valid checkpoint from becoming unpersistable later.
            long chunks = ((long)profile.MaximumCheckpointBytes + LivingDungeonContracts.BinaryChunkBytes - 1) / LivingDungeonContracts.BinaryChunkBytes;
            long encoded = 4 * (((long)profile.MaximumCheckpointBytes + 2) / 3) + 3 * chunks + 1;
            return checked(count * (encoded + 1024) + 4096) <= profile.MaximumOwnerBytes &&
                checked(count * profile.MaximumPartyReplay) <= profile.MaximumTotalReplay;
        }
        internal static IReadOnlyList<string> Advance(LivingDungeonState state, LivingDungeonProfile profile,
            RunHistoryState history, int budget, Func<int, RunExecution> capture)
        {
            if (budget <= 0 || state.Scheduler == null || !AllocatorValid(history, state, false)) throw new InvalidOperationException(Invalid);
            var s = state.Scheduler;
            if (state.IsClosed) return Array.Empty<string>();
            var executions = new Dictionary<int, RunExecution>(); var trace = new List<string>();
            try
            {
                foreach (var p in state.Parties) executions.Add(p.Sequence, LivingDungeonContracts.Restore(p, profile));
                while (budget-- > 0)
                {
                    LivingDungeonParty selected = null; long earliest = long.MaxValue;
                    foreach (var p in state.Parties)
                    {
                        var due = executions[p.Sequence].NextDueTime();
                        if (!due.HasValue) continue;
                        long time = checked(p.AdmittedAt + due.Value);
                        if (time <= s.AcceptedThrough && (selected == null || time < earliest ||
                            (time == earliest && p.Sequence < selected.Sequence))) { selected = p; earliest = time; }
                    }
                    bool opportunity = s.NextOpportunityTime.HasValue && s.NextOpportunityTime <= s.AcceptedThrough;
                    if (selected != null && (!opportunity || earliest <= s.NextOpportunityTime.Value))
                    {
                        var e = executions[selected.Sequence]; long ordinal = e.NextOrdinal;
                        if (e.AdvanceThrough(earliest - selected.AdmittedAt, 1, ordinal).Length != 1) throw new InvalidOperationException(Invalid);
                        Pin(selected, e, profile); Change(state);
                        trace.Add(earliest + ":run-" + selected.Sequence + ":" + ordinal);
                    }
                    else if (opportunity)
                    {
                        long time = s.NextOpportunityTime.Value; long ordinal = s.NextOpportunityOrdinal;
                        // Terminal records remain retained; no economic consumption or retirement.
                        if (executions.Values.Count(e => !e.IsTerminal) < profile.MaximumActive && state.Parties.Count < profile.MaximumRetained &&
                            AllocatorValid(history, state, true) && FitsLifetimeOwner(state.Parties.Count + 1, profile))
                        {
                            RunExecution newborn = capture(history.NextRunSequence);
                            if (newborn != null)
                            {
                                try
                                {
                                    var party = new LivingDungeonParty { Sequence = history.NextRunSequence, AdmittedAt = time };
                                    if (newborn.NextOrdinal != 0 || newborn.IsTerminal || !newborn.Inspect().HasTiming ||
                                        newborn.Inspect().RunId != "run-" + party.Sequence) throw new InvalidOperationException(Invalid);
                                    Pin(party, newborn, profile);
                                    // Detached admission assurance checks every reachable checkpoint, without publishing effects.
                                    var probe = LivingDungeonContracts.Restore(party, profile);
                                    try
                                    {
                                        while (!probe.IsTerminal)
                                        {
                                            if (probe.NextOrdinal >= profile.MaximumPartyReplay)
                                                throw new InvalidOperationException("run.checkpoint.replay_limit");
                                            probe.AdvanceThrough(probe.NextDueTime().Value, 1, probe.NextOrdinal);
                                            probe.ExportCheckpoint(profile.Checkpoints);
                                        }
                                        long totalBound = checked(probe.NextOrdinal + state.Parties.Sum(p => p.NextOrdinal));
                                        if (totalBound > profile.MaximumTotalReplay) throw new InvalidOperationException(Invalid);
                                    }
                                    finally { probe.Abandon(); }
                                    state.Parties.Add(party); executions.Add(party.Sequence, newborn); newborn = null;
                                    history.NextRunSequence = checked(history.NextRunSequence + 1);
                                }
                                catch (InvalidOperationException error) when (error.Message == "run.checkpoint.byte_limit" ||
                                    error.Message == "run.checkpoint.replay_limit" || error.Message == "run.execution.workload_exceeded")
                                {
                                    // Eligible check consumed without reservation when its lifetime cannot fit the technical profile.
                                }
                                finally { newborn?.Abandon(); }
                            }
                        }
                        s.LastOpportunityTime = time; s.NextOpportunityTime = null;
                        s.NextOpportunityOrdinal = checked(s.NextOpportunityOrdinal + 1); Change(state);
                        trace.Add(time + ":opportunity:" + ordinal);
                    }
                    else
                    {
                        s.ClosedThrough = s.AcceptedThrough; Change(state); trace.Add("close:" + s.ClosedThrough); break;
                    }
                }
                return trace;
            }
            finally { foreach (var e in executions.Values) e.Abandon(); }
        }
    }

    public sealed partial class DetachedCanonicalWriteAuthority
    {
        // Controlled qualification only: no production caller, wall clock, recurring arrival policy or settlement.
        public DetachedCanonicalWriteResult ConfigureLivingDungeon(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, string profileId, long minimumSpacingUnits)
            => MutateLivingDungeon(path, fs, session, (owned, runtime) => {
                var profile = context.LivingDungeonProfiles?.SingleOrDefault(p => p.Id == profileId)
                    ?? throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                if (profile.QualificationTiming == null || string.IsNullOrEmpty(profile.QualificationPostureId) ||
                    !profile.BaseBudget.IsValid || (long)profile.MaximumOwnerBytes + profile.MaximumBaseSaveBytes > limits.Whole.MaximumCandidateBytes)
                    throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                if (owned.LivingDungeon.IsDormant)
                {
                    var issues = new SpatialIssueCollector(context.Limits.Serialized.MaximumDiagnostics);
                    if (!ContractJson.TryParse(owned.GetBytes(), context.Limits.Serialized, issues, out var root) ||
                        !LivingDungeonScheduling.HistorySequence(root.Fields[2].Value.Fields.SingleOrDefault(f => f.Key == "runHistory").Value,
                            out int sequence) || sequence != runtime.runHistory.NextRunSequence)
                        throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                    LivingDungeonScheduling.Activate(owned.LivingDungeon, profile, minimumSpacingUnits, runtime.runHistory);
                }
                else
                {
                    if (owned.LivingDungeon.Scheduler.MinimumOpportunitySpacingUnits != minimumSpacingUnits)
                        throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                    LivingDungeonScheduling.UpgradeProfile(owned.LivingDungeon,
                        LivingDungeonContracts.Profile(owned.LivingDungeon, context.LivingDungeonProfiles), profile);
                }
            });
        public DetachedCanonicalWriteResult InstallLivingDungeonOpportunity(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, long ordinal, long time)
            => MutateLivingDungeon(path, fs, session, (owned, runtime) => LivingDungeonScheduling.Install(owned.LivingDungeon, ordinal, time));
        public DetachedCanonicalWriteResult AcceptLivingDungeonHorizon(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, long horizon)
            => MutateLivingDungeon(path, fs, session, (owned, runtime) => LivingDungeonScheduling.Accept(owned.LivingDungeon, horizon));
        public DetachedCanonicalWriteResult AdvanceLivingDungeon(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, int actionBudget)
            => MutateLivingDungeon(path, fs, session, (owned, runtime) => {
                var simulation = new RunSimulationService(configuration, runLoot);
                var profile = LivingDungeonContracts.Profile(owned.LivingDungeon, context.LivingDungeonProfiles);
                LivingDungeonScheduling.Advance(owned.LivingDungeon, profile, runtime.runHistory, actionBudget, sequence => {
                    if (profile.QualificationTiming == null || string.IsNullOrEmpty(profile.QualificationPostureId))
                        throw new InvalidOperationException(LivingDungeonScheduling.Invalid);
                    if (owned.State.Floors.All(f => f.ActivationState != FloorActivationState.Active)) return null;
                    var snapshot = ActiveFloorRunSnapshot.Create(owned, production, configuration, limits.Canonical,
                        runLoot, null, runtime, profile.QualificationPostureId);
                    return simulation.StartSnapshot(sequence, snapshot, profile.QualificationTiming);
                });
            });
        private DetachedCanonicalWriteResult MutateLivingDungeon(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, Action<DetachedCompleteSaveValidationResult, SaveData> change)
        {
            if (session == null || fs == null || context?.LivingDungeonProfiles == null) return Failure(LivingDungeonScheduling.Invalid);
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fs.ReadAllBytes(path))) return Failure("living.scheduler.stale_session");
                var owned = ValidateSession(session);
                if (owned?.CurrentTargetValidated != true || !owned.IsValid || owned.LivingDungeon == null ||
                    !MvpDungeonPlacements.CanonicalMvpRouteProjection.TryPublishValidated(owned, production, out var runtime, out var reason))
                    return Failure(LivingDungeonScheduling.Invalid);
                long revision = owned.LivingDungeon.Revision;
                change(owned, runtime);
                if (revision == owned.LivingDungeon.Revision)
                    return new DetachedCanonicalWriteResult(true, null, true, false, session.GetCurrentBytes(), session, owned, runtime);
                var captured = DetachedRecognizedSaveStateSnapshot.Capture(runtime, limits);
                // Existing controlled B2 transactions may prove vacancy at a closed
                // boundary. This starts no scheduling and attributes no B1 injury.
                return PrepareAndPersist(path, fs, session, session.PrepareLivingDungeonReplacement(captured, owned.State, owned.LivingDungeon), false, recover: true);
            }
            catch (Exception error) { return Failure(error.Message.StartsWith("living.", StringComparison.Ordinal) ? error.Message : LivingDungeonScheduling.Invalid); }
        }
    }
}
