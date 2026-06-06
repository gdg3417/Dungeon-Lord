using System.Collections.Generic;
using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class GuidedMvpActionPathPanelPresenterTests
    {
        [Test]
        public void BuildPanelText_PlayerFacingLabelsResolveThroughLocalizationKeys()
        {
            var requestedKeys = new List<string>();
            var summary = new GuidedMvpActionPathSummary
            {
                RuleResolved = true,
                CurrentStepId = GuidedMvpActionPathPresenter.StepRunOrObserveId,
                CurrentStepStatusKey = GuidedMvpActionPathPresenter.StatusRunOrObserveKey,
                NextActionKey = GuidedMvpActionPathPresenter.ActionRunDungeonKey,
                IsComplete = false
            };

            string text = GuidedMvpActionPathPanelPresenter.BuildPanelText(summary, (key, fallback) =>
            {
                requestedKeys.Add(key);
                return Localize(key, fallback);
            });

            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPanelPresenter.TitleKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.StepRunOrObserveId + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.StatusRunOrObserveKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.ActionRunDungeonKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPanelPresenter.CompleteNoKey + "]"));
            Assert.That(requestedKeys, Does.Contain(GuidedMvpActionPathPanelPresenter.StepFormatKey));
            Assert.That(requestedKeys, Does.Contain(GuidedMvpActionPathPanelPresenter.StatusFormatKey));
            Assert.That(requestedKeys, Does.Contain(GuidedMvpActionPathPanelPresenter.NextActionFormatKey));
            Assert.That(requestedKeys, Does.Contain(GuidedMvpActionPathPanelPresenter.CompleteFormatKey));
        }

        [Test]
        public void BuildPanelText_NullSummary_UsesLocalizedFallbackGuidance()
        {
            string text = GuidedMvpActionPathPanelPresenter.BuildPanelText(null, Localize);

            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.StatusPlaceOrModifyStructureKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPresenter.ActionPlaceStructureKey + "]"));
            Assert.That(text, Does.Contain("LOC[" + GuidedMvpActionPathPanelPresenter.CompleteNoKey + "]"));
        }

        private static string Localize(string key, string fallback)
        {
            switch (key)
            {
                case GuidedMvpActionPathPanelPresenter.StepFormatKey:
                case GuidedMvpActionPathPanelPresenter.StatusFormatKey:
                case GuidedMvpActionPathPanelPresenter.NextActionFormatKey:
                case GuidedMvpActionPathPanelPresenter.CompleteFormatKey:
                    return "LOC[" + key + "]:{0}";
                default:
                    return "LOC[" + key + "]";
            }
        }
    }
}
