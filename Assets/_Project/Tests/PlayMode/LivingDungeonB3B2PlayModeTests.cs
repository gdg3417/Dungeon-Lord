#if UNITY_EDITOR
using System.Collections;
using System;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonB3B2PlayModeTests
    {
        [UnityTest] public IEnumerator ProductionCombatWorkValidationMatchesEditMode()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonB3B2Tests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (var data in new[] { ("0", false), ("-1", false), ("1", true), ("1000000", true),
                ("1.5", false), ("\"1000000\"", false), ("null", false), ("9223372036854775808", false) })
                Assert.DoesNotThrow(() => type.GetMethod("ProductionCombatWorkEnvelopeMustBePositiveInteger")
                    .Invoke(suite, new object[] { data.Item1, data.Item2 }));
            yield return null;
        }

        [UnityTest] public IEnumerator ForegroundAdmissionReopenAndActiveEditingUseProductionWriter()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonB3B2Tests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (string method in new[] { "ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce",
                "ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement",
                "SharedMonotonicClockPreservesWholeUnitsAcrossPartitionFocusPauseAndRestart",
                "DevelopmentReopenClockIsOptInAndPreservesRestoredHorizon",
                "PresentationObserverCannotChangeCheckpointBytesWorkReceiptsOrOutcome",
                "RecoveryConditionChangesAreStagedBeforeAdmissionWhenDoubleWalletDebitRoundsAway",
                "LiveFeedbackUsesCommittedCombatReceiptsAndLocalizedBypassReasons",
                "LiveFeedbackExplainsRecordedTrapCasualtyAndCarriedLoot",
                "AutomaticSettlementRetainsSessionReplayWithoutChangingHistoricalSaveContract",
                "TerminalSettlementPreservesTheCurrentSavedBoundary" })
                Assert.DoesNotThrow(() => type.GetMethod(method).Invoke(suite, null));
            yield return null;
        }
    }
}
#endif
