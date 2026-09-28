using NUnit.Framework;

namespace DungeonBuilder.M0.Editor.Tests
{
    // Match the repository's existing EditMode discovery bridge.
    [TestFixture]
    public sealed class PhaseFiveBPartyHealth : DungeonBuilder.M0.Tests.EditMode.PhaseFiveBPartyHealthTests { }
    [TestFixture]
    public sealed class PhaseFiveBBranchDecision : DungeonBuilder.M0.Tests.EditMode.PhaseFiveBBranchDecisionTests { }
    [TestFixture]
    public sealed class PhaseFiveBBranchIntegration : DungeonBuilder.M0.Tests.EditMode.PhaseFiveBBranchIntegrationTests { }
    [TestFixture]
    public sealed class PhaseFiveBTraversal : DungeonBuilder.M0.Tests.EditMode.PhaseFiveBTraversalTests { }
}
