using System;

namespace DungeonBuilder.M0
{
    public static class MinimalMvpActionPanelPresenter
    {
        public const string TitleKey = "ui.mvp_action.panel.title";
        public const string PlacementButtonKey = "ui.mvp_action.button.place_or_modify";
        public const string RunButtonKey = "ui.mvp_action.button.run_or_observe";
        public const string CompactFormatKey = "ui.mvp_action.panel.compact_format";

        public static MinimalMvpActionPanelLabels BuildLabels(Func<string, string, string> localize)
        {
            return new MinimalMvpActionPanelLabels(
                Localize(localize, TitleKey),
                Localize(localize, PlacementButtonKey),
                Localize(localize, RunButtonKey));
        }

        public static string BuildPanelText(Func<string, string, string> localize)
        {
            MinimalMvpActionPanelLabels labels = BuildLabels(localize);
            string format = Localize(localize, CompactFormatKey);
            return string.Format(format, labels.Title, labels.PlacementButton, labels.RunButton);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }
    }

    public struct MinimalMvpActionPanelLabels
    {
        public MinimalMvpActionPanelLabels(string title, string placementButton, string runButton)
        {
            Title = title;
            PlacementButton = placementButton;
            RunButton = runButton;
        }

        public string Title { get; }
        public string PlacementButton { get; }
        public string RunButton { get; }
    }
}
