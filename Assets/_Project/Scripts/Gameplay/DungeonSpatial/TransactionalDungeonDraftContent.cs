using System;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DungeonDraftContentOperation { Acquire = 1, Unassign = 2, Redeploy = 3, Reassign = 4, Reposition = 5 }

    [Serializable]
    public sealed class DungeonDraftRoomContent
    {
        public DungeonDraftContentOperation Operation;
        public string FloorInstanceId;
        public string RoomInstanceId;
        public string AssignmentId;
        public string CategoryId;
        public string OptionId;
        public TileCoordinate RoomLocalPosition;
        // Bind custody transitions to their actual policy without changing frozen A5 rule hashes.
        public string RemovalPolicyIdentity;
        internal bool HasExactPayload => Enum.IsDefined(typeof(DungeonDraftContentOperation), Operation) &&
            !string.IsNullOrWhiteSpace(FloorInstanceId) &&
            (Operation == DungeonDraftContentOperation.Unassign
                ? string.IsNullOrEmpty(RoomInstanceId) && RoomLocalPosition.Equals(default(TileCoordinate))
                : !string.IsNullOrWhiteSpace(RoomInstanceId)) &&
            (Operation == DungeonDraftContentOperation.Acquire
                ? string.IsNullOrEmpty(AssignmentId) && !string.IsNullOrWhiteSpace(CategoryId) && !string.IsNullOrWhiteSpace(OptionId)
                : !string.IsNullOrWhiteSpace(AssignmentId) && string.IsNullOrEmpty(CategoryId) && string.IsNullOrEmpty(OptionId)) &&
            (Operation == DungeonDraftContentOperation.Unassign || Operation == DungeonDraftContentOperation.Reassign
                ? !string.IsNullOrWhiteSpace(RemovalPolicyIdentity) : string.IsNullOrEmpty(RemovalPolicyIdentity));
        internal DungeonDraftRoomContent Copy() => JsonUtility.FromJson<DungeonDraftRoomContent>(JsonUtility.ToJson(this));
    }

    public sealed class DungeonDraftContentPreview
    {
        public string Reason { get; internal set; }
        public bool IsValid => Candidate != null;
        internal DetachedCanonicalSpatialSaveState Candidate;
        public RoomContentAssignment Assignment { get; internal set; }
    }

    public sealed partial class DungeonDraftContext
    {
        internal string RemovalPolicyIdentity => RemovalPolicy == null ? null :
            SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(RemovalPolicy.Value));

        public DungeonDraftRoomContent ContentIntent(DungeonDraftContentOperation operation, string floorId,
            string roomId = null, string assignmentId = null, string categoryId = null, string optionId = null,
            TileCoordinate position = default) => new DungeonDraftRoomContent { Operation = operation,
                FloorInstanceId = floorId, RoomInstanceId = roomId, AssignmentId = assignmentId,
                CategoryId = categoryId, OptionId = optionId, RoomLocalPosition = position,
                RemovalPolicyIdentity = operation == DungeonDraftContentOperation.Unassign || operation == DungeonDraftContentOperation.Reassign
                    ? RemovalPolicyIdentity : null };

        public DungeonDraftContentPreview PreviewContent(DetachedCanonicalSpatialSaveState state, DungeonDraftRoomContent intent)
        {
            var result = new DungeonDraftContentPreview { Reason = TransactionalDungeonDraft.InvalidReason };
            if (intent?.HasExactPayload != true || !Validate(state) || Compatibility == null) return result;
            if ((intent.Operation == DungeonDraftContentOperation.Unassign || intent.Operation == DungeonDraftContentOperation.Reassign) &&
                intent.RemovalPolicyIdentity != RemovalPolicyIdentity) { result.Reason = TransactionalDungeonDraft.IncompatibleReason; return result; }
            var floor = state.Floors.SingleOrDefault(f => f.FloorInstanceId == intent.FloorInstanceId);
            // All placement is explicitly room scoped; never reach the compatibility starter path.
            if (floor == null || intent.Operation != DungeonDraftContentOperation.Unassign &&
                !floor.Layout.Rooms.Any(r => r.RoomInstanceId == intent.RoomInstanceId))
            { result.Reason = DetachedCanonicalSpatialMutation.TargetRoomMissingReason; return result; }
            var prior = state.Floors.SelectMany(f => f.RoomContents.Assignments).SingleOrDefault(a => a.AssignmentId == intent.AssignmentId);
            var candidate = state;
            if (intent.Operation == DungeonDraftContentOperation.Reposition)
            {
                var command = new DungeonDraftCommand { Kind = DungeonDraftCommandKind.ContentReposition,
                    ContentRepositions = new[] { new DungeonDraftContentReposition { FloorInstanceId = intent.FloorInstanceId,
                        RoomInstanceId = intent.RoomInstanceId, AssignmentId = intent.AssignmentId, RoomLocalPosition = intent.RoomLocalPosition } } };
                if (!Move(state, command, out candidate)) { result.Reason = DetachedCanonicalSpatialMutation.PositionInvalidReason; return result; }
            }
            else
            {
                DetachedCanonicalMutationRequest request;
                if (intent.Operation == DungeonDraftContentOperation.Reassign)
                {
                    if (prior == null || prior.RoomInstanceId == intent.RoomInstanceId) return result;
                    var source = state.Floors.Single(f => f.RoomContents.Assignments.Any(a => a.AssignmentId == intent.AssignmentId));
                    var returned = PrepareContent(candidate, DetachedCanonicalMutationRequest.Unassign(intent.AssignmentId, source.FloorInstanceId));
                    if (!returned.IsSuccess) { result.Reason = returned.Reason; return result; }
                    candidate = returned.State;
                    request = DetachedCanonicalMutationRequest.Redeploy(intent.AssignmentId, intent.RoomInstanceId, intent.FloorInstanceId, intent.RoomLocalPosition);
                }
                else if (intent.Operation == DungeonDraftContentOperation.Acquire)
                    request = DetachedCanonicalMutationRequest.Place(intent.CategoryId, intent.OptionId, intent.RoomInstanceId, intent.FloorInstanceId, intent.RoomLocalPosition);
                else if (intent.Operation == DungeonDraftContentOperation.Redeploy)
                    request = DetachedCanonicalMutationRequest.Redeploy(intent.AssignmentId, intent.RoomInstanceId, intent.FloorInstanceId, intent.RoomLocalPosition);
                else
                {
                    if (prior == null || !floor.RoomContents.Assignments.Any(a => a.AssignmentId == prior.AssignmentId)) return result;
                    request = DetachedCanonicalMutationRequest.Unassign(intent.AssignmentId, intent.FloorInstanceId);
                }
                var mutation = PrepareContent(candidate, request);
                if (!mutation.IsSuccess) { result.Reason = mutation.Reason; return result; }
                candidate = mutation.State;
            }
            if (!Validate(candidate)) return result;
            result.Candidate = Copy(candidate); result.Reason = null;
            result.Assignment = intent.Operation == DungeonDraftContentOperation.Unassign ? prior :
                candidate.Floors.SelectMany(f => f.RoomContents.Assignments).Single(a => intent.Operation == DungeonDraftContentOperation.Acquire
                    ? !state.Floors.SelectMany(f => f.RoomContents.Assignments).Any(old => old.AssignmentId == a.AssignmentId)
                    : a.AssignmentId == intent.AssignmentId);
            return result;
        }
        private DetachedCanonicalMutationResult PrepareContent(DetachedCanonicalSpatialSaveState state, DetachedCanonicalMutationRequest request) =>
            DetachedCanonicalSpatialMutation.Prepare(state, request, Production, Compatibility, Configuration, Limits.Canonical,
                RemovalPolicy, null, null, null, null, Occupancy);
    }

    public sealed partial class TransactionalDungeonDraft
    {
        public DungeonDraftContentPreview PreviewContent(DungeonDraftRoomContent intent) => context.PreviewContent(presented, intent);
        public bool ChangeContent(DungeonDraftRoomContent intent)
        {
            if (IsClosed || Durability == DraftDurability.Unknown) return false;
            if (commands.Count >= context.Limits.Raw.MaximumArrayElements) { Reason = LimitReason; return false; }
            var command = new DungeonDraftCommand { Sequence = commands.Count + 1L, Kind = DungeonDraftCommandKind.RoomContent,
                RoomContentChanges = new[] { intent?.Copy() } };
            var preview = context.PreviewContent(presented, command.RoomContentChanges[0]);
            if (!preview.IsValid) { Reason = preview.Reason; return false; }
            commands.Add(command); presented = preview.Candidate;
            formatVersion = CurrentFormatVersion; ruleIdentity = context.RuleIdentity(formatVersion);
            if (Durability != DraftDurability.Failed) Durability = DraftDurability.Pending;
            Reason = Durability == DraftDurability.Failed ? FailedReason : PendingReason;
            return true;
        }
    }
}
