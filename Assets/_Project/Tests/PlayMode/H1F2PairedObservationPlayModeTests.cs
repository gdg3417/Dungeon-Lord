#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class H1F2PairedObservationPlayModeTests
    {
        [UnityTest] public IEnumerator PairedClockLifecycleAndBusyStorageRetainEligibleTime()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.PairedForegroundObservationTests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (string method in new[] { "SameSecondForegroundAndBackgroundRetainCapturedFractionsAndEligibility",
                "PauseResumeReanchorsWithoutBackgroundLogicalCredit", "StorageElapsedTimeBelongsToTheNextCapturedCut",
                "PredecessorPublicationInvalidatesOldObservation", "ActualForegroundWriterConsumesCapturedEndpointAndRetainsLaterStorageTime" })
                Assert.DoesNotThrow(() => type.GetMethod(method).Invoke(suite, null));
            yield return null;
        }
    }
}
#endif
