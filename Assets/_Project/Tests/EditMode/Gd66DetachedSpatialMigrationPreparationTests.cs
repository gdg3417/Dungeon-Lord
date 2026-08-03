#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

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
            Assert.That(DetachedRequiredValidationInputSpecification.Current.TargetSchemaVersion, Is.EqualTo(7));
        }

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
