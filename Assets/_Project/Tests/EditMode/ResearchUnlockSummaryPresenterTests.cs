#if UNITY_EDITOR
using DungeonBuilder.M0;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class ResearchUnlockSummaryPresenterTests
    {
        private const string ProjectId = "research.project.test_unlock";
        private const string UnknownProjectId = "research.project.unknown";
        private const string UnlockId = "research.unlock.basic_run_analysis";
        private const string SummaryKey = "ui.research_unlock.basic_run_analysis.summary";

        [Test]
        public void CompletedResearch_ProducesResolvedUnlockSummary()
        {
            ResearchUnlockSummary summary = ResearchUnlockSummaryPresenter.Resolve(
                new CompletedResearchState { ProjectIds = new[] { ProjectId } },
                Config());

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.UnlockId, Is.EqualTo(UnlockId));
            Assert.That(summary.SummaryLocalizationKey, Is.EqualTo(SummaryKey));
            Assert.That(summary.MatchedProjectId, Is.EqualTo(ProjectId));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void NoCompletedResearch_ReturnsSafeNoneBehavior()
        {
            ResearchUnlockSummary summary = ResearchUnlockSummaryPresenter.Resolve(new CompletedResearchState(), Config());

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchUnlockSummaryErrorCode.NoCompletedResearch));
            Assert.That(summary.SummaryLocalizationKey, Is.EqualTo(ResearchUnlockSummaryPresenter.NoneKey));
            Assert.That(summary.UnlockId, Is.Null.Or.Empty);
            AssertSafetyFlags(summary);
        }

        [Test]
        public void UnknownCompletedResearch_DoesNotExposeRawId()
        {
            ResearchUnlockSummary summary = ResearchUnlockSummaryPresenter.Resolve(
                new CompletedResearchState { ProjectIds = new[] { UnknownProjectId } },
                Config());
            string serialized = JsonUtility.ToJson(summary);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.SummaryLocalizationKey, Is.EqualTo(ResearchUnlockSummaryPresenter.UnavailableKey));
            Assert.That(summary.UnlockId, Is.Null.Or.Empty);
            Assert.That(serialized, Does.Not.Contain(UnknownProjectId));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void MissingUnlockFields_LoadSafely()
        {
            ResearchUnlockSummary summary = ResearchUnlockSummaryPresenter.Resolve(
                new CompletedResearchState { ProjectIds = new[] { ProjectId } },
                null);

            Assert.That(summary.RuleResolved, Is.False);
            Assert.That(summary.DeterministicErrorCode, Is.EqualTo((int)ResearchUnlockSummaryErrorCode.MissingOrInvalidConfig));
            Assert.That(summary.SummaryLocalizationKey, Is.EqualTo(ResearchUnlockSummaryPresenter.UnavailableKey));
            AssertSafetyFlags(summary);
        }

        [Test]
        public void UnlockDisplayUsesLocalizationKey()
        {
            ResearchUnlockSummary summary = ResearchUnlockSummaryPresenter.Resolve(
                new CompletedResearchState { ProjectIds = new[] { ProjectId } },
                Config());

            Assert.That(summary.SummaryLocalizationKey, Does.StartWith("ui."));
            Assert.That(summary.SummaryLocalizationKey, Is.Not.EqualTo("Adventurer activity analysis unlocked"));
        }

        private static ResearchUnlockBridgeConfig Config()
        {
            return new ResearchUnlockBridgeConfig
            {
                enabled = true,
                ruleSourceId = "research.unlock_bridge.rule.test",
                unlocks = new[]
                {
                    new ResearchUnlockDefinitionConfig
                    {
                        researchProjectId = ProjectId,
                        unlockId = UnlockId,
                        summaryKey = SummaryKey
                    }
                }
            };
        }

        private static void AssertSafetyFlags(ResearchUnlockSummary summary)
        {
            Assert.That(summary.WouldMutateState, Is.False);
            Assert.That(summary.WouldGrantRewards, Is.False);
            Assert.That(summary.WouldUnlockContent, Is.False);
        }
    }
}
#endif
