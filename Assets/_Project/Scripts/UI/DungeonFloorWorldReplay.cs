using UnityEngine;

namespace DungeonBuilder.M0
{
    public sealed partial class DungeonFloorWorldView
    {
        private SpriteRenderer replayParty, replayMarker;
        private SpriteRenderer replaySuppressedContent;
        private string replaySuppressedAssignment;
        public bool ReplayPartyVisible => replayParty != null && replayParty.gameObject.activeSelf;
        public Vector2 ReplayPartyPosition => replayParty != null ? (Vector2)replayParty.transform.localPosition : default;
        public bool ReplayMarkerVisible => replayMarker != null && replayMarker.gameObject.activeSelf;
        public void HideReplay()
        { RestoreReplayContent(); if (replayParty != null) replayParty.gameObject.SetActive(false); if (replayMarker != null) replayMarker.gameObject.SetActive(false); }
        private void RestoreReplayContent()
        { if (replaySuppressedContent != null) replaySuppressedContent.gameObject.SetActive(true); replaySuppressedContent = null; replaySuppressedAssignment = null; }
        public void PresentReplay(DungeonRunReplay replay, bool editing)
        {
            if (editing || replay == null || replay.Closed) { HideReplay(); return; }
            var frame = replay.Current;
            if (frame.FloorId != FloorInstanceId || !frame.HasPosition) { HideReplay(); return; }
            string movingMonster = frame.Step.Kind == Gameplay.RunSimulation.RunStepKind.MonsterEngagement ? frame.Step.AssignmentId : null;
            if (movingMonster != replaySuppressedAssignment)
            {
                RestoreReplayContent();
                // The icon at recorded engagement replaces its configured-start icon;
                // it must not look like a second monster. No unrecorded monster motion.
                for (int i = 0; i < identities.Count && movingMonster != null; i++)
                    if (identities[i].AssignmentId == movingMonster)
                    { replaySuppressedContent = pool[i]; replaySuppressedAssignment = movingMonster; replaySuppressedContent.gameObject.SetActive(false); break; }
            }
            if (replayParty == null)
            {
                replayParty = Entity("RecordedParty", 5);
                replayParty.sprite = anchorSprite;
                replayParty.color = policy.ReplayPartyColor;
                replayParty.transform.localScale = Vector3.one * policy.ReplayPartySize;
                replayMarker = Entity("RecordedInteraction", 4);
                replayMarker.transform.localScale = Vector3.one * policy.ReplayMarkerSize;
            }
            replayParty.gameObject.SetActive(frame.Survivors > 0);
            replayParty.transform.localPosition = replay.DisplayPosition;
            replayMarker.gameObject.SetActive(frame.HasMarker);
            if (frame.HasMarker)
            {
                replayMarker.sprite = frame.Step.Kind == Gameplay.RunSimulation.RunStepKind.UnreachableInteraction ?
                    policy.Visuals.Invalid : policy.Visuals.Resolve(frame.Category, frame.Option);
                replayMarker.color = Color.white;
                replayMarker.transform.localPosition = frame.MarkerPosition;
            }
        }
    }
}
