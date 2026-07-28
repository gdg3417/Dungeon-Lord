using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum ProductionSpatialContentLoadingDiagnostic
    {
        None = 0,
        MissingManifest = 1,
        MissingCatalog = 2,
        MissingLimits = 3,
        NullLanguageCollection = 4,
        EmptyLanguageCollection = 5,
        NullLanguageEntry = 6,
        InvalidWorkloadLimits = 7,
        InvalidLanguageTable = 8,
        BlankLanguageIdentity = 9,
        MissingEnglish = 10,
        DuplicateLanguageIdentity = 11,
        InvalidGeneratedSet = 12,
        LocalizationCoverageInvalid = 13,
        WorkloadExceeded = 14
    }

    public sealed class ProductionSpatialContentLoadResult
    {
        internal ProductionSpatialContentLoadResult(ProductionSpatialContentSnapshot value,
            IEnumerable<ProductionSpatialContentLoadingDiagnostic> diagnostics)
        {
            Value = value;
            Diagnostics = diagnostics.Distinct().OrderBy(value => (int)value).ToArray();
        }

        public bool Success => Value != null && Diagnostics.Length == 0;
        public ProductionSpatialContentSnapshot Value { get; }
        public ProductionSpatialContentLoadingDiagnostic[] Diagnostics { get; }
    }

    public sealed class ProductionSpatialContentSnapshot
    {
        private readonly byte[] manifestBytes;
        private readonly byte[] catalogBytes;
        private readonly byte[][] languageBytes;

        internal ProductionSpatialContentSnapshot(ProductionSpatialContentManifest manifest,
            SpatialContentCatalog catalog, IEnumerable<StringTable> languages)
        {
            manifestBytes = ProductionSpatialGeneratedSetParser.SerializeCanonical(manifest);
            catalogBytes = ProductionSpatialGeneratedSetParser.SerializeCanonical(catalog);
            languageBytes = languages.Select(ProductionSpatialGeneratedSetParser.SerializeCanonical).ToArray();
        }

        public ProductionSpatialContentManifest Manifest => Clone<ProductionSpatialContentManifest>(manifestBytes);
        public SpatialContentCatalog Catalog => Clone<SpatialContentCatalog>(catalogBytes);
        public IReadOnlyList<StringTable> Languages => Array.AsReadOnly(languageBytes.Select(Clone<StringTable>).ToArray());

        private static T Clone<T>(byte[] bytes) where T : class =>
            JsonUtility.FromJson<T>(System.Text.Encoding.UTF8.GetString(bytes));
    }

    public static class ProductionSpatialContentLoader
    {
        public static ProductionSpatialContentLoadResult Load(TextAsset manifest, TextAsset catalog,
            IReadOnlyList<TextAsset> languageTables, TextAsset limits)
        {
            var diagnostics = new List<ProductionSpatialContentLoadingDiagnostic>();
            if (manifest == null) diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.MissingManifest);
            if (catalog == null) diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.MissingCatalog);
            if (limits == null) diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.MissingLimits);
            if (languageTables == null)
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.NullLanguageCollection);
            else if (languageTables.Count == 0)
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.EmptyLanguageCollection);
            else if (languageTables.Any(value => value == null))
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.NullLanguageEntry);
            if (diagnostics.Count != 0) return Failure(diagnostics);

            ProductionSpatialContentWorkloadLimitParseResult limitResult =
                ProductionSpatialContentWorkloadLimitParser.Parse(limits);
            if (!limitResult.Success)
                return Failure(ProductionSpatialContentLoadingDiagnostic.InvalidWorkloadLimits);
            if (languageTables.Count > limitResult.Limits.MaximumNestedRecords)
                return Failure(ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded);

            var parsed = new List<StringTable>();
            bool workloadFailure = false;
            foreach (TextAsset asset in languageTables)
            {
                ProductionSpatialLanguageResult result =
                    ProductionSpatialGeneratedSetParser.ParseAndValidateLanguage(asset.bytes, limitResult.Limits);
                if (!result.Success)
                {
                    if (result.Diagnostics.Contains(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded) ||
                        result.Diagnostics.Contains(ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded))
                        workloadFailure = true;
                    else
                        diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(result.Value.language))
                    diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.BlankLanguageIdentity);
                parsed.Add(result.Value);
            }

            if (workloadFailure) diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded);
            var groups = parsed.GroupBy(value => value.language, StringComparer.Ordinal).ToArray();
            if (!groups.Any(group => string.Equals(group.Key, "en", StringComparison.Ordinal)))
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.MissingEnglish);
            if (groups.Any(group => group.Count() != 1))
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.DuplicateLanguageIdentity);
            if (diagnostics.Count != 0) return Failure(diagnostics);

            StringTable english = groups.Single(group => group.Key == "en").Single();
            var supplied = new ProductionSpatialGeneratedSet(new[]
            {
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.ManifestPath, manifest.bytes),
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.CatalogPath, catalog.bytes),
                new ProductionSpatialGeneratedFile(ProductionSpatialGeneratedSetParser.EnglishPath,
                    ProductionSpatialGeneratedSetParser.SerializeCanonical(english))
            });
            ProductionSpatialGeneratedSetResult baseResult =
                ProductionSpatialGeneratedSetParser.ParseAndValidate(supplied, limitResult.Limits);
            if (!baseResult.Success)
                return Failure(baseResult.Diagnostics.Contains(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded) ||
                    baseResult.Diagnostics.Contains(ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded)
                    ? ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded
                    : ProductionSpatialContentLoadingDiagnostic.InvalidGeneratedSet);

            var requiredKeys = new HashSet<string>(baseResult.Value.English.entries.Select(entry => entry.key),
                StringComparer.Ordinal);
            long records = 0L;
            long characters = 0L;
            bool aggregateOverflow = false;
            foreach (StringTable table in parsed)
            {
                aggregateOverflow |= !TryAdd(ref records, table.entries.LongLength);
                aggregateOverflow |= !TryAdd(ref characters, table.schema?.Length ?? 0);
                aggregateOverflow |= !TryAdd(ref characters, table.language?.Length ?? 0);
                foreach (StringEntry entry in table.entries)
                {
                    aggregateOverflow |= !TryAdd(ref characters, entry.key?.Length ?? 0);
                    aggregateOverflow |= !TryAdd(ref characters, entry.text?.Length ?? 0);
                }
                if (!requiredKeys.SetEquals(table.entries.Select(entry => entry.key)))
                    diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.LocalizationCoverageInvalid);
            }
            if (aggregateOverflow || records > limitResult.Limits.MaximumNestedRecords ||
                characters > limitResult.Limits.MaximumStringCharacters)
                diagnostics.Add(ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded);
            if (diagnostics.Count != 0) return Failure(diagnostics);

            StringTable[] ordered = parsed.OrderBy(value => value.language, StringComparer.Ordinal).ToArray();
            var snapshot = new ProductionSpatialContentSnapshot(baseResult.Value.Manifest,
                baseResult.Value.Catalog, ordered);
            return new ProductionSpatialContentLoadResult(snapshot,
                Array.Empty<ProductionSpatialContentLoadingDiagnostic>());
        }

        private static ProductionSpatialContentLoadResult Failure(
            ProductionSpatialContentLoadingDiagnostic diagnostic) => Failure(new[] { diagnostic });
        private static ProductionSpatialContentLoadResult Failure(
            IEnumerable<ProductionSpatialContentLoadingDiagnostic> diagnostics) =>
            new ProductionSpatialContentLoadResult(null, diagnostics);

        private static bool TryAdd(ref long total, long amount)
        {
            if (amount < 0L || total < 0L || total > long.MaxValue - amount) return false;
            total += amount;
            return true;
        }
    }
}
