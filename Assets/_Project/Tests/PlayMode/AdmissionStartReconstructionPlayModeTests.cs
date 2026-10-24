#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class AdmissionStartReconstructionPlayModeTests
    {
        [UnityTest] public IEnumerator OwnedPreparationRemainsIndependentAcrossExecutionAndFailure()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.AdmissionStartReconstructionTests, Assembly-CSharp", true);
            var suite = Activator.CreateInstance(type);
            foreach (bool descend in new[] { false, true }) {
                Assert.DoesNotThrow(() => type.GetMethod("ActualLifetimeOperationReusesPreparationAndRetainsEveryReader").Invoke(suite, new object[] { descend }));
                yield return null;
            }
            Assert.DoesNotThrow(() => type.GetMethod("MutableExecutionStateAndDisposalCannotAffectAnotherRestoration").Invoke(suite, null));
            yield return null;
            Assert.DoesNotThrow(() => type.GetMethod("AssuranceExceptionCleansUpBeforeTheNextOwnedOperation").Invoke(suite, null));
            yield return null;
        }
    }
}
#endif
