namespace DungeonBuilder.M0
{
    public static class DevelopmentDiagnosticsPolicy
    {
        public static bool AreDiagnosticsEnabled(bool isDevelopmentBuild, bool devFeatureFlagEnabled)
        {
            return isDevelopmentBuild && devFeatureFlagEnabled;
        }

        public static bool IsCurrentBuildDevelopment()
        {
            return UnityEngine.Application.isEditor || UnityEngine.Debug.isDebugBuild;
        }
    }
}
