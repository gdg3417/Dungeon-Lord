#if UNITY_EDITOR
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class CurrentHeatTierDiagnosticsTests
    {
        [Test]
        public void RefreshRunLine_ActiveRuntimeHeat_FormatsCurrentTierWithoutMutation()
        {
            var go = new GameObject("CurrentHeatTierDiagnosticsTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, BuildContent(includeFormat: true));
                SetService(root);
                SetSave(root, new SaveData
                {
                    structureRuntime = new StructureRuntimeState { Heat = 24d },
                    runHistory = new RunHistoryState()
                });

                root.RefreshRunLine();

                Assert.That(root.CurrentHeatTierLine, Is.EqualTo("Current Heat Tier: resolved=True error=0 heat=24 tier=heat_tier.notice min=10 max=24 atMin=False atMax=True ruleSource=run.heat_application.rule.test"));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(24d));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RefreshRunLine_LocalizationUnavailable_UsesKeyFallbackSafely(bool nullContent)
        {
            var go = new GameObject("CurrentHeatTierLocalizationFallbackTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, nullContent ? null : BuildContent(includeFormat: false));
                SetService(root);
                SetSave(root, new SaveData
                {
                    structureRuntime = new StructureRuntimeState { Heat = 10d },
                    runHistory = new RunHistoryState()
                });

                root.RefreshRunLine();

                Assert.That(root.CurrentHeatTierLine, Is.EqualTo("ui.heat.current_tier_summary_format"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ApplyHeatDelta_RefreshesCurrentTierFromActiveRuntimeHeat()
        {
            var go = new GameObject("CurrentHeatTierApplyDeltaTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, BuildContent(includeFormat: true));
                SetService(root);
                SetBackingField(root, "<CurrentHeat>k__BackingField", 9d);
                SetSave(root, new SaveData
                {
                    structureRuntime = new StructureRuntimeState { Heat = 9d },
                    runHistory = new RunHistoryState()
                });

                root.RefreshRunLine();
                Assert.That(root.CurrentHeatTierLine, Does.Contain("tier=heat_tier.peace"));

                root.ApplyHeatDelta(1d);

                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(10d));
                Assert.That(root.CurrentHeatTierLine, Does.Contain("heat=10 tier=heat_tier.notice"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RefreshRunLine_BrowsingHistory_DoesNotChangeCurrentTierOrMutateHeat()
        {
            var go = new GameObject("CurrentHeatTierHistoryTest");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SetContent(root, BuildContent(includeFormat: true));
                SetService(root);
                SetSave(root, new SaveData
                {
                    structureRuntime = new StructureRuntimeState { Heat = 25d },
                    runHistory = new RunHistoryState
                    {
                        RecentOutcomes = new[]
                        {
                            new RunOutcomeRecord { RunId = "old", ReasonKey = "run.reason.success", FeedbackTagKeys = new string[0] },
                            new RunOutcomeRecord { RunId = "new", ReasonKey = "run.reason.success", FeedbackTagKeys = new string[0] }
                        }
                    }
                });

                root.RefreshRunLine();
                string original = root.CurrentHeatTierLine;
                Assert.That(root.SelectPreviousRunOutcome(), Is.True);

                Assert.That(root.CurrentHeatTierLine, Is.EqualTo(original));
                Assert.That(root.Save.structureRuntime.Heat, Is.EqualTo(25d));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BootstrapOverlay_HeatDiagnostics_IncludesCurrentTierAfterCurrentHeat()
        {
            var rootObject = new GameObject("CurrentHeatTierOverlayRootTest");
            var overlayObject = new GameObject("CurrentHeatTierOverlayTest");
            var textObject = new GameObject("CurrentHeatTierOverlayTextTest");
            try
            {
                var root = rootObject.AddComponent<GameRoot>();
                TestDiagnosticsHelper.EnableDevelopmentDiagnostics(root);
                SetBackingField(root, "<HeatLine>k__BackingField", "heat-line");
                SetBackingField(root, "<CurrentHeatTierLine>k__BackingField", "current-tier-line");
                SetBackingField(root, "<TickLine>k__BackingField", "tick-line");

                var overlay = overlayObject.AddComponent<BootstrapOverlay>();
                overlay.overlayText = textObject.AddComponent<TextMeshProUGUI>();
                overlay.Bind(root);
                ShowDiagnostics(overlay);
                overlay.CycleFullDiagnosticsPage();
                overlay.CycleFullDiagnosticsPage();
                overlay.RefreshOverlayText();

                string text = overlay.overlayText.text;
                Assert.That(text.IndexOf("current-tier-line", System.StringComparison.Ordinal), Is.GreaterThan(text.IndexOf("heat-line", System.StringComparison.Ordinal)));
                Assert.That(text, Does.Not.Contain("tick-line"));
            }
            finally
            {
                Object.DestroyImmediate(textObject);
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(rootObject);
            }
        }

        private static void ShowDiagnostics(BootstrapOverlay overlay)
        {
            if (!overlay.DiagnosticsVisible)
            {
                overlay.ToggleDiagnosticsVisibility();
            }
            overlay.RefreshOverlayText();
        }

        private static void SetSave(GameRoot root, SaveData save) => SetBackingField(root, "<Save>k__BackingField", save);
        private static void SetContent(GameRoot root, ContentService content) => SetBackingField(root, "<Content>k__BackingField", content);
        private static void SetService(GameRoot root) => typeof(GameRoot).GetField("_runSimulationService", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(root, new RunSimulationService(ValidConfig()));
        private static void SetBackingField(GameRoot root, string name, object value) => typeof(GameRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(root, value);

        private static ContentService BuildContent(bool includeFormat)
        {
            var content = new ContentService();
            var map = (Dictionary<string, string>)typeof(ContentService).GetField("_stringMap", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(content);
            map["ui.run.history_position_format"] = "Run history: {0}/{1}";
            map["ui.run.latest_format"] = "Run: {0} success={1} score={2} reason={3}";
            if (includeFormat)
            {
                map["ui.heat.current_tier_summary_format"] = "Current Heat Tier: resolved={0} error={1} heat={2:0.###} tier={3} min={4:0.###} max={5:0.###} atMin={6} atMax={7} ruleSource={8}";
            }
            return content;
        }

        private static RunSimulationConfig ValidConfig()
        {
            return new RunSimulationConfig
            {
                HeatPeaceMinimum = 0d,
                HeatPeaceMaximum = 9d,
                HeatNoticeMinimum = 10d,
                HeatNoticeMaximum = 24d,
                HeatConcernMinimum = 25d,
                HeatConcernMaximum = 49d,
                RunHeatApplicationRuleSourceId = "run.heat_application.rule.test"
            };
        }
    }
}
#endif
