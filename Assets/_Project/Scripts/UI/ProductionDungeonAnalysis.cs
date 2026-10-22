using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        public bool IsAnalysis { get; private set; }
        private bool navigationReview;
        private RunOutcomeRecord[] analysisSource;
        private ProductionAnalysisReport analysisReport;

        private void InitializeAnalysis()
        {
            Button("openAnalysis", OpenAnalysis);
            Button("analysisDungeon", ReturnToDungeon);
            Button("analysisDisplay", () => document.rootVisualElement.Q("displayPopover").style.display = DisplayStyle.Flex);
            Button("navigationDiscard", () => { CloseModal(); if (Discard()) ShowAnalysis(); });
            document.rootVisualElement.Q<ScrollView>("analysisScroll").verticalScrollerVisibility = ScrollerVisibility.Auto;
        }

        private bool CanSaveForNavigation => draft?.CanSave == true &&
            draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState) == draft.BaselineFingerprint;

        public void OpenAnalysis()
        {
            if (!initialized || !HasCanonicalRuntime() || IsAnalysis || legacy) return;
            if (recoveryBytes != null || recoveryReason != null) { OfferRecovery(); return; }
            if (modal.style.display == DisplayStyle.Flex) return;
            if (!IsEditing) { ShowAnalysis(); return; }
            if (!draft.HasChanges) { if (Discard()) ShowAnalysis(); return; }

            // The existing authority decides save eligibility (including durability and staleness).
            // Keep Save visible but disabled for temporarily unavailable saves; invalid drafts omit it.
            if (draft.IsStructurallyValid)
            {
                ReviewSave();
                if (modal.style.display != DisplayStyle.Flex)
                    ShowModal("ui.analysis.leave_edit", null, "ui.dungeon.save");
                else document.rootVisualElement.Q<Label>("modalText").text = Text("ui.analysis.leave_edit") + "\n" +
                    document.rootVisualElement.Q<Label>("modalText").text;
                confirm = () => { if (CanSaveForNavigation && Commit()) ShowAnalysis(); };
                document.rootVisualElement.Q<Button>("navigationDiscard").style.display = DisplayStyle.Flex;
            }
            else ShowModal("ui.analysis.leave_invalid", () => { if (Discard()) ShowAnalysis(); }, "ui.analysis.discard_changes");
            navigationReview = true;
            document.rootVisualElement.Q<Button>("cancel").text = Text("ui.analysis.continue_editing");
            PresentNavigationReview();
        }

        private void PresentNavigationReview()
        {
            if (!navigationReview) return;
            document.rootVisualElement.Q<Button>("confirm").SetEnabled(!draft.IsStructurallyValid || CanSaveForNavigation);
            string reason = draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState) != draft.BaselineFingerprint ?
                TransactionalDungeonDraft.StaleReason : draft.Durability != DraftDurability.Acknowledged ? draft.Reason : draft.StructuralReason;
            document.rootVisualElement.Q<Label>("navigationStatus").text = reason == null ? string.Empty : Text(reason);
        }

        private void ShowAnalysis()
        {
            gesture.Cancel(); pointerDown = false;
            CloseReplay();
            IsAnalysis = true;
            document.rootVisualElement.Q("displayPopover").style.display = DisplayStyle.None;
            document.rootVisualElement.Q("analysisScreen").style.display = DisplayStyle.Flex;
            SetDungeonNavigationEnabled(false);
            document.rootVisualElement.Q<Button>("analysisDungeon").Focus();
            worldCamera.enabled = false;
            PresentAnalysis();
        }

        public void ReturnToDungeon()
        {
            if (!initialized || !HasCanonicalRuntime() || !IsAnalysis) return;
            IsAnalysis = false;
            document.rootVisualElement.Q("analysisScreen").style.display = DisplayStyle.None;
            document.rootVisualElement.Q("displayPopover").style.display = DisplayStyle.None;
            SetDungeonNavigationEnabled(true);
            worldCamera.enabled = true;
            Present(); Layout();
            document.rootVisualElement.Q<Button>("openAnalysis").Focus();
        }

        private void SetDungeonNavigationEnabled(bool value)
        {
            top.SetEnabled(value); bottom.SetEnabled(value);
            document.rootVisualElement.Q("mapArea").SetEnabled(value);
        }

        private void PresentAnalysis()
        {
            // Append/reopen replaces the retained array. No aggregate processing on frame ticks.
            var source = root.Save.runHistory?.RecentOutcomes;
            if (analysisReport == null || !ReferenceEquals(source, analysisSource))
            {
                analysisSource = source;
                analysisReport = ProductionAnalysisPresenter.Capture(root.Save.runHistory);
            }
            var ui = document.rootVisualElement;
            var entry = ui.Q<Button>("openAnalysis");
            var host = IsEditing ? ui.Q("displayPopover") : ui.Q("sheetTitle").parent;
            if (entry.parent != host)
            {
                if (IsEditing) host.Add(entry);
                else host.Insert(1, entry);
            }
            entry.text = IsEditing ? Text("ui.analysis.title") : string.Empty;
            PresentPerformanceSummary();
            ui.Q<Label>("analysisReport").text = ProductionAnalysisPresenter.Details(analysisReport, Text, Culture);
            // Detailed causal evidence remains owned and validated by the existing replay presenter.
            ui.Q<Label>("analysisEvidence").text = Text("ui.analysis.evidence");
            ui.Q("performanceSummary").style.display = IsEditing ? DisplayStyle.None : DisplayStyle.Flex;
            PresentNavigationReview();
        }

        private void PresentPerformanceSummary()
        {
            if (analysisReport == null) return;
            var ui = document.rootVisualElement;
            string compact = ProductionAnalysisPresenter.Compact(analysisReport, Text, Culture);
            var summary = ui.Q<Label>("performanceSummary");
            bool bounded = replay != null || safe.ClassListContains("compact-hud");
            summary.text = bounded ? compact.Replace("\n", " ") : compact;
            summary.EnableInClassList("bounded-summary", bounded);
            summary.EnableInClassList("replay-summary", replay != null);
            ui.Q<Button>("openAnalysis").tooltip = compact;
        }
    }
}
