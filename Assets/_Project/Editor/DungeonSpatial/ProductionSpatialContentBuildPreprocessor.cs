#if UNITY_EDITOR
using System;
using DungeonBuilder.M0.Editor.DungeonSpatial;
using DungeonBuilder.M0.EditorTools;
using UnityEditor;
using UnityEditor.Build;

namespace DungeonBuilder.M0.Editor.Build
{
    public sealed class ProductionSpatialContentBuildPreprocessor : BuildPlayerProcessor
    {
        private readonly ProductionSpatialContentBuildGate gate;

        public ProductionSpatialContentBuildPreprocessor() : this(new ProductionSpatialContentBuildGate()) { }

        internal ProductionSpatialContentBuildPreprocessor(ProductionSpatialContentBuildGate gate)
        {
            this.gate = gate;
        }

        public override int callbackOrder => -1000;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            PrepareForBuild(gate, buildPlayerContext.BuildPlayerOptions);
        }

        internal static void PrepareForBuild(ProductionSpatialContentBuildGate gate,
            BuildPlayerOptions buildPlayerOptions)
        {
            ValidateOrThrow(gate, ResolveScenesForProductionValidation(buildPlayerOptions));
        }

        internal static string[] ResolveScenesForProductionValidation(BuildPlayerOptions options)
        {
            string[] scenes = options.scenes;
            if ((options.options & BuildOptions.IncludeTestAssemblies) == 0 ||
                options.target != BuildTarget.StandaloneWindows64 || scenes == null || scenes.Length != 2 ||
                !string.Equals(scenes[1], DevelopmentBuildUtility.BootstrapScenePath,
                    StringComparison.Ordinal) || !IsUnityTestInitializationScene(scenes[0]) ||
                string.Equals(scenes[0], scenes[1], StringComparison.Ordinal)) return scenes;
            return new[] { DevelopmentBuildUtility.BootstrapScenePath };
        }

        private static bool IsUnityTestInitializationScene(string path)
        {
            const string prefix = "Assets/InitTestScene";
            const string suffix = ".unity";
            if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.Ordinal) ||
                !path.EndsWith(suffix, StringComparison.Ordinal)) return false;
            string identity = path.Substring(prefix.Length, path.Length - prefix.Length - suffix.Length);
            return Guid.TryParse(identity, out _);
        }

        internal static void ValidateOrThrow(ProductionSpatialContentBuildGate gate, string[] attemptedScenes)
        {
            ProductionSpatialBuildGateResult result = gate.Validate(attemptedScenes);
            if (!result.Success)
                throw new BuildFailedException("[ProductionSpatialBuildGate:" + result.Reason + "] " + result.Detail);
        }
    }
}
#endif
