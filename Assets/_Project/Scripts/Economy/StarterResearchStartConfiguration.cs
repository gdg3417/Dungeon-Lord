using System;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Economy
{
    internal sealed class StarterResearchStartConfiguration
    {
        internal const string ResourcePath = "Economy/starter_research_start";
        internal double ManaCost { get; }
        private readonly string projectId, progressRuleId, completionRuleId;

        private StarterResearchStartConfiguration(string projectId, string progressRuleId, string completionRuleId, double manaCost)
        {
            this.projectId = projectId;
            this.progressRuleId = progressRuleId;
            this.completionRuleId = completionRuleId;
            ManaCost = manaCost;
        }

        internal bool AppliesTo(string project, ResearchProgressScaffoldConfig progress, ResearchCompletionEligibilityScaffoldConfig completion) =>
            string.Equals(projectId, project, StringComparison.Ordinal) &&
            string.Equals(progressRuleId, progress?.ruleSourceId, StringComparison.Ordinal) &&
            string.Equals(completionRuleId, completion?.ruleSourceId, StringComparison.Ordinal) &&
            progress.progressPerActiveSecond > 0 && progress.maxActiveSessionElapsedSeconds > 0 &&
            double.IsFinite(progress.progressPerActiveSecond * progress.maxActiveSessionElapsedSeconds);

        internal static StarterResearchStartConfiguration Parse(byte[] bytes, CanonicalSpatialSerializationLimits limits)
        {
            if (bytes == null) return null;
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out var root, allowWhitespace: true) ||
                root.Kind != ContractJsonKind.Object || root.Fields.Count != 6) return null;
            var fields = root.Fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
            string ReadId(string name) => fields.TryGetValue(name, out var field) &&
                field.Kind == ContractJsonKind.String && !string.IsNullOrWhiteSpace(field.Text) ? field.Text : null;
            var project = ReadId("projectId");
            var progress = ReadId("progressRuleId");
            var completion = ReadId("completionRuleId");
            if (project == null || progress == null || completion == null ||
                !fields.TryGetValue("schemaVersion", out var version) || version.Kind != ContractJsonKind.Number || version.Text != "1" ||
                !fields.TryGetValue("enabled", out var enabled) || enabled.Kind != ContractJsonKind.Boolean || enabled.Text != "true" ||
                !fields.TryGetValue("manaCost", out var cost) || cost.Kind != ContractJsonKind.Number ||
                !double.TryParse(cost.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double price) ||
                !double.IsFinite(price) || price <= 0) return null;
            return new StarterResearchStartConfiguration(project, progress, completion, price);
        }
    }
}
