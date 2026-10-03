using NUnit.Framework;

namespace DungeonBuilder.M0.Editor.Tests
{
    // Core fixtures live in Assembly-CSharp, which Unity does not discover as an EditMode assembly.
    [TestFixture]
    public sealed class PhaseSevenA2PositionalCanonicalState :
        DungeonBuilder.M0.Tests.EditMode.PhaseSevenA2PositionalCanonicalStateTests { }

    [TestFixture]
    public sealed class PhaseSevenA2WindowsSpatialMigration :
        DungeonBuilder.M0.Tests.EditMode.Gd66WindowsSpatialMigrationFileSystemTests { }

}
