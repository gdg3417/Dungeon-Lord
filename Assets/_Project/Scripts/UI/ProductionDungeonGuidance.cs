using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        [Flags]
        private enum Tutorial
        {
            None = 0, Active = 1, Review = 2, Edit = 4, Rooms = 8,
            Analysis = 64, Finished = 128, Activity = 256, MonsterPreview = 512, LootPreview = 1024
        }

        private static readonly ConditionalWeakTable<SaveService, object> tutorialBoots = new();
        private static readonly string[] objectiveKeys = { "first_room", "prepare", "visit", "results", "revise" };
        private VisualElement nextSteps, tutorialFooter, tutorialOutline, tutorialPointer, tutorialTarget;
        private Label tutorialHint;
        private Button nextStepsToggle;
        private Button[] objectives;
        private VisualElement tutorialScrim;
        private Button tutorialStyledButton;
        private bool tutorialBrowserChosen;
        private string tutorialPreference, tutorialHintKey;
        private Tutorial tutorial;
        private bool guidanceExpanded, firstRoomConfirmed, canWatchArrival, lastGuidanceBlocked;
        private Rect? tutorialRoom;
        private Rect lastTutorialBounds, lastTutorialScreen;
        private IVisualElementScheduledItem dismissActivity;

        private bool HasGuidanceReport => analysisReport?.Runs > 0;
        private bool GuidanceBlocked => recoveryBytes != null || recoveryReason != null ||
            root.SaveService.RequiresCanonicalRecovery || root.TimeService?.RecoveryClockActive == true;
        private bool CanUseObjectives => !legacy && !IsEditing && !IsAnalysis && !GuidanceBlocked &&
            modal.style.display != DisplayStyle.Flex && document.rootVisualElement.Q("displayPopover").style.display != DisplayStyle.Flex;

        internal static string TutorialPreferenceKey(string path, long created)
        {
            string identity = Path.GetFullPath(path);
            if (Path.DirectorySeparatorChar == '\\') identity = identity.ToUpperInvariant();
            return "dungeon.tutorial.v1." + SpatialContractSha256.Compute(Encoding.UTF8.GetBytes(
                identity + "\n" + created.ToString(CultureInfo.InvariantCulture)));
        }

        private bool TutorialActive => (tutorial & (Tutorial.Active | Tutorial.Finished)) == Tutorial.Active;
        private bool Learned(Tutorial action) => (tutorial & action) != 0;

        private void InitializeGuidance()
        {
            var ui = document.rootVisualElement;
            nextSteps = ui.Q("nextSteps");
            nextStepsToggle = ui.Q<Button>("toggleNextSteps");
            nextStepsToggle.BringToFront();
            nextSteps.BringToFront();
            objectives = objectiveKeys.Select(key => ui.Q<Button>("objective_" + key)).ToArray();
            tutorialFooter = ui.Q("tutorialFooter");
            tutorialHint = ui.Q<Label>("tutorialHint");
            tutorialScrim = ui.Q("tutorialScrim");
            tutorialOutline = ui.Q("tutorialOutline");
            tutorialPointer = ui.Q("tutorialPointer");
            tutorialFooter.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float footerHeight = tutorialFooter.resolvedStyle.display == DisplayStyle.None ? 0 : tutorialFooter.layout.height;
                modal.style.bottom = footerHeight;
                ui.Q("analysisScreen").style.bottom = footerHeight;
            });
            tutorialPreference = TutorialPreferenceKey(root.SaveService.SavePath, root.Save.createdUtcUnix);
            if (root.SaveService.LastLoadCreatedNative && !tutorialBoots.TryGetValue(root.SaveService, out _))
            {
                tutorialBoots.Add(root.SaveService, new object());
                tutorial = Tutorial.Active;
                StoreTutorial();
            }
            else tutorial = (Tutorial)PlayerPrefs.GetInt(tutorialPreference, (int)Tutorial.None);

            Button("toggleNextSteps", () =>
            {
                if (!CanUseObjectives) return;
                guidanceExpanded = !guidanceExpanded;
                PresentGuidance();
            });
            for (int index = 0; index < objectives.Length; index++)
            {
                int objective = index;
                objectives[index].clicked += () => FollowObjective(objective);
            }
            Button("skipTutorial", () => { Learn(Tutorial.Finished); PresentGuidance(); });
            Button("buildFirstRoom", () =>
            {
                if (NeedsFirstRoom && modal.style.display == DisplayStyle.Flex) Learn(Tutorial.Review);
                PresentGuidance();
            });
            Button("edit", () =>
            {
                if (IsEditing) Learn(Learned(Tutorial.Analysis) ? Tutorial.Finished : Tutorial.Edit);
                PresentGuidance();
            });
            Button("roomsCategory", () => { if (IsEditing && !detailsCollapsed) Learn(Tutorial.Rooms); PresentGuidance(); });
            Button("openAnalysis", () => { if (IsAnalysis && HasGuidanceReport) Learn(Tutorial.Analysis); PresentGuidance(); });
        }

        private void StoreTutorial()
        {
            PlayerPrefs.SetInt(tutorialPreference, (int)tutorial);
            PlayerPrefs.Save();
        }

        private void Learn(Tutorial action)
        {
            if (!TutorialActive || Learned(action) || (GuidanceBlocked && action != Tutorial.Finished)) return;
            tutorial |= action;
            StoreTutorial();
        }

        private Tutorial PreviewLesson => contentCategory switch
        {
            MvpDungeonPlacementIds.MonsterCategoryId => Tutorial.MonsterPreview,
            MvpDungeonPlacementIds.LootNodeCategoryId => Tutorial.LootPreview,
            _ => Tutorial.None
        };

        private void FocusTutorialRoom(string roomId)
        {
            if (world.TryRoomBounds(roomId, out Rect bounds)) tutorialRoom = bounds;
        }

        private (VisualElement target, string hint) ContentTutorial(VisualElement ui)
        {
            if (IsPlacingContent)
            {
                if (contentPreview?.IsValid == true) return (ui.Q("cancelContent"), "preview");
                FocusTutorialRoom(contentIntent.RoomInstanceId);
                return (null, "preview_cell");
            }

            var choice = ui.Q("contentChoices").Children().OfType<Button>().FirstOrDefault(button => button.enabledInHierarchy);
            if (choice == null) return (ui.Q(contentBrowserOwned ? "newContent" : "ownedContent"), "other_browser");
            if (!tutorialBrowserChosen)
                return (ui.Q(contentBrowserOwned ? "ownedContent" : "newContent"), "browser");
            return (choice, "option");
        }

        private void FollowObjective(int index)
        {
            if (!CanUseObjectives) return;
            guidanceExpanded = false;
            PresentGuidance();
            var ui = document.rootVisualElement;
            switch (index)
            {
                case 0:
                    ui.Q<Button>(firstRoomConfirmed ? "edit" : "buildFirstRoom").Focus();
                    break;
                case 1:
                case 4:
                    ui.Q<Button>("edit").Focus();
                    break;
                case 2:
                    string floor = root.ForegroundActivity?.Parties.FirstOrDefault()?.FloorId ?? selectedFloor;
                    if (floor != null && floor != selectedFloor) SelectFloor(floor);
                    else FitFloor();
                    break;
                case 3:
                    if (HasGuidanceReport) OpenAnalysis();
                    break;
            }
        }

        private void PresentGuidance()
        {
            if (nextSteps == null) return;
            var ui = document.rootVisualElement;
            var living = root.Save.livingDungeon;
            firstRoomConfirmed = root.Save.validatedCanonicalSpatialState.Floors.Any(f => f.Layout.Rooms.Length > 0);
            bool admitted = living is { IsDormant: false } && living.RetainedCount > 0;
            canWatchArrival = living is { IsDormant: false } &&
                (living.Scheduler.NextOpportunityTime.HasValue || admitted);
            bool visitObserved = admitted || HasGuidanceReport;
            lastGuidanceBlocked = GuidanceBlocked;
            bool visible = CanUseObjectives;
            if (!visible) guidanceExpanded = false;
            nextStepsToggle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            nextSteps.style.display = guidanceExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            nextStepsToggle.tooltip = Text(guidanceExpanded ? "ui.dungeon.guidance.collapse" : "ui.dungeon.guidance.expand");
            PresentObjective(0, firstRoomConfirmed, true);
            PresentObjective(1, canWatchArrival || visitObserved, firstRoomConfirmed);
            PresentObjective(2, visitObserved, canWatchArrival || HasGuidanceReport);
            PresentObjective(3, false, HasGuidanceReport);
            PresentObjective(4, false, HasGuidanceReport);

            bool showTutorial = TutorialActive && !legacy;
            tutorialFooter.style.display = showTutorial ? DisplayStyle.Flex : DisplayStyle.None;
            bool hideStarterDetails = showTutorial && NeedsFirstRoom &&
                !IsEditing && !IsAnalysis && !GuidanceBlocked;
            safe.EnableInClassList("tutorial-first-room", hideStarterDetails);
            ui.Q("contextDetails").style.display = hideStarterDetails || detailsCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            safe.EnableInClassList("tutorial-before-visit", showTutorial && !HasGuidanceReport);
            if (!showTutorial)
            {
                modal.style.bottom = 0;
                ui.Q("analysisScreen").style.bottom = 0;
            }
            tutorialRoom = null;
            VisualElement target = null;
            string hint = !showTutorial ? null : GuidanceBlocked || modal.style.display == DisplayStyle.Flex
                ? "dialog" : IsAnalysis ? "return" : guidanceExpanded ? tutorialHintKey ?? "explore" : "explore";
            if (showTutorial && !GuidanceBlocked && !guidanceExpanded && !IsAnalysis &&
                modal.style.display != DisplayStyle.Flex && ui.Q("displayPopover").style.display != DisplayStyle.Flex)
            {
                if (NeedsFirstRoom && !IsEditing)
                    (target, hint) = (ui.Q("buildFirstRoom"), Learned(Tutorial.Review) ? "build_resume" : "build");
                else if (firstRoomConfirmed && !IsEditing)
                {
                    if (Learned(Tutorial.Analysis))
                        (target, hint) = (ui.Q("edit"), "revise");
                    else if (!Learned(Tutorial.Edit))
                        (target, hint) = (ui.Q("edit"), "edit");
                    else if (HasGuidanceReport)
                        (target, hint) = (ui.Q("openAnalysis"), "analysis");
                    else if (canWatchArrival && !Learned(Tutorial.Activity))
                        (target, hint) = (ui.Q("foregroundActivity"), "activity");
                }
                else if (IsEditing && draft.Durability == DraftDurability.Acknowledged)
                {
                    if (IsPlacingContent || PreviewLesson != Tutorial.None && !Learned(PreviewLesson) && reassigning == null)
                        (target, hint) = ContentTutorial(ui);
                    else if (IsConstructing)
                        (target, hint) = (ui.Q("cancelPlacement"), "preview");
                    else if (!Learned(Tutorial.Rooms))
                        (target, hint) = (ui.Q("roomsCategory"), "rooms");
                    else if (!Learned(Tutorial.MonsterPreview) || !Learned(Tutorial.LootPreview))
                    {
                        if (ContextRoomId == null)
                        {
                            var room = renderState.Floors.FirstOrDefault(f => f.FloorInstanceId == selectedFloor)?.Layout.Rooms.FirstOrDefault();
                            if (room != null) FocusTutorialRoom(room.RoomInstanceId);
                            hint = "room";
                        }
                        else if (!Learned(Tutorial.MonsterPreview))
                            (target, hint) = (ui.Q("monstersCategory"), "monsters");
                        else (target, hint) = (ui.Q("lootCategory"), "loot");
                    }
                }
            }
            if (target != tutorialTarget && target is Button button)
                button.schedule.Execute(() =>
                {
                    if (button == tutorialTarget) button.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(button);
                });
            tutorialTarget = target;
            if (tutorialHintKey != hint)
            {
                tutorialHintKey = hint;
                tutorialHint.text = hint == null ? string.Empty : Text("ui.dungeon.tutorial." + hint);
            }
            PositionTutorial();
        }

        private void PresentObjective(int index, bool complete, bool available)
        {
            var button = objectives[index];
            string label = Text("ui.dungeon.objective." + objectiveKeys[index]);
            button.text = complete ? string.Format(Culture,
                root.Content.GetString("ui.dungeon.objective.completed", "✓ {0}"), label) : label;
            button.tooltip = button.text;
            button.EnableInClassList("objective-complete", complete);
            button.SetEnabled(available);
        }

        // Read only the selected target and its clipping ancestors. The outline never picks input.
        // World targets follow the established camera transform and remain inside the viewport.
        private void PositionTutorial()
        {
            if (tutorialOutline == null) return;
            if (guidanceExpanded && nextStepsToggle.panel != null)
            {
                float popoverTop = safe.WorldToLocal(nextStepsToggle.worldBound.max).y + 4;
                if (!float.IsNaN(popoverTop) && nextSteps.style.top.value.value != popoverTop)
                    nextSteps.style.top = popoverTop;
            }
            Rect bounds = default;
            if (tutorialTarget?.panel != null && tutorialTarget.enabledInHierarchy && tutorialTarget.visible)
            {
                bounds = tutorialTarget.worldBound;
                for (var element = tutorialTarget; element != null; element = element.parent)
                {
                    if (element.resolvedStyle.display == DisplayStyle.None) { bounds = default; break; }
                    if (element is ScrollView scroll) bounds = Clip(bounds, scroll.contentViewport.worldBound);
                    if (element == safe) break;
                }
            }
            else if (tutorialRoom is Rect room && worldCamera != null && safe.panel != null)
            {
                var min = worldCamera.WorldToScreenPoint(room.min);
                var max = worldCamera.WorldToScreenPoint(room.max);
                var topLeft = RuntimePanelUtils.ScreenToPanel(safe.panel, new Vector2(min.x, Screen.height - max.y));
                var bottomRight = RuntimePanelUtils.ScreenToPanel(safe.panel, new Vector2(max.x, Screen.height - min.y));
                bounds = Clip(Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y), viewportElement.worldBound);
            }
            bounds = Clip(bounds, safe.worldBound);
            bool focusVisible = bounds.width >= 4 && bounds.height >= 4;
            var styledButton = focusVisible ? tutorialTarget as Button : null;
            if (styledButton != tutorialStyledButton)
            {
                tutorialStyledButton?.RemoveFromClassList("tutorial-button-target");
                tutorialStyledButton = styledButton;
                tutorialStyledButton?.AddToClassList("tutorial-button-target");
            }
            if (!focusVisible)
            {
                tutorialScrim.style.display = tutorialOutline.style.display = tutorialPointer.style.display = DisplayStyle.None;
                dismissActivity?.Pause();
                dismissActivity = null;
                lastTutorialBounds = default;
                return;
            }
            bounds.position = safe.WorldToLocal(bounds.position);
            if (tutorialHintKey == "activity" && dismissActivity == null)
                dismissActivity = tutorialFooter.schedule.Execute(() =>
                {
                    dismissActivity = null;
                    if (tutorialHintKey != "activity") return;
                    Learn(Tutorial.Activity);
                    PresentGuidance();
                }).StartingIn(5000);
            else if (tutorialHintKey != "activity" && dismissActivity != null)
            {
                dismissActivity.Pause();
                dismissActivity = null;
            }
            Rect screen = document.rootVisualElement.worldBound;
            screen.position = safe.WorldToLocal(screen.position);
            if (bounds == lastTutorialBounds && screen == lastTutorialScreen) return;
            lastTutorialBounds = bounds;
            lastTutorialScreen = screen;
            // These four shades cover the screen up to the same clipped bounds used by the outline.
            // Draw the footer above the scrim so its instruction and Skip stay fully lit.
            SetShade(tutorialScrim[0], screen.xMin, screen.yMin, screen.width, bounds.yMin - screen.yMin);
            SetShade(tutorialScrim[1], screen.xMin, bounds.yMax, screen.width, screen.yMax - bounds.yMax);
            SetShade(tutorialScrim[2], screen.xMin, bounds.yMin, bounds.xMin - screen.xMin, bounds.height);
            SetShade(tutorialScrim[3], bounds.xMax, bounds.yMin, screen.xMax - bounds.xMax, bounds.height);
            tutorialScrim.style.display = tutorialOutline.style.display = tutorialPointer.style.display = DisplayStyle.Flex;
            tutorialOutline.style.left = bounds.xMin;
            tutorialOutline.style.top = bounds.yMin;
            tutorialOutline.style.width = bounds.width;
            tutorialOutline.style.height = bounds.height;
            bool above = bounds.yMin >= 16;
            tutorialPointer.EnableInClassList("tutorial-pointer-below", !above);
            tutorialPointer.style.left = Mathf.Clamp(bounds.center.x - 7, 0, Mathf.Max(0, safe.layout.width - 14));
            tutorialPointer.style.top = above ? bounds.yMin - 13 : bounds.yMax;
        }

        private static void SetShade(VisualElement shade, float x, float y, float width, float height)
        {
            shade.style.left = x;
            shade.style.top = y;
            shade.style.width = Mathf.Max(0, width);
            shade.style.height = Mathf.Max(0, height);
        }

        private static Rect Clip(Rect a, Rect b)
        {
            float left = Mathf.Max(a.xMin, b.xMin), top = Mathf.Max(a.yMin, b.yMin);
            float right = Mathf.Min(a.xMax, b.xMax), bottom = Mathf.Min(a.yMax, b.yMax);
            return right > left && bottom > top ? Rect.MinMaxRect(left, top, right, bottom) : default;
        }
    }
}
