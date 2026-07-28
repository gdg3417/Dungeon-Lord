#if UNITY_EDITOR
using System;
using System.Linq;
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
            string scene = System.IO.File.ReadAllText("Assets/_Project/Scenes/Bootstrap.unity");
            StringAssert.Contains("productionSpatialManifest: {fileID: 4900000, guid: 65b3b00000000000000000000000000c", scene);
            StringAssert.Contains("productionSpatialCatalog: {fileID: 4900000, guid: 65b3b00000000000000000000000000d", scene);
            StringAssert.Contains("productionSpatialLanguageTables:\n  - {fileID: 4900000, guid: 65b3b00000000000000000000000000e", scene);
            StringAssert.Contains("productionSpatialValidationLimits: {fileID: 4900000, guid: 10fce78ef6ec499d93fdfc87c97030d6", scene);
            string source = System.IO.File.ReadAllText("Assets/_Project/Scripts/Core/GameRoot.cs");
            string fallback = source.Substring(source.IndexOf("private void EnsureContentAssetsAssigned", StringComparison.Ordinal),
                source.IndexOf("public void InitializeServicesAndData", StringComparison.Ordinal) -
                source.IndexOf("private void EnsureContentAssetsAssigned", StringComparison.Ordinal));
            StringAssert.DoesNotContain("productionSpatial", fallback);
        }

        private ProductionSpatialContentLoadResult Load(ContentService service) =>
            service.LoadProductionSpatialContent(manifest, catalog, new[] { english }, limits);

        private ProductionSpatialContentLoadResult LoadWith(TextAsset suppliedManifest,
            TextAsset suppliedCatalog, TextAsset[] languages) =>
            new ContentService().LoadProductionSpatialContent(
                suppliedManifest, suppliedCatalog, languages, limits);

        private TextAsset Language(string identity, Action<StringTable> mutate)
        {
            StringTable table = JsonUtility.FromJson<StringTable>(english.text);
            table.language = identity;
            foreach (StringEntry entry in table.entries) entry.text = "test:" + entry.key;
            mutate?.Invoke(table);
            return new TextAsset(System.Text.Encoding.UTF8.GetString(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(table)));
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
