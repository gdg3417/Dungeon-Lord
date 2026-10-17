using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private bool floorManagementOpen;
        private FloorConstructionPreview floorConstruction;
        private FloorLifecyclePreview floorLifecycle;
        private DetachedCanonicalSaveSession floorPreviewSession;
        private DetachedCanonicalSpatialSaveState floorPreviewState;
        private string floorPreviewSelection;
        private int floorPreviewConfigurationVersion;
        private bool floorPreviewPermit;
        private double floorPreviewMana;
        private bool CanManageFloors => initialized && HasCanonicalRuntime() && !IsEditing &&
            recoveryBytes == null && recoveryReason == null;

        private void InitializeFloorManagement()
        {
            Button("manageFloors", () => { floorManagementOpen = !floorManagementOpen; floorPreviewSession = null; Present(); });
            Button("constructFloor", () => ConstructFloor());
            Button("activateFloor", () => ChangeFloorLifecycle(FloorLifecycleAction.Activate));
            Button("deactivateFloor", () => ChangeFloorLifecycle(FloorLifecycleAction.Deactivate));
        }

        private bool FloorPreviewsNeedRefresh()
        {
            var service = root.SaveService;
            bool changed = floorPreviewSession != service.CanonicalSession || floorPreviewState != root.Save.validatedCanonicalSpatialState ||
                floorPreviewSelection != selectedFloor || floorPreviewConfigurationVersion != service.FloorPreviewConfigurationVersion ||
                floorPreviewPermit != service.HasFloorConstructionPermission(root.Save);
            double mana = root.Save.structureRuntime.ManaReserve;
            if (!changed && !mana.Equals(floorPreviewMana))
                changed = service.FloorPreviewAffordabilityChanged(root.Save, floorConstruction, floorLifecycle);
            floorPreviewMana = mana;
            return changed;
        }

        private void RefreshFloorManagementIfNeeded()
        {
            if (floorManagementOpen && CanManageFloors && FloorPreviewsNeedRefresh()) PresentFloorManagement(true);
        }

        private void PresentFloorManagement(bool refresh = false)
        {
            var ui = document.rootVisualElement;
            ui.Q("manageFloors").style.display = IsEditing ? DisplayStyle.None : DisplayStyle.Flex;
            ui.Q<Button>("manageFloors").SetEnabled(CanManageFloors);
            ui.Q("floorManagement").style.display = floorManagementOpen && !IsEditing ? DisplayStyle.Flex : DisplayStyle.None;
            if (!floorManagementOpen || !CanManageFloors) { floorPreviewSession = null; return; }
            var floor = root.Save.validatedCanonicalSpatialState.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);
            var action = floor?.ActivationState == FloorActivationState.Active ? FloorLifecycleAction.Deactivate : FloorLifecycleAction.Activate;
            if (refresh || FloorPreviewsNeedRefresh())
            {
                floorLifecycle = root.SaveService.PreviewFloorLifecycle(root.Save, action, selectedFloor);
                floorConstruction = root.SaveService.PreviewFloorConstruction(root.Save);
                floorPreviewSession = root.SaveService.CanonicalSession;
                floorPreviewState = root.Save.validatedCanonicalSpatialState;
                floorPreviewSelection = selectedFloor;
                floorPreviewConfigurationVersion = root.SaveService.FloorPreviewConfigurationVersion;
                floorPreviewPermit = root.SaveService.HasFloorConstructionPermission(root.Save);
                floorPreviewMana = root.Save.structureRuntime.ManaReserve;
            }
            ui.Q("activateFloor").style.display = floor?.ActivationState == FloorActivationState.Inactive ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q("deactivateFloor").style.display = floor?.ActivationState == FloorActivationState.Active ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("activateFloor").SetEnabled(floorLifecycle.IsCommittable);
            ui.Q<Button>("deactivateFloor").SetEnabled(floorLifecycle.IsCommittable);
            ui.Q<Label>("floorLifecycleReason").text = floor == null ? Text("floor.lifecycle.invalid_state") :
                floorLifecycle.Reason != null ? Text(floorLifecycle.Reason) : Text("ui.dungeon.floors.lifecycle_ready");

            bool opportunity = floorConstruction.Profile != null && floorConstruction.Reason != FloorConstructionService.AlreadyConstructedReason;
            ui.Q("constructFloor").style.display = opportunity ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("constructFloor").SetEnabled(opportunity && floorConstruction.IsCommittable);
            ui.Q<Label>("floorConstructionInfo").text = opportunity ? Format("ui.dungeon.floors.opportunity",
                Format("ui.dungeon.floor", floorConstruction.Profile.FloorIndex + 1),
                PlayerManaPresentationFormatter.FormatDiscreteAmount(floorConstruction.Profile.ConstructionMana, Culture),
                Text(CompletedResearchStateResolver.HasCompletedProject(root.Save.completedResearch, floorConstruction.Profile.RequiredResearchId)
                    ? "ui.dungeon.floors.permit_met" : "ui.dungeon.floors.permit_missing")) : Text("ui.dungeon.floors.no_opportunity");
            ui.Q<Label>("floorConstructionReason").text = opportunity && floorConstruction.Reason != null ? Text(floorConstruction.Reason) : string.Empty;
        }

        public bool ConstructFloor()
        {
            RefreshFloorManagementIfNeeded();
            if (!CanManageFloors || !floorManagementOpen || floorConstruction?.IsCommittable != true ||
                modal.style.display == DisplayStyle.Flex) return false;
            var result = root.SaveService.CommitFloorConstruction(root.Save, floorConstruction);
            feedback = result.IsSuccess ? "ui.floor.success" : result.Reason;
            Present(); return result.IsSuccess;
        }

        public bool ChangeFloorLifecycle(FloorLifecycleAction action)
        {
            RefreshFloorManagementIfNeeded();
            if (!CanManageFloors || !floorManagementOpen || floorLifecycle?.IsCommittable != true ||
                floorLifecycle.Action != action || floorLifecycle.FloorInstanceId != selectedFloor ||
                modal.style.display == DisplayStyle.Flex) return false;
            // A direct click gets current runtime inputs. A modal keeps this exact
            // captured preview and must fail closed if inputs change while it is open.
            PresentFloorManagement(true);
            if (!floorLifecycle.IsCommittable || floorLifecycle.Action != action) return false;
            var preview = floorLifecycle;
            if (preview.RequiresConfirmation)
            {
                ShowFloorLifecycleConfirmation(preview);
                return false;
            }
            return CommitFloorLifecycle(preview);
        }

        private void ShowFloorLifecycleConfirmation(FloorLifecyclePreview preview)
        {
            ShowModal("ui.dungeon.floors.cascade", () => CommitFloorLifecycle(preview), "ui.dungeon.floors.deactivate");
            document.rootVisualElement.Q<Label>("modalText").text = Format("ui.dungeon.floors.cascade",
                string.Join(", ", preview.Changes.Select(f => Format("ui.dungeon.floor", f.FloorIndex + 1))));
            document.rootVisualElement.Q<Button>("cancel").text = Text("ui.dungeon.construction.cancel");
        }

        private bool CommitFloorLifecycle(FloorLifecyclePreview preview)
        {
            if (!CanManageFloors || selectedFloor != preview.FloorInstanceId)
            { feedback = "floor.lifecycle.stale_preview"; Present(); return false; }
            var result = root.SaveService.CommitFloorLifecycle(root.Save, preview.Action, preview.FloorInstanceId, preview);
            feedback = result.IsSuccess ? "ui.floor.lifecycle_success" : result.Reason;
            Present(); return result.IsSuccess;
        }
    }
}
