#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Tests.EditMode;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    public sealed class PhaseSevenFloorLifecycleTests
    {
        [Test]
        public void ExplicitPreviewAndCommitTargetFloorTwoAndRetainCanonicalOrderingOnReopen()
        {
            var f = PhaseSixA4Tests.Eligible(); var writer = PhaseSixA4Tests.Writer(f);
            string id = PhaseSixA4Tests.FloorTwo(f);
            foreach (var action in new[] { FloorLifecycleAction.Activate, FloorLifecycleAction.Deactivate })
            {
                var preview = writer.PreviewFloorLifecycle(f.Session, f.Runtime, action, id);
                Assert.That(preview.IsCommittable, Is.True, preview.Reason);
                Assert.That(preview.FloorInstanceId, Is.EqualTo(id));
                Assert.That(preview.Changes.Select(c => c.FloorInstanceId), Is.EqualTo(new[] { id }));
                Assert.That(preview.RequiresConfirmation, Is.False);
                f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, action, id, preview));
                Assert.That(f.State.Floors[0].ActivationState, Is.EqualTo(FloorActivationState.Active));
                Assert.That(f.State.Floors[1].ActivationState, Is.EqualTo(action == FloorLifecycleAction.Activate
                    ? FloorActivationState.Active : FloorActivationState.Inactive));
                var reopened = DetachedCanonicalSaveSession.Open(f.FileSystem.ReadAllBytes(f.ActivePath), f.Context, f.Profile);
                Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
                Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
            }
        }

        [TestCase("other-target")][TestCase("other-action")][TestCase("new-session")][TestCase("invalid-target")]
        public void StaleOrRetargetedPreviewNeverMutates(string scenario)
        {
            var f = PhaseSixA4Tests.Eligible(); var writer = PhaseSixA4Tests.Writer(f);
            string id = PhaseSixA4Tests.FloorTwo(f);
            var preview = writer.PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Activate, id);
            var action = FloorLifecycleAction.Activate;
            if (scenario == "other-target") id = f.State.Floors[0].FloorInstanceId;
            if (scenario == "invalid-target") id = "missing.floor";
            if (scenario == "other-action") action = FloorLifecycleAction.Deactivate;
            if (scenario == "new-session")
                f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, action, id));
            var before = f.Session.GetCurrentBytes(); string live = JsonUtility.ToJson(f.Runtime);
            var rejected = writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, action, id, preview);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("floor.lifecycle.stale_preview"));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live));
        }

        [TestCase(null)][TestCase("")][TestCase("missing.floor")]
        public void MissingExplicitSelectionFailsClosed(string id)
        {
            var f = PhaseSixA4Tests.Eligible(); var writer = PhaseSixA4Tests.Writer(f);
            var before = f.Session.GetCurrentBytes();
            foreach (var action in new[] { FloorLifecycleAction.Activate, FloorLifecycleAction.Deactivate })
            {
                Assert.That(writer.PreviewFloorLifecycle(f.Session, f.Runtime, action, id).IsCommittable, Is.False);
                Assert.That(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, action, id).IsSuccess, Is.False);
            }
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void RepeatedDeactivationIsNoOpWithoutReplacementOrPublication()
        {
            var f = PhaseSixA4Tests.Eligible(); var writer = PhaseSixA4Tests.Writer(f);
            var before = f.Session.GetCurrentBytes();
            var preview = writer.PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Deactivate, PhaseSixA4Tests.FloorTwo(f));
            Assert.That(preview.IsNoOp, Is.True);
            var result = writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Deactivate, PhaseSixA4Tests.FloorTwo(f), preview);
            Assert.That(result.IsSuccess && result.IsNoOp, Is.True);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void SuffixConsequencesAreDerivedFromChangedStateAndOrderedCanonically()
        {
            // Test-only identities exercise the generic suffix primitive. No production content is added.
            var before = new DetachedCanonicalSpatialSaveState { Floors = new[] {
                Floor("test.floor.3", 2, FloorActivationState.Active),
                Floor("test.floor.1", 0, FloorActivationState.Active),
                Floor("test.floor.4", 3, FloorActivationState.Inactive),
                Floor("test.floor.2", 1, FloorActivationState.Active) } };
            var after = JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(JsonUtility.ToJson(before));
            DetachedCanonicalWriteAuthority.ApplyDetachedActivation(after, "test.floor.2", false);
            var preview = new FloorLifecyclePreview { Action = FloorLifecycleAction.Deactivate,
                Changes = DetachedCanonicalWriteAuthority.DescribeChanges(before, after) };
            Assert.That(preview.RequiresConfirmation, Is.True);
            Assert.That(preview.Changes.Select(c => c.FloorInstanceId), Is.EqualTo(new[] { "test.floor.2", "test.floor.3" }));
            Assert.That(before.Floors.Count(f => f.ActivationState == FloorActivationState.Active), Is.EqualTo(3));
        }

        private static SavedSpatialFloor Floor(string id, int index, FloorActivationState state) =>
            new SavedSpatialFloor { FloorInstanceId = id, FloorIndex = index, ActivationState = state };

        [Test]
        public void NewFloorBindingsHaveUniqueReadableLocalizationAndValidFormats()
        {
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            Assert.That(table.entries.GroupBy(e => e.key).Any(g => g.Count() != 1), Is.False);
            var keys = new[] { "floor.lifecycle.stale_preview", "ui.dungeon.floors.manage", "ui.dungeon.floors.deactivate",
                "ui.dungeon.floors.lifecycle_ready", "ui.dungeon.floors.opportunity", "ui.dungeon.floors.permit_met",
                "ui.dungeon.floors.permit_missing", "ui.dungeon.floors.no_opportunity", "ui.dungeon.floors.cascade" };
            foreach (var key in keys)
            {
                string text = table.entries.Single(e => e.key == key).text;
                Assert.That(text, Is.Not.Empty.And.Not.EqualTo(key));
                Assert.DoesNotThrow(() => string.Format(text, "Floor 2", "450", "Complete"), key);
            }
            var markup = new System.Xml.XmlDocument();
            markup.Load("Assets/_Project/UI/ProductionDungeon/Dungeon.uxml");
            foreach (System.Xml.XmlElement element in markup.SelectNodes("//*[@tooltip]"))
                Assert.That(table.entries.Count(e => e.key == element.GetAttribute("tooltip")), Is.EqualTo(1), element.GetAttribute("name"));
        }
    }
}
#endif
