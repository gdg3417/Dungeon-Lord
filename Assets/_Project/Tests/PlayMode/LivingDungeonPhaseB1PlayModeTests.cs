#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonPhaseB1PlayModeTests
    {
        [UnityTest]
        public IEnumerator FreshRestorationAndDeadlinePauseAcrossFramesRetainOutcome()
        {
            var type = Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB1Tests, Assembly-CSharp", true);
            var create = type.GetMethod("CreateForPlayMode"); var restore = type.GetMethod("RestoreForPlayMode"); var complete = type.GetMethod("CompleteForPlayMode");
            var inspect = type.GetMethod("InspectForPlayMode"); var advance = type.GetMethod("AdvanceForPlayMode");
            var dueMethod = type.GetMethod("DueForPlayMode"); var elapsed = type.GetMethod("ElapsedForPlayMode");
            var reference = create.Invoke(null, null); var state = create.Invoke(null, null);
            for (int cycle = 0; cycle < 8; cycle++) {
                long due = (long)dueMethod.Invoke(null, new[] { state });
                if (due > (long)elapsed.Invoke(null, new[] { state })) {
                    var before = inspect.Invoke(null, new[] { state });
                    for (int frame = 0; frame <= cycle; frame++) {
                        Assert.That(advance.Invoke(null, new object[] { state, due - 1 }), Is.EqualTo(0));
                        Assert.That(inspect.Invoke(null, new[] { state }), Is.EqualTo(before));
                        yield return null;
                    }
                }
                advance.Invoke(null, new object[] { state, due }); state = restore.Invoke(null, new[] { state }); yield return null;
            }
            Assert.That(complete.Invoke(null, new object[] { state }), Is.EqualTo(complete.Invoke(null, new object[] { reference })));
        }
    }
}
#endif
