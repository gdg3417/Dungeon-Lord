using System.Linq;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private bool guidanceExpanded;
        private bool CanFollowGuidance => HasCanonicalRuntime() && !IsEditing && !IsAnalysis && !legacy &&
            modal.style.display != DisplayStyle.Flex && recoveryBytes == null && recoveryReason == null;

        // Read the cached Analysis window and committed scheduler. Outcomes include losses.
        private bool HasGuidanceReport => !NeedsFirstRoom && analysisReport.Runs > 0;
        private bool CanWatchArrival => !NeedsFirstRoom && !HasGuidanceReport &&
            root.Save.livingDungeon is { } living &&
            (living.Scheduler.NextOpportunityTime.HasValue || living.RetainedCount > 0);

        private void InitializeGuidance()
        {
            Button("toggleNextSteps", () =>
            {
                guidanceExpanded = !guidanceExpanded;
                PresentGuidance();
            });
            Button("nextStepAction", FollowGuidance);
            Button("nextStepsRevise", () => { if (CanFollowGuidance && HasGuidanceReport) EnterEdit(); });
        }

        private void FollowGuidance()
        {
            if (!CanFollowGuidance) return;
            if (NeedsFirstRoom) ReviewFirstRoom();
            else if (HasGuidanceReport) OpenAnalysis();
            else if (CanWatchArrival)
            {
                string floor = root.ForegroundActivity?.Parties.FirstOrDefault()?.FloorId ??
                    root.Save.validatedCanonicalSpatialState.Floors.FirstOrDefault(f => f.FloorIndex == 0)?.FloorInstanceId;
                if (floor != null && floor != selectedFloor) SelectFloor(floor);
                else FitFloor();
            }
            else EnterEdit();
        }

        private void PresentGuidance()
        {
            var ui = document.rootVisualElement;
            bool needsRoom = NeedsFirstRoom;
            bool hasReport = HasGuidanceReport;
            bool watch = CanWatchArrival;
            var card = ui.Q("nextSteps");
            card.EnableInClassList("next-steps-expanded", guidanceExpanded);
            var toggle = ui.Q<Button>("toggleNextSteps");
            toggle.text = Text(guidanceExpanded ? "ui.dungeon.guidance.collapse" : "ui.dungeon.guidance.expand");
            toggle.tooltip = toggle.text;
            var action = ui.Q<Button>("nextStepAction");
            action.text = Text(needsRoom ? "ui.dungeon.first_room.action" : hasReport ?
                "ui.dungeon.guidance.analysis" : watch ? "ui.dungeon.guidance.watch" : "ui.dungeon.edit");
            action.tooltip = action.text;
            bool recoveryBlocked = recoveryBytes != null || recoveryReason != null;
            action.SetEnabled(!recoveryBlocked);
            ui.Q<Button>("nextStepsRevise").SetEnabled(!recoveryBlocked);
            ui.Q<Label>("nextStepsInstruction").text = Text(needsRoom ? "ui.dungeon.guidance.build" :
                hasReport ? "ui.dungeon.guidance.inspect" : "ui.dungeon.guidance.construct");
            ui.Q("nextStepsInstruction").EnableInClassList("guidance-hidden", watch);
            ui.Q("nextStepsArrival").EnableInClassList("guidance-hidden", needsRoom || hasReport);
            ui.Q("nextStepsRevise").EnableInClassList("guidance-hidden", !hasReport);
            ui.Q("nextStepsCompare").EnableInClassList("guidance-hidden", !hasReport);
        }
    }
}
