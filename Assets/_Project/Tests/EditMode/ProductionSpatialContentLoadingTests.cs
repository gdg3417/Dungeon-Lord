#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class ProductionSpatialContentLoadingTests
    {
        private const string Root = "Assets/_Project/Data/Production/DungeonSpatial/";
        private TextAsset manifest;
        private TextAsset catalog;
        private TextAsset english;
        private TextAsset limits;

        [SetUp]
        public void SetUp()
        {
            manifest = Asset("content_manifest.json");
            catalog = Asset("dungeon_spatial_content.json");
            english = Asset("string_table_en.json");
            limits = Asset("validation_limits.json");
        }

        [Test]
        public void CommittedProductionSet_LoadsDeterministicallyAndRetainsManifest()
        {
            var service = new ContentService();
            ProductionSpatialContentLoadResult first = Load(service);
            ProductionSpatialContentLoadResult second = Load(service);

            Assert.That(first.Success, Is.True, Join(first));
            Assert.That(second.Success, Is.True, Join(second));
            Assert.That(first.Value.Manifest.contentVersion, Is.EqualTo("0.1.0"));
            CollectionAssert.AreEqual(new[] { "dungeon_spatial_content", "string_table" },
                first.Value.Manifest.requiredSchemas.Select(value => value.schemaId));
            Assert.That(first.Value.Languages, Has.Count.EqualTo(1));
            Assert.That(first.Value.Languages[0].language, Is.EqualTo("en"));
            Assert.That(JsonUtility.ToJson(first.Value.Catalog),
                Is.EqualTo(JsonUtility.ToJson(second.Value.Catalog)));
        }

        [Test]
        public void SyntheticLanguage_PermutationPublishesOrdinalDetachedCollection()
        {
            StringTable synthetic = JsonUtility.FromJson<StringTable>(english.text);
            synthetic.language = "test-language";
            foreach (StringEntry entry in synthetic.entries) entry.text = "test:" + entry.key;
            var syntheticAsset = new TextAsset(System.Text.Encoding.UTF8.GetString(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(synthetic)));
            var service = new ContentService();

            ProductionSpatialContentLoadResult result = service.LoadProductionSpatialContent(
                manifest, catalog, new[] { syntheticAsset, english }, limits);
            Assert.That(result.Success, Is.True, Join(result));
            CollectionAssert.AreEqual(new[] { "en", "test-language" },
                service.ProductionSpatialContent.Languages.Select(value => value.language));

            StringTable callerCopy = result.Value.Languages[0];
            callerCopy.language = "mutated";
            callerCopy.entries[0].text = "mutated";
            Assert.That(service.ProductionSpatialContent.Languages[0].language, Is.EqualTo("en"));
            Assert.That(service.ProductionSpatialContent.Languages[0].entries[0].text, Is.Not.EqualTo("mutated"));
        }

        [Test]
        public void BothLanguagePermutationsProduceEquivalentCanonicalPublicationWithoutMutatingInputs()
        {
            TextAsset additional = Language("test-language", null);
            TextAsset[] forward = { english, additional };
            TextAsset[] reverse = { additional, english };
            byte[][] before = forward.Select(asset => (byte[])asset.bytes.Clone()).ToArray();
            byte[] manifestBefore = (byte[])manifest.bytes.Clone();
            byte[] catalogBefore = (byte[])catalog.bytes.Clone();
            byte[] limitsBefore = (byte[])limits.bytes.Clone();

            ProductionSpatialContentLoadResult first = LoadWith(manifest, catalog, forward);
            ProductionSpatialContentLoadResult second = LoadWith(manifest, catalog, reverse);

            Assert.That(first.Success, Is.True, Join(first));
            Assert.That(second.Success, Is.True, Join(second));
            Assert.That(Canonical(first.Value), Is.EqualTo(Canonical(second.Value)));
            CollectionAssert.AreEqual(new[] { english, additional }, forward);
            CollectionAssert.AreEqual(new[] { additional, english }, reverse);
            CollectionAssert.AreEqual(before[0], english.bytes);
            CollectionAssert.AreEqual(before[1], additional.bytes);
            CollectionAssert.AreEqual(manifestBefore, manifest.bytes);
            CollectionAssert.AreEqual(catalogBefore, catalog.bytes);
            CollectionAssert.AreEqual(limitsBefore, limits.bytes);
        }

        [Test]
        public void CompleteLoadUsesOneExactNestedRecordBoundary()
        {
            TextAsset additional = Language("test-language", null);
            Assert.That(LoadWithLimits(new[] { english }, Limits(41, 32768)).Success, Is.True);
            AssertWorkloadFailure(new[] { english, additional }, Limits(41, 32768));
            Assert.That(LoadWithLimits(new[] { english, additional }, Limits(47, 32768)).Success, Is.True);
            AssertWorkloadFailure(new[] { english, additional }, Limits(46, 32768));
        }

        [Test]
        public void CompleteLoadUsesOneExactDerivedStringCharacterBoundary()
        {
            TextAsset additional = Language("test-language", null);
            TextAsset[] languages = { english, additional };
            int exact = FindMinimumStringLimit(languages, 47);

            Assert.That(LoadWithLimits(languages, Limits(47, exact)).Success, Is.True);
            AssertWorkloadFailure(languages, Limits(47, exact - 1));

            TextAsset oneMoreCharacter = Language("test-language", table => table.entries[0].text += "x");
            AssertWorkloadFailure(new[] { english, oneMoreCharacter }, Limits(47, exact));
        }

        [Test]
        public void SharedStrictBudgetRejectsMultipleTablesAndStopsBeforeLaterMalformedInput()
        {
            TextAsset first = Language("test-a", null);
            TextAsset second = Language("test-b", null);
            TextAsset third = Language("test-c", null);
            TextAsset constrained = Limits(47, 32768);

            ProductionSpatialContentLoadResult forward = LoadWithLimits(
                new[] { english, first, second, third }, constrained);
            ProductionSpatialContentLoadResult reverse = LoadWithLimits(
                new[] { third, second, first, english }, constrained);
            CollectionAssert.AreEqual(new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                forward.Diagnostics);
            CollectionAssert.AreEqual(forward.Diagnostics, reverse.Diagnostics);

            int exactEnglishCharacters = FindMinimumStringLimit(new[] { english }, 41);
            TextAsset oversized = Language("test-large", table => table.entries[0].text +=
                new string('x', exactEnglishCharacters));
            TextAsset malformed = new TextAsset("{\n");
            ProductionSpatialContentLoadResult stopped = LoadWithLimits(
                new[] { english, oversized, malformed }, Limits(128, exactEnglishCharacters));
            CollectionAssert.AreEqual(new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                stopped.Diagnostics);
            CollectionAssert.AreEqual(stopped.Diagnostics, LoadWithLimits(
                new[] { english, oversized, malformed }, Limits(128, exactEnglishCharacters)).Diagnostics);
        }

        [Test]
        public void MixedLanguageFailuresReturnIdenticalDiagnosticsForEveryPermutation()
        {
            int exactEnglishCharacters = FindMinimumStringLimit(new[] { english }, 41);
            TextAsset constrained = Limits(128, exactEnglishCharacters);
            TextAsset oversized = Language("test-oversized", table => table.entries[0].text +=
                new string('x', exactEnglishCharacters));
            TextAsset malformed = new TextAsset("{\n");
            TextAsset additional = Language("test-noncanonical", null);
            TextAsset noncanonical = new TextAsset(additional.text.TrimEnd());

            AssertEveryPermutationMatches(new[] { english, oversized, malformed }, constrained);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { english, oversized, malformed }, constrained).Diagnostics);
            AssertEveryPermutationMatches(
                new[] { english, oversized, malformed, noncanonical }, constrained);
            AssertEveryPermutationMatches(
                new[] { english, malformed, new TextAsset("{}\n"), noncanonical }, limits);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable },
                LoadWithLimits(new[] { english, malformed, new TextAsset("{}\n"), noncanonical },
                    limits).Diagnostics);
        }

        [Test]
        public void CumulativeDiagnosticLimitIsBoundedAndOrderIndependent()
        {
            TextAsset[] invalid = Enumerable.Range(0, 12)
                .Select(index => new TextAsset(index % 2 == 0 ? "{\n" : "{}\n"))
                .ToArray();
            TextAsset constrained = Limits(512, 32768, 3);

            ProductionSpatialContentLoadResult forward = LoadWithLimits(invalid, constrained);
            TextAsset[] reverse = (TextAsset[])invalid.Clone();
            Array.Reverse(reverse);
            ProductionSpatialContentLoadResult reversed = LoadWithLimits(reverse, constrained);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                forward.Diagnostics);
            CollectionAssert.AreEqual(forward.Diagnostics, reversed.Diagnostics);
            CollectionAssert.AreEqual(forward.Diagnostics,
                LoadWithLimits((TextAsset[])invalid.Clone(), constrained).Diagnostics);
        }

        [Test]
        public void RepeatedDiagnosticAttemptsConsumeExactCumulativeIssueCapacity()
        {
            TextAsset repeated = RepeatedMissingRequiredFieldLanguage();
            TextAsset malformed = new TextAsset("{\n");

            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { repeated }, Limits(512, 32768, 3)).Diagnostics);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable },
                LoadWithLimits(new[] { repeated }, Limits(512, 32768, 4)).Diagnostics);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable },
                LoadWithLimits(new[] { repeated, malformed }, Limits(512, 32768, 5)).Diagnostics);

            TextAsset[] source = { english, repeated, malformed };
            byte[][] before = source.Select(asset => (byte[])asset.bytes.Clone()).ToArray();
            ProductionSpatialContentLoadingDiagnostic[] expected =
                { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded };
            foreach (TextAsset[] permutation in Permutations(source))
            {
                var service = new ContentService();
                Assert.That(Load(service).Success, Is.True);
                ProductionSpatialContentSnapshot published = service.ProductionSpatialContent;
                ProductionSpatialContentLoadResult result = service.LoadProductionSpatialContent(
                    manifest, catalog, permutation, Limits(512, 32768, 4));
                CollectionAssert.AreEqual(expected, result.Diagnostics);
                Assert.That(result.Value, Is.Null);
                Assert.That(service.ProductionSpatialContent, Is.SameAs(published));
            }

            for (int index = 0; index < source.Length; index++)
                CollectionAssert.AreEqual(before[index], source[index].bytes);
        }

        [Test]
        public void NoncanonicalDiagnosticsConsumeTheCumulativeIssueCapacity()
        {
            TextAsset first = NoncanonicalLanguage("test-noncanonical-a");
            TextAsset second = NoncanonicalLanguage("test-noncanonical-b");
            TextAsset malformed = new TextAsset("{\n");

            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable },
                LoadWithLimits(new[] { first }, Limits(512, 32768, 1)).Diagnostics);
            AssertEveryPermutationMatches(new[] { first, second }, Limits(512, 32768, 1));
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { first, second }, Limits(512, 32768, 1)).Diagnostics);

            TextAsset[] mixed = { english, first, malformed };
            AssertEveryPermutationMatches(mixed, Limits(512, 32768, 2));
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable },
                LoadWithLimits(mixed, Limits(512, 32768, 2)).Diagnostics);
            AssertEveryPermutationMatches(mixed, Limits(512, 32768, 1));
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(mixed, Limits(512, 32768, 1)).Diagnostics);
        }

        [Test]
        public void BlankLanguageAndMissingEnglishConsumeTheCumulativeIssueCapacity()
        {
            TextAsset blankA = Language("", null);
            TextAsset blankB = Language("", table => table.entries[0].text += "x");
            TextAsset duplicateA = Language("test-duplicate-budget", null);
            TextAsset duplicateB = Language("test-duplicate-budget", table =>
                table.entries[0].text += "x");

            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.BlankLanguageIdentity },
                LoadWithLimits(new[] { english, blankA }, Limits(512, 32768, 1)).Diagnostics);
            AssertEveryPermutationMatches(
                new[] { english, blankA, blankB }, Limits(512, 32768, 1));
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { english, blankA, blankB }, Limits(512, 32768, 1)).Diagnostics);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { blankA }, Limits(512, 32768, 1)).Diagnostics);
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.DuplicateLanguageIdentity },
                LoadWithLimits(new[] { english, duplicateA, duplicateB },
                    Limits(512, 32768, 1)).Diagnostics);
            AssertEveryPermutationMatches(
                new[] { duplicateA, duplicateB }, Limits(512, 32768, 1));
            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                LoadWithLimits(new[] { duplicateA, duplicateB },
                    Limits(512, 32768, 1)).Diagnostics);
        }

        [Test]
        public void LocalizationCoverageDiagnosticsConsumeTheCumulativeIssueCapacity()
        {
            TextAsset invalidA = Language("test-coverage-a", table =>
                table.entries[0].key = "test.coverage.extra.a");
            TextAsset invalidB = Language("test-coverage-b", table =>
                table.entries[0].key = "test.coverage.extra.b");

            CollectionAssert.AreEqual(
                new[] { ProductionSpatialContentLoadingDiagnostic.LocalizationCoverageInvalid },
                LoadWithLimits(new[] { english, invalidA }, Limits(512, 32768, 1)).Diagnostics);
            ProductionSpatialContentLoadingDiagnostic[] expected =
                { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded };
            CollectionAssert.AreEqual(expected,
                LoadWithLimits(new[] { english, invalidA, invalidB }, Limits(512, 32768, 1)).Diagnostics);
            CollectionAssert.AreEqual(expected,
                LoadWithLimits(new[] { invalidB, english, invalidA }, Limits(512, 32768, 1)).Diagnostics);
        }

        [Test]
        public void DuplicateAndMissingEnglishDiagnosticsArePermutationIndependent()
        {
            TextAsset duplicate = Language("test-duplicate", null);
            TextAsset missingA = Language("test-a", null);
            TextAsset missingB = Language("test-b", null);

            AssertEveryPermutationMatches(new[] { english, duplicate, duplicate }, limits);
            AssertEveryPermutationMatches(new[] { missingA, missingB }, limits);
        }

        [Test]
        public void EveryFailedPermutationPreservesPriorPublicationAndSources()
        {
            int exactEnglishCharacters = FindMinimumStringLimit(new[] { english }, 41);
            TextAsset oversized = Language("test-oversized", table => table.entries[0].text +=
                new string('x', exactEnglishCharacters));
            TextAsset malformed = new TextAsset("{\n");
            TextAsset[] source = { english, oversized, malformed };
            byte[][] before = source.Select(asset => (byte[])asset.bytes.Clone()).ToArray();
            ProductionSpatialContentLoadingDiagnostic[] expected = null;

            foreach (TextAsset[] permutation in Permutations(source))
            {
                var service = new ContentService();
                Assert.That(Load(service).Success, Is.True);
                ProductionSpatialContentSnapshot published = service.ProductionSpatialContent;
                ProductionSpatialContentLoadResult result = service.LoadProductionSpatialContent(
                    manifest, catalog, permutation, Limits(128, exactEnglishCharacters));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Value, Is.Null);
                Assert.That(service.ProductionSpatialContent, Is.SameAs(published));
                if (expected == null) expected = result.Diagnostics;
                else CollectionAssert.AreEqual(expected, result.Diagnostics);
            }

            for (int index = 0; index < source.Length; index++)
                CollectionAssert.AreEqual(before[index], source[index].bytes);
        }

        [Test]
        public void CumulativeFailureNeverPartiallyPublishesAndPreservesExactPriorSnapshot()
        {
            var service = new ContentService();
            Assert.That(Load(service).Success, Is.True);
            ProductionSpatialContentSnapshot before = service.ProductionSpatialContent;
            TextAsset additional = Language("test-language", null);

            ProductionSpatialContentLoadResult failure = service.LoadProductionSpatialContent(
                manifest, catalog, new[] { english, additional }, Limits(41, 32768));

            CollectionAssert.AreEqual(new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                failure.Diagnostics);
            Assert.That(service.ProductionSpatialContent, Is.SameAs(before));
        }

        [TestCase("manifest")]
        [TestCase("catalog")]
        [TestCase("limits")]
        [TestCase("languages")]
        [TestCase("emptyLanguages")]
        [TestCase("nullLanguage")]
        public void MissingInputs_FailWithoutThrowOrPublication(string missing)
        {
            var service = new ContentService();
            TextAsset[] languages = missing == "languages" ? null :
                missing == "emptyLanguages" ? Array.Empty<TextAsset>() :
                missing == "nullLanguage" ? new TextAsset[] { null } : new[] { english };
            Assert.DoesNotThrow(() => service.LoadProductionSpatialContent(
                missing == "manifest" ? null : manifest,
                missing == "catalog" ? null : catalog,
                languages,
                missing == "limits" ? null : limits));
            Assert.That(service.ProductionSpatialContent, Is.Null);
        }

        [Test]
        public void MissingAndDuplicateEnglishAndDuplicateAdditionalLanguage_FailClosed()
        {
            TextAsset test = Language("test-language", null);
            AssertFailure(new[] { test }, ProductionSpatialContentLoadingDiagnostic.MissingEnglish);
            AssertFailure(new[] { english, english }, ProductionSpatialContentLoadingDiagnostic.DuplicateLanguageIdentity);
            AssertFailure(new[] { english, test, test }, ProductionSpatialContentLoadingDiagnostic.DuplicateLanguageIdentity);
        }

        [TestCase("{\n")]
        [TestCase("{}\n")]
        [TestCase(" { }\n")]
        public void MalformedOrNoncanonicalLanguage_Fails(string json)
        {
            AssertFailure(new[] { english, new TextAsset(json) },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
        }

        [Test]
        public void InvalidLimitsAndMalformedBaseInputs_FailClosed()
        {
            var service = new ContentService();
            Assert.That(service.LoadProductionSpatialContent(manifest, catalog, new[] { english },
                new TextAsset("{}" )).Success, Is.False);
            Assert.That(service.LoadProductionSpatialContent(new TextAsset("{}\n"), catalog,
                new[] { english }, limits).Success, Is.False);
            Assert.That(service.LoadProductionSpatialContent(manifest, new TextAsset("{}\n"),
                new[] { english }, limits).Success, Is.False);
            Assert.That(service.ProductionSpatialContent, Is.Null);
        }

        [Test]
        public void CanonicalIdentityVersionRegistrationAndLocalizationDefectsFailClosed()
        {
            ProductionSpatialContentManifest wrongVersion =
                JsonUtility.FromJson<ProductionSpatialContentManifest>(manifest.text);
            wrongVersion.contentVersion = "test.mismatch";
            Assert.That(LoadWith(Asset(wrongVersion), catalog, new[] { english }).Success, Is.False);

            ProductionSpatialContentManifest wrongRegistration =
                JsonUtility.FromJson<ProductionSpatialContentManifest>(manifest.text);
            wrongRegistration.requiredSchemas[0].schemaVersion++;
            Assert.That(LoadWith(Asset(wrongRegistration), catalog, new[] { english }).Success, Is.False);

            SpatialContentCatalog wrongCatalog = JsonUtility.FromJson<SpatialContentCatalog>(catalog.text);
            wrongCatalog.Metadata.SchemaId = "test.wrong";
            Assert.That(LoadWith(manifest, Asset(wrongCatalog), new[] { english }).Success, Is.False);

            AssertFailure(new[] { english, Language("test-language", table =>
                table.entries[1].key = table.entries[0].key) },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
            AssertFailure(new[] { Language("", null) },
                ProductionSpatialContentLoadingDiagnostic.BlankLanguageIdentity);
        }

        [Test]
        public void CoverageSchemaBlankAndNoncanonicalAdditionalLanguage_Fail()
        {
            AssertFailure(new[] { english, Language("test-language", table =>
                table.entries = table.entries.Skip(1).ToArray()) },
                ProductionSpatialContentLoadingDiagnostic.LocalizationCoverageInvalid);
            AssertFailure(new[] { english, Language("test-language", table =>
                table.entries[0].key = "test.extra") },
                ProductionSpatialContentLoadingDiagnostic.LocalizationCoverageInvalid);
            AssertFailure(new[] { english, Language("test-language", table =>
                table.entries[0].text = "") },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
            AssertFailure(new[] { english, Language("test-language", table =>
                table.schema = "wrong") },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
            AssertFailure(new[] { english, new TextAsset(english.text.TrimEnd()) },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
        }

        [Test]
        public void EachGeneratedInputRejectsNoncanonicalBytes()
        {
            Assert.That(LoadWith(new TextAsset(manifest.text.TrimEnd()), catalog, new[] { english }).Success, Is.False);
            Assert.That(LoadWith(manifest, new TextAsset(catalog.text.TrimEnd()), new[] { english }).Success, Is.False);
            AssertFailure(new[] { new TextAsset(english.text.TrimEnd()) },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
            TextAsset additional = Language("test-language", null);
            AssertFailure(new[] { english, new TextAsset(additional.text.TrimEnd()) },
                ProductionSpatialContentLoadingDiagnostic.InvalidLanguageTable);
        }

        [Test]
        public void EveryPublishedGraphIsDetachedAndSaveAuthorityRemainsUnchanged()
        {
            ProductionSpatialContentSnapshot snapshot = LoadWith(
                manifest, catalog, new[] { english }).Value;
            ProductionSpatialContentManifest publishedManifest = snapshot.Manifest;
            SpatialContentCatalog publishedCatalog = snapshot.Catalog;
            IReadOnlyList<StringTable> publishedLanguages = snapshot.Languages;
            publishedManifest.requiredSchemas[0].schemaId = "mutated";
            publishedCatalog.Rooms[0].LocalizationKey = "mutated";
            publishedCatalog.Rooms[0].ConnectionPoints[0].ConnectionPointId = "mutated";
            publishedLanguages[0].entries[0].key = "mutated";

            Assert.That(snapshot.Manifest.requiredSchemas[0].schemaId, Is.Not.EqualTo("mutated"));
            Assert.That(snapshot.Catalog.Rooms[0].LocalizationKey, Is.Not.EqualTo("mutated"));
            Assert.That(snapshot.Catalog.Rooms[0].ConnectionPoints[0].ConnectionPointId,
                Is.Not.EqualTo("mutated"));
            Assert.That(snapshot.Languages[0].entries[0].key, Is.Not.EqualTo("mutated"));
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(6));
            Assert.That(typeof(SaveData).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Any(field => field.Name.IndexOf("spatial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.FieldType == typeof(ProductionSpatialContentSnapshot)), Is.False);
        }

        [Test]
        public void ProductionPublicationHasNoGameplayPresenterUiPlacementSimulationOrRouteConsumer()
        {
            string[] consumers = Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\.ProductionSpatialContent\b"))
                .ToArray();
            CollectionAssert.IsEmpty(consumers);
        }

        [Test]
        public void FailurePreservesPriorPublicationAndDiagnosticsAreStableOrderedAndMirrored()
        {
            var service = new ContentService();
            Assert.That(Load(service).Success, Is.True);
            ProductionSpatialContentSnapshot before = service.ProductionSpatialContent;
            ProductionSpatialContentLoadingDiagnostic[] sink = null;
            var received = new System.Collections.Generic.List<ProductionSpatialContentLoadingDiagnostic>();
            ProductionSpatialContentLoadResult first = service.LoadProductionSpatialContent(
                null, null, null, null, received.Add);
            ProductionSpatialContentLoadResult repeated = service.LoadProductionSpatialContent(
                null, null, null, null);
            sink = received.ToArray();

            Assert.That(service.ProductionSpatialContent, Is.SameAs(before));
            CollectionAssert.AreEqual(first.Diagnostics.OrderBy(value => (int)value), first.Diagnostics);
            CollectionAssert.AreEqual(first.Diagnostics, repeated.Diagnostics);
            CollectionAssert.AreEqual(first.Diagnostics, sink);
        }

        [Test]
        public void BootstrapSceneContainsOnlyExplicitProductionAssignments()
        {
            string scene = File.ReadAllText("Assets/_Project/Scenes/Bootstrap.unity")
                .Replace("\r\n", "\n").Replace("\r", "\n");
            StringAssert.Contains("productionSpatialManifest: {fileID: 4900000, guid: 65b3b00000000000000000000000000c", scene);
            StringAssert.Contains("productionSpatialCatalog: {fileID: 4900000, guid: 65b3b00000000000000000000000000d", scene);
            StringAssert.Contains("productionSpatialLanguageTables:\n  - {fileID: 4900000, guid: 65b3b00000000000000000000000000e", scene);
            Assert.That(scene.Split(new[] { "productionSpatialLanguageTables:" }, StringSplitOptions.None),
                Has.Length.EqualTo(2));
            StringAssert.Contains("productionSpatialValidationLimits: {fileID: 4900000, guid: 10fce78ef6ec499d93fdfc87c97030d6", scene);
            string source = System.IO.File.ReadAllText("Assets/_Project/Scripts/Core/GameRoot.cs");
            string fallback = source.Substring(source.IndexOf("private void EnsureContentAssetsAssigned", StringComparison.Ordinal),
                source.IndexOf("public void InitializeServicesAndData", StringComparison.Ordinal) -
                source.IndexOf("private void EnsureContentAssetsAssigned", StringComparison.Ordinal));
            StringAssert.DoesNotContain("productionSpatial", fallback);
            string initialization = source.Substring(
                source.IndexOf("public void InitializeServicesAndData", StringComparison.Ordinal),
                source.IndexOf("private void InitializeStructureSimulationPass", StringComparison.Ordinal) -
                source.IndexOf("public void InitializeServicesAndData", StringComparison.Ordinal));
            StringAssert.DoesNotContain("diagnostic.ToString", initialization);
            StringAssert.DoesNotContain("Logger?.Warn", initialization);
        }

        private ProductionSpatialContentLoadResult Load(ContentService service) =>
            service.LoadProductionSpatialContent(manifest, catalog, new[] { english }, limits);

        private ProductionSpatialContentLoadResult LoadWith(TextAsset suppliedManifest,
            TextAsset suppliedCatalog, TextAsset[] languages) =>
            new ContentService().LoadProductionSpatialContent(
                suppliedManifest, suppliedCatalog, languages, limits);

        private ProductionSpatialContentLoadResult LoadWithLimits(TextAsset[] languages,
            TextAsset suppliedLimits) => new ContentService().LoadProductionSpatialContent(
                manifest, catalog, languages, suppliedLimits);

        private void AssertWorkloadFailure(TextAsset[] languages, TextAsset suppliedLimits)
        {
            ProductionSpatialContentLoadResult result = LoadWithLimits(languages, suppliedLimits);
            CollectionAssert.AreEqual(new[] { ProductionSpatialContentLoadingDiagnostic.WorkloadExceeded },
                result.Diagnostics);
            Assert.That(result.Value, Is.Null);
        }

        private void AssertEveryPermutationMatches(TextAsset[] source, TextAsset suppliedLimits)
        {
            byte[][] before = source.Select(asset => (byte[])asset.bytes.Clone()).ToArray();
            ProductionSpatialContentLoadingDiagnostic[] expected = null;
            int executions = 0;
            foreach (TextAsset[] permutation in Permutations(source))
            {
                ProductionSpatialContentLoadResult result = LoadWithLimits(permutation, suppliedLimits);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Value, Is.Null);
                if (expected == null) expected = result.Diagnostics;
                else CollectionAssert.AreEqual(expected, result.Diagnostics);
                executions++;
            }

            Assert.That(executions, Is.GreaterThan(1));
            CollectionAssert.AreEqual(expected,
                LoadWithLimits((TextAsset[])source.Clone(), suppliedLimits).Diagnostics);
            for (int index = 0; index < source.Length; index++)
                CollectionAssert.AreEqual(before[index], source[index].bytes);
        }

        private static IEnumerable<TextAsset[]> Permutations(TextAsset[] source)
        {
            var working = (TextAsset[])source.Clone();
            foreach (TextAsset[] permutation in Permutations(working, 0)) yield return permutation;
        }

        private static IEnumerable<TextAsset[]> Permutations(TextAsset[] working, int index)
        {
            if (index == working.Length)
            {
                yield return (TextAsset[])working.Clone();
                yield break;
            }

            for (int swapIndex = index; swapIndex < working.Length; swapIndex++)
            {
                TextAsset temporary = working[index];
                working[index] = working[swapIndex];
                working[swapIndex] = temporary;
                foreach (TextAsset[] permutation in Permutations(working, index + 1))
                    yield return permutation;
                temporary = working[index];
                working[index] = working[swapIndex];
                working[swapIndex] = temporary;
            }
        }

        private int FindMinimumStringLimit(TextAsset[] languages, int nested)
        {
            int low = 1;
            int high = 32768;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (LoadWithLimits(languages, Limits(nested, middle)).Success) high = middle;
                else low = middle + 1;
            }
            Assert.That(LoadWithLimits(languages, Limits(nested, low)).Success, Is.True);
            return low;
        }

        private static TextAsset Limits(int nested, int characters, int issues = 256) => new TextAsset(
            "{\n" +
            "  \"MaximumTopLevelRecords\": 128,\n" +
            "  \"MaximumNestedRecords\": " + nested + ",\n" +
            "  \"MaximumMaterializedTiles\": 4096,\n" +
            "  \"MaximumIssues\": " + issues + ",\n" +
            "  \"MaximumStringCharacters\": " + characters + "\n" +
            "}");

        private static string Canonical(ProductionSpatialContentSnapshot snapshot) =>
            Convert.ToBase64String(ProductionSpatialGeneratedSetParser.SerializeCanonical(snapshot.Manifest)) + "|" +
            Convert.ToBase64String(ProductionSpatialGeneratedSetParser.SerializeCanonical(snapshot.Catalog)) + "|" +
            string.Join("|", snapshot.Languages.Select(table => Convert.ToBase64String(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(table))));

        private TextAsset Language(string identity, Action<StringTable> mutate)
        {
            StringTable table = JsonUtility.FromJson<StringTable>(english.text);
            table.language = identity;
            foreach (StringEntry entry in table.entries) entry.text = "test:" + entry.key;
            mutate?.Invoke(table);
            return new TextAsset(System.Text.Encoding.UTF8.GetString(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(table)));
        }

        private static TextAsset RepeatedMissingRequiredFieldLanguage() => new TextAsset(
            "{\n" +
            "  \"schema\": \"string_table\",\n" +
            "  \"schemaVersion\": 1,\n" +
            "  \"language\": \"test-repeated-diagnostics\",\n" +
            "  \"entries\": [\n" +
            "    {},\n" +
            "    {}\n" +
            "  ]\n" +
            "}\n");

        private TextAsset NoncanonicalLanguage(string identity)
        {
            TextAsset canonical = Language(identity, null);
            return new TextAsset(canonical.text.TrimEnd());
        }

        private void AssertFailure(TextAsset[] languages,
            ProductionSpatialContentLoadingDiagnostic diagnostic)
        {
            ProductionSpatialContentLoadResult result = new ContentService().LoadProductionSpatialContent(
                manifest, catalog, languages, limits);
            Assert.That(result.Success, Is.False);
            CollectionAssert.Contains(result.Diagnostics, diagnostic);
        }

        private static TextAsset Asset(string name)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + name);
            Assert.That(asset, Is.Not.Null, name);
            return asset;
        }

        private static TextAsset Asset(object value) => new TextAsset(
            System.Text.Encoding.UTF8.GetString(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(value)));

        private static string Join(ProductionSpatialContentLoadResult result) =>
            string.Join(",", result.Diagnostics.Select(value => value.ToString()));
    }
}
#endif
