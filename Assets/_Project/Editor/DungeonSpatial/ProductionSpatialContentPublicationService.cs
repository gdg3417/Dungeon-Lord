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
        None = 0, AuthoringReadFailed = 1, LimitsReadFailed = 2, LimitsInvalid = 3,
        LimitsIdentityMismatch = 4, AuthoringInvalid = 5, CandidateInvalid = 6,
        TargetSetPartial = 7, PreviousSetInvalid = 8, WorkspaceOperationFailed = 9,
        JournalMalformed = 10, JournalFieldMissing = 11, JournalFieldDuplicate = 12,
        JournalFieldCaseAmbiguous = 13, JournalFieldUnknown = 14, JournalValueInvalid = 15,
        JournalStateCombinationInvalid = 16, JournalPathInvalid = 17, JournalHashInvalid = 18,
        JournalVersionMismatch = 19, JournalConflict = 20, JournalTransitionFailed = 21,
        StagedSetInvalid = 22, BackupSetInvalid = 23, TargetInstallationFailed = 24,
        RefreshFailed = 25, InstalledSetInvalid = 26, InstalledHashMismatch = 27,
        RecoveryRestorationFailed = 28, NoRecoverableCompleteSet = 29,
        CleanupFailed = 30, InjectedFailure = 31
    }

    public enum ProductionSpatialPublicationFailurePoint
    {
        BeforeJournalCreation, AfterStaging, AfterBackupCreation, AfterJournalFlush,
        AfterFirstTargetReplacement, AfterIntermediateTargetReplacement,
        AfterAllTargetReplacementsBeforeInstalledValidation,
        AfterInstalledValidationBeforeCleanup,
        DuringNextJournalWrite, AfterNextJournalFlushBeforePromotion,
        DuringJournalPromotion, AfterJournalPromotionBeforePriorRemoval
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
        internal const string CurrentJournalRelativePath = TransactionWorkspacePath + "/journal.json";
        internal const string NextJournalRelativePath = TransactionWorkspacePath + "/journal.next.json";
        internal const string PreviousJournalRelativePath = TransactionWorkspacePath + "/journal.previous.json";
        private const string JournalSchema = "dungeon_spatial_publication_journal";
        private const int JournalSchemaVersion = 1;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly string[] RequiredPaths = ProductionSpatialGeneratedSetParser.RequiredPaths.ToArray();

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
            try
            {
                // Project Temp is ignored by Git and Unity import and is on the same project filesystem as Assets.
                // Flush(true) is the strongest supported file flush; .NET exposes no portable directory fsync.
                if (Directory.Exists(workspace)) Directory.Delete(workspace, true);
            }
            catch { return Fail(ProductionSpatialPublicationStatus.StagingFailure, ProductionSpatialPublicationDiagnostic.WorkspaceOperationFailed); }

            SpatialContentValidationWorkloadLimits limits;
            ProductionSpatialGeneratedSet candidate;
            ProductionSpatialGeneratedSet previous = null;
            string contentVersion;
            string previousContentVersion = string.Empty;
            string limitsHash;
            bool priorPresent;
            try
            {
                DungeonSpatialAuthoringSource source = DungeonSpatialAuthoringRepository.Read(context.AuthoringRoot);
                if (source == null) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringReadFailed);
                string limitFile = Absolute(context.ProjectRoot, LimitsPath);
                if (!File.Exists(limitFile)) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.LimitsReadFailed);
                byte[] limitBytes = File.ReadAllBytes(limitFile);
                string limitText;
                try { limitText = StrictUtf8.GetString(limitBytes); }
                catch { return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.LimitsInvalid); }
                ProductionSpatialContentWorkloadLimitParseResult limitResult = ProductionSpatialContentWorkloadLimitParser.Parse(limitText);
                if (!limitResult.Success) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.LimitsInvalid);
                limits = limitResult.Limits;
                limitsHash = Hash(limitBytes);
                DungeonSpatialAuthoringResult parsed = DungeonSpatialAuthoringPackageParser.ParseAndProject(source, limits, true);
                if (!parsed.Success) return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringInvalid);
                ProductionSpatialGeneratedSetBuildResult built = ProductionSpatialGeneratedSetBuilder.Build(parsed.Projection, limits);
                ProductionSpatialGeneratedSetResult candidateParsed = built.Success
                    ? ProductionSpatialGeneratedSetParser.ParseAndValidate(built.Output, limits) : null;
                if (candidateParsed == null || !candidateParsed.Success)
                    return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.CandidateInvalid);
                candidate = built.Output;
                contentVersion = candidateParsed.Value.Manifest.contentVersion;
                if (string.IsNullOrWhiteSpace(contentVersion) || !ExactPaths(candidate))
                    return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.CandidateInvalid);
                int existing = RequiredPaths.Count(path => File.Exists(Absolute(context.ProjectRoot, path)));
                if (existing != 0 && existing != RequiredPaths.Length)
                    return Fail(ProductionSpatialPublicationStatus.InvalidExistingTargetState, ProductionSpatialPublicationDiagnostic.TargetSetPartial);
                priorPresent = existing == RequiredPaths.Length;
                if (priorPresent)
                {
                    previous = ReadSet(context.ProjectRoot, RequiredPaths);
                    ProductionSpatialGeneratedSetResult previousParsed =
                        ProductionSpatialGeneratedSetParser.ParseAndValidate(previous, limits);
                    if (!previousParsed.Success ||
                        string.IsNullOrWhiteSpace(previousParsed.Value.Manifest.contentVersion))
                        return Fail(ProductionSpatialPublicationStatus.InvalidExistingTargetState, ProductionSpatialPublicationDiagnostic.PreviousSetInvalid);
                    previousContentVersion = previousParsed.Value.Manifest.contentVersion;
                }
            }
            catch { return Fail(ProductionSpatialPublicationStatus.PreInstallValidationFailure, ProductionSpatialPublicationDiagnostic.AuthoringReadFailed); }

            string[] staged = RequiredPaths.Select((_, i) => TransactionWorkspacePath + "/staged/" + i.ToString(CultureInfo.InvariantCulture) + ".json").ToArray();
            string[] backups = RequiredPaths.Select((_, i) => TransactionWorkspacePath + "/backup/" + i.ToString(CultureInfo.InvariantCulture) + ".json").ToArray();
            string[] candidateHashes = candidate.Files.Select(file => Hash(file.Bytes)).ToArray();
            string[] priorHashes = priorPresent ? previous.Files.Select(file => Hash(file.Bytes)).ToArray() : Array.Empty<string>();
            Journal journal = new Journal(0, "Prepared", priorPresent, 0, contentVersion,
                previousContentVersion, limitsHash,
                staged, backups, candidateHashes, priorHashes);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Absolute(context.ProjectRoot, staged[0])));
                for (int i = 0; i < RequiredPaths.Length; i++) WriteDurable(Absolute(context.ProjectRoot, staged[i]), candidate.Files[i].Bytes);
                if (!ValidateMappedSet(context.ProjectRoot, staged, limits, candidateHashes, contentVersion))
                    return Fail(ProductionSpatialPublicationStatus.StagingFailure, ProductionSpatialPublicationDiagnostic.StagedSetInvalid);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterStaging);
                if (priorPresent)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Absolute(context.ProjectRoot, backups[0])));
                    for (int i = 0; i < RequiredPaths.Length; i++) WriteDurable(Absolute(context.ProjectRoot, backups[i]), previous.Files[i].Bytes);
                    if (!ValidateMappedSet(context.ProjectRoot, backups, limits, priorHashes, previousContentVersion))
                        return Fail(ProductionSpatialPublicationStatus.BackupFailure, ProductionSpatialPublicationDiagnostic.BackupSetInvalid);
                }
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterBackupCreation);
                context.Fail(ProductionSpatialPublicationFailurePoint.BeforeJournalCreation);
                WriteInitialJournal(context, journal);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterJournalFlush);
            }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.JournalDurabilityFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            catch { return Fail(ProductionSpatialPublicationStatus.JournalDurabilityFailure, ProductionSpatialPublicationDiagnostic.JournalTransitionFailed); }

            ProductionSpatialPublicationResult transition = Transition(context, ref journal, "Installing", 0);
            if (transition != null) return transition;
            for (int i = 0; i < RequiredPaths.Length; i++)
            {
                try { CopyDurable(Absolute(context.ProjectRoot, staged[i]), Absolute(context.ProjectRoot, RequiredPaths[i])); }
                catch { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.TargetInstallationFailed); }
                transition = Transition(context, ref journal, "Installing", i + 1);
                if (transition != null) return transition;
                try
                {
                    if (i == 0) context.Fail(ProductionSpatialPublicationFailurePoint.AfterFirstTargetReplacement);
                    if (i == 1) context.Fail(ProductionSpatialPublicationFailurePoint.AfterIntermediateTargetReplacement);
                }
                catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            }
            transition = Transition(context, ref journal, "Installed", RequiredPaths.Length);
            if (transition != null) return transition;
            try { context.Fail(ProductionSpatialPublicationFailurePoint.AfterAllTargetReplacementsBeforeInstalledValidation); }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            try { context.Refresh(); }
            catch { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.RefreshFailed); }
            if (!ValidateMappedSet(context.ProjectRoot, RequiredPaths, limits, candidateHashes, contentVersion))
                return Fail(ProductionSpatialPublicationStatus.InstalledSetValidationFailure, ProductionSpatialPublicationDiagnostic.InstalledSetInvalid);
            transition = Transition(context, ref journal, "Validated", RequiredPaths.Length);
            if (transition != null) return transition;
            try { context.Fail(ProductionSpatialPublicationFailurePoint.AfterInstalledValidationBeforeCleanup); }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            transition = Transition(context, ref journal, "Complete", RequiredPaths.Length);
            if (transition != null) return transition;
            return Cleanup(workspace, ProductionSpatialPublicationStatus.PublicationSucceeded);
        }

        internal static ProductionSpatialPublicationResult Recover(ProductionSpatialPublicationContext context)
        {
            string workspace = Absolute(context.ProjectRoot, TransactionWorkspacePath);
            if (!AnyJournalExists(context.ProjectRoot))
                return new ProductionSpatialPublicationResult(ProductionSpatialPublicationStatus.NoByteChangesNeeded, Array.Empty<ProductionSpatialPublicationDiagnostic>());
            if (!TrySelectJournal(context.ProjectRoot, out Journal journal, out ProductionSpatialPublicationDiagnostic journalDiagnostic))
                return Fail(ProductionSpatialPublicationStatus.InvalidJournal, journalDiagnostic);

            SpatialContentValidationWorkloadLimits limits;
            try
            {
                string limitsPath = Absolute(context.ProjectRoot, LimitsPath);
                if (!File.Exists(limitsPath)) return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsReadFailed);
                byte[] bytes = File.ReadAllBytes(limitsPath);
                ProductionSpatialContentWorkloadLimitParseResult parsed = ProductionSpatialContentWorkloadLimitParser.Parse(StrictUtf8.GetString(bytes));
                if (!parsed.Success) return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsInvalid);
                if (!string.Equals(Hash(bytes), journal.LimitsHash, StringComparison.Ordinal))
                    return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsIdentityMismatch);
                limits = parsed.Limits;
            }
            catch { return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.LimitsInvalid); }

            bool installedValid = ValidateMappedSet(context.ProjectRoot, RequiredPaths, limits, journal.StagedHashes, journal.ContentVersion);
            bool priorValid = journal.PriorPresent && ValidateMappedSet(context.ProjectRoot,
                journal.BackupPaths, limits, journal.BackupHashes, journal.PreviousContentVersion);
            // Staging is evaluated independently. It is never a prerequisite for accepting an already-complete installed set.
            bool stagedValid = ValidateMappedSet(context.ProjectRoot, journal.StagedPaths, limits, journal.StagedHashes, journal.ContentVersion);
            ProductionSpatialPublicationStatus selected;
            if (installedValid)
            {
                try { context.Refresh(); }
                catch { return Fail(ProductionSpatialPublicationStatus.InstallationFailure, ProductionSpatialPublicationDiagnostic.RefreshFailed); }
                selected = ProductionSpatialPublicationStatus.RecoveryCompletedToNewSet;
            }
            else if (priorValid)
            {
                try
                {
                    for (int i = 0; i < RequiredPaths.Length; i++)
                        CopyDurable(Absolute(context.ProjectRoot, journal.BackupPaths[i]), Absolute(context.ProjectRoot, RequiredPaths[i]));
                    context.Refresh();
                }
                catch { return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.RecoveryRestorationFailed); }
                if (!ValidateMappedSet(context.ProjectRoot, RequiredPaths, limits,
                    journal.BackupHashes, journal.PreviousContentVersion))
                    return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.RecoveryRestorationFailed);
                selected = ProductionSpatialPublicationStatus.RecoveryCompletedToPreviousSet;
            }
            else if (!journal.PriorPresent && CanRestoreInitialAbsent(context.ProjectRoot, journal, stagedValid))
            {
                try
                {
                    for (int i = 0; i < RequiredPaths.Length; i++)
                    {
                        string target = Absolute(context.ProjectRoot, RequiredPaths[i]);
                        if (File.Exists(target)) File.Delete(target);
                    }
                    context.Refresh();
                }
                catch { return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.RecoveryRestorationFailed); }
                if (RequiredPaths.Any(path => File.Exists(Absolute(context.ProjectRoot, path))))
                    return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.RecoveryRestorationFailed);
                selected = ProductionSpatialPublicationStatus.RecoveryCompletedToInitialUnpublishedState;
            }
            else return Fail(ProductionSpatialPublicationStatus.UnrecoverableTransaction, ProductionSpatialPublicationDiagnostic.NoRecoverableCompleteSet);

            if (journal.Phase != "Complete")
            {
                ProductionSpatialPublicationResult transition = Transition(context, ref journal, "Complete", RequiredPaths.Length);
                if (transition != null) return transition;
            }
            return Cleanup(workspace, selected);
        }

        private static ProductionSpatialPublicationResult Transition(ProductionSpatialPublicationContext context,
            ref Journal current, string phase, int progress)
        {
            Journal next = current.Next(phase, progress);
            try
            {
                byte[] bytes = SerializeJournal(next);
                string nextPath = Absolute(context.ProjectRoot, NextJournalRelativePath);
                WriteNextJournalDurable(nextPath, bytes, context);
                if (!TryReadJournalBytes(bytes, out Journal reparsed, out _) || !JournalEquals(next, reparsed))
                    return Fail(ProductionSpatialPublicationStatus.JournalDurabilityFailure, ProductionSpatialPublicationDiagnostic.JournalTransitionFailed);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterNextJournalFlushBeforePromotion);
                context.Fail(ProductionSpatialPublicationFailurePoint.DuringJournalPromotion);
                File.Replace(nextPath, Absolute(context.ProjectRoot, CurrentJournalRelativePath),
                    Absolute(context.ProjectRoot, PreviousJournalRelativePath), true);
                context.Fail(ProductionSpatialPublicationFailurePoint.AfterJournalPromotionBeforePriorRemoval);
                current = next;
                return null;
            }
            catch (InjectedFailureException) { return Fail(ProductionSpatialPublicationStatus.JournalDurabilityFailure, ProductionSpatialPublicationDiagnostic.InjectedFailure); }
            catch { return Fail(ProductionSpatialPublicationStatus.JournalDurabilityFailure, ProductionSpatialPublicationDiagnostic.JournalTransitionFailed); }
        }

        private static void WriteInitialJournal(ProductionSpatialPublicationContext context, Journal journal)
        {
            byte[] bytes = SerializeJournal(journal);
            WriteDurable(Absolute(context.ProjectRoot, CurrentJournalRelativePath), bytes);
            if (!TryReadJournalBytes(bytes, out Journal reparsed, out _) || !JournalEquals(journal, reparsed))
                throw new IOException();
        }

        private static void WriteNextJournalDurable(string path, byte[] bytes, ProductionSpatialPublicationContext context)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int first = bytes.Length / 2;
                stream.Write(bytes, 0, first);
                context.Fail(ProductionSpatialPublicationFailurePoint.DuringNextJournalWrite);
                stream.Write(bytes, first, bytes.Length - first);
                stream.Flush(true);
            }
        }

        private static bool TrySelectJournal(string root, out Journal selected,
            out ProductionSpatialPublicationDiagnostic diagnostic)
        {
            selected = null;
            diagnostic = ProductionSpatialPublicationDiagnostic.JournalMalformed;
            List<JournalCopy> valid = new List<JournalCopy>();
            bool any = false;
            foreach (string path in JournalPaths())
            {
                string absolute = Absolute(root, path);
                if (!File.Exists(absolute)) continue;
                any = true;
                if (TryReadJournalFile(absolute, out Journal journal, out ProductionSpatialPublicationDiagnostic problem))
                    valid.Add(new JournalCopy(path, journal, File.ReadAllBytes(absolute)));
                else if (path == CurrentJournalRelativePath && valid.Count == 0)
                    diagnostic = problem;
            }
            if (!any || valid.Count == 0) return false;
            Journal first = valid[0].Journal;
            if (valid.Any(copy => !SameTransaction(first, copy.Journal)))
            { diagnostic = ProductionSpatialPublicationDiagnostic.JournalConflict; return false; }
            foreach (IGrouping<long, JournalCopy> group in valid.GroupBy(copy => copy.Journal.Sequence))
                if (group.Select(copy => Convert.ToBase64String(copy.Bytes)).Distinct(StringComparer.Ordinal).Count() != 1)
                { diagnostic = ProductionSpatialPublicationDiagnostic.JournalConflict; return false; }
            long[] sequences = valid.Select(copy => copy.Journal.Sequence).Distinct().OrderBy(value => value).ToArray();
            for (int i = 1; i < sequences.Length; i++)
                if (sequences[i] != sequences[i - 1] + 1)
                { diagnostic = ProductionSpatialPublicationDiagnostic.JournalConflict; return false; }
            Journal[] ordered = valid.Select(copy => copy.Journal).OrderBy(value => value.Sequence).ToArray();
            for (int i = 1; i < ordered.Length; i++)
                if (StateRank(ordered[i]) < StateRank(ordered[i - 1]))
                { diagnostic = ProductionSpatialPublicationDiagnostic.JournalConflict; return false; }
            selected = ordered[ordered.Length - 1];
            diagnostic = ProductionSpatialPublicationDiagnostic.None;
            return true;
        }

        private static bool AnyJournalExists(string root) => JournalPaths().Any(path => File.Exists(Absolute(root, path)));
        private static string[] JournalPaths() => new[] { PreviousJournalRelativePath, CurrentJournalRelativePath, NextJournalRelativePath };
        private static bool CanRestoreInitialAbsent(string root, Journal journal, bool stagedValid) =>
            !journal.PriorPresent && journal.BackupHashes.Length == 0 &&
            journal.BackupPaths.All(path => !File.Exists(Absolute(root, path))) &&
            (stagedValid || journal.Phase == "Prepared" || journal.Phase == "Installing" || journal.Phase == "Installed" || journal.Phase == "Validated" || journal.Phase == "Complete");

        private static ProductionSpatialPublicationResult Cleanup(string workspace, ProductionSpatialPublicationStatus success)
        {
            try { Directory.Delete(workspace, true); }
            catch { return Fail(ProductionSpatialPublicationStatus.CleanupFailureAfterValidSelectedState, ProductionSpatialPublicationDiagnostic.CleanupFailed); }
            return new ProductionSpatialPublicationResult(success, Array.Empty<ProductionSpatialPublicationDiagnostic>());
        }

        private static bool ExactPaths(ProductionSpatialGeneratedSet set) => set?.Files != null &&
            set.Files.Select(file => file.Path).SequenceEqual(RequiredPaths, StringComparer.Ordinal);
        private static ProductionSpatialGeneratedSet ReadSet(string root, string[] paths) =>
            new ProductionSpatialGeneratedSet(RequiredPaths.Select((path, i) =>
                new ProductionSpatialGeneratedFile(path, File.ReadAllBytes(Absolute(root, paths[i])))));
        private static bool ValidateMappedSet(string root, string[] paths, SpatialContentValidationWorkloadLimits limits,
            string[] hashes, string contentVersion)
        {
            try
            {
                if (paths == null || paths.Length != RequiredPaths.Length || hashes == null || hashes.Length != RequiredPaths.Length) return false;
                for (int i = 0; i < paths.Length; i++)
                {
                    string absolute = Absolute(root, paths[i]);
                    if (!File.Exists(absolute) || !string.Equals(Hash(File.ReadAllBytes(absolute)), hashes[i], StringComparison.Ordinal)) return false;
                }
                return ValidateSet(ReadSet(root, paths), limits, hashes, contentVersion);
            }
            catch { return false; }
        }
        private static bool ValidateSet(ProductionSpatialGeneratedSet set, SpatialContentValidationWorkloadLimits limits,
            string[] hashes, string contentVersion)
        {
            if (hashes != null && (set?.Files == null || !set.Files.Select(file => Hash(file.Bytes)).SequenceEqual(hashes, StringComparer.Ordinal))) return false;
            ProductionSpatialGeneratedSetResult parsed = ProductionSpatialGeneratedSetParser.ParseAndValidate(set, limits);
            return parsed.Success && !string.IsNullOrWhiteSpace(contentVersion) &&
                string.Equals(parsed.Value.Manifest.contentVersion, contentVersion, StringComparison.Ordinal);
        }
        private static string Absolute(string root, string relative) => Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string Hash(byte[] bytes) { using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture))); }
        private static void CopyDurable(string source, string target) { Directory.CreateDirectory(Path.GetDirectoryName(target)); WriteDurable(target, File.ReadAllBytes(source)); }
        private static void WriteDurable(string path, byte[] bytes) { using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); } }
        private static ProductionSpatialPublicationResult Fail(ProductionSpatialPublicationStatus status, ProductionSpatialPublicationDiagnostic diagnostic) => new ProductionSpatialPublicationResult(status, new[] { diagnostic });

        private sealed class Journal
        {
            internal Journal(long sequence, string phase, bool priorPresent, int progress, string contentVersion,
                string previousContentVersion, string limitsHash, string[] staged, string[] backups,
                string[] stagedHashes, string[] backupHashes)
            { Sequence=sequence;Phase=phase;PriorPresent=priorPresent;InstallationProgress=progress;ContentVersion=contentVersion;PreviousContentVersion=previousContentVersion;LimitsHash=limitsHash;StagedPaths=staged;BackupPaths=backups;StagedHashes=stagedHashes;BackupHashes=backupHashes; }
            internal long Sequence; internal string Phase; internal bool PriorPresent; internal int InstallationProgress;
            internal string ContentVersion, PreviousContentVersion, LimitsHash; internal string[] StagedPaths, BackupPaths, StagedHashes, BackupHashes;
            internal Journal Next(string phase, int progress) => new Journal(Sequence + 1, phase, PriorPresent,
                progress, ContentVersion, PreviousContentVersion, LimitsHash, StagedPaths, BackupPaths,
                StagedHashes, BackupHashes);
        }
        private sealed class JournalCopy
        {
            internal JournalCopy(string path, Journal journal, byte[] bytes){Path=path;Journal=journal;Bytes=bytes;}
            internal string Path; internal Journal Journal; internal byte[] Bytes;
        }

        private static byte[] SerializeJournal(Journal value)
        {
            string json = "{\n" +
                "  \"schema\": \"" + JournalSchema + "\",\n" +
                "  \"schemaVersion\": " + JournalSchemaVersion + ",\n" +
                "  \"sequence\": " + value.Sequence.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"contentVersion\": \"" + value.ContentVersion + "\",\n" +
                "  \"previousContentVersion\": \"" + value.PreviousContentVersion + "\",\n" +
                "  \"limitsSha256\": \"" + value.LimitsHash + "\",\n" +
                "  \"phase\": \"" + value.Phase + "\",\n" +
                "  \"priorState\": \"" + (value.PriorPresent ? "Complete" : "AllAbsent") + "\",\n" +
                "  \"installationProgress\": " + value.InstallationProgress.ToString(CultureInfo.InvariantCulture) + ",\n" +
                JournalArray("generatedPaths", RequiredPaths) + ",\n" + JournalArray("finalPaths", RequiredPaths) + ",\n" +
                JournalArray("stagedPaths", value.StagedPaths) + ",\n" + JournalArray("backupPaths", value.BackupPaths) + ",\n" +
                JournalArray("stagedHashes", value.StagedHashes) + ",\n" + JournalArray("backupHashes", value.BackupHashes) + "\n}\n";
            return StrictUtf8.GetBytes(json);
        }
        private static string JournalArray(string name, IEnumerable<string> values) => "  \"" + name + "\": [" + string.Join(", ", values.Select(value => "\"" + value + "\"")) + "]";

        private static bool TryReadJournalFile(string path, out Journal journal, out ProductionSpatialPublicationDiagnostic diagnostic)
        {
            journal = null; diagnostic = ProductionSpatialPublicationDiagnostic.JournalMalformed;
            try { return TryReadJournalBytes(File.ReadAllBytes(path), out journal, out diagnostic); }
            catch { return false; }
        }
        private static bool TryReadJournalBytes(byte[] bytes, out Journal journal, out ProductionSpatialPublicationDiagnostic diagnostic)
        {
            journal = null; diagnostic = ProductionSpatialPublicationDiagnostic.JournalMalformed;
            Dictionary<string, object> fields;
            try { fields = new StrictJournalJson(StrictUtf8.GetString(bytes)).Object(); }
            catch (JournalProblem problem) { diagnostic = problem.Diagnostic; return false; }
            catch { return false; }
            string[] names = { "schema", "schemaVersion", "sequence", "contentVersion", "previousContentVersion", "limitsSha256", "phase", "priorState", "installationProgress", "generatedPaths", "finalPaths", "stagedPaths", "backupPaths", "stagedHashes", "backupHashes" };
            if (fields.Keys.Any(key => Array.IndexOf(names, key) < 0)) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalFieldUnknown; return false; }
            if (names.Any(name => !fields.ContainsKey(name))) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalFieldMissing; return false; }
            if (!(fields["schema"] is string schema) || schema != JournalSchema || !(fields["schemaVersion"] is long version) || version != JournalSchemaVersion)
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalVersionMismatch; return false; }
            if (!(fields["sequence"] is long sequence) || sequence < 0 || !(fields["contentVersion"] is string content) || string.IsNullOrWhiteSpace(content) ||
                !(fields["previousContentVersion"] is string previousContent) ||
                !(fields["limitsSha256"] is string limitsHash) || !ValidHash(limitsHash) || !(fields["phase"] is string phase) ||
                !(fields["priorState"] is string prior) || (prior != "Complete" && prior != "AllAbsent") ||
                !(fields["installationProgress"] is long progress) || progress < 0 || progress > int.MaxValue)
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalValueInvalid; return false; }
            if (!ValidPhaseProgress(phase, (int)progress)) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalStateCombinationInvalid; return false; }
            if (!Strings(fields,"generatedPaths",out string[] generated)||!Strings(fields,"finalPaths",out string[] finals)||
                !Strings(fields,"stagedPaths",out string[] staged)||!Strings(fields,"backupPaths",out string[] backups)||
                !Strings(fields,"stagedHashes",out string[] stagedHashes)||!Strings(fields,"backupHashes",out string[] backupHashes))
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalValueInvalid; return false; }
            bool priorPresent = prior == "Complete";
            if (priorPresent ? string.IsNullOrWhiteSpace(previousContent) : previousContent.Length != 0)
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalStateCombinationInvalid; return false; }
            if (!generated.SequenceEqual(RequiredPaths) || !finals.SequenceEqual(RequiredPaths) || staged.Length != 3 || backups.Length != 3 || stagedHashes.Length != 3 || backupHashes.Length != (priorPresent ? 3 : 0))
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalStateCombinationInvalid; return false; }
            if (!ValidPaths(finals,RequiredPaths)||!ValidPaths(staged,RequiredPaths.Select((_,i)=>TransactionWorkspacePath+"/staged/"+i+".json").ToArray())||
                !ValidPaths(backups,RequiredPaths.Select((_,i)=>TransactionWorkspacePath+"/backup/"+i+".json").ToArray()))
            { diagnostic=ProductionSpatialPublicationDiagnostic.JournalPathInvalid; return false; }
            if (stagedHashes.Concat(backupHashes).Any(hash => !ValidHash(hash))) { diagnostic=ProductionSpatialPublicationDiagnostic.JournalHashInvalid; return false; }
            journal = new Journal(sequence,phase,priorPresent,(int)progress,content,previousContent,
                limitsHash,staged,backups,stagedHashes,backupHashes);
            diagnostic=ProductionSpatialPublicationDiagnostic.None; return true;
        }
        private static bool ValidPhaseProgress(string phase, int progress) =>
            phase == "Prepared" ? progress == 0 :
            phase == "Installing" ? progress >= 0 && progress <= RequiredPaths.Length :
            phase == "Installed" || phase == "Validated" || phase == "Complete" ? progress == RequiredPaths.Length : false;
        private static int StateRank(Journal value) => value.Phase == "Prepared" ? 0 : value.Phase == "Installing" ? 1 + value.InstallationProgress : value.Phase == "Installed" ? 5 : value.Phase == "Validated" ? 6 : 7;
        private static bool SameTransaction(Journal a, Journal b) => a.PriorPresent==b.PriorPresent &&
            a.ContentVersion==b.ContentVersion && a.PreviousContentVersion==b.PreviousContentVersion && a.LimitsHash==b.LimitsHash &&
            a.StagedPaths.SequenceEqual(b.StagedPaths) && a.BackupPaths.SequenceEqual(b.BackupPaths) && a.StagedHashes.SequenceEqual(b.StagedHashes) && a.BackupHashes.SequenceEqual(b.BackupHashes);
        private static bool JournalEquals(Journal a, Journal b) => SameTransaction(a,b)&&a.Sequence==b.Sequence&&a.Phase==b.Phase&&a.InstallationProgress==b.InstallationProgress;
        private static bool ValidHash(string hash) => hash != null && hash.Length == 64 && hash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        private static bool ValidPaths(string[] actual,string[] expected)=>actual.SequenceEqual(expected)&&actual.All(path=>!string.IsNullOrWhiteSpace(path)&&!Path.IsPathRooted(path)&&!path.Split('/').Contains(".."))&&actual.Distinct(StringComparer.Ordinal).Count()==actual.Length;
        private static bool Strings(Dictionary<string,object> fields,string name,out string[] values){values=null;if(!(fields[name] is List<object> list)||list.Any(value=>!(value is string)))return false;values=list.Cast<string>().ToArray();return true;}

        private sealed class JournalProblem : Exception { internal JournalProblem(ProductionSpatialPublicationDiagnostic value){Diagnostic=value;} internal ProductionSpatialPublicationDiagnostic Diagnostic; }
        private sealed class StrictJournalJson
        {
            private readonly string source; private int index; internal StrictJournalJson(string value){source=value;}
            internal Dictionary<string,object> Object(){Space();if(!Pop('{'))throw new FormatException();var result=new Dictionary<string,object>(StringComparer.Ordinal);var insensitive=new HashSet<string>(StringComparer.OrdinalIgnoreCase);Space();if(Pop('}'))return result;while(true){string key=String();Space();if(!Pop(':'))throw new FormatException();object value=Value();if(result.ContainsKey(key))throw new JournalProblem(ProductionSpatialPublicationDiagnostic.JournalFieldDuplicate);if(!insensitive.Add(key))throw new JournalProblem(ProductionSpatialPublicationDiagnostic.JournalFieldCaseAmbiguous);result.Add(key,value);Space();if(Pop('}'))break;if(!Pop(','))throw new FormatException();}Space();if(index!=source.Length)throw new FormatException();return result;}
            private object Value(){Space();if(index>=source.Length)throw new FormatException();if(source[index]=='\"')return String();if(source[index]=='[')return List();if(source[index]=='-'||char.IsDigit(source[index]))return Number();if(Take("true"))return true;if(Take("false"))return false;throw new FormatException();}
            private List<object> List(){index++;var result=new List<object>();Space();if(Pop(']'))return result;while(true){result.Add(Value());Space();if(Pop(']'))return result;if(!Pop(','))throw new FormatException();}}
            private string String(){Space();if(index>=source.Length||source[index++]!='\"')throw new FormatException();var b=new StringBuilder();while(index<source.Length){char c=source[index++];if(c=='\"')return b.ToString();if(c<' '||c=='\\')throw new FormatException();b.Append(c);}throw new FormatException();}
            private long Number(){int start=index;if(source[index]=='-')index++;if(index>=source.Length||!char.IsDigit(source[index]))throw new FormatException();while(index<source.Length&&char.IsDigit(source[index]))index++;return long.Parse(source.Substring(start,index-start),CultureInfo.InvariantCulture);}
            private void Space(){while(index<source.Length&&char.IsWhiteSpace(source[index]))index++;}private bool Pop(char c){Space();if(index<source.Length&&source[index]==c){index++;return true;}return false;}private bool Take(string value){if(index+value.Length<=source.Length&&source.Substring(index,value.Length)==value){index+=value.Length;return true;}return false;}
        }
    }

    internal sealed class ProductionSpatialPublicationContext
    {
        internal ProductionSpatialPublicationContext(string projectRoot,string authoringRoot,Action refresh,Action<ProductionSpatialPublicationFailurePoint> failure=null)
        {ProjectRoot=projectRoot;AuthoringRoot=authoringRoot;Refresh=refresh??(()=>{});Failure=failure;}
        internal string ProjectRoot{get;}internal string AuthoringRoot{get;}internal Action Refresh{get;}private Action<ProductionSpatialPublicationFailurePoint> Failure{get;}
        internal void Fail(ProductionSpatialPublicationFailurePoint point){try{Failure?.Invoke(point);}catch{throw new InjectedFailureException();}}
    }
    internal sealed class InjectedFailureException : Exception { }
}
#endif
