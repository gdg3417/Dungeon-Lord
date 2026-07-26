#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Editor.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class ProductionSpatialContentExportTests
    {
        private const string LimitsPath = "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json";

        private static SpatialContentValidationWorkloadLimits Limits()
        {
            var parsed = ProductionSpatialContentWorkloadLimitParser.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(LimitsPath));
            Assert.That(parsed.Success, Is.True);
            return parsed.Limits;
        }

        private static DungeonSpatialAuthoringSource Production() => DungeonSpatialAuthoringRepository.Read();

        [Test]
        public void CommittedPackage_HasExactFilesNormalizedCanonicalRowsAndProjectsApprovedRecords()
        {
            string[] exact = new[] { "README.md", "authoring_manifest.json", "authoring_schema.json" }
                .Concat(new[] { "floors", "floor_allowed_rooms", "floor_allowed_corridors", "rooms", "room_orientations",
                    "room_reserved_offsets", "room_connection_points", "corridors", "corridor_orientations",
                    "corridor_compatible_sockets", "fixed_structures", "fixed_structure_orientations",
                    "fixed_structure_reserved_offsets", "fixed_structure_connection_points", "socket_types",
                    "socket_compatibility", "localization_en" }.Select(value => "tables/" + value + ".csv")).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var source = Production();
            Assert.That(source, Is.Not.Null);
            var before = source.Snapshot();
            CollectionAssert.AreEqual(exact, before.Keys.OrderBy(x => x, StringComparer.Ordinal));
            foreach (var pair in before)
            {
                Assert.That(pair.Value, Is.Not.Empty, pair.Key);
                Assert.That(pair.Value.Take(3).SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }), Is.False, pair.Key);
                Assert.That(pair.Value.Contains((byte)'\r'), Is.False, pair.Key);
                Assert.That(pair.Value.Last(), Is.EqualTo((byte)'\n'), pair.Key);
                Assert.That(pair.Value.Length == 1 || pair.Value[pair.Value.Length - 2] != (byte)'\n', Is.True, pair.Key);
                Assert.DoesNotThrow(() => new UTF8Encoding(false, true).GetString(pair.Value), pair.Key);
            }

            DungeonSpatialAuthoringResult result = DungeonSpatialAuthoringPackageParser.ParseAndProject(source, Limits(), true);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Issues.Select(x => x.ToString())));
            SpatialContentCatalog catalog = result.Projection.Catalog;
            Assert.That(catalog.Metadata.SchemaId, Is.EqualTo("dungeon_spatial_content"));
            Assert.That(catalog.Metadata.SchemaVersion, Is.EqualTo(1));
            Assert.That(catalog.Metadata.ContentVersion, Is.EqualTo("0.1.0"));
            Assert.That(catalog.Floors, Has.Length.EqualTo(1)); Assert.That(catalog.Rooms, Has.Length.EqualTo(3));
            Assert.That(catalog.Corridors, Has.Length.EqualTo(1)); Assert.That(catalog.FixedStructures, Has.Length.EqualTo(2));
            Assert.That(catalog.SocketTypes, Has.Length.EqualTo(1));
            Assert.That(catalog.Floors[0].Bounds.TileCount, Is.EqualTo(144));
            Assert.That(result.Projection.English.schema, Is.EqualTo("string_table"));
            Assert.That(result.Projection.English.schemaVersion, Is.EqualTo(1));
            Assert.That(result.Projection.English.language, Is.EqualTo("en"));
            Assert.That(result.Projection.English.entries, Has.Length.EqualTo(6));
            CollectionAssert.AreEqual(result.Projection.English.entries.Select(e => e.key).OrderBy(x => x, StringComparer.Ordinal), result.Projection.English.entries.Select(e => e.key));
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, source.Snapshot()[pair.Key], pair.Key);
            Assert.That(File.Exists("Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json"), Is.False);
            Assert.That(File.Exists("Assets/_Project/Data/Production/DungeonSpatial/string_table_en.json"), Is.False);
            Assert.That(File.Exists("Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json"), Is.False);
        }

        [TestCase("authoring_manifest.json", DungeonSpatialAuthoringDiagnostic.MissingManifest)]
        [TestCase("authoring_schema.json", DungeonSpatialAuthoringDiagnostic.MissingSchema)]
        [TestCase("tables/rooms.csv", DungeonSpatialAuthoringDiagnostic.MissingTable)]
        public void MissingRequiredSource_FailsClosed(string path, DungeonSpatialAuthoringDiagnostic expected)
        {
            DungeonSpatialAuthoringResult result = DungeonSpatialAuthoringPackageParser.ParseAndProject(Without(path), Limits());
            Assert.That(result.Success, Is.False); Assert.That(result.Issues.Any(x => x.Diagnostic == expected), Is.True);
        }

        [TestCase("tables/rooms.csv", "\ufeff")]
        [TestCase("tables/rooms.csv", "\r")]
        public void InvalidByteNormalization_FailsClosed(string path, string insertion)
        {
            var files = Production().Snapshot().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            byte[] prefix = Encoding.UTF8.GetBytes(insertion); files[path] = prefix.Concat(files[path]).ToArray();
            var result = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits());
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void JsonDuplicateAmbiguousUnknownAndMalformedFields_FailWithStableDiagnostics()
        {
            AssertManifestChange(text => text.Replace("\"schema\":", "\"schema\":\"test\",\n  \"schema\":"), DungeonSpatialAuthoringDiagnostic.DuplicateJsonField);
            AssertManifestChange(text => text.Replace("\"schema\":", "\"Schema\":\"test\",\n  \"schema\":"), DungeonSpatialAuthoringDiagnostic.AmbiguousJsonField);
            AssertManifestChange(text => text.Replace("{", "{\n  \"testUnknown\": 1,"), DungeonSpatialAuthoringDiagnostic.UnknownJsonField);
            AssertManifestChange(text => "{\n", DungeonSpatialAuthoringDiagnostic.MalformedJson);
        }

        [Test]
        public void DuplicateKeysInvalidNumbersEnumsAndForeignKeys_FailClosed()
        {
            AssertTableChange("tables/rooms.csv", s => s + s.Split('\n')[1] + "\n", DungeonSpatialAuthoringDiagnostic.DuplicatePrimaryKey);
            AssertTableChange("tables/floors.csv", s => s.Replace(",12,12,", ",2147483648,12,"), DungeonSpatialAuthoringDiagnostic.Int32Overflow);
            AssertTableChange("tables/room_orientations.csv", s => s.Replace(",Zero", ",zero"), DungeonSpatialAuthoringDiagnostic.InvalidEnumToken);
            AssertTableChange("tables/floor_allowed_rooms.csv", s => s.Replace("spatial.room.basic", "test.gd65b.missing.room"), DungeonSpatialAuthoringDiagnostic.MissingForeignKey);
        }

        [Test]
        public void RowPermutationsProduceEqualCanonicalProjectionAndLowerLimitsPublishNothing()
        {
            var files = Production().Snapshot().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            string path = "tables/localization_en.csv"; string[] lines = Encoding.UTF8.GetString(files[path]).TrimEnd('\n').Split('\n');
            files[path] = Encoding.UTF8.GetBytes(lines[0] + "\n" + string.Join("\n", lines.Skip(1).Reverse()) + "\n");
            var a = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), Limits());
            var b = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits());
            Assert.That(a.Success && b.Success, Is.True);
            CollectionAssert.AreEqual(a.Projection.English.entries.Select(x => x.key), b.Projection.English.entries.Select(x => x.key));
            var low = new SpatialContentValidationWorkloadLimits(1, 1, 1, 1, 1);
            var failure = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), low);
            Assert.That(failure.Success, Is.False); Assert.That(failure.Projection, Is.Null);
            CollectionAssert.AreEqual(failure.Issues.Select(x => x.ToString()), DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), low).Issues.Select(x => x.ToString()));
        }

        private static DungeonSpatialAuthoringSource Without(string path) => new DungeonSpatialAuthoringSource(Production().Snapshot().Where(x => x.Key != path));
        private static void AssertManifestChange(Func<string, string> change, DungeonSpatialAuthoringDiagnostic expected) => AssertChanged("authoring_manifest.json", change, expected);
        private static void AssertTableChange(string path, Func<string, string> change, DungeonSpatialAuthoringDiagnostic expected) => AssertChanged(path, change, expected);
        private static void AssertChanged(string path, Func<string, string> change, DungeonSpatialAuthoringDiagnostic expected)
        {
            var files = Production().Snapshot().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            files[path] = Encoding.UTF8.GetBytes(change(Encoding.UTF8.GetString(files[path])));
            var result = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits());
            Assert.That(result.Success, Is.False); Assert.That(result.Issues.Any(x => x.Diagnostic == expected), Is.True, string.Join("\n", result.Issues.Select(x => x.ToString())));
        }
    }
}
#endif
