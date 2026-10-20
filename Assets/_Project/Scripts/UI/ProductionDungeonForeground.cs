using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private LivingDungeonForegroundInspection presentedForeground;
        private bool foregroundWasEditing, foregroundWasReplay;
        private string foregroundPresentedFloor;
        private RunOutcomeRecord foregroundLatest;
        private Label foregroundPartyLabel;
        private Vector2 foregroundLabelSize;
        private float foregroundLabelFont;
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
                    // Normal play keeps activity visible below the scrolling HUD.
                    // Editing retains the existing compact chrome arrangement.
                    var scroll = (ScrollView)top;
                    if (IsEditing && label.parent != scroll.contentContainer) scroll.Add(label);
                    else if (!IsEditing && label.parent != safe) safe.Insert(safe.IndexOf(top) + 1, label);
                    label.style.display = view == null || observingReplay ? DisplayStyle.None : DisplayStyle.Flex;
                    if (view != null) {
                        label.text = view.RunId == null ? Text(view.StateKey) : Format("ui.living.active_format", view.RunId,
                            view.Step?.FloorIndex + 1 ?? 1, Text("ui.living.pending." + (view.AnnouncedAction?.ToString() ?? "RoomEntry")));
                        label.enableRichText = false;
                        if (view.RunId != null) {
                            label.text += "\n" + ProductionDungeonPresenter.ForegroundParty(view, Text, Culture);
                            string recent = ProductionDungeonPresenter.ForegroundInteraction(view, Text, ContentName, Culture);
                            if (!string.IsNullOrEmpty(recent)) label.text += "\n" + recent;
                        }
                        if (view.RunId != null && (IsEditing || observingReplay || !view.GeometryCompatible))
                            label.text += "\n" + Text(IsEditing ? "ui.living.editing" : observingReplay ? "ui.living.replay_hidden" : "ui.living.geometry_unavailable");
                        if (!IsEditing && view.RunId == null && latest?.SurvivalSummary?.RuleResolved == true &&
                            (view.StateKey == "ui.living.awaiting" || view.StateKey == "ui.living.not_eligible")) {
                            label.text = ProductionDungeonPresenter.ForegroundOutcome(latest, Text, Culture);
                        }
                    }
                }
                if (foregroundPartyLabel == null) {
                    foregroundPartyLabel = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
                    foregroundPartyLabel.name = "foregroundPartyLabel";
                    foregroundPartyLabel.AddToClassList("foreground-party-label");
                    document.rootVisualElement.Q("mapArea").Add(foregroundPartyLabel);
                }
                if (view?.RunId != null) {
                    foregroundPartyLabel.text = Format("ui.living.marker", view.Members.Count(m => m.Health > 0));
                    foregroundPartyLabel.tooltip = view.RunId;
                }
                foregroundLabelSize = Vector2.zero;
            }
            world?.PresentForeground(view, IsEditing || legacy || observingReplay || !root.ForegroundPresentationActive, root.ForegroundPresentationTime);
            PresentForegroundPartyLabel();
        }

        private void PresentForegroundPartyLabel()
        {
            if (foregroundPartyLabel == null) return;
            bool visible = world?.ForegroundPartyVisible == true;
            if (!visible) { foregroundPartyLabel.style.display = DisplayStyle.None; return; }
            float font = Mathf.Max(presentationPolicy.ReplayPartyTextSize, presentationPolicy.Text(textSize) * presentationPolicy.ReplayPartyTextScale);
            if (foregroundLabelFont != font || foregroundLabelSize == Vector2.zero || foregroundPartyLabel.style.fontSize.value.value != font) {
                foregroundLabelFont = font; foregroundPartyLabel.style.fontSize = font;
                foregroundLabelSize = foregroundPartyLabel.MeasureTextSize(foregroundPartyLabel.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            }
            var parent = foregroundPartyLabel.parent;
            var screen = worldCamera.WorldToScreenPoint(world.transform.TransformPoint(world.ForegroundPartyPosition));
            visible &= worldCamera.pixelRect.Contains(new Vector2(screen.x, screen.y));
            var point = parent.WorldToLocal(new Vector2(screen.x, Screen.height - screen.y) / document.panelSettings.scale);
            float width = Mathf.Min(foregroundLabelSize.x, Mathf.Min(presentationPolicy.ReplayPartyInfoWidth, parent.contentRect.width));
            float height = foregroundLabelSize.y;
            var placed = new Rect(Mathf.Clamp(point.x - width * .5f, 0, Mathf.Max(0, parent.contentRect.width - width)),
                Mathf.Clamp(point.y - height - presentationPolicy.ReplayActorLabelOffset, 0, Mathf.Max(0, parent.contentRect.height - height)), width, height);
            var rail = document.rootVisualElement.Q("floorRail");
            if (rail.resolvedStyle.display != DisplayStyle.None) {
                var blocked = parent.WorldToLocal(rail.worldBound);
                if (placed.Overlaps(blocked)) {
                    float gap = presentationPolicy.ReplayActorLabelOffset;
                    float leftWidth = Mathf.Max(0, blocked.xMin - gap), rightStart = Mathf.Min(parent.contentRect.width, blocked.xMax + gap);
                    float rightWidth = Mathf.Max(0, parent.contentRect.width - rightStart);
                    bool right = rightWidth >= leftWidth;
                    width = Mathf.Min(width, right ? rightWidth : leftWidth);
                    placed.width = width;
                    placed.x = Mathf.Clamp(point.x - width * .5f, right ? rightStart : 0,
                        right ? Mathf.Max(rightStart, parent.contentRect.width - width) : Mathf.Max(0, leftWidth - width));
                    visible &= width > 0;
                }
            }
            foregroundPartyLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) {
                foregroundPartyLabel.style.width = width; foregroundPartyLabel.style.left = placed.x; foregroundPartyLabel.style.top = placed.y;
            }
        }
    }
}
