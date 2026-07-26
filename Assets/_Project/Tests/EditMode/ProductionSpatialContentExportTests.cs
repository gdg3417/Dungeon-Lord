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
            Assert.That(catalog.Floors.Length + catalog.Rooms.Length + catalog.Corridors.Length + catalog.FixedStructures.Length + catalog.SocketTypes.Length, Is.EqualTo(8));
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
            foreach (string path in new[] { "tables/rooms.csv", "tables/floor_allowed_rooms.csv", "tables/room_orientations.csv", "tables/room_connection_points.csv", "tables/localization_en.csv" })
            {
                string[] lines = Encoding.UTF8.GetString(files[path]).TrimEnd('\n').Split('\n');
                files[path] = Encoding.UTF8.GetBytes(lines[0] + "\n" + string.Join("\n", lines.Skip(1).Reverse()) + "\n");
            }
            var a = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), Limits());
            var b = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits());
            Assert.That(a.Success && b.Success, Is.True);
            Assert.That(JsonUtility.ToJson(b.Projection.Catalog), Is.EqualTo(JsonUtility.ToJson(a.Projection.Catalog)));
            Assert.That(JsonUtility.ToJson(b.Projection.English), Is.EqualTo(JsonUtility.ToJson(a.Projection.English)));
            var low = new SpatialContentValidationWorkloadLimits(1, 1, 1, 1, 1);
            var failure = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), low);
            Assert.That(failure.Success, Is.False); Assert.That(failure.Projection, Is.Null);
            CollectionAssert.AreEqual(failure.Issues.Select(x => x.ToString()), DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), low).Issues.Select(x => x.ToString()));
        }

        [Test]
        public void CommittedPackage_ProjectsEveryApprovedProductionValue()
        {
            DungeonSpatialAuthoringResult result = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), Limits());
            Assert.That(result.Success, Is.True, string.Join("\n", result.Issues.Select(issue => issue.ToString())));
            SpatialContentCatalog catalog = result.Projection.Catalog;
            FloorSpatialConfiguration floor = catalog.Floors.Single();
            Assert.Multiple(() =>
            {
                Assert.That(floor.FloorDefinitionId, Is.EqualTo("spatial.floor.01"));
                Assert.That(floor.FloorIndex, Is.Zero); Assert.That(floor.Bounds.Minimum.X, Is.Zero); Assert.That(floor.Bounds.Minimum.Y, Is.Zero);
                Assert.That(floor.Bounds.Width, Is.EqualTo(12)); Assert.That(floor.Bounds.Height, Is.EqualTo(12));
                Assert.That(floor.FinalFloorSpaceCapacity, Is.EqualTo(60)); Assert.That(floor.OptionalBranchAllowance, Is.EqualTo(1));
                Assert.That(floor.EntranceStructureDefinitionId, Is.EqualTo("spatial.fixed.entrance_hall"));
                Assert.That(floor.CompletionStructureDefinitionId, Is.EqualTo("spatial.fixed.completion_terminal"));
            });
            CollectionAssert.AreEqual(new[] { "spatial.room.basic", "spatial.room.large_chamber", "spatial.room.rectangle" }, floor.AllowedRoomDefinitionIds);
            CollectionAssert.AreEqual(new[] { "spatial.corridor.straight_stone" }, floor.AllowedCorridorDefinitionIds);
            AssertRoom(catalog, "spatial.room.basic", 4, 4, 3, 2, 2, 2, new[] { CardinalOrientation.Zero },
                new[] { "east:3:1:Ninety", "north:1:3:Zero", "south:1:0:OneEighty", "west:0:1:TwoSeventy" });
            AssertRoom(catalog, "spatial.room.large_chamber", 5, 6, 3, 4, 4, 4, new[] { CardinalOrientation.Zero, CardinalOrientation.Ninety },
                new[] { "east:4:2:Ninety", "north:2:5:Zero", "south:2:0:OneEighty", "west:0:2:TwoSeventy" });
            AssertRoom(catalog, "spatial.room.rectangle", 3, 5, 3, 3, 1, 2, new[] { CardinalOrientation.Zero, CardinalOrientation.Ninety },
                new[] { "east:2:2:Ninety", "north:1:4:Zero", "south:1:0:OneEighty", "west:0:2:TwoSeventy" });
            CorridorSpatialDefinition corridor = catalog.Corridors.Single();
            Assert.Multiple(() => { Assert.That(corridor.CorridorDefinitionId, Is.EqualTo("spatial.corridor.straight_stone")); Assert.That(corridor.Category, Is.EqualTo(CorridorSpatialCategory.Straight)); Assert.That(corridor.MinimumLength, Is.EqualTo(1)); Assert.That(corridor.MaximumLength, Is.EqualTo(4)); Assert.That(corridor.Width, Is.EqualTo(1)); Assert.That(corridor.MonsterCapacity, Is.Zero); Assert.That(corridor.TrapCapacity, Is.EqualTo(1)); Assert.That(corridor.LootCapacity, Is.EqualTo(1)); });
            Assert.That(corridor.LocalizationKey, Is.EqualTo("spatial.corridor.straight_stone.display_name"));
            CollectionAssert.AreEqual(new[] { CardinalOrientation.Zero, CardinalOrientation.Ninety }, corridor.AllowedOrientations);
            CollectionAssert.AreEqual(new[] { "spatial.socket.standard_passage" }, corridor.CompatibleSocketTypeIds);
            AssertFixed(catalog, "spatial.fixed.entrance_hall", FixedSpatialStructureKind.Entrance, 3, 2, 1, 1, 1, CardinalOrientation.Zero);
            AssertFixed(catalog, "spatial.fixed.completion_terminal", FixedSpatialStructureKind.CompletionTerminal, 2, 2, 1, 0, 0, CardinalOrientation.OneEighty);
            SpatialSocketTypeDefinition socket = catalog.SocketTypes.Single(); Assert.That(socket.SocketTypeId, Is.EqualTo("spatial.socket.standard_passage")); CollectionAssert.AreEqual(new[] { socket.SocketTypeId }, socket.CompatibleSocketTypeIds);
            CollectionAssert.AreEqual(new[] { "Straight Stone Corridor", "Completion Terminal", "Entrance Hall", "Basic Room", "Large Chamber", "Rectangle Room" }, result.Projection.English.entries.Select(entry => entry.text));
            CollectionAssert.AreEqual(new[] { "spatial.corridor.straight_stone.display_name", "spatial.fixed.completion_terminal.display_name", "spatial.fixed.entrance_hall.display_name", "spatial.room.basic.display_name", "spatial.room.large_chamber.display_name", "spatial.room.rectangle.display_name" }, result.Projection.English.entries.Select(entry => entry.key));
            int nested = floor.AllowedRoomDefinitionIds.Length + floor.AllowedCorridorDefinitionIds.Length + catalog.Rooms.Sum(room => room.AllowedOrientations.Length + room.ReservedTileOffsets.Length + room.ConnectionPoints.Length) + corridor.AllowedOrientations.Length + corridor.CompatibleSocketTypeIds.Length + catalog.FixedStructures.Sum(item => item.AllowedOrientations.Length + item.ReservedTileOffsets.Length + item.ConnectionPoints.Length) + socket.CompatibleSocketTypeIds.Length + result.Projection.English.entries.Length;
            Assert.That(nested, Is.EqualTo(41));
        }

        [Test]
        public void EveryHeaderOnlyTable_FailsClosedWithoutThrowing()
        {
            foreach (string path in Production().Snapshot().Keys.Where(path => path.EndsWith(".csv", StringComparison.Ordinal)))
            {
                DungeonSpatialAuthoringResult result = null;
                Assert.DoesNotThrow(() => result = Change(path, text => text.Substring(0, text.IndexOf('\n') + 1)));
                if (path.EndsWith("room_reserved_offsets.csv", StringComparison.Ordinal) || path.EndsWith("fixed_structure_reserved_offsets.csv", StringComparison.Ordinal))
                    Assert.That(result.Success, Is.True, path);
                else
                    Assert.That(result.Success, Is.False, path);
            }
        }

        [Test]
        public void NullEmptyAndInvalidUtf8Sources_FailClosedWithoutThrowing()
        {
            Assert.DoesNotThrow(() => Assert.That(DungeonSpatialAuthoringPackageParser.ParseAndProject(null, Limits()).Success, Is.False));
            AssertNoThrowBytes("tables/rooms.csv", null, DungeonSpatialAuthoringDiagnostic.MissingTable);
            AssertNoThrowBytes("tables/rooms.csv", Array.Empty<byte>(), DungeonSpatialAuthoringDiagnostic.EmptyFile);
            AssertNoThrowBytes("authoring_schema.json", new byte[] { 0xff, (byte)'\n' }, DungeonSpatialAuthoringDiagnostic.InvalidUtf8);
        }

        [Test]
        public void DuplicateSourcePath_IsRetainedAndFailsClosed()
        {
            var files = Production().Snapshot().ToList();
            files.Add(new KeyValuePair<string, byte[]>(files[0].Key, files[0].Value));
            DungeonSpatialAuthoringResult result = DungeonSpatialAuthoringPackageParser.ParseAndProject(
                new DungeonSpatialAuthoringSource(files), Limits());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.DuplicateSourcePath), Is.True);
        }

        [TestCase("\"contentVersion\": \"0.1.0\"", "\"contentVersion\": 1", DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType)]
        [TestCase("\"schema\": \"dungeon_spatial_authoring\"", "\"schema\": \"test.schema\"", DungeonSpatialAuthoringDiagnostic.UnsupportedAuthoringSchema)]
        [TestCase("\"schemaVersion\": 1", "\"schemaVersion\": 2", DungeonSpatialAuthoringDiagnostic.UnsupportedAuthoringSchema)]
        [TestCase("\"contentVersion\": \"0.1.0\"", "\"contentVersion\": \"0.2.0\"", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        [TestCase("\"catalogSchemaId\": \"dungeon_spatial_content\"", "\"catalogSchemaId\": \"test.catalog\"", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        [TestCase("\"catalogSchemaVersion\": 1", "\"catalogSchemaVersion\": 2", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        [TestCase("\"stringTableSchemaId\": \"string_table\"", "\"stringTableSchemaId\": \"test.table\"", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        [TestCase("\"stringTableSchemaVersion\": 1", "\"stringTableSchemaVersion\": 2", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        [TestCase("\"requiredLanguage\": \"en\"", "\"requiredLanguage\": \"ja\"", DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch)]
        public void ManifestValueAndTypeFailures_ArePrecise(string oldValue, string newValue, DungeonSpatialAuthoringDiagnostic diagnostic)
        {
            AssertChanged("authoring_manifest.json", text => text.Replace(oldValue, newValue), diagnostic);
        }

        [TestCase("\"type\": \"int32\"", "\"type\": \"testType\"")]
        [TestCase("\"required\": true", "\"required\": \"true\"")]
        [TestCase("\"allowBlank\": false", "\"allowBlank\": 0")]
        [TestCase("\"enum\": \"CardinalOrientation\"", "\"enum\": \"TestEnum\"")]
        [TestCase("\"primaryKey\": [", "\"primaryKey\": [\"UnknownColumn\",")]
        [TestCase("\"canonicalOrder\": [", "\"canonicalOrder\": [\"UnknownColumn\",")]
        [TestCase("\"parent\": \"floors\"", "\"parent\": \"test.parent\"")]
        public void SchemaOwnedDimensions_AreParsedAndRejectedWhenInvalid(string oldValue, string newValue)
        {
            DungeonSpatialAuthoringResult result = Change("authoring_schema.json", text => text.Replace(oldValue, newValue));
            Assert.That(result.Success, Is.False); Assert.That(result.Issues.Any(issue => issue.RelativePath == "authoring_schema.json"), Is.True);
        }

        [TestCase("EntranceStructureDefinitionId", "spatial.fixed.entrance_hall")]
        [TestCase("CompletionStructureDefinitionId", "spatial.fixed.completion_terminal")]
        public void FloorEndpointForeignKeys_AreSchemaValidatedWithExactIssue(string column, string value)
        {
            DungeonSpatialAuthoringResult result = Change("tables/floors.csv", text => text.Replace(value, "test.missing.fixed"));
            DungeonSpatialAuthoringIssue issue = result.Issues.Single(item => item.Diagnostic == DungeonSpatialAuthoringDiagnostic.MissingForeignKey && item.Column == column);
            Assert.That(issue.RelativePath, Is.EqualTo("tables/floors.csv")); Assert.That(issue.TableId, Is.EqualTo("floors")); Assert.That(issue.RecordKey, Is.EqualTo("spatial.floor.01"));
        }

        [Test]
        public void CompleteEnglishWorkload_ExactBoundaryPassesAndOneCharacterOverFailsDeterministically()
        {
            SpatialContentValidationWorkloadLimits production = Limits();
            int low = 1, high = production.MaximumStringCharacters;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (ParseWithStringLimit(middle).Success) high = middle; else low = middle + 1;
            }
            DungeonSpatialAuthoringSource source = Production(); var before = source.Snapshot();
            Assert.That(ParseWithStringLimit(low).Success, Is.True);
            DungeonSpatialAuthoringResult first = ParseWithStringLimit(low - 1);
            DungeonSpatialAuthoringResult second = ParseWithStringLimit(low - 1);
            Assert.That(first.Success, Is.False); Assert.That(first.Projection, Is.Null);
            Assert.That(first.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded), Is.True);
            CollectionAssert.AreEqual(first.Issues.Select(issue => issue.ToString()), second.Issues.Select(issue => issue.ToString()));
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, source.Snapshot()[pair.Key]);
            Assert.That(low, Is.GreaterThan("string_table".Length + "en".Length + 6));
        }

        [Test]
        public void LargeEnglishText_ExceedsCumulativePackageWorkload()
        {
            DungeonSpatialAuthoringResult result = Change("tables/localization_en.csv", text =>
                text.Replace("Basic Room", new string('a', Limits().MaximumStringCharacters)));
            Assert.That(result.Success, Is.False); Assert.That(result.Projection, Is.Null);
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded), Is.True);
        }

        [TestCase("\"MonsterCapacity\",\n          \"type\": \"int32\"", "\"MonsterCapacity\",\n          \"type\": \"ownerScopedId\"")]
        [TestCase("\"FloorIndex\",\n          \"type\": \"int32\"", "\"FloorIndex\",\n          \"type\": \"spatialId\"")]
        [TestCase("\"enum\": \"CardinalOrientation\"", "\"enum\": \"CorridorSpatialCategory\"")]
        [TestCase("\"required\": true,\n          \"allowBlank\": false", "\"required\": true,\n          \"allowBlank\": true")]
        public void V1ProjectorCompatibility_RejectsRecognizedButIncompatibleSchema(string oldValue, string newValue)
        {
            DungeonSpatialAuthoringResult result = Change("authoring_schema.json", text => text.Replace(oldValue, newValue));
            Assert.That(result.Success, Is.False); Assert.That(result.Projection, Is.Null);
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.InvalidSchema), Is.True);
        }

        [TestCase("\"id\": \"floors\"", "\"id\": \"rooms\"")]
        [TestCase("\"path\": \"tables/floors.csv\"", "\"path\": \"tables/rooms.csv\"")]
        [TestCase("\"id\": \"floors\"", "\"id\": \"\"")]
        [TestCase("\"name\": \"FloorDefinitionId\"", "\"name\": \"\"")]
        [TestCase("\"name\": \"FloorIndex\"", "\"name\": \"FloorDefinitionId\"")]
        [TestCase("\"primaryKey\": [\n        \"FloorDefinitionId\"\n      ]", "\"primaryKey\": []")]
        [TestCase("\"canonicalOrder\": [\n        \"FloorDefinitionId\"\n      ]", "\"canonicalOrder\": []")]
        public void InvalidSchemaIdentitiesAndKeys_FailWithoutThrowing(string oldValue, string newValue)
        {
            DungeonSpatialAuthoringResult result = null;
            Assert.DoesNotThrow(() => result = Change("authoring_schema.json", text => text.Replace(oldValue, newValue)));
            Assert.That(result.Success, Is.False); Assert.That(result.Projection, Is.Null);
        }

        [Test]
        public void ChildRelationshipWithoutOwnerForeignKeyAndOrphanRow_FailsWithoutPublication()
        {
            var files = Production().Snapshot().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            string schema = Encoding.UTF8.GetString(files["authoring_schema.json"])
                .Replace("\"table\": \"room_orientations\"", "\"table\": \"test.room_orientations\"");
            files["authoring_schema.json"] = Encoding.UTF8.GetBytes(schema);
            string rows = Encoding.UTF8.GetString(files["tables/room_orientations.csv"]);
            files["tables/room_orientations.csv"] = Encoding.UTF8.GetBytes(rows + "test.missing.room,Zero\n");
            DungeonSpatialAuthoringResult result = null;
            Assert.DoesNotThrow(() => result = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits()));
            Assert.That(result.Success, Is.False); Assert.That(result.Projection, Is.Null);
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.InvalidSchema), Is.True);
        }

        [Test]
        public void FloorEndpointKinds_AreOwnedByExistingProjectedCatalogValidator()
        {
            DungeonSpatialAuthoringResult valid = DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), Limits());
            Assert.That(valid.Projection.Catalog.FixedStructures.Single(item => item.StructureDefinitionId == valid.Projection.Catalog.Floors[0].EntranceStructureDefinitionId).Kind, Is.EqualTo(FixedSpatialStructureKind.Entrance));
            Assert.That(valid.Projection.Catalog.FixedStructures.Single(item => item.StructureDefinitionId == valid.Projection.Catalog.Floors[0].CompletionStructureDefinitionId).Kind, Is.EqualTo(FixedSpatialStructureKind.CompletionTerminal));
            DungeonSpatialAuthoringResult swapped = Change("tables/floors.csv", text => text
                .Replace("spatial.fixed.entrance_hall", "test.swap")
                .Replace("spatial.fixed.completion_terminal", "spatial.fixed.entrance_hall")
                .Replace("test.swap", "spatial.fixed.completion_terminal"));
            Assert.That(swapped.Success, Is.False); Assert.That(swapped.Projection, Is.Null);
            Assert.That(swapped.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.ProjectedCatalogInvalid), Is.True);
        }

        [TestCase("Rectangle Room\n", "Rectangle Room", DungeonSpatialAuthoringDiagnostic.InvalidTrailingNewline)]
        [TestCase("Rectangle Room\n", "Rectangle Room\n\n", DungeonSpatialAuthoringDiagnostic.InvalidTrailingNewline)]
        [TestCase("Basic Room\n", "Basic Room\r\n", DungeonSpatialAuthoringDiagnostic.InvalidLineEnding)]
        [TestCase("Basic Room\n", "Basic Room\r", DungeonSpatialAuthoringDiagnostic.InvalidLineEnding)]
        [TestCase("Basic Room", "Basic\tRoom", DungeonSpatialAuthoringDiagnostic.InvalidFormat)]
        [TestCase("Basic Room", "\"Basic Room", DungeonSpatialAuthoringDiagnostic.MalformedCsv)]
        [TestCase("Basic Room", "\"Basic\nRoom\"", DungeonSpatialAuthoringDiagnostic.MalformedCsv)]
        public void CsvByteAndQuoteFailures_ReturnStableDiagnostics(string oldValue, string newValue, DungeonSpatialAuthoringDiagnostic diagnostic)
        {
            AssertChanged("tables/localization_en.csv", text => text.Replace(oldValue, newValue), diagnostic);
        }

        [TestCase("Basic Room", "\"Basic, Room\"", "Basic, Room")]
        [TestCase("Basic Room", "\"Basic \"\"Room\"\"\"", "Basic \"Room\"")]
        public void CsvQuotedCommaAndDoubledQuote_AreAccepted(string oldValue, string replacement, string expected)
        {
            DungeonSpatialAuthoringResult result = Change("tables/localization_en.csv", text => text.Replace(oldValue, replacement));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Issues.Select(issue => issue.ToString())));
            Assert.That(result.Projection.English.entries.Single(entry => entry.key == "spatial.room.basic.display_name").text, Is.EqualTo(expected));
        }

        [TestCase(",0,0,12,12,", ",+0,0,12,12,")]
        [TestCase(",0,0,12,12,", ",00,0,12,12,")]
        [TestCase(",0,0,12,12,", ",-0,0,12,12,")]
        [TestCase(",0,0,12,12,", ",2147483648,0,12,12,")]
        public void CsvUnsupportedIntegerForms_FailClosed(string oldValue, string replacement)
        {
            DungeonSpatialAuthoringResult result = Change("tables/floors.csv", text => text.Replace(oldValue, replacement));
            Assert.That(result.Success, Is.False); Assert.That(result.Projection, Is.Null);
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.InvalidInt32 || issue.Diagnostic == DungeonSpatialAuthoringDiagnostic.Int32Overflow), Is.True);
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

        private static DungeonSpatialAuthoringResult Change(string path, Func<string, string> change)
        {
            var files = Production().Snapshot().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            files[path] = Encoding.UTF8.GetBytes(change(Encoding.UTF8.GetString(files[path])));
            return DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits());
        }
        private static void AssertNoThrowBytes(string path, byte[] bytes, DungeonSpatialAuthoringDiagnostic diagnostic)
        {
            var files = Production().Snapshot().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal); files[path] = bytes;
            DungeonSpatialAuthoringResult result = null; Assert.DoesNotThrow(() => result = DungeonSpatialAuthoringPackageParser.ParseAndProject(new DungeonSpatialAuthoringSource(files), Limits()));
            Assert.That(result.Issues.Any(issue => issue.Diagnostic == diagnostic), Is.True);
        }
        private static void AssertRoom(SpatialContentCatalog catalog,string id,int width,int height,int connections,int monsters,int traps,int loot,CardinalOrientation[] orientations,string[] points)
        {
            RoomSpatialDefinition room=catalog.Rooms.Single(item=>item.RoomDefinitionId==id); Assert.Multiple(()=>{Assert.That(room.LocalizationKey,Is.EqualTo(id+".display_name"));Assert.That(room.GrossFootprint.Width,Is.EqualTo(width));Assert.That(room.GrossFootprint.Height,Is.EqualTo(height));Assert.That(room.MaximumConnectionCount,Is.EqualTo(connections));Assert.That(room.MonsterCapacity,Is.EqualTo(monsters));Assert.That(room.TrapCapacity,Is.EqualTo(traps));Assert.That(room.LootCapacity,Is.EqualTo(loot));Assert.That(room.ReservedTileOffsets,Is.Empty);}); CollectionAssert.AreEqual(orientations,room.AllowedOrientations); CollectionAssert.AreEqual(points,room.ConnectionPoints.Select(point=>$"{point.ConnectionPointId}:{point.Offset.X}:{point.Offset.Y}:{point.Facing}")); Assert.That(room.ConnectionPoints.All(point=>point.SocketTypeId=="spatial.socket.standard_passage"),Is.True);
        }
        private static void AssertFixed(SpatialContentCatalog catalog,string id,FixedSpatialStructureKind kind,int width,int height,int connections,int x,int y,CardinalOrientation facing)
        {
            FixedSpatialStructureDefinition item=catalog.FixedStructures.Single(value=>value.StructureDefinitionId==id); Assert.Multiple(()=>{Assert.That(item.LocalizationKey,Is.EqualTo(id+".display_name"));Assert.That(item.Kind,Is.EqualTo(kind));Assert.That(item.GrossFootprint.Width,Is.EqualTo(width));Assert.That(item.GrossFootprint.Height,Is.EqualTo(height));Assert.That(item.MaximumConnectionCount,Is.EqualTo(connections));Assert.That(item.ReservedTileOffsets,Is.Empty);Assert.That(item.AllowedOrientations,Is.EqualTo(new[]{CardinalOrientation.Zero,CardinalOrientation.Ninety,CardinalOrientation.OneEighty,CardinalOrientation.TwoSeventy}));Assert.That(item.ConnectionPoints.Single().ConnectionPointId,Is.EqualTo("route"));Assert.That(item.ConnectionPoints.Single().Offset.X,Is.EqualTo(x));Assert.That(item.ConnectionPoints.Single().Offset.Y,Is.EqualTo(y));Assert.That(item.ConnectionPoints.Single().Facing,Is.EqualTo(facing));Assert.That(item.ConnectionPoints.Single().SocketTypeId,Is.EqualTo("spatial.socket.standard_passage"));});
        }
        private static DungeonSpatialAuthoringResult ParseWithStringLimit(int maximum)
        {
            SpatialContentValidationWorkloadLimits limits = Limits();
            return DungeonSpatialAuthoringPackageParser.ParseAndProject(Production(), new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords, limits.MaximumNestedRecords, limits.MaximumMaterializedTiles, limits.MaximumIssues, maximum));
        }
    }
}
#endif
