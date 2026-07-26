#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEditor;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    public enum ProductionSpatialPublicationStatus
    {
        PublicationSucceeded = 0, NoByteChangesNeeded = 1, RecoveryCompletedToPreviousSet = 2,
        RecoveryCompletedToNewSet = 3, RecoveryCompletedToInitialUnpublishedState = 4,
        PreInstallValidationFailure = 5, InvalidExistingTargetState = 6, InvalidJournal = 7,
        StagingFailure = 8, BackupFailure = 9, JournalDurabilityFailure = 10,
        InstallationFailure = 11, InstalledSetValidationFailure = 12,
        UnrecoverableTransaction = 13, CleanupFailureAfterValidSelectedState = 14
    }

    public enum ProductionSpatialPublicationDiagnostic
    {
        None = 0, RecoveryRequired = 1, AuthoringReadFailed = 2, LimitsReadFailed = 3,
        LimitsInvalid = 4, AuthoringInvalid = 5, CandidateInvalid = 6,
        TargetSetPartial = 7, PreviousSetInvalid = 8, WorkspaceOperationFailed = 9,
        JournalMalformed = 10, JournalFieldMissing = 11, JournalFieldDuplicate = 12,
        JournalFieldCaseAmbiguous = 13, JournalFieldUnknown = 14, JournalValueInvalid = 15,
        JournalPathInvalid = 16, JournalHashInvalid = 17, JournalVersionMismatch = 18,
        StagedSetInvalid = 19, BackupSetInvalid = 20, InstalledSetInvalid = 21,
        InstalledHashMismatch = 22, NoRecoverableCompleteSet = 23, RefreshFailed = 24,
        CleanupFailed = 25, InjectedFailure = 26
    }

    public enum ProductionSpatialPublicationFailurePoint
    {
        BeforeJournalCreation, AfterStaging, AfterBackupCreation, AfterJournalFlush,
        AfterFirstTargetReplacement, AfterIntermediateTargetReplacement,
        AfterAllTargetReplacementsBeforeInstalledValidation,
        AfterInstalledValidationBeforeCleanup
    }

    public sealed class ProductionSpatialPublicationResult
    {
        internal ProductionSpatialPublicationResult(ProductionSpatialPublicationStatus status,
            IEnumerable<ProductionSpatialPublicationDiagnostic> diagnostics)
        {
            Status = status;
            Diagnostics = (diagnostics ?? Array.Empty<ProductionSpatialPublicationDiagnostic>())
                .Distinct().OrderBy(value => (int)value).ToArray();
        }
        public ProductionSpatialPublicationStatus Status { get; }
        public ProductionSpatialPublicationDiagnostic[] Diagnostics { get; }
        public bool Success => Status <= ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState;
    }

    public static class ProductionSpatialContentPublicationService
    {
        public const string LimitsPath = "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json";
        public const string TransactionWorkspacePath = "Temp/DungeonSpatialProductionPublication";
        private const string JournalPath = TransactionWorkspacePath + "/journal.json";
        private const string JournalSchema = "dungeon_spatial_publication_journal";
        private const int JournalSchemaVersion = 1;
        private const string ContentVersion = "0.1.0";

        public static ProductionSpatialPublicationResult PublishProduction() =>
            Publish(new ProductionSpatialPublicationContext(Directory.GetCurrentDirectory(),
                DungeonSpatialAuthoringRepository.PackageRoot, AssetDatabase.Refresh));

        public static ProductionSpatialPublicationResult RecoverProduction() =>
            Recover(new ProductionSpatialPublicationContext(Directory.GetCurrentDirectory(),
                DungeonSpatialAuthoringRepository.PackageRoot, AssetDatabase.Refresh));

        internal static ProductionSpatialPublicationResult Publish(ProductionSpatialPublicationContext context)
        {
            ProductionSpatialPublicationResult recovery = Recover(context);
            if (!recovery.Success) return recovery;
            string workspace = Absolute(context.ProjectRoot, TransactionWorkspacePath);
            string journalPath = Absolute(context.ProjectRoot, JournalPath);
            try
            {
                // Temp is ignored by Unity and Git, is beneath the project root, and therefore resides on
                // the same filesystem as Assets. FileStream.Flush(true) flushes file contents/metadata to
                // the strongest API available here; .NET provides no portable parent-directory fsync.
                if (Directory.Exists(workspace)) Directory.Delete(workspace, true);
            }
            catch { return Fail(ProductionSpatialPublicationStatus.StagingFailure, ProductionSpatialPublicationDiagnostic.WorkspaceOperationFailed); }

            SpatialContentValidationWorkloadLimits limits;
            ProductionSpatialGeneratedSet candidate;
            ProductionSpatialGeneratedSet previous = null;
            bool priorPresent;
            try
            {
                DungeonSpatialAuthoringSource source = DungeonSpatialAuthoringRepository.Read(context.AuthoringRoot);
                if (source == null) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringReadFailed);
                string limitFile = Absolute(context.ProjectRoot, LimitsPath);
                if (!File.Exists(limitFile)) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.LimitsReadFailed);
                var limitResult = ProductionSpatialContentWorkloadLimitParser.Parse(File.ReadAllText(limitFile));
                if (!limitResult.Success) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.LimitsInvalid);
                limits = limitResult.Limits;
                DungeonSpatialAuthoringResult parsed = DungeonSpatialAuthoringPackageParser.ParseAndProject(source, limits, true);
                if (!parsed.Success) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringInvalid);
                ProductionSpatialGeneratedSetBuildResult built = ProductionSpatialGeneratedSetBuilder.Build(parsed.Projection, limits);
                if (!built.Success || !ProductionSpatialGeneratedSetParser.ParseAndValidate(built.Output, limits).Success)
                    return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.CandidateInvalid);
                candidate = built.Output;
                if (!ExactPaths(candidate)) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.CandidateInvalid);
                int existing = RequiredPaths.Count(path => File.Exists(Absolute(context.ProjectRoot, path)));
                if (existing != 0 && existing != RequiredPaths.Length)
                    return Fail(ProductionSpatialPublicationStatus.InvalidExistingTargetState, ProductionSpatialPublicationDiagnostic.TargetSetPartial);
                priorPresent = existing == RequiredPaths.Length;
                if (priorPresent)
                {
                    previous = ReadSet(context.ProjectRoot, path => Absolute(context.ProjectRoot, path));
                    if (!ProductionSpatialGeneratedSetParser.ParseAndValidate(previous, limits).Success)
                        return Fail(ProductionSpatialPublicationStatus.InvalidExistingTargetState, ProductionSpatialPublicationDiagnostic.PreviousSetInvalid);
                }
            }
            catch { return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringReadFailed); }

            string[] staged = RequiredPaths.Select((_, i) => TransactionWorkspacePath + "/staged/" + i.ToString(CultureInfo.InvariantCulture) + ".json").ToArray();
            string[] backups = RequiredPaths.Select((_, i) => TransactionWorkspacePath + "/backup/" + i.ToString(CultureInfo.InvariantCulture) + ".json").ToArray();
            string[] candidateHashes = candidate.Files.Select(file => Hash(file.Bytes)).ToArray();
            string[] priorHashes = priorPresent ? previous.Files.Select(file => Hash(file.Bytes)).ToArray() : Array.Empty<string>();
            var journal = new Journal("Prepared", priorPresent, 0, staged, backups, candidateHashes, priorHashes);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Absolute(context.ProjectRoot, staged[0])));
                for (int i = 0; i < RequiredPaths.Length; i++) WriteDurable(Absolute(context.ProjectRoot, staged[i]), candidate.Files[i].Bytes);
                if (!ValidateMappedSet(context.ProjectRoot, staged, limits, candidateHashes))
                    return Fail(ProductionSpatialPublicationStatus.StagingFailure, ProductionSpatialPublicationDiagnostic.StagedSetInvalid);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterStaging);
                if (priorPresent)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Absolute(context.ProjectRoot, backups[0])));
                    for (int i = 0; i < RequiredPaths.Length; i++) WriteDurable(Absolute(context.ProjectRoot, backups[i]), previous.Files[i].Bytes);
                    if (!ValidateMappedSet(context.ProjectRoot, backups, limits, priorHashes))
                        return Fail(ProductionSpatialPublicationStatus.BackupFailure, ProductionSpatialPublicationDiagnostic.BackupSetInvalid);
                }
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterBackupCreation);
                context.Fail(ProductionSpatialPublicationFailurePoint.BeforeJournalCreation);
                WriteJournal(journalPath, journal);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterJournalFlush);
            }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            catch { return Fail(File.Exists(journalPath) ? ProductionSpatialPublicationStatus.JournalDurabilityFailure : ProductionSpatialPublicationStatus.StagingFailure, ProductionSpatialPublicationDiagnostic.WorkspaceOperationFailed); }

            try
            {
                journal.Phase = "Installing";
                for (int i = 0; i < RequiredPaths.Length; i++)
                {
                    journal.InstallationProgress = i;
                    WriteJournal(journalPath, journal);
                    CopyDurable(Absolute(context.ProjectRoot, staged[i]), Absolute(context.ProjectRoot, RequiredPaths[i]));
                    journal.InstallationProgress = i + 1;
                    WriteJournal(journalPath, journal);
                    if (i == 0) context.Fail(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement);
                    if (i == 1) context.Fail(ProductionSpatialPublicationFailurePoint.AfterIntermediateTargetReplacement);
                }
                journal.Phase = "Installed";
                WriteJournal(journalPath, journal);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation);
                context.Refresh();
                if (!ValidateMappedSet(context.ProjectRoot, RequiredPaths, limits, candidateHashes))
                    return Fail(ProductionSpatialPublicationStatus.InstalledSetValidationFailure, ProductionSpatialPublicationDiagnostic.InstalledSetInvalid);
                journal.Phase = "Validated";
                WriteJournal(journalPath, journal);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup);
                journal.Phase = "Complete";
                WriteJournal(journalPath, journal);
            }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            catch { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.RefreshFailed); }
            try { Directory.Delete(workspace, true); }
            catch { return Fail(ProductionSpatialPublicationStatus.CleanupFailureAfterValidSelectedState, ProductionSpatialPublicationDiagnostic.CleanupFailed); }
            return new ProductionSpatialPublicationResult(ProductionSpatialPublicationStatus.PublicationSucceeded, Array.Empty<ProductionSpatialPublicationDiagnostic>());
        }

        internal static ProductionSpatialPublicationResult Recover(ProductionSpatialPublicationContext context)
        {
            string journalPath = Absolute(context.ProjectRoot, JournalPath);
            if (!File.Exists(journalPath)) return new ProductionSpatialPublicationResult(ProductionSpatialPublicationStatus.NoByteChangesNeeded, Array.Empty<ProductionSpatialPublicationDiagnostic>());
            if (!TryReadJournal(journalPath, out Journal journal, out ProductionSpatialPublicationDiagnostic diagnostic))
                return Fail(ProductionSpatialPublicationStatus.InvalidJournal, diagnostic);
            try
            {
                string limitPath = Absolute(context.ProjectRoot, LimitsPath);
                if (!File.Exists(limitPath)) return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsReadFailed);
                var parsedLimits = ProductionSpatialContentWorkloadLimitParser.Parse(File.ReadAllText(limitPath));
                if (!parsedLimits.Success) return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsInvalid);
                bool stagedValid = ValidateMappedSet(context.ProjectRoot, journal.StagedPaths, parsedLimits.Limits, journal.StagedHashes);
                bool newValid = stagedValid && ValidateMappedSet(context.ProjectRoot, RequiredPaths, parsedLimits.Limits, journal.StagedHashes);
                bool priorValid = journal.PriorPresent && ValidateMappedSet(context.ProjectRoot, journal.BackupPaths, parsedLimits.Limits, journal.BackupHashes);
                ProductionSpatialPublicationStatus selected;
                if (newValid)
                {
                    context.Refresh();
                    selected = ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet;
                }
                else if (priorValid)
                {
                    for (int i = 0; i < RequiredPaths.Length; i++) CopyDurable(Absolute(context.ProjectRoot, journal.BackupPaths[i]), Absolute(context.ProjectRoot, RequiredPaths[i]));
                    context.Refresh();
                    if (!ValidateMappedSet(context.ProjectRoot, RequiredPaths, parsedLimits.Limits, journal.BackupHashes))
                        return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.NoRecoverableCompleteSet);
                    selected = ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet;
                }
                else if (!journal.PriorPresent && ValidateInitialRecoveryEvidence(context.ProjectRoot, journal))
                {
                    for (int i = 0; i < RequiredPaths.Length; i++) { string target = Absolute(context.ProjectRoot, RequiredPaths[i]); if (File.Exists(target)) File.Delete(target); }
                    context.Refresh();
                    if (RequiredPaths.Any(path => File.Exists(Absolute(context.ProjectRoot, path))))
                        return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.NoRecoverableCompleteSet);
                    selected = ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState;
                }
                else return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.NoRecoverableCompleteSet);

                journal.Phase = "Complete";
                WriteJournal(journalPath, journal);
                try { Directory.Delete(Absolute(context.ProjectRoot, TransactionWorkspacePath), true); }
                catch { return Fail(ProductionSpatialPublicationStatus.CleanupFailureAfterValidSelectedState, ProductionSpatialPublicationDiagnostic.CleanupFailed); }
                return new ProductionSpatialPublicationResult(selected, Array.Empty<ProductionSpatialPublicationDiagnostic>());
            }
            catch { return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.NoRecoverableCompleteSet); }
        }

        private static readonly string[] RequiredPaths = ProductionSpatialGeneratedSetParser.RequiredPaths.ToArray();
        private static bool ExactPaths(ProductionSpatialGeneratedSet set) => set?.Files != null &&
            set.Files.Select(file => file.Path).SequenceEqual(RequiredPaths, StringComparer.Ordinal);
        private static ProductionSpatialGeneratedSet ReadSet(string root, Func<string, string> map) =>
            new ProductionSpatialGeneratedSet(RequiredPaths.Select(path => new ProductionSpatialGeneratedFile(path, File.ReadAllBytes(map(path)))));
        private static bool ValidateMappedSet(string root, string[] paths, SpatialContentValidationWorkloadLimits limits, string[] hashes)
        {
            if (paths == null || paths.Length != RequiredPaths.Length || hashes == null || hashes.Length != RequiredPaths.Length) return false;
            for (int i = 0; i < paths.Length; i++)
            {
                string absolute = Absolute(root, paths[i]);
                if (!File.Exists(absolute) || !string.Equals(Hash(File.ReadAllBytes(absolute)), hashes[i], StringComparison.Ordinal)) return false;
            }
            ProductionSpatialGeneratedSet set = new ProductionSpatialGeneratedSet(RequiredPaths.Select((path, i) =>
                new ProductionSpatialGeneratedFile(path, File.ReadAllBytes(Absolute(root, paths[i])))));
            ProductionSpatialGeneratedSetResult parsed = ProductionSpatialGeneratedSetParser.ParseAndValidate(set, limits);
            return parsed.Success && parsed.Value.Manifest.contentVersion == ContentVersion;
        }
        private static bool ValidateInitialRecoveryEvidence(string root, Journal journal) =>
            journal.BackupHashes.Length == 0 && journal.BackupPaths.All(path => !File.Exists(Absolute(root, path)));
        private static string Absolute(string root, string relative) => Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string Hash(byte[] bytes) { using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture))); }
        private static void CopyDurable(string source, string target) { Directory.CreateDirectory(Path.GetDirectoryName(target)); WriteDurable(target, File.ReadAllBytes(source)); }
        private static void WriteDurable(string path, byte[] bytes) { using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); } }
        private static ProductionSpatialPublicationResult Fail(ProductionSpatialPublicationStatus status, ProductionSpatialPublicationDiagnostic diagnostic) => new ProductionSpatialPublicationResult(status, new[] { diagnostic });

        private sealed class Journal
        {
            internal Journal(string phase, bool priorPresent, int progress, string[] staged, string[] backups, string[] stagedHashes, string[] backupHashes)
            { Phase=phase; PriorPresent=priorPresent; InstallationProgress=progress; StagedPaths=staged; BackupPaths=backups; StagedHashes=stagedHashes; BackupHashes=backupHashes; }
            internal string Phase; internal bool PriorPresent; internal int InstallationProgress;
            internal string[] StagedPaths, BackupPaths, StagedHashes, BackupHashes;
        }

        private static void WriteJournal(string path, Journal value)
        {
            string json = "{\n" +
                "  \"schema\": \"" + JournalSchema + "\",\n" +
                "  \"schemaVersion\": " + JournalSchemaVersion + ",\n" +
                "  \"contentVersion\": \"" + ContentVersion + "\",\n" +
                "  \"phase\": \"" + value.Phase + "\",\n" +
                "  \"priorState\": \"" + (value.PriorPresent ? "Complete" : "AllAbsent") + "\",\n" +
                "  \"installationProgress\": " + value.InstallationProgress.ToString(CultureInfo.InvariantCulture) + ",\n" +
                JournalArray("generatedPaths", RequiredPaths) + ",\n" + JournalArray("finalPaths", RequiredPaths) + ",\n" +
                JournalArray("stagedPaths", value.StagedPaths) + ",\n" + JournalArray("backupPaths", value.BackupPaths) + ",\n" +
                JournalArray("stagedHashes", value.StagedHashes) + ",\n" + JournalArray("backupHashes", value.BackupHashes) + "\n}\n";
            WriteDurable(path, new UTF8Encoding(false).GetBytes(json));
        }
        private static string JournalArray(string name, IEnumerable<string> values) => "  \"" + name + "\": [" + string.Join(", ", values.Select(value => "\"" + value + "\"")) + "]";

        private static bool TryReadJournal(string path, out Journal journal, out ProductionSpatialPublicationDiagnostic diagnostic)
        {
            journal = null; diagnostic = ProductionSpatialPublicationDiagnostic.JournalMalformed;
            Dictionary<string, object> fields;
            try { fields = new StrictJournalJson(File.ReadAllText(path)).Object(); }
            catch (JournalProblem problem) { diagnostic = problem.Diagnostic; return false; }
            catch { return false; }
            string[] names = { "schema", "schemaVersion", "contentVersion", "phase", "priorState", "installationProgress", "generatedPaths", "finalPaths", "stagedPaths", "backupPaths", "stagedHashes", "backupHashes" };
            if (fields.Keys.Any(key => Array.IndexOf(names, key) < 0)) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalFieldUnknown; return false; }
            if (names.Any(name => !fields.ContainsKey(name))) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalFieldMissing; return false; }
            if (!(fields["schema"] is string schema) || schema != JournalSchema || !(fields["schemaVersion"] is long version) || version != JournalSchemaVersion ||
                !(fields["contentVersion"] is string content) || content != ContentVersion) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalVersionMismatch; return false; }
            if (!(fields["phase"] is string phase) || Array.IndexOf(new[] { "Prepared", "Installing", "Installed", "Validated", "Complete" }, phase) < 0 ||
                !(fields["priorState"] is string prior) || (prior != "Complete" && prior != "AllAbsent") ||
                !(fields["installationProgress"] is long progress) || progress < 0 || progress > RequiredPaths.Length) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalValueInvalid; return false; }
            if (!Strings(fields, "generatedPaths", out string[] generated) || !Strings(fields, "finalPaths", out string[] finals) ||
                !Strings(fields, "stagedPaths", out string[] staged) || !Strings(fields, "backupPaths", out string[] backups) ||
                !Strings(fields, "stagedHashes", out string[] stagedHashes) || !Strings(fields, "backupHashes", out string[] backupHashes)) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalValueInvalid; return false; }
            bool priorPresent = prior == "Complete";
            if (!generated.SequenceEqual(RequiredPaths) || !finals.SequenceEqual(RequiredPaths) || staged.Length != 3 || backups.Length != 3 || stagedHashes.Length != 3 || backupHashes.Length != (priorPresent ? 3 : 0)) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalValueInvalid; return false; }
            if (!ValidPaths(finals, RequiredPaths) || !ValidPaths(staged, RequiredPaths.Select((_,i)=>TransactionWorkspacePath+"/staged/"+i+".json").ToArray()) ||
                !ValidPaths(backups, RequiredPaths.Select((_,i)=>TransactionWorkspacePath+"/backup/"+i+".json").ToArray())) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalPathInvalid; return false; }
            if (stagedHashes.Concat(backupHashes).Any(hash => hash.Length != 64 || hash.Any(c => !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')))) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalHashInvalid; return false; }
            journal = new Journal(phase, priorPresent, (int)progress, staged, backups, stagedHashes, backupHashes); diagnostic=ProductionSpatialPublicationDiagnostic.None; return true;
        }
        private static bool ValidPaths(string[] actual, string[] expected) => actual.SequenceEqual(expected) && actual.All(path => !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) && !path.Split('/').Contains("..")) && actual.Distinct(StringComparer.Ordinal).Count()==actual.Length;
        private static bool Strings(Dictionary<string,object> fields,string name,out string[] values) { values=null; if(!(fields[name] is List<object> list)||list.Any(value=>!(value is string)))return false; values=list.Cast<string>().ToArray(); return true; }

        private sealed class JournalProblem : Exception { internal JournalProblem(ProductionSpatialPublicationDiagnostic value){Diagnostic=value;} internal ProductionSpatialPublicationDiagnostic Diagnostic; }
        private sealed class StrictJournalJson
        {
            private readonly string source; private int index; internal StrictJournalJson(string value){source=value;}
            internal Dictionary<string,object> Object(){ Space(); if(!Pop('{'))throw new FormatException(); var result=new Dictionary<string,object>(StringComparer.Ordinal);var insensitive=new HashSet<string>(StringComparer.OrdinalIgnoreCase);Space();if(Pop('}'))return result;while(true){string key=String();Space();if(!Pop(':'))throw new FormatException();object value=Value();if(result.ContainsKey(key))throw new JournalProblem(ProductionSpatialPublicationDiagnostic.JournalFieldDuplicate);if(!insensitive.Add(key))throw new JournalProblem(ProductionSpatialPublicationDiagnostic.JournalFieldCaseAmbiguous);result.Add(key,value);Space();if(Pop('}'))break;if(!Pop(','))throw new FormatException();}Space();if(index!=source.Length)throw new FormatException();return result;}
            private object Value(){Space();if(index>=source.Length)throw new FormatException();if(source[index]=='\"')return String();if(source[index]=='[')return List();if(source[index]=='-'||char.IsDigit(source[index]))return Number();if(Take("true"))return true;if(Take("false"))return false;throw new FormatException();}
            private List<object> List(){index++;var result=new List<object>();Space();if(Pop(']'))return result;while(true){result.Add(Value());Space();if(Pop(']'))return result;if(!Pop(','))throw new FormatException();}}
            private string String(){Space();if(index>=source.Length||source[index++]!='\"')throw new FormatException();var b=new StringBuilder();while(index<source.Length){char c=source[index++];if(c=='\"')return b.ToString();if(c<' '||c=='\\')throw new FormatException();b.Append(c);}throw new FormatException();}
            private long Number(){int start=index;if(source[index]=='-')index++;while(index<source.Length&&char.IsDigit(source[index]))index++;return long.Parse(source.Substring(start,index-start),CultureInfo.InvariantCulture);}
            private void Space(){while(index<source.Length&&char.IsWhiteSpace(source[index]))index++;} private bool Pop(char c){Space();if(index<source.Length&&source[index]==c){index++;return true;}return false;} private bool Take(string value){if(index+value.Length<=source.Length&&source.Substring(index,value.Length)==value){index+=value.Length;return true;}return false;}
        }
    }

    internal sealed class ProductionSpatialPublicationContext
    {
        internal ProductionSpatialPublicationContext(string projectRoot,string authoringRoot,Action refresh,Action<ProductionSpatialPublicationFailurePoint> failure=null)
        { ProjectRoot=projectRoot;AuthoringRoot=authoringRoot;Refresh=refresh??(()=>{});Failure=failure; }
        internal string ProjectRoot { get; } internal string AuthoringRoot { get; } internal Action Refresh { get; }
        private Action<ProductionSpatialPublicationFailurePoint> Failure { get; }
        internal void Fail(ProductionSpatialPublicationFailurePoint point){try{Failure?.Invoke(point);}catch{throw new InjectedFailureException();}}
    }
    internal sealed class InjectedFailureException : Exception { }
}
#endif
