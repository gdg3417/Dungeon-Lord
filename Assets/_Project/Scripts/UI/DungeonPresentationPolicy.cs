using UnityEngine;

namespace DungeonBuilder.M0
{
    public enum DungeonTextSize { Small, Default, Large }
    public enum DungeonTargetPlatform { Desktop, Android, IOS }

    /// <summary>Presentation units only. No gameplay, occupancy, or spatial capacity tuning.</summary>
    [CreateAssetMenu(menuName = "Dungeon Lord/Production Dungeon Presentation")]
    public sealed class DungeonPresentationPolicy : ScriptableObject
    {
        public DungeonVisualCatalog Visuals;
        public float ReferenceShortSide = 720;
        public Color SelectionColor = new Color(.96f,.76f,.37f);
        public float SmallText = 16, DefaultText = 20, LargeText = 26;
        public float AndroidMinimumDp = 48, IOSMinimumPoints = 44, DesktopMinimumPixels = 48;
        public float AndroidBaselineDpi = 160, IOSPointsPerInch = 163;
        public float FallbackDpi = 160;
        public float Padding = 12, TapSlopPhysicalUnits = 8;
        public float FitMargin = 1.15f, MinimumViewSize = 2, MaximumZoomFactor = 4;
        public float EntitySize = 0.65f, TileSize = 0.92f, PreviewSize = 0.98f;
        public float GridTileSize = 0.96f, FloorBoundaryWidth = 0.04f, MoveAnchorSize = 0.45f;
        public float CameraDepth = -10;
        [Range(0, 31)] public int WorldLayer = 31;
        public Color RoomColor = new Color(0.24f, 0.31f, 0.39f);
        public Color FixedColor = new Color(0.38f, 0.40f, 0.32f);
        public Color CorridorColor = new Color(0.35f, 0.30f, 0.24f);
        public Color MonsterColor = new Color(0.68f, 0.83f, 0.98f);
        public Color TrapColor = new Color(1, 0.72f, 0.35f);
        public Color LootColor = new Color(0.74f, 0.94f, 0.56f);
        public Color ValidColor = new Color(0.45f, 0.92f, 0.75f);
        public Color InvalidColor = new Color(1, 0.47f, 0.48f);
        public Color GridColor = new Color(0.12f, 0.16f, 0.20f);
        public Color FloorBoundaryColor = new Color(0.35f, 0.48f, 0.58f);
        public Color EditorGridColor = new Color(0.18f, 0.23f, 0.28f);

        public float Text(DungeonTextSize size) => size == DungeonTextSize.Small ? SmallText :
            size == DungeonTextSize.Large ? LargeText : DefaultText;
        public float PhysicalScale(DungeonTargetPlatform platform, float dpi) =>
            platform == DungeonTargetPlatform.Desktop ? 1 : (dpi > 0 ? dpi : FallbackDpi) /
            (platform == DungeonTargetPlatform.IOS ? IOSPointsPerInch : AndroidBaselineDpi);
        public float MinimumPixels(DungeonTargetPlatform platform, float dpi) =>
            (platform == DungeonTargetPlatform.Android ? AndroidMinimumDp :
             platform == DungeonTargetPlatform.IOS ? IOSMinimumPoints : DesktopMinimumPixels) * PhysicalScale(platform, dpi);
        public float MinimumPanelUnits(DungeonTargetPlatform platform, float dpi, float panelScale) =>
            MinimumPixels(platform, dpi) / Mathf.Max(panelScale, float.Epsilon);
        public float MinimumPanelUnitsFromNativeScale(DungeonTargetPlatform platform, float pixelsPerUnit, float panelScale) =>
            (platform == DungeonTargetPlatform.Android ? AndroidMinimumDp : platform == DungeonTargetPlatform.IOS ?
                IOSMinimumPoints : DesktopMinimumPixels) * pixelsPerUnit / Mathf.Max(panelScale, float.Epsilon);

        // Screen bottom-left coordinates to UI Toolkit top-left, then actual panel units.
        public static Rect SafePanelRect(Rect safeArea, float screenHeight, float panelScale) =>
            new Rect(safeArea.x / panelScale, (screenHeight - safeArea.yMax) / panelScale,
                safeArea.width / panelScale, safeArea.height / panelScale);
    }

