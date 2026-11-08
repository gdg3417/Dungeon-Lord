#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class ForegroundPredecessorValidationTests
    {
        private bool enabled, detailed;
        [SetUp] public void CaptureMetrics()
        { enabled = Metrics.Enabled; detailed = Metrics.DetailedEnabled; Metrics.Enabled = Metrics.DetailedEnabled = true; }
        [TearDown] public void RestoreMetrics()
        { Metrics.Reset(); Metrics.Enabled = enabled; Metrics.DetailedEnabled = detailed; }

        private static DetachedCurrentTargetValidationContext EquivalentContext(Fixture f)
        {
            var source = f.Context;
            var copy = new DetachedCurrentTargetValidationContext(source.Compatibility, source.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), source.Limits,
                source.RoomContentOccupancy, false, source.LivingDungeonProfiles, source.MonsterRecovery);
            copy.BindRecoveryInputs(source.RecoveryInputs);
            return copy;
        }
        private static void Pump(Fixture f, bool two, long through, bool reuse)
        {
            var original = f.Context;
            // A distinct but byte-equivalent context cannot borrow the session's
            // proof. This exercises the unchanged fresh-validation path without
            // adding a production switch or changing any captured input.
            if (!reuse) f.Context = EquivalentContext(f);
            try {
                var result = PhaseFourTestSupport.PumpForeground(f,
                    two ? f.Context.RecoveryInputs.TwoFloor : f.Context.RecoveryInputs.OneFloor, through);
                f.Accept(result);
            } finally { f.Context = original; }
        }
        private static long Next(Fixture f, bool finish = false)
        {
            var p = f.Runtime.livingDungeon.Parties.First();
            var e = LivingDungeonContracts.Restore(p, LivingDungeonContracts.Profile(f.Runtime.livingDungeon, f.Context.LivingDungeonProfiles));
            try {
                if (!finish) return checked(p.AdmittedAt + e.NextDueTime().Value);
                e.DrainToCompletion(); return checked(p.AdmittedAt + e.Inspect().LogicalTime);
            } finally { e.Abandon(); }
        }
        private static Fixture Admitted()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(); Pump(f, false, 0, true);
            Pump(f, false, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value, true); return f;
        }

        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void PumpPartitionsMatchFreshValidationAtEveryPublication(bool two, bool split)
        {
            var f = two ? LivingDungeonTwoFloorTests.Dungeon() : LivingDungeonB3B2Tests.Dungeon();
            var seed = f.Session.GetCurrentBytes(); var expected = new List<byte[]>();
            long validations = 0, priors = 0, stages = 0, writes = 0, readbacks = 0;
            foreach (bool reuse in new[] { false, true }) {
                f.FileSystem.Seed(f.ActivePath, seed); f.Reopen(); int index = 0;
                Metrics.Reset();
                void Step(long target) {
                    Pump(f, two, target, reuse);
                    if (!reuse) expected.Add(f.Session.GetCurrentBytes());
                    else CollectionAssert.AreEqual(expected[index++], f.Session.GetCurrentBytes());
                }
                Step(0); Step(f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                long finish = Next(f, true);
                if (split) for (int i = 0; i < 3; i++) Step(Next(f));
                Step(finish);
                Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
                if (!reuse) {
                    validations = Metrics.BoundaryCalls[(int)Metrics.Boundary.CompleteValidation];
                    priors = Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation];
                    stages = Metrics.Calls[(int)Metrics.Area.Stage]; writes = Metrics.Calls[(int)Metrics.Area.AtomicWrite];
                    readbacks = Metrics.BoundaryCalls[(int)Metrics.Boundary.SemanticReadback];
                    Assert.That(priors, Is.EqualTo(stages)); Assert.That(stages, Is.GreaterThan(writes));
                } else {
                    Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation], Is.Zero);
                    Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CompleteValidation], Is.EqualTo(validations - priors));
                    Assert.That(Metrics.Calls[(int)Metrics.Area.Stage], Is.EqualTo(stages));
                    Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.EqualTo(writes));
                    Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SemanticReadback], Is.EqualTo(readbacks));
                }
                TestContext.WriteLine("two=" + two + " split=" + split + " reuse=" + reuse + " prior=" +
                    Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation] + " complete=" +
                    Metrics.BoundaryCalls[(int)Metrics.Boundary.CompleteValidation] + " stages=" + Metrics.Calls[(int)Metrics.Area.Stage]);
            }
        }

        [Test] public void WaitingReleaseMatchesFreshValidationAndKeepsCausalProof()
        {
            var f = LivingDungeonTwoFloorTests.Waiting(); var seed = f.Session.GetCurrentBytes(); byte[] expected = null;
            foreach (bool reuse in new[] { false, true }) {
                f.FileSystem.Seed(f.ActivePath, seed); f.Reopen(); long finish = Next(f, true); Metrics.Reset();
                Pump(f, true, finish, reuse);
                if (!reuse) expected = f.Session.GetCurrentBytes(); else CollectionAssert.AreEqual(expected, f.Session.GetCurrentBytes());
                Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.TransferReleased], Is.EqualTo(1));
                Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1"), Is.EqualTo(1));
            }
        }

        [Test] public void ExposedValidationAndReturnedPartyCopiesCannotMutateEvidence()
        {
            var f = Admitted(); var owned = f.Session.ValidateForegroundPredecessor(f.Context, out var proof);
            var p = owned.LivingDungeon.Parties.Single(); var original = (byte[])p.Checkpoint.Clone();
            p.Checkpoint[0] ^= 1; p.NextOrdinal++;
            CollectionAssert.AreEqual(original, proof.CopyParty(p.Sequence).Checkpoint);
            var copy = proof.CopyParty(p.Sequence); copy.Checkpoint[0] ^= 1; copy.NextOrdinal++;
            CollectionAssert.AreEqual(original, proof.CopyParty(p.Sequence).Checkpoint);
            Assert.That(proof.Matches(f.Session), Is.True);
            Metrics.Reset();
            var rejected = f.Session.PrepareForegroundReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile),
                owned, owned.LivingDungeon, owned.MonsterContinuity, predecessor: proof);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation], Is.Zero);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CandidateValidation], Is.EqualTo(1));
        }

        [TestCase("reopen")] [TestCase("changed")] [TestCase("context")] [TestCase("profile")] [TestCase("configuration")]
        public void EvidenceRejectsStaleOrChangedDependencies(string mismatch)
        {
            var f = Admitted(); f.Session.ValidateForegroundPredecessor(f.Context, out var proof);
            Assert.That(proof.Matches(f.Session), Is.True);
            if (mismatch == "reopen") f.Reopen();
            else if (mismatch == "changed") {
                var loot = f.State.Floors[0].RoomContents.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId);
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(loot.AssignmentId, f.State.Floors[0].FloorInstanceId)));
            }
            else if (mismatch == "context") {
                var equivalent = EquivalentContext(f);
                var validated = f.Session.ValidateForegroundPredecessor(equivalent, out var wrong);
                Assert.That(validated.CurrentTargetValidated, Is.True); Assert.That(wrong, Is.Null); return;
            } else if (mismatch == "profile") {
                var profiles = (LivingDungeonProfile[])f.Context.LivingDungeonProfiles;
                int i = Array.FindIndex(profiles, p => p.Id == f.Runtime.livingDungeon.Scheduler.ProfileId);
                profiles[i] = f.Context.RecoveryInputs.OneFloor.CreateProfile(profiles[i].BaseBudget);
            } else {
                int tick = f.Context.RecoveryInputs.Bootstrap.tickSeconds++;
                try { Assert.That(proof.Matches(f.Session), Is.False); }
                finally { f.Context.RecoveryInputs.Bootstrap.tickSeconds = tick; }
                return;
            }
            Assert.That(proof.Matches(f.Session), Is.False);
        }

        [TestCase(false)] [TestCase(true)] public void InvalidBytesCannotIssueEvidence(bool checkpoint)
        {
            var f = Admitted(); var bytes = f.Session.GetCurrentBytes();
            if (checkpoint) {
                string text = System.Text.Encoding.UTF8.GetString(bytes);
                var p = f.Runtime.livingDungeon.Parties.Single();
                string chunk = Convert.ToBase64String(p.Checkpoint.Take(Math.Min(p.Checkpoint.Length, LivingDungeonContracts.BinaryChunkBytes)).ToArray());
                bytes = System.Text.Encoding.UTF8.GetBytes(text.Replace(chunk, (chunk[0] == 'A' ? "B" : "A") + chunk.Substring(1)));
            } else bytes = System.Text.Encoding.UTF8.GetBytes("{malformed");
            var session = (DetachedCanonicalSaveSession)Activator.CreateInstance(typeof(DetachedCanonicalSaveSession),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { bytes, f.Context, f.Profile }, null);
            Assert.That(session.ValidateForegroundPredecessor(f.Context, out var proof).IsValid, Is.False);
            Assert.That(proof, Is.Null);
        }

        [TestCase(false)] [TestCase(true)] public void SharedWriterDefaultsStillValidateThePredecessor(bool spatialOnly)
        {
            var f = Fixture.Create(null); Metrics.Reset();
            var result = spatialOnly ? f.Session.PrepareSpatialOnlyReplacement(f.State) :
                f.Session.PrepareLiveReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.PriorValidation], Is.EqualTo(1));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.CandidateValidation], Is.EqualTo(1));
            Assert.That(result.ForegroundPredecessor, Is.Null);
        }
    }
}
#endif
