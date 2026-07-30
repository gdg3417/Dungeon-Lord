#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Editor.Build;
using DungeonBuilder.M0.EditorTools;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonBuilder.M0.Editor.DungeonSpatial.Tests
{
    public sealed class ProductionSpatialContentBuildGateTests
    {
        private const string TestRoot = "Assets/_Project/Editor/DungeonSpatial/Tests/TempBuildGate";
        private TextAsset manifest;
        private TextAsset catalog;
        private TextAsset english;
        private TextAsset limits;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            Directory.CreateDirectory(TestRoot);
            AssetDatabase.Refresh();
            manifest = Load(ProductionSpatialGeneratedSetParser.ManifestPath);
            catalog = Load(ProductionSpatialGeneratedSetParser.CatalogPath);
            english = Load(ProductionSpatialGeneratedSetParser.EnglishPath);
            limits = Load(ProductionSpatialContentPublicationService.LimitsPath);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
            Assert.That(AssetDatabase.IsValidFolder(TestRoot), Is.False);
        }

        [Test]
        public void ProductionGate_ValidInstalledSetAndExactBootstrapAssignmentsPass()
        {
            ProductionSpatialBuildGateResult result = RealValidationWithoutRecovery().Validate(BootstrapOnly());
            Assert.That(result.Success, Is.True, result.Reason + ":" + result.Detail);
        }

        [Test]
        public void ProductionGate_SuccessPreservesRequiredFilesByteForByte()
        {
            string[] paths = ProductionSpatialGeneratedSetParser.RequiredPaths
                .Concat(new[] { ProductionSpatialContentPublicationService.LimitsPath }).ToArray();
            byte[][] before = paths.Select(File.ReadAllBytes).ToArray();
            Assert.That(RealValidationWithoutRecovery().Validate(BootstrapOnly()).Success, Is.True);
            for (int index = 0; index < paths.Length; index++)
                CollectionAssert.AreEqual(before[index], File.ReadAllBytes(paths[index]), paths[index]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(DevelopmentBuildUtility.SampleScenePath)]
        [TestCase(DevelopmentBuildUtility.BootstrapScenePath + "|" + DevelopmentBuildUtility.SampleScenePath)]
        [TestCase(DevelopmentBuildUtility.BootstrapScenePath + "|" + DevelopmentBuildUtility.BootstrapScenePath)]
        [TestCase("Assets/_Project/Scenes/BootstrapCopy.unity")]
        [TestCase("assets/_Project/Scenes/Bootstrap.unity")]
        public void AttemptedSceneCompositionRejectsEveryNonCanonicalList(string joined)
        {
            string[] scenes = joined == null ? null : joined.Length == 0 ? Array.Empty<string>() : joined.Split('|');
            Assert.That(ProductionSpatialContentBuildGate.ValidateBuildScenes(scenes).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidBuildSceneComposition));
        }

        [Test]
        public void AttemptedSceneCompositionAcceptsExactlyBootstrap()
        {
            Assert.That(ProductionSpatialContentBuildGate.ValidateBuildScenes(BootstrapOnly()).Success, Is.True);
        }

        [Test]
        public void BuildPlayerProcessorPassesAttemptedBuildPlayerOptionsScenesToGate()
        {
            string[] supplied = { DevelopmentBuildUtility.SampleScenePath };
            string[] observed = null;
            var gate = Gate(composition: scenes => { observed = scenes; return ProductionSpatialContentBuildGate.Success(); });
            var callback = new ProductionSpatialContentBuildPreprocessor(gate);
            callback.PrepareForBuild(new BuildPlayerContext(new BuildPlayerOptions { scenes = supplied }));
            Assert.That(observed, Is.SameAs(supplied));
            Assert.That(callback, Is.InstanceOf<BuildPlayerProcessor>());
        }

        [Test]
        public void StagesExecuteInRequiredOrderAndShortCircuit()
        {
            var calls = new List<string>();
            var gate = Gate(
                () => Record(calls, "recovery"),
                () => Record(calls, "installed"),
                scenes => Record(calls, "composition"));
            Assert.That(gate.Validate(BootstrapOnly()).Success, Is.True);
            CollectionAssert.AreEqual(new[] { "recovery", "installed", "composition" }, calls);

            calls.Clear();
            gate = Gate(() => ProductionSpatialContentBuildGate.Failure(
                ProductionSpatialBuildGateReason.RecoveryFailure, "test"),
                () => Record(calls, "installed"), scenes => Record(calls, "composition"));
            Assert.That(gate.Validate(BootstrapOnly()).Reason, Is.EqualTo(ProductionSpatialBuildGateReason.RecoveryFailure));
            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void InitialUnpublishedRecoverySuccessContinuesToMissingInstalledSetFailure()
        {
            var gate = Gate(ProductionSpatialContentBuildGate.Success,
                () => ProductionSpatialContentBuildGate.Failure(
                    ProductionSpatialBuildGateReason.MissingRequiredProductionFile, "manifest"));
            Assert.That(gate.Validate(BootstrapOnly()).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.MissingRequiredProductionFile));
        }

        [Test]
        public void InstalledSetRepresentativeFailuresHaveStableClassifications()
        {
            ProductionSpatialBuildGateResult missing = ProductionSpatialContentBuildGate.ValidateRequiredPaths(
                new[] { "manifest", "catalog" }, path => path == "catalog");
            Assert.That(missing.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.MissingRequiredProductionFile));
            Assert.That(ProductionSpatialContentBuildGate.ValidateLoadedAssets(manifest, catalog,
                new[] { english }, new TextAsset("{}")).Reason, Is.EqualTo(ProductionSpatialBuildGateReason.LimitsFailure));
            Assert.That(ProductionSpatialContentBuildGate.ValidateLoadedAssets(new TextAsset("{}"), catalog,
                new[] { english }, limits).Reason, Is.EqualTo(ProductionSpatialBuildGateReason.InstalledGeneratedSetValidationFailure));
            Assert.That(ProductionSpatialContentBuildGate.ValidateLoadedAssets(manifest, catalog,
                new[] { new TextAsset("{}") }, limits).Reason, Is.EqualTo(ProductionSpatialBuildGateReason.LocalizationFailure));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OneActiveOrInactiveCorrectGameRootPasses(bool inactive)
        {
            string path = CreateScene("valid" + inactive, root =>
            {
                AssignValid(root);
                root.gameObject.SetActive(!inactive);
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Success, Is.True);
            AssertPreviewClosed(path);
        }

        [Test]
        public void MissingAndDuplicateGameRootsAreClassified()
        {
            string missing = CreateScene("missing", null, false);
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(missing).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.MissingGameRoot));
            string duplicate = CreateScene("duplicate", AssignValid, true, 2);
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(duplicate).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.DuplicateGameRoot));
        }

        [TestCase("manifest")]
        [TestCase("catalog")]
        [TestCase("languages-null")]
        [TestCase("languages-empty")]
        [TestCase("language-null-entry")]
        [TestCase("limits")]
        public void MissingAssignmentsAreClassified(string field)
        {
            string path = CreateScene("missing-" + field, root =>
            {
                AssignValid(root);
                if (field == "manifest") root.productionSpatialManifest = null;
                if (field == "catalog") root.productionSpatialCatalog = null;
                if (field == "languages-null") root.productionSpatialLanguageTables = null;
                if (field == "languages-empty") root.productionSpatialLanguageTables = Array.Empty<TextAsset>();
                if (field == "language-null-entry") root.productionSpatialLanguageTables = new TextAsset[] { null };
                if (field == "limits") root.productionSpatialValidationLimits = null;
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.MissingAssignment));
        }

        [TestCase("manifest")]
        [TestCase("catalog")]
        [TestCase("english")]
        [TestCase("limits")]
        public void ByteIdenticalCopiedRequiredAssignmentsAreRejected(string field)
        {
            string source = field == "manifest" ? ProductionSpatialGeneratedSetParser.ManifestPath :
                field == "catalog" ? ProductionSpatialGeneratedSetParser.CatalogPath :
                field == "english" ? ProductionSpatialGeneratedSetParser.EnglishPath :
                ProductionSpatialContentPublicationService.LimitsPath;
            string copy = TestRoot + "/copied-" + field + ".json";
            Assert.That(AssetDatabase.CopyAsset(source, copy), Is.True);
            TextAsset copied = Load(copy);
            string path = CreateScene("wrong-" + field, root =>
            {
                AssignValid(root);
                if (field == "manifest") root.productionSpatialManifest = copied;
                if (field == "catalog") root.productionSpatialCatalog = copied;
                if (field == "english") root.productionSpatialLanguageTables = new[] { copied };
                if (field == "limits") root.productionSpatialValidationLimits = copied;
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.WrongAssetAssignment));
        }

        [Test]
        public void DuplicateLanguagePathIsRejected()
        {
            string path = CreateScene("duplicate-language", root =>
            {
                AssignValid(root);
                root.productionSpatialLanguageTables = new[] { english, english };
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.WrongAssetAssignment));
        }

        [Test]
        public void AdditionalLanguagesUseCompleteLoaderValidation()
        {
            TextAsset valid = CreateAdditionalLanguage("zz", true);
            string validScene = CreateScene("valid-language", root =>
            {
                AssignValid(root);
                root.productionSpatialLanguageTables = new[] { english, valid };
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(validScene).Success, Is.True);

            TextAsset invalid = CreateAdditionalLanguage("bad", false);
            string invalidScene = CreateScene("invalid-language", root =>
            {
                AssignValid(root);
                root.productionSpatialLanguageTables = new[] { english, invalid };
            });
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(invalidScene).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.LocalizationFailure));
        }

        [Test]
        public void PreviewOpenFailureAndPostOpenFailureHaveAccurateReasonsAndCleanup()
        {
            string path = CreateScene("exception", AssignValid);
            ProductionSpatialBuildGateResult open = ProductionSpatialContentBuildGate.ValidateBootstrapComposition(
                path, null, _ => throw new IOException("test"));
            Assert.That(open.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.BootstrapSceneUnavailable));

            var gate = Gate(composition: scenes => ProductionSpatialContentBuildGate.ValidateBootstrapComposition(
                path, () => throw new InvalidOperationException("test")));
            ProductionSpatialBuildGateResult unexpected = gate.Validate(BootstrapOnly());
            Assert.That(unexpected.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.UnexpectedInternalValidationFailure));
            AssertPreviewClosed(path);
        }

        [Test]
        public void PreviewInspectionDoesNotDirtyOrSaveSourceScene()
        {
            string path = CreateScene("nondirty", AssignValid);
            byte[] before = File.ReadAllBytes(path);
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Success, Is.True);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            AssertPreviewClosed(path);
        }

        [Test]
        public void CallbackConvertsFailureToBuildFailedExceptionWithReasonCode()
        {
            var failing = Gate(() => ProductionSpatialContentBuildGate.Failure(
                ProductionSpatialBuildGateReason.RecoveryFailure, "InvalidJournal"));
            BuildFailedException exception = Assert.Throws<BuildFailedException>(() =>
                ProductionSpatialContentBuildPreprocessor.ValidateOrThrow(failing, BootstrapOnly()));
            StringAssert.Contains("[ProductionSpatialBuildGate:RecoveryFailure]", exception.Message);
        }

        private TextAsset CreateAdditionalLanguage(string language, bool valid)
        {
            string path = TestRoot + "/language-" + language + ".json";
            string text = valid ? english.text.Replace("\"language\": \"en\"", "\"language\": \"" + language + "\"") : "{}";
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return Load(path);
        }

        private string CreateScene(string name, Action<GameRoot> configure, bool includeRoot = true, int count = 1)
        {
            string path = TestRoot + "/" + name + ".unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            if (includeRoot)
                for (int index = 0; index < count; index++)
                {
                    GameRoot root = new GameObject("GameRoot" + index).AddComponent<GameRoot>();
                    configure?.Invoke(root);
                }
            Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
            return path;
        }

        private void AssignValid(GameRoot root)
        {
            root.productionSpatialManifest = manifest;
            root.productionSpatialCatalog = catalog;
            root.productionSpatialLanguageTables = new[] { english };
            root.productionSpatialValidationLimits = limits;
        }

        private static void AssertPreviewClosed(string path)
        {
            Assert.That(SceneManager.GetSceneByPath(path).isLoaded, Is.False);
        }

        private static TextAsset Load(string path) => AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        private static string[] BootstrapOnly() => new[] { DevelopmentBuildUtility.BootstrapScenePath };

        private static ProductionSpatialContentBuildGate Gate(
            Func<ProductionSpatialBuildGateResult> recovery = null,
            Func<ProductionSpatialBuildGateResult> installed = null,
            Func<string[], ProductionSpatialBuildGateResult> composition = null) =>
            new ProductionSpatialContentBuildGate(recovery ?? ProductionSpatialContentBuildGate.Success,
                installed ?? ProductionSpatialContentBuildGate.Success,
                composition ?? (_ => ProductionSpatialContentBuildGate.Success()));

        private static ProductionSpatialContentBuildGate RealValidationWithoutRecovery() =>
            Gate(ProductionSpatialContentBuildGate.Success,
                ProductionSpatialContentBuildGate.ValidateInstalledSet,
                ProductionSpatialContentBuildGate.ValidateComposition);

        private static ProductionSpatialBuildGateResult Record(ICollection<string> calls, string value)
        {
            calls.Add(value);
            return ProductionSpatialContentBuildGate.Success();
        }
    }
}
#endif
