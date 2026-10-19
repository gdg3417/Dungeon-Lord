using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private LivingDungeonForegroundInspection presentedForeground;
        private bool foregroundWasEditing, foregroundWasReplay;
        private string foregroundPresentedFloor;
        private RunOutcomeRecord foregroundLatest;
        private void PresentForegroundActivity()
        {
            var view = root?.ForegroundActivity;
            bool observingReplay = replay != null;
            var latest = root?.Save?.runHistory?.LatestOutcome;
            if (view != presentedForeground || IsEditing != foregroundWasEditing || observingReplay != foregroundWasReplay ||
                foregroundPresentedFloor != selectedFloor || latest != foregroundLatest) {
                presentedForeground = view; foregroundWasEditing = IsEditing; foregroundWasReplay = observingReplay;
                foregroundPresentedFloor = selectedFloor; foregroundLatest = latest;
                var label = document.rootVisualElement.Q<Label>("foregroundActivity");
                if (label != null) {
                    label.style.display = view == null || observingReplay ? DisplayStyle.None : DisplayStyle.Flex;
                    if (view != null) {
                        label.text = view.RunId == null ? Text(view.StateKey) : Format("ui.living.active_format", view.RunId,
                            view.Step?.FloorIndex + 1 ?? 1, Text("ui.living.action." + (view.Step?.Kind.ToString() ?? "RoomEntry")), view.Ordinal);
                        if (view.RunId != null && (IsEditing || observingReplay || !view.GeometryCompatible))
                            label.text += "\n" + Text(IsEditing ? "ui.living.editing" : observingReplay ? "ui.living.replay_hidden" : "ui.living.geometry_unavailable");
                        if (!IsEditing && view.RunId == null && latest?.SurvivalSummary?.RuleResolved == true &&
                            (view.StateKey == "ui.living.awaiting" || view.StateKey == "ui.living.not_eligible")) {
                            label.text = Format("ui.living.latest", latest.RunId, latest.SurvivalSummary.SurvivorCount);
                            if (!string.IsNullOrEmpty(latest.FinalRouteOutcomeKey)) label.text += " " + Text(latest.FinalRouteOutcomeKey);
                        }
                    }
                }
            }
            world?.PresentForeground(view, IsEditing || legacy || observingReplay);
        }
    }
}
