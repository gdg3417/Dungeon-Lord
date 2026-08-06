using System;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66WindowsSpatialMigrationFileSystemTests
    {
        [Test]
        public void UnsupportedPlatformSelectionFailsClosedWithoutFileSystem()
        {
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                SpatialMigrationPlatform.Unsupported, Path.GetFullPath("save.json"));

            Assert.That(result.IsSupported, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PlatformUnsupported));
            Assert.That(result.FileSystem, Is.Null);
        }

        [Test]
        public void WindowsSelectionRejectsNonNormalizedPathBeforeNativeProbe()
        {
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                SpatialMigrationPlatform.WindowsEditor, "." + Path.DirectorySeparatorChar + "save.json");

            Assert.That(result.IsSupported, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PathInvalid));
            Assert.That(result.FileSystem, Is.Null);
        }

        [Test]
        public void CurrentNonWindowsRuntimeFailsClosed()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only_inverse");
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                Path.GetFullPath("save.json"));
            Assert.That(result.IsSupported, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PlatformUnsupported));
        }

        [Test]
        public void WindowsDurableCreateMoveReplaceDeleteAndReadback()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-qualification");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                string active = Path.Combine(directory, "save.json");
                SpatialMigrationActivationPreflight preflight = SpatialMigrationFileSystemSelector.Evaluate(active);
                if (!preflight.IsSupported) Assert.Ignore(preflight.Reason);
                ISpatialMigrationFileSystem fileSystem = preflight.FileSystem;
                string first = Path.Combine(directory, "first.tmp");
                string second = Path.Combine(directory, "second.tmp");
                string quarantine = Path.Combine(directory, "quarantine.evidence");
                fileSystem.WriteAllBytesDurable(active, new byte[] { 1 });
                fileSystem.WriteAllBytesDurable(first, new byte[] { 2 });
                fileSystem.ReplaceSameDirectoryAtomic(first, active);
                fileSystem.FlushDirectory(directory);
                CollectionAssert.AreEqual(new byte[] { 2 }, fileSystem.ReadAllBytes(active));
                fileSystem.WriteAllBytesDurable(second, new byte[] { 3 });
                fileSystem.MoveSameDirectoryAtomic(second, quarantine);
                fileSystem.FlushDirectory(directory);
                CollectionAssert.AreEqual(new byte[] { 3 }, fileSystem.ReadAllBytes(quarantine));
                fileSystem.DeleteFile(quarantine);
                Assert.That(fileSystem.Exists(quarantine), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void WindowsImplementationRejectsCrossDirectoryMove()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            var fileSystem = new WindowsSpatialMigrationFileSystem();
            Assert.Throws<IOException>(() => fileSystem.MoveSameDirectoryAtomic(
                Path.Combine(Path.GetTempPath(), "a", "source"),
                Path.Combine(Path.GetTempPath(), "b", "destination")));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WindowsTransactionExecutesLegacyFixtureToFinalizedAndReopens(bool populated)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            byte[] original = Encoding.UTF8.GetBytes(populated
                ? "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"mvpRoomSlotAssignments\":{\"Rooms\":[{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":[],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}}}"
                : "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            string directory = Path.Combine(Path.GetTempPath(), populated
                ? "gd66-winfs-transaction-populated" : "gd66-winfs-transaction-empty");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                string active = Path.Combine(directory, "save.json");
                SpatialMigrationActivationPreflight preflight = SpatialMigrationFileSystemSelector.Evaluate(active);
                if (!preflight.IsSupported) Assert.Ignore(preflight.Reason);
                File.WriteAllBytes(active, original);
                var transaction = new DetachedSpatialMigrationTransaction(preflight.FileSystem,
                    Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture));
                DetachedSpatialMigrationOutcome executed = transaction.Execute(active, fixture.Result.Attempt);
                Assert.That(executed.IsSuccess, Is.True, executed.Reason);
                Assert.That(executed.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
                DetachedSpatialMigrationOutcome reopened = new DetachedSpatialMigrationTransaction(
                    preflight.FileSystem, Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture)).Recover(active);
                Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
                Assert.That(reopened.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
