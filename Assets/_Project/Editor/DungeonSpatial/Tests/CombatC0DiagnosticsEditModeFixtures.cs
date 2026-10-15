#if UNITY_EDITOR
using NUnit.Framework;

namespace DungeonBuilder.M0.EditorTools.Tests
{
    public sealed class CombatC0DiagnosticsEditModeFixtures : DungeonBuilder.M0.Tests.EditMode.CombatC0DiagnosticsTests
    {
        [Test] public void FocusedC0OverlayPagesWithoutOverflow()
        {
            var fixture = new DungeonBuilder.Tests.EditMode.BootstrapOverlayPagingTests();
            fixture.SetUp();
            try { fixture.C0FocusedDiagnosticsUseExistingPagingAndConstrainTextToSafeArea(); }
            finally { fixture.TearDown(); }
        }
    }
}
#endif
