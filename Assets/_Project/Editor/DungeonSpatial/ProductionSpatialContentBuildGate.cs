#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.EditorTools;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    public enum ProductionSpatialBuildGateReason
    {
        None = 0,
        RecoveryFailure = 1,
        MissingRequiredProductionFile = 2,
        InstalledGeneratedSetValidationFailure = 3,
        LimitsFailure = 4,
        LocalizationFailure = 5,
        BootstrapSceneUnavailable = 6,
        MissingGameRoot = 7,
        DuplicateGameRoot = 8,
        MissingAssignment = 9,
        WrongAssetAssignment = 10,
        UnexpectedInternalValidationFailure = 11
    }

    public sealed class ProductionSpatialBuildGateResult
    {
        internal ProductionSpatialBuildGateResult(ProductionSpatialBuildGateReason reason, string detail)
        {
            Reason = reason;
            Detail = detail ?? string.Empty;
        }

        public bool Success => Reason == ProductionSpatialBuildGateReason.None;
        public ProductionSpatialBuildGateReason Reason { get; }
        public string Detail { get; }
    }

    public sealed class ProductionSpatialContentBuildGate
    {
        private readonly Func<ProductionSpatialBuildGateResult> recover;
        private readonly Func<ProductionSpatialBuildGateResult> validateInstalled;
        private readonly Func<ProductionSpatialBuildGateResult> validateComposition;

        public ProductionSpatialContentBuildGate()
            : this(Recover, ValidateInstalledSet, ValidateBootstrapComposition) { }

        internal ProductionSpatialContentBuildGate(Func<ProductionSpatialBuildGateResult> recover,
            Func<ProductionSpatialBuildGateResult> validateInstalled,
            Func<ProductionSpatialBuildGateResult> validateComposition)
        {
            this.recover = recover ?? throw new ArgumentNullException(nameof(recover));
            this.validateInstalled = validateInstalled ?? throw new ArgumentNullException(nameof(validateInstalled));
            this.validateComposition = validateComposition ?? throw new ArgumentNullException(nameof(validateComposition));
        }

        public ProductionSpatialBuildGateResult Validate()
        {
            try
            {
                ProductionSpatialBuildGateResult result = recover();
                if (!result.Success) return result;
                result = validateInstalled();
                if (!result.Success) return result;
                return validateComposition();
            }
            catch (Exception exception)
            {
                return Failure(ProductionSpatialBuildGateReason.UnexpectedInternalValidationFailure,
                    exception.GetType().FullName);
            }
        }

        private static ProductionSpatialBuildGateResult Recover()
        {
            ProductionSpatialPublicationResult result = ProductionSpatialContentPublicationService.RecoverProduction();
            return result.Success
                ? Success()
                : Failure(ProductionSpatialBuildGateReason.RecoveryFailure,
                    StableDetail(result.Status, result.Diagnostics));
        }

        private static ProductionSpatialBuildGateResult ValidateInstalledSet()
        {
            foreach (string path in ProductionSpatialGeneratedSetParser.RequiredPaths
                         .Concat(new[] { ProductionSpatialContentPublicationService.LimitsPath }))
            {
                if (!File.Exists(path))
                    return Failure(ProductionSpatialBuildGateReason.MissingRequiredProductionFile, path);
            }

            TextAsset manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(ProductionSpatialGeneratedSetParser.ManifestPath);
            TextAsset catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(ProductionSpatialGeneratedSetParser.CatalogPath);
            TextAsset english = AssetDatabase.LoadAssetAtPath<TextAsset>(ProductionSpatialGeneratedSetParser.EnglishPath);
            TextAsset limits = AssetDatabase.LoadAssetAtPath<TextAsset>(ProductionSpatialContentPublicationService.LimitsPath);
            if (manifest == null || catalog == null || english == null || limits == null)
                return Failure(ProductionSpatialBuildGateReason.MissingRequiredProductionFile, "AssetDatabaseImport");

            return FromLoadResult(ProductionSpatialContentLoader.Load(manifest, catalog,
                new[] { english }, limits));
        }

        private static ProductionSpatialBuildGateResult ValidateBootstrapComposition()
        {
            if (!File.Exists(DevelopmentBuildUtility.BootstrapScenePath) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(DevelopmentBuildUtility.BootstrapScenePath) == null)
                return Failure(ProductionSpatialBuildGateReason.BootstrapSceneUnavailable,
                    DevelopmentBuildUtility.BootstrapScenePath);

            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(DevelopmentBuildUtility.BootstrapScenePath);
                GameRoot[] roots = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameRoot>(true)).ToArray();
                if (roots.Length == 0) return Failure(ProductionSpatialBuildGateReason.MissingGameRoot, "GameRoot");
                if (roots.Length != 1) return Failure(ProductionSpatialBuildGateReason.DuplicateGameRoot, roots.Length.ToString());

                GameRoot gameRoot = roots[0];
                if (gameRoot.productionSpatialManifest == null || gameRoot.productionSpatialCatalog == null ||
                    gameRoot.productionSpatialValidationLimits == null || gameRoot.productionSpatialLanguageTables == null ||
                    gameRoot.productionSpatialLanguageTables.Length == 0 ||
                    gameRoot.productionSpatialLanguageTables.Any(asset => asset == null))
                    return Failure(ProductionSpatialBuildGateReason.MissingAssignment, "ProductionSpatialContent");

                if (!ExactPath(gameRoot.productionSpatialManifest, ProductionSpatialGeneratedSetParser.ManifestPath) ||
                    !ExactPath(gameRoot.productionSpatialCatalog, ProductionSpatialGeneratedSetParser.CatalogPath) ||
                    !ExactPath(gameRoot.productionSpatialValidationLimits, ProductionSpatialContentPublicationService.LimitsPath) ||
                    !gameRoot.productionSpatialLanguageTables.Any(asset =>
                        ExactPath(asset, ProductionSpatialGeneratedSetParser.EnglishPath)) ||
                    gameRoot.productionSpatialLanguageTables.Select(AssetDatabase.GetAssetPath)
                        .Distinct(StringComparer.Ordinal).Count() != gameRoot.productionSpatialLanguageTables.Length)
                    return Failure(ProductionSpatialBuildGateReason.WrongAssetAssignment, "ProductionSpatialContent");

                return FromLoadResult(ProductionSpatialContentLoader.Load(gameRoot.productionSpatialManifest,
                    gameRoot.productionSpatialCatalog, gameRoot.productionSpatialLanguageTables,
                    gameRoot.productionSpatialValidationLimits));
            }
            catch (Exception exception)
            {
                return Failure(ProductionSpatialBuildGateReason.BootstrapSceneUnavailable,
                    exception.GetType().FullName);
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static bool ExactPath(UnityEngine.Object asset, string expected) =>
            string.Equals(AssetDatabase.GetAssetPath(asset), expected, StringComparison.Ordinal);

        private static ProductionSpatialBuildGateResult FromLoadResult(ProductionSpatialContentLoadResult result)
        {
            if (result.Success) return Success();
            ProductionSpatialContentLoadingDiagnostic[] diagnostics = result.Diagnostics;
            ProductionSpatialBuildGateReason reason = diagnostics.Any(value =>
                    value == ProductionSpatialContentLoadingDiagnostic.MissingLimits ||
                    value == ProductionSpatialContentLoadingDiagnostic.InvalidWorkloadLimits ||
                    value == ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded)
                ? ProductionSpatialBuildGateReason.LimitsFailure
                : diagnostics.Any(value => value == ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable ||
                    value == ProductionSpatialContentLoadingDiagnostic.BlankLanguageIdentity ||
                    value == ProductionSpatialContentLoadingDiagnostic.MissingEnglish ||
                    value == ProductionSpatialContentLoadingDiagnostic.DuplicateLanguageIdentity ||
                    value == ProductionSpatialContentLoadingDiagnostic.LocalizationCoverageInvalid)
                    ? ProductionSpatialBuildGateReason.LocalizationFailure
                    : ProductionSpatialBuildGateReason.InstalledGeneratedSetValidationFailure;
            return Failure(reason, StableDetail(null, diagnostics));
        }

        private static string StableDetail<TDiagnostic>(object status,
            IEnumerable<TDiagnostic> diagnostics) =>
            (status == null ? string.Empty : status + ":") +
            string.Join(",", diagnostics.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal));

        internal static ProductionSpatialBuildGateResult Success() =>
            new ProductionSpatialBuildGateResult(ProductionSpatialBuildGateReason.None, string.Empty);

        internal static ProductionSpatialBuildGateResult Failure(ProductionSpatialBuildGateReason reason,
            string detail) => new ProductionSpatialBuildGateResult(reason, detail);
    }
}
#endif
