using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class SaveMigration
    {
        public const int LatestSchemaVersion = 3;
        public const int DefaultFloorCount = 5;
        public const int DefaultSlotsPerFloor = 6;

        public static SaveRoot MigrateToLatest(SaveRoot root)
        {
            if (root == null)
            {
                root = new SaveRoot();
            }

            if (root.primary == null)
            {
                root.primary = new SaveData();
            }

            if (root.primary.dungeonLayout == null)
            {
                root.primary.dungeonLayout = DungeonLayoutState.CreateEmpty(DefaultFloorCount, DefaultSlotsPerFloor);
            }

            if (root.primary.mvpDungeonPlacements == null)
            {
                root.primary.mvpDungeonPlacements = new MvpDungeonPlacementState();
            }

            if (root.primary.mvpDungeonPlacements.Entries == null)
            {
                root.primary.mvpDungeonPlacements.Entries = new System.Collections.Generic.List<MvpDungeonPlacementEntry>();
            }

            if (root.primary.mvpDungeonPlacements.NextRevision < 1)
            {
                root.primary.mvpDungeonPlacements.NextRevision = 1;
            }

            if (root.primary.structureRuntime == null)
            {
                root.primary.structureRuntime = new StructureRuntimeState();
            }

            if (root.primary.runHistory == null)
            {
                root.primary.runHistory = new RunHistoryState();
            }

            RunHistoryState history = root.primary.runHistory;
            if (history.RecentOutcomes == null)
            {
                history.RecentOutcomes = System.Array.Empty<RunOutcomeRecord>();
            }

            if ((history.RecentOutcomes == null || history.RecentOutcomes.Length == 0) && history.LatestOutcome != null)
            {
                history.RecentOutcomes = new[] { history.LatestOutcome };
            }

            if (root.schemaVersion < LatestSchemaVersion)
            {
                root.schemaVersion = LatestSchemaVersion;
            }

            return root;
        }
    }
}
