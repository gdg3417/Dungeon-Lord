using NUnit.Framework;

namespace DungeonBuilder.M0.Editor.Tests
{
    // Existing repository bridge: runtime-assembly fixtures also run in EditMode.
    [TestFixture]
    public sealed class PhaseSixAActivation : DungeonBuilder.M0.Tests.EditMode.PhaseSixAActivationTests { }
    [TestFixture]
    public sealed class PhaseSixA2FloorConstruction : DungeonBuilder.M0.Tests.EditMode.PhaseSixA2FloorConstructionTests { }
    [TestFixture]
    public sealed class PhaseSixA3FloorActivationEligibility : DungeonBuilder.M0.Tests.EditMode.PhaseSixA3FloorActivationEligibilityTests { }

    public sealed class PhaseSixA4 : DungeonBuilder.M0.Tests.EditMode.PhaseSixA4Tests { }
    public sealed class PhaseSixA5A : DungeonBuilder.M0.Tests.EditMode.PhaseSixA5ATests { }
}
