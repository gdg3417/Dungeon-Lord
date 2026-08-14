#if UNITY_EDITOR
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SaveSpatialMigrationLimitsTests
    {
        private static string ProductionJson => File.ReadAllText(
            SaveSpatialMigrationLimitsLoader.ProductionPath);

        [Test]
        public void ProductionProfile_LoadsAllApprovedValues()
        {
            SaveSpatialMigrationLimitsLoadResult result =
                SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(
                    SaveSpatialMigrationLimitsLoader.ProductionPath));

            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Profile.Raw.MaximumInputBytes, Is.EqualTo(524288));
            Assert.That(result.Profile.Raw.MaximumNestingDepth, Is.EqualTo(32));
            Assert.That(result.Profile.Raw.MaximumObjectMembers, Is.EqualTo(64));
            Assert.That(result.Profile.Raw.MaximumArrayElements, Is.EqualTo(128));
            Assert.That(result.Profile.Raw.MaximumStringBytes, Is.EqualTo(4096));
            Assert.That(result.Profile.Raw.MaximumScanWork, Is.EqualTo(1048576));
            Assert.That(result.Profile.Canonical.Serialized.MaximumInputBytes, Is.EqualTo(262144));
            Assert.That(result.Profile.Canonical.Serialized.MaximumParsedNodes, Is.EqualTo(8192));
            Assert.That(result.Profile.Canonical.Serialized.MaximumCollectionRecords, Is.EqualTo(4096));
            Assert.That(result.Profile.Canonical.Serialized.MaximumStringCharacters, Is.EqualTo(262144));
            Assert.That(result.Profile.Canonical.Serialized.MaximumDiagnostics, Is.EqualTo(64));
            Assert.That(result.Profile.Canonical.Spatial.MaximumRecords, Is.EqualTo(64));
            Assert.That(result.Profile.Canonical.Spatial.MaximumMaterializedTiles, Is.EqualTo(64));
            Assert.That(result.Profile.Whole.MaximumCandidateBytes, Is.EqualTo(262144));
            Assert.That(result.Profile.Whole.MaximumCopiedValueBytes, Is.EqualTo(524288));
            Assert.That(result.Profile.Whole.MaximumUnknownMembers, Is.EqualTo(64));
            Assert.That(result.Profile.Whole.MaximumUnknownMemberBytes, Is.EqualTo(131072));
        }

        [Test]
        public void ProductionFramingAcceptsExactlyOneLfAndRejectsOtherTerminators()
        {
            byte[] production = File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath);
            Assert.That(production[production.Length - 1], Is.EqualTo((byte)'\n'));
            Assert.That(SaveSpatialMigrationLimitsLoader.Load(production).IsSuccess, Is.True);
            Assert.That(SaveSpatialMigrationLimitsLoader.Load(production.Take(
                production.Length - 1).ToArray()).Reason,
                Is.EqualTo(SaveSpatialMigrationLimitsLoader.InvalidReason));
            Assert.That(SaveSpatialMigrationLimitsLoader.Load(production.Concat(
                new byte[] { (byte)'\n' }).ToArray()).Reason,
                Is.EqualTo(SaveSpatialMigrationLimitsLoader.InvalidReason));
            Assert.That(SaveSpatialMigrationLimitsLoader.Load(new byte[] { 0xef, 0xbb, 0xbf }.Concat(
                production).ToArray()).Reason, Is.EqualTo(SaveSpatialMigrationLimitsLoader.InvalidReason));
            byte[] crlf = production.Take(production.Length - 1).Concat(
                new byte[] { (byte)'\r', (byte)'\n' }).ToArray();
            Assert.That(SaveSpatialMigrationLimitsLoader.Load(crlf).Reason,
                Is.EqualTo(SaveSpatialMigrationLimitsLoader.InvalidReason));
        }

        [TestCase(null, SaveSpatialMigrationLimitsLoader.MissingReason)]
        [TestCase("", SaveSpatialMigrationLimitsLoader.MissingReason)]
        [TestCase("{}", SaveSpatialMigrationLimitsLoader.InvalidReason)]
        [TestCase("not-json", SaveSpatialMigrationLimitsLoader.InvalidReason)]
        public void MissingOrMalformedProfile_FailsClosed(string json, string reason)
        {
            SaveSpatialMigrationLimitsLoadResult result = SaveSpatialMigrationLimitsLoader.Load(json);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
        }

        [Test]
        public void WrongSchemaVersionAndEveryMissingFieldFailClosed()
        {
            AssertFailure(ProductionJson.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":2"),
                SaveSpatialMigrationLimitsLoader.VersionMismatchReason);
            AssertFailure(ProductionJson.Replace("save_spatial_migration_limits", "wrong"),
                SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace(",\"MaximumUnknownMembers\":64", ""),
                SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"MaximumRawSaveBytes\":524288",
                "\"MaximumRawSaveBytes\":524288,\"MaximumRawSaveBytes\":524288"),
                SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"MaximumRawSaveBytes\":524288",
                "\"MaximumRawSaveBytes\":\"524288\""),
                SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"Schema\":\"save_spatial_migration_limits\"," +
                "\"SchemaVersion\":1", "\"SchemaVersion\":1,\"Schema\":" +
                "\"save_spatial_migration_limits\""),
                SaveSpatialMigrationLimitsLoader.InvalidReason);
        }

        [Test]
        public void InvalidDimensionsAndCrossFieldRelationshipsFailClosed()
        {
            AssertFailure(ProductionJson.Replace("\"MaximumRawNestingDepth\":32",
                "\"MaximumRawNestingDepth\":0"), SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"MaximumWholeSaveCandidateBytes\":262144",
                "\"MaximumWholeSaveCandidateBytes\":262145"), SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"MaximumCopiedSourceValueBytes\":524288",
                "\"MaximumCopiedSourceValueBytes\":524289"), SaveSpatialMigrationLimitsLoader.InvalidReason);
            AssertFailure(ProductionJson.Replace("\"MaximumUnknownMemberBytes\":131072",
                "\"MaximumUnknownMemberBytes\":524289"), SaveSpatialMigrationLimitsLoader.InvalidReason);
        }

        [Test]
        public void ProductionAuthorityIsDedicatedAndHasNoBootstrapOrSpatialValidationFallback()
        {
            Assert.That(SaveSpatialMigrationLimitsLoader.ProductionPath,
                Does.Not.Contain("Bootstrap").And.Not.Contain("validation_limits.json"));
            Assert.That(ProductionJson, Does.Not.Contain("MaximumTopLevelRecords"));
        }

        [Test]
        public void ProductionBootstrapSceneOwnsAndLoadsDedicatedProfile()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                SaveSpatialMigrationLimitsLoader.ProductionPath);
            Assert.That(asset, Is.Not.Null);
            string guid = AssetDatabase.AssetPathToGUID(SaveSpatialMigrationLimitsLoader.ProductionPath);
            string scene = File.ReadAllText("Assets/_Project/Scenes/Bootstrap.unity");
            Assert.That(scene, Does.Contain(
                "saveSpatialMigrationLimitsJson: {fileID: 4900000, guid: " + guid + ", type: 3}"));

            SaveSpatialMigrationLimitsLoadResult result = SaveSpatialMigrationLimitsLoader.Load(asset);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.Profile.Raw.MaximumInputBytes, Is.EqualTo(524288));
            Assert.That(result.Profile.Whole.MaximumCandidateBytes, Is.EqualTo(262144));
        }

        [Test]
        public void GameRootRejectsInvalidProfileBeforeConstructingLegacySaveService()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Core/GameRoot.cs");
            int load = source.IndexOf("SaveSpatialMigrationLimitsLoader.Load(saveSpatialMigrationLimitsJson)",
                System.StringComparison.Ordinal);
            int failure = source.IndexOf("if (!saveLimits.IsSuccess)", System.StringComparison.Ordinal);
            int stop = source.IndexOf("return false;", failure, System.StringComparison.Ordinal);
            int legacy = source.IndexOf("new SaveService(", System.StringComparison.Ordinal);
            Assert.That(load, Is.GreaterThanOrEqualTo(0));
            Assert.That(failure, Is.GreaterThan(load));
            Assert.That(stop, Is.GreaterThan(failure));
            Assert.That(legacy, Is.GreaterThan(stop));
        }

        private static void AssertFailure(string json, string reason)
        {
            SaveSpatialMigrationLimitsLoadResult result = SaveSpatialMigrationLimitsLoader.Load(json);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
        }
    }
}
#endif
