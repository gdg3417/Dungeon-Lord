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
            foreach (var variant in new[] { (false, false), (true, false), (true, true) }) {
                Assert.DoesNotThrow(() => type.GetMethod("ActualLifetimeOperationReusesPreparationAndRetainsEveryReader").Invoke(suite, new object[] { variant.Item1, variant.Item2 }));
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
