#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public class LivingDungeonPhaseB3APlayModeTests
    {
        [UnityTest] public IEnumerator FramesCannotSettleAndExplicitAtomicSettlementSurvivesReopen()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(
                "DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB3ATests")).First(t => t != null);
            var state = type.GetMethod("CreateForPlayMode").Invoke(null, null);
            var fingerprint = type.GetMethod("FingerprintForPlayMode");
            var before = fingerprint.Invoke(null, new[] { state });
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(fingerprint.Invoke(null, new[] { state }), Is.EqualTo(before));
            type.GetMethod("SettleForPlayMode").Invoke(null, new[] { state });
            Assert.That(fingerprint.Invoke(null, new[] { state }), Is.Not.EqualTo(before));
        }
    }
}
#endif
