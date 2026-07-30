#if UNITY_EDITOR
using DungeonBuilder.M0.Editor.DungeonSpatial;
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
            ValidateOrThrow(gate, buildPlayerOptions.scenes);
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
