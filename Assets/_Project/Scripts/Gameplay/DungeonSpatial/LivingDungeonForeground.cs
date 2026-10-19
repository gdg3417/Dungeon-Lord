using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed partial class DetachedCanonicalWriteAuthority
    {
        // Only the foreground adapter calls this producer. Every stage is detached;
        // one final exact predecessor-to-candidate write publishes the whole pump.
        internal DetachedCanonicalWriteResult AdvanceForeground(string path, ISpatialMigrationFileSystem fs,
            DetachedCanonicalSaveSession session, SaveData current, LivingDungeonForegroundConfig policy,
            long observedThrough, int tickSeconds, Action<SaveData, long> passiveTick, long savedUtcUnix)
        {
            const string invalid = "living.foreground.invalid_state";
            try {
                if (policy == null || current == null || session == null || fs == null || observedThrough < 0 || tickSeconds <= 0 || passiveTick == null ||
                    !session.GetCurrentBytes().SequenceEqual(fs.ReadAllBytes(path))) return Failure(invalid);
                var profile = context.LivingDungeonProfiles.SingleOrDefault(p => p.Id == LivingDungeonForegroundConfig.ProfileId);
                if (profile == null) return Failure("living.foreground.provenance_required");
                var working = session; var owned = ValidateSession(working);
                if (owned?.CurrentTargetValidated != true || !CanonicalEqual(current.validatedCanonicalSpatialState, owned.State)) return Failure(invalid);
                var runtime = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(current));
                if (current.researchPending == null) runtime.researchPending = null;
                if (current.researchProgress == null) runtime.researchProgress = null;
                if (current.lastOfflineSummary == null) runtime.lastOfflineSummary = null;
                RunTransientEvidence.Retain(current, runtime);
                runtime.lastSavedUtcUnix = savedUtcUnix;
                var owner = owned.LivingDungeon;
                string admissionReason = null;
                double recoveredManaSpent = 0;
                if (!owner.IsDormant && owner.Scheduler.ProfileId != profile.Id) return Failure("living.foreground.provenance_required");
                runtime.monsterContinuity = owned.MonsterContinuity.Copy();
                runtime.validatedCanonicalSpatialState = owned.State; runtime.canonicalSpatialAuthority = owned.State.Authority; runtime.spatialFloors = owned.State.Floors;
                runtime.corridorContent = owned.CorridorContent; runtime.sharedBranchKnowledge = owned.BranchKnowledge; runtime.sharedFloorKnowledge = owned.FloorKnowledge;

                void Stage(MonsterContinuityState monsters = null, DurableSettlementHistory history = null, bool knowledge = false)
                {
                    var prepared = working.PrepareForegroundReplacement(DetachedRecognizedSaveStateSnapshot.Capture(runtime, limits),
                        owned, owner, monsters ?? runtime.monsterContinuity,
                        knowledge ? runtime.sharedBranchKnowledge : null, knowledge ? runtime.sharedFloorKnowledge : null, history);
                    if (!prepared.IsSuccess) throw new InvalidOperationException(prepared.Reason);
                    var next = DetachedCanonicalSaveSession.Open(prepared.Update.GetBytes(), context, limits);
                    if (!next.IsSuccess) throw new InvalidOperationException(next.Reason);
                    working = next.Session; owned = ValidateSession(working);
                    if (!CanonicalMvpRouteProjection.TryPublishValidated(owned, production, out var projection, out _)) throw new InvalidOperationException(invalid);
                    RunTransientEvidence.Retain(runtime, projection); runtime = projection; owner = owned.LivingDungeon;
                }

                if (owner.IsDormant) LivingDungeonScheduling.Activate(owner, profile, policy.MinimumArrival, runtime.runHistory);
                bool previousHorizonClosed = owner.IsClosed;
                if (!LivingDungeonScheduling.AllocatorValid(runtime.runHistory, owner, false) || observedThrough < owner.Scheduler.AcceptedThrough)
                    return Failure(invalid);
                if (owner.IsClosed && owner.Scheduler.AcceptedThrough != observedThrough) LivingDungeonScheduling.Accept(owner, observedThrough);
                Stage(); // accepted time, not any encounter consequence
                long tickUnits = checked((long)tickSeconds * policy.UnitsPerSecond);
                int boundaryEvents = 0;

                long? NextEvent()
                {
                    long? earliest = null;
                    foreach (var p in owner.Parties) {
                        var e = LivingDungeonContracts.Restore(p, profile);
                        try {
                            if (e.IsTerminal) throw new InvalidOperationException("living.foreground.unsettled_terminal");
                            long due = checked(p.AdmittedAt + e.NextDueTime().Value);
                            if (!earliest.HasValue || due < earliest.Value) earliest = due;
                        } finally { e.Abandon(); }
                    }
                    return earliest;
                }

                void ProcessParties(long time)
                {
                    foreach (int sequence in owner.Parties.Select(p => p.Sequence).ToArray()) {
                        var p = owner.Parties.Single(party => party.Sequence == sequence);
                        var e = LivingDungeonContracts.Restore(p, profile);
                        try {
                            while (!e.IsTerminal && checked(p.AdmittedAt + e.NextDueTime().Value) <= time) {
                                if (++boundaryEvents > policy.MaximumBoundaryEvents) throw new InvalidOperationException("living.foreground.boundary_limit");
                                e.AdvanceThrough(checked(time - p.AdmittedAt), 1, e.NextOrdinal);
                            }
                            if (e.IsTerminal) {
                                var outcome = e.TakeOutcome();
                                string runId = outcome.RunId;
                                if (runtime.runHistory.LatestOutcome?.RunId == runId ||
                                    (runtime.runHistory.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>()).Any(r => r?.RunId == runId)) throw new InvalidOperationException(invalid);
                                var reconciled = LivingDungeonForegroundReconciliation.Apply(e, outcome, runtime.monsterContinuity, owned.State, context.MonsterRecovery);
                                LivingDungeonOutcomeConsequences.Apply(runtime, owned, e, outcome, configuration, production, limits.Canonical);
                                runtime.monsterContinuity = reconciled;
                                owner.Parties.Remove(p); LivingDungeonScheduling.Change(owner);
                                var authorization = DurableSettlementHistory.CreateForeground(working.GetCurrentBytes(), owner,
                                    runtime.runHistory, configuration.MaxRunHistoryEntries, limits.Canonical.Serialized, e);
                                Stage(reconciled, authorization, true);
                            } else LivingDungeonScheduling.Pin(p, e, profile);
                        } finally { e.Abandon(); }
                    }
                }

                long DemandInterval()
                {
                    var active = owned.State.Floors.Where(f => f.ActivationState == FloorActivationState.Active).ToArray();
                    if (active.Length != 1 || active[0].FloorIndex != 0 || !active[0].RoomContents.Assignments.Any(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId)) return 0;
                    var effects = MvpPlacementEffectsResolver.ResolveConfiguredRouteForSave(runtime, configuration, production);
                    var tier = CurrentHeatTierResolver.Resolve(configuration, runtime.structureRuntime.Heat);
                    var arrival = AdventurerArrivalPressureResolver.Resolve(configuration, effects, tier, runtime.runHistory.LatestOutcome);
                    var intent = AdventurerRunIntentResolver.Resolve(configuration, effects, runtime.structureRuntime.Heat, tier, runtime.runHistory.LatestOutcome);
                    var traffic = AdventurerTrafficPressureResolver.Resolve(configuration, arrival, intent);
                    return arrival.RuleResolved && traffic.RuleResolved && arrival.PathComplete && arrival.LootSignal > 0 ? policy.Interval(traffic.TrafficBandId) : 0;
                }

                void Recover()
                {
                    // Terminal records were reconciled/retired before deriving vacancy.
                    var healed = runtime.monsterContinuity.Copy();
                    double wallet = MonsterContinuity.Recover(healed, owned.State, context.MonsterRecovery,
                        runtime.structureRuntime.ManaReserve, OccupiedFloors(owned));
                    recoveredManaSpent += runtime.structureRuntime.ManaReserve - wallet;
                    runtime.structureRuntime.ManaReserve = wallet; runtime.monsterContinuity = healed;
                    Stage(healed);
                }

                // Activation and an edit are authoritative boundaries too. Apply
                // already affordable recovery before a later passive tick, even
                // when the first observed foreground sample spans many ticks.
                if (previousHorizonClosed) Recover();

                // Anchor eligibility to committed activation/edit time, never a render frame.
                if (!owner.Scheduler.NextOpportunityTime.HasValue) {
                    long interval = DemandInterval();
                    if (interval > 0) {
                        owner.Scheduler.NextOpportunityTime = checked(Math.Max(0, owner.Scheduler.ClosedThrough) + interval);
                        LivingDungeonScheduling.Change(owner); Stage();
                    }
                }
                bool initial = true;
                for (int boundary = 0; boundary < policy.MaximumBoundaries; boundary++) {
                    long closed = owner.Scheduler.ClosedThrough, target = owner.Scheduler.AcceptedThrough;
                    if (!initial && closed == target) break;
                    initial = false; boundaryEvents = 0;
                    long nextTick = checked((Math.Max(0, closed) / tickUnits + 1) * tickUnits);
                    long time = Math.Min(target, nextTick);
                    var due = NextEvent(); if (due.HasValue) time = Math.Min(time, due.Value);
                    if (owner.Scheduler.NextOpportunityTime.HasValue) time = Math.Min(time, owner.Scheduler.NextOpportunityTime.Value);
                    if (time < closed || time < 0) throw new InvalidOperationException(invalid);
                    if (time == nextTick) {
                        runtime.totalTicks = checked(runtime.totalTicks + 1);
                        passiveTick(runtime, runtime.totalTicks);
                        Stage();
                    }
                    ProcessParties(time);
                    Stage(); // retain progression before occupancy/recovery inspection
                    Recover();
                    if (owner.Scheduler.NextOpportunityTime == time) {
                        owner.Scheduler.NextOpportunityTime = null; owner.Scheduler.LastOpportunityTime = time;
                        owner.Scheduler.NextOpportunityOrdinal = checked(owner.Scheduler.NextOpportunityOrdinal + 1);
                        LivingDungeonScheduling.Change(owner);
                        long interval = DemandInterval();
                        if (owner.Parties.Count < policy.Capacity && interval > 0 && runtime.runHistory.NextRunSequence < int.MaxValue) {
                            try {
                            var snapshot = ActiveFloorRunSnapshot.Create(owned, production, configuration, limits.Canonical,
                                runLoot, null, runtime, RunPostureResolver.BalancedId);
                            var execution = new RunSimulationService(configuration, runLoot).StartTimedCombatSnapshot(
                                runtime.runHistory.NextRunSequence, snapshot, policy.Timing);
                            try {
                                var newborn = new LivingDungeonParty { Sequence = runtime.runHistory.NextRunSequence, AdmittedAt = time };
                                LivingDungeonScheduling.Pin(newborn, execution, profile);
                                // Same lifetime checkpoint assurance as B2, with no effects published.
                                var probe = LivingDungeonContracts.Restore(newborn, profile);
                                try {
                                    while (!probe.IsTerminal) {
                                        if (probe.NextOrdinal >= profile.MaximumPartyReplay) throw new InvalidOperationException("run.checkpoint.replay_limit");
                                        probe.Advance(1, probe.NextOrdinal); probe.ExportCheckpoint(profile.Checkpoints);
                                    }
                                } finally { probe.Abandon(); }
                                owner.Parties.Add(newborn); runtime.runHistory.NextRunSequence = checked(runtime.runHistory.NextRunSequence + 1);
                            } finally { execution.Abandon(); }
                            } catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is OverflowException) {
                                // Unsupported starts consume an opportunity without allocation,
                                // retaining time/passive progress and the editable dungeon.
                                admissionReason = "living.foreground.admission_unavailable";
                            }
                        }
                        if (interval > 0) owner.Scheduler.NextOpportunityTime = checked(time + interval);
                        Stage(); ProcessParties(time);
                    }
                    if (!owner.Scheduler.NextOpportunityTime.HasValue) {
                        long interval = DemandInterval();
                        if (interval > 0) { owner.Scheduler.NextOpportunityTime = checked(time + interval); LivingDungeonScheduling.Change(owner); }
                    }
                    if (owner.Scheduler.ClosedThrough != time) { owner.Scheduler.ClosedThrough = time; LivingDungeonScheduling.Change(owner); }
                    Stage();
                }
                // A bounded pump may retain an accepted horizon. It resumes that exact
                // prefix before accepting additional observed time on a later invocation.
                var final = new DetachedCanonicalSaveSessionResult(null,
                    new DetachedCanonicalSaveSessionUpdate(working.GetCurrentBytes(), owned.State), null);
                var result = PrepareAndPersist(path, fs, session, final, false);
                if (result.IsSuccess && recoveredManaSpent != 0) result = new DetachedCanonicalWriteResult(true, result.Reason,
                    result.IsNoOp, result.ApplyExplicitRoomEffect, result.GetPersistedBytes(), result.Session, result.Validation,
                    result.RuntimeProjection, result.RecoveryManaSpent + recoveredManaSpent);
                result.ForegroundAdmissionReason = admissionReason;
                if (result.IsSuccess) RunTransientEvidence.Retain(runtime, result.RuntimeProjection);
                return result;
            }
            catch (Exception error) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!error.Message.StartsWith("living.", StringComparison.Ordinal) && !error.Message.StartsWith("combat.", StringComparison.Ordinal))
                    UnityEngine.Debug.LogWarning("living.foreground.transaction_failed: " + error);
#endif
                return Failure(error.Message.StartsWith("living.", StringComparison.Ordinal) || error.Message.StartsWith("combat.", StringComparison.Ordinal)
                    ? error.Message : invalid);
            }
        }
    }
}
