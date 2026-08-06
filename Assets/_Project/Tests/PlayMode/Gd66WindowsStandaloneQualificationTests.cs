using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class Gd66WindowsStandaloneQualificationTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath,
                "gd66-windows-standalone-qualification");
            RemoveRoot();
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown() => RemoveRoot();

        [Test]
        public void WindowsStandalonePreflightAndNativeFilesystemQualification()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer)
                Assert.Ignore("gd66.test.windows_player_only");

            string activePath = Path.GetFullPath(Path.Combine(root, "save.json"));
            Assembly production = AppDomain.CurrentDomain.GetAssemblies().Single(value =>
                value.GetName().Name == "Assembly-CSharp");
            Type selector = production.GetType(
                "DungeonBuilder.M0.Gameplay.DungeonSpatial.SpatialMigrationFileSystemSelector", true);
            object preflight = selector.GetMethod("Evaluate", new[] { typeof(string) })
                .Invoke(null, new object[] { activePath });
            string platform = GetProperty(preflight, "Platform").ToString();
            bool supported = (bool)GetProperty(preflight, "IsSupported");
            string reason = (string)GetProperty(preflight, "Reason");
            object selectedFileSystem = GetProperty(preflight, "FileSystem");
            Assert.That(platform, Is.EqualTo("WindowsStandalone"));
            Assert.That(supported, Is.True, reason);
            Assert.That(reason, Is.EqualTo("gd66.preflight.ready"));
            Assert.That(selectedFileSystem, Is.Not.Null);
            Assert.That(selectedFileSystem.GetType().FullName, Is.EqualTo(
                "DungeonBuilder.M0.Gameplay.DungeonSpatial.WindowsSpatialMigrationFileSystem"));

            var fileSystem = new QualificationFileSystem(selectedFileSystem);
            byte[] firstBytes = { 1, 2, 3 };
            byte[] secondBytes = { 4, 5, 6 };
            string first = Path.Combine(root, "first.tmp");
            string moved = Path.Combine(root, "moved.tmp");
            string replacement = Path.Combine(root, "replacement.tmp");

            fileSystem.WriteAllBytesDurable(first, firstBytes);
            CollectionAssert.AreEqual(firstBytes, fileSystem.ReadAllBytes(first));
            fileSystem.MoveSameDirectoryAtomic(first, moved);
            fileSystem.FlushDirectory(root);
            Assert.That(fileSystem.Exists(first), Is.False);
            Assert.That(fileSystem.Exists(moved), Is.True);
            CollectionAssert.AreEqual(firstBytes, fileSystem.ReadAllBytes(moved));

            fileSystem.WriteAllBytesDurable(replacement, secondBytes);
            fileSystem.ReplaceSameDirectoryAtomic(replacement, moved);
            fileSystem.FlushDirectory(root);
            Assert.That(fileSystem.Exists(replacement), Is.False);
            CollectionAssert.AreEqual(secondBytes, fileSystem.ReadAllBytes(moved));

            AssertNonReplacingExistingDestinationFailsAndReleasesHandle(fileSystem);
            AssertMissingSourceFailsWithoutDestination(fileSystem);
            AssertCrossDirectoryFailsBeforeMutation(fileSystem);
            AssertDuplicateEvidenceCollapses(fileSystem);
            AssertSourceLockFailsThenRetries(fileSystem);
            AssertDestinationLockFailsThenRetries(fileSystem);

            Assert.That(Directory.GetFiles(root, "gd66.retired-*", SearchOption.AllDirectories), Is.Empty);
        }

        private void AssertNonReplacingExistingDestinationFailsAndReleasesHandle(
            QualificationFileSystem fileSystem)
        {
            string source = Path.Combine(root, "existing-source.tmp");
            string destination = Path.Combine(root, "existing-destination.tmp");
            fileSystem.WriteAllBytesDurable(source, new byte[] { 7 });
            fileSystem.WriteAllBytesDurable(destination, new byte[] { 8 });
            Assert.Throws<Win32Exception>(() =>
                fileSystem.MoveSameDirectoryAtomic(source, destination));
            fileSystem.ReplaceSameDirectoryAtomic(source, destination);
            Assert.That(fileSystem.Exists(source), Is.False);
            CollectionAssert.AreEqual(new byte[] { 7 }, fileSystem.ReadAllBytes(destination));
        }

        private void AssertMissingSourceFailsWithoutDestination(QualificationFileSystem fileSystem)
        {
            string destination = Path.Combine(root, "missing-destination.tmp");
            Assert.Throws<Win32Exception>(() => fileSystem.MoveSameDirectoryAtomic(
                Path.Combine(root, "missing-source.tmp"), destination));
            Assert.That(fileSystem.Exists(destination), Is.False);
        }

        private void AssertCrossDirectoryFailsBeforeMutation(QualificationFileSystem fileSystem)
        {
            string other = Path.Combine(root, "other");
            Directory.CreateDirectory(other);
            string source = Path.Combine(root, "cross-source.tmp");
            string destination = Path.Combine(other, "cross-destination.tmp");
            fileSystem.WriteAllBytesDurable(source, new byte[] { 9 });
            Assert.Throws<IOException>(() =>
                fileSystem.MoveSameDirectoryAtomic(source, destination));
            CollectionAssert.AreEqual(new byte[] { 9 }, fileSystem.ReadAllBytes(source));
            Assert.That(fileSystem.Exists(destination), Is.False);
        }

        private void AssertDuplicateEvidenceCollapses(QualificationFileSystem fileSystem)
        {
            string source = Path.Combine(root, "save.gd66-journal-next.json");
            string quarantine = Path.Combine(root, "save.gd66-quarantine-test.evidence");
            byte[] evidence = { 10, 11 };
            fileSystem.WriteAllBytesDurable(source, evidence);
            fileSystem.WriteAllBytesDurable(quarantine, evidence);
            CollectionAssert.AreEqual(evidence, fileSystem.ReadAllBytes(quarantine));
            fileSystem.ReplaceSameDirectoryAtomic(source, quarantine);
            fileSystem.FlushDirectory(root);
            Assert.That(fileSystem.Exists(source), Is.False);
            CollectionAssert.AreEqual(evidence, fileSystem.ReadAllBytes(quarantine));
            Assert.That(Directory.GetFiles(root, "*.evidence", SearchOption.TopDirectoryOnly),
                Is.EqualTo(new[] { quarantine }));
        }

        private void AssertSourceLockFailsThenRetries(QualificationFileSystem fileSystem)
        {
            string source = Path.Combine(root, "locked-source.tmp");
            string destination = Path.Combine(root, "locked-source-moved.tmp");
            fileSystem.WriteAllBytesDurable(source, new byte[] { 12 });
            using (new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.Throws<Win32Exception>(() =>
                    fileSystem.MoveSameDirectoryAtomic(source, destination));
            fileSystem.MoveSameDirectoryAtomic(source, destination);
            Assert.That(fileSystem.Exists(source), Is.False);
            CollectionAssert.AreEqual(new byte[] { 12 }, fileSystem.ReadAllBytes(destination));
        }

        private void AssertDestinationLockFailsThenRetries(QualificationFileSystem fileSystem)
        {
            string source = Path.Combine(root, "destination-lock-source.tmp");
            string destination = Path.Combine(root, "destination-lock.tmp");
            fileSystem.WriteAllBytesDurable(source, new byte[] { 13 });
            fileSystem.WriteAllBytesDurable(destination, new byte[] { 14 });
            using (new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.Throws<Win32Exception>(() =>
                    fileSystem.ReplaceSameDirectoryAtomic(source, destination));
            fileSystem.ReplaceSameDirectoryAtomic(source, destination);
            Assert.That(fileSystem.Exists(source), Is.False);
            CollectionAssert.AreEqual(new byte[] { 13 }, fileSystem.ReadAllBytes(destination));
        }

        private void RemoveRoot()
        {
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root)) Directory.Delete(root, true);
        }

        private static object GetProperty(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance, null);

        private sealed class QualificationFileSystem
        {
            private readonly object instance;
            private readonly Type type;
            internal QualificationFileSystem(object instance)
            { this.instance = instance; type = instance.GetType(); }
            internal bool Exists(string path) => (bool)Invoke("Exists", path);
            internal byte[] ReadAllBytes(string path) => (byte[])Invoke("ReadAllBytes", path);
            internal void WriteAllBytesDurable(string path, byte[] bytes) =>
                Invoke("WriteAllBytesDurable", path, bytes);
            internal void ReplaceSameDirectoryAtomic(string source, string destination) =>
                Invoke("ReplaceSameDirectoryAtomic", source, destination);
            internal void MoveSameDirectoryAtomic(string source, string destination) =>
                Invoke("MoveSameDirectoryAtomic", source, destination);
            internal void FlushDirectory(string path) => Invoke("FlushDirectory", path);
            private object Invoke(string name, params object[] arguments)
            {
                try { return type.GetMethod(name).Invoke(instance, arguments); }
                catch (TargetInvocationException exception)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    throw;
                }
            }
        }
    }
}
