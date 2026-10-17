using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorLifecycleAction { Activate, Deactivate, ActivateAllEligible }

    public sealed class FloorLifecyclePreview
    {
        public bool IsCommittable { get; internal set; }
        public bool IsNoOp { get; internal set; }
        public string Reason { get; internal set; }
        internal DetachedCanonicalSaveSessionResult Prepared;
        public ActiveFloorRunSnapshot RunSnapshot { get; internal set; }
        public FloorLifecycleAction Action { get; internal set; }
        public string FloorInstanceId { get; internal set; }
        public IReadOnlyList<FloorLifecycleChange> Changes { get; internal set; } = Array.Empty<FloorLifecycleChange>();
        public bool RequiresConfirmation => Action == FloorLifecycleAction.Deactivate && Changes.Count > 1;
        internal byte[] BaselineBytes;
        internal DetachedCanonicalWriteAuthority.LifecyclePreviewBinding Binding;
        internal bool HasReserveQuote;
        internal long RequiredReserveBefore, RequiredReserveAfter;
    }

    public sealed class FloorLifecycleChange
    {
        public string FloorInstanceId { get; }
        public int FloorIndex { get; }
        internal FloorLifecycleChange(SavedSpatialFloor floor)
        { FloorInstanceId = floor.FloorInstanceId; FloorIndex = floor.FloorIndex; }
    }

    public sealed partial class DetachedCanonicalWriteAuthority
    {
        // Transient input binding, never a save owner. Capture all recognized runtime
        // inputs conservatively, including changes not yet published to the session.
        internal sealed class LifecyclePreviewBinding
        {
            private readonly string runtime, configuration, combat, loot;
            private readonly DetachedCanonicalWriteAuthority source;
            internal LifecyclePreviewBinding(DetachedCanonicalWriteAuthority source, SaveData current)
            {
                this.source = source;
                runtime = JsonUtility.ToJson(current);
                configuration = JsonUtility.ToJson(source.configuration);
                combat = JsonUtility.ToJson(source.configuration?.CombatC0);
                loot = JsonUtility.ToJson(source.runLoot);
            }
            internal bool Matches(DetachedCanonicalWriteAuthority writer, SaveData current) =>
                ReferenceEquals(source.context, writer.context) && ReferenceEquals(source.production, writer.production) &&
                ReferenceEquals(source.floorConstructionProfiles, writer.floorConstructionProfiles) &&
                ReferenceEquals(source.floorConstructionResearch, writer.floorConstructionResearch) &&
                runtime == JsonUtility.ToJson(current) && configuration == JsonUtility.ToJson(writer.configuration) &&
                combat == JsonUtility.ToJson(writer.configuration?.CombatC0) && loot == JsonUtility.ToJson(writer.runLoot);
        }

        public FloorLifecyclePreview PreviewFloorLifecycle(DetachedCanonicalSaveSession session,
            SaveData current, FloorLifecycleAction action, string floorId)
        {
            var result = new FloorLifecyclePreview { Reason = "floor.lifecycle.invalid_state",
                Action = action, FloorInstanceId = floorId };
            try
            {
                if (runLoot == null) return result;
                var owned = ValidateSession(session);
                if (owned?.CurrentTargetValidated != true || !owned.IsValid || current == null ||
                    !CanonicalEqual(current.validatedCanonicalSpatialState, owned.State) ||
                    !Enum.IsDefined(typeof(FloorLifecycleAction), action)) return result;
                if (action != FloorLifecycleAction.ActivateAllEligible &&
                    (string.IsNullOrWhiteSpace(floorId) || !CanonicalEditFloorTarget.TryResolve(owned.State, floorId, out _)))
                    return result;
                result.BaselineBytes = session.GetCurrentBytes();
                result.Binding = new LifecyclePreviewBinding(this, current);
                var candidate = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(owned.State));
                if (action == FloorLifecycleAction.Deactivate)
                {
                    var target = candidate.Floors.SingleOrDefault(f => f.FloorInstanceId == floorId);
                    if (target == null) return result;
                    if (target.FloorIndex == 0) { result.Reason = "floor.lifecycle.first_floor_required"; return result; }
                    ApplyDetachedActivation(candidate, target.FloorInstanceId, false);
                }
                else
                {
                    // A4 has one configured progression target. No synthetic deeper content
                    // is invented; Activate All stops at the first A3 blocker.
                    var eligible = FloorActivationEligibilityAuthority.Resolve(candidate, current.completedResearch,
                        floorConstructionProfiles, floorConstructionResearch, production, configuration, limits.Canonical);
                    if (action == FloorLifecycleAction.ActivateAllEligible && eligible.HasTargetFloor &&
                        eligible.PrimaryBlocker == FloorActivationEligibilityReasons.TargetAlreadyActive)
                    {
                        // The authored prefix is already active. Validate the exact runtime snapshot
                        // without preparing replacement bytes or changing the current session.
                        result.RunSnapshot = ActiveFloorRunSnapshot.Create(owned, production, configuration,
                            limits.Canonical, runLoot);
                        result.IsCommittable = true; result.IsNoOp = true; result.Reason = null;
                        return result;
                    }
                    if (!eligible.IsEligible) { result.Reason = eligible.PrimaryBlocker; return result; }
                    if (action == FloorLifecycleAction.Activate && floorId != eligible.TargetFloorInstanceId) return result;
                    ApplyDetachedActivation(candidate, eligible.TargetFloorInstanceId, true);
                }
                result.Changes = DescribeChanges(owned.State, candidate);
                var recognized = DetachedRecognizedSaveStateSnapshot.Capture(current, limits);
                long required = MonsterContinuity.RequiredReserve(candidate, context.MonsterRecovery);
                result.RequiredReserveBefore = MonsterContinuity.RequiredReserve(owned.State, context.MonsterRecovery);
                result.RequiredReserveAfter = required;
                if (required > result.RequiredReserveBefore &&
                    current.structureRuntime.ManaReserve < required)
                { result.Reason = MonsterRecoveryRules.Insufficient; result.HasReserveQuote = true; return result; }
                var prepared = session.PrepareLiveReplacement(recognized, candidate, owned.Investment,
                    owned.CorridorContent, owned.BranchKnowledge);
                if (!prepared.IsSuccess) { result.Reason = prepared.Reason; return result; }
                var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(prepared.Update.GetBytes(), context);
                // Exactly the same authority called by real execution, including floor-local
                // branch plans, damage profiles and the entire Active prefix.
                result.RunSnapshot = ActiveFloorRunSnapshot.Create(validated, production, configuration, limits.Canonical, runLoot);
                result.IsNoOp = result.Changes.Count == 0;
                result.Prepared = prepared; result.IsCommittable = true; result.HasReserveQuote = true; result.Reason = null;
                return result;
            }
            catch { return result; }
        }

        internal static IReadOnlyList<FloorLifecycleChange> DescribeChanges(
            DetachedCanonicalSpatialSaveState before, DetachedCanonicalSpatialSaveState after) =>
            Array.AsReadOnly(after.Floors.Where(f => before.Floors.Single(b => b.FloorInstanceId == f.FloorInstanceId)
                .ActivationState != f.ActivationState).OrderBy(f => f.FloorIndex)
                .ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal).Select(f => new FloorLifecycleChange(f)).ToArray());

        // Only the detached transaction candidate is passed here. Activation touches exactly
        // the requested floor; deactivation is a suffix operation, independent of content.
        internal static void ApplyDetachedActivation(DetachedCanonicalSpatialSaveState candidate, string floorId, bool active)
        {
            var target = candidate.Floors.Single(f => f.FloorInstanceId == floorId);
            if (!active && target.FloorIndex == 0) throw new ArgumentException("floor.lifecycle.first_floor_required");
            foreach (var floor in candidate.Floors.Where(f => f.FloorInstanceId == floorId || (!active && f.FloorIndex > target.FloorIndex)))
                floor.ActivationState = active ? FloorActivationState.Active : FloorActivationState.Inactive;
        }

        public DetachedCanonicalWriteResult CommitFloorLifecycle(string activePath,
            ISpatialMigrationFileSystem fileSystem, DetachedCanonicalSaveSession session,
            SaveData current, FloorLifecycleAction action, string floorId, FloorLifecyclePreview expectedPreview = null)
        {
            if (session == null || fileSystem == null) return Failure("floor.lifecycle.invalid_state");
            try
            {
                if (expectedPreview != null && (!expectedPreview.IsCommittable || expectedPreview.Action != action ||
                    expectedPreview.FloorInstanceId != floorId || expectedPreview.BaselineBytes == null ||
                    !expectedPreview.BaselineBytes.SequenceEqual(session.GetCurrentBytes()) ||
                    expectedPreview.Binding?.Matches(this, current) != true))
                    return Failure("floor.lifecycle.stale_preview");
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure("floor.lifecycle.stale_session");
            }
            catch { return Failure(AtomicSaveFailedReason); }
            // Never trust a UI preview: eligibility, canonical/production validation and
            // complete runtime materialization are repeated against the current session.
            var preview = PreviewFloorLifecycle(session, current, action, floorId);
            if (!preview.IsCommittable) return Failure(preview.Reason);
            if (expectedPreview != null && (preview.IsNoOp != expectedPreview.IsNoOp ||
                !preview.Changes.Select(c => (c.FloorInstanceId, c.FloorIndex))
                    .SequenceEqual(expectedPreview.Changes.Select(c => (c.FloorInstanceId, c.FloorIndex)))))
                return Failure("floor.lifecycle.stale_preview");
            if (preview.IsNoOp)
                return new DetachedCanonicalWriteResult(true, null, true, false,
                    session.GetCurrentBytes(), session, ValidateSession(session), current);
            var result = PrepareAndPersist(activePath, fileSystem, session, preview.Prepared, false);
            if (result.IsSuccess) RunTransientEvidence.Retain(current, result.RuntimeProjection);
            return result;
        }
    }
}