    /// <summary>Pure orthographic viewport calculations; device reads live in the component.</summary>
    public sealed class DungeonViewport
    {
        private readonly DungeonPresentationPolicy policy;
        private Rect bounds;
        public Rect ScreenRect { get; private set; }
        public Vector2 Center { get; private set; }
        public float Size { get; private set; }
        public float FitSize { get; private set; }
        public DungeonViewport(DungeonPresentationPolicy policy) { this.policy = policy; }
        public void Configure(Rect floorBounds, Rect screenRect, bool reset)
        {
            bounds = floorBounds; ScreenRect = screenRect;
            float aspect = screenRect.width / Mathf.Max(screenRect.height, 1);
            FitSize = Mathf.Max(policy.MinimumViewSize,
                Mathf.Max(bounds.height, bounds.width / aspect) * 0.5f * policy.FitMargin);
            if (reset) { Center = bounds.center; Size = FitSize; }
            Clamp();
        }
        public Vector2 ScreenToWorld(Vector2 screen)
        {
            float scale = 2 * Size / Mathf.Max(ScreenRect.height, 1);
            return Center + (screen - ScreenRect.center) * scale;
        }
        public void Pan(Vector2 delta) { Center -= delta * (2 * Size / Mathf.Max(ScreenRect.height, 1)); Clamp(); }
        public void Zoom(float ratio, Vector2 anchor)
        {
            if (!float.IsFinite(ratio) || ratio <= 0) return;
            Vector2 before = ScreenToWorld(anchor);
            Size /= ratio; Clamp();
            Center += before - ScreenToWorld(anchor); Clamp();
        }
        public void Reset() { Center = bounds.center; Size = FitSize; Clamp(); }
        public void Focus(Rect geometry)
        {
            float aspect = ScreenRect.width / Mathf.Max(ScreenRect.height, 1);
            Center = geometry.center;
            Size = Mathf.Max(geometry.height, geometry.width / aspect) * 0.5f * policy.FitMargin;
            Clamp();
        }
        private void Clamp()
        {
            Size = Mathf.Clamp(Size, Mathf.Max(policy.MinimumViewSize, FitSize / policy.MaximumZoomFactor), FitSize);
            float halfWidth = Size * ScreenRect.width / Mathf.Max(ScreenRect.height, 1);
            Center = new Vector2(ClampCenter(Center.x, bounds.xMin, bounds.xMax, halfWidth),
                ClampCenter(Center.y, bounds.yMin, bounds.yMax, Size));
        }
        private static float ClampCenter(float value, float min, float max, float half) =>
            max - min <= half * 2 ? (min + max) * 0.5f : Mathf.Clamp(value, min + half, max - half);
    }

    public sealed class DungeonViewportGesture
    {
        private bool active, blocked, dragged, pinched;
        private Vector2 origin, previous;
        private float previousDistance;
        public void Begin(Vector2 point, bool overChrome)
        { active = true; blocked = overChrome; dragged = pinched = false; origin = previous = point; previousDistance = 0; }
        public void Update(DungeonViewport viewport, Vector2 point, Vector2? second, bool secondOverChrome, float tapSlop)
        {
            if (!active) return;
            if (second.HasValue && secondOverChrome) blocked = true;
            if (!blocked)
            {
                if (second.HasValue)
                {
                    float distance = Vector2.Distance(point, second.Value);
                    if (previousDistance > 0) viewport.Zoom(distance / previousDistance, (point + second.Value) * 0.5f);
                    previousDistance = distance; pinched = true;
                }
                else
                {
                    if (Vector2.Distance(origin, point) > tapSlop) dragged = true;
                    if (dragged && !pinched) viewport.Pan(point - previous);
                }
            }
            previous = point;
        }
        public bool End(out Vector2 tap)
        { tap = previous; bool accepted = active && !blocked && !dragged && !pinched; active = false; return accepted; }
        public void Cancel() { active = false; }
    }
}
