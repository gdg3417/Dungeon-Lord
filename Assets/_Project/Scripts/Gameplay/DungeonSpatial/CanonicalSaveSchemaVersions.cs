namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class CanonicalSaveSchemaVersions
    {
        public const int FrozenLegacyCanonicalMigrationTarget = 7;
        public const int PhaseFiveIntroduction = 10;
        public const int FloorActivationIntroduction = 11;
        public const int FloorKnowledgeIntroduction = 12;
        public const int CurrentWritableTarget = FloorKnowledgeIntroduction;
    }
}
