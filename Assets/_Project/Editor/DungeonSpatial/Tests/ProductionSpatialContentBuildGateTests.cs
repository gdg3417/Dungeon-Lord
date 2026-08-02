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
                .Concat(new[] { ProductionSpatialContentPublicationService.LimitsPath,
                    SpatialLayoutCompatibilityProfiles.ProductionPath }).ToArray();
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
            ProductionSpatialContentBuildPreprocessor.PrepareForBuild(gate,
                new BuildPlayerOptions { scenes = supplied });
            Assert.That(observed, Is.SameAs(supplied));
            Assert.That(callback, Is.InstanceOf<BuildPlayerProcessor>());
            Assert.That(callback.callbackOrder, Is.EqualTo(-1000));
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

        [Test]
        public void CompatibilityFailuresUseAppendedStableReasons()
        {
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(new TextAsset("{}\n"), manifest,
                catalog, new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidCompatibilityProfile));

            TextAsset production = Load(SpatialLayoutCompatibilityProfiles.ProductionPath);
            SpatialLayoutCompatibilityProfilesData empty = JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(production.text);
            empty.GeometryRecords = Array.Empty<CompatibilityLayoutGeometryRecord>();
            TextAsset emptyAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(empty)));
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(emptyAsset, manifest, catalog,
                new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidCompatibilityProfile));

            SpatialLayoutCompatibilityProfilesData active = JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(production.text);
            active.ContractSelections = new[] { new CanonicalLayoutContractSelection { TargetSchemaVersion = 8,
                CanonicalLayoutContractVersion = 1, Lifecycle = CompatibilityProfileLifecycle.Active } };
            TextAsset activeAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(active)));
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(activeAsset, manifest, catalog,
                new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.UnauthorizedActiveCompatibilitySelection));

            SpatialLayoutCompatibilityProfilesData invalidProfile = JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(production.text);
            CompatibilityLayoutGeometryRecord geometry = invalidProfile.GeometryRecords[0];
            invalidProfile.MigrationProfiles = new[] { new SpatialMigrationCompatibilityProfile {
                ProfileId = "test.migration", ProfileVersion = 1, CanonicalHash = "invalid",
                Lifecycle = CompatibilityProfileLifecycle.Active, MinimumSourceSchemaVersion = 1,
                MaximumSourceSchemaVersion = 6, TargetSchemaVersion = 8,
                TargetCanonicalLayoutContractVersion = 1, GeometryId = geometry.GeometryId,
                GeometryVersion = geometry.GeometryVersion, GeometryCanonicalHash = geometry.CanonicalHash } };
            TextAsset invalidProfileAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(invalidProfile)));
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(invalidProfileAsset, manifest,
                catalog, new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidCompatibilityProfile));
            invalidProfile.MigrationProfiles[0].CanonicalHash =
                SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(invalidProfile.MigrationProfiles[0]);
            TextAsset activeMigrationAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(invalidProfile)));
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(activeMigrationAsset, manifest,
                catalog, new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.UnauthorizedActiveCompatibilitySelection));

            SpatialLayoutCompatibilityProfilesData activeStarter = JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(production.text);
            var starter = new CanonicalStarterLayoutProfile { ProfileId = "test.starter", ProfileVersion = 1,
                Lifecycle = CompatibilityProfileLifecycle.Active, TargetSchemaVersion = 8,
                CanonicalLayoutContractVersion = 1, GeometryId = geometry.GeometryId,
                GeometryVersion = geometry.GeometryVersion, GeometryCanonicalHash = geometry.CanonicalHash };
            starter.CanonicalHash = SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(starter);
            activeStarter.StarterProfiles = new[] { starter };
            TextAsset activeStarterAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(activeStarter)));
            Assert.That(ProductionSpatialContentBuildGate.ValidateCompatibility(activeStarterAsset, manifest,
                catalog, new[] { english }, limits).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.UnauthorizedActiveCompatibilitySelection));
        }

        [Test]
        public void ProductionAuthorizationRejectsCompleteStructurallyValidMutationMatrix()
        {
            var mutations = new Dictionary<string, Action<SpatialLayoutCompatibilityProfilesData>>
            {
                ["missing migration"] = data => data.MigrationProfiles = Array.Empty<SpatialMigrationCompatibilityProfile>(),
                ["missing starter"] = data => data.StarterProfiles = Array.Empty<CanonicalStarterLayoutProfile>(),
                ["missing contract"] = data => data.ContractSelections = Array.Empty<CanonicalLayoutContractSelection>(),
                ["retired migration"] = data => data.MigrationProfiles[0].Lifecycle = CompatibilityProfileLifecycle.Retired,
                ["retired starter"] = data => data.StarterProfiles[0].Lifecycle = CompatibilityProfileLifecycle.Retired,
                ["retired contract"] = data => data.ContractSelections[0].Lifecycle = CompatibilityProfileLifecycle.Retired,
                ["additional active migration"] = AddActiveMigration,
                ["additional active starter"] = AddActiveStarter,
                ["additional active contract"] = data => data.ContractSelections = data.ContractSelections.Concat(new[] {
                    new CanonicalLayoutContractSelection { TargetSchemaVersion = 8,
                        CanonicalLayoutContractVersion = 1, Lifecycle = CompatibilityProfileLifecycle.Active }}).ToArray(),
                ["migration minimum"] = data => data.MigrationProfiles[0].MinimumSourceSchemaVersion = 2,
                ["migration maximum"] = data => data.MigrationProfiles[0].MaximumSourceSchemaVersion = 5,
                ["migration target"] = data => data.MigrationProfiles[0].TargetSchemaVersion = 8,
                ["migration contract"] = data => data.MigrationProfiles[0].TargetCanonicalLayoutContractVersion = 2,
                ["migration id"] = data => data.MigrationProfiles[0].ProfileId = "compat.profile.migration.unauthorized",
                ["migration version"] = data => data.MigrationProfiles[0].ProfileVersion = 2,
                ["starter target"] = data => data.StarterProfiles[0].TargetSchemaVersion = 8,
                ["starter contract"] = data => data.StarterProfiles[0].CanonicalLayoutContractVersion = 2,
                ["starter id"] = data => data.StarterProfiles[0].ProfileId = "compat.profile.starter.unauthorized",
                ["starter version"] = data => data.StarterProfiles[0].ProfileVersion = 2,
                ["contract target"] = data => data.ContractSelections[0].TargetSchemaVersion = 8,
                ["contract version"] = data => data.ContractSelections[0].CanonicalLayoutContractVersion = 2,
                ["geometry id"] = data => PointStarterAtAdditionalGeometry(data,
                    "compat.geometry.alternate", 1),
                ["geometry version"] = data => PointStarterAtAdditionalGeometry(data,
                    data.GeometryRecords[0].GeometryId, 2),
                ["geometry hash"] = data => PointStarterAtAdditionalGeometry(data,
                    data.GeometryRecords[0].GeometryId, 2)
            };
            foreach (KeyValuePair<string, Action<SpatialLayoutCompatibilityProfilesData>> mutation in mutations)
            {
                SpatialLayoutCompatibilityProfilesData data = ProductionCompatibilityData();
                mutation.Value(data);
                RecomputeProfileHashes(data);
                ProductionSpatialBuildGateResult result = ValidateCompatibility(data);
                Assert.That(result.Reason,
                    Is.EqualTo(ProductionSpatialBuildGateReason.UnauthorizedActiveCompatibilitySelection),
                    mutation.Key + ":" + result.Detail);
            }
        }

        [Test]
        public void InvalidProfileHashAndMissingGeometryReferenceRemainInvalidConfiguration()
        {
            SpatialLayoutCompatibilityProfilesData invalidHash = ProductionCompatibilityData();
            invalidHash.MigrationProfiles[0].CanonicalHash = "invalid";
            Assert.That(ValidateCompatibility(invalidHash).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidCompatibilityProfile));

            SpatialLayoutCompatibilityProfilesData missingGeometry = ProductionCompatibilityData();
            missingGeometry.StarterProfiles[0].GeometryId = "compat.geometry.missing";
            missingGeometry.StarterProfiles[0].CanonicalHash =
                SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(missingGeometry.StarterProfiles[0]);
            Assert.That(ValidateCompatibility(missingGeometry).Reason,
                Is.EqualTo(ProductionSpatialBuildGateReason.InvalidCompatibilityProfile));
        }

        [Test]
        public void ExactActiveReleaseAllowsStructurallyValidRetiredRecoveryRecords()
        {
            SpatialLayoutCompatibilityProfilesData data = ProductionCompatibilityData();
            SpatialMigrationCompatibilityProfile retiredMigration = Clone(data.MigrationProfiles[0]);
            retiredMigration.ProfileId = "compat.profile.migration.retired";
            retiredMigration.Lifecycle = CompatibilityProfileLifecycle.Retired;
            retiredMigration.MinimumSourceSchemaVersion = 8;
            retiredMigration.MaximumSourceSchemaVersion = 8;
            SpatialLayoutCompatibilityProfilesData temporary = new SpatialLayoutCompatibilityProfilesData {
                MigrationProfiles = new[] { retiredMigration }};
            RecomputeProfileHashes(temporary);
            CanonicalStarterLayoutProfile retiredStarter = Clone(data.StarterProfiles[0]);
            retiredStarter.ProfileId = "compat.profile.starter.retired";
            retiredStarter.Lifecycle = CompatibilityProfileLifecycle.Retired;
            retiredStarter.CanonicalHash = SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(retiredStarter);
            data.MigrationProfiles = data.MigrationProfiles.Concat(new[] { retiredMigration }).ToArray();
            data.StarterProfiles = data.StarterProfiles.Concat(new[] { retiredStarter }).ToArray();
            data.ContractSelections = data.ContractSelections.Concat(new[] {
                new CanonicalLayoutContractSelection { TargetSchemaVersion = 8,
                    CanonicalLayoutContractVersion = 1, Lifecycle = CompatibilityProfileLifecycle.Retired }}).ToArray();
            Assert.That(ValidateCompatibility(data).Success, Is.True);
            SpatialLayoutCompatibilityResult parsed = SpatialLayoutCompatibilityProfiles.ParseAndValidate(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(
                    SpatialLayoutCompatibilityProfiles.Canonicalize(data)), ProductionSnapshot(), ParsedLimits());
            Assert.That(parsed.Value.SelectMigration(8, 7, 1).Code, Is.EqualTo("gd66.profile.missing"));
            Assert.That(parsed.Value.SelectStarter(8, 1).Code, Is.EqualTo("gd66.starter_profile.missing"));
            Assert.That(parsed.Value.SelectContract(8).Code,
                Is.EqualTo("gd66.layout_contract.selection_missing"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OneActiveOrInactiveCorrectGameRootPasses(bool inactive)
        {
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
            {
                root.gameObject.SetActive(!inactive);
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(result.Success, Is.True, result.Reason + ":" + result.Detail);
        }

        [Test]
        public void BlankPreviewSceneIsClassifiedAsMissingGameRoot()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            Exception originalException = null;
            try
            {
                Assert.That(ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene).Reason,
                    Is.EqualTo(ProductionSpatialBuildGateReason.MissingGameRoot));
            }
            catch (Exception exception)
            {
                originalException = exception;
                throw;
            }
            finally
            {
                try
                {
                    if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                }
                catch when (originalException != null)
                {
                    // Preserve the original test failure.
                }
            }
        }

        [Test]
        public void DuplicateDiscoveredGameRootCountIsClassified()
        {
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
                ProductionSpatialContentBuildGate.ValidateGameRootComposition(new[] { root, root }));
            Assert.That(result.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.DuplicateGameRoot));
        }

        [TestCase("manifest")]
        [TestCase("catalog")]
        [TestCase("languages-null")]
        [TestCase("languages-empty")]
        [TestCase("language-null-entry")]
        [TestCase("limits")]
        [TestCase("compatibility")]
        public void MissingAssignmentsAreClassified(string field)
        {
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
            {
                if (field == "manifest") root.productionSpatialManifest = null;
                if (field == "catalog") root.productionSpatialCatalog = null;
                if (field == "languages-null") root.productionSpatialLanguageTables = null;
                if (field == "languages-empty") root.productionSpatialLanguageTables = Array.Empty<TextAsset>();
                if (field == "language-null-entry") root.productionSpatialLanguageTables = new TextAsset[] { null };
                if (field == "limits") root.productionSpatialValidationLimits = null;
                if (field == "compatibility") root.spatialLayoutCompatibilityProfilesJson = null;
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(result.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.MissingAssignment));
        }

        [TestCase("manifest")]
        [TestCase("catalog")]
        [TestCase("english")]
        [TestCase("limits")]
        [TestCase("compatibility")]
        public void ByteIdenticalCopiedRequiredAssignmentsAreRejected(string field)
        {
            string source = field == "manifest" ? ProductionSpatialGeneratedSetParser.ManifestPath :
                field == "catalog" ? ProductionSpatialGeneratedSetParser.CatalogPath :
                field == "english" ? ProductionSpatialGeneratedSetParser.EnglishPath :
                field == "limits" ? ProductionSpatialContentPublicationService.LimitsPath :
                SpatialLayoutCompatibilityProfiles.ProductionPath;
            string copy = TestRoot + "/copied-" + field + ".json";
            Assert.That(AssetDatabase.CopyAsset(source, copy), Is.True);
            TextAsset copied = Load(copy);
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
            {
                if (field == "manifest") root.productionSpatialManifest = copied;
                if (field == "catalog") root.productionSpatialCatalog = copied;
                if (field == "english") root.productionSpatialLanguageTables = new[] { copied };
                if (field == "limits") root.productionSpatialValidationLimits = copied;
                if (field == "compatibility") root.spatialLayoutCompatibilityProfilesJson = copied;
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(result.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.WrongAssetAssignment));
        }

        [Test]
        public void DuplicateLanguagePathIsRejected()
        {
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
            {
                root.productionSpatialLanguageTables = new[] { english, english };
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(result.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.WrongAssetAssignment));
        }

        [Test]
        public void AdditionalLanguagesUseCompleteLoaderValidation()
        {
            TextAsset valid = CreateAdditionalLanguage("zz", true);
            ProductionSpatialBuildGateResult validResult = WithCanonicalBootstrapPreview((scene, root) =>
            {
                root.productionSpatialLanguageTables = new[] { english, valid };
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(validResult.Success, Is.True, validResult.Reason + ":" + validResult.Detail);

            TextAsset invalid = CreateAdditionalLanguage("bad", false);
            ProductionSpatialBuildGateResult invalidResult = WithCanonicalBootstrapPreview((scene, root) =>
            {
                root.productionSpatialLanguageTables = new[] { english, invalid };
                return ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene);
            });
            Assert.That(invalidResult.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.LocalizationFailure));
        }

        [Test]
        public void PreviewOpenFailureAndPostOpenFailureHaveAccurateReasonsAndCleanup()
        {
            string path = DevelopmentBuildUtility.BootstrapScenePath;
            bool wasLoaded = SceneManager.GetSceneByPath(path).isLoaded;
            ProductionSpatialBuildGateResult open = ProductionSpatialContentBuildGate.ValidateBootstrapComposition(
                path, null, _ => throw new IOException("test"));
            Assert.That(open.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.BootstrapSceneUnavailable));

            var gate = Gate(composition: scenes => ProductionSpatialContentBuildGate.ValidateBootstrapComposition(
                path, () => throw new InvalidOperationException("test")));
            ProductionSpatialBuildGateResult unexpected = gate.Validate(BootstrapOnly());
            Assert.That(unexpected.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.UnexpectedInternalValidationFailure));
            Assert.That(SceneManager.GetSceneByPath(path).isLoaded, Is.EqualTo(wasLoaded));
        }

        [Test]
        public void PreviewInspectionDoesNotDirtyOrSaveSourceScene()
        {
            string path = DevelopmentBuildUtility.BootstrapScenePath;
            bool wasLoaded = SceneManager.GetSceneByPath(path).isLoaded;
            byte[] before = File.ReadAllBytes(path);
            Assert.That(ProductionSpatialContentBuildGate.ValidateBootstrapComposition(path).Success, Is.True);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            Assert.That(SceneManager.GetSceneByPath(path).isLoaded, Is.EqualTo(wasLoaded));
        }

        [Test]
        public void PreviewFixturePreservesNormalEditorSceneSetup()
        {
            Scene activeBefore = SceneManager.GetActiveScene();
            SceneSetup[] setupBefore = EditorSceneManager.GetSceneManagerSetup();
            bool dirtyBefore = activeBefore.IsValid() && activeBefore.isDirty;
            ProductionSpatialBuildGateResult result = WithCanonicalBootstrapPreview((scene, root) =>
                ProductionSpatialContentBuildGate.ValidateOpenSceneComposition(scene));
            Assert.That(result.Success, Is.True, result.Reason + ":" + result.Detail);
            Scene activeAfter = SceneManager.GetActiveScene();
            SceneSetup[] setupAfter = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(activeAfter.handle, Is.EqualTo(activeBefore.handle));
            Assert.That(activeAfter.path, Is.EqualTo(activeBefore.path));
            Assert.That(activeAfter.IsValid() && activeAfter.isDirty, Is.EqualTo(dirtyBefore));
            Assert.That(setupAfter.Length, Is.EqualTo(setupBefore.Length));
            for (int index = 0; index < setupBefore.Length; index++)
            {
                Assert.That(setupAfter[index].path, Is.EqualTo(setupBefore[index].path));
                Assert.That(setupAfter[index].isLoaded, Is.EqualTo(setupBefore[index].isLoaded));
                Assert.That(setupAfter[index].isActive, Is.EqualTo(setupBefore[index].isActive));
            }
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

        private T WithCanonicalBootstrapPreview<T>(Func<Scene, GameRoot, T> action)
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(DevelopmentBuildUtility.BootstrapScenePath);
            Exception originalException = null;
            try
            {
                Assert.That(EditorSceneManager.IsPreviewScene(scene), Is.True);
                GameRoot[] roots = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameRoot>(true)).ToArray();
                Assert.That(roots.Length, Is.EqualTo(1),
                    "Canonical Bootstrap preview fixture must contain exactly one GameRoot.");
                return action(scene, roots[0]);
            }
            catch (Exception exception)
            {
                originalException = exception;
                throw;
            }
            finally
            {
                try
                {
                    if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                }
                catch when (originalException != null)
                {
                    // Preserve the fixture's original failure instead of replacing it with cleanup failure.
                }
            }
        }

        private SpatialLayoutCompatibilityProfilesData ProductionCompatibilityData() =>
            JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(
                Load(SpatialLayoutCompatibilityProfiles.ProductionPath).text);

        private ProductionSpatialBuildGateResult ValidateCompatibility(SpatialLayoutCompatibilityProfilesData data)
        {
            TextAsset asset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(
                    SpatialLayoutCompatibilityProfiles.Canonicalize(data))));
            return ProductionSpatialContentBuildGate.ValidateCompatibility(asset, manifest, catalog,
                new[] { english }, limits);
        }

        private ProductionSpatialContentSnapshot ProductionSnapshot() =>
            ProductionSpatialContentLoader.Load(manifest, catalog, new[] { english }, limits).Value;

        private SpatialContentValidationWorkloadLimits ParsedLimits() =>
            ProductionSpatialContentWorkloadLimitParser.Parse(limits).Limits;

        private static void RecomputeProfileHashes(SpatialLayoutCompatibilityProfilesData data)
        {
            foreach (SpatialMigrationCompatibilityProfile profile in data.MigrationProfiles ??
                Array.Empty<SpatialMigrationCompatibilityProfile>())
                profile.CanonicalHash = SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile);
            foreach (CanonicalStarterLayoutProfile profile in data.StarterProfiles ??
                Array.Empty<CanonicalStarterLayoutProfile>())
                profile.CanonicalHash = SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(profile);
        }

        private static void AddActiveMigration(SpatialLayoutCompatibilityProfilesData data)
        {
            SpatialMigrationCompatibilityProfile profile = Clone(data.MigrationProfiles[0]);
            profile.ProfileId = "compat.profile.migration.additional";
            profile.MinimumSourceSchemaVersion = 8;
            profile.MaximumSourceSchemaVersion = 8;
            data.MigrationProfiles = data.MigrationProfiles.Concat(new[] { profile }).ToArray();
        }

        private static void AddActiveStarter(SpatialLayoutCompatibilityProfilesData data)
        {
            CanonicalStarterLayoutProfile profile = Clone(data.StarterProfiles[0]);
            profile.ProfileId = "compat.profile.starter.additional";
            profile.TargetSchemaVersion = 8;
            data.StarterProfiles = data.StarterProfiles.Concat(new[] { profile }).ToArray();
        }

        private static void PointStarterAtAdditionalGeometry(SpatialLayoutCompatibilityProfilesData data,
            string geometryId, int geometryVersion)
        {
            CompatibilityLayoutGeometryRecord geometry = Clone(data.GeometryRecords[0]);
            geometry.GeometryId = geometryId;
            geometry.GeometryVersion = geometryVersion;
            geometry.CanonicalHash = SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(geometry);
            data.GeometryRecords = data.GeometryRecords.Concat(new[] { geometry }).ToArray();
            data.StarterProfiles[0].GeometryId = geometry.GeometryId;
            data.StarterProfiles[0].GeometryVersion = geometry.GeometryVersion;
            data.StarterProfiles[0].GeometryCanonicalHash = geometry.CanonicalHash;
        }

        private static T Clone<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));

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
