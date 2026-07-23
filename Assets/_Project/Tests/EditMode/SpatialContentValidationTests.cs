#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SpatialContentValidationTests
    {
        private static SpatialContentValidationWorkloadLimits Limits(int top = 50, int nested = 100, int tiles = 100, int issues = 1000) =>
            new SpatialContentValidationWorkloadLimits(top, nested, tiles, issues);

        private static SpatialContentCatalog Valid()
        {
            const string socket = "test.gd65a.socket";
            const string room = "test.gd65a.room";
            const string corridor = "test.gd65a.corridor";
            const string entrance = "test.gd65a.entrance";
            const string completion = "test.gd65a.completion";
            var point = new SpatialConnectionPointDefinition { ConnectionPointId = "test.gd65a.point", Offset = new TileCoordinate(0, 0), Facing = CardinalOrientation.OneEighty, SocketTypeId = socket };
            return new SpatialContentCatalog {
                Metadata = new SpatialContentExportMetadata { SchemaId = "test.gd65a.schema", SchemaVersion = 1, ContentVersion = "test.gd65a.version" },
                SocketTypes = new[] { new SpatialSocketTypeDefinition { SocketTypeId = socket, CompatibleSocketTypeIds = new[] { socket } } },
                Rooms = new[] { new RoomSpatialDefinition { RoomDefinitionId = room, LocalizationKey = "test.gd65a.loc.room", GrossFootprint = new RectangularFootprintDefinition(2, 2), AllowedOrientations = new[] { CardinalOrientation.Zero }, ConnectionPoints = new[] { point }, MaximumConnectionCount = 1 } },
                Corridors = new[] { new CorridorSpatialDefinition { CorridorDefinitionId = corridor, LocalizationKey = "test.gd65a.loc.corridor", Category = CorridorSpatialCategory.Straight, MinimumLength = 1, MaximumLength = 2, Width = 1, AllowedOrientations = new[] { CardinalOrientation.Zero }, CompatibleSocketTypeIds = new[] { socket } } },
                FixedStructures = new[] {
                    Fixed(entrance, FixedSpatialStructureKind.Entrance, socket), Fixed(completion, FixedSpatialStructureKind.CompletionTerminal, socket) },
                Floors = new[] { new FloorSpatialConfiguration { FloorDefinitionId = "test.gd65a.floor", FloorIndex = 1, Bounds = new RectangularFloorBounds(default, 4, 4), AllowedRoomDefinitionIds = new[] { room }, AllowedCorridorDefinitionIds = new[] { corridor }, EntranceStructureDefinitionId = entrance, CompletionStructureDefinitionId = completion } }
            };
        }

        private static FixedSpatialStructureDefinition Fixed(string id, FixedSpatialStructureKind kind, string socket) => new FixedSpatialStructureDefinition {
            StructureDefinitionId = id, LocalizationKey = "test.gd65a.loc.fixed", Kind = kind, GrossFootprint = new RectangularFootprintDefinition(1, 1),
            AllowedOrientations = new[] { CardinalOrientation.Zero }, MaximumConnectionCount = 1,
            ConnectionPoints = new[] { new SpatialConnectionPointDefinition { ConnectionPointId = id + ".point", Offset = default, Facing = CardinalOrientation.Zero, SocketTypeId = socket } }
        };

        private static SpatialContentValidationReason[] Reasons(SpatialContentCatalog c, SpatialContentValidationWorkloadLimits? limits = null) =>
            SpatialContentValidator.Validate(c, limits ?? Limits()).Issues.Select(x => x.Reason).ToArray();

        [Test] public void MinimalCatalog_IsValid() => Assert.That(SpatialContentValidator.Validate(Valid(), Limits()).IsValid, Is.True);
        [Test] public void NullCatalog_AndInvalidLimits_FailClosed() { CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.CatalogMissing }, Reasons(null)); CollectionAssert.AreEqual(new[] { SpatialContentValidationReason.WorkloadLimitsInvalid }, Reasons(Valid(), default)); }
        [Test] public void MetadataFailures_AreReported() { var c=Valid();c.Metadata=new SpatialContentExportMetadata();var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.SchemaIdentityMissing));Assert.That(r,Does.Contain(SpatialContentValidationReason.SchemaVersionNonpositive));Assert.That(r,Does.Contain(SpatialContentValidationReason.ContentVersionMissing)); }
        [Test] public void MissingDuplicateAndOrdinalIds_AreDistinct() { var c=Valid();c.Rooms=new[]{c.Rooms[0],new RoomSpatialDefinition{RoomDefinitionId=c.Rooms[0].RoomDefinitionId,GrossFootprint=new RectangularFootprintDefinition(1,1)},new RoomSpatialDefinition{RoomDefinitionId="TEST.GD65A.ROOM",GrossFootprint=new RectangularFootprintDefinition(1,1)},new RoomSpatialDefinition{RoomDefinitionId=" "}};var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.DuplicateStableId));Assert.That(r,Does.Contain(SpatialContentValidationReason.StableIdMissing)); }
        [Test] public void DuplicateFloorIndex_AndMissingForeignKey_AreReported() { var c=Valid();var copy=new FloorSpatialConfiguration{FloorDefinitionId="test.gd65a.floor.other",FloorIndex=1,Bounds=new RectangularFloorBounds(default,1,1),EntranceStructureDefinitionId="missing",CompletionStructureDefinitionId="missing"};c.Floors=new[]{c.Floors[0],copy};var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.DuplicateFloorIndex));Assert.That(r,Does.Contain(SpatialContentValidationReason.ForeignKeyMissing)); }
        [Test] public void AmbiguousForeignKey_IsReported() { var c=Valid();c.SocketTypes=new[]{c.SocketTypes[0],c.SocketTypes[0]};Assert.That(Reasons(c),Does.Contain(SpatialContentValidationReason.ForeignKeyAmbiguous)); }
        [Test] public void GeometryAndOrientationFailures_AreReported() { var c=Valid();var x=c.Rooms[0];x.GrossFootprint=new RectangularFootprintDefinition(int.MaxValue,int.MaxValue);x.ReservedTileOffsets=new[]{new TileCoordinate(-1,0),new TileCoordinate(-1,0)};x.AllowedOrientations=new[]{CardinalOrientation.Zero,CardinalOrientation.Zero,(CardinalOrientation)99};x.ConnectionPoints[0].Offset=new TileCoordinate(1,1);var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.FootprintTileCountExceeded));Assert.That(r,Does.Contain(SpatialContentValidationReason.ReservedTileOutsideFootprint));Assert.That(r,Does.Contain(SpatialContentValidationReason.ReservedTileDuplicate));Assert.That(r,Does.Contain(SpatialContentValidationReason.OrientationDuplicate));Assert.That(r,Does.Contain(SpatialContentValidationReason.OrientationInvalid));Assert.That(r,Does.Contain(SpatialContentValidationReason.ConnectionPointBoundaryInvalid)); }
        [Test] public void MissingFootprintNegativeCapacitiesAndConnections_AreReported() { var c=Valid();c.Rooms[0].GrossFootprint=null;c.Rooms[0].MonsterCapacity=-1;c.Rooms[0].MaximumConnectionCount=-1;var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.FootprintMissing));Assert.That(r,Does.Contain(SpatialContentValidationReason.CapacityNegative));Assert.That(r,Does.Contain(SpatialContentValidationReason.MaximumConnectionsNegative)); }
        [Test] public void ConnectionAndSocketFailures_AreReported() { var c=Valid();var p=c.Rooms[0].ConnectionPoints[0];c.Rooms[0].ConnectionPoints=new[]{p,p};p.SocketTypeId="missing";var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.ConnectionPointIdDuplicate));Assert.That(r,Does.Contain(SpatialContentValidationReason.ForeignKeyMissing)); }
        [Test] public void CorridorAndFixedEnumsAndRanges_AreReported() { var c=Valid();c.Corridors[0].Category=(CorridorSpatialCategory)99;c.Corridors[0].MinimumLength=3;c.Corridors[0].MaximumLength=2;c.Corridors[0].Width=0;c.FixedStructures[0].Kind=(FixedSpatialStructureKind)99;var r=Reasons(c);Assert.That(r,Does.Contain(SpatialContentValidationReason.UnknownEnumValue));Assert.That(r,Does.Contain(SpatialContentValidationReason.CorridorLengthInvalid));Assert.That(r,Does.Contain(SpatialContentValidationReason.CorridorWidthInvalid));Assert.That(r,Does.Contain(SpatialContentValidationReason.FixedStructureKindInvalid)); }
        [Test] public void WorkloadLimitsRejectBeforeDiagnostics() { var c=Valid();CollectionAssert.AreEqual(new[]{SpatialContentValidationReason.WorkloadExceeded},Reasons(c,Limits(1)));CollectionAssert.AreEqual(new[]{SpatialContentValidationReason.WorkloadExceeded},Reasons(c,Limits(50,1))); }
        [Test] public void ReasonValues_AreStable() { Assert.That((int)SpatialContentValidationReason.CatalogMissing,Is.EqualTo(1));Assert.That((int)SpatialContentValidationReason.DiagnosticLimitExceeded,Is.EqualTo(33));Assert.That(Enum.GetValues(typeof(SpatialContentValidationReason)).Cast<SpatialContentValidationReason>().Select(x=>(int)x),Is.EqualTo(Enumerable.Range(1,33))); }

        [Test] public void Canonicalization_IsDetachedStableAndRoundTripIdempotent()
        {
            var source=Valid();source.FixedStructures=source.FixedStructures.Reverse().ToArray();string before=JsonUtility.ToJson(source);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(source,Limits(),out var first),Is.True);
            string json=JsonUtility.ToJson(first);Assert.That(JsonUtility.ToJson(source),Is.EqualTo(before));Assert.That(first,Is.Not.SameAs(source));
            var round=JsonUtility.FromJson<SpatialContentCatalog>(json);Assert.That(SpatialContentCanonicalizer.TryCanonicalize(round,Limits(),out var second),Is.True);Assert.That(JsonUtility.ToJson(second),Is.EqualTo(json));
            var permutation=Valid();Assert.That(SpatialContentCanonicalizer.TryCanonicalize(permutation,Limits(),out var third),Is.True);Assert.That(JsonUtility.ToJson(third),Is.EqualTo(json));
        }

        [Test] public void CanonicalizationPreservesBoundedInvalidPayloadAndNullCollections()
        {
            var c=Valid();c.Rooms[0].MonsterCapacity=-1;c.Rooms[0].ReservedTileOffsets=null;var before=Reasons(c);
            Assert.That(SpatialContentCanonicalizer.TryCanonicalize(c,Limits(),out var canonical),Is.True);CollectionAssert.AreEqual(before,Reasons(canonical));Assert.That(canonical.Rooms[0].ReservedTileOffsets,Is.Null);
        }

        [Test] public void Diagnostics_AreDeterministicAcrossInputPermutation()
        {
            var a=Valid();a.Rooms[0].MonsterCapacity=-1;a.Corridors[0].Width=0;var b=JsonUtility.FromJson<SpatialContentCatalog>(JsonUtility.ToJson(a));Array.Reverse(b.FixedStructures);
            var left=SpatialContentValidator.Validate(a,Limits()).Issues.Select(x=>x.Reason+":"+x.Path).ToArray();var right=SpatialContentValidator.Validate(b,Limits()).Issues.Select(x=>x.Reason+":"+x.Path).ToArray();CollectionAssert.AreEqual(left,right);
        }
    }
}
#endif
