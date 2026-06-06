using DungeonBuilder.M0;
using NUnit.Framework;
using System.Collections.Generic;

namespace DungeonBuilder.Tests.EditMode
{
    public class MinimalMvpActionPanelPresenterTests
    {
        [Test]
        public void BuildPanelText_UsesLocalizationKeysForTitleAndButtons()
        {
            var requestedKeys = new List<string>();

            string text = MinimalMvpActionPanelPresenter.BuildPanelText((key, fallback) =>
            {
                requestedKeys.Add(key);
                return key == MinimalMvpActionPanelPresenter.CompactFormatKey
                    ? "{0}|{1}|{2}"
                    : "LOC[" + key + "]";
            });

            Assert.That(text, Is.EqualTo("LOC[ui.mvp_action.panel.title]|LOC[ui.mvp_action.button.place_or_modify]|LOC[ui.mvp_action.button.run_or_observe]"));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.RunButtonKey));
            Assert.That(requestedKeys, Does.Contain(MinimalMvpActionPanelPresenter.CompactFormatKey));
        }

        [Test]
        public void BuildLabels_NullLocalizerFallsBackToStableKeys()
        {
            MinimalMvpActionPanelLabels labels = MinimalMvpActionPanelPresenter.BuildLabels(null);

            Assert.That(labels.Title, Is.EqualTo(MinimalMvpActionPanelPresenter.TitleKey));
            Assert.That(labels.PlacementButton, Is.EqualTo(MinimalMvpActionPanelPresenter.PlacementButtonKey));
            Assert.That(labels.RunButton, Is.EqualTo(MinimalMvpActionPanelPresenter.RunButtonKey));
        }
    }
}
