using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public readonly struct DungeonReplayFrame
    {
        public readonly RunStep Step;
        public readonly string FloorId;
        public readonly Vector2 Position;
        public readonly bool HasPosition;
        public readonly int Survivors;
        public readonly string Category, Option;
        public readonly Vector2 MarkerPosition;
        public readonly bool HasMarker;
        internal DungeonReplayFrame(RunStep step, string floor, Vector2 position, bool hasPosition,
            int survivors, string category, string option, Vector2 marker, bool hasMarker)
        { Step = step; FloorId = floor; Position = position; HasPosition = hasPosition;
          Survivors = survivors; Category = category; Option = option; MarkerPosition = marker; HasMarker = hasMarker; }
    }

    // A detached display sequence of already committed evidence. No simulation/service/save reference.
    public sealed class DungeonRunReplay
    {
        public const string Missing = "ui.dungeon.replay.unavailable";
        public const string Historical = "ui.dungeon.replay.historical";
        private readonly DungeonReplayFrame[] frames;
        private readonly DungeonPresentationPolicy policy;
        private float elapsed;
        public int Index { get; private set; }
        public bool Paused { get; private set; }
        public bool Closed { get; private set; }
        public bool Complete => Index == frames.Length - 1;
        public DungeonReplayFrame Current => frames[Index];
        public int FrameCount => frames.Length;
        public int CurrentFloorIndex
        {
            get { for (int i = Index; i >= 0; i--) if (frames[i].Step.FloorIndex >= 0) return frames[i].Step.FloorIndex; return -1; }
        }
        public IReadOnlyList<DungeonReplayFrame> Frames { get; }
        public Vector2 DisplayPosition
        {
            get
            {
                var current = Current;
                if (current.Step.Kind != RunStepKind.Movement || Index == 0) return current.Position;
                var previous = frames[Index - 1];
                // Never interpolate a floor transfer or an unrecorded gap.
                if (!previous.HasPosition || previous.FloorId != current.FloorId ||
                    Vector2.Distance(previous.Position, current.Position) > 1) return current.Position;
                return Vector2.Lerp(previous.Position, current.Position, elapsed / Duration(current));
            }
        }
        private DungeonRunReplay(DungeonReplayFrame[] frames, DungeonPresentationPolicy policy)
        { this.frames = frames; this.policy = policy; Frames = Array.AsReadOnly(frames); }
        private float Duration(DungeonReplayFrame frame) => frame.Step.Kind == RunStepKind.Movement ?
            policy.ReplayMovementSeconds : policy.ReplayInteractionSeconds;
        public void TogglePause() { if (!Closed && !Complete) Paused = !Paused; }
        public void Suspend() { if (!Closed && !Complete) Paused = true; }
        public void Close() { Closed = true; }
        public bool Advance(float seconds)
        {
            if (Closed || Paused || Complete || !float.IsFinite(seconds) || seconds <= 0) return false;
            elapsed += seconds;
            int before = Index;
            // At most one configured sequence's worth of work, including long frame stalls.
            while (!Complete && elapsed >= Duration(Current))
            { elapsed -= Duration(Current); Index++; }
            return before != Index;
        }
        public static bool TryCreate(RunOutcomeRecord outcome, DetachedCanonicalSpatialSaveState state,
            SpatialContentCatalog catalog, string fingerprint, DungeonPresentationPolicy policy,
            out DungeonRunReplay replay, out string reason)
        {
            replay = null; reason = Missing;
            var steps = outcome?.PresentationSteps;
            if (steps == null || steps.Length == 0 || steps.Length > policy.MaximumReplaySteps ||
                outcome.Party == null || policy.ReplayMovementSeconds <= 0 || policy.ReplayInteractionSeconds <= 0 ||
                !float.IsFinite(policy.ReplayMovementSeconds) || !float.IsFinite(policy.ReplayInteractionSeconds) ||
                steps[steps.Length - 1].Kind != RunStepKind.Terminal) return false;
            if (string.IsNullOrEmpty(outcome.PresentationLayoutFingerprint) ||
                outcome.PresentationLayoutFingerprint != fingerprint) { reason = Historical; return false; }
            var list = new List<DungeonReplayFrame>(steps.Length);
            var interactions = new Dictionary<(int, string, string), RunSpatialEvent>();
            foreach (var value in outcome.SpatialEvents ?? Array.Empty<RunSpatialEvent>())
                if (value != null && value.AssignmentId != null)
                    if (!interactions.TryAdd((value.FloorIndex, value.RoomInstanceId, value.AssignmentId), value)) return false;
            var deaths = new HashSet<int>();
            int survivors = outcome.Party.Members.Count;
            string lastFloor = null; Vector2 lastPosition = default; bool lastHasPosition = false;
            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (step.Ordinal != i || step.RunId != outcome.RunId) return false;
                var floor = state.Floors.SingleOrDefault(f => f.FloorIndex == step.FloorIndex);
                bool located = step.PositionSpace != RunPositionSpace.None;
                Vector2 position = default;
                if (located && (floor == null || !TryProject(step.Position, step.PositionSpace, step.RoomId, floor, catalog, out position)))
                { reason = Historical; return false; }
                string floorId = floor?.FloorInstanceId;
                if (!located && step.Kind != RunStepKind.FloorTransfer)
                { position = lastPosition; floorId = lastFloor; located = lastHasPosition; }
                if (step.Encounter?.HealthAfter == 0 && deaths.Add(step.Encounter.MemberOrdinal)) survivors--;
                string category = step.Kind == RunStepKind.MonsterEngagement ? CanonicalSpatialSaveContracts.MonsterCategoryId :
                    step.Kind == RunStepKind.TrapTrigger ? CanonicalSpatialSaveContracts.TrapCategoryId :
                    step.Kind == RunStepKind.LootInteraction ? CanonicalSpatialSaveContracts.LootNodeCategoryId : null;
                interactions.TryGetValue((step.FloorIndex, step.RoomId, step.AssignmentId), out var spatial);
                category = spatial?.CategoryId ?? category;
                Vector2 marker = position; bool hasMarker = category != null && located;
                if (step.Kind == RunStepKind.UnreachableInteraction)
                {
                    hasMarker = spatial != null && floor != null && TryProject(spatial.ConfiguredStart,
                        RunPositionSpace.RoomLocal, step.RoomId, floor, catalog, out marker);
                    if (!hasMarker) return false;
                }
                var assignment = floor?.RoomContents.Assignments.FirstOrDefault(a => a.AssignmentId == step.AssignmentId);
                var option = spatial?.OptionId ?? step.Encounter?.OptionId ?? assignment?.OptionId;
                list.Add(new DungeonReplayFrame(step, floorId, position, located, survivors, category, option, marker, hasMarker));
                lastPosition = position; lastFloor = floorId; lastHasPosition = located;
            }
            if (!list.Any(f => f.HasPosition) || survivors != outcome.SurvivalSummary?.SurvivorCount) return false;
            replay = new DungeonRunReplay(list.ToArray(), policy); reason = null; return true;
        }
        public static bool TryProject(TileCoordinate position, RunPositionSpace space, string roomId,
            SavedSpatialFloor floor, SpatialContentCatalog catalog, out Vector2 result)
        {
            result = default;
            TileCoordinate tile = position;
            if (space == RunPositionSpace.RoomLocal)
            {
                var room = floor.Layout.Rooms.SingleOrDefault(r => r.RoomInstanceId == roomId);
                var definition = catalog.Rooms.SingleOrDefault(r => r.RoomDefinitionId == room?.RoomDefinitionId);
                if (definition == null || !RoomLocalCoordinateTransform.TryToFloor(position, definition.GrossFootprint,
                    room.Anchor, room.Orientation, out tile)) return false;
            }
            else if (space != RunPositionSpace.Floor) return false;
            result = new Vector2(tile.X + .5f, tile.Y + .5f); return true;
        }
    }
}
