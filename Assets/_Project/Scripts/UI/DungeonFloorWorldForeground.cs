using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public sealed partial class DungeonFloorWorldView
    {
        private SpriteRenderer foregroundParty;
        private LivingDungeonForegroundInspection foregroundView;
        private int foregroundProjection = -1;
        private bool foregroundHidden;
        private Vector2 foregroundFrom, foregroundTarget;
        private long foregroundMotionAt, foregroundMotionUnits;
        private bool foregroundMotion;
        public bool ForegroundPartyVisible => foregroundParty != null && foregroundParty.gameObject.activeSelf;
        public Vector2 ForegroundPartyPosition => foregroundParty == null ? default : (Vector2)foregroundParty.transform.localPosition;
        public bool TryForegroundPartyBounds(out Bounds bounds)
        {
            bounds = ForegroundPartyVisible ? foregroundParty.bounds : default;
            return ForegroundPartyVisible;
        }
        public void PresentForeground(LivingDungeonForegroundInspection view, bool hidden, long? observedTime = null)
        {
            if (foregroundView != view || foregroundProjection != ReconstructionCount || foregroundHidden != hidden) {
                var previous = foregroundView;
                bool continuous = ForegroundPartyVisible && !foregroundHidden && foregroundProjection == ReconstructionCount &&
                    previous?.RunId == view?.RunId && previous?.FloorId == view?.FloorId;
                var previousTarget = foregroundTarget;
                foregroundView = view; foregroundProjection = ReconstructionCount; foregroundHidden = hidden; foregroundMotion = false;
                if (foregroundParty != null) foregroundParty.gameObject.SetActive(false);
                var location = view?.PositionStep ?? view?.Step;
                if (hidden || view?.RunId == null || floor == null || production == null || !view.GeometryCompatible || view.FloorId != FloorInstanceId ||
                    !location.HasValue || view.Members == null || !view.Members.Any(m => m.Health > 0) ||
                    !TryForegroundPosition(location.Value, out var position)) return;
                if (foregroundParty == null) {
                    foregroundParty = Entity("ForegroundParty", 5); foregroundParty.sprite = anchorSprite;
                    foregroundParty.transform.localScale = Vector3.one * policy.ReplayPartySize;
                }
                foregroundTarget = position;
                if (continuous && observedTime.HasValue && view.Ordinal > previous.Ordinal && view.MovementUnits > 0) {
                    var moves = view.Steps.Where(s => s.Ordinal >= previous.Ordinal && s.Kind == Gameplay.RunSimulation.RunStepKind.Movement).ToArray();
                    if (moves.Length == 1 && moves[0].Distance == 1 &&
                        !view.Steps.Any(s => s.Ordinal >= previous.Ordinal && s.Kind == Gameplay.RunSimulation.RunStepKind.FloorTransfer) &&
                        TryForegroundPosition(moves[0], out var destination) && destination == position) {
                        var origin = view.Steps.Where(s => s.Ordinal < moves[0].Ordinal &&
                            s.PositionSpace != Gameplay.RunSimulation.RunPositionSpace.None).Select(s => (Gameplay.RunSimulation.RunStep?)s).LastOrDefault();
                        var delta = position - previousTarget;
                        if (origin.HasValue && TryForegroundPosition(origin.Value, out var source) && source == previousTarget &&
                            (Mathf.Abs(delta.x) == 1 && delta.y == 0 || Mathf.Abs(delta.y) == 1 && delta.x == 0)) {
                            foregroundFrom = previousTarget; foregroundMotionAt = checked(view.AdmittedAt + moves[0].LogicalTime);
                            foregroundMotionUnits = view.MovementUnits; foregroundMotion = true;
                        }
                    }
                }
                foregroundParty.color = view.AnnouncedAction == Gameplay.RunSimulation.RunStepKind.MonsterEngagement ? policy.ReplayEngagingColor : policy.ReplayPartyColor;
                foregroundParty.transform.localPosition = position;
                foregroundParty.gameObject.SetActive(true);
            }
            if (foregroundMotion && ForegroundPartyVisible && observedTime.HasValue) {
                float progress = Mathf.Clamp01((float)((double)(observedTime.Value - foregroundMotionAt) / foregroundMotionUnits));
                foregroundParty.transform.localPosition = Vector2.Lerp(foregroundFrom, foregroundTarget, progress);
                if (progress >= 1) foregroundMotion = false;
            }
        }
        private bool TryForegroundPosition(Gameplay.RunSimulation.RunStep step, out Vector2 position)
        {
            if (!DungeonRunReplay.TryProject(step.Position, step.PositionSpace, step.RoomId, floor, production.Catalog, out position)) return false;
            var tile = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), 0);
            return rooms.HasTile(tile) || corridors.HasTile(tile) || fixedStructures.HasTile(tile);
        }
    }
}
