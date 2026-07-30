#if UNITY_EDITOR
using DungeonBuilder.M0.Editor.DungeonSpatial;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DungeonBuilder.M0.Editor.Build
{
    public sealed class ProductionSpatialContentBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow(new ProductionSpatialContentBuildGate());
        }

        internal static void ValidateOrThrow(ProductionSpatialContentBuildGate gate)
        {
            ProductionSpatialBuildGateResult result = gate.Validate();
            if (!result.Success)
                throw new BuildFailedException("[ProductionSpatialBuildGate:" + result.Reason + "] " + result.Detail);
        }
    }
}
#endif
