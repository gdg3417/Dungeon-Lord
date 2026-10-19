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
        public bool ForegroundPartyVisible => foregroundParty != null && foregroundParty.gameObject.activeSelf;
        public Vector2 ForegroundPartyPosition => foregroundParty == null ? default : (Vector2)foregroundParty.transform.localPosition;
        public void PresentForeground(LivingDungeonForegroundInspection view, bool hidden)
        {
            if (foregroundView == view && foregroundProjection == ReconstructionCount && foregroundHidden == hidden) return;
            foregroundView = view; foregroundProjection = ReconstructionCount; foregroundHidden = hidden;
            if (foregroundParty != null) foregroundParty.gameObject.SetActive(false);
            if (hidden || view?.RunId == null || !view.GeometryCompatible || view.FloorId != FloorInstanceId ||
                !view.Step.HasValue || view.Members == null || !view.Members.Any(m => m.Health > 0) ||
                !DungeonRunReplay.TryProject(view.Step.Value.Position, view.Step.Value.PositionSpace,
                    view.Step.Value.RoomId, floor, production.Catalog, out var position)) return;
            if (foregroundParty == null) {
                foregroundParty = Entity("ForegroundParty", 5); foregroundParty.sprite = anchorSprite;
                foregroundParty.color = policy.ReplayPartyColor; foregroundParty.transform.localScale = Vector3.one * policy.ReplayPartySize;
            }
            foregroundParty.transform.localPosition = position;
            foregroundParty.gameObject.SetActive(true);
        }
    }
}
