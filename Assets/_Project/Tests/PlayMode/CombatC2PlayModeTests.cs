#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class CombatC2PlayModeTests
    {
        [UnityTest] public IEnumerator FrameDelaysDoNotAdvanceRecoveryAndRestartPreservesRevival()
        {
            var type=Type.GetType("DungeonBuilder.M0.Tests.EditMode.CombatC2Tests, Assembly-CSharp",true);
            var test=Activator.CreateInstance(type);
            for(int i=0;i<3;i++) yield return null;
            type.GetMethod("LivingBelowReadinessCanFightWhilePartiallyRevivedCannotAndConsecutiveRunsRetainConsequences").Invoke(test,null);
            for(int i=0;i<3;i++) yield return null;
            type.GetMethod("FundingRestoresWholeHealthOnlyAndClearsRevivalExactlyAtReadiness").Invoke(test,new object[]{0,true,2.75,2,true,.75});
        }
    }
}
#endif
