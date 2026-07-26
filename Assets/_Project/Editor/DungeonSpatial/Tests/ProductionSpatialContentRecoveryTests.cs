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

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "DungeonLord_GD65B3A_Recovery");
            ResetFixture();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void InitialPublication_WritesExactValidatedSet_AndLeavesNoResidue()
        {
            byte[] limitsBefore = File.ReadAllBytes(LimitsPath());
            ProductionSpatialPublicationResult result = Publish();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.PublicationSucceeded));
            AssertCompleteSet(Bytes());
            CollectionAssert.AreEqual(limitsBefore, File.ReadAllBytes(LimitsPath()));
            Assert.That(Directory.Exists(Workspace()), Is.False);
            Assert.That(Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void ValidPriorSet_IsReplacedByByteDistinctValidCandidate()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            byte[][] candidate = CandidateBytes();
            Assert.That(candidate.Where((bytes, index) => !bytes.SequenceEqual(prior[index])).Any(), Is.True);
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PublicationSucceeded));
            AssertBytes(candidate);
            AssertCompleteSet(candidate);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PartialExistingSets_FailBeforePublication(int count)
        {
            byte[][] candidate = CandidateBytes();
            for (int i = 0; i < count; i++) WriteTarget(i, candidate[i]);
            byte[][] before = ExistingBytes();
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.InvalidExistingTargetState));
            CollectionAssert.AreEqual(before, ExistingBytes());
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [Test]
        public void InvalidAuthoringAndMissingOrInvalidLimits_LeaveTargetsUnchanged()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] before = Bytes();
            File.AppendAllText(Path.Combine(source, "tables", "localization_en.csv"), "unexpected,row\n");
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            AssertBytes(before);
            RestoreSource();
            File.WriteAllText(LimitsPath(), "{}");
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            AssertBytes(before);
            File.Delete(LimitsPath());
            Assert.That(Publish().Status, Is.EqualTo(ProductionSpatialPublicationStatus.PreInstallValidationFailure));
            AssertBytes(before);
        }

        [TestCase(ProductionSpatialPublicationFailurePoint.BeforeJournalCreation)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterStaging)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterBackupCreation)]
        public void PreJournalInterruptions_LeaveCompletePreviousSetAndNoDiscoverableJournal(
            ProductionSpatialPublicationFailurePoint point)
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Interrupt(point).Success, Is.False);
            AssertBytes(prior);
            AssertCompleteSet(prior);
            Assert.That(JournalPaths().Any(File.Exists), Is.False);
        }

        [TestCase(ProductionSpatialPublicationFailurePoint.AfterJournalFlush, ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement, ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterIntermediateTargetReplacement, ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet, false)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation, ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet, true)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup, ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet, true)]
        public void JournaledInterruptions_RecoverTheExactDeterministicCompleteSet(
            ProductionSpatialPublicationFailurePoint point, ProductionSpatialPublicationStatus expectedStatus,
            bool expectCandidate)
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            byte[][] candidate = CandidateBytes();
            Assert.That(Interrupt(point).Success, Is.False);
            Assert.That(Directory.Exists(Workspace()), Is.True);

            ProductionSpatialPublicationResult recovered = Recover();
            Assert.That(recovered.Status, Is.EqualTo(expectedStatus));
            byte[][] expected = expectCandidate ? candidate : prior;
            AssertBytes(expected);
            AssertCompleteSet(expected);
            Assert.That(IsMixed(prior, candidate, Bytes()), Is.False);
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [TestCase(false, ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation)]
        [TestCase(false, ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup)]
        [TestCase(true, ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation)]
        [TestCase(true, ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup)]
        public void CompleteInstalledNewSet_IsKeptWhenStagingIsCorrupt(bool replacement,
            ProductionSpatialPublicationFailurePoint point)
        {
            if (replacement)
            {
                Assert.That(Publish().Success, Is.True);
                ChangeTestLocalization();
            }
            byte[][] candidate = CandidateBytes();
            Assert.That(Interrupt(point).Success, Is.False);
            File.WriteAllText(Staged(0), "corrupt");

            ProductionSpatialPublicationResult recovered = Recover();
            Assert.That(recovered.Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet));
            AssertBytes(candidate);
            AssertCompleteSet(candidate);
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [Test]
        public void InvalidInstalledAndCorruptStaging_RestoresValidPreviousBackup()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            File.WriteAllText(Staged(0), "corrupt");
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet));
            AssertBytes(prior);
            AssertCompleteSet(prior);
        }

        [Test]
        public void InitialInvalidInstalledAndCorruptStaging_RestoresOnlyRecordedAllAbsentState()
        {
            Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            File.WriteAllText(Staged(0), "corrupt");
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState));
            Assert.That(ProductionSpatialGeneratedSetParser.RequiredPaths.Any(path => File.Exists(Path.Combine(root, path))), Is.False);
            Assert.That(Directory.Exists(Workspace()), Is.False);
        }

        [Test]
        public void NoValidInstalledPriorOrStagedSet_FailsClosedAndPreservesEvidence()
        {
            Assert.That(Publish().Success, Is.True);
            ChangeTestLocalization();
            Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement).Success, Is.False);
            File.WriteAllText(Target(0), "corrupt");
            File.WriteAllText(Staged(0), "corrupt");
            File.WriteAllText(Backup(0), "corrupt");
            string[] evidence = Directory.GetFiles(Workspace(), "*", SearchOption.AllDirectories);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.UnrecoverableTransaction));
            CollectionAssert.AreEquivalent(evidence, Directory.GetFiles(Workspace(), "*", SearchOption.AllDirectories));
        }

        [TestCase(ProductionSpatialPublicationFailurePoint.DuringNextJournalWrite)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterNextJournalFlushBeforePromotion)]
        [TestCase(ProductionSpatialPublicationFailurePoint.DuringJournalPromotion)]
        [TestCase(ProductionSpatialPublicationFailurePoint.AfterJournalPromotionBeforePriorRemoval)]
        public void InterruptedJournalTransition_AlwaysLeavesARecoverableValidJournal(
            ProductionSpatialPublicationFailurePoint point)
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Interrupt(point).Status, Is.EqualTo(ProductionSpatialPublicationStatus.JournalDurabilityFailure));
            Assert.That(JournalPaths().Any(File.Exists), Is.True);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet));
            AssertBytes(prior);
            AssertCompleteSet(prior);
        }

        [Test]
        public void InvalidCurrentJournal_UsesValidPreservedPriorJournal()
        {
            Assert.That(Publish().Success, Is.True);
            byte[][] prior = Bytes();
            ChangeTestLocalization();
            Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterJournalPromotionBeforePriorRemoval).Success, Is.False);
            File.WriteAllText(CurrentJournal(), "corrupt");
            Assert.That(File.Exists(PreviousJournal()), Is.True);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet));
            AssertBytes(prior);
        }

        [Test]
        public void RepeatedEquivalentJournalTransitionRecovery_SelectsIdenticalStateAndBytes()
        {
            ProductionSpatialPublicationStatus firstStatus = default;
            byte[][] firstBytes = null;
            for (int repetition = 0; repetition < 2; repetition++)
            {
                if (repetition != 0) ResetFixture();
                Assert.That(Publish().Success, Is.True);
                ChangeTestLocalization();
                Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterNextJournalFlushBeforePromotion).Success, Is.False);
                ProductionSpatialPublicationResult recovered = Recover();
                if (repetition == 0) { firstStatus = recovered.Status; firstBytes = Bytes(); }
                else
                {
                    Assert.That(recovered.Status, Is.EqualTo(firstStatus));
                    AssertBytes(firstBytes);
                    AssertCompleteSet(firstBytes);
                }
            }
        }

        [Test]
        public void ConflictingValidJournalCopies_FailClosedDeterministically()
        {
            CreatePreparedJournal();
            string current = File.ReadAllText(CurrentJournal());
            string conflict = current.Replace("\"sequence\": 0", "\"sequence\": 1")
                .Replace("\"contentVersion\": \"", "\"contentVersion\": \"conflict.");
            File.WriteAllText(NextJournal(), conflict);
            ProductionSpatialPublicationResult first = Recover();
            ProductionSpatialPublicationResult second = Recover();
            Assert.That(first.Status, Is.EqualTo(ProductionSpatialPublicationStatus.InvalidJournal));
            CollectionAssert.AreEqual(new[] { ProductionSpatialPublicationDiagnostic.JournalConflict }, first.Diagnostics);
            Assert.That(second.Status, Is.EqualTo(first.Status));
            CollectionAssert.AreEqual(first.Diagnostics, second.Diagnostics);
            Assert.That(Directory.Exists(Workspace()), Is.True);
        }

        [TestCase("Prepared", 1)]
        [TestCase("Installing", 4)]
        [TestCase("Installed", 2)]
        [TestCase("Validated", 2)]
        [TestCase("Complete", 2)]
        [TestCase("Unknown", 0)]
        public void ContradictoryPhaseProgressCombinations_AreRejectedDeterministically(string phase, int progress)
        {
            CreatePreparedJournal();
            string json = File.ReadAllText(CurrentJournal())
                .Replace("\"phase\": \"Prepared\"", "\"phase\": \"" + phase + "\"")
                .Replace("\"installationProgress\": 0", "\"installationProgress\": " + progress);
            File.WriteAllText(CurrentJournal(), json);
            AssertRepeatedInvalidJournal(ProductionSpatialPublicationDiagnostic.JournalStateCombinationInvalid);
        }

        [Test]
        public void PriorStateAndBackupHashContradictions_AreRejected()
        {
            CreatePreparedJournal();
            string json = File.ReadAllText(CurrentJournal()).Replace("\"priorState\": \"AllAbsent\"", "\"priorState\": \"Complete\"");
            File.WriteAllText(CurrentJournal(), json);
            AssertRepeatedInvalidJournal(ProductionSpatialPublicationDiagnostic.JournalStateCombinationInvalid);
        }

        [TestCase("{")]
        [TestCase("duplicate")]
        [TestCase("ambiguous")]
        [TestCase("unknown")]
        [TestCase("traversal")]
        [TestCase("hash")]
        public void StrictJournalMutations_AreRejected(string mutation)
        {
            CreatePreparedJournal();
            string json = File.ReadAllText(CurrentJournal());
            ProductionSpatialPublicationDiagnostic expected;
            if (mutation == "{") { json = "{"; expected = ProductionSpatialPublicationDiagnostic.JournalMalformed; }
            else if (mutation == "duplicate") { json=json.Replace("\"schema\":", "\"schema\": \"dungeon_spatial_publication_journal\",\n  \"schema\":"); expected=ProductionSpatialPublicationDiagnostic.JournalFieldDuplicate; }
            else if (mutation == "ambiguous") { json=json.Replace("\"schema\":", "\"Schema\": \"dungeon_spatial_publication_journal\",\n  \"schema\":"); expected=ProductionSpatialPublicationDiagnostic.JournalFieldCaseAmbiguous; }
            else if (mutation == "unknown") { json=json.Replace("{\n", "{\n  \"unknown\": 1,\n"); expected=ProductionSpatialPublicationDiagnostic.JournalFieldUnknown; }
            else if (mutation == "traversal") { json=json.Replace("Temp/DungeonSpatialProductionPublication/staged/0.json", "../outside.json"); expected=ProductionSpatialPublicationDiagnostic.JournalPathInvalid; }
            else { json=CorruptFirstStagedHash(json); expected=ProductionSpatialPublicationDiagnostic.JournalHashInvalid; }
            File.WriteAllText(CurrentJournal(), json);
            AssertRepeatedInvalidJournal(expected);
        }

        [Test]
        public void JournalContentVersionIsCandidateDerivedAndMismatchRejectsInstalledSet()
        {
            byte[][] candidate = CandidateBytes();
            string expectedVersion = Parse(candidate).Value.Manifest.contentVersion;
            CreatePreparedJournal();
            Assert.That(File.ReadAllText(CurrentJournal()), Does.Contain("\"contentVersion\": \"" + expectedVersion + "\""));
            File.WriteAllText(CurrentJournal(), File.ReadAllText(CurrentJournal()).Replace(
                "\"contentVersion\": \"" + expectedVersion + "\"", "\"contentVersion\": \"test.mismatch\""));
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState));
        }

        [Test]
        public void ChangedValidLimitsIdentity_FailsClosedAndPreservesEvidence()
        {
            CreatePreparedJournal();
            File.AppendAllText(LimitsPath(), " \n");
            ProductionSpatialPublicationResult result = Recover();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.UnrecoverableTransaction));
            CollectionAssert.AreEqual(new[] { ProductionSpatialPublicationDiagnostic.LimitsIdentityMismatch }, result.Diagnostics);
            Assert.That(Directory.Exists(Workspace()), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrMalformedLimits_FailClosedAndPreserveEvidence(bool missing)
        {
            CreatePreparedJournal();
            if (missing) File.Delete(LimitsPath()); else File.WriteAllText(LimitsPath(), "{");
            ProductionSpatialPublicationResult result = Recover();
            Assert.That(result.Status, Is.EqualTo(ProductionSpatialPublicationStatus.UnrecoverableTransaction));
            Assert.That(Directory.Exists(Workspace()), Is.True);
        }

        [Test]
        public void RefreshOccursOnlyAfterCompleteCanonicalReplacement_AndFailureIsRecoverable()
        {
            int refreshes = 0;
            Assert.That(Publish(refresh: () =>
            {
                refreshes++;
                Assert.That(ProductionSpatialGeneratedSetParser.RequiredPaths.All(path => File.Exists(Path.Combine(root, path))), Is.True);
                Assert.That(Parse(Bytes()).Success, Is.True);
            }).Success, Is.True);
            Assert.That(refreshes, Is.EqualTo(1));
            ChangeTestLocalization();
            CollectionAssert.AreEqual(new[] { ProductionSpatialPublicationDiagnostic.RefreshFailed },
                Publish(refresh: () => { throw new Exception(); }).Diagnostics);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet));
        }

        [Test]
        public void SourceLimitsAndBootstrapRemainByteIdenticalAfterPublicationAndRecovery()
        {
            Dictionary<string,byte[]> sourceBefore = Snapshot(source);
            byte[] limitsBefore = File.ReadAllBytes(LimitsPath());
            string[] bootstrap = Directory.GetFiles("Assets/_Project/Data/Bootstrap", "*", SearchOption.AllDirectories);
            Dictionary<string,byte[]> bootstrapBefore = bootstrap.ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal);
            Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation).Success, Is.False);
            Assert.That(Recover().Status, Is.EqualTo(ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet));
            AssertSnapshots(sourceBefore, Snapshot(source));
            CollectionAssert.AreEqual(limitsBefore, File.ReadAllBytes(LimitsPath()));
            AssertSnapshots(bootstrapBefore, bootstrap.ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal));
        }

        private void ResetFixture()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(root);
            source = Path.Combine(root, "ContentAuthoring", "DungeonSpatial");
            CopyDirectory(DungeonSpatialAuthoringRepository.PackageRoot, source);
            Directory.CreateDirectory(Path.GetDirectoryName(LimitsPath()));
            File.Copy(ProductionSpatialContentPublicationService.LimitsPath, LimitsPath());
        }
        private ProductionSpatialPublicationResult Publish(Action refresh=null,Action<ProductionSpatialPublicationFailurePoint> fail=null)=>ProductionSpatialContentPublicationService.Publish(new ProductionSpatialPublicationContext(root,source,refresh??(()=>{}),fail));
        private ProductionSpatialPublicationResult Recover()=>ProductionSpatialContentPublicationService.Recover(new ProductionSpatialPublicationContext(root,source,()=>{}));
        private ProductionSpatialPublicationResult Interrupt(ProductionSpatialPublicationFailurePoint point)=>Publish(fail:current=>{if(current==point)throw new Exception();});
        private void CreatePreparedJournal(){Assert.That(Interrupt(ProductionSpatialPublicationFailurePoint.AfterJournalFlush).Success,Is.False);}
        private void AssertRepeatedInvalidJournal(ProductionSpatialPublicationDiagnostic diagnostic){ProductionSpatialPublicationResult first=Recover();ProductionSpatialPublicationResult second=Recover();Assert.That(first.Status,Is.EqualTo(ProductionSpatialPublicationStatus.InvalidJournal));CollectionAssert.AreEqual(new[]{diagnostic},first.Diagnostics);Assert.That(second.Status,Is.EqualTo(first.Status));CollectionAssert.AreEqual(first.Diagnostics,second.Diagnostics);}
        private string LimitsPath()=>Path.Combine(root,ProductionSpatialContentPublicationService.LimitsPath);
        private string Workspace()=>Path.Combine(root,ProductionSpatialContentPublicationService.TransactionWorkspacePath);
        private string CurrentJournal()=>Path.Combine(root,ProductionSpatialContentPublicationService.CurrentJournalRelativePath);
        private string NextJournal()=>Path.Combine(root,ProductionSpatialContentPublicationService.NextJournalRelativePath);
        private string PreviousJournal()=>Path.Combine(root,ProductionSpatialContentPublicationService.PreviousJournalRelativePath);
        private string[] JournalPaths()=>new[]{CurrentJournal(),NextJournal(),PreviousJournal()};
        private string Target(int index)=>Path.Combine(root,ProductionSpatialGeneratedSetParser.RequiredPaths[index]);
        private string Staged(int index)=>Path.Combine(Workspace(),"staged",index+".json");
        private string Backup(int index)=>Path.Combine(Workspace(),"backup",index+".json");
        private void WriteTarget(int index,byte[] bytes){Directory.CreateDirectory(Path.GetDirectoryName(Target(index)));File.WriteAllBytes(Target(index),bytes);}
        private byte[][] Bytes()=>ProductionSpatialGeneratedSetParser.RequiredPaths.Select(path=>File.ReadAllBytes(Path.Combine(root,path))).ToArray();
        private byte[][] ExistingBytes()=>ProductionSpatialGeneratedSetParser.RequiredPaths.Where(path=>File.Exists(Path.Combine(root,path))).Select(path=>File.ReadAllBytes(Path.Combine(root,path))).ToArray();
        private byte[][] CandidateBytes()=>BuildCandidate().Files.Select(file=>file.Bytes).ToArray();
        private ProductionSpatialGeneratedSet BuildCandidate(){SpatialContentValidationWorkloadLimits limits=Limits();DungeonSpatialAuthoringResult projection=DungeonSpatialAuthoringPackageParser.ParseAndProject(DungeonSpatialAuthoringRepository.Read(source),limits,true);return ProductionSpatialGeneratedSetBuilder.Build(projection.Projection,limits).Output;}
        private ProductionSpatialGeneratedSetResult Parse(byte[][] bytes)=>ProductionSpatialGeneratedSetParser.ParseAndValidate(new ProductionSpatialGeneratedSet(ProductionSpatialGeneratedSetParser.RequiredPaths.Select((path,index)=>new ProductionSpatialGeneratedFile(path,bytes[index]))),Limits());
        private SpatialContentValidationWorkloadLimits Limits()=>ProductionSpatialContentWorkloadLimitParser.Parse(File.ReadAllText(LimitsPath())).Limits;
        private void AssertCompleteSet(byte[][] expected){Assert.That(expected.Length,Is.EqualTo(3));Assert.That(Parse(expected).Success,Is.True);}
        private void AssertBytes(byte[][] expected){byte[][] actual=Bytes();Assert.That(actual.Length,Is.EqualTo(expected.Length));for(int i=0;i<expected.Length;i++)CollectionAssert.AreEqual(expected[i],actual[i]);}
        private static bool IsMixed(byte[][] prior,byte[][] candidate,byte[][] actual){bool priorEqual=true,candidateEqual=true;for(int i=0;i<actual.Length;i++){priorEqual&=actual[i].SequenceEqual(prior[i]);candidateEqual&=actual[i].SequenceEqual(candidate[i]);}return !priorEqual&&!candidateEqual;}
        private void ChangeTestLocalization(){string path=Path.Combine(source,"tables","localization_en.csv");string text=File.ReadAllText(path);int newline=text.IndexOf('\n');int comma=text.IndexOf(',',newline+1);int end=text.IndexOf('\n',comma+1);File.WriteAllText(path,text.Substring(0,comma+1)+"Test-only alternate text"+text.Substring(end));}
        private void RestoreSource(){Directory.Delete(source,true);CopyDirectory(DungeonSpatialAuthoringRepository.PackageRoot,source);}
        private static string CorruptFirstStagedHash(string json){int field=json.IndexOf("\"stagedHashes\": [\"",StringComparison.Ordinal);int hash=field+"\"stagedHashes\": [\"".Length;return json.Substring(0,hash)+"x"+json.Substring(hash+1);}
        private static void CopyDirectory(string from,string to){foreach(string directory in Directory.GetDirectories(from,"*",SearchOption.AllDirectories))Directory.CreateDirectory(directory.Replace(from,to));Directory.CreateDirectory(to);foreach(string file in Directory.GetFiles(from,"*",SearchOption.AllDirectories)){string target=file.Replace(from,to);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,true);}}
        private static Dictionary<string,byte[]> Snapshot(string path)=>Directory.GetFiles(path,"*",SearchOption.AllDirectories).ToDictionary(file=>file.Substring(path.Length),File.ReadAllBytes,StringComparer.Ordinal);
        private static void AssertSnapshots(Dictionary<string,byte[]> expected,Dictionary<string,byte[]> actual){CollectionAssert.AreEquivalent(expected.Keys,actual.Keys);foreach(string key in expected.Keys)CollectionAssert.AreEqual(expected[key],actual[key]);}
    }
}
#endif
