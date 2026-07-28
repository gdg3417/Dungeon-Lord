#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    /// <summary>Editor invocation boundary for the production spatial publication service.</summary>
    public static class ProductionSpatialContentExportCommand
    {
        public const string MenuPath = "Tools/Dungeon Lord/Content/Export Production Spatial Content";
        private const string MessagePrefix = "Production spatial content export";

        [MenuItem(MenuPath)]
        public static void ExportProductionSpatialContentMenu() => ExecuteProductionSpatialContent();

        public static void ExportProductionSpatialContentCommandLine() => ExecuteProductionSpatialContent();

        internal static ProductionSpatialPublicationResult ExecuteProductionSpatialContent() =>
            Execute(ProductionSpatialContentPublicationService.PublishProduction);

        internal static ProductionSpatialPublicationResult Execute(
            Func<ProductionSpatialPublicationResult> publish)
        {
            if (publish == null) throw new ArgumentNullException(nameof(publish));

            ProductionSpatialPublicationResult result = publish();
            if (result == null)
                throw new InvalidOperationException(MessagePrefix + " failed: publication returned no result.");

            string message = FormatDiagnostics(result);
            if (result.Success)
            {
                Debug.Log(message);
                return result;
            }

            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        internal static string FormatDiagnostics(ProductionSpatialPublicationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            string header = MessagePrefix + " status: " + result.Status + ".";
            if (result.Diagnostics == null || result.Diagnostics.Length == 0)
                return header + " Diagnostics: None.";

            return header + " Diagnostics: " + string.Join(", ", result.Diagnostics
                .OrderBy(value => (int)value).Select(value => value.ToString())) + ".";
        }
    }
}
#endif
