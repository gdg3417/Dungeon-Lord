#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralConstructionGameRootTests
    {
        [Test]
        public void PreviewWithoutCanonicalAuthorityFailsClosedAndCommitPreservesRuntime()
        {
            var go = new GameObject("StructuralConstructionGameRootNoAuthority");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);

                StructuralEditPreview preview = root.PreviewStructuralConstruction(Request());
                DetachedCanonicalWriteResult commit = root.CommitStructuralConstruction();

                Assert.That(preview.IsValid, Is.False);
                CollectionAssert.AreEqual(new[] { StructuralEditService.InvalidContextReason },
                    preview.ReasonCodes);
                Assert.That(commit.IsSuccess, Is.False);
                Assert.That(commit.Reason, Is.EqualTo(StructuralEditService.InvalidContextReason));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.InvalidContextReason));
                Assert.That(root.Save, Is.SameAs(current));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CommitRetainedInvalidPreviewPreservesSpecificReasonAndRuntime()
        {
            var go = new GameObject("StructuralConstructionGameRootInvalidPreview");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);
                SetProperty(root, "StructuralConstructionPreview",
                    StructuralEditService.InvalidPreview(StructuralEditService.OutOfBoundsReason, Request()));

                DetachedCanonicalWriteResult result = root.CommitStructuralConstruction();

                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Reason, Is.EqualTo(StructuralEditService.OutOfBoundsReason));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.OutOfBoundsReason));
                Assert.That(root.Save, Is.SameAs(current));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CanonicalPublicationReplacesRuntimeAndClearsPreviewPresentation()
        {
            var go = new GameObject("StructuralConstructionGameRootPublication");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var oldSave = new SaveData();
                var published = new SaveData();
                SetProperty(root, "Save", oldSave);
                SetProperty(root, "StructuralConstructionPreview",
                    StructuralEditService.InvalidPreview(StructuralEditService.OutOfBoundsReason, Request()));
                SetProperty(root, "StructuralConstructionReasonKey",
                    StructuralEditService.OutOfBoundsReason);

                typeof(GameRoot).GetMethod("PublishCanonicalRuntime",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root,
                    new object[] { published });

                Assert.That(root.Save, Is.SameAs(published));
                Assert.That(root.StructuralConstructionPreview, Is.Null);
                Assert.That(root.StructuralConstructionReasonKey, Is.Empty);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SaveDeleteQuiescenceBlocksStructuralPreviewAndCommit()
        {
            var go = new GameObject("StructuralConstructionGameRootQuiescence");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);
                typeof(GameRoot).GetField("_explicitSaveDeleteQuiesced",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, true);

                StructuralEditPreview preview = root.PreviewStructuralConstruction(Request());
                DetachedCanonicalWriteResult commit = root.CommitStructuralConstruction();

                Assert.That(preview.IsValid, Is.False);
                Assert.That(commit.IsSuccess, Is.False);
                Assert.That(root.Save, Is.SameAs(current));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.InvalidContextReason));
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static StructuralConstructionRequest Request() =>
            new StructuralConstructionRequest
            {
                RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "north"
            };

        private static void SetProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
