using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.Structures;

namespace DungeonBuilder.M0
{
    public static class SaveMigration
    {
        public const int LatestSchemaVersion = 2;
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

            if (root.primary.structureRuntime == null)
            {
                root.primary.structureRuntime = new StructureRuntimeState();
            }

            if (root.schemaVersion < LatestSchemaVersion)
            {
                root.schemaVersion = LatestSchemaVersion;
            }

            return root;
        }
    }
}
