// Core fixtures live in Assembly-CSharp, which Unity does not discover as an EditMode assembly.
// Editor subclasses inherit NUnit cases and lifecycle attributes without reimplementing a test runner.
using DungeonBuilder.M0.Tests.EditMode;
using NUnit.Framework;

namespace DungeonBuilder.M0.Editor.Tests
{
    [TestFixture] public sealed class PhaseFourEconomy : StructuralEconomyTests { }
    [TestFixture] public sealed class PhaseFourRedeployment : ReturnedContentRedeploymentTests { }
    [TestFixture] public sealed class PhaseFourWrites : DetachedCanonicalWriteAuthorityTests { }
    [TestFixture] public sealed class PhaseFourSpatial : StructuralEditServiceTests { }
    [TestFixture] public sealed class PhaseFourBootstrap : StructuralConstructionGameRootTests { }
    [TestFixture] public sealed class PhaseFourDeletion : StructuralDeletionServiceTests { }
    [TestFixture] public sealed class PhaseFourLifecycle : SchemaEightLifecycleOwnershipTests { }
    [TestFixture] public sealed class PhaseFourSessions : DetachedCanonicalSaveSessionTests { }
    [TestFixture] public sealed class PhaseFourWorkload : Gd66SaveWorkloadMeasurementTests { }
    [TestFixture] public sealed class PhaseFourLoad : DetachedSpatialSaveLoadCoordinatorTests { }
    [TestFixture] public sealed class PhaseFourCompleteSave : Gd66DetachedCompleteSaveContractTests { }
    [TestFixture] public sealed class PhaseFourSaveSemantics : Gd66DetachedCompleteSaveSemanticValidationTests { }
    [TestFixture] public sealed class PhaseFourProjection : Gd66CanonicalRuntimeProjectionTests { }
    [TestFixture] public sealed class PhaseFourCompatibility : SpatialLayoutCompatibilityProfileTests { }
    [TestFixture] public sealed class PhaseFourContent : ProductionSpatialContentLoadingTests { }
    [TestFixture] public sealed class PhaseFourIdentity : NativeStructuralIdentityTests { }
}
