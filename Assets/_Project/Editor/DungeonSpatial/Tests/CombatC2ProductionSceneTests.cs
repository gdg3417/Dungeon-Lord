#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest] public IEnumerator CombatC2ConsecutiveRunsStartFromCommittedIndividualsAndReplayRemainsReadOnly()
        {
            var root=GameRoot.Instance;
            root.Save.structureRuntime.ManaReserve=0;
            Assert.That(root.SimulateRunOnce(),Is.True);
            var committed=root.Save.monsterContinuity.Inspect().ToDictionary(r=>r.AssignmentId,r=>(r.Health,r.RevivalRequired));
            Assert.That(committed.Count,Is.GreaterThan(0));
            Assert.That(root.SimulateRunOnce(),Is.True);
            var run=root.Save.runHistory.LatestOutcome;
            Assert.That(CombatC0Diagnostics.IsCombatC2(run),Is.True);
            foreach(var monster in run.CombatMonsters)
            {
                Assert.That(monster.InitialHealth,Is.EqualTo(committed[monster.AssignmentId].Health));
                Assert.That(monster.InitialRevivalRequired,Is.EqualTo(committed[monster.AssignmentId].RevivalRequired));
            }
            var before=root.SaveService.CanonicalSession.GetCurrentBytes();
            Assert.That(controller.StartReplay(),Is.True); controller.ToggleReplayPause();
            foreach(var actor in controller.Replay.Combat.Actors.Where(a=>a.IsMonster))
                Assert.That(actor.InitialHealth,Is.EqualTo(committed[actor.AssignmentId].Health));
            for(int i=0;i<3;i++) yield return null;
            CollectionAssert.AreEqual(before,root.SaveService.CanonicalSession.GetCurrentBytes());
            controller.CloseReplay();
        }
    }
}
#endif
