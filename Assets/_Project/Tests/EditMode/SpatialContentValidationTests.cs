#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SpatialContentValidationTests
    {
        private static SpatialContentValidationWorkloadLimits Limits(
            int top = 64, int nested = 256, int tiles = 256, int issues = 256,
            int characters = 8192) =>
            new SpatialContentValidationWorkloadLimits(top, nested, tiles, issues, characters);

        private static SpatialContentCatalog Valid()
        {
            const string socket = "test.gd65a.socket";
            const string room = "test.gd65a.room";
            const string corridor = "test.gd65a.corridor";
            const string entrance = "test.gd65a.entrance";
            const string completion = "test.gd65a.completion";
            return new SpatialContentCatalog
            {
                Metadata = new SpatialContentExportMetadata
                {
                    SchemaId = "test.gd65a.schema",
                    SchemaVersion = 1,
                    ContentVersion = "test.gd65a.version"
                },
                SocketTypes = new[]
                {
                    new SpatialSocketTypeDefinition
                    {
                        SocketTypeId = socket,
                        CompatibleSocketTypeIds = new[] { socket }
                    }
                },
                Rooms = new[]
                {
                    Room(room, "test.gd65a.loc.room", socket)
                },
                Corridors = new[]
                {
                    new CorridorSpatialDefinition
                    {
                        CorridorDefinitionId = corridor,
                        LocalizationKey = "test.gd65a.loc.corridor",
                        Category = CorridorSpatialCategory.Straight,
                        MinimumLength = 1,
                        MaximumLength = 2,
                        Width = 1,
                        AllowedOrientations = new[] { CardinalOrientation.Zero },
                        CompatibleSocketTypeIds = new[] { socket }
                    }
                },
                FixedStructures = new[]
                {
                    Fixed(entrance, FixedSpatialStructureKind.Entrance, socket),
                    Fixed(completion, FixedSpatialStructureKind.CompletionTerminal, socket)
                },
                Floors = new[]
                {
                    new FloorSpatialConfiguration
                    {
                        FloorDefinitionId = "test.gd65a.floor",
                        FloorIndex = 0,
                        Bounds = new RectangularFloorBounds(default, 4, 4),
                        FinalFloorSpaceCapacity = 16,
                        OptionalBranchAllowance = 1,
                        AllowedRoomDefinitionIds = new[] { room },
                        AllowedCorridorDefinitionIds = new[] { corridor },
                        EntranceStructureDefinitionId = entrance,
                        CompletionStructureDefinitionId = completion
                    }
                }
            };
        }

        private static RoomSpatialDefinition Room(string id, string localizationKey, string socket) =>
            new RoomSpatialDefinition
            {
                RoomDefinitionId = id,
                LocalizationKey = localizationKey,
                GrossFootprint = new RectangularFootprintDefinition(2, 2),
                AllowedOrientations = new[] { CardinalOrientation.Zero },
                MaximumConnectionCount = 1,
                ConnectionPoints = new[]
                {
                    Point(id + ".point", 0, 0, CardinalOrientation.OneEighty, socket)
                }
            };

        private static FixedSpatialStructureDefinition Fixed(
            string id, FixedSpatialStructureKind kind, string socket) =>
            new FixedSpatialStructureDefinition
            {
                StructureDefinitionId = id,
                LocalizationKey = "test.gd65a.loc." + id,
                Kind = kind,
                GrossFootprint = new RectangularFootprintDefinition(1, 1),
                AllowedOrientations = new[] { CardinalOrientation.Zero },
                MaximumConnectionCount = 1,
                ConnectionPoints = new[]
                {
                    Point(id + ".point", 0, 0, CardinalOrientation.Zero, socket)
                }
            };

        private static SpatialConnectionPointDefinition Point(
            string id, int x, int y, CardinalOrientation facing, string socket) =>
            new SpatialConnectionPointDefinition
            {
                ConnectionPointId = id,
                Offset = new TileCoordinate(x, y),
                Facing = facing,
                SocketTypeId = socket
            };

        private static SpatialContentCatalog Clone(SpatialContentCatalog value) =>
            JsonUtility.FromJson<SpatialContentCatalog>(JsonUtility.ToJson(value));

        private static SpatialContentValidationResult Validate(
            SpatialContentCatalog catalog,
            SpatialContentValidationWorkloadLimits? limits = null,
            ISet<string> localizationKeys = null) =>
            SpatialContentValidator.Validate(catalog, limits ?? Limits(), localizationKeys);

        private static SpatialContentValidationReason[] Reasons(
            SpatialContentCatalog catalog,
            SpatialContentValidationWorkloadLimits? limits = null,
            ISet<string> localizationKeys = null) =>
            Validate(catalog, limits, localizationKeys).Issues.Select(issue => issue.Reason).ToArray();

        private static string[] CompleteDiagnostics(SpatialContentCatalog catalog) =>
            Validate(catalog).Issues.Select(JsonUtility.ToJson).ToArray();

        [Test]
        public void ValidMinimalCatalog_IsValid()
        {
            Assert.That(Validate(Valid()).IsValid, Is.True);
        }

        [Test]
        public void NullCatalogAndInvalidLimits_FailClosed()
        {
            CollectionAssert.AreEqual(
                new[] { SpatialContentValidationReason.CatalogMissing }, Reasons(null));
            CollectionAssert.AreEqual(
                new[] { SpatialContentValidationReason.WorkloadLimitsInvalid },
                SpatialContentValidator.Validate(Valid(),
                    default(SpatialContentValidationWorkloadLimits)).Issues
                    .Select(issue => issue.Reason).ToArray());
        }

        [Test]
        public void MetadataMissingAndMalformed_AreReported()
        {
            var missing = Valid();
            missing.Metadata = null;
            Assert.That(Reasons(missing), Does.Contain(SpatialContentValidationReason.MetadataMissing));

            var invalid = Valid();
            invalid.Metadata = new SpatialContentExportMetadata
            {
                SchemaId = "test gd65a.schema",
                SchemaVersion = 0,
                ContentVersion = " "
            };
            var reasons = Reasons(invalid);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.SchemaIdentityMalformed));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.SchemaVersionNonpositive));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ContentVersionMissing));

            invalid.Metadata.SchemaId = null;
            Assert.That(Reasons(invalid), Does.Contain(SpatialContentValidationReason.SchemaIdentityMissing));
        }

        [Test]
        public void MissingDuplicateAndNullDefinitions_AreReportedInEveryNamespace()
        {
            var catalog = Valid();
            catalog.Floors = new[] { catalog.Floors[0], Clone(catalog).Floors[0], null };
            catalog.Rooms = new[] { catalog.Rooms[0], Clone(catalog).Rooms[0], Room(null, "test.gd65a.loc.missing", "test.gd65a.socket") };
            catalog.Corridors = new[] { catalog.Corridors[0], Clone(catalog).Corridors[0], new CorridorSpatialDefinition() };
            catalog.FixedStructures = new[] { catalog.FixedStructures[0], catalog.FixedStructures[1], Clone(catalog).FixedStructures[0], new FixedSpatialStructureDefinition() };
            catalog.SocketTypes = new[] { catalog.SocketTypes[0], Clone(catalog).SocketTypes[0], null };

            var reasons = Reasons(catalog);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.DefinitionMissing));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.StableIdMissing));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.DuplicateStableId));
        }

        [Test]
        public void InvalidDuplicateTopLevelPayloads_ArePermutationStable()
        {
            var rooms = Valid();
            var firstRoom = Room("test.gd65a.duplicate.room", "test.gd65a.loc.a", "test.gd65a.socket");
            var secondRoom = Room("test.gd65a.duplicate.room", "test.gd65a.loc.b", "test.gd65a.socket");
            firstRoom.MonsterCapacity = -1;
            secondRoom.TrapCapacity = -2;
            rooms.Rooms = new[] { firstRoom, secondRoom };
            AssertPermutationStable(rooms, value => Array.Reverse(value.Rooms));

            var fixedStructures = Valid();
            fixedStructures.FixedStructures = new[]
            {
                Fixed("test.gd65a.duplicate.fixed", FixedSpatialStructureKind.Entrance, "test.gd65a.socket"),
                Fixed("test.gd65a.duplicate.fixed", FixedSpatialStructureKind.CompletionTerminal, "test.gd65a.socket")
            };
            AssertPermutationStable(fixedStructures, value => Array.Reverse(value.FixedStructures));

            var missingIds = Valid();
            var missingA = Room(null, "test.gd65a.loc.a", "test.gd65a.socket");
            var missingB = Room(null, "test.gd65a.loc.b", "test.gd65a.socket");
            missingA.MaximumConnectionCount = 0;
            missingB.MaximumConnectionCount = 1;
            missingIds.Rooms = new[] { missingA, missingB };
            AssertPermutationStable(missingIds, value => Array.Reverse(value.Rooms));

            var sockets = Valid();
            sockets.SocketTypes = new[]
            {
                new SpatialSocketTypeDefinition
                {
                    SocketTypeId = "test.gd65a.duplicate.socket",
                    CompatibleSocketTypeIds = new[] { "test.gd65a.socket.z" }
                },
                new SpatialSocketTypeDefinition
                {
                    SocketTypeId = "test.gd65a.duplicate.socket",
                    CompatibleSocketTypeIds = new[] { "test.gd65a.socket.a" }
                }
            };
            AssertPermutationStable(sockets, value => Array.Reverse(value.SocketTypes));
        }

        [Test]
        public void FloorIndexesCapacitiesAndBranchAllowances_ValidateBoundaries()
        {
            var catalog = Valid();
            var floor = catalog.Floors[0];
            floor.FloorIndex = -1;
            floor.FinalFloorSpaceCapacity = -1;
            floor.OptionalBranchAllowance = -1;
            var negative = Reasons(catalog);
            Assert.That(negative, Does.Contain(SpatialContentValidationReason.FloorIndexNegative));
            Assert.That(negative, Does.Contain(SpatialContentValidationReason.FloorCapacityNegative));
            Assert.That(negative, Does.Contain(SpatialContentValidationReason.FloorBranchAllowanceNegative));

            floor.FloorIndex = 0;
            floor.FinalFloorSpaceCapacity = 16;
            floor.OptionalBranchAllowance = 0;
            Assert.That(Validate(catalog).IsValid, Is.True);
            floor.OptionalBranchAllowance = 1;
            Assert.That(Validate(catalog).IsValid, Is.True);
            floor.OptionalBranchAllowance = 2;
            Assert.That(Validate(catalog).IsValid, Is.True);
            Assert.That(Reasons(catalog), Does.Not.Contain(SpatialContentValidationReason.FloorBranchAllowanceExceeded));
            floor.OptionalBranchAllowance = 37;
            Assert.That(Validate(catalog).IsValid, Is.True);
            Assert.That(Reasons(catalog), Does.Not.Contain(SpatialContentValidationReason.FloorBranchAllowanceExceeded));
            floor.FinalFloorSpaceCapacity = 17;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FloorCapacityExceedsBounds));
        }

        [Test]
        public void BranchAllowanceAboveOne_SurvivesJsonAndCanonicalizationWithoutMutation()
        {
            var source = Valid();
            source.Floors[0].OptionalBranchAllowance = 37;
            string json = JsonUtility.ToJson(source);

            SpatialContentCatalog roundTrip = JsonUtility.FromJson<SpatialContentCatalog>(json);
            Assert.That(roundTrip.Floors[0].OptionalBranchAllowance, Is.EqualTo(37));
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(
                roundTrip, Limits(), out SpatialContentCatalog canonical), Is.True);
            Assert.That(canonical.Floors[0].OptionalBranchAllowance, Is.EqualTo(37));
            Assert.That(roundTrip.Floors[0].OptionalBranchAllowance, Is.EqualTo(37));
            Assert.That(Validate(canonical).IsValid, Is.True);
        }

        [Test]
        public void FloorBoundsAndDuplicateIndexes_AreValidated()
        {
            var catalog = Valid();
            var duplicate = Clone(catalog).Floors[0];
            duplicate.FloorDefinitionId = "test.gd65a.floor.other";
            catalog.Floors = new[] { catalog.Floors[0], duplicate };
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.DuplicateFloorIndex));

            catalog.Floors = new[] { catalog.Floors[0] };
            catalog.Floors[0].Bounds = null;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FootprintMissing));
            catalog.Floors[0].Bounds = new RectangularFloorBounds(default, 0, 1);
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FootprintDimensionsInvalid));
            catalog.Floors[0].Bounds = new RectangularFloorBounds(default, 20, 20);
            Assert.That(Reasons(catalog, Limits(tiles: 256)), Does.Contain(SpatialContentValidationReason.FootprintTileCountExceeded));
        }

        [Test]
        public void MissingAndAmbiguousForeignKeys_AreReportedWithoutSelectingFixedKind()
        {
            var missing = Valid();
            missing.Floors[0].AllowedRoomDefinitionIds = new[] { "test.gd65a.missing.room" };
            Assert.That(Reasons(missing), Does.Contain(SpatialContentValidationReason.ForeignKeyMissing));

            var ambiguous = Valid();
            var entrance = ambiguous.FixedStructures[0];
            var conflicting = Clone(ambiguous).FixedStructures[0];
            conflicting.Kind = FixedSpatialStructureKind.CompletionTerminal;
            ambiguous.FixedStructures = new[] { entrance, conflicting, ambiguous.FixedStructures[1] };
            SpatialContentValidationIssue[] issues = Validate(ambiguous).Issues;
            Assert.That(issues.Select(value => value.Reason), Does.Contain(SpatialContentValidationReason.ForeignKeyAmbiguous));
            Assert.That(issues.Any(value => value.Path == "floors[0].entrance" &&
                value.Reason == SpatialContentValidationReason.FixedStructureKindInvalid), Is.False);
            AssertPermutationStable(ambiguous, value => Array.Reverse(value.FixedStructures));
        }

        [Test]
        public void FootprintsReservedTilesAndOrientations_AreValidatedIndependently()
        {
            var catalog = Valid();
            RoomSpatialDefinition room = catalog.Rooms[0];
            room.GrossFootprint = null;
            room.ReservedTileOffsets = new[] { new TileCoordinate(-1, 0), new TileCoordinate(-1, 0) };
            room.AllowedOrientations = new[]
            {
                CardinalOrientation.Zero, CardinalOrientation.Zero, (CardinalOrientation)99
            };
            room.ConnectionPoints[0].ConnectionPointId = null;
            room.ConnectionPoints[0].SocketTypeId = "test.gd65a.missing.socket";
            var reasons = Reasons(catalog);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.FootprintMissing));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ReservedTileDuplicate));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.OrientationDuplicate));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.OrientationInvalid));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.StableIdMissing));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ForeignKeyMissing));

            room.GrossFootprint = new RectangularFootprintDefinition(2, 2);
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.ReservedTileOutsideFootprint));
            room.GrossFootprint = new RectangularFootprintDefinition(int.MaxValue, int.MaxValue);
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FootprintTileCountExceeded));
            room.GrossFootprint = new RectangularFootprintDefinition(0, 1);
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FootprintDimensionsInvalid));
            room.AllowedOrientations = Array.Empty<CardinalOrientation>();
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.OrientationSetMissing));
        }

        [Test]
        public void CapacitiesAndCorridorStaticRules_AreValidated()
        {
            var catalog = Valid();
            catalog.Rooms[0].LootCapacity = -1;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.CapacityNegative));

            catalog.Rooms[0].LootCapacity = 0;
            catalog.Corridors[0].MonsterCapacity = 1;
            catalog.Corridors[0].TrapCapacity = 1;
            catalog.Corridors[0].LootCapacity = 1;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.CorridorMonsterCapacityInvalid));
            catalog.Corridors[0].MonsterCapacity = 0;
            Assert.That(Validate(catalog).IsValid, Is.True);
        }

        [Test]
        public void CorridorEnumsLengthWidthAndCompatibility_AreValidated()
        {
            var catalog = Valid();
            CorridorSpatialDefinition corridor = catalog.Corridors[0];
            corridor.Category = (CorridorSpatialCategory)99;
            corridor.MinimumLength = 3;
            corridor.MaximumLength = 2;
            corridor.Width = 0;
            corridor.CompatibleSocketTypeIds = new[] { "test.gd65a.missing.socket" };
            var reasons = Reasons(catalog);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.UnknownEnumValue));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.CorridorLengthInvalid));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.CorridorWidthInvalid));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ForeignKeyMissing));
        }

        [Test]
        public void ConnectionPointIdentityGeometryFacingAndCounts_AreValidated()
        {
            var catalog = Valid();
            RoomSpatialDefinition room = catalog.Rooms[0];
            room.GrossFootprint = new RectangularFootprintDefinition(3, 3);
            room.ConnectionPoints = new[]
            {
                Point("test.gd65a.point.same", 1, 1, CardinalOrientation.Zero, "test.gd65a.socket"),
                Point("test.gd65a.point.same", 1, 1, CardinalOrientation.OneEighty, "test.gd65a.socket")
            };
            room.MaximumConnectionCount = 2;
            var reasons = Reasons(catalog);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ConnectionPointIdDuplicate));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ConnectionPointPositionDuplicate));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ConnectionPointBoundaryInvalid));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.MaximumConnectionsExceedPoints));

            room.ConnectionPoints = new[]
            {
                Point("test.gd65a.point.outside", 4, 0, CardinalOrientation.Zero, "test.gd65a.socket")
            };
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.ConnectionPointOffsetInvalid));
            room.ConnectionPoints[0] = Point("test.gd65a.point.facing", 0, 0, CardinalOrientation.Zero, "test.gd65a.socket");
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.ConnectionPointFacingInvalid));
            room.ReservedTileOffsets = new[] { new TileCoordinate(0, 0) };
            room.ConnectionPoints[0].Facing = CardinalOrientation.OneEighty;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.ConnectionPointOnReservedTile));
        }

        [Test]
        public void EmptyPointsAndNegativeMaximumConnections_AreValidated()
        {
            var catalog = Valid();
            catalog.Rooms[0].ConnectionPoints = Array.Empty<SpatialConnectionPointDefinition>();
            catalog.Rooms[0].MaximumConnectionCount = -1;
            var reasons = Reasons(catalog);
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.ConnectionPointSetMissing));
            Assert.That(reasons, Does.Contain(SpatialContentValidationReason.MaximumConnectionsNegative));

            catalog.FixedStructures[0].Kind = (FixedSpatialStructureKind)99;
            Assert.That(Reasons(catalog), Does.Contain(SpatialContentValidationReason.FixedStructureKindInvalid));
        }

        [Test]
        public void MissingAndAmbiguousSocketReferences_AreValidated()
        {
            var missing = Valid();
            missing.Rooms[0].ConnectionPoints[0].SocketTypeId = "test.gd65a.missing.socket";
            Assert.That(Reasons(missing), Does.Contain(SpatialContentValidationReason.ForeignKeyMissing));

            var ambiguous = Valid();
            ambiguous.SocketTypes = new[] { ambiguous.SocketTypes[0], Clone(ambiguous).SocketTypes[0] };
            Assert.That(Reasons(ambiguous), Does.Contain(SpatialContentValidationReason.ForeignKeyAmbiguous));
        }

        [Test]
        public void LocalizationKeysAreRequiredAndOrdinalWhenLookupIsSupplied()
        {
            var blank = Valid();
            blank.Rooms[0].LocalizationKey = " ";
            Assert.That(Reasons(blank), Does.Contain(SpatialContentValidationReason.LocalizationKeyMissing));

            var catalog = Valid();
            var keys = AllLocalizationKeys(catalog);
            Assert.That(Validate(catalog, localizationKeys: keys).IsValid, Is.True);
            keys.Remove(catalog.Rooms[0].LocalizationKey);
            Assert.That(Reasons(catalog, localizationKeys: keys),
                Does.Contain(SpatialContentValidationReason.LocalizationReferenceMissing));
            keys.Add(catalog.Rooms[0].LocalizationKey.ToUpperInvariant());
            Assert.That(Reasons(catalog, localizationKeys: keys),
                Does.Contain(SpatialContentValidationReason.LocalizationReferenceMissing));
        }

        [Test]
        public void LocalizationLookupHandlesNullAndIsWorkloadBoundOncePerCall()
        {
            var catalog = Valid();
            HashSet<string> keys = AllLocalizationKeys(catalog);
            keys.Add(null);
            Assert.That(Reasons(catalog, localizationKeys: keys),
                Does.Contain(SpatialContentValidationReason.LocalizationLookupEntryMissing));

            Assert.That(Reasons(catalog, Limits(nested: 16), keys),
                Does.Contain(SpatialContentValidationReason.LocalizationLookupEntryMissing));
            CollectionAssert.AreEqual(
                new[] { SpatialContentValidationReason.WorkloadExceeded },
                Reasons(catalog, Limits(nested: 15), keys));

            catalog.Rooms = new[]
            {
                catalog.Rooms[0],
                Room("test.gd65a.room.second", "test.gd65a.loc.room.second", "test.gd65a.socket")
            };
            keys.Add("test.gd65a.loc.room.second");
            Assert.That(Validate(catalog, localizationKeys: keys).Issues.Count(issue =>
                issue.Reason == SpatialContentValidationReason.LocalizationLookupEntryMissing), Is.EqualTo(1));
        }

        [Test]
        public void WorkloadAndDiagnosticLimitsHaveExactBoundaries()
        {
            var catalog = Valid();
            Assert.That(Validate(catalog, Limits(top: 6)).IsValid, Is.True);
            CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.WorkloadExceeded },
                Reasons(catalog, Limits(top: 5)));
            CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.WorkloadExceeded },
                Reasons(catalog, Limits(characters: 1)));

            catalog.Rooms[0].LocalizationKey = null;
            Assert.That(Reasons(catalog, Limits(issues: 1)),
                Is.EqualTo(new[] { SpatialContentValidationReason.LocalizationKeyMissing }));
            catalog.Corridors[0].LocalizationKey = null;
            CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.DiagnosticLimitExceeded },
                Reasons(catalog, Limits(issues: 1)));
        }

        [Test]
        public void CumulativeNestedAndCharacterLimitsFailClosedAtExactBoundaries()
        {
            SpatialContentCatalog catalog = Valid();
            HashSet<string> localizationKeys = AllLocalizationKeys(catalog);
            int nestedCount = CountNestedRecords(catalog) + localizationKeys.Count;
            int characterCount = CountAuthoredCharacters(catalog) +
                localizationKeys.Sum(value => value?.Length ?? 0);

            Assert.That(Validate(catalog,
                Limits(nested: nestedCount, characters: characterCount), localizationKeys).IsValid, Is.True);
            CollectionAssert.AreEqual(
                new[] { SpatialContentValidationReason.WorkloadExceeded },
                Reasons(catalog, Limits(nested: nestedCount - 1, characters: characterCount),
                    localizationKeys));
            CollectionAssert.AreEqual(
                new[] { SpatialContentValidationReason.WorkloadExceeded },
                Reasons(catalog, Limits(nested: nestedCount, characters: characterCount - 1),
                    localizationKeys));

            Assert.That(nestedCount, Is.GreaterThan(localizationKeys.Count));
            Assert.That(characterCount, Is.GreaterThan(
                localizationKeys.Sum(value => value?.Length ?? 0)));
        }

        [Test]
        public void CanonicalizationIsDetachedStableAndRoundTripIdempotent()
        {
            var source = Valid();
            Array.Reverse(source.FixedStructures);
            string before = JsonUtility.ToJson(source);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source, Limits(), out SpatialContentCatalog first), Is.True);
            string canonicalJson = JsonUtility.ToJson(first);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
            Assert.That(first, Is.Not.SameAs(source));

            SpatialContentCatalog roundTrip = JsonUtility.FromJson<SpatialContentCatalog>(canonicalJson);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(roundTrip, Limits(), out SpatialContentCatalog second), Is.True);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(canonicalJson));
        }

        [Test]
        public void CanonicalizationPreservesBoundedInvalidPayloadAndNullCollections()
        {
            var catalog = Valid();
            catalog.Rooms[0].MonsterCapacity = -1;
            catalog.Rooms[0].ReservedTileOffsets = null;
            SpatialContentValidationReason[] before = Reasons(catalog);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(catalog, Limits(), out SpatialContentCatalog canonical), Is.True);
            CollectionAssert.AreEqual(before, Reasons(canonical));
            Assert.That(canonical.Rooms[0].ReservedTileOffsets, Is.Null);
        }

        [Test]
        public void DetachedCopyPreservesCompleteNullTopologyAndStableReasons()
        {
            SpatialContentCatalog source = Valid();
            source.Metadata = null;
            source.Floors[0].Bounds = null;
            source.Floors = new[] { source.Floors[0], null };
            source.Rooms[0].GrossFootprint = null;
            source.Rooms[0].ReservedTileOffsets = null;
            source.Rooms[0].AllowedOrientations = Array.Empty<CardinalOrientation>();
            source.Rooms[0].ConnectionPoints = new SpatialConnectionPointDefinition[] { null };
            source.FixedStructures[0].GrossFootprint = null;
            source.FixedStructures[0].ReservedTileOffsets = Array.Empty<TileCoordinate>();
            source.FixedStructures[0].ConnectionPoints = null;
            source.Corridors[0].AllowedOrientations = null;
            source.Corridors[0].CompatibleSocketTypeIds = new[] { "test.gd65a.socket", null };
            source.SocketTypes[0].CompatibleSocketTypeIds = Array.Empty<string>();

            SpatialContentValidationReason[] reasonsBefore = Reasons(source);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(
                source, Limits(), out SpatialContentCatalog canonical), Is.True);

            Assert.That(canonical.Metadata, Is.Null);
            Assert.That(canonical.Floors.Any(value => value == null), Is.True);
            Assert.That(canonical.Floors.Single(value => value != null).Bounds, Is.Null);
            Assert.That(canonical.Rooms[0].GrossFootprint, Is.Null);
            Assert.That(canonical.Rooms[0].ReservedTileOffsets, Is.Null);
            Assert.That(canonical.Rooms[0].AllowedOrientations, Is.Empty);
            Assert.That(canonical.Rooms[0].ConnectionPoints.Single(), Is.Null);
            Assert.That(canonical.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.Entrance).GrossFootprint, Is.Null);
            Assert.That(canonical.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.Entrance).ReservedTileOffsets, Is.Empty);
            Assert.That(canonical.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.Entrance).ConnectionPoints, Is.Null);
            Assert.That(canonical.Corridors[0].AllowedOrientations, Is.Null);
            Assert.That(canonical.Corridors[0].CompatibleSocketTypeIds[0], Is.Null);
            Assert.That(canonical.SocketTypes[0].CompatibleSocketTypeIds, Is.Empty);
            CollectionAssert.AreEqual(reasonsBefore, Reasons(canonical));

            Assert.That(source.Metadata, Is.Null);
            Assert.That(source.Floors[1], Is.Null);
            Assert.That(source.Rooms[0].ReservedTileOffsets, Is.Null);
            Assert.That(source.Corridors[0].CompatibleSocketTypeIds[1], Is.Null);

            Assert.That(reasonsBefore, Does.Contain(SpatialContentValidationReason.MetadataMissing));
            Assert.That(reasonsBefore, Does.Contain(SpatialContentValidationReason.FootprintMissing));
            Assert.That(reasonsBefore, Does.Contain(SpatialContentValidationReason.DefinitionMissing));
            Assert.That(
                reasonsBefore.Contains(SpatialContentValidationReason.SchemaIdentityMissing),
                Is.False);
        }

        [Test]
        public void DetachedCopyPreservesNullTopLevelCollectionsAndEmptyTopLevelCollections()
        {
            SpatialContentCatalog source = Valid();
            source.Floors = null;
            source.Rooms = Array.Empty<RoomSpatialDefinition>();
            source.Corridors = null;
            source.FixedStructures = Array.Empty<FixedSpatialStructureDefinition>();
            source.SocketTypes = null;

            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(
                source, Limits(), out SpatialContentCatalog canonical), Is.True);
            Assert.That(canonical.Floors, Is.Null);
            Assert.That(canonical.Rooms, Is.Empty);
            Assert.That(canonical.Corridors, Is.Null);
            Assert.That(canonical.FixedStructures, Is.Empty);
            Assert.That(canonical.SocketTypes, Is.Null);
            Assert.That(source.Floors, Is.Null);
            Assert.That(source.Rooms, Is.Empty);
        }

        [Test]
        public void DuplicateRoomPayloadSortingUsesScratchCopiesAndPreservesNullPoints()
        {
            const string duplicateId = "test.gd65a.room.duplicate.null-point";
            RoomSpatialDefinition nullPointRoom = Room(
                duplicateId, "test.gd65a.loc.room.null-point", "test.gd65a.socket");
            nullPointRoom.ConnectionPoints = new SpatialConnectionPointDefinition[] { null };
            RoomSpatialDefinition defaultPointRoom = Room(
                duplicateId, "test.gd65a.loc.room.default-point", "test.gd65a.socket");
            defaultPointRoom.ConnectionPoints = new[] { new SpatialConnectionPointDefinition() };

            SpatialContentCatalog forward = Valid();
            forward.Rooms = new[] { nullPointRoom, defaultPointRoom };
            SpatialContentCatalog reverse = Valid();
            RoomSpatialDefinition reverseNullPointRoom = Room(
                duplicateId, "test.gd65a.loc.room.null-point", "test.gd65a.socket");
            reverseNullPointRoom.ConnectionPoints = new SpatialConnectionPointDefinition[] { null };
            RoomSpatialDefinition reverseDefaultPointRoom = Room(
                duplicateId, "test.gd65a.loc.room.default-point", "test.gd65a.socket");
            reverseDefaultPointRoom.ConnectionPoints = new[] { new SpatialConnectionPointDefinition() };
            reverse.Rooms = new[] { reverseDefaultPointRoom, reverseNullPointRoom };

            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(
                forward, Limits(), out SpatialContentCatalog forwardCanonical), Is.True);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(
                reverse, Limits(), out SpatialContentCatalog reverseCanonical), Is.True);

            CollectionAssert.AreEqual(
                forwardCanonical.Rooms.Select(value => value.LocalizationKey),
                reverseCanonical.Rooms.Select(value => value.LocalizationKey));
            Assert.That(forwardCanonical.Rooms[0].ConnectionPoints.Single(), Is.Null);
            Assert.That(reverseCanonical.Rooms[0].ConnectionPoints.Single(), Is.Null);
            Assert.That(forwardCanonical.Rooms[1].ConnectionPoints.Single(), Is.Not.Null);
            Assert.That(forwardCanonical.Rooms[1].ConnectionPoints.Single(),
                Is.Not.SameAs(defaultPointRoom.ConnectionPoints.Single()));
            Assert.That(nullPointRoom.ConnectionPoints.Single(), Is.Null);
            Assert.That(reverseNullPointRoom.ConnectionPoints.Single(), Is.Null);
            Assert.That(defaultPointRoom.ConnectionPoints.Single(), Is.Not.Null);
        }

        [Test]
        public void ExactContentAndGd64ReasonMapsArePreserved()
        {
            var expected = new Dictionary<SpatialContentValidationReason, int>
            {
                { SpatialContentValidationReason.CatalogMissing, 1 },
                { SpatialContentValidationReason.WorkloadLimitsInvalid, 2 },
                { SpatialContentValidationReason.WorkloadExceeded, 3 },
                { SpatialContentValidationReason.DiagnosticLimitExceeded, 4 },
                { SpatialContentValidationReason.MetadataMissing, 5 },
                { SpatialContentValidationReason.SchemaIdentityMissing, 6 },
                { SpatialContentValidationReason.SchemaIdentityMalformed, 7 },
                { SpatialContentValidationReason.SchemaVersionNonpositive, 8 },
                { SpatialContentValidationReason.ContentVersionMissing, 9 },
                { SpatialContentValidationReason.DefinitionMissing, 10 },
                { SpatialContentValidationReason.StableIdMissing, 11 },
                { SpatialContentValidationReason.DuplicateStableId, 12 },
                { SpatialContentValidationReason.DuplicateFloorIndex, 13 },
                { SpatialContentValidationReason.UnknownEnumValue, 14 },
                { SpatialContentValidationReason.FootprintMissing, 15 },
                { SpatialContentValidationReason.FootprintDimensionsInvalid, 16 },
                { SpatialContentValidationReason.FootprintTileCountExceeded, 17 },
                { SpatialContentValidationReason.ReservedTileDuplicate, 18 },
                { SpatialContentValidationReason.ReservedTileOutsideFootprint, 19 },
                { SpatialContentValidationReason.OrientationSetMissing, 20 },
                { SpatialContentValidationReason.OrientationDuplicate, 21 },
                { SpatialContentValidationReason.OrientationInvalid, 22 },
                { SpatialContentValidationReason.CapacityNegative, 23 },
                { SpatialContentValidationReason.MaximumConnectionsNegative, 24 },
                { SpatialContentValidationReason.MaximumConnectionsExceedPoints, 25 },
                { SpatialContentValidationReason.ConnectionPointSetMissing, 26 },
                { SpatialContentValidationReason.ConnectionPointIdDuplicate, 27 },
                { SpatialContentValidationReason.ConnectionPointOffsetInvalid, 28 },
                { SpatialContentValidationReason.ConnectionPointBoundaryInvalid, 29 },
                { SpatialContentValidationReason.ConnectionPointFacingInvalid, 30 },
                { SpatialContentValidationReason.ConnectionPointOnReservedTile, 31 },
                { SpatialContentValidationReason.ConnectionPointPositionDuplicate, 32 },
                { SpatialContentValidationReason.ForeignKeyMissing, 33 },
                { SpatialContentValidationReason.ForeignKeyAmbiguous, 34 },
                { SpatialContentValidationReason.CorridorLengthInvalid, 35 },
                { SpatialContentValidationReason.CorridorWidthInvalid, 36 },
                { SpatialContentValidationReason.CorridorMonsterCapacityInvalid, 37 },
                { SpatialContentValidationReason.FixedStructureKindInvalid, 38 },
                { SpatialContentValidationReason.LocalizationKeyMissing, 39 },
                { SpatialContentValidationReason.LocalizationReferenceMissing, 40 },
                { SpatialContentValidationReason.LocalizationLookupEntryMissing, 41 },
                { SpatialContentValidationReason.FloorIndexNegative, 42 },
                { SpatialContentValidationReason.FloorCapacityNegative, 43 },
                { SpatialContentValidationReason.FloorCapacityExceedsBounds, 44 },
                { SpatialContentValidationReason.FloorBranchAllowanceNegative, 45 },
                { SpatialContentValidationReason.FloorBranchAllowanceExceeded, 46 }
            };
            CollectionAssert.AreEquivalent(expected.Keys, Enum.GetValues(typeof(SpatialContentValidationReason)));
            foreach (KeyValuePair<SpatialContentValidationReason, int> pair in expected)
                Assert.That((int)pair.Key, Is.EqualTo(pair.Value));

            Assert.That((int)FloorRouteConnectionKind.DirectDoorway, Is.EqualTo(1));
            Assert.That((int)FloorRouteConnectionKind.PhysicalCorridor, Is.EqualTo(2));
            int[] layoutReasons = Enum.GetValues(typeof(FloorLayoutValidationReason))
                .Cast<FloorLayoutValidationReason>().Select(value => (int)value).ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, 45), layoutReasons);
        }

        private static HashSet<string> AllLocalizationKeys(SpatialContentCatalog catalog) =>
            new HashSet<string>(catalog.Rooms.Select(value => value.LocalizationKey)
                .Concat(catalog.Corridors.Select(value => value.LocalizationKey))
                .Concat(catalog.FixedStructures.Select(value => value.LocalizationKey)),
                StringComparer.Ordinal);

        private static int CountNestedRecords(SpatialContentCatalog catalog) =>
            catalog.Floors.Sum(value => Length(value.AllowedRoomDefinitionIds) +
                Length(value.AllowedCorridorDefinitionIds)) +
            catalog.Rooms.Sum(value => Length(value.ReservedTileOffsets) +
                Length(value.AllowedOrientations) + Length(value.ConnectionPoints)) +
            catalog.Corridors.Sum(value => Length(value.AllowedOrientations) +
                Length(value.CompatibleSocketTypeIds)) +
            catalog.FixedStructures.Sum(value => Length(value.ReservedTileOffsets) +
                Length(value.AllowedOrientations) + Length(value.ConnectionPoints)) +
            catalog.SocketTypes.Sum(value => Length(value.CompatibleSocketTypeIds));

        private static int CountAuthoredCharacters(SpatialContentCatalog catalog) =>
            StringLength(catalog.Metadata.SchemaId) + StringLength(catalog.Metadata.ContentVersion) +
            catalog.Floors.Sum(value => StringLength(value.FloorDefinitionId) +
                StringLength(value.EntranceStructureDefinitionId) +
                StringLength(value.CompletionStructureDefinitionId) +
                StringLengths(value.AllowedRoomDefinitionIds) +
                StringLengths(value.AllowedCorridorDefinitionIds)) +
            catalog.Rooms.Sum(value => StringLength(value.RoomDefinitionId) +
                StringLength(value.LocalizationKey) + PointStringLengths(value.ConnectionPoints)) +
            catalog.Corridors.Sum(value => StringLength(value.CorridorDefinitionId) +
                StringLength(value.LocalizationKey) + StringLengths(value.CompatibleSocketTypeIds)) +
            catalog.FixedStructures.Sum(value => StringLength(value.StructureDefinitionId) +
                StringLength(value.LocalizationKey) + PointStringLengths(value.ConnectionPoints)) +
            catalog.SocketTypes.Sum(value => StringLength(value.SocketTypeId) +
                StringLengths(value.CompatibleSocketTypeIds));

        private static int PointStringLengths(SpatialConnectionPointDefinition[] values) =>
            values?.Sum(value => StringLength(value?.ConnectionPointId) +
                StringLength(value?.SocketTypeId)) ?? 0;

        private static int StringLengths(string[] values) =>
            values?.Sum(StringLength) ?? 0;

        private static int StringLength(string value) => value?.Length ?? 0;
        private static int Length(Array values) => values?.Length ?? 0;

        private static void AssertPermutationStable(
            SpatialContentCatalog original,
            Action<SpatialContentCatalog> permute)
        {
            SpatialContentCatalog permutation = Clone(original);
            permute(permutation);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(original, Limits(), out SpatialContentCatalog first), Is.True);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(permutation, Limits(), out SpatialContentCatalog second), Is.True);
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            CollectionAssert.AreEqual(CompleteDiagnostics(original), CompleteDiagnostics(permutation));
        }
    }
}
#endif
