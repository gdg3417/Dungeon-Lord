#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedSpatialMigrationPreparationTests
    {
        [Test]
        public void TransactionExecute_AcceptsOnePreparedAttemptBoundary()
        {
            MethodInfo execute = typeof(DetachedSpatialMigrationTransaction).GetMethods()
                .Single(method => method.Name == "Execute");
            Type[] parameters = execute.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

            Assert.That(parameters, Is.EqualTo(new[]
            {
                typeof(string), typeof(DetachedPreparedSpatialMigrationAttempt)
            }));
        }

        [Test]
        public void TransactionConstruction_RequiresRecoveryValidationContext()
        {
            ConstructorInfo constructor = typeof(DetachedSpatialMigrationTransaction).GetConstructors().Single();
            Assert.That(constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                Is.EqualTo(new[]
                {
                    typeof(ISpatialMigrationFileSystem), typeof(DetachedSpatialMigrationRecoveryContext)
                }));
        }

        [Test]
        public void CandidateSerializer_DoesNotExposeCallerAuthoredBuild()
        {
            Assert.That(typeof(DetachedWholeSaveCandidateSerializer).GetMethod("Build",
                BindingFlags.Public | BindingFlags.Static), Is.Null);
        }

        [Test]
        public void Preparation_RejectsClassificationFromDifferentOriginalBytes()
        {
            byte[] saveA = Encoding.UTF8.GetBytes("{\"saveVersion\":1}");
            byte[] saveB = Encoding.UTF8.GetBytes("{\"saveVersion\":2}");
            RawSavePayloadClassification classification = RawSavePayloadClassifier.Classify(saveA,
                new RawSavePayloadClassificationLimits(1024, 16, 32, 32, 256, 4096),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());
            var inputs = new DetachedSpatialMigrationPreparationInputs(saveB, classification, null,
                null, null, null, default(CanonicalSpatialSerializationLimits),
                default(DetachedWholeSaveLimits));

            DetachedSpatialMigrationPreparationResult result = DetachedSpatialMigrationPreparer.Prepare(inputs);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo("gd66.transaction.input_fingerprint_mismatch"));
        }

        [TestCase("gd66.transaction.pinned_input_hash_mismatch")]
        [TestCase("gd66.content.duplicate_assignment")]
        [TestCase("gd66.content.outcome_mismatch")]
        [TestCase("gd66.content.migration_blocked_narrow_hall")]
        [TestCase("gd66.content.invalid_option")]
        [TestCase("gd66.content.room_capacity_exceeded")]
        public void PreparationFailureCodes_AreStableRegisteredKeys(string reason)
        {
            Assert.That(reason, Does.StartWith("gd66."));
        }

        [Test]
        public void RequiredValidationInputRegistry_RejectsUnexpectedCallerAuthoredInput()
        {
            var values = new System.Collections.Generic.Dictionary<string, byte[]>
            { { "unexpected", new byte[] { 1 } } };
            var pins = new[]
            {
                new SpatialValidationInputHash("unexpected", SpatialContractSha256.Compute(new byte[] { 1 }))
            };

            string reason = DetachedRequiredValidationInputSpecification.Current.Validate(values, pins);

            Assert.That(reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(DetachedRequiredValidationInputSpecification.Current.TargetSchemaVersion, Is.EqualTo(8));
        }

        [TestCase("assignments")]
        [TestCase("floor")]
        [TestCase("placements")]
        public void LegacyRouteContracts_PrettyAndCompactSaveRootEvidenceAreEquivalent(string authority)
        {
            SaveData save = RouteSave(authority);
            byte[] compact = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveRoot
                { schemaVersion = 6, primary = save }));
            byte[] pretty = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveRoot
                { schemaVersion = 6, primary = save }, true));

            object compactProjection = ParseAuthority(Classify(compact), authority, out string compactReason);
            object prettyProjection = ParseAuthority(Classify(pretty), authority, out string prettyReason);

            Assert.That(compactReason, Is.Null);
            Assert.That(prettyReason, Is.Null);
            Assert.That(JsonUtility.ToJson(prettyProjection), Is.EqualTo(JsonUtility.ToJson(compactProjection)));
        }

        [Test]
        public void LegacyWhitespaceNormalization_PreservesStringsAndIsBoundedByOriginalBytes()
        {
            byte[] source = Encoding.UTF8.GetBytes(" { \n \"value\" : \"space \\\" quote \\\\ slash\" \n } ");

            Assert.That(LegacyJsonWhitespaceNormalizer.TryNormalize(source, source.Length,
                out byte[] normalized), Is.True);
            Assert.That(Encoding.UTF8.GetString(normalized),
                Is.EqualTo("{\"value\":\"space \\\" quote \\\\ slash\"}"));
            Assert.That(LegacyJsonWhitespaceNormalizer.TryNormalize(source, source.Length - 1,
                out _), Is.False);
            Assert.That(LegacyJsonWhitespaceNormalizer.TryNormalize(
                Encoding.UTF8.GetBytes("{\"unterminated\":\"value}"), 1024, out _), Is.False);
        }

        [TestCase("{\"Entries\":[],\"Entries\":[],\"NextRevision\":1}")]
        [TestCase("{\"NextRevision\":1,\"Entries\":[]}")]
        [TestCase("{\"Entries\":[],\"Unknown\":0,\"NextRevision\":1}")]
        [TestCase("{\"Entries\":[,],\"NextRevision\":1}")]
        public void LegacyWhitespaceNormalization_DoesNotWeakenStrictRouteShape(string placementJson)
        {
            string root = "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"mvpDungeonPlacements\":" +
                placementJson + "}}";

            MvpDungeonPlacementState parsed = RawLegacyRouteContracts.ParsePlacements(
                Classify(Encoding.UTF8.GetBytes(root)), "mvpDungeonPlacements", SerializedLimits(),
                out string reason);

            Assert.That(parsed, Is.Null);
            Assert.That(reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
        }

        private static object ParseAuthority(RawSavePayloadClassification classification, string authority,
            out string reason)
        {
            switch (authority)
            {
                case "assignments":
                    return RawLegacyRouteContracts.ParseAssignments(classification,
                        "mvpRoomSlotAssignments", SerializedLimits(), out reason);
                case "floor":
                    return RawLegacyRouteContracts.ParseFloor(classification,
                        "mvpDungeonFloorLayout", SerializedLimits(), out reason);
                default:
                    return RawLegacyRouteContracts.ParsePlacements(classification,
                        "mvpDungeonPlacements", SerializedLimits(), out reason);
            }
        }

        private static SaveData RouteSave(string authority)
        {
            var save = new SaveData
            {
                mvpRoomSlotAssignments = null,
                mvpDungeonFloorLayout = null,
                mvpDungeonPlacements = null
            };
            if (authority == "assignments")
                save.mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection
                {
                    Rooms = new List<MvpRoomSlotAssignmentState>
                    {
                        new MvpRoomSlotAssignmentState
                        {
                            FloorIndex = 0, RoomIndex = 0,
                            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
                            MonsterOptionIds = new[] { "placement.option.monster.goblin" }
                        }
                    },
                    NextRevision = 2
                };
            else if (authority == "floor")
                save.mvpDungeonFloorLayout = new MvpDungeonFloorLayoutState
                {
                    Nodes = new List<MvpDungeonNodeState>
                    {
                        new MvpDungeonNodeState(0, 0, "slot.0", MvpDungeonPlacementIds.RoomCategoryId,
                            MvpDungeonPlacementIds.BasicRoomOptionId, 1)
                    },
                    NextRevision = 2
                };
            else
                save.mvpDungeonPlacements = new MvpDungeonPlacementState
                {
                    Entries = new List<MvpDungeonPlacementEntry>
                    {
                        new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId,
                            MvpDungeonPlacementIds.BasicRoomOptionId, 1)
                    },
                    NextRevision = 2
                };
            return save;
        }

        private static RawSavePayloadClassification Classify(byte[] bytes) =>
            RawSavePayloadClassifier.Classify(bytes,
                new RawSavePayloadClassificationLimits(100000, 64, 128, 128, 4096, 1000000),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());

        private static SpatialSerializedInputLimits SerializedLimits() =>
            new SpatialSerializedInputLimits(100000, 10000, 1000, 10000, 32);

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            new[]
            {
                new RawLegacyBlankFloorNodeContract(0, 0, "slot.0", "", "", 0),
                new RawLegacyBlankFloorNodeContract(0, 1, "slot.1", "", "", 0),
                new RawLegacyBlankFloorNodeContract(0, 2, "slot.2", "", "", 0),
                new RawLegacyBlankFloorNodeContract(0, 3, "slot.3", "", "", 0)
            }, true, true, new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });
    }
}
#endif
