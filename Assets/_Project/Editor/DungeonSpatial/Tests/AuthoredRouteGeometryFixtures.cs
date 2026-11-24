#if UNITY_EDITOR
using DungeonBuilder.M0.Tests;
using DungeonBuilder.M0.Tests.EditMode;
using NUnit.Framework;

namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    // The sealed fixtures in Assembly-CSharp require discoverable Editor entry points.
    public sealed class AuthoredRouteGeometryFixtures
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void OrthogonalPassageValidatesItsStepsAndDistinctTileLimit(int length) =>
            new FloorLayoutValidatorTests().OrthogonalPassageValidatesItsStepsAndDistinctTileLimit(length);

        [TestCase(1, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 0)]
        public void OrthogonalPassageRejectsDiagonalBrokenAndRepeatedSteps(int x, int y) =>
            new FloorLayoutValidatorTests().OrthogonalPassageRejectsDiagonalBrokenAndRepeatedSteps(x, y);

        [Test]
        public void SlidingMiddleSegmentMovesBothBendsWithDoorwayCellsPinned() =>
            new FloorLayoutValidatorTests().SlidingMiddleSegmentMovesBothBendsWithDoorwayCellsPinned();

        [TestCase(false, 80)]
        [TestCase(true, 76)]
        public void ProductionFloorTwoCapturesEveryAuthoredPassageStepWithoutSideContactShortcuts(bool shorter, int used) =>
            new FloorLayoutValidatorTests().ProductionFloorTwoCapturesEveryAuthoredPassageStepWithoutSideContactShortcuts(shorter, used);

        [Test]
        public void AuthoredRouteWirePreservesPassageOrderAndRejectsMixedOrFrozenGeometry() =>
            new DetachedCanonicalSpatialSaveContractTests().AuthoredRouteWirePreservesPassageOrderAndRejectsMixedOrFrozenGeometry();
    }
}
#endif
