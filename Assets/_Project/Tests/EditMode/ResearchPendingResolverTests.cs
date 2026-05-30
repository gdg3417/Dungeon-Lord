using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchPendingResolverTests
    {
        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("research.slot.unused", "   ")]
        public void Resolve_NullDefaultOrEmptyState_ReportsPendingFalse(string slotId, string projectId)
        {
            ResearchPendingState state = slotId == null && projectId == null
                ? null
                : new ResearchPendingState { SlotId = slotId, ProjectId = projectId };

            ResearchPendingValidationResult result = ResearchPendingResolver.Resolve(state, ValidConfig());

            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)ResearchPendingValidationErrorCode.None));
            Assert.That(result.Pending, Is.False);
            Assert.That(result.SlotId, Is.Empty);
            Assert.That(result.ProjectId, Is.Empty);
        }

        [Test]
        public void Resolve_ConfiguredSingleSlotScaffold_ReportsPendingTrue()
        {
            ResearchPendingValidationResult result = ResearchPendingResolver.ResolveScaffold(ValidConfig());

            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.Pending, Is.True);
            Assert.That(result.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(result.ProjectId, Is.EqualTo("research.project.scaffold"));
            Assert.That(result.RuleSourceIdUsed, Is.EqualTo("research.pending.rule.test"));
        }

        [Test]
        public void Resolve_PendingProject_NormalizesConfiguredSingleSlot()
        {
            ResearchPendingValidationResult result = ResearchPendingResolver.Resolve(
                new ResearchPendingState { SlotId = "research.slot.unconfigured", ProjectId = "research.project.saved" },
                ValidConfig());

            Assert.That(result.RuleResolved, Is.True);
            Assert.That(result.Pending, Is.True);
            Assert.That(result.SlotId, Is.EqualTo("research.slot.primary"));
            Assert.That(result.ProjectId, Is.EqualTo("research.project.saved"));
        }

        [TestCase(false, "research.slot.primary", "research.project.scaffold", ResearchPendingValidationErrorCode.ScaffoldConfigMissingOrDisabled)]
        [TestCase(true, "", "research.project.scaffold", ResearchPendingValidationErrorCode.ScaffoldSlotIdMissing)]
        [TestCase(true, "research.slot.primary", "", ResearchPendingValidationErrorCode.ScaffoldProjectIdMissing)]
        public void ResolveScaffold_InvalidConfig_ReturnsDeterministicDisabledSafeResult(
            bool enabled,
            string slotId,
            string projectId,
            ResearchPendingValidationErrorCode expected)
        {
            var config = new ResearchPendingScaffoldConfig { enabled = enabled, slotId = slotId, projectId = projectId };

            ResearchPendingValidationResult result = ResearchPendingResolver.ResolveScaffold(config);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)expected));
            Assert.That(result.Pending, Is.False);
            Assert.That(result.SlotId, Is.Empty);
            Assert.That(result.ProjectId, Is.Empty);
        }

        [Test]
        public void ResolveScaffold_MissingConfig_ReturnsDeterministicDisabledSafeResult()
        {
            ResearchPendingValidationResult result = ResearchPendingResolver.ResolveScaffold(null);

            Assert.That(result.RuleResolved, Is.False);
            Assert.That(result.DeterministicErrorCode, Is.EqualTo((int)ResearchPendingValidationErrorCode.ScaffoldConfigMissingOrDisabled));
            Assert.That(result.Pending, Is.False);
        }

        private static ResearchPendingScaffoldConfig ValidConfig()
        {
            return new ResearchPendingScaffoldConfig
            {
                enabled = true,
                slotId = "research.slot.primary",
                projectId = "research.project.scaffold",
                ruleSourceId = "research.pending.rule.test"
            };
        }
    }
}
