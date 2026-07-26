#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    public sealed class ProductionSpatialGeneratedSetBuildResult
    {
        internal ProductionSpatialGeneratedSetBuildResult(ProductionSpatialGeneratedSet output,
            ProductionSpatialGeneratedSetDiagnostic[] diagnostics)
        { Output = output; Diagnostics = diagnostics ?? Array.Empty<ProductionSpatialGeneratedSetDiagnostic>(); }
        public bool Success => Output != null && Diagnostics.Length == 0;
        public ProductionSpatialGeneratedSet Output { get; }
        public ProductionSpatialGeneratedSetDiagnostic[] Diagnostics { get; }
    }

    public static class ProductionSpatialGeneratedSetBuilder
    {
        public static ProductionSpatialGeneratedSetBuildResult Build(DungeonSpatialAuthoringProjection projection,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (projection?.Catalog == null || projection.English == null)
                return Failure(ProductionSpatialGeneratedSetDiagnostic.MissingInput);
            var sourceKeys = projection.English.entries == null ? null : new HashSet<string>(
                projection.English.entries.Where(entry => entry != null).Select(entry => entry.key), StringComparer.Ordinal);
            long englishCharacters = (projection.English.schema?.Length ?? 0) +
                (projection.English.language?.Length ?? 0);
            foreach (StringEntry entry in projection.English.entries ?? Array.Empty<StringEntry>())
                englishCharacters += entry?.text?.Length ?? 0;
            SpatialContentValidationResult validation = SpatialContentValidator.Validate(
                projection.Catalog, limits, sourceKeys, englishCharacters);
            if (!validation.IsValid)
                return Failure(validation.Issues.Any(issue =>
                    issue.Reason == SpatialContentValidationReason.WorkloadLimitsInvalid ||
                    issue.Reason == SpatialContentValidationReason.WorkloadExceeded)
                    ? ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded
                    : ProductionSpatialGeneratedSetDiagnostic.CatalogInvalid);
            if (!SpatialContentCanonicalizer.TryCanonicalize(
                projection.Catalog, limits, out SpatialContentCatalog catalog))
                return Failure(ProductionSpatialGeneratedSetDiagnostic.CatalogInvalid);

            StringTable english;
            try { english = JsonUtility.FromJson<StringTable>(JsonUtility.ToJson(projection.English)); }
            catch { return Failure(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid); }
            if (english?.entries == null)
                return Failure(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid);
            english.entries = english.entries.OrderBy(entry => entry?.key, StringComparer.Ordinal).ToArray();
            var manifest = new ProductionSpatialContentManifest
            {
                schema = "content_manifest", schemaVersion = 1, contentVersion = catalog.Metadata.ContentVersion,
                requiredSchemas = new[]
                {
                    new ProductionSpatialRequiredSchema { schemaId = catalog.Metadata.SchemaId, schemaVersion = catalog.Metadata.SchemaVersion },
                    new ProductionSpatialRequiredSchema { schemaId = english.schema, schemaVersion = english.schemaVersion }
                }.OrderBy(entry => entry.schemaId, StringComparer.Ordinal).ToArray()
            };
            var output = new ProductionSpatialGeneratedSet(new[]
            {
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.ManifestPath,
                    ProductionSpatialGeneratedSetParser.SerializeCanonical(manifest)),
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.CatalogPath,
                    ProductionSpatialGeneratedSetParser.SerializeCanonical(catalog)),
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.EnglishPath,
                    ProductionSpatialGeneratedSetParser.SerializeCanonical(english))
            });
            ProductionSpatialGeneratedSetResult reparsed = ProductionSpatialGeneratedSetParser.ParseAndValidate(output, limits);
            return reparsed.Success ? new ProductionSpatialGeneratedSetBuildResult(output, Array.Empty<ProductionSpatialGeneratedSetDiagnostic>())
                : new ProductionSpatialGeneratedSetBuildResult(null, reparsed.Diagnostics);
        }

        private static ProductionSpatialGeneratedSetBuildResult Failure(ProductionSpatialGeneratedSetDiagnostic diagnostic) =>
            new ProductionSpatialGeneratedSetBuildResult(null, new[] { diagnostic });
    }
}
#endif
