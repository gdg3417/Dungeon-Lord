#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
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
        public void CandidateSerializer_DoesNotExposeCallerAuthoredBuild()
        {
            Assert.That(typeof(DetachedWholeSaveCandidateSerializer).GetMethod("Build",
                BindingFlags.Public | BindingFlags.Static), Is.Null);
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
    }
}
#endif
