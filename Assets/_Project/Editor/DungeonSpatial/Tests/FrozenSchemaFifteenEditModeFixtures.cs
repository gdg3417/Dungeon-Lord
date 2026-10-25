#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using DungeonBuilder.M0.Tests.EditMode;

namespace DungeonBuilder.M0.EditorTools.Tests
{
    public sealed class FrozenSchemaFifteen : DungeonBuilder.M0.Tests.EditMode.FrozenSchemaFifteenTests { }

    // Existing sealed core fixtures need an Editor entry point for Unity discovery.
    public sealed class FrozenSchemaFifteenMigrationRegression
    {
        public static IEnumerable<TestCaseData> FailureCases() {
            var rows = (IEnumerable)typeof(Gd66DetachedSpatialMigrationTransactionFailureMatrixTests)
                .GetProperty("Cases", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (object row in rows) yield return new TestCaseData(row)
                .SetName("FrozenSchemaFifteen.EstablishedFailureMatrix." + row);
        }
        [TestCaseSource(nameof(FailureCases))]
        public void EstablishedMigrationFailureMatrix(object row) =>
            new Gd66DetachedSpatialMigrationTransactionFailureMatrixTests()
                .PacketC_BoundedFailureMatrix_ExecutesDeclaredFaultAndRestartScenario(row);
        [Test] public void EstablishedMigrationMatrixDimensionsAndChronology() {
            var tests = new Gd66DetachedSpatialMigrationTransactionFailureMatrixTests();
            tests.PacketC_BoundedFailureMatrix_CoversExpectedDimensions();
            tests.PacketC_DiagnosticChronologyGuardRejectsReorderedAndDuplicateEvents();
        }
        [Test] public void EstablishedHistoricalLimitLoaderAssertions() {
            var tests = new SaveSpatialMigrationLimitsTests();
            tests.ProductionProfile_LoadsAllApprovedValues();
            tests.ProductionFramingAcceptsExactlyOneLfAndRejectsOtherTerminators();
            tests.WrongSchemaVersionAndEveryMissingFieldFailClosed();
            tests.InvalidDimensionsAndCrossFieldRelationshipsFailClosed();
            tests.ProductionAuthorityIsDedicatedAndHasNoBootstrapOrSpatialValidationFallback();
            tests.ProductionBootstrapSceneOwnsAndLoadsDedicatedProfile();
            tests.GameRootRejectsInvalidProfileBeforeConstructingLegacySaveService();
        }
    }
}
#endif
