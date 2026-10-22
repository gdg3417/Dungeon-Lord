using System;
using System.Collections.Generic;
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
            var executions = new Dictionary<int, RunExecution>();
            try {
                if (policy == null || current == null || session == null || fs == null || observedThrough < 0 || tickSeconds <= 0 || passiveTick == null ||
                    !session.GetCurrentBytes().SequenceEqual(fs.ReadAllBytes(path))) return Failure(invalid);
                var profile = context.LivingDungeonProfiles.SingleOrDefault(p => p.Id == (current.livingDungeon?.Scheduler?.ProfileId ?? policy.Id));
                if (profile == null) return Failure("living.foreground.provenance_required");
                var working = session; var owned = ValidateSession(working);
                if (owned?.CurrentTargetValidated != true || !CanonicalEqual(current.validatedCanonicalSpatialState, owned.State)) return Failure(invalid);
                var runtime = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(current));
                if (current.researchPending == null) runtime.researchPending = null;
                if (current.researchProgress == null) runtime.researchProgress = null;
                if (current.lastOfflineSummary == null) runtime.lastOfflineSummary = null;
                if (current.runHistory.LatestOutcome == null) runtime.runHistory.LatestOutcome = null;
                if (current.runHistory.RecentOutcomes == null) runtime.runHistory.RecentOutcomes = null;
                RunTransientEvidence.Retain(current, runtime);
                runtime.lastSavedUtcUnix = savedUtcUnix;
                var owner = owned.LivingDungeon;
                runtime.livingDungeon = owner;
                DetachedCanonicalSaveSessionResult staged = null;
                string admissionReason = null;
                double recoveredManaSpent = 0;
                if (!LivingDungeonForegroundConfig.IsProduction(profile.Id) || !owner.IsDormant && owner.Scheduler.ProfileId != profile.Id)
                    return Failure("living.foreground.provenance_required");
                var lineage = new LivingDungeonForegroundLineage(session.GetCurrentBytes());
                runtime.monsterContinuity = owned.MonsterContinuity.Copy();
                runtime.validatedCanonicalSpatialState = owned.State; runtime.canonicalSpatialAuthority = owned.State.Authority; runtime.spatialFloors = owned.State.Floors;
                runtime.corridorContent = owned.CorridorContent; runtime.sharedBranchKnowledge = owned.BranchKnowledge; runtime.sharedFloorKnowledge = owned.FloorKnowledge;

                void Stage(MonsterContinuityState monsters = null, DurableSettlementHistory history = null, bool knowledge = false)
                {
                    using var stageMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Stage);
                    var prepareMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Prepare);
                    var before = working.GetCurrentBytes();
                    var prepared = working.PrepareForegroundReplacement(DetachedRecognizedSaveStateSnapshot.Capture(runtime, limits),
                        owned, owner, monsters ?? runtime.monsterContinuity,
                        knowledge ? runtime.sharedBranchKnowledge : null, knowledge ? runtime.sharedFloorKnowledge : null, history, lineage.Permit(before));
                    prepareMeasurement.Dispose();
                    if (!prepared.IsSuccess) throw new InvalidOperationException(prepared.Reason);
                    // Prepare already round-tripped these exact bytes under this
                    // context. Do not parse/replay the same candidate twice more.
                    staged = prepared; working = prepared.Session; owned = prepared.ForegroundValidation;
                    lineage.Advance(before, working.GetCurrentBytes());
                    if (working == null || owned?.CurrentTargetValidated != true) throw new InvalidOperationException(invalid);
                    var projectionMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Projection);
                    if (!CanonicalMvpRouteProjection.TryPublishValidated(owned, production, out var projection, out _)) throw new InvalidOperationException(invalid);
                    projectionMeasurement.Dispose();
                    RunTransientEvidence.Retain(runtime, projection); runtime = projection; owner = owned.LivingDungeon;
                }

                if (owner.IsDormant) LivingDungeonScheduling.Activate(owner, profile, policy.MinimumArrival, runtime.runHistory);
                if (policy.TwoFloor && profile.Id == LivingDungeonForegroundConfig.ProfileId && owner.IsClosed && owner.Parties.Count == 0) {
                    var nextProfile = context.LivingDungeonProfiles.Single(p => p.Id == policy.Id);
                    LivingDungeonScheduling.UpgradeProfile(owner, profile, nextProfile); profile = nextProfile;
                }
                bool previousHorizonClosed = owner.IsClosed;
                if (!LivingDungeonScheduling.AllocatorValid(runtime.runHistory, owner, false) || observedThrough < owner.Scheduler.AcceptedThrough)
                    return Failure(invalid);
                if (owner.IsClosed && owner.Scheduler.AcceptedThrough != observedThrough) LivingDungeonScheduling.Accept(owner, observedThrough);
                // Acceptance changes only the typed scheduler. Validate its checkpoint
                // envelope now; the complete detached candidate is validated at Stage
                // before persistence. No accepted state is published in the meantime.
                if (!LivingDungeonContracts.Validate(owner, context.LivingDungeonProfiles)) return Failure(invalid);
                long tickUnits = checked((long)tickSeconds * policy.UnitsPerSecond);
                int boundaryEvents = 0;

                RunExecution Execution(LivingDungeonParty p)
                {
                    if (!executions.TryGetValue(p.Sequence, out var e)) {
                        e = LivingDungeonContracts.Restore(p, profile, retainPresentation: true); executions.Add(p.Sequence, e);
                    }
                    return e;
                }
                ISet<string> Occupied() => owner.Parties.Select(p => {
                    var e = Execution(p); return e.TransferContract ? e.OccupiedFloor : e.Snapshot.Floors[0].FloorInstanceId;
                }).ToHashSet(StringComparer.Ordinal);

                long? NextEvent()
                {
                    long? earliest = null;
                    foreach (var p in owner.Parties) {
                        var e = Execution(p);
                            if (e.IsTerminal) throw new InvalidOperationException("living.foreground.unsettled_terminal");
                            var next = e.NextDueTime(); if (!next.HasValue) continue;
                            long due = checked(p.AdmittedAt + next.Value);
                            if (!earliest.HasValue || due < earliest.Value) earliest = due;
                    }
                    return earliest;
                }

                void ProcessParties(long time)
                {
                    while (true) {
                        var selected = owner.Parties.Select(p => (party: p, due: Execution(p).NextDueTime()))
                            .Where(x => x.due.HasValue && checked(x.party.AdmittedAt + x.due.Value) <= time)
                            .OrderBy(x => checked(x.party.AdmittedAt + x.due.Value)).ThenBy(x => x.party.Sequence).FirstOrDefault();
                        var p = selected.party; if (p == null) break;
                        var e = Execution(p);
                        if (++boundaryEvents > policy.MaximumBoundaryEvents) throw new InvalidOperationException("living.foreground.boundary_limit");
                        if (e.TransferContract && !e.TransferReleased && e.AnnouncedAction == RunStepKind.FloorTransfer) {
                            var blocker = owner.Parties.FirstOrDefault(other => other.Sequence != p.Sequence && Execution(other).OccupiedFloor == e.TransferDestination);
                            if (blocker != null) {
                                using var blockedMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.TransferBlocked);
                                e.BlockTransfer(selected.due.Value, blocker.Sequence);
                            }
                            else e.AdvanceThrough(selected.due.Value, 1, e.NextOrdinal);
                        } else e.AdvanceThrough(selected.due.Value, 1, e.NextOrdinal);
                        LivingDungeonScheduling.Pin(p, e, profile); LivingDungeonScheduling.Change(owner);
                            if (e.IsTerminal) {
                                using var settlementMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Settlement);
                                // Pin the actual unconsumed terminal in the exact private
                                // lineage before authorizing settlement and its vacancy.
                                Stage(); var terminalPredecessor = ValidateSession(working);
                                var outcome = e.TakeOutcome();
                                // Bind session-only C1 observation to compatible
                                // captured inputs. An edit never authenticates old
                                // positions against a different current layout.
                                if (e.Snapshot.Floors.All(f => FloorKnowledgeApplicability.TryCompute(owned.State, owned.CorridorContent,
                                        limits.Canonical, f.FloorInstanceId, out string fingerprint) && fingerprint == f.KnowledgeFingerprint) &&
                                    StructuralEditService.TryFingerprint(owned.State, limits.Canonical, out string layout) &&
                                    RunTransientEvidence.TryCorridorFingerprint(outcome, owned.CorridorContent, limits.Canonical, out string corridor)) {
                                    outcome.PresentationLayoutFingerprint = layout; outcome.PresentationCorridorFingerprint = corridor;
                                }
                                string runId = outcome.RunId;
                                if (runtime.runHistory.LatestOutcome?.RunId == runId ||
                                    (runtime.runHistory.RecentOutcomes ?? Array.Empty<RunOutcomeRecord>()).Any(r => r?.RunId == runId)) throw new InvalidOperationException(invalid);
                                var reconciled = LivingDungeonForegroundReconciliation.Apply(e, outcome, runtime.monsterContinuity, owned.State, context.MonsterRecovery);
                                LivingDungeonOutcomeConsequences.Apply(runtime, owned, e, outcome, configuration, production, limits.Canonical);
                                runtime.monsterContinuity = reconciled;
                                owner.Parties.RemoveAll(party => party.Sequence == p.Sequence); LivingDungeonScheduling.Change(owner);
                                var authorization = DurableSettlementHistory.CreateForeground(working.GetCurrentBytes(), owner,
                                    runtime.runHistory, configuration.MaxRunHistoryEntries, limits.Canonical.Serialized, e);
                                Stage(reconciled, authorization, true);
                                lineage.Retired(terminalPredecessor, owned, profile, p.Sequence, e);
                            }
                    }
                }

                void ReleaseWaiting(long time)
                {
                    bool changed;
                    do {
                        changed = false;
                        foreach (int sequence in owner.Parties.Select(p => p.Sequence).OrderBy(n => n).ToArray()) {
                            var p = owner.Parties.Single(party => party.Sequence == sequence); var e = Execution(p);
                            if (!lineage.Release(working.GetCurrentBytes(), p, e, time, Occupied(), profile)) continue;
                            boundaryEvents = checked(boundaryEvents + 2);
                            if (boundaryEvents > policy.MaximumBoundaryEvents) throw new InvalidOperationException("living.foreground.boundary_limit");
                            LivingDungeonScheduling.Change(owner); LivingDungeonScheduling.Change(owner); Stage();
                            ProcessParties(time); Recover(); changed = true;
                        }
                    } while (changed);
                }

                long DemandInterval()
                {
                    var active = owned.State.Floors.Where(f => f.ActivationState == FloorActivationState.Active).ToArray();
                    if (active.Length < 1 || active.Length > (policy.TwoFloor ? 2 : 1) || active[0].FloorIndex != 0 ||
                        !active[0].RoomContents.Assignments.Any(a => a.CategoryId == CanonicalSpatialSaveContracts.MonsterCategoryId)) return 0;
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
                        runtime.structureRuntime.ManaReserve, Occupied());
                    recoveredManaSpent += runtime.structureRuntime.ManaReserve - wallet;
                    bool changed = runtime.structureRuntime.ManaReserve != wallet;
                    // A small existing-policy debit can round away at a valid
                    // large double balance. HP/revival still changed and must
                    // reach the validated owner before same-boundary admission.
                    for (int index = 0; !changed && index < healed.Records.Length; index++)
                        changed = healed.Records[index].Health != runtime.monsterContinuity.Records[index].Health ||
                            healed.Records[index].RevivalRequired != runtime.monsterContinuity.Records[index].RevivalRequired;
                    runtime.structureRuntime.ManaReserve = wallet; runtime.monsterContinuity = healed;
                    if (changed) {
                        using var recoveryMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.RecoveryApplied);
                        Stage(healed);
                    }
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
                    lineage.BeginBoundary();
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
                    // Terminal parties were already settled and retired. Recover
                    // vacant floors before releasing a previously blocked transfer.
                    Recover();
                    ReleaseWaiting(time);
                    if (owner.Scheduler.NextOpportunityTime == time) {
                        owner.Scheduler.NextOpportunityTime = null; owner.Scheduler.LastOpportunityTime = time;
                        owner.Scheduler.NextOpportunityOrdinal = checked(owner.Scheduler.NextOpportunityOrdinal + 1);
                        LivingDungeonScheduling.Change(owner);
                        long interval = DemandInterval();
                        bool draining = policy.TwoFloor && profile.Id == LivingDungeonForegroundConfig.ProfileId;
                        string entry = owned.State.Floors.Single(f => f.FloorIndex == 0).FloorInstanceId;
                        if (!draining && !Occupied().Contains(entry) && owner.Parties.Count < policy.Capacity && interval > 0 && runtime.runHistory.NextRunSequence < int.MaxValue) {
                            try {
                            var snapshot = ActiveFloorRunSnapshot.Create(owned, production, configuration, limits.Canonical,
                                runLoot, null, runtime, RunPostureResolver.BalancedId);
                            var execution = new RunSimulationService(configuration, runLoot).StartTimedCombatSnapshot(
                                runtime.runHistory.NextRunSequence, snapshot, policy.Timing);
                            if (policy.TwoFloor) execution.EnableTransfers();
                            try {
                                var newborn = new LivingDungeonParty { Sequence = runtime.runHistory.NextRunSequence, AdmittedAt = time };
                                LivingDungeonScheduling.Pin(newborn, execution, profile);
                                // Same lifetime checkpoint assurance as B2, with no effects published.
                                var probe = LivingDungeonContracts.Restore(newborn, profile);
                                try {
                                    using var probeMeasurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.LifetimeProbe);
                                    LivingDungeonLifetimeAssurance.Check(probe, profile);
                                } finally { probe.Abandon(); }
                                owner.Parties.Add(newborn); runtime.runHistory.NextRunSequence = checked(runtime.runHistory.NextRunSequence + 1);
                            } finally { execution.Abandon(); }
                            } catch (Exception error) when (error is ArgumentException || error is InvalidOperationException ||
                                error is OverflowException || error is System.IO.InvalidDataException) {
                                // Unsupported starts consume an opportunity without allocation,
                                // retaining time/passive progress and the editable dungeon.
                                admissionReason = "living.foreground.admission_unavailable";
                            }
                        }
                        if (interval > 0) owner.Scheduler.NextOpportunityTime = checked(time + interval);
                        Stage(); ProcessParties(time); Recover(); ReleaseWaiting(time);
                    }
                    if (!owner.Scheduler.NextOpportunityTime.HasValue) {
                        long interval = DemandInterval();
                        if (interval > 0) { owner.Scheduler.NextOpportunityTime = checked(time + interval); LivingDungeonScheduling.Change(owner); }
                    }
                    if (owner.Scheduler.ClosedThrough != time) { owner.Scheduler.ClosedThrough = time; LivingDungeonScheduling.Change(owner); }
                    if (policy.TwoFloor && profile.Id == LivingDungeonForegroundConfig.ProfileId && owner.IsClosed && owner.Parties.Count == 0) {
                        var nextProfile = context.LivingDungeonProfiles.Single(p => p.Id == policy.Id);
                        LivingDungeonScheduling.UpgradeProfile(owner, profile, nextProfile); profile = nextProfile;
                    }
                    // Authorized history composition deliberately retains the
                    // predecessor's unrelated metadata. Closure must restore the
                    // already-injected save boundary before ordinary staging.
                    runtime.lastSavedUtcUnix = savedUtcUnix;
                    Stage();
                }
                // A bounded pump may retain an accepted horizon. It resumes that exact
                // prefix before accepting additional observed time on a later invocation.
                var result = PrepareAndPersist(path, fs, session, staged, false);
                if (result.IsSuccess && recoveredManaSpent != 0) result = new DetachedCanonicalWriteResult(true, result.Reason,
                    result.IsNoOp, result.ApplyExplicitRoomEffect, result.GetPersistedBytes(), result.Session, result.Validation,
                    result.RuntimeProjection, result.RecoveryManaSpent + recoveredManaSpent);
                result.ForegroundAdmissionReason = admissionReason;
                if (result.IsSuccess) RunTransientEvidence.Retain(runtime, result.RuntimeProjection);
                return result;
            }
            catch (Exception error) {
                return Failure(error.Message.StartsWith("living.", StringComparison.Ordinal) || error.Message.StartsWith("combat.", StringComparison.Ordinal)
                    ? error.Message : invalid);
            }
            finally { foreach (var e in executions.Values) e.Abandon(); }
        }
    }
}
