using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0
{
    [Serializable]
    public class SaveRoot
    {
        public string schema = "save_root";
        public int schemaVersion = 1;
        public SaveData primary = new SaveData();
    }

    public interface ISaveMigrationStep
    {
        int FromVersion { get; }
        int ToVersion { get; }
        void Apply(SaveRoot root);
    }

    public sealed class MigrationRunner
    {
        private readonly Dictionary<int, ISaveMigrationStep> _steps = new Dictionary<int, ISaveMigrationStep>();

        public void Register(ISaveMigrationStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            _steps[step.FromVersion] = step;
        }

        public bool Run(SaveRoot root, int targetVersion, out string error)
        {
            error = string.Empty;
            if (root == null)
            {
                error = "Save root is null.";
                return false;
            }

            int guard = 0;
            while (root.schemaVersion < targetVersion)
            {
                if (!_steps.TryGetValue(root.schemaVersion, out ISaveMigrationStep step))
                {
                    error = $"Missing migration step from v{root.schemaVersion}.";
                    return false;
                }

                if (step.ToVersion <= root.schemaVersion)
                {
                    error = $"Invalid migration step from v{step.FromVersion} to v{step.ToVersion}.";
                    return false;
                }

                step.Apply(root);
                root.schemaVersion = step.ToVersion;

                guard++;
                if (guard > 32)
                {
                    error = "Migration guard tripped (possible loop).";
                    return false;
                }
            }

            return true;
        }
    }
}
