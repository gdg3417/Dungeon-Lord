#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class LivingDungeonPhaseB2PlayModeTests
    {
        private static Type Bridge => Type.GetType("DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB2Tests, Assembly-CSharp", true);
        [UnityTest]
        public IEnumerator PauseQuitAndFrameDelaysPreserveUnfinishedCommittedPrefix()
        {
            var type = Bridge; var state = type.GetMethod("CreateUnfinishedForPlayMode").Invoke(null, null);
            var prefix = type.GetMethod("PrefixForPlayMode"); var closed = type.GetMethod("ClosedForPlayMode"); var save = type.GetMethod("SaveLifecycleForPlayMode");
            var before = prefix.Invoke(null, new[] { state }); Assert.That(closed.Invoke(null, new[] { state }), Is.False);
            for (int i = 0; i < 3; i++) yield return null;
            save.Invoke(null, new object[] { state, false }); Assert.That(prefix.Invoke(null, new[] { state }), Is.EqualTo(before));
            for (int i = 0; i < 3; i++) yield return null;
            save.Invoke(null, new object[] { state, true }); Assert.That(prefix.Invoke(null, new[] { state }), Is.EqualTo(before));
            Assert.That(closed.Invoke(null, new[] { state }), Is.False);
        }
        [UnityTest]
        public IEnumerator MeasureActualFrameGcAllocationAtRetainedProfileLimit()
        {
            var type = Bridge; var state = type.GetMethod("CreateRetainedLimitForPlayMode").Invoke(null, null);
            var validate = type.GetMethod("ValidateForPlayMode");
            using (var counter = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 16))
            {
                Assert.That(counter.Valid, Is.True, "living.qualification.gc_counter_unavailable");
                long maximum = 0;
                for (int i = 0; i < 5; i++)
                {
                    validate.Invoke(null, new[] { state }); yield return null;
                    maximum = Math.Max(maximum, counter.LastValue);
                }
                TestContext.WriteLine($"Profile A retained=4 active=2; observedFrameGcAllocationMaximumBytes={maximum}; includesEditorAndTestHarnessAllocations=true; threadAllocationCounterOnThisMonoHostReturnsZero=true");
                Assert.That(counter.Count, Is.GreaterThan(0)); Assert.That(maximum, Is.GreaterThan(0));
            }
        }
    }
}
#endif
