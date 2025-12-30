using System;
using System.IO;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public class SaveService
    {
        private readonly SimpleLogger _logger;
        private readonly SaveConfig _saveConfig;

        public string SavePath { get; private set; }

        public SaveService(SimpleLogger logger, SaveConfig saveConfig)
        {
            _logger = logger;
            _saveConfig = saveConfig;

            string fileName = (saveConfig != null && !string.IsNullOrEmpty(saveConfig.fileName))
                ? saveConfig.fileName
                : "save_primary.json";

            SavePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public SaveData LoadOrCreate(string contentVersion, out string banner)
        {
            banner = string.Empty;

            if (!File.Exists(SavePath))
            {
                _logger.Info("No save found. Creating new save.");
                return CreateNew(contentVersion);
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    banner = "Save file invalid. Created a new save.";
                    ArchiveCorruptSave();
                    return CreateNew(contentVersion);
                }

                if (data.createdUtcUnix <= 0)
                {
                    data.createdUtcUnix = TimeUtil.UtcNowUnixSeconds();
                }

                if (string.IsNullOrEmpty(data.contentVersion))
                {
                    data.contentVersion = contentVersion ?? "0.0.0";
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.Error($"Save load failed. Exception: {ex.Message}");
                banner = "Save load failed. Created a new save.";
                ArchiveCorruptSave();
                return CreateNew(contentVersion);
            }
        }

        public void Save(SaveData data, SaveReason reason)
        {
            if (data == null)
            {
                _logger.Error("Save called with null data.");
                return;
            }

            data.lastSavedUtcUnix = TimeUtil.UtcNowUnixSeconds();

            string json = JsonUtility.ToJson(data, true);

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

