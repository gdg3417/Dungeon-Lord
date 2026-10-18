#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonB3B1PlayModeTests
    {
        [UnityTest] public IEnumerator TimedCombatReconstructionUsesTheSameReceiptsInPlayMode()
        {
            yield return null;
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonB3B1Tests, Assembly-CSharp", true);
            Assert.DoesNotThrow(() => type.GetMethod("VerifyForPlayMode").Invoke(null, null));
            yield return null;
        }
    }
}
#endif
