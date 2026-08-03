#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedSpatialMigrationTransactionTests
    {
        private static readonly SpatialSerializedInputLimits Limits =
            new SpatialSerializedInputLimits(32768, 256, 32, 4096, 16);

        [Test]
        public void FinalizationReceipt_RoundTripsCanonicalThreeFieldContract()
        {
            var receipt = new DetachedFinalizationReceipt(TransactionId('1'), Hash('2'), Hash('3'));

            byte[] bytes = DetachedFinalizationReceiptContract.Serialize(receipt, Limits);
            DetachedFinalizationReceipt parsed = DetachedFinalizationReceiptContract.Parse(bytes, Limits);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.TransactionId, Is.EqualTo(receipt.TransactionId));
            Assert.That(parsed.DescriptorFingerprint, Is.EqualTo(receipt.DescriptorFingerprint));
            Assert.That(parsed.CandidateSha256, Is.EqualTo(receipt.CandidateSha256));
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Not.Contain("FinalStage"));
        }

        [TestCase("{\"TransactionId\":\"{0}\",\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}")]
        [TestCase("{\"transactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}")]
        [TestCase("{\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\",\"FinalStage\":6}")]
        public void FinalizationReceipt_RejectsDuplicateCaseAmbiguousAndExtraFields(string format)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format(format,
                TransactionId('1'), Hash('2'), Hash('3')));

            Assert.That(DetachedFinalizationReceiptContract.Parse(bytes, Limits), Is.Null);
        }

        [Test]
        public void RestorationIntent_BindsAttemptBackupAndPersistedStage()
        {
            var intent = new DetachedRestorationIntent(TransactionId('1'), Hash('2'), Hash('3'),
                Hash('3'), "save.gd66-" + new string('1', 64) + ".journal.json",
                (int)SpatialMigrationJournalStage.Replaced);

            byte[] bytes = DetachedRestorationIntentContract.Serialize(intent, Limits);
            DetachedRestorationIntent parsed = DetachedRestorationIntentContract.Parse(bytes, Limits);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.TransactionId, Is.EqualTo(intent.TransactionId));
            Assert.That(parsed.JournalFilename, Is.EqualTo(intent.JournalFilename));
            Assert.That(parsed.JournalStage, Is.EqualTo((int)SpatialMigrationJournalStage.Replaced));
        }

        [Test]
        public void DeterministicFileSystem_InjectsWriteReadReplaceMoveFlushAndEnumerationFailures()
        {
            foreach (FailurePoint point in Enum.GetValues(typeof(FailurePoint)).Cast<FailurePoint>())
            {
                if (point == FailurePoint.None) continue;
                var fileSystem = new DeterministicFileSystem(point);
                Assert.That(() => Exercise(fileSystem, point), Throws.TypeOf<IOException>(), point.ToString());
            }
        }

        [Test]
        public void GenericRuntimeFileSystem_CannotClaimDirectoryDurability()
        {
            Assert.That(() => new RuntimeSpatialMigrationFileSystem().FlushDirectory(Path.GetTempPath()),
                Throws.TypeOf<PlatformNotSupportedException>());
        }

        private static void Exercise(DeterministicFileSystem fileSystem, FailurePoint point)
        {
            if (point == FailurePoint.Write) { fileSystem.WriteAllBytesDurable("/s/a", new byte[] { 1 }); return; }
            fileSystem.Seed("/s/a", new byte[] { 1 });
            if (point == FailurePoint.Read) { fileSystem.ReadAllBytes("/s/a"); return; }
            if (point == FailurePoint.Replace) { fileSystem.ReplaceSameDirectoryAtomic("/s/a", "/s/b"); return; }
            if (point == FailurePoint.Move) { fileSystem.MoveSameDirectoryAtomic("/s/a", "/s/b"); return; }
            if (point == FailurePoint.Flush) { fileSystem.FlushDirectory("/s"); return; }
            if (point == FailurePoint.Enumerate) { fileSystem.EnumerateFiles("/s", "*", 2); return; }
            fileSystem.IsPathContainedWithoutRedirection("/s", "/s/a");
        }

        private static string Hash(char value) => new string(value, 64);
        private static string TransactionId(char value) => "gd66-" + Hash(value);

        private enum FailurePoint { None, Write, Read, Replace, Move, Flush, Enumerate, Containment }

        private sealed class DeterministicFileSystem : ISpatialMigrationFileSystem
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            private readonly FailurePoint failure;
            internal DeterministicFileSystem(FailurePoint failure) { this.failure = failure; }
            internal void Seed(string path, byte[] bytes) { files[path] = (byte[])bytes.Clone(); }
            public bool Exists(string path) => files.ContainsKey(path);
            public byte[] ReadAllBytes(string path)
            { Fail(FailurePoint.Read); return (byte[])files[path].Clone(); }
            public void WriteAllBytesDurable(string path, byte[] bytes)
            { Fail(FailurePoint.Write); files.Add(path, (byte[])bytes.Clone()); }
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath)
            { Fail(FailurePoint.Replace); files[activePath] = files[stagingPath]; files.Remove(stagingPath); }
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath)
            { Fail(FailurePoint.Move); files.Add(destinationPath, files[sourcePath]); files.Remove(sourcePath); }
            public void FlushDirectory(string directoryPath) { Fail(FailurePoint.Flush); }
            public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern,
                int maximumResults)
            { Fail(FailurePoint.Enumerate); return files.Keys.OrderBy(value => value).Take(maximumResults).ToArray(); }
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
            { Fail(FailurePoint.Containment); return path.StartsWith(directoryPath + "/", StringComparison.Ordinal); }
            private void Fail(FailurePoint point) { if (failure == point) throw new IOException(point.ToString()); }
        }
    }
}
#endif
