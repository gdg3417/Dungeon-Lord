using System;
using System.Text;

namespace DungeonBuilder.M0
{
    public static class GuidedMvpActionPathPanelPresenter
    {
        public const string TitleKey = "ui.guided_mvp.panel.title";
        public const string StepFormatKey = "ui.guided_mvp.panel.step_format";
        public const string StatusFormatKey = "ui.guided_mvp.panel.status_format";
        public const string NextActionFormatKey = "ui.guided_mvp.panel.next_action_format";
        public const string CompleteFormatKey = "ui.guided_mvp.panel.complete_format";
        public const string CompleteYesKey = "ui.guided_mvp.value.complete_yes";
        public const string CompleteNoKey = "ui.guided_mvp.value.complete_no";
        public const string FallbackStepKey = GuidedMvpActionPathPresenter.StepPlaceOrModifyStructureId;
        public const string FallbackStatusKey = GuidedMvpActionPathPresenter.StatusPlaceOrModifyStructureKey;
        public const string FallbackActionKey = GuidedMvpActionPathPresenter.ActionPlaceStructureKey;

        public static string BuildPanelText(GuidedMvpActionPathSummary guidedPath, Func<string, string, string> localize)
        {
            var builder = new StringBuilder();
            AppendLine(builder, Localize(localize, TitleKey));
            AppendLine(builder, string.Format(
                Localize(localize, StepFormatKey),
                ResolveKeyOrFallback(guidedPath?.CurrentStepId, localize, FallbackStepKey)));
            AppendLine(builder, string.Format(
                Localize(localize, StatusFormatKey),
                ResolveKeyOrFallback(guidedPath?.CurrentStepStatusKey, localize, FallbackStatusKey)));
            AppendLine(builder, string.Format(
                Localize(localize, NextActionFormatKey),
                ResolveKeyOrFallback(guidedPath?.NextActionKey, localize, FallbackActionKey)));
            AppendLine(builder, string.Format(
                Localize(localize, CompleteFormatKey),
                Localize(localize, guidedPath != null && guidedPath.IsComplete ? CompleteYesKey : CompleteNoKey)));
            return builder.ToString();
        }

        private static string ResolveKeyOrFallback(string key, Func<string, string, string> localize, string fallbackKey)
        {
            return string.IsNullOrWhiteSpace(key)
                ? Localize(localize, fallbackKey)
                : Localize(localize, key);
        }

        private static string Localize(Func<string, string, string> localize, string key)
        {
            if (localize == null)
            {
                return key;
            }

            return localize(key, key);
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(line ?? string.Empty);
        }
    }
}
