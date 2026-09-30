using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorLifecycleAction { Activate, Deactivate, ActivateAllEligible }

    public sealed class FloorLifecyclePreview
    {
        public bool IsCommittable { get; internal set; }
        public string Reason { get; internal set; }
        internal DetachedCanonicalSaveSessionResult Prepared;
        public ActiveFloorRunSnapshot RunSnapshot { get; internal set; }
    }

    public sealed partial class DetachedCanonicalWriteAuthority
    {
        public FloorLifecyclePreview PreviewFloorLifecycle(DetachedCanonicalSaveSession session,
            SaveData current, FloorLifecycleAction action, string floorId)
        {
            var result = new FloorLifecyclePreview { Reason = "floor.lifecycle.invalid_state" };
            try
            {
                if (runLoot == null) return result;
                var owned = ValidateSession(session);
                if (owned?.CurrentTargetValidated != true || !owned.IsValid || current == null ||
                    !CanonicalEqual(current.validatedCanonicalSpatialState, owned.State) ||
                    !Enum.IsDefined(typeof(FloorLifecycleAction), action)) return result;
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
                    if (!eligible.IsEligible) { result.Reason = eligible.PrimaryBlocker; return result; }
                    if (action == FloorLifecycleAction.Activate && floorId != eligible.TargetFloorInstanceId) return result;
                    ApplyDetachedActivation(candidate, eligible.TargetFloorInstanceId, true);
                }
                var recognized = DetachedRecognizedSaveStateSnapshot.Capture(current, limits);
                var prepared = session.PrepareLiveReplacement(recognized, candidate, owned.Investment,
                    owned.CorridorContent, owned.BranchKnowledge);
                if (!prepared.IsSuccess) { result.Reason = prepared.Reason; return result; }
                var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(prepared.Update.GetBytes(), context);
                // Exactly the same authority called by real execution, including floor-local
                // branch plans, damage profiles and the entire Active prefix.
                result.RunSnapshot = ActiveFloorRunSnapshot.Create(validated, production, configuration, limits.Canonical, runLoot);
                result.Prepared = prepared; result.IsCommittable = true; result.Reason = null;
                return result;
            }
            catch { return result; }
        }

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
            SaveData current, FloorLifecycleAction action, string floorId)
        {
            if (session == null || fileSystem == null) return Failure("floor.lifecycle.invalid_state");
            try
            {
                if (!session.GetCurrentBytes().SequenceEqual(fileSystem.ReadAllBytes(activePath)))
                    return Failure("floor.lifecycle.stale_session");
            }
            catch { return Failure(AtomicSaveFailedReason); }
            // Never trust a UI preview: eligibility, canonical/production validation and
            // complete runtime materialization are repeated against the current session.
            var preview = PreviewFloorLifecycle(session, current, action, floorId);
            if (!preview.IsCommittable) return Failure(preview.Reason);
            var result = PrepareAndPersist(activePath, fileSystem, session, preview.Prepared, false);
            if (result.IsSuccess) RunTransientEvidence.Retain(current, result.RuntimeProjection);
            return result;
        }
    }
}
