#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class ProductionSpatialContentScalabilityTests
    {
        private const string LimitsPath = "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json";
        private const string SocketId = "test.gd65b.scalability.socket.self";
        private const string CorridorId = "test.gd65b.scalability.corridor.straight";
        private const string EntranceId = "test.gd65b.scalability.fixed.entrance";
        private const string CompletionId = "test.gd65b.scalability.fixed.completion";
        private static readonly string[] RoomIds =
        {
            "test.gd65b.scalability.room.basic", "test.gd65b.scalability.room.rectangle",
            "test.gd65b.scalability.room.large"
        };

        private static SpatialContentValidationWorkloadLimits ProductionLimits()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(LimitsPath);
            Assert.That(asset, Is.Not.Null);
            var parsed = ProductionSpatialContentWorkloadLimitParser.Parse(asset);
            Assert.That(parsed.Success, Is.True);
            return parsed.Limits;
        }

        private static HashSet<string> LocalizationKeys() => new HashSet<string>(new[]
        {
            "test.gd65b.scalability.loc.room.basic", "test.gd65b.scalability.loc.room.rectangle",
            "test.gd65b.scalability.loc.room.large", "test.gd65b.scalability.loc.corridor.straight",
            "test.gd65b.scalability.loc.fixed.entrance", "test.gd65b.scalability.loc.fixed.completion"
        }, StringComparer.Ordinal);

        private static SpatialContentCatalog Fixture(int firstFloorWidth = 64, int firstFloorHeight = 64)
        {
            var floors = Enumerable.Range(0, 80).Select(index => new FloorSpatialConfiguration
            {
                FloorDefinitionId = "test.gd65b.scalability.floor." + index.ToString("D3"),
                FloorIndex = index,
                Bounds = index == 0
                    ? new RectangularFloorBounds(default, firstFloorWidth, firstFloorHeight)
                    : new RectangularFloorBounds(default, 8, 8),
                FinalFloorSpaceCapacity = index == 0 ? firstFloorWidth * firstFloorHeight : 64,
                OptionalBranchAllowance = 1,
                AllowedRoomDefinitionIds = (string[])RoomIds.Clone(),
                AllowedCorridorDefinitionIds = new[] { CorridorId },
                EntranceStructureDefinitionId = EntranceId,
                CompletionStructureDefinitionId = CompletionId
            }).ToArray();

            return new SpatialContentCatalog
            {
                Metadata = new SpatialContentExportMetadata
                {
                    SchemaId = "test.gd65b.scalability.schema",
                    SchemaVersion = 1,
                    ContentVersion = "test.gd65b.scalability.version"
                },
                Floors = floors,
                Rooms = new[]
                {
                    Room(RoomIds[0], "test.gd65b.scalability.loc.room.basic", 4, 4),
                    Room(RoomIds[1], "test.gd65b.scalability.loc.room.rectangle", 5, 3),
                    Room(RoomIds[2], "test.gd65b.scalability.loc.room.large", 6, 5)
                },
                Corridors = new[]
                {
                    new CorridorSpatialDefinition
                    {
                        CorridorDefinitionId = CorridorId,
                        LocalizationKey = "test.gd65b.scalability.loc.corridor.straight",
                        Category = CorridorSpatialCategory.Straight,
                        MinimumLength = 1,
                        MaximumLength = 4,
                        Width = 1,
                        TrapCapacity = 1,
                        LootCapacity = 1,
                        AllowedOrientations = new[] { CardinalOrientation.Ninety, CardinalOrientation.Zero },
                        CompatibleSocketTypeIds = new[] { SocketId }
                    }
                },
                FixedStructures = new[]
                {
                    Fixed(EntranceId, "test.gd65b.scalability.loc.fixed.entrance", FixedSpatialStructureKind.Entrance),
                    Fixed(CompletionId, "test.gd65b.scalability.loc.fixed.completion", FixedSpatialStructureKind.CompletionTerminal)
                },
                SocketTypes = new[]
                {
                    new SpatialSocketTypeDefinition
                    {
                        SocketTypeId = SocketId,
                        CompatibleSocketTypeIds = new[] { SocketId }
                    }
                }
            };
        }

        private static RoomSpatialDefinition Room(string id, string key, int width, int height) =>
            new RoomSpatialDefinition
            {
                RoomDefinitionId = id,
                LocalizationKey = key,
                GrossFootprint = new RectangularFootprintDefinition(width, height),
                MonsterCapacity = 1,
                TrapCapacity = 1,
                LootCapacity = 1,
                MaximumConnectionCount = 2,
                AllowedOrientations = new[] { CardinalOrientation.Ninety, CardinalOrientation.Zero },
                ReservedTileOffsets = new[] { new TileCoordinate(1, 1), new TileCoordinate(0, 0) },
                ConnectionPoints = new[]
                {
                    Point(id + ".point.b", width - 1, height / 2, CardinalOrientation.Ninety),
                    Point(id + ".point.a", 0, height / 2, CardinalOrientation.TwoSeventy)
                }
            };

        private static FixedSpatialStructureDefinition Fixed(
            string id, string key, FixedSpatialStructureKind kind) =>
            new FixedSpatialStructureDefinition
            {
                StructureDefinitionId = id,
                LocalizationKey = key,
                Kind = kind,
                GrossFootprint = new RectangularFootprintDefinition(2, 2),
                MaximumConnectionCount = 1,
                AllowedOrientations = new[] { CardinalOrientation.Ninety, CardinalOrientation.Zero },
                ConnectionPoints = new[] { Point(id + ".point", 0, 0, CardinalOrientation.OneEighty) }
            };

        private static SpatialConnectionPointDefinition Point(
            string id, int x, int y, CardinalOrientation facing) => new SpatialConnectionPointDefinition
            {
                ConnectionPointId = id,
                Offset = new TileCoordinate(x, y),
                Facing = facing,
                SocketTypeId = SocketId
            };

        [Test]
        public void EightyFloorFixture_ValidatesAndCanonicalizesWithoutMutation()
        {
            SpatialContentCatalog source = Fixture();
            string before = JsonUtility.ToJson(source);
            SpatialContentValidationWorkloadLimits limits = ProductionLimits();
            SpatialContentValidationResult validation = SpatialContentValidator.Validate(source, limits, LocalizationKeys());
            Assert.That(validation.IsValid, Is.True, string.Join(",", validation.Issues.Select(issue => issue.Reason)));
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));

            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source, limits, out SpatialContentCatalog canonical), Is.True);
            Assert.That(canonical.Floors, Has.Length.EqualTo(80));
            CollectionAssert.AreEqual(Enumerable.Range(0, 80), canonical.Floors.Select(floor => floor.FloorIndex));
            CollectionAssert.AreEqual(Enumerable.Range(0, 80).Select(index =>
                "test.gd65b.scalability.floor." + index.ToString("D3")),
                canonical.Floors.Select(floor => floor.FloorDefinitionId));
            Assert.That(canonical.Floors.All(floor =>
                floor.AllowedRoomDefinitionIds.Length + floor.AllowedCorridorDefinitionIds.Length == 4), Is.True);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
        }

        [Test]
        public void TopLevelAndNestedPermutations_CanonicalizeAndDiagnoseDeterministically()
        {
            SpatialContentValidationWorkloadLimits limits = ProductionLimits();
            SpatialContentCatalog source = Fixture();
            SpatialContentCatalog permutation = Clone(source);
            Array.Reverse(permutation.Floors);
            Array.Reverse(permutation.Rooms);
            Array.Reverse(permutation.FixedStructures);
            foreach (FloorSpatialConfiguration floor in permutation.Floors)
                Array.Reverse(floor.AllowedRoomDefinitionIds);
            foreach (RoomSpatialDefinition room in permutation.Rooms)
            {
                Array.Reverse(room.AllowedOrientations);
                Array.Reverse(room.ReservedTileOffsets);
                Array.Reverse(room.ConnectionPoints);
            }
            Array.Reverse(permutation.Corridors[0].AllowedOrientations);
            Array.Reverse(permutation.SocketTypes[0].CompatibleSocketTypeIds);

            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source, limits, out SpatialContentCatalog first), Is.True);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(permutation, limits, out SpatialContentCatalog second), Is.True);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source, limits, out SpatialContentCatalog repeated), Is.True);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(JsonUtility.ToJson(repeated), Is.EqualTo(JsonUtility.ToJson(first)));
            CollectionAssert.AreEqual(Diagnostics(source, limits), Diagnostics(permutation, limits));
        }

        [Test]
        public void LowerCallerLimits_RejectWithWorkloadExceededAndCanonicalizationRefuses()
        {
            SpatialContentCatalog source = Fixture();
            SpatialContentValidationWorkloadLimits production = ProductionLimits();
            var lower = new SpatialContentValidationWorkloadLimits(
                79, production.MaximumNestedRecords, production.MaximumMaterializedTiles,
                production.MaximumIssues, production.MaximumStringCharacters);
            SpatialContentValidationResult result = SpatialContentValidator.Validate(source, lower, LocalizationKeys());
            CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.WorkloadExceeded },
                result.Issues.Select(issue => issue.Reason));
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source, lower, out _), Is.False);
        }

        [Test]
        public void IndividualBoundary_4096Passes_And4097ReportsFootprintTileCountExceeded()
        {
            SpatialContentValidationWorkloadLimits limits = ProductionLimits();
            Assert.That(SpatialContentValidator.Validate(Fixture(), limits, LocalizationKeys()).IsValid, Is.True);
            SpatialContentValidationResult over = SpatialContentValidator.Validate(Fixture(4097, 1), limits, LocalizationKeys());
            Assert.That(
                over.Issues.Any(issue =>
                    issue.Reason == SpatialContentValidationReason.FootprintTileCountExceeded),
                Is.True);

            Assert.That(
                over.Issues.Any(issue =>
                    issue.Reason == SpatialContentValidationReason.WorkloadExceeded),
                Is.False);
        }

        private static SpatialContentCatalog Clone(SpatialContentCatalog source) =>
            JsonUtility.FromJson<SpatialContentCatalog>(JsonUtility.ToJson(source));
        private static string[] Diagnostics(SpatialContentCatalog source, SpatialContentValidationWorkloadLimits limits) =>
            SpatialContentValidator.Validate(source, limits, LocalizationKeys()).Issues.Select(JsonUtility.ToJson).ToArray();
    }
}
#endif
