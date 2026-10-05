using System;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

namespace DungeonBuilder.M0
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProductionDungeonController : MonoBehaviour
    {
        public DungeonPresentationPolicy presentationPolicy;
        private GameRoot root;
        private UIDocument document;
        private VisualElement safe, top, bottom, viewportElement, sheet, modal;
        private Label status;
        private DungeonFloorWorldView world;
        private Camera worldCamera;
        private Camera bootstrapCamera;
        private int bootstrapCameraMask;
        private DungeonViewport viewport;
        private readonly DungeonViewportGesture gesture = new DungeonViewportGesture();
        private IDungeonDraftStore store;
        private DungeonDraftContext draftContext;
        private TransactionalDungeonDraft draft, recovery;
        private byte[] recoveryBytes;
        private string recoveryReason, feedback;
        private RoomContentAssignment selected;
        private string selectedFloor;
        private DetachedCanonicalSpatialSaveState renderState;
        private DungeonTextSize textSize = DungeonTextSize.Default;
        private bool initialized, moveMode, pointerDown, legacy, autoFit = true;
        private int primaryTouchId;
        private bool suppressTouch;
        private double lastWallet = double.NaN, lastHeat = double.NaN;
        private long lastTick = -1;
        private float pixelsPerPhysicalUnit;
        private Rect lastSafe;
        private Vector2 lastResolution;
        private Action confirm;
        public bool IsEditing => draft != null && !draft.IsClosed;
        public bool IsMoving => moveMode;
        public TransactionalDungeonDraft Draft => draft;
        public DungeonFloorWorldView World => world;
        public VisualElement SafeRoot => safe;
        public DungeonTextSize TextSize => textSize;

        private void Start() { root = GameRoot.Instance; }
        private void Update()
        {
            if (root == null) root = GameRoot.Instance;
            if (!initialized)
            {
                if (root?.Save?.validatedCanonicalSpatialState == null || root.SaveService.DungeonDraftContext == null) return;
                Initialize();
            }
            if (draft?.Durability == DraftDurability.Pending) { draft.FlushNext(); Present(); }
            if (lastWallet != root.Save.structureRuntime.ManaReserve || lastHeat != root.Save.structureRuntime.Heat || lastTick != root.Save.totalTicks)
                PresentHud();
            if (lastResolution != new Vector2(Screen.width, Screen.height) || lastSafe != Screen.safeArea) Layout();
            if (!legacy) ReadInput();
        }

        private void Initialize()
        {
            document = GetComponent<UIDocument>();
            if (presentationPolicy == null || document.visualTreeAsset == null || document.panelSettings == null)
                throw new InvalidOperationException("production.dungeon.assets_missing");
            var ui = document.rootVisualElement;
            ui.pickingMode = PickingMode.Ignore;
            safe = ui.Q("safeRoot"); top = ui.Q("topChrome"); bottom = ui.Q("bottomChrome");
            viewportElement = ui.Q("viewport"); sheet = ui.Q("contextSheet"); modal = ui.Q("modal"); status = ui.Q<Label>("status");
            draftContext = root.SaveService.DungeonDraftContext; store = root.SaveService.CreateDungeonDraftStore();
            viewport = new DungeonViewport(presentationPolicy);
            var worldObject = new GameObject("ProductionDungeonWorld"); worldObject.transform.SetParent(transform, false);
            worldObject.layer = presentationPolicy.WorldLayer;
            world = worldObject.AddComponent<DungeonFloorWorldView>(); world.Initialize(presentationPolicy);
            var cameraObject = new GameObject("ProductionDungeonCamera", typeof(Camera)); cameraObject.transform.SetParent(transform, false);
            worldCamera = cameraObject.GetComponent<Camera>(); worldCamera.orthographic = true;
            worldCamera.cullingMask = 1 << presentationPolicy.WorldLayer;
            bootstrapCamera = Camera.main;
            if (bootstrapCamera != null)
            { bootstrapCameraMask = bootstrapCamera.cullingMask; bootstrapCamera.cullingMask &= ~worldCamera.cullingMask; }
            worldCamera.clearFlags = CameraClearFlags.SolidColor; worldCamera.backgroundColor = presentationPolicy.GridColor;
            worldCamera.depth = 1;
            root.SaveService.CanonicalRuntimePublished += CanonicalPublished;
            initialized = true;
            pixelsPerPhysicalUnit = DungeonPhysicalUnits.PixelsPerUnit();
            Button("edit", EnterEdit); Button("save", ReviewSave); Button("discard", ReviewDiscard);
            Button("move", BeginMove); Button("closeSheet", CloseSheet); Button("reset", () => { autoFit = true; viewport.Reset(); ApplyCamera(); });
            Button("retry", () => { draft?.FlushNext(); Present(); });
            Button("small", () => SetTextSize(DungeonTextSize.Small));
            Button("default", () => SetTextSize(DungeonTextSize.Default));
            Button("large", () => SetTextSize(DungeonTextSize.Large));
            Button("confirm", () => { var action = confirm; CloseModal(); action?.Invoke(); });
            Button("cancel", CancelModal);
            Button("legacy", ToggleLegacy);
            foreach (var label in ui.Query<Label>().ToList())
                if (!string.IsNullOrEmpty(label.tooltip)) { label.text = Text(label.tooltip); label.tooltip = label.text; }
            foreach (var button in ui.Query<Button>().ToList()) { button.text = Text(button.tooltip); button.tooltip = button.text; }
            ui.Q<Button>("legacy").style.display = DevelopmentDiagnosticsPolicy.AreDiagnosticsEnabled(
                DevelopmentDiagnosticsPolicy.IsCurrentBuildDevelopment(), root.DevPanelEnabled) ? DisplayStyle.Flex : DisplayStyle.None;
            SetLegacy(false);
            renderState = draftContext.Copy(root.Save.validatedCanonicalSpatialState);
            selectedFloor = renderState.Floors.FirstOrDefault()?.FloorInstanceId;
            try
            {
                recoveryBytes = store.Read();
                if (recoveryBytes != null) recovery = TransactionalDungeonDraft.Recover(recoveryBytes,
                    renderState, draftContext, store, out recoveryReason);
            }
            catch { recoveryReason = TransactionalDungeonDraft.RecoveryFailedReason; }
            RebuildFloor(true); Present(); Layout();
            if (recoveryBytes != null || recoveryReason != null) OfferRecovery();
        }

        private string Text(string key) => root.Content.GetString(key, key);
        private IFormatProvider Culture => PassiveManaPresenter.ResolveFormatProvider(root.Content.Strings?.language);
        private string Format(string key, params object[] args) => ProductionDungeonPresenter.Format(Text, Culture, key, args);
        private void Button(string name, Action action) => document.rootVisualElement.Q<Button>(name).clicked += action;
        public void EnterEdit()
        {
            if (IsEditing || recoveryBytes != null || recoveryReason != null) return;
            draft = TransactionalDungeonDraft.Create(root.Save.validatedCanonicalSpatialState, draftContext, store);
            feedback = null; selected = null; moveMode = false; CloseSheet();
            RebuildFloor(false); Present();
        }
        public void BeginMove()
        {
            if (!IsEditing || selected == null) return;
            moveMode = true; sheet.style.display = DisplayStyle.None;
            feedback = "ui.dungeon.move_hint"; Present();
        }
        private void CloseSheet() { selected = null; moveMode = false; sheet.style.display = DisplayStyle.None; world?.ClearPreview(); }
        public void TapWorld(TileCoordinate cell)
        {
            if (!initialized || legacy || modal.style.display == DisplayStyle.Flex) return;
            if (moveMode && selected != null && IsEditing)
            {
                bool valid = world.TryRoomLocal(selected, cell, out var local) &&
                    draft.Move(selectedFloor, selected.RoomInstanceId, selected.AssignmentId, local);
                if (valid)
                {
                    renderState = draft.ReadModel; RebuildFloor(false);
                    selected = renderState.Floors.Single(f => f.FloorInstanceId == selectedFloor).RoomContents.Assignments
                        .Single(a => a.AssignmentId == selected.AssignmentId);
                    feedback = null; moveMode = false; OpenSheet();
                }
                else feedback = draft.Reason ?? TransactionalDungeonDraft.InvalidReason;
                world.Preview(cell, valid); Present();
                return;
            }
            selected = world.Select(cell);
            if (selected == null) CloseSheet(); else { OpenSheet(); world.Preview(cell, true); }
            Present();
        }
        private void OpenSheet()
        {
            sheet.style.display = DisplayStyle.Flex;
            document.rootVisualElement.Q<Label>("selectedName").text = MvpDungeonPlacementPresenter.ResolveOptionName(
                selected.OptionId, (key, fallback) => root.Content.GetString(key, fallback));
        }
        private void ReviewSave()
        {
            if (draft?.CanSave != true) return;
            ShowModal("ui.dungeon.commit_review", () => Commit(), "ui.dungeon.save");
        }
        public bool Commit()
        {
            var result = root.SaveService.CommitDungeonDraft(root.Save, draft);
            if (!result.IsSuccess) { feedback = result.Reason; Present(); return false; }
            feedback = draft.Reason ?? "ui.dungeon.committed"; draft = null; CloseSheet();
            renderState = draftContext.Copy(root.Save.validatedCanonicalSpatialState); RebuildFloor(false); Present(); return true;
        }
        private void ReviewDiscard()
        { if (draft == null) return; if (!draft.HasChanges) Discard(); else ShowModal("ui.dungeon.discard_review", () => Discard(), "ui.dungeon.discard"); }
        public bool Discard()
        {
            if (draft == null || !draft.Discard()) { feedback = draft?.Reason; Present(); return false; }
            draft = null; feedback = "ui.dungeon.discarded"; CloseSheet();
            renderState = draftContext.Copy(root.Save.validatedCanonicalSpatialState); RebuildFloor(false); Present(); return true;
        }
        private void OfferRecovery()
        {
            ShowModal(feedback == TransactionalDungeonDraft.DeleteFailedReason ? feedback :
                recovery != null ? "ui.dungeon.recovery" : recoveryReason,
                recovery != null ? (Action)Resume : DiscardRecovery,
                recovery != null ? "ui.dungeon.resume" : "ui.dungeon.discard");
            var cancel = document.rootVisualElement.Q<Button>("cancel");
            cancel.text = Text(recovery != null ? "ui.dungeon.discard" : "ui.dungeon.continue");
            cancel.style.display = recovery != null ? DisplayStyle.Flex : DisplayStyle.None;
            confirm = recovery != null ? (Action)Resume : DiscardRecovery;
        }
        private void Resume()
        { draft = recovery; recovery = null; recoveryBytes = null; recoveryReason = null;
          renderState = draft.ReadModel; RebuildFloor(false); Present(); }
        private void DiscardRecovery()
        {
            if (!store.Delete(recoveryBytes))
            {
                // A failed deletion may have removed part of the recovered chain. Keep
                // resolution available, but never resume the pre-deletion read model.
                recovery = null; recoveryBytes = null; recoveryReason = TransactionalDungeonDraft.RecoveryFailedReason;
                feedback = TransactionalDungeonDraft.DeleteFailedReason; Present(); OfferRecovery(); return;
            }
            recovery = null; recoveryBytes = null; recoveryReason = null; feedback = "ui.dungeon.discarded"; CloseModal(); Present();
        }
        private void ShowModal(string key, Action action, string confirmKey)
        {
            gesture.Cancel(); pointerDown = false; confirm = action;
            document.rootVisualElement.Q<Label>("modalText").text = Text(key);
            document.rootVisualElement.Q<Button>("confirm").text = Text(confirmKey);
            document.rootVisualElement.Q<Button>("cancel").text = Text("ui.dungeon.continue");
            document.rootVisualElement.Q<Button>("cancel").style.display = DisplayStyle.Flex;
            modal.style.display = DisplayStyle.Flex;
        }
        private void CloseModal()
        {
            modal.style.display = DisplayStyle.None; confirm = null;
        }
        private void CancelModal()
        {
            if (recovery == null && (recoveryBytes != null || recoveryReason != null)) { OfferRecovery(); return; }
            if (recovery != null) { DiscardRecovery(); return; }
            CloseModal();
        }
        private void CanonicalPublished(SaveData save)
        {
            if (!IsEditing && draftContext.Fingerprint(renderState) != draftContext.Fingerprint(save.validatedCanonicalSpatialState))
            { renderState = draftContext.Copy(save.validatedCanonicalSpatialState); RebuildFloor(false); }
            if (IsEditing && draftContext.Fingerprint(save.validatedCanonicalSpatialState) != draft.BaselineFingerprint)
                feedback = TransactionalDungeonDraft.StaleReason;
            Present();
        }

        private void RebuildFloor(bool reset)
        {
            if (!initialized) return;
            if (reset) autoFit = true;
            if (IsEditing) renderState = draft.ReadModel;
            var selectedValue = renderState.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);
            if (selectedValue == null) { selectedValue = renderState.Floors.FirstOrDefault(); selectedFloor = selectedValue?.FloorInstanceId; }
            world.Render(selectedValue, root.ProductionSpatialContent, draftContext.Occupancy,
                root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles, IsEditing);
            viewport.Configure(world.Bounds, worldCamera.pixelRect.width > 0 ? worldCamera.pixelRect : new Rect(0, 0, Screen.width, Screen.height), reset);
            ApplyCamera();
            var floors = document.rootVisualElement.Q("floors"); floors.Clear();
            foreach (var floor in renderState.Floors.OrderBy(f => f.FloorIndex).ThenBy(f => f.FloorInstanceId, StringComparer.Ordinal))
            {
                string id = floor.FloorInstanceId;
                var button = new Button(() => SelectFloor(id)) { text = Format("ui.dungeon.floor", floor.FloorIndex + 1) +
                    (draft?.FloorChanged(id) == true ? Text("ui.dungeon.floor_changed") : string.Empty) };
                if (id == selectedFloor) button.AddToClassList("selected-floor"); floors.Add(button);
            }
            SetTextSize(textSize);
        }
        public void SelectFloor(string id)
        { if (!renderState.Floors.Any(f => f.FloorInstanceId == id)) return;
          selectedFloor = id; CloseSheet(); RebuildFloor(true); Present(); Layout(); }
        private void Present()
        {
            if (!initialized) return;
            var ui = document.rootVisualElement;
            PresentHud();
            ui.Q<Label>("mode").text = Text(IsEditing ? "ui.dungeon.edit_mode" : "ui.dungeon.normal");
            status.text = Text(feedback ?? ProductionDungeonPresenter.DraftStatus(draft));
            ui.Q<Button>("edit").style.display = IsEditing ? DisplayStyle.None : DisplayStyle.Flex;
            ui.Q("editActions").style.display = IsEditing ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("edit").SetEnabled(recoveryBytes == null && recoveryReason == null);
            ui.Q<Button>("save").SetEnabled(draft?.CanSave == true && draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState) == draft.BaselineFingerprint);
            ui.Q<Button>("discard").SetEnabled(IsEditing);
            ui.Q<Button>("retry").style.display = IsEditing && draft.Durability == DraftDurability.Failed ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q<Button>("move").SetEnabled(IsEditing && selected != null && draft.Durability != DraftDurability.Unknown);
            var capacity = ui.Q<Label>("capacity"); capacity.style.display = IsEditing ? DisplayStyle.Flex : DisplayStyle.None;
            var floor = renderState.Floors.SingleOrDefault(f => f.FloorInstanceId == selectedFloor);
            if (floor != null && IsEditing)
            {
                var catalog = root.ProductionSpatialContent.Catalog;
                var definition = catalog.Floors.Single(f => f.FloorDefinitionId == floor.FloorDefinitionId);
                var validation = FloorLayoutValidator.Validate(floor.Layout, definition, catalog.Rooms, catalog.Corridors,
                    new SpatialValidationWorkloadLimits(root.SaveSpatialMigrationLimits.Canonical.Spatial.MaximumMaterializedTiles),
                    floor.FixedStructures, catalog.FixedStructures, CanonicalEditFloorTarget.Mode(floor));
                capacity.text = Format("ui.dungeon.capacity", validation.Capacity.UsedFloorSpaceCapacity,
                    validation.Capacity.RemainingFloorSpaceCapacity);
            }
            world.SetEdit(IsEditing);
        }
        private void PresentHud()
        {
            lastWallet = root.Save.structureRuntime.ManaReserve; lastHeat = root.Save.structureRuntime.Heat; lastTick = root.Save.totalTicks;
            var ui = document.rootVisualElement;
            var hud = ProductionDungeonPresenter.Hud(root.Save, root.RunSimulationConfig, root.PassiveManaPerHourForPresentation, Text, Culture);
            ui.Q<Label>("totalMana").text = hud.TotalMana; ui.Q<Label>("usableMana").text = hud.UsableMana;
            ui.Q<Label>("manaRate").text = hud.ManaPerHour; ui.Q<Label>("heat").text = hud.Heat;
        }
        public void SetTextSize(DungeonTextSize size)
        {
            textSize = size;
            if (!initialized) return;
            float font = presentationPolicy.Text(size);
            foreach (var text in document.rootVisualElement.Query<TextElement>().ToList()) text.style.fontSize = font;
            var platform = Application.platform == RuntimePlatform.Android ? DungeonTargetPlatform.Android :
                Application.platform == RuntimePlatform.IPhonePlayer ? DungeonTargetPlatform.IOS : DungeonTargetPlatform.Desktop;
            float target = presentationPolicy.MinimumPanelUnitsFromNativeScale(platform, pixelsPerPhysicalUnit, document.panelSettings.scale);
            foreach (var button in document.rootVisualElement.Query<Button>().ToList())
            { button.style.minWidth = target; button.style.minHeight = target; }
        }
        public void ApplySafeArea(Rect area, Vector2 screenSize)
        {
            if (!initialized) return;
            float scale = document.panelSettings.scale;
            Rect rect = DungeonPresentationPolicy.SafePanelRect(area, screenSize.y, scale);
            safe.style.left = rect.x; safe.style.top = rect.y; safe.style.width = rect.width; safe.style.height = rect.height;
        }
        private void Layout()
        {
            lastSafe = Screen.safeArea; lastResolution = new Vector2(Screen.width, Screen.height);
            ApplySafeArea(lastSafe, lastResolution); SetTextSize(textSize);
            safe.EnableInClassList("landscape", Screen.width > Screen.height);
            viewportElement.UnregisterCallback<GeometryChangedEvent>(ViewportGeometry);
            viewportElement.RegisterCallback<GeometryChangedEvent>(ViewportGeometry);
        }
        private void ViewportGeometry(GeometryChangedEvent evt)
        {
            Rect panelRect = viewportElement.worldBound; float scale = document.panelSettings.scale;
            var rect = new Rect(panelRect.x * scale, Screen.height - panelRect.yMax * scale,
                panelRect.width * scale, panelRect.height * scale);
            if (rect.width < 1 || rect.height < 1) return;
            worldCamera.pixelRect = rect; viewport.Configure(world.Bounds, rect, autoFit); ApplyCamera();
        }
        private void ApplyCamera()
        { worldCamera.orthographicSize = viewport.Size; worldCamera.transform.position = new Vector3(viewport.Center.x, viewport.Center.y, presentationPolicy.CameraDepth); }
        public bool IsChrome(Vector2 screenPoint, int pointerId = -1)
        {
            if (legacy || modal.style.display == DisplayStyle.Flex) return true;
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(document.rootVisualElement.panel,
                new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            var picked = document.rootVisualElement.panel.Pick(panelPoint);
            // The retained EventSystem also raycasts the transparent UI Toolkit viewport.
            // Its own panel hit must not block world input; other canvases still do.
            if (picked != null && picked != viewportElement) return true;
            if (EventSystem.current?.currentInputModule is InputSystemUIInputModule module)
            {
                var hit = module.GetLastRaycastResult(pointerId);
                if (hit.gameObject != null && !(hit.module is PanelRaycaster panel && panel.panel == document.rootVisualElement.panel))
                    return true;
            }
            return false;
        }
        private void ReadInput()
        {
            var touchScreen = Touchscreen.current;
            bool pressed = false; Vector2 point = default; Vector2? second = null; int id = 0, secondId = 0;
            if (touchScreen != null)
            {
                foreach (var touch in touchScreen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    int touchId = touch.touchId.ReadValue();
                    if (!pressed && (!pointerDown || primaryTouchId == touchId))
                    { pressed = true; point = touch.position.ReadValue(); id = touchId; }
                    else if (!second.HasValue) { second = touch.position.ReadValue(); secondId = touchId; }
                }
            }
            if (suppressTouch)
            { if (!pressed && !second.HasValue) suppressTouch = false; else return; }
            if (pointerDown && primaryTouchId != 0 && !pressed && second.HasValue)
            { gesture.Cancel(); pointerDown = false; suppressTouch = true; return; }
            if (!pressed && !second.HasValue && Mouse.current != null)
            {
                point = Mouse.current.position.ReadValue(); pressed = Mouse.current.leftButton.isPressed; id = Mouse.current.deviceId;
                float wheel = Mouse.current.scroll.ReadValue().y;
                if (wheel != 0 && !IsChrome(point, id) && viewport.ScreenRect.Contains(point))
                { autoFit = false; viewport.Zoom(Mathf.Exp(wheel / presentationPolicy.AndroidBaselineDpi), point); ApplyCamera(); }
            }
            if (pressed && !pointerDown)
            { primaryTouchId = id; gesture.Begin(point, IsChrome(point, id) || !viewport.ScreenRect.Contains(point)); pointerDown = true; }
            if (pressed && pointerDown)
            {
                var beforeCenter = viewport.Center; float beforeSize = viewport.Size;
                gesture.Update(viewport, point, second, second.HasValue && IsChrome(second.Value, secondId),
                    presentationPolicy.TapSlopPhysicalUnits * pixelsPerPhysicalUnit);
                if (beforeCenter != viewport.Center || beforeSize != viewport.Size) autoFit = false;
                ApplyCamera();
            }
            if (!pressed && pointerDown)
            {
                pointerDown = false;
                if (gesture.End(out var tap))
                { var position = viewport.ScreenToWorld(tap); TapWorld(new TileCoordinate(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y))); }
            }
        }
        private void ToggleLegacy() { SetLegacy(!legacy); }
        private void SetLegacy(bool value)
        {
            legacy = value;
            if (root.overlay != null)
            { root.overlay.enabled = value; if (root.overlay.overlayText != null) root.overlay.overlayText.gameObject.SetActive(value); }
            top.style.display = bottom.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
            document.rootVisualElement.Q<Button>("legacy").text = Text(value ? "ui.dungeon.return" : "ui.dungeon.legacy");
        }
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() { Flush(); }
        private void Flush()
        { while (draft != null && draft.Durability == DraftDurability.Pending) if (!draft.FlushNext()) break; }
        private void OnDestroy()
        {
            if (bootstrapCamera != null) bootstrapCamera.cullingMask = bootstrapCameraMask;
            if (root?.SaveService != null) root.SaveService.CanonicalRuntimePublished -= CanonicalPublished;
        }
    }
}
