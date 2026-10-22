#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonTwoFloorPlayModeTests
    {
        [UnityTest] public IEnumerator WaitingReleaseAndSnapshotReconciliationUseTheCanonicalWriter()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonTwoFloorTests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (string method in new[] { "BlockReleaseAndTransferReplayRetainSourceUntilCompletionWithoutCombatWork",
                "AutomaticTwoFloorAdmissionCompletesAndRetiresExactlyOnce", "WaitingReopensAndRealRetirementReleasesItWithAuthenticatedCause",
                "WaitingProtectsBothFloorsFromRecoveryAndThrottlesAdmissionAfterReopen", "CurrentDeactivationDoesNotFabricateVacancyOrRewriteCapturedRoute",
                "CompletedForgedReleaseCannotRetireItsBlockerOrBypassVacancyPermit" }) {
                Assert.DoesNotThrow(() => type.GetMethod(method).Invoke(suite, null)); yield return null;
            }
            Assert.DoesNotThrow(() => type.GetMethod("OverlappingSnapshotsPreservePriorDamageAndPartialRecoveryAtBothSettlements").Invoke(suite, new object[] { true }));
            yield return null;
        }
    }
}
#endif
