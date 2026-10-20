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
            var view = root?.ForegroundActivity?.ForFloor(selectedFloor);
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
                        if (view.Waiting) label.text = Text("ui.living.waiting") + "\n" + label.text;
                        if (view.Descended) label.text += "\n" + Text("ui.living.descended");
                        if (view.Decision.HasValue) label.text += "\n" + Text(view.Decision.Value ? "ui.living.decision.descend" : "ui.living.decision.exit");
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
            if (world == null || !world.TryForegroundPartyBounds(out var bounds)) {
                foregroundPartyLabel.style.display = DisplayStyle.None; return;
            }
            float font = Mathf.Max(presentationPolicy.ReplayPartyTextSize, presentationPolicy.Text(textSize) * presentationPolicy.ReplayPartyTextScale);
            if (foregroundLabelFont != font || foregroundPartyLabel.style.fontSize.value.value != font) {
                foregroundLabelFont = font; foregroundPartyLabel.style.fontSize = font;
                foregroundLabelSize = Vector2.zero;
            }
            // Wait for the changed font to resolve before caching its metrics.
            // The explicit USS box has no margin/padding/border to add afterward.
            if (!Mathf.Approximately(foregroundPartyLabel.resolvedStyle.fontSize, font)) {
                foregroundPartyLabel.style.display = DisplayStyle.None; return;
            }
            if (foregroundLabelSize == Vector2.zero) {
                foregroundLabelSize = foregroundPartyLabel.MeasureTextSize(foregroundPartyLabel.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            }
            var parent = foregroundPartyLabel.parent;
            Vector2 PanelPoint(Vector3 screen) => parent.WorldToLocal(new Vector2(screen.x, Screen.height - screen.y) / document.panelSettings.scale);
            var screen = worldCamera.WorldToScreenPoint(bounds.center);
            var min = PanelPoint(worldCamera.WorldToScreenPoint(bounds.min));
            var max = PanelPoint(worldCamera.WorldToScreenPoint(bounds.max));
            var icon = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
            var pixels = worldCamera.pixelRect;
            min = PanelPoint(new Vector3(pixels.xMin, pixels.yMax)); max = PanelPoint(new Vector3(pixels.xMax, pixels.yMin));
            var area = Rect.MinMaxRect(Mathf.Max(parent.contentRect.xMin, min.x), Mathf.Max(parent.contentRect.yMin, min.y),
                Mathf.Min(parent.contentRect.xMax, max.x), Mathf.Min(parent.contentRect.yMax, max.y));
            var rail = document.rootVisualElement.Q("floorRail");
            bool railVisible = rail.resolvedStyle.display != DisplayStyle.None;
            var blocked = parent.WorldToLocal(rail.worldBound);
            float width = Mathf.Min(Mathf.Ceil(foregroundLabelSize.x), presentationPolicy.ReplayPartyInfoWidth);
            // UI Toolkit rounds the final box to physical pixels. Reserve one
            // pixel so that rounding cannot consume the configured clear gap.
            float height = Mathf.Ceil(foregroundLabelSize.y), gap = presentationPolicy.ReplayActorLabelOffset + 1f / document.panelSettings.scale;
            bool visible = pixels.Contains(new Vector2(screen.x, screen.y)) && width > 0 && height > 0 && width <= area.width && height <= area.height;
            bool found = false; Rect placed = default;
            // Bounded screen-space inspection only: four sides, with at most
            // three alignments to clear the existing rail. Never clamp toward
            // the icon or move/hide the icon to make an optional caption fit.
            for (int side = 0; visible && side < 4 && !found; side++)
                for (int shift = 0; shift < (railVisible ? 3 : 1) && !found; shift++) {
                    float x = side < 2 ? icon.center.x - width * .5f : side == 2 ? icon.xMax + gap : icon.xMin - gap - width;
                    float y = side < 2 ? side == 0 ? icon.yMin - gap - height : icon.yMax + gap : icon.center.y - height * .5f;
                    if (side < 2) {
                        if (shift > 0) x = shift == 1 ? blocked.xMax + gap : blocked.xMin - gap - width;
                        x = Mathf.Clamp(x, area.xMin, area.xMax - width);
                    } else {
                        if (shift > 0) y = shift == 1 ? blocked.yMax + gap : blocked.yMin - gap - height;
                        y = Mathf.Clamp(y, area.yMin, area.yMax - height);
                    }
                    var candidate = new Rect(x, y, width, height);
                    if (candidate.xMin < area.xMin || candidate.yMin < area.yMin || candidate.xMax > area.xMax || candidate.yMax > area.yMax ||
                        candidate.Overlaps(icon) || railVisible && candidate.Overlaps(blocked)) continue;
                    placed = candidate; found = true;
                }
            visible &= found;
            foregroundPartyLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) {
                foregroundPartyLabel.style.width = width; foregroundPartyLabel.style.height = height;
                foregroundPartyLabel.style.left = placed.x; foregroundPartyLabel.style.top = placed.y;
            }
        }
    }
}
