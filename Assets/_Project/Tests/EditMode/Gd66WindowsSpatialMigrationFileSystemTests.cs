#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66WindowsSpatialMigrationFileSystemTests
    {
        [Test]
        public void EvidenceProbeAndTransactionShareExtensionlessSaveStemGrammar()
        {
            SpatialContractResult<string> pattern =
                SpatialMigrationSidecarPaths.EvidenceSearchPattern("save_primary.json");
            Assert.That(pattern.IsValid, Is.True);
            Assert.That(pattern.Value, Is.EqualTo("save_primary.gd66-*"));
            string transaction = "gd66-" + new string('a', 64);
            SpatialContractResult<SpatialMigrationSidecarNames> names =
                SpatialMigrationSidecarPaths.Derive("save_primary.json", transaction);
            Assert.That(names.IsValid, Is.True);
            Assert.That(names.Value.Journal, Does.StartWith("save_primary.gd66-"));
            Assert.That(names.Value.Journal, Does.Not.StartWith("save_primary.json.gd66-"));
            Assert.That(SpatialMigrationSidecarPaths.IsOwnedEvidenceFilename(
                "save_primary.json", names.Value.Journal), Is.True);
            Assert.That(SpatialMigrationSidecarPaths.IsOwnedEvidenceFilename(
                "save_primary.json", "save_primary.gd66-lookalike.journal.json"), Is.False);
        }
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
        public void WindowsDurableCreateMoveReplaceAndReadback()
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
        public void WindowsExistingIdenticalQuarantineCollapsesThroughOneReplacement()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-collapse");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "save.gd66-journal-next.json");
                string quarantine = Path.Combine(directory, "save.gd66-quarantine-evidence.evidence");
                byte[] evidence = { 4, 5, 6 };
                fileSystem.WriteAllBytesDurable(source, evidence);
                fileSystem.WriteAllBytesDurable(quarantine, evidence);
                CollectionAssert.AreEqual(evidence, fileSystem.ReadAllBytes(quarantine));
                fileSystem.ReplaceSameDirectoryAtomic(source, quarantine);
                fileSystem.FlushDirectory(directory);
                Assert.That(fileSystem.Exists(source), Is.False);
                CollectionAssert.AreEqual(evidence, fileSystem.ReadAllBytes(quarantine));
                Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { quarantine }));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsCreateNewFailureReleasesHandleForCleanup()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-create-new");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string path = Path.Combine(directory, "existing.tmp");
                string moved = Path.Combine(directory, "moved.tmp");
                fileSystem.WriteAllBytesDurable(path, new byte[] { 1 });
                Assert.Throws<System.ComponentModel.Win32Exception>(() =>
                    fileSystem.WriteAllBytesDurable(path, new byte[] { 2 }));
                fileSystem.MoveSameDirectoryAtomic(path, moved);
                Assert.That(File.Exists(path), Is.False);
                CollectionAssert.AreEqual(new byte[] { 1 }, File.ReadAllBytes(moved));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsLockedSourceMoveFailsThenSucceedsAfterRelease()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-lock");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "source.tmp");
                string destination = Path.Combine(directory, "destination.tmp");
                fileSystem.WriteAllBytesDurable(source, new byte[] { 7 });
                using (new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Assert.Throws<System.ComponentModel.Win32Exception>(() =>
                        fileSystem.MoveSameDirectoryAtomic(source, destination));
                fileSystem.MoveSameDirectoryAtomic(source, destination);
                Assert.That(File.Exists(source), Is.False);
                CollectionAssert.AreEqual(new byte[] { 7 }, File.ReadAllBytes(destination));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsNonReplacingMoveRejectsExistingDestinationAndReleasesSourceHandle()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-existing-destination");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "source.tmp");
                string destination = Path.Combine(directory, "destination.tmp");
                fileSystem.WriteAllBytesDurable(source, new byte[] { 8 });
                fileSystem.WriteAllBytesDurable(destination, new byte[] { 9 });
                Assert.Throws<System.ComponentModel.Win32Exception>(() =>
                    fileSystem.MoveSameDirectoryAtomic(source, destination));
                fileSystem.ReplaceSameDirectoryAtomic(source, destination);
                Assert.That(File.Exists(source), Is.False);
                CollectionAssert.AreEqual(new byte[] { 8 }, File.ReadAllBytes(destination));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsLockedDestinationReplacementFailsThenSucceedsAfterRelease()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-destination-lock");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "source.tmp");
                string destination = Path.Combine(directory, "destination.tmp");
                fileSystem.WriteAllBytesDurable(source, new byte[] { 10 });
                fileSystem.WriteAllBytesDurable(destination, new byte[] { 11 });
                using (new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Assert.Throws<System.ComponentModel.Win32Exception>(() =>
                        fileSystem.ReplaceSameDirectoryAtomic(source, destination));
                fileSystem.ReplaceSameDirectoryAtomic(source, destination);
                Assert.That(File.Exists(source), Is.False);
                CollectionAssert.AreEqual(new byte[] { 10 }, File.ReadAllBytes(destination));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsMissingSourceFailsWithoutCreatingDestination()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-missing-source");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                string destination = Path.Combine(directory, "destination.tmp");
                Assert.Throws<System.ComponentModel.Win32Exception>(() =>
                    new WindowsSpatialMigrationFileSystem().MoveSameDirectoryAtomic(
                        Path.Combine(directory, "missing.tmp"), destination));
                Assert.That(File.Exists(destination), Is.False);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void ProductionRenameInteropUsesSetFileInformationByHandleNotMoveFileEx()
        {
            string[] nativeMethods = typeof(WindowsSpatialMigrationFileSystem).GetMethods(
                BindingFlags.Static | BindingFlags.NonPublic).Where(method =>
                    method.GetCustomAttributes(typeof(System.Runtime.InteropServices.DllImportAttribute), false)
                        .Length != 0).Select(method => method.Name).ToArray();
            Assert.That(nativeMethods, Does.Contain("SetFileInformationByHandle"));
            Assert.That(nativeMethods, Does.Not.Contain("MoveFileExW"));
        }

        [Test]
        public void RenameInfoBufferUsesRuntimeLayoutExactUtf16LengthAndTrailingNull()
        {
            Type fileSystemType = typeof(WindowsSpatialMigrationFileSystem);
            Type layoutType = fileSystemType.GetNestedType("FileRenameInfoLayout", BindingFlags.NonPublic);
            Assert.That(layoutType, Is.Not.Null);
            int replaceOffset = Marshal.OffsetOf(layoutType, "ReplaceIfExists").ToInt32();
            int rootOffset = Marshal.OffsetOf(layoutType, "RootDirectory").ToInt32();
            int lengthOffset = Marshal.OffsetOf(layoutType, "FileNameLength").ToInt32();
            int fileNameOffset = Marshal.OffsetOf(layoutType, "FileName").ToInt32();
            int nativeSize = Marshal.SizeOf(layoutType);
            Assert.That(replaceOffset, Is.Zero);
            Assert.That(lengthOffset, Is.EqualTo(rootOffset + IntPtr.Size));
            Assert.That(fileNameOffset, Is.EqualTo(lengthOffset + sizeof(uint)));
            Assert.That(nativeSize, Is.GreaterThanOrEqualTo(fileNameOffset + sizeof(ushort)));
            if (IntPtr.Size == 8)
            {
                Assert.That(rootOffset, Is.EqualTo(8));
                Assert.That(lengthOffset, Is.EqualTo(16));
                Assert.That(fileNameOffset, Is.EqualTo(20));
                Assert.That(nativeSize, Is.EqualTo(24));
            }

            string destination = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-layout-Ω.json"));
            byte[] expected = Encoding.Unicode.GetBytes(destination);
            MethodInfo allocate = fileSystemType.GetMethod("AllocateRenameInfo",
                BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = { destination, true, 0 };
            IntPtr buffer = (IntPtr)allocate.Invoke(null, arguments);
            try
            {
                int allocationSize = (int)arguments[2];
                Assert.That(allocationSize, Is.EqualTo(Math.Max(nativeSize,
                    fileNameOffset + expected.Length + sizeof(ushort))));
                Assert.That(Marshal.ReadByte(buffer, replaceOffset), Is.EqualTo(1));
                Assert.That(Marshal.ReadIntPtr(buffer, rootOffset), Is.EqualTo(IntPtr.Zero));
                Assert.That(Marshal.ReadInt32(buffer, lengthOffset), Is.EqualTo(expected.Length));
                byte[] actual = new byte[expected.Length];
                Marshal.Copy(IntPtr.Add(buffer, fileNameOffset), actual, 0, actual.Length);
                CollectionAssert.AreEqual(expected, actual);
                Assert.That(Marshal.ReadInt16(buffer, fileNameOffset + expected.Length), Is.Zero);
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        [Test]
        public void WindowsLongBoundedJournalReplacementUsesExactDestination()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-long-journal");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string stem = "save." + new string('a', 64) + ".journal.json";
                string journal = Path.Combine(directory, stem);
                string next = journal + ".next";
                byte[] oldBytes = { 21 };
                byte[] nextBytes = { 22, 23 };
                fileSystem.WriteAllBytesDurable(journal, oldBytes);
                fileSystem.WriteAllBytesDurable(next, nextBytes);
                fileSystem.ReplaceSameDirectoryAtomic(next, journal);
                fileSystem.FlushDirectory(directory);
                Assert.That(File.Exists(next), Is.False);
                CollectionAssert.AreEqual(nextBytes, File.ReadAllBytes(journal));
                Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { journal }));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsSuccessfulReplacementPreservesExactSourceFileIdentity()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-file-identity");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "source.tmp");
                string destination = Path.Combine(directory, "destination.tmp");
                byte[] sourceBytes = { 25, 26 };
                fileSystem.WriteAllBytesDurable(source, sourceBytes);
                fileSystem.WriteAllBytesDurable(destination, new byte[] { 27 });
                MethodInfo readIdentity = typeof(WindowsSpatialMigrationFileSystem).GetMethod("ReadIdentity",
                    BindingFlags.Static | BindingFlags.NonPublic);
                object before;
                using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                    before = readIdentity.Invoke(null, new object[] { stream.SafeFileHandle });
                fileSystem.ReplaceSameDirectoryAtomic(source, destination);
                object after;
                using (var stream = new FileStream(destination, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                    after = readIdentity.Invoke(null, new object[] { stream.SafeFileHandle });
                Type identityType = before.GetType();
                Assert.That(identityType.GetField("VolumeSerialNumber").GetValue(after),
                    Is.EqualTo(identityType.GetField("VolumeSerialNumber").GetValue(before)));
                Assert.That(identityType.GetField("FileIndexHigh").GetValue(after),
                    Is.EqualTo(identityType.GetField("FileIndexHigh").GetValue(before)));
                Assert.That(identityType.GetField("FileIndexLow").GetValue(after),
                    Is.EqualTo(identityType.GetField("FileIndexLow").GetValue(before)));
                Assert.That(File.Exists(source), Is.False);
                CollectionAssert.AreEqual(sourceBytes, File.ReadAllBytes(destination));
                Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { destination }));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsPathBoundFailurePreservesSourceBeforeNativeMutation()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string directory = Path.Combine(Path.GetTempPath(), "gd66-winfs-path-bound");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                var fileSystem = new WindowsSpatialMigrationFileSystem();
                string source = Path.Combine(directory, "source.tmp");
                byte[] bytes = { 24 };
                fileSystem.WriteAllBytesDurable(source, bytes);
                string destination = Path.Combine(directory, new string('b',
                    SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters));
                Assert.Throws<PathTooLongException>(() =>
                    fileSystem.MoveSameDirectoryAtomic(source, destination));
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(source));
                Assert.That(File.Exists(destination), Is.False);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void WindowsPreflightRejectsReparseAncestor()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            string root = Path.Combine(Path.GetTempPath(), "gd66-winfs-reparse");
            string target = Path.Combine(root, "target");
            string ordinaryChild = Path.Combine(target, "ordinary-child");
            string junction = Path.Combine(root, "junction");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(ordinaryChild);
            try
            {
                var start = new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + junction + "\" \"" + target + "\"")
                { UseShellExecute = false, CreateNoWindow = true };
                using (Process process = Process.Start(start))
                { process.WaitForExit(); Assert.That(process.ExitCode, Is.Zero, "gd66.test.junction_setup_failed"); }
                Assert.That((File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0, Is.True);
                Assert.That((File.GetAttributes(ordinaryChild) & FileAttributes.ReparsePoint) == 0, Is.True);
                SpatialMigrationActivationPreflight result = SpatialMigrationFileSystemSelector.Evaluate(
                    Path.Combine(junction, "ordinary-child", "save.json"));
                Assert.That(result.IsSupported, Is.False);
                Assert.That(result.Reason, Is.EqualTo(SpatialMigrationCapabilityReason.PathRedirected));
                Assert.That(result.FileSystem, Is.Null);
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
                ? "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"mvpRoomSlotAssignments\":{\"Rooms\":[{\"FloorIndex\":0,\"RoomIndex\":0,\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":[\"placement.option.monster.skeleton\"],\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}],\"NextRevision\":3}}}"
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
                var diagnostic = new DiagnosticFileSystem(preflight.FileSystem);
                var transaction = new DetachedSpatialMigrationTransaction(diagnostic,
                    Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture));
                DetachedSpatialMigrationOutcome executed = transaction.Execute(active, fixture.Result.Attempt);
                Assert.That(executed.IsSuccess, Is.True, executed.Reason + "\n" + diagnostic.Trace);
                Assert.That(executed.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
                CollectionAssert.AreEqual(fixture.Result.Attempt.Candidate.GetBytes(), File.ReadAllBytes(active));
                DetachedSpatialMigrationOutcome reopened = new DetachedSpatialMigrationTransaction(
                    diagnostic, Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture)).Recover(active);
                Assert.That(reopened.IsSuccess, Is.True, reopened.Reason + "\n" + diagnostic.Trace);
                Assert.That(reopened.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
                CollectionAssert.AreEqual(fixture.Result.Attempt.Candidate.GetBytes(), File.ReadAllBytes(active));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WindowsPersistedReplacedRecoveryFinalizesWithoutRepeatingActiveReplacement(bool includeBackup)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("gd66.test.windows_only");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            string directory = Path.Combine(Path.GetTempPath(), includeBackup
                ? "gd66-winfs-replaced-backup" : "gd66-winfs-replaced-no-backup");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            try
            {
                string active = Path.Combine(directory, "save.json");
                SpatialMigrationActivationPreflight preflight = SpatialMigrationFileSystemSelector.Evaluate(active);
                Assert.That(preflight.IsSupported, Is.True, preflight.Reason);
                var persisted = Gd66DetachedSpatialMigrationTransactionTests.BuildPersistedJournalFixture(
                    fixture, active, SpatialMigrationJournalStage.Replaced);
                byte[] candidate = fixture.Result.Attempt.Candidate.GetBytes();
                File.WriteAllBytes(active, candidate);
                preflight.FileSystem.WriteAllBytesDurable(Path.Combine(directory, persisted.Names.Journal),
                    persisted.JournalBytes);
                if (includeBackup) preflight.FileSystem.WriteAllBytesDurable(
                    Path.Combine(directory, persisted.Names.OriginalBackup), fixture.Original);
                preflight.FileSystem.FlushDirectory(directory);
                var diagnostic = new DiagnosticFileSystem(preflight.FileSystem);
                var counting = new CountingFileSystem(diagnostic, active);
                DetachedSpatialMigrationOutcome recovered = new DetachedSpatialMigrationTransaction(counting,
                    Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture)).Recover(active);
                Assert.That(recovered.IsSuccess, Is.True, recovered.Reason + "\n" + diagnostic.Trace);
                Assert.That(recovered.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
                Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
                Assert.That(counting.ActiveReplacementCount, Is.Zero);
                CollectionAssert.AreEqual(candidate, File.ReadAllBytes(active));
                DetachedSpatialMigrationOutcome reopened = new DetachedSpatialMigrationTransaction(counting,
                    Gd66DetachedSpatialMigrationTransactionTests.Recovery(fixture)).Recover(active);
                Assert.That(reopened.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
                Assert.That(counting.ActiveReplacementCount, Is.Zero);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
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
            public void DeleteFile(string path) { }
            public bool Exists(string path) => false;
            public byte[] ReadAllBytes(string path) => throw new NotSupportedException();
            public void WriteAllBytesDurable(string path, byte[] bytes) => throw new NotSupportedException();
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath) => throw new NotSupportedException();
            public void FlushDirectory(string directoryPath) => throw new NotSupportedException();
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directoryPath,
                string searchPattern, int maximumResults) => throw new NotSupportedException();
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path) => false;
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath) => throw new NotSupportedException();
        }

        private sealed class CountingFileSystem : ISpatialMigrationFileSystem
        {
            private readonly ISpatialMigrationFileSystem inner;
            private readonly string activePath;
            internal CountingFileSystem(ISpatialMigrationFileSystem inner, string activePath)
            { this.inner = inner; this.activePath = Path.GetFullPath(activePath); }
            internal int ActiveReplacementCount { get; private set; }
            public bool Exists(string path) => inner.Exists(path);
            public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
            public void WriteAllBytesDurable(string path, byte[] bytes) => inner.WriteAllBytesDurable(path, bytes);
            public void ReplaceSameDirectoryAtomic(string stagingPath, string destinationPath)
            {
                if (string.Equals(Path.GetFullPath(destinationPath), activePath,
                    StringComparison.OrdinalIgnoreCase)) ActiveReplacementCount++;
                inner.ReplaceSameDirectoryAtomic(stagingPath, destinationPath);
            }
            public void FlushDirectory(string directoryPath) => inner.FlushDirectory(directoryPath);
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directoryPath,
                string searchPattern, int maximumResults) => inner.EnumerateFiles(directoryPath,
                    searchPattern, maximumResults);
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path) =>
                inner.IsPathContainedWithoutRedirection(directoryPath, path);
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath) =>
                inner.MoveSameDirectoryAtomic(sourcePath, destinationPath);
            public void DeleteFile(string path) => inner.DeleteFile(path);
        }

        private sealed class DiagnosticFileSystem : ISpatialMigrationFileSystem
        {
            private readonly ISpatialMigrationFileSystem inner;
            private readonly StringBuilder trace = new StringBuilder();
            internal DiagnosticFileSystem(ISpatialMigrationFileSystem inner) { this.inner = inner; }
            internal string Trace => trace.ToString();
            public bool Exists(string path) => inner.Exists(path);
            public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
            public void WriteAllBytesDurable(string path, byte[] bytes) => Record("Write", path, null,
                () => inner.WriteAllBytesDurable(path, bytes));
            public void ReplaceSameDirectoryAtomic(string source, string destination) => Record("Replace",
                source, destination, () => inner.ReplaceSameDirectoryAtomic(source, destination));
            public void FlushDirectory(string directoryPath) => Record("Flush", directoryPath, null,
                () => inner.FlushDirectory(directoryPath));
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directoryPath,
                string searchPattern, int maximumResults) => inner.EnumerateFiles(directoryPath,
                    searchPattern, maximumResults);
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path) =>
                inner.IsPathContainedWithoutRedirection(directoryPath, path);
            public void MoveSameDirectoryAtomic(string source, string destination) => Record("Move", source,
                destination, () => inner.MoveSameDirectoryAtomic(source, destination));
            public void DeleteFile(string path) => inner.DeleteFile(path);

            private void Record(string operation, string source, string destination, Action action)
            {
                bool destinationExisted = destination != null && SafeExists(destination);
                try
                {
                    action();
                    Append(operation, source, destination, destinationExisted, true, null);
                }
                catch (Exception exception)
                {
                    Append(operation, source, destination, destinationExisted, false, exception);
                    throw;
                }
            }

            private void Append(string operation, string source, string destination,
                bool destinationExisted, bool completed, Exception exception)
            {
                var native = exception as System.ComponentModel.Win32Exception;
                string line = operation + " source=" + source + " destination=" + (destination ?? "<none>") +
                    " destinationExisted=" + destinationExisted + " completed=" + completed +
                    " exception=" + (exception == null ? "<none>" : exception.GetType().FullName) +
                    " nativeError=" + (native == null ? "<none>" : native.NativeErrorCode.ToString()) +
                    " message=" + (exception == null ? "<none>" : exception.Message) +
                    " sourceAfter=" + Snapshot(source) + " destinationAfter=" + Snapshot(destination);
                trace.AppendLine(line);
                TestContext.Progress.WriteLine(line);
            }

            private bool SafeExists(string path)
            { try { return path != null && inner.Exists(path); } catch { return false; } }

            private string Snapshot(string path)
            {
                if (string.IsNullOrEmpty(path)) return "<none>";
                try
                {
                    if (!inner.Exists(path)) return "absent";
                    byte[] bytes = inner.ReadAllBytes(path);
                    return "present:" + bytes.Length + ":" + SpatialContractSha256.Compute(bytes);
                }
                catch (Exception exception) { return "unreadable:" + exception.GetType().Name; }
            }
        }
    }
}
#endif
