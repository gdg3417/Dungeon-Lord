#if UNITY_EDITOR
using System.Collections;
using System;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonB3B2PlayModeTests
    {
        [UnityTest] public IEnumerator ForegroundAdmissionReopenAndActiveEditingUseProductionWriter()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonB3B2Tests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (string method in new[] { "ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce",
                "ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement",
                "SharedMonotonicClockPreservesWholeUnitsAcrossPartitionFocusPauseAndRestart",
                "DevelopmentReopenClockIsOptInAndPreservesRestoredHorizon" })
                Assert.DoesNotThrow(() => type.GetMethod(method).Invoke(suite, null));
            yield return null;
        }
    }
}
#endif
