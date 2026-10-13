#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonPhaseAPlayModeTests
    {
        [UnityTest]
        public IEnumerator PausingAcrossDifferentFrameCountsCannotChangeAuthoritativeOutcome()
        {
            var type=Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseAExecutionTests, Assembly-CSharp",true);
            var create=type.GetMethod("CreateExecutionForPlayMode",BindingFlags.Public|BindingFlags.Static);
            var advance=type.GetMethod("AdvanceForPlayMode",BindingFlags.Public|BindingFlags.Static);
            var complete=type.GetMethod("CompleteForPlayMode",BindingFlags.Public|BindingFlags.Static);
            var a=create.Invoke(null,null); var b=create.Invoke(null,null);
            advance.Invoke(null,new[] { a }); yield return null;
            advance.Invoke(null,new[] { a });
            for(int frame=0;frame<5;frame++) yield return null;
            advance.Invoke(null,new[] { b });
            Assert.That(complete.Invoke(null,new[] { b }),Is.EqualTo(complete.Invoke(null,new[] { a })));
        }
        [UnityTest]
        public IEnumerator NonspatialLootRemainsUnreachedWhilePausedBeforeItsBoundary()
        {
            var type=Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseAExecutionTests, Assembly-CSharp",true);
            var state=type.GetMethod("CreateNonspatialExecutionForPlayMode").Invoke(null,null);
            var advance=type.GetMethod("AdvanceForPlayMode");
            var inspect=state.GetType().GetMethod("Inspect");
            Func<object,int> count=s => Convert.ToInt32(s.GetType().GetProperty("ReachedLootInteractions").GetValue(s));
            for(int i=0;i<5;i++) advance.Invoke(null,new[] { state });
            for(int frame=0;frame<5;frame++) { yield return null; Assert.That(count(inspect.Invoke(state,null)),Is.Zero); }
            advance.Invoke(null,new[] { state });
            Assert.That(count(inspect.Invoke(state,null)),Is.EqualTo(1));
            yield return null;
            Assert.That(count(inspect.Invoke(state,null)),Is.EqualTo(1));
            type.GetMethod("CompleteForPlayMode").Invoke(null,new[] { state });
            Assert.That(count(inspect.Invoke(state,null)),Is.EqualTo(2));
        }
    }
}
#endif
