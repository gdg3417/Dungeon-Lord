#if UNITY_EDITOR
namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    public sealed class R0ResearchDurability : DungeonBuilder.Tests.EditMode.ResearchDurableAcknowledgementTests { }
    public sealed class R0ResearchHandler : DungeonBuilder.Tests.EditMode.PlayerResearchActionHandlerTests { }
    public sealed class R0ResearchSaveIntegration : DungeonBuilder.Tests.EditMode.PlayerResearchSaveServiceIntegrationTests { }
    public sealed class R0ResearchJourney : DungeonBuilder.Tests.EditMode.MvpCanonicalJourneyIntegrationTests { }
    public sealed class R0ResearchDiagnostics : DungeonBuilder.Tests.EditMode.ResearchCompletionEligibilityDiagnosticsTests { }
    public sealed class R0ResearchObjectives : DungeonBuilder.Tests.EditMode.MvpFirstSessionObjectivePresenterTests { }
}
#endif
