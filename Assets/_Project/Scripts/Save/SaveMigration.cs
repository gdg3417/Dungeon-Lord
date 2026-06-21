using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class SaveMigration
    {
        public const int LatestSchemaVersion = 6;
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

            if (root.primary.mvpDungeonFloorLayout == null)
            {
                root.primary.mvpDungeonFloorLayout = MvpDungeonFloorLayoutState.CreateStarterFloorFromLegacyPlacements(root.primary.mvpDungeonPlacements);
            }

            if (root.primary.mvpDungeonFloorLayout.Nodes == null)
            {
                root.primary.mvpDungeonFloorLayout.Nodes = MvpDungeonFloorLayoutState.CreateStarterFloorFromLegacyPlacements(root.primary.mvpDungeonPlacements).Nodes;
            }

            MvpDungeonLayoutResolver.BackfillMissingStarterNodesFromLegacy(root.primary.mvpDungeonFloorLayout, root.primary.mvpDungeonPlacements);

            if (root.primary.mvpDungeonFloorLayout.NextRevision < 1)
            {
                root.primary.mvpDungeonFloorLayout.NextRevision = 1;
            }

            if (root.primary.mvpRoomSlotAssignments == null)
            {
                root.primary.mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection();
            }

            if (root.primary.mvpRoomSlotAssignments.Rooms == null)
            {
                root.primary.mvpRoomSlotAssignments.Rooms = new System.Collections.Generic.List<MvpRoomSlotAssignmentState>();
            }

            if (root.primary.mvpRoomSlotAssignments.NextRevision < 1)
            {
                root.primary.mvpRoomSlotAssignments.NextRevision = 1;
            }

            if (root.primary.structureRuntime == null)
            {
                root.primary.structureRuntime = new StructureRuntimeState();
            }

            if (root.primary.runHistory == null)
            {
                root.primary.runHistory = new RunHistoryState();
            }

            if (root.primary.completedObjectives == null)
            {
                root.primary.completedObjectives = new CompletedObjectiveState();
            }

            if (root.primary.completedObjectives.ObjectiveIds == null)
            {
                root.primary.completedObjectives.ObjectiveIds = System.Array.Empty<string>();
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
