#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    [TestFixture]
    public sealed class RoomContentPositionMigrationProfileTests
    {
        private ProductionSpatialContentSnapshot spatial;
        private SpatialContentValidationWorkloadLimits structuralLimits;
        private SpatialSerializedInputLimits serializedLimits;
        private TextAsset profiles;

        [SetUp]
        public void SetUp()
        {
            const string root = "Assets/_Project/Data/Production/DungeonSpatial/";
            TextAsset limitAsset = Asset(root + "validation_limits.json");
            ProductionSpatialContentWorkloadLimitParseResult parsedLimits =
                ProductionSpatialContentWorkloadLimitParser.Parse(limitAsset);
            Assert.That(parsedLimits.Success, Is.True);
            structuralLimits = parsedLimits.Limits;
            ProductionSpatialContentLoadResult loaded = ProductionSpatialContentLoader.Load(
                Asset(root + "content_manifest.json"), Asset(root + "dungeon_spatial_content.json"),
                new[] { Asset(root + "string_table_en.json") }, limitAsset);
            Assert.That(loaded.Success, Is.True);
            spatial = loaded.Value;
            SaveSpatialMigrationLimitsLoadResult saveLimits = SaveSpatialMigrationLimitsLoader.Load(
                File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath));
            Assert.That(saveLimits.IsSuccess, Is.True, saveLimits.Reason);
            serializedLimits = saveLimits.Profile.Canonical.Serialized;
            profiles = Asset(RoomContentPositionMigrationProfiles.ProductionPath);
        }

        [Test]
        public void ProductionAsset_IsCanonicalFrozenAndCoversExactProductionEnvelope()
        {
            RoomContentPositionMigrationProfilesResult result = Parse(profiles.bytes, true);
            AssertSuccess(result);
            CollectionAssert.AreEqual(profiles.bytes, result.Value.CanonicalBytes);
            RoomContentPositionMigrationProfilesData data = result.Value.Value;
            Assert.That(data.Schema, Is.EqualTo(RoomContentPositionMigrationProfiles.SchemaId));
            Assert.That(data.SchemaVersion, Is.EqualTo(1));
            Assert.That(data.ProfileSetVersion, Is.EqualTo(1));
            Assert.That(data.SourceSaveSchemaVersion, Is.EqualTo(12));
            Assert.That(data.Profiles, Has.Length.EqualTo(5));
            AssertProfile(data, "spatial.room.basic", CardinalOrientation.Zero, 4, 4, 2, 2, 2, 6);
            AssertProfile(data, "spatial.room.rectangle", CardinalOrientation.Zero, 3, 5, 3, 1, 2, 6);
            AssertProfile(data, "spatial.room.rectangle", CardinalOrientation.Ninety, 5, 3, 3, 1, 2, 6);
            AssertProfile(data, "spatial.room.large_chamber", CardinalOrientation.Zero, 5, 6, 4, 4, 4, 12);
            AssertProfile(data, "spatial.room.large_chamber", CardinalOrientation.Ninety, 6, 5, 4, 4, 4, 12);
            foreach (RoomContentPositionMigrationProfile profile in data.Profiles)
                Assert.That(RoomContentPositionMigrationProfiles.ComputeProfileHash(profile),
                    Is.EqualTo(profile.CanonicalHash), profile.ProfileId);
            Assert.That(RoomContentPositionMigrationProfiles.ComputeSetHash(data),
                Is.EqualTo(data.CanonicalHash));
        }

        [Test]
        public void CanonicalAuthoringPipeline_IsDetachedDeterministicAndPermutationInvariant()
        {
            RoomContentPositionMigrationProfilesData source = Data();
            string before = JsonUtility.ToJson(source);
            Array.Reverse(source.Profiles);
            foreach (RoomContentPositionMigrationProfile profile in source.Profiles)
            {
                Array.Reverse(profile.CategoryCapacities);
                Array.Reverse(profile.OrderedSlots);
                foreach (RoomContentMigrationSlot slot in profile.OrderedSlots)
                    Array.Reverse(slot.AllowedCategoryIds);
            }
            RoomContentPositionMigrationProfilesData first =
                RoomContentPositionMigrationProfiles.WithComputedIntegrity(source);
            RoomContentPositionMigrationProfilesData second =
                RoomContentPositionMigrationProfiles.WithComputedIntegrity(source);
            CollectionAssert.AreEqual(RoomContentPositionMigrationProfiles.SerializeCanonical(first),
                RoomContentPositionMigrationProfiles.SerializeCanonical(second));
            CollectionAssert.AreEqual(profiles.bytes,
                RoomContentPositionMigrationProfiles.SerializeCanonical(first));
            Assert.That(before, Is.Not.EqualTo(JsonUtility.ToJson(source)));
            string sourceAfter = JsonUtility.ToJson(source);
            RoomContentPositionMigrationProfiles.Canonicalize(source);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceAfter));
        }

        [TestCase("", RoomContentPositionMigrationProfileDiagnostic.EmptyInput)]
        [TestCase("{}\n", RoomContentPositionMigrationProfileDiagnostic.InvalidJson)]
        [TestCase("{broken\n", RoomContentPositionMigrationProfileDiagnostic.InvalidJson)]
        public void MissingUnreadableAndMalformedInputFailsClosed(string text,
            RoomContentPositionMigrationProfileDiagnostic expected)
        {
            byte[] bytes = text == null ? null : Encoding.UTF8.GetBytes(text);
            RoomContentPositionMigrationProfilesResult result = Parse(bytes);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Does.Contain(expected));
        }

        [Test]
        public void MissingInputFailsClosed()
        {
            RoomContentPositionMigrationProfilesResult result = Parse(null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Does.Contain(
                RoomContentPositionMigrationProfileDiagnostic.MissingInput));
        }

        [Test]
        public void InvalidUtf8NoncanonicalFramingUnexpectedNullAndSizeLimitFailClosed()
        {
            AssertFailure(new byte[] { 0xff, (byte)'\n' },
                RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding);
            AssertFailure(profiles.bytes.Take(profiles.bytes.Length - 1).ToArray(),
                RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding);
            AssertFailure(Encoding.UTF8.GetPreamble().Concat(profiles.bytes).ToArray(),
                RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding);
            byte[] crlf = Encoding.UTF8.GetBytes(profiles.text.Replace("\n", "\r\n"));
            AssertFailure(crlf, RoomContentPositionMigrationProfileDiagnostic.InvalidEncoding);
            byte[] nullProfiles = Encoding.UTF8.GetBytes(profiles.text.Replace("\"Profiles\": [",
                "\"Profiles\": null"));
            Assert.That(Parse(nullProfiles, true).Success, Is.False);
            var tooSmall = new SpatialSerializedInputLimits(profiles.bytes.Length - 1,
                serializedLimits.MaximumParsedNodes, serializedLimits.MaximumCollectionRecords,
                serializedLimits.MaximumStringCharacters, serializedLimits.MaximumDiagnostics);
            RoomContentPositionMigrationProfilesResult limited =
                RoomContentPositionMigrationProfiles.ParseAndValidate(profiles.bytes, spatial,
                    structuralLimits, tooSmall, true);
            Assert.That(limited.Diagnostics, Does.Contain(
                RoomContentPositionMigrationProfileDiagnostic.WorkloadExceeded));
        }

        [Test]
        public void ContractVersionSourceSchemaAndIntegrityMutationsFailClosed()
        {
            AssertSemantic(data => data.SchemaVersion++,
                RoomContentPositionMigrationProfileDiagnostic.InvalidSchema, false);
            AssertSemantic(data => data.ProfileSetVersion++,
                RoomContentPositionMigrationProfileDiagnostic.InvalidVersion, false);
            AssertSemantic(data => data.SourceSaveSchemaVersion++,
                RoomContentPositionMigrationProfileDiagnostic.UnsupportedSourceSchema, false);
            AssertSemantic(data => data.Profiles[0].ProfileVersion++,
                RoomContentPositionMigrationProfileDiagnostic.InvalidVersion);
            AssertSemantic(data => data.Profiles[0].SourceSaveSchemaVersion++,
                RoomContentPositionMigrationProfileDiagnostic.InvalidVersion);
            byte[] changedWithoutHash = (byte[])profiles.bytes.Clone();
            string text = Encoding.UTF8.GetString(changedWithoutHash).Replace(
                "\"X\": 1,\n                        \"Y\": 1",
                "\"X\": 0,\n                        \"Y\": 1");
            AssertFailure(Encoding.UTF8.GetBytes(text),
                RoomContentPositionMigrationProfileDiagnostic.InvalidHash);
        }

        [Test]
        public void DuplicateUnknownCoverageCapacitySlotAndOccupancyStatesFailClosed()
        {
            AssertSemantic(data =>
            {
                RoomContentPositionMigrationProfile copy = Clone(data.Profiles[0]);
                data.Profiles = data.Profiles.Concat(new[] { copy }).ToArray();
            }, RoomContentPositionMigrationProfileDiagnostic.DuplicateProfileId);
            AssertSemantic(data =>
            {
                RoomContentPositionMigrationProfile copy = Clone(data.Profiles[0]);
                copy.ProfileId = "compat.room_content_position.schema_12.duplicate";
                data.Profiles = data.Profiles.Concat(new[] { copy }).ToArray();
            }, RoomContentPositionMigrationProfileDiagnostic.DuplicateRoomOrientation);
            AssertSemantic(data => data.Profiles[0].RoomDefinitionId = "spatial.room.unknown",
                RoomContentPositionMigrationProfileDiagnostic.UnknownRoom);
            AssertSemantic(data => data.Profiles[0].Orientation = CardinalOrientation.Ninety,
                RoomContentPositionMigrationProfileDiagnostic.UnsupportedOrientation);
            AssertSemantic(data => data.Profiles = data.Profiles.Skip(1).ToArray(),
                RoomContentPositionMigrationProfileDiagnostic.MissingProductionCoverage);
            AssertSemantic(data => data.Profiles[0].OrderedSlots =
                    data.Profiles[0].OrderedSlots.Take(5).ToArray(),
                RoomContentPositionMigrationProfileDiagnostic.InsufficientSlots);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[1].SlotId =
                    data.Profiles[0].OrderedSlots[0].SlotId,
                RoomContentPositionMigrationProfileDiagnostic.DuplicateSlotId);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[1].Order = 0,
                RoomContentPositionMigrationProfileDiagnostic.DuplicateSlotOrder);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[0].AllowedCategoryIds =
                    new[] { "placement.category.invalid" },
                RoomContentPositionMigrationProfileDiagnostic.InvalidCategoryCompatibility);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[0].OccupiedTileOffsets =
                    Array.Empty<TileCoordinate>(),
                RoomContentPositionMigrationProfileDiagnostic.InvalidOccupancyFootprint);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[0].Anchor =
                    new TileCoordinate(int.MaxValue, int.MaxValue),
                RoomContentPositionMigrationProfileDiagnostic.OutOfFootprint);
            AssertSemantic(data => data.Profiles[0].OrderedSlots[1].Anchor =
                    data.Profiles[0].OrderedSlots[0].Anchor,
                RoomContentPositionMigrationProfileDiagnostic.SlotOverlap);
            AssertSemantic(data => data.Profiles[0].CategoryCapacities[0].MaximumAssignments++,
                RoomContentPositionMigrationProfileDiagnostic.InvalidCategoryCapacity);
        }

        [Test]
        public void ReservedTilesAndSyntheticMultiTileOccupancyUseExtensibleValidation()
        {
            RoomContentPositionMigrationProfilesData reservedData = SingleBasic();
            reservedData.Profiles[0].FrozenReservedTileOffsets =
                new[] { new TileCoordinate(1, 1) };
            SpatialContentCatalog catalog = spatial.Catalog;
            catalog.Rooms.Single(value => value.RoomDefinitionId == "spatial.room.basic")
                .ReservedTileOffsets = new[] { new TileCoordinate(1, 1) };
            var reservedSpatial = new ProductionSpatialContentSnapshot(spatial.Manifest, catalog,
                spatial.Languages);
            reservedData = RoomContentPositionMigrationProfiles.WithComputedIntegrity(reservedData);
            RoomContentPositionMigrationProfilesResult reserved =
                RoomContentPositionMigrationProfiles.ParseAndValidate(
                    RoomContentPositionMigrationProfiles.SerializeCanonical(reservedData), reservedSpatial,
                    structuralLimits, serializedLimits);
            Assert.That(reserved.Diagnostics, Does.Contain(
                RoomContentPositionMigrationProfileDiagnostic.ReservedTileOverlap));

            RoomContentPositionMigrationProfilesData multi = SingleBasic();
            RoomContentMigrationSlot loot = multi.Profiles[0].OrderedSlots.Single(value =>
                value.SlotId == "loot.00");
            loot.OccupiedTileOffsets = new[] { new TileCoordinate(0, 0), new TileCoordinate(0, 1) };
            multi = RoomContentPositionMigrationProfiles.WithComputedIntegrity(multi);
            AssertSuccess(ParseData(multi));
            Assert.That(ParseData(multi, true).Diagnostics, Does.Contain(
                RoomContentPositionMigrationProfileDiagnostic.UnauthorizedProductionOccupancy));
            multi.Profiles[0].OrderedSlots.Single(value => value.SlotId == "loot.01").Anchor =
                new TileCoordinate(0, 1);
            multi = RoomContentPositionMigrationProfiles.WithComputedIntegrity(multi);
            Assert.That(ParseData(multi).Diagnostics, Does.Contain(
                RoomContentPositionMigrationProfileDiagnostic.SlotOverlap));
        }

        [Test]
        public void MaximumEnvelopesMapDeterministicallyPreserveIdentityAndDoNotMutateSource()
        {
            RoomContentPositionMigrationProfilesSnapshot snapshot = Parse(profiles.bytes, true).Value;
            foreach (RoomContentPositionMigrationProfile profile in snapshot.Value.Profiles)
            {
                Assert.That(snapshot.TryGetProfile(profile.RoomDefinitionId, profile.Orientation,
                    out ValidatedRoomContentPositionMigrationProfile validated), Is.True);
                RoomContentAssignment[] source = MaximumAssignments(profile, "room.instance.01");
                string before = AssignmentsJson(source);
                RoomContentPositionMigrationPlanResult first =
                    RoomContentPositionMigrationPlanner.Plan(validated, "room.instance.01", source);
                RoomContentPositionMigrationPlanResult second =
                    RoomContentPositionMigrationPlanner.Plan(validated, "room.instance.01", source.Reverse());
                Assert.That(first.Success, Is.True, profile.ProfileId);
                Assert.That(first.Entries.Count, Is.EqualTo(profile.OrderedSlots.Length));
                Assert.That(first.Entries.Select(EntryIdentity),
                    Is.EqualTo(second.Entries.Select(EntryIdentity)), profile.ProfileId);
                Assert.That(first.Entries.SelectMany(value => value.OccupiedTiles).Distinct().Count(),
                    Is.EqualTo(first.Entries.SelectMany(value => value.OccupiedTiles).Count()));
                Assert.That(AssignmentsJson(source), Is.EqualTo(before));
                foreach (RoomContentPositionMigrationPlanEntry entry in first.Entries)
                {
                    RoomContentAssignment original = source.Single(value =>
                        value.AssignmentId == entry.AssignmentId);
                    Assert.That(entry.RoomInstanceId, Is.EqualTo(original.RoomInstanceId));
                    Assert.That(entry.CategoryId, Is.EqualTo(original.CategoryId));
                    Assert.That(entry.OptionId, Is.EqualTo(original.OptionId));
                    Assert.That(entry.Sequence, Is.EqualTo(original.Sequence));
                }
            }
        }

        [Test]
        public void PlannerUsesSharedCanonicalAssignmentOrderAndReturnsNoPartialFailure()
        {
            RoomContentPositionMigrationProfilesSnapshot snapshot = Parse(profiles.bytes, true).Value;
            snapshot.TryGetProfile("spatial.room.basic", CardinalOrientation.Zero,
                out ValidatedRoomContentPositionMigrationProfile profile);
            var input = new[]
            {
                Assignment("loot", CanonicalSpatialSaveContracts.LootNodeCategoryId, 1, "z"),
                Assignment("monster-b", CanonicalSpatialSaveContracts.MonsterCategoryId, 2, "b"),
                Assignment("trap", CanonicalSpatialSaveContracts.TrapCategoryId, 0, "t"),
                Assignment("monster-a", CanonicalSpatialSaveContracts.MonsterCategoryId, 1, "a")
            };
            RoomContentAssignment[] expected =
                CanonicalSpatialSaveContracts.CanonicalOrderAssignments(input);
            RoomContentPositionMigrationPlanResult result =
                RoomContentPositionMigrationPlanner.Plan(profile, "room.instance.01", input.Reverse());
            CollectionAssert.AreEqual(expected.Select(value => value.AssignmentId).ToArray(),
                result.Entries.Select(value => value.AssignmentId).ToArray());
            RoomContentAssignment[] over = MaximumAssignments(
                snapshot.Value.Profiles.Single(value => value.RoomDefinitionId == "spatial.room.basic"),
                "room.instance.01").Concat(new[] {
                    Assignment("monster-extra", CanonicalSpatialSaveContracts.MonsterCategoryId, 99, "x")
                }).ToArray();
            RoomContentPositionMigrationPlanResult failed =
                RoomContentPositionMigrationPlanner.Plan(profile, "room.instance.01", over);
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Failure, Is.EqualTo(
                RoomContentPositionMigrationPlanFailure.CapacityExceeded));
            Assert.That(failed.Entries, Is.Empty);
        }

        [Test]
        public void SchemaTwelveContractRemainsNonPositionalAndInactive()
        {
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(12));
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(12));
            CollectionAssert.AreEqual(new[] { "AssignmentId", "CategoryId", "OptionId", "RoomInstanceId", "Sequence" },
                typeof(RoomContentAssignment).GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Select(value => value.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes).Any(value =>
                value.Name.IndexOf("SchemaTwelveToThirteen", StringComparison.Ordinal) >= 0), Is.False);
            string gameRoot = File.ReadAllText("Assets/_Project/Scripts/Core/GameRoot.cs");
            string saveService = File.ReadAllText("Assets/_Project/Scripts/Services/SaveService.cs");
            Assert.That(gameRoot, Does.Not.Contain(nameof(RoomContentPositionMigrationProfiles)));
            Assert.That(saveService, Does.Not.Contain(nameof(RoomContentPositionMigrationProfiles)));
        }

        private RoomContentPositionMigrationProfilesResult Parse(byte[] bytes, bool release = false) =>
            RoomContentPositionMigrationProfiles.ParseAndValidate(bytes, spatial, structuralLimits,
                serializedLimits, release);

        private RoomContentPositionMigrationProfilesResult ParseData(
            RoomContentPositionMigrationProfilesData data, bool release = false) =>
            Parse(RoomContentPositionMigrationProfiles.SerializeCanonical(data), release);

        private void AssertSemantic(Action<RoomContentPositionMigrationProfilesData> mutation,
            RoomContentPositionMigrationProfileDiagnostic expected, bool recomputeIntegrity = true)
        {
            RoomContentPositionMigrationProfilesData data = Data();
            mutation(data);
            data = recomputeIntegrity
                ? RoomContentPositionMigrationProfiles.WithComputedIntegrity(data)
                : RoomContentPositionMigrationProfiles.Canonicalize(data);
            RoomContentPositionMigrationProfilesResult result = ParseData(data, true);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Does.Contain(expected),
                string.Join(",", result.Diagnostics.Select(value => value.ToString()).ToArray()));
        }

        private void AssertFailure(byte[] bytes, RoomContentPositionMigrationProfileDiagnostic expected)
        {
            byte[] before = bytes == null ? null : (byte[])bytes.Clone();
            RoomContentPositionMigrationProfilesResult result = Parse(bytes, true);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Does.Contain(expected));
            if (before != null) CollectionAssert.AreEqual(before, bytes);
        }

        private static void AssertSuccess(RoomContentPositionMigrationProfilesResult result) =>
            Assert.That(result.Success, Is.True,
                string.Join(",", result.Diagnostics.Select(value => value.ToString()).ToArray()));

        private static void AssertProfile(RoomContentPositionMigrationProfilesData data,
            string roomId, CardinalOrientation orientation, int width, int height,
            int monsters, int traps, int loot, int slots)
        {
            RoomContentPositionMigrationProfile profile = data.Profiles.Single(value =>
                value.RoomDefinitionId == roomId && value.Orientation == orientation);
            Assert.That(profile.FrozenFootprint.Width, Is.EqualTo(width));
            Assert.That(profile.FrozenFootprint.Height, Is.EqualTo(height));
            Assert.That(profile.FrozenReservedTileOffsets, Is.Empty);
            Assert.That(Capacity(profile, CanonicalSpatialSaveContracts.MonsterCategoryId), Is.EqualTo(monsters));
            Assert.That(Capacity(profile, CanonicalSpatialSaveContracts.TrapCategoryId), Is.EqualTo(traps));
            Assert.That(Capacity(profile, CanonicalSpatialSaveContracts.LootNodeCategoryId), Is.EqualTo(loot));
            Assert.That(profile.OrderedSlots, Has.Length.EqualTo(slots));
            Assert.That(profile.OrderedSlots.Select(value => value.Order),
                Is.EqualTo(Enumerable.Range(0, slots)));
        }

        private static int Capacity(RoomContentPositionMigrationProfile profile, string category) =>
            profile.CategoryCapacities.Single(value => value.CategoryId == category).MaximumAssignments;

        private RoomContentPositionMigrationProfilesData Data() =>
            JsonUtility.FromJson<RoomContentPositionMigrationProfilesData>(profiles.text);

        private RoomContentPositionMigrationProfilesData SingleBasic()
        {
            RoomContentPositionMigrationProfilesData data = Data();
            data.Profiles = new[] { data.Profiles.Single(value =>
                value.RoomDefinitionId == "spatial.room.basic") };
            return data;
        }

        private static RoomContentAssignment[] MaximumAssignments(
            RoomContentPositionMigrationProfile profile, string roomId)
        {
            var result = new List<RoomContentAssignment>();
            foreach (RoomContentMigrationCategoryCapacity capacity in profile.CategoryCapacities)
                for (int index = 0; index < capacity.MaximumAssignments; index++)
                    result.Add(new RoomContentAssignment
                    {
                        AssignmentId = "assignment." + CategorySuffix(capacity.CategoryId) + "." + index,
                        RoomInstanceId = roomId,
                        CategoryId = capacity.CategoryId,
                        OptionId = "option." + CategorySuffix(capacity.CategoryId) + "." + index,
                        Sequence = index
                    });
            return result.ToArray();
        }

        private static RoomContentAssignment Assignment(string id, string category, long sequence,
            string option) => new RoomContentAssignment
        {
            AssignmentId = "assignment." + id,
            RoomInstanceId = "room.instance.01",
            CategoryId = category,
            OptionId = "option." + option,
            Sequence = sequence
        };

        private static string CategorySuffix(string category) =>
            category == CanonicalSpatialSaveContracts.MonsterCategoryId ? "monster" :
            category == CanonicalSpatialSaveContracts.TrapCategoryId ? "trap" : "loot";

        private static string EntryIdentity(RoomContentPositionMigrationPlanEntry value) =>
            value.AssignmentId + "|" + value.RoomInstanceId + "|" + value.CategoryId + "|" +
            value.OptionId + "|" + value.Sequence + "|" + value.SlotId + "|" +
            value.RoomLocalPosition.X + "," + value.RoomLocalPosition.Y;

        private static string AssignmentsJson(IEnumerable<RoomContentAssignment> values) =>
            string.Join("\n", values.Select(JsonUtility.ToJson).ToArray());

        private static Type[] SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception)
            { return exception.Types.Where(value => value != null).ToArray(); }
        }

        private static T Clone<T>(T value) where T : class =>
            JsonUtility.FromJson<T>(JsonUtility.ToJson(value));

        private static TextAsset Asset(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}
#endif
