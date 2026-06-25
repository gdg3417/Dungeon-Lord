using System.Reflection;
using DungeonBuilder.M0;

namespace DungeonBuilder.Tests.EditMode
{
    internal static class TestDiagnosticsHelper
    {
        public static void SetDevelopmentDiagnostics(GameRoot root, bool enabled)
        {
            typeof(GameRoot).GetField("<DevPanelEnabled>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(root, enabled);
        }

        public static void EnableDevelopmentDiagnostics(GameRoot root)
        {
            SetDevelopmentDiagnostics(root, true);
        }
    }
}
