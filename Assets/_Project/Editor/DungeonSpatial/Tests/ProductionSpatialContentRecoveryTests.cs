#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Editor.DungeonSpatial.Tests
{
    public sealed class ProductionSpatialContentRecoveryTests
    {
        private string root;
        private string source;
        private readonly List<ProductionSpatialPublicationFailurePoint> observed = new List<ProductionSpatialPublicationFailurePoint>();

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "DungeonLord_GD65B3A_Recovery");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(root);
            source = Path.Combine(root, "ContentAuthoring", "DungeonSpatial");
            CopyDirectory(DungeonSpatialAuthoringRepository.PackageRoot, source);
            string limits = Path.Combine(root, ProductionSpatialContentPublicationService.LimitsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(limits));
            File.Copy(ProductionSpatialContentPublicationService.LimitsPath, limits);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void InitialPublication_WritesExactValidatedSet_AndLeavesNoResidue()
        {
            ProductionSpatialPublicationResult result = Publish();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.PublicationSucceeded));
            Assert.That(ReadTargets().Files.Select(file => file.Path), Is.EqualTo(ProductionSpatialGeneratedSetParser.RequiredPaths));
            Assert.That(ParseTargets().Success, Is.True);
            Assert.That(Directory.Exists(Workspace()), Is.False);
            Assert.That(Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories), Is.Empty);
            Assert.That(observed, Is.Empty);
        }

        [Test]
        public void ValidPriorSet_IsReplacedByByteDistinctValidCandidate()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Publish().Success, Is.True);
            byte[][] installed = Bytes();
            Assert.That(installed.Where((bytes, index) => !bytes.SequenceEqual(prior[index])).Any(), Is.True);
            Assert.That(ParseTargets().Success, Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PartialExistingSets_FailBeforeWorkspaceOrTargetMutation(int count)
        {
            ProductionSpatialGeneratedSet candidate = BuildCandidate();
            for (int i = 0; i < count; i++) WriteTarget(i, candidate.Files[i].Bytes);
            byte[][] before = ExistingBytes();
            ProductionSpatialPublicationResult result = Publish();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.InvalidExistingTargetState));
            Assert.That(ExistingBytes(), Is.EqualTo(before));
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [Test]
        public void InvalidAuthoringAndMissingOrInvalidLimits_LeaveCompleteTargetsByteIdentical()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] before = Bytes();
            File.AppendAllText(Path.Combine(source, "tables", "localization_en.csv"), "unexpected,row\n");
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            Assert.That(Bytes(), Is.EqualTo(before));
            RestoreSource();
            File.WriteAllText(Path.Combine(root, ProductionSpatialContentPublicationService.LimitsPath), "{}");
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            Assert.That(Bytes(), Is.EqualTo(before));
            File.Delete(Path.Combine(root, ProductionSpatialContentPublicationService.LimitsPath));
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            Assert.That(Bytes(), Is.EqualTo(before));
        }

        [TestCase(ProductionSpatialPublicationFailurePoint.BeforeJournalCreation, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterStaging, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterBackupCreation, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterJournalFlush, true)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement, true)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterIntermediateTargetReplacement, true)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation, true)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup, true)]
        public void EveryInterruptionPoint_IsDeterministicAndJournaledOnceInstallationCanBegin(
            ProductionSpatialPublicationFailurePoint point, bool journalExpected)
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            ProductionSpatialPublicationResult interrupted = Publish(fail: current => { observed.Add(current); if (current == point) throw new Exception(); });
            Assert.That(interrupted.Success, Is.False);
            Assert.That(observed.Count(current => current == point), Is.EqualTo(1));
            Assert.That(File.Exists(Journal()), Is.EqualTo(journalExpected));
            if (journalExpected)
            {
                ProductionSpatialPublicationResult recovered = Recover();
                Assert.That(recovered.Status, Is.AnyOf(
                    ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet,
                    ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet));
                Assert.That(ParseTargets().Success, Is.True);
                Assert.That(Directory.Exists(Workspace()), Is.False);
            }
            else
            {
                CollectionAssert.AreEqual(prior, Bytes());
                Assert.That(Publish().Success, Is.True, "A later publication clears deterministic pre-journal residue.");
            }
        }

        [Test]
        public void InterruptedInitialPublication_RestoresRecordedAllAbsentState_WhenNewSetIsCorrupt()
        {
            Assert.That(Publish(fail: point => { if (point == ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement) throw new Exception(); }).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            ProductionSpatialPublicationResult recovered = Recover();
            Assert.That(recovered.Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState));
            Assert.That(ProductionSpatialGeneratedSetParser.RequiredPaths.Any(path => File.Exists(Path.Combine(root, path))), Is.False);
        }

        [Test]
        public void CorruptMixedTargets_RestoreCompletePriorBackups_NeverMixedFiles()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Publish(fail: point => { if (point == ProductionSpatialPublicationFailurePoint.AfterIntermediateTargetReplacement) throw new Exception(); }).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            ProductionSpatialPublicationResult recovered = Recover();
            Assert.That(recovered.Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet));
            CollectionAssert.AreEqual(prior, Bytes());
            Assert.That(ParseTargets().Success, Is.True);
        }

        [Test]
        public void CorruptTargetsAndBackups_FailClosedAndPreserveAllEvidence()
        {
            Assert.That(Publish().Success, Is.True);
            ChangeTestLocalization();
            Assert.That(Publish(fail: point => { if (point == ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement) throw new Exception(); }).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            File.WriteAllText(Path.Combine(Workspace(), "backup", "0.json"), "corrupt");
            string[] evidence = Directory.GetFiles(Workspace(), "*", SearchOption.AllDirectories);
            ProductionSpatialPublicationResult result = Recover();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.UnrecoverableTransaction));
            CollectionAssert.AreEquivalent(evidence, Directory.GetFiles(Workspace(), "*", SearchOption.AllDirectories));
        }

        [Test]
        public void CorruptStagedSet_IsRejectedAndCompletePriorSetIsSelected()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Publish(fail: point => { if (point == ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation) throw new Exception(); }).Success, Is.False);
            File.WriteAllText(Path.Combine(Workspace(), "staged", "0.json"), "corrupt");
            ProductionSpatialPublicationResult recovered = Recover();
            Assert.That(recovered.Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet));
            CollectionAssert.AreEqual(prior, Bytes());
        }

        [Test]
        public void RecoveryRunsBeforeLaterPublicationAndThenPublishesCandidate()
        {
            Assert.That(Publish().Success, Is.True);
            ChangeTestLocalization();
            Assert.That(Publish(fail: point => { if (point == ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement) throw new Exception(); }).Success, Is.False);
            Assert.That(File.Exists(Journal()), Is.True);
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PublicationSucceeded));
            Assert.That(ParseTargets().Success, Is.True);
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [TestCase("{", ProductionSpatialPublicationDiagnostic.JournalMalformed)]
        [TestCase("\"schema\":", ProductionSpatialPublicationDiagnostic.JournalFieldDuplicate)]
        [TestCase("\"Schema\":", ProductionSpatialPublicationDiagnostic.JournalFieldCaseAmbiguous)]
        [TestCase("\"unknown\": 1,", ProductionSpatialPublicationDiagnostic.JournalFieldUnknown)]
        public void MalformedDuplicateAmbiguousAndUnknownJournalFields_AreRejected(string mutation,
            ProductionSpatialPublicationDiagnostic expected)
        {
            CreateJournal();
            if (mutation == "{") File.WriteAllText(Journal(), mutation);
            else
            {
                string json = File.ReadAllText(Journal());
                if (expected == ProductionSpatialPublicationDiagnostic.JournalFieldDuplicate)
                    json = json.Replace("\"schema\":", "\"schema\": \"dungeon_spatial_publication_journal\",\n  \"schema\":");
                else if (expected == ProductionSpatialPublicationDiagnostic.JournalFieldCaseAmbiguous)
                    json = json.Replace("\"schema\":", "\"Schema\": \"dungeon_spatial_publication_journal\",\n  \"schema\":");
                else json = json.Replace("{\n", "{\n  \"unknown\": 1,\n");
                File.WriteAllText(Journal(), json);
            }
            ProductionSpatialPublicationResult result = Recover();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.InvalidJournal));
            CollectionAssert.Contains(result.Diagnostics, expected);
            Assert.That(File.Exists(Journal()), Is.True);
        }

        [Test]
        public void TraversalHashAndContentVersionJournalMutations_AreRejectedDeterministically()
        {
            foreach (Func<string,string> mutation in new Func<string,string>[]
            {
                json => json.Replace("Temp/DungeonSpatialProductionPublication/staged/0.json", "../outside.json"),
                CorruptFirstStagedHash,
                json => json.Replace("\"contentVersion\": \"0.1.0\"", "\"contentVersion\": \"9\"")
            })
            {
                if (Directory.Exists(Workspace())) Directory.Delete(Workspace(), true);
                CreateJournal();
                File.WriteAllText(Journal(), mutation(File.ReadAllText(Journal())));
                ProductionSpatialPublicationResult first = Recover();
                ProductionSpatialPublicationResult second = Recover();
                Assert.That(second.Status, Is.EqualTo(first.Status));
                CollectionAssert.AreEqual(first.Diagnostics, second.Diagnostics);
            }
        }

        [Test]
        public void RefreshOccursOnlyAfterAllCanonicalReplacements_AndRefreshFailureIsRecoverable()
        {
            int refreshes = 0;
            Assert.That(Publish(refresh: () =>
            {
                refreshes++;
                Assert.That(ProductionSpatialGeneratedSetParser.RequiredPaths.All(path => File.Exists(Path.Combine(root, path))), Is.True);
                Assert.That(ParseTargets().Success, Is.True);
            }).Success, Is.True);
            Assert.That(refreshes, Is.EqualTo(1));
            ChangeTestLocalization();
            Assert.That(Publish(refresh: () => throw new Exception()).Success, Is.False);
            Assert.That(File.Exists(Journal()), Is.True);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet));
        }

        [Test]
        public void SourceLimitsAndBootstrapFilesRemainByteIdenticalAndAreNeverTransactionPaths()
        {
            Dictionary<string,byte[]> sourceBefore = Snapshot(source);
            string limit = Path.Combine(root, ProductionSpatialContentPublicationService.LimitsPath);
            byte[] limitsBefore = File.ReadAllBytes(limit);
            string[] bootstrap = Directory.GetFiles("Assets/_Project/Data/Bootstrap", "*", SearchOption.AllDirectories);
            Dictionary<string,byte[]> bootstrapBefore = bootstrap.ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal);
            Assert.That(Publish().Success, Is.True);
            AssertSnapshots(sourceBefore, Snapshot(source));
            CollectionAssert.AreEqual(limitsBefore, File.ReadAllBytes(limit));
            AssertSnapshots(bootstrapBefore, bootstrap.ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal));
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        private ProductionSpatialPublicationResult Publish(Action refresh=null, Action<ProductionSpatialPublicationFailurePoint> fail=null) =>
            ProductionSpatialContentPublicationService.Publish(new ProductionSpatialPublicationContext(root, source, refresh ?? (()=>{}), fail));
        private ProductionSpatialPublicationResult Recover() => ProductionSpatialContentPublicationService.Recover(new ProductionSpatialPublicationContext(root, source, ()=>{}));
        private void CreateJournal() { Assert.That(Publish(fail: point => { if(point==ProductionSpatialPublicationFailurePoint.AfterJournalFlush)throw new Exception(); }).Success, Is.False); }
        private string Workspace() => Path.Combine(root, ProductionSpatialContentPublicationService.TransactionWorkspacePath);
        private string Journal() => Path.Combine(Workspace(), "journal.json");
        private string Target(int index) => Path.Combine(root, ProductionSpatialGeneratedSetParser.RequiredPaths[index]);
        private void WriteTarget(int index, byte[] bytes) { Directory.CreateDirectory(Path.GetDirectoryName(Target(index))); File.WriteAllBytes(Target(index), bytes); }
        private byte[][] Bytes() => ProductionSpatialGeneratedSetParser.RequiredPaths.Select(path => File.ReadAllBytes(Path.Combine(root,path))).ToArray();
        private byte[][] ExistingBytes() => ProductionSpatialGeneratedSetParser.RequiredPaths.Where(path=>File.Exists(Path.Combine(root,path))).Select(path=>File.ReadAllBytes(Path.Combine(root,path))).ToArray();
        private ProductionSpatialGeneratedSet ReadTargets() => new ProductionSpatialGeneratedSet(ProductionSpatialGeneratedSetParser.RequiredPaths.Select(path=>new ProductionSpatialGeneratedFile(path,File.ReadAllBytes(Path.Combine(root,path)))));
        private ProductionSpatialGeneratedSetResult ParseTargets() => ProductionSpatialGeneratedSetParser.ParseAndValidate(ReadTargets(), Limits());
        private SpatialContentValidationWorkloadLimits Limits() => ProductionSpatialContentWorkloadLimitParser.Parse(File.ReadAllText(Path.Combine(root,ProductionSpatialContentPublicationService.LimitsPath))).Limits;
        private ProductionSpatialGeneratedSet BuildCandidate() { var projection=DungeonSpatialAuthoringPackageParser.ParseAndProject(DungeonSpatialAuthoringRepository.Read(source),Limits(),true);return ProductionSpatialGeneratedSetBuilder.Build(projection.Projection,Limits()).Output; }
        private void ChangeTestLocalization() { string path=Path.Combine(source,"tables","localization_en.csv");string text=File.ReadAllText(path);int newline=text.IndexOf('\n');int comma=text.IndexOf(',',newline+1);int end=text.IndexOf('\n',comma+1);File.WriteAllText(path,text.Substring(0,comma+1)+"Test-only alternate text"+text.Substring(end)); }
        private void RestoreSource(){Directory.Delete(source,true);CopyDirectory(DungeonSpatialAuthoringRepository.PackageRoot,source);}
        private static string CorruptFirstStagedHash(string json)
        {
            int field = json.IndexOf("\"stagedHashes\": [\"", StringComparison.Ordinal);
            int hash = field + "\"stagedHashes\": [\"".Length;
            return json.Substring(0, hash) + "x" + json.Substring(hash + 1);
        }
        private static void CopyDirectory(string from,string to){foreach(string directory in Directory.GetDirectories(from,"*",SearchOption.AllDirectories))Directory.CreateDirectory(directory.Replace(from,to));Directory.CreateDirectory(to);foreach(string file in Directory.GetFiles(from,"*",SearchOption.AllDirectories)){string target=file.Replace(from,to);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,true);}}
        private static Dictionary<string,byte[]> Snapshot(string path)=>Directory.GetFiles(path,"*",SearchOption.AllDirectories).ToDictionary(file=>file.Substring(path.Length),File.ReadAllBytes,StringComparer.Ordinal);
        private static void AssertSnapshots(Dictionary<string,byte[]> expected,Dictionary<string,byte[]> actual){CollectionAssert.AreEquivalent(expected.Keys,actual.Keys);foreach(string key in expected.Keys)CollectionAssert.AreEqual(expected[key],actual[key]);}
    }
}
#endif
