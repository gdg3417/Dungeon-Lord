namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class CanonicalSaveSchemaVersions
    {
        public const int FrozenLegacyCanonicalMigrationTarget = 7;
        public const int PhaseFiveIntroduction = 10;
        public const int FloorActivationIntroduction = 11;
        public const int FloorKnowledgeIntroduction = 12;
        public const int RoomContentPositionIntroduction = 13;
        public const int LivingDungeonIntroduction = 14;
        public const int MonsterContinuityIntroduction = 15;
        public const int FrozenSchemaFifteen = MonsterContinuityIntroduction;
        public const int TimeProvenanceIntroduction = 16;
        public const int FrozenSchemaSixteen = TimeProvenanceIntroduction;
        public const int AtomicRecoveryIntroduction = 17;
        public const int ResearchTimingIntroduction = 18;
        public const int AuthoredRouteIntroduction = 19;
        public const int CurrentWritableTarget = ResearchTimingIntroduction;

        internal static bool HasAuthoredRoutes(int schema) => schema == AuthoredRouteIntroduction;

        internal static bool HasResearchTiming(int schema) =>
            schema == ResearchTimingIntroduction || schema == AuthoredRouteIntroduction;

        internal static bool HasPreciseTime(int schema) =>
            schema == AtomicRecoveryIntroduction || HasResearchTiming(schema);

        internal static int SpatialRegistration(int schema) =>
            schema == ResearchTimingIntroduction ? AtomicRecoveryIntroduction : schema;
    }
}
