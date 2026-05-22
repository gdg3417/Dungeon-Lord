using DungeonBuilder.M0.Gameplay.DungeonLayout;

namespace DungeonBuilder.M0.Save
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

            if (root.Layout == null)
            {
                root.Layout = DungeonLayoutState.CreateEmpty(DefaultFloorCount, DefaultSlotsPerFloor);
            }

            if (root.SchemaVersion < LatestSchemaVersion)
            {
                root.SchemaVersion = LatestSchemaVersion;
            }

            return root;
        }
    }
}
