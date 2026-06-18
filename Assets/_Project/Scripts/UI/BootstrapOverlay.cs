using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public class BootstrapOverlay : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text overlayText;

        private const int DiagnosticsPageCount = 9;
        private const int RuntimeSummaryPage = 0;
        private const int RunDiagnosticsPage = 1;
        private const int HeatDiagnosticsPage = 2;
        private const int SystemsDiagnosticsPage = 3;
        private const int ResearchDiagnosticsPage = 4;
        private const int ResearchStatusPresentationDiagnosticsPage = 5;
        private const int ResearchStatusSafetyDiagnosticsPage = 6;
        private const int ResearchVerificationBoundaryDiagnosticsPage = 7;
        private const int ResearchVerificationSafetyDiagnosticsPage = 8;
        private const int VisibleDiagnosticsBodyLineCount = 4;
        private const int VisiblePlayerFacingLineCount = 28;
        private const int PlayerFacingSectionCount = 4;
        private const int PlayerFacingSectionFull = 0;
        private const int PlayerFacingSectionLoopSummary = 1;
        private const int PlayerFacingSectionPlanAndAction = 2;
        private const int PlayerFacingSectionLatestRunFeedback = 3;
        private const float MinimalMvpActionPanelWidth = 260f;
        private const float MinimalMvpActionPanelHeight = 420f;
        private const float MinimalMvpActionPanelMargin = 10f;
        private const float MinimalMvpActionPanelLabelHeight = 17f;
        private const float MinimalMvpActionPanelButtonHeight = 19f;
        private const float MinimalMvpActionPanelScrollBarWidth = 16f;
        private const float OverlayTextSafeLeftMargin = 24f;
        private const float OverlayTextSafeTopMargin = 14f;
        private const float OverlayTextSafeBottomMargin = 10f;
        private const float OverlayTextRightActionPanelReserve = MinimalMvpActionPanelWidth + (MinimalMvpActionPanelMargin * 2f) + OverlayTextSafeLeftMargin;
        private const float OverlayTextRightCollapsedActionPanelReserve = 96f;
        private const string DefaultMvpStructureId = StructureSimulationPass.ManaGeneratorBasicId;
        private const string DefaultMvpPlacementCategoryId = MvpDungeonPlacementIds.RoomCategoryId;
        private const string DefaultMvpPlacementOptionId = MvpDungeonPlacementIds.BasicRoomOptionId;
        private const string AddBasicRoomSlotButtonKey = "ui.mvp_room_slots.add_basic_room_slot_button";
        private const string AddBasicRoomSlotSuccessKey = "ui.mvp_room_slots.add_basic_room_slot_success";
        private const string AddBasicRoomSlotAlreadyExistsKey = "ui.mvp_room_slots.add_basic_room_slot_already_exists";

        private GameRoot _root;
        private bool _devPanelVisible;
        private bool _runDiagnosticsOnlyVisible;
        private bool _diagnosticsVisible;
        private int _fullDiagnosticsPage;
        private readonly int[] _fullDiagnosticsPageScrollOffsets = new int[DiagnosticsPageCount];
        private Vector2 _devPanelScrollPosition;
        private Vector2 _minimalMvpActionPanelScrollPosition;
        private string _selectedMvpStructureId = DefaultMvpStructureId;
        private string _selectedMvpPlacementCategoryId = DefaultMvpPlacementCategoryId;
        private string _selectedMvpPlacementOptionId = DefaultMvpPlacementOptionId;
        private string _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
        private string _mvpStructurePlacementFeedback = string.Empty;
        private bool _roomSlotPlacementFailureIsLatestAction;
        private string _mvpRunResultFeedback = string.Empty;
        private AdventurerRunIntentSummary _lastRunIntentSummary;
        private string _lastRunPostureUsedId = string.Empty;
        private string _lastRunDebugPostureId = string.Empty;
        private bool _lastRunIntentFallbackUsed;
        private int _playerFacingScrollOffset;
        private bool _compactSmokeViewEnabled;
        private int _playerFacingSectionIndex;
        private bool _minimalMvpActionPanelCollapsed;
        private string _smokeViewportStatusMessage = string.Empty;

        public int FullDiagnosticsPageNumber => _fullDiagnosticsPage + 1;
        public int FullDiagnosticsScrollOffset => _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage];
        public int PlayerFacingScrollOffset => _playerFacingScrollOffset;
        public bool CompactSmokeViewEnabled => _compactSmokeViewEnabled;
        public int PlayerFacingSectionNumber => _playerFacingSectionIndex + 1;
        public bool MinimalMvpActionPanelCollapsed => _minimalMvpActionPanelCollapsed;
        public bool DevPanelVisible => _devPanelVisible;
        public bool DiagnosticsVisible => _diagnosticsVisible || _runDiagnosticsOnlyVisible;
        public bool PlayerFacingPanelsVisible => !_runDiagnosticsOnlyVisible;
        public bool MinimalMvpActionGuiVisible => _root != null && PlayerFacingPanelsVisible && !_minimalMvpActionPanelCollapsed;
        public Vector2 MinimalMvpActionPanelScrollPosition => _minimalMvpActionPanelScrollPosition;
        public string SelectedMvpStructureId => _selectedMvpStructureId;
        public string SelectedMvpPlacementCategoryId => _selectedMvpPlacementCategoryId;
        public string SelectedMvpPlacementOptionId => _selectedMvpPlacementOptionId;
        public string SelectedMvpRunPostureId => _selectedMvpRunPostureId;
        public string MvpStructurePlacementFeedback => _mvpStructurePlacementFeedback;
        public string MvpRunResultFeedback => _mvpRunResultFeedback;

        public void Bind(GameRoot root)
        {
            _root = root;
        }

        public void CycleFullDiagnosticsPage()
        {
            _fullDiagnosticsPage = (_fullDiagnosticsPage + 1) % DiagnosticsPageCount;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDevPanel()
        {
            _devPanelVisible = !_devPanelVisible;
        }

        public void ToggleRunDiagnosticsFocus()
        {
            _runDiagnosticsOnlyVisible = !_runDiagnosticsOnlyVisible;
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public void ToggleDiagnosticsVisibility()
        {
            _diagnosticsVisible = !_diagnosticsVisible;
            if (_diagnosticsVisible)
            {
                _fullDiagnosticsPage = RuntimeSummaryPage;
            }
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = 0;
        }

        public bool SelectMvpStructure(string structureId)
        {
            if (!IsAllowedMvpStructure(structureId))
            {
                return false;
            }

            _selectedMvpStructureId = structureId;
            return true;
        }

        public bool SelectMvpPlacementCategory(string categoryId)
        {
            if (!MvpDungeonPlacementIds.IsAllowedCategory(categoryId))
            {
                return false;
            }

            _selectedMvpPlacementCategoryId = categoryId;
            _selectedMvpPlacementOptionId = MvpDungeonPlacementIds.GetStarterOptionForCategory(categoryId);
            return !string.IsNullOrWhiteSpace(_selectedMvpPlacementOptionId);
        }

        public bool SelectMvpPlacementOption(string optionId)
        {
            if (!MvpDungeonPlacementIds.IsAllowedOption(optionId) ||
                !MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string categoryId) ||
                !string.Equals(categoryId, _selectedMvpPlacementCategoryId, System.StringComparison.Ordinal))
            {
                return false;
            }

            _selectedMvpPlacementOptionId = optionId;
            return true;
        }

        public string GetSelectedMvpStructureNameKey()
        {
            return GetMvpSelectionNameKey(_selectedMvpStructureId);
        }

        public string GetSelectedMvpStructureDisplayName()
        {
            return MvpPlayerFacingLabelResolver.ResolveStructureDisplayName(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpStructurePreviewText()
        {
            return MvpStructureImpactPreviewPresenter.BuildPreviewText(_selectedMvpStructureId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpPlacementPreviewText()
        {
            return MvpDungeonPlacementPresenter.BuildPreviewText(_selectedMvpPlacementOptionId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpRunPlanPreviewText()
        {
            return MvpStructureImpactPreviewPresenter.BuildRunPlanPreviewText(_selectedMvpStructureId, GetSelectedMvpRunPostureNameKey(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpRoomCapacityText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, _root.RunSimulationConfig);
            int selectedRoomIndex = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(_root.Save, layout);
            return MvpRoomSlotTargetPresenter.BuildSelectedCapacityText(layout, selectedRoomIndex, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string GetSelectedMvpPlacementFitText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, _root.RunSimulationConfig);
            int selectedRoomIndex = MvpRoomSlotTargetResolver.ResolveClampedSelectedRoomIndex(_root.Save, layout);
            return MvpRoomSlotTargetPresenter.BuildSelectedPlacementFitText(layout, selectedRoomIndex, _selectedMvpPlacementCategoryId, (key, fallback) => GetLocalizedString(key, fallback));
        }

        public bool SelectMvpRunPosture(string postureId)
        {
            if (!BootstrapMvpActionHandler.IsAllowedMvpRunPosture(postureId))
            {
                return false;
            }

            _selectedMvpRunPostureId = postureId;
            return true;
        }

        public string GetSelectedMvpRunPostureNameKey()
        {
            return GetMvpRunPostureNameKey(_selectedMvpRunPostureId);
        }

        public string GetSelectedMvpRunPostureDisplayName()
        {
            return GetLocalizedString(GetSelectedMvpRunPostureNameKey(), GetSelectedMvpRunPostureNameKey());
        }

        public void PlaceSelectedMvpStructure()
        {
            BootstrapMvpActionHandler.PlacementResult result = CreateMvpActionHandler().PlaceOrModifySelectedMvpPlacement(
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId);
            _mvpStructurePlacementFeedback = result.PlacementFeedback;
            _roomSlotPlacementFailureIsLatestAction = !result.Succeeded && IsRoomSlotPlacementFailureFeedback();
            RefreshOverlayText();
        }

        public void AddMvpBasicRoomSlot()
        {
            bool added = _root != null && _root.TryAddSecondMvpBasicRoomSlot();
            _mvpStructurePlacementFeedback = GetLocalizedString(added ? AddBasicRoomSlotSuccessKey : AddBasicRoomSlotAlreadyExistsKey);
            _roomSlotPlacementFailureIsLatestAction = false;
            _root?.SetBanner(_mvpStructurePlacementFeedback);
            RefreshOverlayText();
        }

        public void CycleSelectedMvpRoomSlotTarget()
        {
            _root?.CycleSelectedMvpRoomSlotTarget();
            ClearRoomSlotPlacementFailureFeedback();
            RefreshOverlayText();
        }

        public void RunOrObserveDungeon()
        {
            BootstrapMvpActionHandler.RunResult result = CreateMvpActionHandler().RunOrObserveDungeon(_selectedMvpRunPostureId);
            _mvpRunResultFeedback = result.RunFeedback;
            _lastRunIntentSummary = result.IntentSummary;
            _lastRunPostureUsedId = result.PostureUsedId;
            _lastRunDebugPostureId = result.DebugPostureId;
            _lastRunIntentFallbackUsed = result.IntentFallbackUsed;
            RefreshOverlayText();
        }

        public bool ResetCleanMvpValidationSessionFromDevPanel()
        {
            if (_root == null || !_root.ResetCleanMvpValidationSession())
            {
                return false;
            }

            _selectedMvpStructureId = DefaultMvpStructureId;
            _selectedMvpPlacementCategoryId = DefaultMvpPlacementCategoryId;
            _selectedMvpPlacementOptionId = DefaultMvpPlacementOptionId;
            _selectedMvpRunPostureId = RunPostureResolver.BalancedId;
            _mvpStructurePlacementFeedback = string.Empty;
            _roomSlotPlacementFailureIsLatestAction = false;
            _mvpRunResultFeedback = string.Empty;
            _lastRunIntentSummary = null;
            _lastRunPostureUsedId = string.Empty;
            _lastRunDebugPostureId = string.Empty;
            _lastRunIntentFallbackUsed = false;
            _playerFacingScrollOffset = 0;
            _compactSmokeViewEnabled = false;
            _playerFacingSectionIndex = PlayerFacingSectionFull;
            _minimalMvpActionPanelCollapsed = false;
            _smokeViewportStatusMessage = string.Empty;
            _root.SetBanner(GetLocalizedString("ui.banner.clean_mvp_validation_reset", "ui.banner.clean_mvp_validation_reset"));
            RefreshOverlayText();
            return true;
        }

        public void ScrollFullDiagnosticsLines(int lineDelta)
        {
            if (_runDiagnosticsOnlyVisible || !_diagnosticsVisible || lineDelta == 0)
            {
                return;
            }

            string[] bodyLines = BuildCurrentFullDiagnosticsBody().ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = Mathf.Clamp(
                _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] + lineDelta,
                0,
                maxOffset);
        }

        public void ScrollPlayerFacingTextLines(int lineDelta)
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible || lineDelta == 0)
            {
                return;
            }

            ClampPlayerFacingScrollOffset(_playerFacingScrollOffset + lineDelta);
        }

        public void JumpPlayerFacingTextToTop()
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible)
            {
                return;
            }

            _playerFacingScrollOffset = 0;
        }

        public void JumpPlayerFacingTextToBottom()
        {
            if (_runDiagnosticsOnlyVisible || _diagnosticsVisible)
            {
                return;
            }

            ClampPlayerFacingScrollOffset(int.MaxValue);
        }

        public void ToggleCompactSmokeView()
        {
            _compactSmokeViewEnabled = !_compactSmokeViewEnabled;
            _playerFacingScrollOffset = 0;
        }

        public void CyclePlayerFacingSmokeSection()
        {
            _playerFacingSectionIndex = (_playerFacingSectionIndex + 1) % PlayerFacingSectionCount;
            _playerFacingScrollOffset = 0;
        }

        public void ToggleMinimalMvpActionPanelCollapsed()
        {
            _minimalMvpActionPanelCollapsed = !_minimalMvpActionPanelCollapsed;
            _playerFacingScrollOffset = 0;
        }

        public string CopyFullSmokeTextToClipboard()
        {
            string smokeText = BuildFullPlayerFacingSmokeText();
            GUIUtility.systemCopyBuffer = smokeText;
            _smokeViewportStatusMessage = GetLocalizedString("ui.mvp_smoke.copy.confirmation");
            RefreshOverlayText();
            return smokeText;
        }

        public void RefreshOverlayText()
        {
            if (_root == null || overlayText == null)
            {
                return;
            }

            ApplyOverlayTextSafeArea();
            overlayText.text = BuildOverlayText();
        }

        public void ApplyOverlayTextSafeArea()
        {
            if (overlayText == null || overlayText.rectTransform == null)
            {
                return;
            }

            RectTransform rectTransform = overlayText.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.offsetMin = new Vector2(OverlayTextSafeLeftMargin, OverlayTextSafeBottomMargin);
            float rightReserve = _minimalMvpActionPanelCollapsed ? OverlayTextRightCollapsedActionPanelReserve : OverlayTextRightActionPanelReserve;
            rectTransform.offsetMax = new Vector2(-rightReserve, -OverlayTextSafeTopMargin);
            overlayText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleDevPanel();
            }
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ToggleRunDiagnosticsFocus();
            }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                CycleFullDiagnosticsPage();
            }
            if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
            {
                ToggleCompactSmokeView();
            }
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                CyclePlayerFacingSmokeSection();
            }
            if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                CopyFullSmokeTextToClipboard();
            }
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            {
                ToggleMinimalMvpActionPanelCollapsed();
            }
            if (Keyboard.current != null && Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                ScrollPlayerFacingTextLines(-VisiblePlayerFacingLineCount);
                ScrollFullDiagnosticsLines(-VisibleDiagnosticsBodyLineCount);
            }
            if (Keyboard.current != null && Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                ScrollPlayerFacingTextLines(VisiblePlayerFacingLineCount);
                ScrollFullDiagnosticsLines(VisibleDiagnosticsBodyLineCount);
            }
            if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
            {
                JumpPlayerFacingTextToTop();
            }
            if (Keyboard.current != null && Keyboard.current.endKey.wasPressedThisFrame)
            {
                JumpPlayerFacingTextToBottom();
            }
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0f)
                {
                    ScrollFullDiagnosticsLines(-1);
                }
                else if (scrollY < 0f)
                {
                    ScrollFullDiagnosticsLines(1);
                }
            }

            RefreshOverlayText();
        }

        private string BuildOverlayText()
        {
            var builder = new StringBuilder();
            if (!_runDiagnosticsOnlyVisible)
            {
                if (_diagnosticsVisible)
                {
                    AppendLine(builder, BootstrapSmokeTextComposer.BuildMvpLoopSummaryPanelText(
                        BuildSmokeTextContext(),
                        (key, fallback) => GetLocalizedString(key, fallback)));
                }
                else
                {
                    AppendScrolledPlayerFacingSmokeText(builder, BuildCurrentPlayerFacingSmokeText());
                }
                AppendLine(builder, string.Empty);
            }
            if (_runDiagnosticsOnlyVisible)
            {
                AppendHeader(builder);
                AppendRunDiagnostics(builder, includeBreakdownAndFeedback: false, includeHeatDiagnostics: true);
                return builder.ToString();
            }

            if (!_diagnosticsVisible)
            {
                return builder.ToString();
            }

            AppendHeader(builder);
            AppendScrolledFullDiagnosticsBody(builder, BuildCurrentFullDiagnosticsBody());
            return builder.ToString();
        }

        private BootstrapSmokeTextComposer.Context BuildSmokeTextContext()
        {
            ClearStaleRoomSlotPlacementFailureFeedback();
            MvpPlayerLoopSummary summary = _root.ResolveMvpPlayerLoopSummary();
            GuidedMvpActionPathSummary guidedPath = _root.ResolveGuidedMvpActionPath(summary);
            return new BootstrapSmokeTextComposer.Context(
                summary,
                guidedPath,
                MvpFirstSessionObjectivePresenter.Resolve(_root.Save, _root.RunSimulationConfig),
                MvpDungeonLayoutPresenter.BuildLayoutText(_root.Save, _root.RunSimulationConfig, _selectedMvpPlacementCategoryId, (key, fallback) => GetLocalizedString(key, fallback)),
                MvpDungeonPlacementPresenter.ResolveCategoryName(_selectedMvpPlacementCategoryId, (key, fallback) => GetLocalizedString(key, fallback)),
                _selectedMvpPlacementCategoryId,
                MvpDungeonPlacementPresenter.ResolveOptionName(_selectedMvpPlacementOptionId, (key, fallback) => GetLocalizedString(key, fallback)),
                GetSelectedMvpPlacementPreviewText(),
                BuildSelectedMvpPlacementComparisonText(),
                GetSelectedMvpRunPostureDisplayName(),
                GetSelectedMvpRunPlanPreviewText(),
                _mvpStructurePlacementFeedback,
                _mvpRunResultFeedback,
                _root.BannerMessage,
                _lastRunIntentSummary,
                _lastRunPostureUsedId,
                _lastRunDebugPostureId,
                _lastRunIntentFallbackUsed,
                _smokeViewportStatusMessage,
                _playerFacingSectionIndex,
                PlayerFacingSectionCount);
        }

        public string BuildFullPlayerFacingSmokeText()
        {
            return BootstrapSmokeTextComposer.BuildFullPlayerFacingSmokeText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string BuildCurrentPlayerFacingSmokeText()
        {
            if (_compactSmokeViewEnabled)
            {
                return BuildCompactSmokeText();
            }

            switch (_playerFacingSectionIndex)
            {
                case PlayerFacingSectionLoopSummary:
                    return BuildLoopSummarySectionText();
                case PlayerFacingSectionPlanAndAction:
                    return BuildPlanAndActionSectionText();
                case PlayerFacingSectionLatestRunFeedback:
                    return BuildLatestRunFeedbackSectionText();
                case PlayerFacingSectionFull:
                default:
                    return BuildPlayableMvpScreenText();
            }
        }

        public string BuildPlayableMvpScreenText()
        {
            return BootstrapSmokeTextComposer.BuildPlayableMvpScreenText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        public string BuildCompactSmokeText()
        {
            return BootstrapSmokeTextComposer.BuildCompactSmokeText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildLoopSummarySectionText()
        {
            return BootstrapSmokeTextComposer.BuildLoopSummarySectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildPlanAndActionSectionText()
        {
            return BootstrapSmokeTextComposer.BuildPlanAndActionSectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private string BuildLatestRunFeedbackSectionText()
        {
            return BootstrapSmokeTextComposer.BuildLatestRunFeedbackSectionText(BuildSmokeTextContext(), (key, fallback) => GetLocalizedString(key, fallback));
        }

        private void AppendScrolledPlayerFacingSmokeText(StringBuilder builder, string text)
        {
            string[] bodyLines = (text ?? string.Empty).Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisiblePlayerFacingLineCount);
            _playerFacingScrollOffset = Mathf.Clamp(_playerFacingScrollOffset, 0, maxOffset);
            int end = Mathf.Min(bodyLines.Length, _playerFacingScrollOffset + VisiblePlayerFacingLineCount);
            for (int i = _playerFacingScrollOffset; i < end; i++)
            {
                AppendLine(builder, bodyLines[i]);
            }
        }

        private void ClampPlayerFacingScrollOffset(int requestedOffset)
        {
            string[] bodyLines = BuildCurrentPlayerFacingSmokeText().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisiblePlayerFacingLineCount);
            _playerFacingScrollOffset = Mathf.Clamp(requestedOffset, 0, maxOffset);
        }

        private void AppendHeader(StringBuilder builder)
        {
            if (_runDiagnosticsOnlyVisible)
            {
                AppendLine(builder, GetLocalizedString("ui.dev.diagnostics.focus.run_diagnostics"));
            }
            else
            {
                string pageName = GetLocalizedString(GetPageNameKey(_fullDiagnosticsPage));
                AppendLine(builder, string.Format(
                    GetLocalizedString("ui.dev.diagnostics.header_format"),
                    pageName,
                    _fullDiagnosticsPage + 1,
                    DiagnosticsPageCount));
            }
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_panel"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.toggle_run_diagnostics"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.cycle_diagnostics_page"));
            AppendLine(builder, GetLocalizedString("ui.dev.hint.scroll_diagnostics"));
        }

        private StringBuilder BuildCurrentFullDiagnosticsBody()
        {
            var builder = new StringBuilder();
            switch (_fullDiagnosticsPage)
            {
                case RuntimeSummaryPage:
                    AppendRuntimeSummary(builder);
                    break;
                case RunDiagnosticsPage:
                    AppendRunDiagnostics(builder, includeBreakdownAndFeedback: true, includeHeatDiagnostics: false);
                    break;
                case HeatDiagnosticsPage:
                    AppendHeatDiagnostics(builder);
                    break;
                case SystemsDiagnosticsPage:
                    AppendSystemsDiagnostics(builder);
                    break;
                case ResearchDiagnosticsPage:
                    AppendResearchDiagnostics(builder);
                    break;
                case ResearchStatusPresentationDiagnosticsPage:
                    AppendResearchStatusPresentationDiagnostics(builder);
                    break;
                case ResearchStatusSafetyDiagnosticsPage:
                    AppendResearchStatusSafetyDiagnostics(builder);
                    break;
                case ResearchVerificationBoundaryDiagnosticsPage:
                    AppendResearchVerificationBoundaryDiagnostics(builder);
                    break;
                case ResearchVerificationSafetyDiagnosticsPage:
                    AppendResearchVerificationSafetyDiagnostics(builder);
                    break;
            }
            return builder;
        }

        private void AppendScrolledFullDiagnosticsBody(StringBuilder builder, StringBuilder bodyBuilder)
        {
            string[] bodyLines = bodyBuilder.ToString().Split('\n');
            int maxOffset = Mathf.Max(0, bodyLines.Length - VisibleDiagnosticsBodyLineCount);
            int offset = Mathf.Clamp(_fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage], 0, maxOffset);
            _fullDiagnosticsPageScrollOffsets[_fullDiagnosticsPage] = offset;
            int end = Mathf.Min(bodyLines.Length, offset + VisibleDiagnosticsBodyLineCount);
            for (int i = offset; i < end; i++)
            {
                AppendLine(builder, bodyLines[i]);
            }
        }

        private void AppendRuntimeSummary(StringBuilder builder)
        {
            AppendLine(builder, _root.BuildLine);
            AppendLine(builder, _root.StateLine);
            AppendLine(builder, _root.PendingStateLine);
            AppendLine(builder, _root.GateStatusLine);
            AppendLine(builder, _root.KpiLine);
            AppendLine(builder, _root.TickLine);
            AppendLine(builder, _root.ManaLine);
            AppendLine(builder, _root.SaveLine);
            AppendLine(builder, _root.PauseLine);

            if (!string.IsNullOrEmpty(_root.BannerMessage))
            {
                AppendLine(builder, GetLocalizedString("ui.dev.banner.heading") + ":");
                AppendLine(builder, _root.BannerMessage);
            }
        }

        private void AppendRunDiagnostics(StringBuilder builder, bool includeBreakdownAndFeedback, bool includeHeatDiagnostics)
        {
            AppendLine(builder, _root.RunLine);
            AppendLine(builder, _root.RunHistoryLine);
            if (includeBreakdownAndFeedback)
            {
                AppendLine(builder, _root.RunBreakdownLine);
                AppendLine(builder, _root.RunFeedbackLine);
            }
            AppendLine(builder, _root.RunLootLine);
            AppendLine(builder, _root.RunSurvivalLine);
            AppendLine(builder, _root.RunExtractionLine);
            if (includeHeatDiagnostics)
            {
                AppendLine(builder, _root.RunHeatCoolingLine);
                AppendLine(builder, _root.RunHeatDeltaLine);
                AppendLine(builder, _root.RunHeatApplicationLine);
            }
            AppendLine(builder, _root.RunAdventurerAttractionLine);
            AppendLine(builder, _root.RunAdventurerInterestForecastLine);
            AppendLine(builder, _root.RunAdventurerDemandBudgetLine);
        }

        private void AppendHeatDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.HeatLine);
            AppendLine(builder, _root.CurrentHeatTierLine);
            AppendLine(builder, _root.RunHeatCoolingLine);
            AppendLine(builder, _root.RunHeatDeltaLine);
            AppendLine(builder, _root.RunHeatApplicationLine);
        }

        private void AppendSystemsDiagnostics(StringBuilder builder)
        {
            if (_root.Content == null)
            {
                return;
            }

            AppendLine(builder, string.Format(
                GetLocalizedString("ui.dev.structure_status"),
                _root.SelectedFloorIndex,
                _root.SelectedSlotIndex,
                _root.GetSelectedSlotStructureId(),
                _root.Save != null && _root.Save.structureRuntime != null && _root.Save.structureRuntime.IsHeatCrisisActive));
            AppendLine(builder, _root.OfflineSummaryLine);
        }

        private void AppendResearchDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchPendingLine);
            AppendLine(builder, _root.ResearchPendingValidationLine);
            AppendLine(builder, _root.ResearchProgressLine);
            AppendLine(builder, _root.ResearchProgressStateLine);
            AppendLine(builder, _root.ResearchCompletionEligibilityLine);
            AppendLine(builder, _root.ResearchCompletionPendingApplyLine);
            AppendLine(builder, _root.ResearchCompletionClaimReadinessLine);
            AppendLine(builder, _root.CompletedResearchStateLine);
            AppendLine(builder, _root.ResearchCompletionClaimApplyLine);
        }

        private void AppendResearchStatusPresentationDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusPresentationLine);
        }

        private void AppendResearchStatusSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchStatusSafetyLine);
        }

        private void AppendResearchVerificationBoundaryDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationBoundaryLine);
        }

        private void AppendResearchVerificationSafetyDiagnostics(StringBuilder builder)
        {
            AppendLine(builder, _root.ResearchVerificationSafetyLine);
        }

        private string GetLocalizedString(string key)
        {
            return GetLocalizedString(key, key);
        }

        private string GetLocalizedString(string key, string fallback)
        {
            return _root.Content != null ? _root.Content.GetString(key, fallback) : fallback;
        }

        private static string GetPageNameKey(int page)
        {
            switch (page)
            {
                case RuntimeSummaryPage:
                    return "ui.dev.diagnostics.page.runtime_summary";
                case RunDiagnosticsPage:
                    return "ui.dev.diagnostics.page.run_diagnostics";
                case HeatDiagnosticsPage:
                    return "ui.dev.diagnostics.page.heat_diagnostics";
                case SystemsDiagnosticsPage:
                    return "ui.dev.diagnostics.page.systems_diagnostics";
                case ResearchDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_diagnostics";
                case ResearchStatusPresentationDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_presentation_diagnostics";
                case ResearchStatusSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_status_safety_diagnostics";
                case ResearchVerificationBoundaryDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_boundary_diagnostics";
                case ResearchVerificationSafetyDiagnosticsPage:
                    return "ui.dev.diagnostics.page.research_verification_safety_diagnostics";
                default:
                    return "ui.dev.diagnostics.page.runtime_summary";
            }
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(line ?? string.Empty);
        }

        private void OnGUI()
        {
            DrawMinimalMvpActionPanel();

            if (_root == null || !_root.DevPanelEnabled || !_devPanelVisible)
            {
                return;
            }

            float panelHeight = Mathf.Max(240f, Screen.height - 140f);
            GUILayout.BeginArea(new Rect(10, 120, 360, panelHeight), GUI.skin.box);
            GUILayout.Label(_root.Content.GetString("ui.dev.panel.title", "ui.dev.panel.title"));
            _devPanelScrollPosition = GUILayout.BeginScrollView(_devPanelScrollPosition);

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.save_now", "ui.dev.button.save_now")))
            {
                _root.SaveService.Save(_root.Save, SaveReason.ManualDev);
                _root.SetBanner(_root.Content.GetString("ui.banner.saved_dev", "ui.banner.saved_dev"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.delete_save", "ui.dev.button.delete_save")))
            {
                _root.SaveService.DeleteSave(out string banner);
                _root.SetBanner(banner);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_banner", "ui.dev.button.clear_banner")))
            {
                _root.SetBanner(string.Empty);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clean_mvp_validation_reset", "ui.dev.button.clean_mvp_validation_reset")))
            {
                ResetCleanMvpValidationSessionFromDevPanel();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_online", "ui.dev.button.toggle_online")))
            {
                _root.SetOnline(!_root.IsOnline);
                if (!_root.IsOnline)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("ui.banner.offline", "Offline mode.")
                        : "Offline mode.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.online_restored", "ui.banner.online_restored"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_verification", "ui.dev.button.toggle_verification")))
            {
                _root.SetVerificationPending(!_root.VerificationPending);
                if (_root.VerificationPending)
                {
                    string msg = _root.Content != null
                        ? _root.Content.GetString("gate.error.verification_pending", "Verification pending.")
                        : "Verification pending.";
                    _root.SetBanner(msg);
                }
                else
                {
                    _root.SetBanner(_root.Content.GetString("ui.banner.verification_cleared", "ui.banner.verification_cleared"));
                }
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.toggle_pause", "ui.dev.button.toggle_pause")))
            {
                bool pause = _root.PauseLine != "Pause: Paused";
                _root.ApplyPauseState(pause);
                _root.SetBanner(pause
                    ? _root.Content.GetString("ui.banner.paused_dev_panel", "ui.banner.paused_dev_panel")
                    : _root.Content.GetString("ui.banner.resumed_dev_panel", "ui.banner.resumed_dev_panel"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.set_research_pending", "ui.dev.button.set_research_pending")))
            {
                bool didSet = _root.SetResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed",
                    didSet ? "ui.banner.research_pending_set" : "ui.banner.research_pending_set_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.clear_research_pending", "ui.dev.button.clear_research_pending")))
            {
                _root.ClearResearchPendingScaffold();
                _root.SetBanner(_root.Content.GetString("ui.banner.research_pending_cleared", "ui.banner.research_pending_cleared"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.claim_research_completion", "ui.dev.button.claim_research_completion")))
            {
                bool didClaim = _root.ClaimResearchCompletionScaffold();
                _root.SetBanner(_root.Content.GetString(
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed",
                    didClaim ? "ui.banner.research_completion_claimed" : "ui.banner.research_completion_claim_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_mana", "ui.dev.button.sim_mana")))
            {
                _root.TrackManaGenerated(10);
                _root.SetBanner(_root.Content.GetString("ui.banner.simulated_mana_kpi", "ui.banner.simulated_mana_kpi"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_heat", "ui.dev.button.sim_heat")))
            {
                _root.ApplyHeatDelta(5d);
                _root.SetBanner(_root.Content.GetString("ui.banner.applied_heat_event", "ui.banner.applied_heat_event"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.select_slot", "ui.dev.button.select_slot")))
            {
                _root.SelectNextSlot();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_mana_generator", "ui.dev.button.place_mana_generator")))
            {
                ShowPlacementBanner(StructureSimulationPass.ManaGeneratorBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_heat_scrubber", "ui.dev.button.place_heat_scrubber")))
            {
                ShowPlacementBanner(StructureSimulationPass.HeatScrubberBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.place_risk_lab", "ui.dev.button.place_risk_lab")))
            {
                ShowPlacementBanner(StructureSimulationPass.RiskLabBasicId);
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_structure_tick", "ui.dev.button.sim_structure_tick")))
            {
                bool didRun = _root.SimulateStructureTick();
                _root.SetBanner(didRun
                    ? _root.Content.GetString("ui.banner.simulated_tick", "ui.banner.simulated_tick")
                    : _root.Content.GetString("ui.banner.structure_tick_failed", "ui.banner.structure_tick_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.sim_run_once", "ui.dev.button.sim_run_once")))
            {
                bool didRun = _root.SimulateRunOnce();
                _root.SetBanner(didRun
                    ? _root.Content.GetString("ui.banner.run_simulated", "ui.banner.run_simulated")
                    : _root.Content.GetString("ui.banner.run_sim_failed", "ui.banner.run_sim_failed"));
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_previous", "ui.dev.button.run_previous")))
            {
                _root.SelectPreviousRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_next", "ui.dev.button.run_next")))
            {
                _root.SelectNextRunOutcome();
                _root.RefreshRunLine();
            }

            if (GUILayout.Button(_root.Content.GetString("ui.dev.button.run_latest", "ui.dev.button.run_latest")))
            {
                _root.SelectLatestRunOutcome();
                _root.RefreshRunLine();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }


        private void DrawCollapsedMinimalMvpActionPanel()
        {
            GUIStyle compactBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUIStyle compactButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 1, 1)
            };
            GUILayout.BeginArea(GetCollapsedMinimalMvpActionPanelRect(), compactBox);
            if (GUILayout.Button(GetLocalizedString("ui.mvp_action.button.expand_panel"), compactButton, GUILayout.Height(MinimalMvpActionPanelButtonHeight)))
            {
                ToggleMinimalMvpActionPanelCollapsed();
                RefreshOverlayText();
            }
            GUILayout.EndArea();
        }

        public Rect GetCollapsedMinimalMvpActionPanelRect()
        {
            float width = OverlayTextRightCollapsedActionPanelReserve - (MinimalMvpActionPanelMargin * 2f);
            float x = Mathf.Max(MinimalMvpActionPanelMargin, Screen.width - width - MinimalMvpActionPanelMargin);
            return new Rect(x, MinimalMvpActionPanelMargin, width, MinimalMvpActionPanelButtonHeight + (MinimalMvpActionPanelMargin * 2f));
        }

        public Rect GetMinimalMvpActionPanelRect()
        {
            float x = Mathf.Max(MinimalMvpActionPanelMargin, Screen.width - MinimalMvpActionPanelWidth - MinimalMvpActionPanelMargin);
            return new Rect(x, MinimalMvpActionPanelMargin, MinimalMvpActionPanelWidth, MinimalMvpActionPanelHeight);
        }

        private void DrawMinimalMvpActionPanel()
        {
            if (_root == null || !PlayerFacingPanelsVisible)
            {
                return;
            }

            if (_minimalMvpActionPanelCollapsed)
            {
                DrawCollapsedMinimalMvpActionPanel();
                return;
            }

            string placementComparisonText = BuildSelectedMvpPlacementComparisonText();
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildPlacementLabels(
                (key, fallback) => GetLocalizedString(key, fallback),
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId,
                _selectedMvpStructureId,
                GetSelectedMvpRunPostureNameKey(),
                placementComparisonText);
            GUIStyle compactBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUIStyle compactLabel = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 0, 0)
            };
            GUIStyle compactButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 1, 1)
            };
            GUIStyle groupHeaderLabel = new GUIStyle(compactLabel)
            {
                fontStyle = FontStyle.Bold
            };
            GUILayoutOption labelHeight = GUILayout.Height(MinimalMvpActionPanelLabelHeight);
            GUILayoutOption previewHeight = GUILayout.Height(MinimalMvpActionPanelLabelHeight * 2f);
            GUILayoutOption buttonHeight = GUILayout.Height(MinimalMvpActionPanelButtonHeight);

            GUILayout.BeginArea(GetMinimalMvpActionPanelRect(), compactBox);
            _minimalMvpActionPanelScrollPosition = GUILayout.BeginScrollView(
                _minimalMvpActionPanelScrollPosition,
                false,
                true,
                GUILayout.Width(MinimalMvpActionPanelWidth - MinimalMvpActionPanelScrollBarWidth));
            GUILayout.Label(labels.Title, compactLabel, labelHeight);
            GUILayout.Label(labels.CategoryLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.SelectedStructureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PostureLabel, compactLabel, labelHeight);
            GUILayout.Label(labels.PreviewText, compactLabel, labelHeight);
            if (!string.IsNullOrWhiteSpace(labels.ComparisonText))
            {
                GUILayout.Label(labels.ComparisonText, compactLabel, labelHeight);
            }
            GUILayout.Label(labels.RunPlanPreviewText, compactLabel, previewHeight);
            GUILayout.Label(MvpRoomSlotTargetPresenter.BuildSelectedTargetText(_root.Save, _root.RunSimulationConfig, (key, fallback) => GetLocalizedString(key, fallback)), compactLabel, labelHeight);
            GUILayout.Label(GetSelectedMvpRoomCapacityText(), compactLabel, labelHeight);
            string selectedPlacementFitText = GetSelectedMvpPlacementFitText();
            if (!string.IsNullOrWhiteSpace(selectedPlacementFitText))
            {
                GUILayout.Label(selectedPlacementFitText, compactLabel, labelHeight);
            }
            if (GUILayout.Button(GetLocalizedString("ui.mvp_room_slots.cycle_target_button"), compactButton, buttonHeight))
            {
                CycleSelectedMvpRoomSlotTarget();
            }
            if (GUILayout.Button(GetLocalizedString(AddBasicRoomSlotButtonKey), compactButton, buttonHeight))
            {
                AddMvpBasicRoomSlot();
            }
            GUILayout.Label(labels.RoomsGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.BasicRoomSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.RoomCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.BasicRoomOptionId);
            }
            if (GUILayout.Button(labels.NarrowHallSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.RoomCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.NarrowHallOptionId);
            }
            GUILayout.Label(labels.MonstersGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.SkeletonSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SkeletonOptionId);
            }
            if (GUILayout.Button(labels.GoblinSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.GoblinOptionId);
            }
            GUILayout.Label(labels.TrapsGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.SpikeTrapSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SpikeTrapOptionId);
            }
            if (GUILayout.Button(labels.SnareTrapSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.SnareTrapOptionId);
            }
            GUILayout.Label(labels.LootGroupHeader, groupHeaderLabel, labelHeight);
            if (GUILayout.Button(labels.BasicLootNodeSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.BasicLootNodeOptionId);
            }
            if (GUILayout.Button(labels.HiddenCacheSelection, compactButton, buttonHeight))
            {
                SelectMvpPlacementCategory(MvpDungeonPlacementIds.LootNodeCategoryId);
                SelectMvpPlacementOption(MvpDungeonPlacementIds.HiddenCacheOptionId);
            }
            if (GUILayout.Button(labels.CautiousPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.CautiousId);
            }
            if (GUILayout.Button(labels.BalancedPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.BalancedId);
            }
            if (GUILayout.Button(labels.GreedyPosture, compactButton, buttonHeight))
            {
                SelectMvpRunPosture(RunPostureResolver.GreedyId);
            }
            GUILayout.Label(GetLocalizedString(_diagnosticsVisible
                ? "ui.mvp_view.diagnostics_mode.status"
                : "ui.mvp_view.player_mode.status"), compactLabel, labelHeight);
            if (GUILayout.Button(labels.PlacementButton, compactButton, buttonHeight))
            {
                PlaceSelectedMvpStructure();
            }

            if (GUILayout.Button(labels.RunButton, compactButton, buttonHeight))
            {
                RunOrObserveDungeon();
            }

            string diagnosticsButton = _diagnosticsVisible ? labels.HideDiagnosticsButton : labels.ShowDiagnosticsButton;
            if (GUILayout.Button(diagnosticsButton, compactButton, buttonHeight))
            {
                ToggleDiagnosticsVisibility();
                RefreshOverlayText();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string BuildSelectedMvpPlacementComparisonText()
        {
            if (_root == null)
            {
                return string.Empty;
            }

            MvpPlacementComparisonPreview preview = MvpPlacementComparisonPresenter.Resolve(
                _root.Save != null ? _root.Save.mvpDungeonFloorLayout : null,
                _root.Save != null ? _root.Save.mvpDungeonPlacements : null,
                _root.RunSimulationConfig,
                _selectedMvpPlacementCategoryId,
                _selectedMvpPlacementOptionId);
            return MvpPlacementComparisonPresenter.BuildComparisonText(preview, (key, fallback) => GetLocalizedString(key, fallback));
        }

        private void ClearStaleRoomSlotPlacementFailureFeedback()
        {
            if (!_roomSlotPlacementFailureIsLatestAction && IsRoomSlotPlacementFailureFeedback() && IsSelectedPlacementAssignedInValidRoomSlot())
            {
                _mvpStructurePlacementFeedback = string.Empty;
                _roomSlotPlacementFailureIsLatestAction = false;
            }
        }

        private void ClearRoomSlotPlacementFailureFeedback()
        {
            if (IsRoomSlotPlacementFailureFeedback())
            {
                _mvpStructurePlacementFeedback = string.Empty;
            }

            _roomSlotPlacementFailureIsLatestAction = false;
        }

        private bool IsRoomSlotPlacementFailureFeedback()
        {
            if (string.IsNullOrWhiteSpace(_mvpStructurePlacementFeedback))
            {
                return false;
            }

            string format = GetLocalizedString(MvpRoomSlotTargetPresenter.NoValidSlotFormatKey, MvpRoomSlotTargetPresenter.NoValidSlotFormatKey);
            string prefix = format.Split('{')[0];
            return !string.IsNullOrWhiteSpace(prefix) && _mvpStructurePlacementFeedback.StartsWith(prefix, System.StringComparison.Ordinal);
        }

        private bool IsSelectedPlacementAssignedInValidRoomSlot()
        {
            if (_root?.Save == null ||
                string.IsNullOrWhiteSpace(_selectedMvpPlacementCategoryId) ||
                string.IsNullOrWhiteSpace(_selectedMvpPlacementOptionId) ||
                string.Equals(_selectedMvpPlacementCategoryId, MvpDungeonPlacementIds.RoomCategoryId, System.StringComparison.Ordinal))
            {
                return false;
            }

            MvpDungeonFloorSlotLayout layout = MvpRoomSlotLayoutResolver.ResolveDefaultFloor(_root.Save, _root.RunSimulationConfig);
            if (layout?.Rooms == null)
            {
                return false;
            }

            for (int i = 0; i < layout.Rooms.Length; i++)
            {
                MvpDungeonRoomInstance room = layout.Rooms[i];
                if (room == null || !MvpRoomSlotTargetResolver.CanAccept(room, _selectedMvpPlacementCategoryId))
                {
                    continue;
                }

                if (ContainsAssignedOption(room, _selectedMvpPlacementCategoryId, _selectedMvpPlacementOptionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAssignedOption(MvpDungeonRoomInstance room, string categoryId, string optionId)
        {
            string[] assigned = null;
            switch (categoryId)
            {
                case MvpDungeonPlacementIds.MonsterCategoryId:
                    assigned = room.AssignedMonsterOptionIds;
                    break;
                case MvpDungeonPlacementIds.TrapCategoryId:
                    assigned = room.AssignedTrapOptionIds;
                    break;
                case MvpDungeonPlacementIds.LootNodeCategoryId:
                    assigned = room.AssignedLootNodeOptionIds;
                    break;
            }

            if (assigned == null)
            {
                return false;
            }

            for (int i = 0; i < assigned.Length; i++)
            {
                if (string.Equals(assigned[i], optionId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private BootstrapMvpActionHandler CreateMvpActionHandler()
        {
            return new BootstrapMvpActionHandler(new BootstrapMvpActionHandler.Context(
                (key, fallback) => GetLocalizedString(key, fallback),
                (categoryId, optionId) =>
                {
                    bool ok = _root.TryMvpPlaceOrModifySelectedPlacementEnforcingRoomTarget(
                        categoryId,
                        optionId,
                        out MvpDungeonPlacementEntry priorEntry,
                        out MvpDungeonPlacementEntry newEntry,
                        out string bannerKey,
                        out string failureFeedback,
                        out string targetFeedback);
                    return new BootstrapMvpActionHandler.PlacementAttempt(ok, priorEntry, newEntry, bannerKey, targetFeedback, failureFeedback);
                },
                () => _root.ResolveMvpPlayerLoopSummary(),
                postureId => _root.SimulateMvpActiveLoopOnce(out _, postureId),
                message => _root.SetBanner(message)));
        }

        private void ShowPlacementBanner(string structureId)
        {
            bool ok = _root.TryPlaceSelectedStructure(structureId, out string bannerKey);
            string message = _root.Content.GetString(bannerKey, bannerKey);
            _root.SetBanner(ok ? string.Format(message, structureId) : message);
        }

        private static bool IsAllowedMvpStructure(string structureId)
        {
            return structureId == StructureSimulationPass.ManaGeneratorBasicId ||
                   structureId == StructureSimulationPass.HeatScrubberBasicId ||
                   structureId == StructureSimulationPass.RiskLabBasicId;
        }

        private static string GetMvpRunPostureNameKey(string postureId)
        {
            switch (postureId)
            {
                case RunPostureResolver.CautiousId:
                    return MinimalMvpActionPanelPresenter.CautiousPostureKey;
                case RunPostureResolver.GreedyId:
                    return MinimalMvpActionPanelPresenter.GreedyPostureKey;
                case RunPostureResolver.BalancedId:
                default:
                    return MinimalMvpActionPanelPresenter.BalancedPostureKey;
            }
        }

        private static string GetMvpSelectionNameKey(string structureId)
        {
            switch (structureId)
            {
                case StructureSimulationPass.HeatScrubberBasicId:
                    return MinimalMvpActionPanelPresenter.HeatScrubberSelectionKey;
                case StructureSimulationPass.RiskLabBasicId:
                    return MinimalMvpActionPanelPresenter.RiskLabSelectionKey;
                case StructureSimulationPass.ManaGeneratorBasicId:
                default:
                    return MinimalMvpActionPanelPresenter.ManaGeneratorSelectionKey;
            }
        }
    }
}
