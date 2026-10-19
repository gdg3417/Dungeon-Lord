#if UNITY_EDITOR
using System.Collections;
using DungeonBuilder.M0.Tests.EditMode;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonB3B2PlayModeTests
    {
        [UnityTest] public IEnumerator ForegroundAdmissionReopenAndActiveEditingUseProductionWriter()
        {
            yield return null;
            var suite = new LivingDungeonB3B2Tests();
            suite.ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce();
            suite.ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement();
            suite.SharedMonotonicClockPreservesWholeUnitsAcrossPartitionFocusPauseAndRestart();
            yield return null;
        }
    }
}
#endif
