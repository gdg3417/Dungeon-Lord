using System;
using System.IO;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class SaveService
    {
        private const int LegacyCompatibilitySchemaVersion = 6;
        private readonly SimpleLogger _logger;
        private readonly SaveConfig _saveConfig;
        private readonly MigrationRunner _migrationRunner = new MigrationRunner();
        private SaveSpatialMigrationLimitsProfile _limits;
        private ProductionSpatialContentSnapshot _production;
        private SpatialLayoutCompatibilitySnapshot _compatibility;
        private byte[] _legacyConfiguration;
        private DetachedCurrentTargetValidationContext _validationContext;
        private DetachedCanonicalSaveSession _canonicalSession;
        private ISpatialMigrationFileSystem _canonicalFileSystem;
        private bool _canonicalConfigured;

        public event Action<SaveData> CanonicalRuntimePublished;

        public string SavePath { get; private set; }
        public DetachedCanonicalSaveSession CanonicalSession => _canonicalSession;

        public void ConfigureCanonical(SaveSpatialMigrationLimitsProfile limits,
            ProductionSpatialContentSnapshot production, SpatialLayoutCompatibilitySnapshot compatibility,
            byte[] legacyConfiguration)
        {
            _canonicalConfigured = true;
            _limits = limits; _production = production; _compatibility = compatibility;
            _legacyConfiguration = legacyConfiguration == null ? null : (byte[])legacyConfiguration.Clone();
            _validationContext = limits == null || production == null || compatibility == null ||
                legacyConfiguration == null ? null : new DetachedCurrentTargetValidationContext(
                    compatibility, production, legacyConfiguration, limits.Canonical);
        }

        public SaveService(SimpleLogger logger, SaveConfig saveConfig)
            : this(logger, saveConfig, Application.persistentDataPath)
        {
        }

        public SaveService(SimpleLogger logger, SaveConfig saveConfig, string persistentDataDirectory)
        {
            _logger = logger;
            _saveConfig = saveConfig;

            string fileName = (saveConfig != null && !string.IsNullOrEmpty(saveConfig.fileName))
                ? saveConfig.fileName
                : "save_primary.json";

            string baseDirectory = !string.IsNullOrEmpty(persistentDataDirectory)
                ? persistentDataDirectory
                : Application.persistentDataPath;

            SavePath = Path.Combine(baseDirectory, fileName);
        }

        public SaveData LoadOrCreate(string contentVersion, out string banner)
        {
            banner = string.Empty;

            // Unconfigured instances are retained only for schema<=6 test/repair compatibility.
            if (!_canonicalConfigured) return LoadOrCreateLegacy(contentVersion, out banner);
            if (_validationContext == null || _limits == null)
            { banner = Gd66MigrationReasonRegistry.PlayerLocalizationKey("gd66.profile.invalid"); return null; }
            SpatialMigrationActivationPreflight preflight = SpatialMigrationFileSystemSelector.Evaluate(SavePath);
            if (!preflight.IsSupported || preflight.FileSystem == null)
            { banner = Gd66MigrationReasonRegistry.PlayerLocalizationKey(preflight.Reason); return null; }
            _canonicalFileSystem = preflight.FileSystem;
            if (!File.Exists(SavePath))
            {
                SaveData initial = CreateNew(contentVersion);
                NativeCanonicalSaveResult created = NativeCanonicalSaveCreator.Create(SavePath,
                    _canonicalFileSystem, initial, _compatibility, _production, _legacyConfiguration, _limits);
                if (!created.IsSuccess)
                { banner = Gd66MigrationReasonRegistry.PlayerLocalizationKey(created.Reason); return null; }
                _canonicalSession = created.Session;
                return created.RuntimeProjection;
            }
            var coordinator = new DetachedSpatialSaveLoadCoordinator(_limits, _compatibility, _production,
                _legacyConfiguration, new Dictionary<string, byte[]>(),
                new RawSaveEnvelopeVersionContract(1, 6), CreateBlankFloorContract());
            DetachedSpatialSaveLoadResult loaded = coordinator.Load(SavePath, preflight);
            if (!loaded.IsSuccess)
            {
                _logger.Error("GD66 load failed: " + loaded.Reason);
                banner = Gd66MigrationReasonRegistry.PlayerLocalizationKey(loaded.Reason);
                return null;
            }
            _canonicalSession = loaded.Session;
            return loaded.RuntimeProjection;
        }

        private SaveData LoadOrCreateLegacy(string contentVersion, out string banner)
        {
            banner = string.Empty;
            if (!File.Exists(SavePath)) return CreateNew(contentVersion);
            try
            {
                string json = File.ReadAllText(SavePath);
                SaveRoot root = TryParseSaveRoot(json);
                if (root == null)
                { banner = "Save file invalid. Created a new save."; ArchiveCorruptSave(); return CreateNew(contentVersion); }
                _migrationRunner.Run(root, 6, out _);
                root = SaveMigration.MigrateToLatest(root);
                return root.primary;
            }
            catch
            { banner = "Save load failed. Created a new save."; ArchiveCorruptSave(); return CreateNew(contentVersion); }
        }

        public void Save(SaveData data, SaveReason reason)
        {
            if (data == null)
            {
                _logger.Error("Save called with null data.");
                return;
            }

            if (CanonicalMvpRouteProjection.HasCanonicalLookingState(data))
            {
                if (!_canonicalConfigured || _canonicalSession == null || _canonicalFileSystem == null)
                { _logger.Error("Canonical save authority is unavailable."); return; }
                long previous = data.lastSavedUtcUnix;
                data.lastSavedUtcUnix = TimeUtil.UtcNowUnixSeconds();
                DetachedCanonicalWriteResult result = CreateWriteAuthority().SaveRecognizedState(
                    SavePath, _canonicalFileSystem, _canonicalSession, data);
                if (!result.IsSuccess)
                { data.lastSavedUtcUnix = previous; _logger.Error("GD66 save failed: " + result.Reason); return; }
                _canonicalSession = result.Session;
                CanonicalRuntimePublished?.Invoke(result.RuntimeProjection);
                _logger.Info($"Saved canonical complete save. Reason: {reason}");
                return;
            }

            data.lastSavedUtcUnix = TimeUtil.UtcNowUnixSeconds();

            string json = JsonUtility.ToJson(data, true);
            SaveRoot root = new SaveRoot
            {
                schemaVersion = LegacyCompatibilitySchemaVersion,
                primary = data
            };
            json = JsonUtility.ToJson(root, true);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath);

                bool atomic = _saveConfig != null && _saveConfig.useAtomicWrites;

                if (atomic)
                {
                    string tempPath = SavePath + ".tmp";
                    File.WriteAllText(tempPath, json);
                    ReplaceFile(tempPath, SavePath);
                }
                else
                {
                    File.WriteAllText(SavePath, json);
                }

                _logger.Info($"Saved. Reason: {reason}");
                MaintainBackups();
            }
            catch (Exception ex)
            {
                _logger.Error($"Save write failed. Exception: {ex.Message}");
            }
        }

        public DetachedCanonicalWriteResult ExecuteCanonicalMutation(SaveData current,
            DetachedCanonicalMutationRequest request)
        {
            if (!_canonicalConfigured || _canonicalSession == null || _canonicalFileSystem == null)
                return new DetachedCanonicalWriteResult(false,
                    DetachedCanonicalSpatialMutation.ValidationFailedReason, false, false,
                    null, null, null, null);
            DetachedCanonicalWriteResult result = CreateWriteAuthority().Execute(SavePath,
                _canonicalFileSystem, _canonicalSession, current, request);
            if (result.IsSuccess)
            { _canonicalSession = result.Session; CanonicalRuntimePublished?.Invoke(result.RuntimeProjection); }
            return result;
        }

        private DetachedCanonicalWriteAuthority CreateWriteAuthority() =>
            new DetachedCanonicalWriteAuthority(_production, _compatibility,
                LegacyGameplayConfigurationContract.Parse(_legacyConfiguration),
                _validationContext, _limits);

        public void DeleteSave(out string banner)
        {
            banner = string.Empty;

            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    banner = "Save deleted.";
                    _logger.Warn("Save deleted by dev command.");
                }
                else
                {
                    banner = "No save to delete.";
                }
            }
            catch (Exception ex)
            {
                banner = "Failed to delete save.";
                _logger.Error($"Delete save failed. Exception: {ex.Message}");
            }
        }

        private SaveData CreateNew(string contentVersion)
        {
            long now = TimeUtil.UtcNowUnixSeconds();

            SaveData data = new SaveData
            {
                saveVersion = 1,
                contentVersion = contentVersion ?? "0.0.0",
                createdUtcUnix = now,
                lastSavedUtcUnix = now,
                lastPausedUtcUnix = 0,
                lastResumedUtcUnix = 0,
                totalTicks = 0,
                lastKnownAppState = "Boot",
                integrityFlags = Array.Empty<string>()
            };

            return data;
        }

        private static RawLegacyBlankFloorContract CreateBlankFloorContract()
        {
            MvpDungeonFloorLayoutState state = MvpDungeonFloorLayoutState.CreateEmptyStarterFloor();
            return new RawLegacyBlankFloorContract(state.NextRevision, state.Nodes.Select(node =>
                new RawLegacyBlankFloorNodeContract(node.FloorIndex, node.NodeIndex, node.SlotId,
                    node.CategoryId, node.OptionId, node.Revision)), true, true,
                new[] { "Nodes", "NextRevision" }, new[] { "FloorIndex", "NodeIndex", "SlotId",
                    "CategoryId", "OptionId", "Revision" });
        }

        private SaveRoot TryParseSaveRoot(string json)
        {
            SaveRoot root = JsonUtility.FromJson<SaveRoot>(json);
            if (root != null && root.primary != null)
            {
                return root;
            }

            SaveData legacy = JsonUtility.FromJson<SaveData>(json);
            if (legacy == null)
            {
                return null;
            }

            return new SaveRoot
            {
                schemaVersion = LegacyCompatibilitySchemaVersion,
                primary = legacy
            };
        }

        private void ArchiveCorruptSave()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return;
                }

                string dir = Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath;
                string name = Path.GetFileNameWithoutExtension(SavePath);
                string ext = Path.GetExtension(SavePath);

                string archived = Path.Combine(dir, $"{name}_corrupt_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}");
                File.Copy(SavePath, archived, true);
                File.Delete(SavePath);

                _logger.Warn($"Archived corrupt save to: {archived}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to archive corrupt save. Exception: {ex.Message}");
            }
        }

        private void ReplaceFile(string tempPath, string finalPath)
        {
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(tempPath, finalPath);
        }

        private void MaintainBackups()
        {
            int keep = _saveConfig != null ? Mathf.Max(0, _saveConfig.keepBackups) : 0;
            if (keep <= 0)
            {
                return;
            }

            try
            {
                string dir = Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath;
                string name = Path.GetFileNameWithoutExtension(SavePath);
                string ext = Path.GetExtension(SavePath);

                string backup = Path.Combine(dir, $"{name}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}");
                File.Copy(SavePath, backup, true);

                string[] backups = Directory.GetFiles(dir, $"{name}_backup_*{ext}");
                Array.Sort(backups);

                int extra = backups.Length - keep;
                for (int i = 0; i < extra; i++)
                {
                    File.Delete(backups[i]);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Backup maintenance failed. Exception: {ex.Message}");
            }
        }
    }
}
