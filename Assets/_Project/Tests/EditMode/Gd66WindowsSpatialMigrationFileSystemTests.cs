using System;
using System.IO;
using System.Text;
using System.Diagnostics;
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
                Assert.That(preflight.IsSupported, Is.True, preflight.Reason);
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
                string[] retired = Directory.GetFiles(directory, "gd66.retired-*.evidence");
                Assert.That(retired, Has.Length.EqualTo(1));
                CollectionAssert.AreEqual(new byte[] { 3 }, File.ReadAllBytes(retired[0]));
                fileSystem.WriteAllBytesDurable(quarantine, new byte[] { 3 });
                fileSystem.DeleteFile(quarantine);
                Assert.That(fileSystem.Exists(quarantine), Is.False);
                Assert.That(Directory.GetFiles(directory, "gd66.retired-*.evidence"), Has.Length.EqualTo(1));
                Assert.Throws<FileNotFoundException>(() => fileSystem.DeleteFile(quarantine));
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

        [Test]
        public void WindowsPreflightRejectsReparseAncestor()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string root = Path.Combine(Path.GetTempPath(), "gd66-winfs-reparse");
            string target = Path.Combine(root, "target");
            string junction = Path.Combine(root, "junction");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(target);
            try
            {
                var start = new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + junction + "\" \"" + target + "\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using (Process process = Process.Start(start))
                { process.WaitForExit(); Assert.That(process.ExitCode, Is.Zero, "gd66.test.junction_setup_failed"); }
                SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                    Path.Combine(junction, "save.json"));
                Assert.That(result.IsSupported, Is.False);
                Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PathRedirected));
            }
            finally
            {
                if (Directory.Exists(junction)) Directory.Delete(junction);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
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
                Assert.That(preflight.IsSupported, Is.True, preflight.Reason);
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

        [TestCase(SpatialMigrationCapabilityReason.Ready, true)]
        [TestCase(SpatialMigrationCapabilityReason.PathRedirected, false)]
        [TestCase(SpatialMigrationCapabilityReason.VolumeUnsupported, false)]
        [TestCase(SpatialMigrationCapabilityReason.NativeProbeFailed, false)]
        public void EveryProbeCapabilityCodeHasAnExactEmissionTest(string reason, bool expectedSupported)
        {
            string active = Path.GetFullPath("save.json");
            var probe = new CapabilityProbe(true,
                reason == SpatialMigrationCapabilityReason.Ready ? null : reason);
            var fileSystem = new SelectorFileSystem();
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                SpatialMigrationPlatform.WindowsStandalone, active, probe, fileSystem);
            Assert.That(result.IsSupported, Is.EqualTo(expectedSupported));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.FileSystem, Is.EqualTo(expectedSupported ? fileSystem : null));
        }

        [Test]
        public void RedirectedContainmentHasDistinctExactEmission()
        {
            string active = Path.GetFullPath("save.json");
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                SpatialMigrationPlatform.WindowsEditor, active, new CapabilityProbe(false, null),
                new SelectorFileSystem());
            Assert.That(result.IsSupported, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PathRedirected));
        }

        [Test]
        public void ProbeExceptionFailsClosedAsNativeProbeFailure()
        {
            string active = Path.GetFullPath("save.json");
            SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                SpatialMigrationPlatform.WindowsEditor, active, new CapabilityProbe(true, null, true),
                new SelectorFileSystem());
            Assert.That(result.IsSupported, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.NativeProbeFailed));
        }

        private sealed class CapabilityProbe : IWindowsSpatialMigrationCapabilityProbe
        {
            private readonly bool contained;
            private readonly string result;
            private readonly bool throws;
            internal CapabilityProbe(bool contained, string result, bool throws = false)
            { this.contained = contained; this.result = result; this.throws = throws; }
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path) => contained;
            public string ProbeSupportedVolume(string directoryPath)
            { if (throws) throw new IOException(); return result; }
        }

        private sealed class SelectorFileSystem : ISpatialMigrationFileSystem
        {
            public bool Exists(string path) => false;
            public byte[] ReadAllBytes(string path) => throw new NotSupportedException();
            public void WriteAllBytesDurable(string path, byte[] bytes) => throw new NotSupportedException();
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath) => throw new NotSupportedException();
            public void FlushDirectory(string directoryPath) => throw new NotSupportedException();
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directoryPath,
                string searchPattern, int maximumResults) => throw new NotSupportedException();
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path) => false;
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath) => throw new NotSupportedException();
            public void DeleteFile(string path) => throw new NotSupportedException();
        }
    }
}
