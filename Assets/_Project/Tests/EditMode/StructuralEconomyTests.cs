#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class StructuralEconomyTests
    {
        private static StructuralEconomyConfiguration Config() => JsonUtility.FromJson<StructuralEconomyConfiguration>(
            File.ReadAllText("Assets/_Project/Resources/structural_economy.json"));
        private static Fixture R1()
        {
            var f = Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            return f;
        }
        private static StructuralInvestmentRecord[] Ledger(Fixture f) => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
            f.Session.GetCurrentBytes(), f.Context).Investment;
        private static StructuralEditPreview Build(Fixture f, string id = "spatial.room.basic", int x = 0, int y = 6,
            string exit = "east") => StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = id, Anchor = new TileCoordinate(x, y), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = exit }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
        private static StructuralEconomyPreview Price(Fixture f, StructuralEditPreview preview) => StructuralEconomyService.Preview(
            preview, f.State, Ledger(f), f.Runtime.structureRuntime.ManaReserve, f.Economy);
        private static StructuralEditPreview Move(Fixture f) => StructuralRenovationService.PreviewMovement(f.State,
            new StructuralMovementRequest { RoomInstanceId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId,
                Anchor = new TileCoordinate(0, 3) }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
        private static StructuralEditPreview Replace(Fixture f, string id = "spatial.room.rectangle") =>
            StructuralRenovationService.PreviewReplacement(f.State, new StructuralReplacementRequest {
                RoomInstanceId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, RoomDefinitionId = id },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
        private static StructuralEditPreview Delete(Fixture f) => StructuralDeletionService.Preview(f.State,
            new StructuralDeletionRequest { TargetRoomInstanceId = f.State.Floors[0].Layout.Rooms.Single(r =>
                r.RoomInstanceId.Contains(".room.player.")).RoomInstanceId }, f.RemovalPolicy, f.Production,
                f.Configuration, f.Profile.Canonical);

        [Test]
        public void ProductionConfig_IsCompleteImmutableAndOrderIndependent()
        {
            var f = R1(); var c = Config(); Array.Reverse(c.Rooms);
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out var snapshot), Is.True);
            c.Rooms.Single(r => r.DefinitionId == "spatial.room.basic").Mana = 999;
            Assert.That(snapshot.TryRoom("spatial.room.basic", out double value), Is.True);
            Assert.That(value, Is.EqualTo(100)); Assert.That(snapshot.ManaCapacity, Is.EqualTo(1000));
            Assert.That(snapshot.UndoSeconds, Is.EqualTo(30));
        }

        [TestCase("duplicate")][TestCase("missing")][TestCase("negative")][TestCase("nan")][TestCase("infinity")]
        [TestCase("movement")][TestCase("replacement")][TestCase("refund")][TestCase("refund_negative")]
        [TestCase("capacity")][TestCase("duration")][TestCase("corridor_missing")]
        public void InvalidConfig_FailsClosed(string scenario)
        {
            var f = R1(); var c = Config();
            switch (scenario)
            {
                case "duplicate": c.Rooms = c.Rooms.Concat(new[] { c.Rooms[0] }).ToArray(); break;
                case "missing": c.Rooms = c.Rooms.Skip(1).ToArray(); break;
                case "corridor_missing": c.Corridors = Array.Empty<StructuralPrice>(); break;
                case "negative": c.Rooms[0].Mana = -1; break;
                case "nan": c.Rooms[0].Mana = double.NaN; break;
                case "infinity": c.Corridors[0].Mana = double.PositiveInfinity; break;
                case "movement": c.MovementFactor = -1; break;
                case "replacement": c.ReplacementFactor = double.NaN; break;
                case "refund": c.RefundPercentage = 1.01; break;
                case "refund_negative": c.RefundPercentage = -1; break;
                case "capacity": c.ManaCapacity = 0; break;
                case "duration": c.UndoSeconds = 0; break;
            }
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out _), Is.False);
        }

        [TestCase("spatial.room.basic", 0, 6, "east", 100)]
        [TestCase("spatial.room.rectangle", 4, 1, "north", 100)]
        [TestCase("spatial.room.large_chamber", 4, 1, "north", 200)]
        [TestCase("spatial.room.basic", 5, 2, "east", 105)]
        [TestCase("spatial.room.basic", 8, 2, "north", 120)]
        public void ConfiguredConstructionAndPhysicalTilePrices(string id, int x, int y, string exit, double expected)
        {
            var f = R1(); var p = Price(f, Build(f, id, x, y, exit));
            Assert.That(p.IsAffordable, Is.True, p.Reason); Assert.That(p.Cost, Is.EqualTo(expected));
            Assert.That(p.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(expected));
            var existing = f.State.Floors[0].Layout.Edges.Select(e => e.EdgeId).ToArray();
            string[] newEdges = p.Spatial.DetachedCandidate.Floors[0].Layout.Edges.Where(e =>
                !existing.Contains(e.EdgeId)).Select(e => e.EdgeId).ToArray();
            Assert.That(p.Investment.Where(r => newEdges.Contains(r.StructureId))
                .Sum(r => r.ConstructionMana), Is.EqualTo(expected - (id.EndsWith("large_chamber") ? 200 : 100)));
            Assert.That(p.ResultingMana, Is.EqualTo(1000 - expected));
        }

        [TestCase(101, true)][TestCase(100, true)][TestCase(99, false)]
        public void CurrentBalanceBoundaryAndStaleAffordability(double balance, bool succeeds)
        {
            var f = R1(); var spatial = Build(f); Assert.That(Price(f, spatial).IsAffordable, Is.True);
            f.Runtime.structureRuntime.ManaReserve = balance;
            byte[] before = f.Session.GetCurrentBytes();
            var result = f.Execute(DetachedCanonicalMutationRequest.Construct(spatial));
            Assert.That(result.IsSuccess, Is.EqualTo(succeeds), result.Reason);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(balance));
            if (succeeds)
            {
                Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(balance - 100));
                f.Accept(result); var repeat = f.Execute(DetachedCanonicalMutationRequest.Construct(spatial));
                Assert.That(repeat.IsSuccess, Is.False); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(balance - 100));
            }
            else
            {
                Assert.That(result.Reason, Is.EqualTo(StructuralEconomyService.InsufficientReason));
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(result.RuntimeProjection, Is.Null);
            }
        }

        [TestCase(false)][TestCase(true)]
        public void PersistenceFailurePublishesNeitherSideAndRetryPaysExactlyOnce(bool deletion)
        {
            var f = R1(); var build = Build(f);
            if (deletion) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(build)));
            var request = deletion ? DetachedCanonicalMutationRequest.Delete(Delete(f)) : DetachedCanonicalMutationRequest.Construct(build);
            double beforeMana = f.Runtime.structureRuntime.ManaReserve; byte[] before = f.Session.GetCurrentBytes();
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            var failed = f.Execute(request); Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(beforeMana));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.DisableFailure(); f.Accept(f.Execute(request));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(beforeMana + (deletion ? 75 : -100)));
            byte[] committed = f.Session.GetCurrentBytes(); Assert.That(f.Execute(request).IsSuccess, Is.False);
            CollectionAssert.AreEqual(committed, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void MissingConfigAndInvalidGeometryNeverSpend()
        {
            var f = R1(); var valid = Build(f); byte[] before = f.Session.GetCurrentBytes(); f.Economy = null;
            Assert.That(f.Execute(DetachedCanonicalMutationRequest.Construct(valid)).Reason, Is.EqualTo(StructuralEconomyService.InvalidReason));
            Assert.That(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f, x: -10))).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(1000));
        }

        [Test]
        public void HistoricalInvestmentSurvivesReopenCatalogChangeAndCorridorRetirement()
        {
            var f = R1(); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f, x: 5, y: 2))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(895));
            f.Reopen(); Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(105));
            var c = Config(); foreach (var r in c.Rooms) r.Mana = 900;
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out f.Economy), Is.True);
            var deletion = Delete(f); var p = Price(f, deletion);
            Assert.That(p.RefundBasis, Is.EqualTo(100)); Assert.That(p.Refund, Is.EqualTo(75));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(deletion))); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(970));
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(5));
            FloorRouteEdge retained = f.State.Floors[0].Layout.Edges.Single(e =>
                Ledger(f).Single(r => r.ConstructionMana == 5).StructureId == e.EdgeId);
            Assert.That(retained.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            f.Economy = PhaseFourTestSupport.Economy(f.Production, f.Profile.Canonical);
            StructuralEconomyPreview rebuild = Price(f, Build(f, x: 5, y: 2));
            FloorRouteEdge incoming = rebuild.Spatial.DetachedCandidate.Floors[0].Layout.Edges.Single(e =>
                e.EdgeId.EndsWith(".edge.incoming"));
            CollectionAssert.AreEquivalent(retained.Footprint.OccupiedTiles, incoming.Footprint.OccupiedTiles);
            Assert.That(rebuild.BaseCost, Is.EqualTo(100));
            Assert.That(rebuild.Cost, Is.EqualTo(100));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(rebuild.Spatial)));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(870));
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(105));
            Assert.That(Ledger(f).Single(r => r.StructureId.EndsWith(".edge.incoming")).ConstructionMana, Is.EqualTo(5));
        }

        [Test]
        public void RebuildChargesOnlyNewPhysicalTilesAndKeepsCarriedInvestmentOnce()
        {
            var f = R1();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f, x: 5, y: 2))));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(Delete(f))));
            FloorRouteEdge retained = f.State.Floors[0].Layout.Edges.Single(e =>
                Ledger(f).Single(r => r.ConstructionMana == 5).StructureId == e.EdgeId);

            StructuralEconomyPreview rebuild = Price(f, Build(f, x: 8, y: 2, exit: "north"));
            Assert.That(rebuild.IsAffordable, Is.True, rebuild.Reason);
            FloorRouteEdge incoming = rebuild.Spatial.DetachedCandidate.Floors[0].Layout.Edges.Single(e =>
                e.EdgeId.EndsWith(".edge.incoming"));
            int carried = incoming.Footprint.OccupiedTiles.Intersect(
                retained.Footprint.OccupiedTiles).Count();
            int created = incoming.Footprint.OccupiedTiles.Length - carried;
            Assert.That(carried, Is.EqualTo(retained.Footprint.OccupiedTiles.Length));
            Assert.That(created, Is.GreaterThan(0));
            Assert.That(rebuild.BaseCost, Is.EqualTo(100 + created * 5));
            Assert.That(rebuild.Investment.Single(r => r.StructureId == incoming.EdgeId).ConstructionMana,
                Is.EqualTo(5 + created * 5));
            Assert.That(rebuild.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(5 + rebuild.Cost));

            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(rebuild.Spatial)));
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(5 + rebuild.Cost));
        }

        [Test]
        public void RebuildModifiersAllocateExactPaymentWithoutRepurchasingCarriedCorridor()
        {
            var f = R1();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f, x: 5, y: 2))));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(Delete(f))));
            var modifiers = new[] { new FormulaModifier("test.research", ModifierBucket.Research,
                    ModifierType.AdditiveFlat, 1),
                new FormulaModifier("test.heat", ModifierBucket.Heat,
                    ModifierType.MultiplicativePercent, 0.5) };

            StructuralEditPreview spatial = Build(f, x: 5, y: 2);
            StructuralEconomyPreview rebuild = StructuralEconomyService.Preview(spatial, f.State,
                Ledger(f), f.Runtime.structureRuntime.ManaReserve, f.Economy, modifiers);

            Assert.That(rebuild.BaseCost, Is.EqualTo(100));
            Assert.That(rebuild.Cost, Is.EqualTo(151));
            Assert.That(rebuild.Investment.Single(r => r.StructureId.EndsWith(".edge.incoming"))
                .ConstructionMana, Is.EqualTo(5));
            Assert.That(rebuild.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(156));
        }

        [Test]
        public void RebuildWithDirectDoorwayRefundsActuallyRetiredPhysicalInfrastructure()
        {
            var f = R1();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f, x: 5, y: 2))));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(Delete(f))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(970));

            StructuralEconomyPreview rebuild = Price(f, Build(f));
            FloorRouteEdge incoming = rebuild.Spatial.DetachedCandidate.Floors[0].Layout.Edges.Single(e =>
                e.EdgeId.EndsWith(".edge.incoming"));
            Assert.That(incoming.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(rebuild.BaseCost, Is.EqualTo(100));
            Assert.That(rebuild.Cost, Is.EqualTo(100));
            Assert.That(rebuild.RefundBasis, Is.EqualTo(5));
            Assert.That(rebuild.Refund, Is.EqualTo(3));
            Assert.That(rebuild.CreditedRefund, Is.EqualTo(3));
            Assert.That(rebuild.ResultingMana, Is.EqualTo(873));
            Assert.That(rebuild.Investment.Single(r => r.StructureId == incoming.EdgeId).ConstructionMana,
                Is.Zero);
            Assert.That(rebuild.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(100));

            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(rebuild.Spatial)));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(873));
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(100));
        }

        [TestCase(800, 875)][TestCase(925, 1000)][TestCase(990, 1000)][TestCase(1200, 1200)]
        public void RefundUsesSingleCapAndPreservesExistingExcess(double balance, double expected)
        {
            var f = R1(); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f))));
            f.Runtime.structureRuntime.ManaReserve = balance;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(Delete(f))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(expected));
        }

        [Test]
        public void FormulaOrderingRoundingAndInvestmentAllocationUseActualCharge()
        {
            var f = R1(); var p = Build(f, x: 5, y: 2);
            var modifiers = new[] { new FormulaModifier("test.research", ModifierBucket.Research, ModifierType.AdditiveFlat, 1),
                new FormulaModifier("test.heat", ModifierBucket.Heat, ModifierType.MultiplicativePercent, 0.5) };
            var result = StructuralEconomyService.Preview(p, f.State, Ledger(f), 1000, f.Economy, modifiers);
            Assert.That(result.Cost, Is.EqualTo(159)); // (105 * 1.5) + 1 = 158.5; framework rounds away from zero.
            Assert.That(result.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(159));
            Assert.That(result.Investment.Single(r => r.StructureId.EndsWith(".edge.incoming")).ConstructionMana, Is.EqualTo(7));
            var authority = new DetachedCanonicalWriteAuthority(f.Production, f.Compatibility, f.Configuration,
                f.Context, f.Profile, f.RemovalPolicy, f.Economy, modifiers);
            f.Accept(authority.Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                DetachedCanonicalMutationRequest.Construct(p)));
            f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(841));
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.EqualTo(159));
            Assert.That(Ledger(f).Single(r => r.StructureId.EndsWith(".edge.incoming")).ConstructionMana, Is.EqualTo(7));
        }

        [TestCase(false)][TestCase(true)]
        public void FailedPaidRenovationPreservesIdentityInvestmentAndWallet(bool replacement)
        {
            var f = R1(); var p = replacement ? Replace(f) : Move(f);
            var request = replacement ? DetachedCanonicalMutationRequest.Replace(p) : DetachedCanonicalMutationRequest.Move(p);
            byte[] before = f.Session.GetCurrentBytes();
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            var failure = f.Execute(request);
            Assert.That(failure.IsSuccess, Is.False); Assert.That(failure.RuntimeProjection, Is.Null);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(1000));
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.DisableFailure(); f.Accept(f.Execute(request));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(990));
            Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.EqualTo(10));
            Assert.That(f.Execute(request).IsSuccess, Is.False);
        }

        [TestCase(false)][TestCase(true)]
        public void PaidRenovationAndSessionUndoAreExact(bool replacement)
        {
            var f = R1(); double now = 5; var service = Service(f, () => now);
            var p = replacement ? Replace(f) : Move(f);
            Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes));
            byte[] spatialBefore = CanonicalSpatialSaveSerializer.Serialize(f.State, f.Profile.Canonical).Value;
            var result = service.ExecuteCanonicalMutation(f.Runtime, replacement ? DetachedCanonicalMutationRequest.Replace(p) : DetachedCanonicalMutationRequest.Move(p));
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(990));
            Assert.That(result.Validation.Investment.Sum(r => r.RenovationMana), Is.EqualTo(10));
            Assert.That(service.RenovationUndoRemainingSeconds, Is.EqualTo(30));
            Assert.That(Encoding.UTF8.GetString(result.GetPersistedBytes()), Does.Not.Contain("\"_undo\":").And.Not.Contain("\"Started\":"));
            now = 34; result.RuntimeProjection.lastSavedUtcUnix = 987;
            var undone = service.UndoStructuralRenovation(result.RuntimeProjection);
            Assert.That(undone.IsSuccess, Is.True, undone.Reason);
            Assert.That(undone.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(1000));
            Assert.That(undone.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(987));
            CollectionAssert.AreEqual(spatialBefore, CanonicalSpatialSaveSerializer.Serialize(undone.Validation.State, f.Profile.Canonical).Value);
            Assert.That(undone.Validation.Investment.Sum(r => r.RenovationMana), Is.Zero);
            Assert.That(service.UndoStructuralRenovation(undone.RuntimeProjection).IsSuccess, Is.False);
        }

        [TestCase("expired")][TestCase("run")][TestCase("commit")][TestCase("reopen")]
        public void UndoInvalidation(string cause)
        {
            var f = R1(); double now = 0; var service = Service(f, () => now);
            var paid = service.ExecuteCanonicalMutation(f.Runtime, DetachedCanonicalMutationRequest.Move(Move(f)));
            Assert.That(paid.IsSuccess, Is.True, paid.Reason);
            if (cause == "expired") now = 30;
            if (cause == "run") service.InvalidateRenovationUndo();
            if (cause == "commit")
            {
                var replacement = service.PreviewStructuralReplacement(new StructuralReplacementRequest {
                    RoomInstanceId = paid.Validation.State.Floors[0].Layout.Rooms[0].RoomInstanceId,
                    RoomDefinitionId = "spatial.room.rectangle" });
                var next = service.ExecuteCanonicalMutation(paid.RuntimeProjection, DetachedCanonicalMutationRequest.Replace(replacement));
                Assert.That(next.IsSuccess, Is.True, next.Reason);
                // Latest renovation supersedes the previous undo; its inverse retains the first movement.
                var undo = service.UndoStructuralRenovation(next.RuntimeProjection);
                Assert.That(undo.IsSuccess, Is.True); Assert.That(undo.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(990)); return;
            }
            if (cause == "reopen") { f.Accept(paid); f.Reopen(); service = Service(f, () => now); }
            Assert.That(service.UndoStructuralRenovation(paid.RuntimeProjection).IsSuccess, Is.False);
        }

        [TestCase("spatial.room.rectangle", 10)][TestCase("spatial.room.large_chamber", 110)]
        public void ReplacementFeeAndUpgradeUseCurrentBasePrices(string definition, double expected)
        {
            var f = R1();
            bool upgrade = definition == "spatial.room.large_chamber";
            if (upgrade) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(
                Build(f, "spatial.room.rectangle", 4, 1, "north"))));
            string target = upgrade ? f.State.Floors[0].Layout.Rooms.Single(r =>
                r.RoomDefinitionId == "spatial.room.rectangle").RoomInstanceId : f.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            StructuralEditPreview Replacement(string id) => StructuralRenovationService.PreviewReplacement(f.State,
                new StructuralReplacementRequest { RoomInstanceId = target, RoomDefinitionId = id },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            var p = Replacement(definition);
            Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes));
            Assert.That(Price(f, p).Cost, Is.EqualTo(expected));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Replace(p)));
            var downgrade = Replacement(upgrade ? "spatial.room.rectangle" : "spatial.room.basic");
            Assert.That(downgrade.IsValid, Is.True, string.Join(",", downgrade.ReasonCodes));
            Assert.That(Price(f, downgrade).Cost, Is.EqualTo(definition.EndsWith("large_chamber") ? 20 : 10));
            Assert.That(Price(f, downgrade).Refund, Is.Zero);
        }

        [Test]
        public void RefundFloorsPaidInvestmentAndMovementDoesNotCompound()
        {
            var f = R1(); var c = Config(); c.Rooms.Single(r => r.DefinitionId == "spatial.room.basic").Mana = 101;
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out f.Economy), Is.True);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f))));
            Assert.That(Price(f, Delete(f)).Refund, Is.EqualTo(75));
            var first = R1(); var movement = Move(first); first.Accept(first.Execute(DetachedCanonicalMutationRequest.Move(movement)));
            var returned = StructuralRenovationService.PreviewMovement(first.State, new StructuralMovementRequest {
                RoomInstanceId = first.State.Floors[0].Layout.Rooms[0].RoomInstanceId, Anchor = new TileCoordinate(0, 2) },
                first.Production, first.Compatibility, first.Configuration, first.Profile.Canonical);
            Assert.That(Price(first, returned).Cost, Is.EqualTo(10));
            first.Accept(first.Execute(DetachedCanonicalMutationRequest.Move(returned)));
            Assert.That(Ledger(first).Sum(r => r.RenovationMana), Is.EqualTo(20));
        }

        [Test]
        public void UndoFailureRetainsCapabilityAndInjectedWindowUsesElapsedTime()
        {
            var f = R1(); var c = Config(); c.UndoSeconds = 2;
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out f.Economy), Is.True);
            double now = 100; var service = Service(f, () => now);
            var paid = service.ExecuteCanonicalMutation(f.Runtime, DetachedCanonicalMutationRequest.Move(Move(f)));
            Assert.That(paid.IsSuccess, Is.True, paid.Reason); now = 101;
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            Assert.That(service.UndoStructuralRenovation(paid.RuntimeProjection).IsSuccess, Is.False);
            CollectionAssert.AreEqual(paid.GetPersistedBytes(), f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(service.RenovationUndoRemainingSeconds, Is.EqualTo(1));
            f.FileSystem.DisableFailure(); var undo = service.UndoStructuralRenovation(paid.RuntimeProjection);
            Assert.That(undo.IsSuccess, Is.True, undo.Reason);
            Assert.That(undo.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(1000));
        }

        [Test]
        public void SchemaEightUpgradePreservesEverythingAndCreatesOnlyZeroInvestment()
        {
            var f = R1(); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(Build(f))));
            // Keep both active assignment and retained custody in the source migration fixture.
            f.State.LifecycleAndOwnership.ReturnedContents = new[] { new ReturnedStructuralContent {
                AssignmentId = "content.returned.0001", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId,
                RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody } };
            f = f.Rebase(f.State);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId)));
            f.Runtime.structureRuntime.ManaReserve = 123.5;
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            string current = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            int start = current.IndexOf(",\"structuralInvestment\":", StringComparison.Ordinal);
            int end = current.IndexOf(']', start) + 1;
            byte[] eight = Encoding.UTF8.GetBytes(current.Remove(start, end - start).Replace("\"schemaVersion\":9", "\"schemaVersion\":8"));
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(eight, f.Profile.Canonical, out byte[] nine), Is.True);
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(eight, f.Profile.Canonical, out byte[] again), Is.True);
            CollectionAssert.AreEqual(nine, again);
            string upgraded = Encoding.UTF8.GetString(nine);
            int investmentStart = upgraded.IndexOf(",\"structuralInvestment\":", StringComparison.Ordinal);
            int investmentEnd = upgraded.IndexOf(']', investmentStart) + 1;
            Assert.That(upgraded.Remove(investmentStart, investmentEnd - investmentStart)
                .Replace("\"schemaVersion\":9", "\"schemaVersion\":8"), Is.EqualTo(Encoding.UTF8.GetString(eight)));
            var restored = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(nine, f.Context);
            Assert.That(restored.IsValid, Is.True); Assert.That(restored.Investment.All(r => r.ConstructionMana == 0 && r.RenovationMana == 0), Is.True);
            CollectionAssert.AreEqual(CanonicalSpatialSaveSerializer.Serialize(f.State, f.Profile.Canonical).Value,
                CanonicalSpatialSaveSerializer.Serialize(restored.State, f.Profile.Canonical).Value);
            Assert.That(Encoding.UTF8.GetString(nine), Does.Contain("\"ManaReserve\":123.5"));
            Assert.That(DetachedCanonicalSaveSession.Open(nine, f.Context, f.Profile).IsSuccess, Is.True);
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(nine, f.Profile.Canonical, out _), Is.False);
            Assert.That(StructuralEconomyService.Preview(Delete(f), f.State, restored.Investment, 123.5, f.Economy).Refund, Is.Zero);
        }

        [TestCase("missing")][TestCase("duplicate")][TestCase("negative")][TestCase("unknown")][TestCase("order")][TestCase("numeric")]
        public void SchemaNineRejectsInvalidLedger(string corruption)
        {
            var f = R1(); string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            int start = json.IndexOf("\"structuralInvestment\":[", StringComparison.Ordinal);
            int recordStart = json.IndexOf('{', start); int recordEnd = json.IndexOf('}', recordStart) + 1;
            switch (corruption)
            {
                case "missing": json = json.Remove(recordStart, recordEnd - recordStart + 1); break;
                case "duplicate": json = json.Insert(recordEnd, "," + json.Substring(recordStart, recordEnd - recordStart)); break;
                case "negative": json = json.Replace("\"ConstructionMana\":0", "\"ConstructionMana\":-1"); break;
                case "unknown": json = json.Insert(recordEnd - 1, ",\"FuturePrice\":0"); break;
                case "order": json = json.Replace("\"ConstructionMana\":0,\"RenovationMana\":0", "\"RenovationMana\":0,\"ConstructionMana\":0"); break;
                case "numeric": json = json.Replace("\"ConstructionMana\":0", "\"ConstructionMana\":0.0"); break;
            }
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(json), f.Context).IsValid, Is.False);
        }

        [Test]
        public void EconomyLocalizationResolvesAndNeverLeaksIdentifiers()
        {
            var f = R1(); f.Runtime.structureRuntime.ManaReserve = 99;
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            var dictionary = table.entries.ToDictionary(e => e.key, e => e.text);
            foreach (string key in new[] { StructuralEconomyService.InvalidReason, StructuralEconomyService.InsufficientReason,
                StructuralEconomyService.UndoUnavailableReason, "ui.structural.economy.cost", "ui.structural.economy.refund",
                "ui.structural.economy.balance", "ui.structural.economy.affordable", "ui.structural.economy.undo.remaining",
                "ui.structural.economy.undo.action", "ui.structural.economy.undo.success" }) Assert.That(dictionary.ContainsKey(key), Is.True, key);
            string text = StructuralEconomyPresenter.Present(Price(f, Build(f)), key => dictionary[key]);
            Assert.That(text, Does.Contain(dictionary[StructuralEconomyService.InsufficientReason]));
            Assert.That(text, Does.Not.Contain("spatial.").And.Not.Contain("structural.economy.").And.Not.Contain("compat."));
            Assert.That(text, Is.EqualTo(StructuralEconomyPresenter.Present(Price(f, Build(f)), key => dictionary[key])));
        }

        [TestCase(false)][TestCase(true)]
        public void BothRunEntrypointsInvalidatePendingRenovation(bool activeLoop)
        {
            var f = R1(); var service = Service(f, () => 0);
            var paid = service.ExecuteCanonicalMutation(f.Runtime, DetachedCanonicalMutationRequest.Move(Move(f)));
            Assert.That(paid.IsSuccess, Is.True, paid.Reason);
            var go = new GameObject("PhaseFourRunUndo");
            try
            {
                var root = go.AddComponent<GameRoot>(); var content = new ContentService();
                typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(content, f.Production);
                typeof(GameRoot).GetProperty("Content").SetValue(root, content);
                typeof(GameRoot).GetProperty("Save").SetValue(root, paid.RuntimeProjection);
                typeof(GameRoot).GetProperty("SaveService").SetValue(root, service);
                typeof(GameRoot).GetField("_runSimulationService", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(root, new RunSimulationService(f.Configuration));
                Assert.That(service.RenovationUndoRemainingSeconds, Is.GreaterThan(0));
                Assert.That(activeLoop ? root.SimulateMvpActiveLoopOnce(out _) : root.SimulateRunOnce(), Is.True);
                Assert.That(service.RenovationUndoRemainingSeconds, Is.Zero);
                Assert.That(service.UndoStructuralRenovation(root.Save).IsSuccess, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void QaWalletFreshNativeSaveFillsClearsAndReopensWithoutLegacyPlacement()
        {
            var f = QaNative(); var service = Service(f, () => 0);
            var go = new GameObject("QaWalletNative");
            try
            {
                var root = QaRoot(go, f, service);
                Assert.That(f.State.Authority.CreationKind, Is.EqualTo(CanonicalSpatialCreationKind.NativeCanonical));
                Assert.That(root.Save.dungeonLayout.Slots, Is.Empty);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.Zero);
                byte[] before = service.CanonicalSession.GetCurrentBytes();
                root.SelectNextSlot();
                Assert.That(root.TryPlaceSelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out _), Is.False);
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));

                SaveData original = root.Save;
                Assert.That(root.TrySetQaManaFromDevPanel(true), Is.True);
                Assert.That(original.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(root.Save, Is.Not.SameAs(original));
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(f.Economy.ManaCapacity));
                Assert.That(root.ManaLine, Does.Contain(f.Economy.ManaCapacity.ToString("0.00")));
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString("ui.banner.qa_mana_filled", "")));
                AssertOnlyQaManaChanged(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(f.Economy.ManaCapacity));

                Assert.That(root.TrySetQaManaFromDevPanel(false), Is.True);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString("ui.banner.qa_mana_cleared", "")));
                AssertOnlyQaManaChanged(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(f.FileSystem.Paths.Any(p => p.Contains(".canonical-write-")), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [TestCase(0, 321)][TestCase(17, 321)][TestCase(444, 444)]
        public void QaWalletFillUsesInjectedValidatedCapacityAndPreservesOverCap(double initial, double expected)
        {
            var f = QaNative(initial); var c = Config(); c.ManaCapacity = 321;
            Assert.That(StructuralEconomySnapshot.TryCreate(c, f.Production.Catalog, out f.Economy), Is.True);
            var service = Service(f, () => 0); byte[] before = f.Session.GetCurrentBytes();
            var result = service.SetQaMana(f.Runtime, true);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(initial));
            Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(expected));
            AssertOnlyQaManaChanged(before, result.GetPersistedBytes());
            f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(expected));
        }

        [TestCase(false)][TestCase(true)]
        public void QaWalletFailurePreservesLiveDiskPublicationAndUndoThenSuccessfulRetryInvalidatesUndo(bool fill)
        {
            var f = R1(); var service = Service(f, () => 0);
            f.Accept(service.ExecuteCanonicalMutation(f.Runtime, DetachedCanonicalMutationRequest.Move(Move(f))));
            Assert.That(service.RenovationUndoRemainingSeconds, Is.EqualTo(f.Economy.UndoSeconds));
            var go = new GameObject("QaWalletFailure");
            try
            {
                var root = QaRoot(go, f, service); SaveData live = root.Save;
                var session = service.CanonicalSession; byte[] before = session.GetCurrentBytes();
                int publications = 0;
                service.CanonicalRuntimePublished += _ => publications++;
                f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
                Assert.That(root.TrySetQaManaFromDevPanel(fill), Is.False);
                Assert.That(root.Save, Is.SameAs(live));
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(990));
                Assert.That(publications, Is.Zero);
                Assert.That(service.CanonicalSession, Is.SameAs(session));
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(service.RenovationUndoRemainingSeconds, Is.EqualTo(f.Economy.UndoSeconds));
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(
                    Gd66MigrationReasonRegistry.PlayerLocalizationKey(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason), "")));
                Assert.That(root.BannerMessage, Does.Not.Contain("gd66.").And.Not.Contain("qa_mana"));

                f.FileSystem.DisableFailure();
                Assert.That(root.TrySetQaManaFromDevPanel(fill), Is.True);
                Assert.That(publications, Is.EqualTo(1));
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(fill ? f.Economy.ManaCapacity : 0));
                Assert.That(service.RenovationUndoRemainingSeconds, Is.Zero);
                Assert.That(service.UndoStructuralRenovation(root.Save).IsSuccess, Is.False);
                AssertOnlyQaManaChanged(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(fill ? f.Economy.ManaCapacity : 0));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [TestCase(false)][TestCase(true)]
        public void QaWalletKeepsPaidGeometryInvestmentContentsCustodyCountersHistoryAndExtensions(bool fill)
        {
            var f = Fixture.Create("\"mvpDungeonPlacements\":{\"Entries\":[{\"CategoryId\":\"placement.category.room\"," +
                "\"OptionId\":\"placement.option.room.basic\",\"Revision\":1}],\"NextRevision\":2}," +
                "\"qaExtension\":{\"evidence\":7}", "\"qaRootExtension\":[3,2,1]");
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            var construction = Build(f, x: 5, y: 2);
            Assert.That(construction.IsValid, Is.True, string.Join(",", construction.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(construction)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, f.State.Floors[0].Layout.Rooms.Single(r =>
                    r.RoomInstanceId.Contains(".room.player.")).RoomInstanceId)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId, f.State.Floors[0].Layout.Rooms.Single(r =>
                    !r.RoomInstanceId.Contains(".room.player.")).RoomInstanceId)));
            f.Runtime.totalTicks = 42; f.Runtime.runHistory.NextRunSequence = 7;
            f.Runtime.runHistory.AppendOutcome(new RunSimulationService(f.Configuration).SimulateOnce(
                f.Runtime.structureRuntime, 2, 6), 2);
            var deletion = Delete(f);
            Assert.That(deletion.IsValid, Is.True, string.Join(",", deletion.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(deletion)));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents, Is.Not.Empty);
            Assert.That(Ledger(f).Sum(r => r.ConstructionMana), Is.GreaterThan(0));
            byte[] before = f.Session.GetCurrentBytes();
            var service = Service(f, () => 0); var result = service.SetQaMana(f.Runtime, fill);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            AssertOnlyQaManaChanged(before, result.GetPersistedBytes());
            f.Reopen(); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(7));
            Assert.That(f.Runtime.runHistory.RecentOutcomes, Is.Not.Empty);
            Assert.That(f.Runtime.totalTicks, Is.EqualTo(42));
        }

        [Test]
        public void QaWalletClearDoesNotChangeSpatialFingerprintAndRetainedCommitRechecksAffordability()
        {
            var f = R1(); var service = Service(f, () => 0); var preview = Build(f);
            Assert.That(Price(f, preview).IsAffordable, Is.True);
            f.Accept(service.SetQaMana(f.Runtime, false));
            byte[] before = f.Session.GetCurrentBytes();
            var result = service.ExecuteCanonicalMutation(f.Runtime, DetachedCanonicalMutationRequest.Construct(preview));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(StructuralEconomyService.InsufficientReason));
            Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [TestCase("diagnostics")][TestCase("legacy")][TestCase("content")][TestCase("economy")]
        public void QaWalletUnavailableAuthorityFailsClosedWithoutPublication(string missing)
        {
            var f = QaNative(); var service = Service(f, () => 0);
            var go = new GameObject("QaWalletUnavailable");
            try
            {
                var root = QaRoot(go, f, service); SaveData live = root.Save;
                byte[] before = f.Session.GetCurrentBytes(); int publications = 0;
                service.CanonicalRuntimePublished += _ => publications++;
                if (missing == "diagnostics") typeof(GameRoot).GetProperty("DevPanelEnabled").SetValue(root, false);
                if (missing == "legacy") typeof(GameRoot).GetProperty("Save").SetValue(root, new SaveData());
                if (missing == "content") typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(root.Content, null);
                if (missing == "economy") service.ConfigureStructuralEconomy(null);
                Assert.That(root.TrySetQaManaFromDevPanel(true), Is.False);
                Assert.That(live.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(publications, Is.Zero);
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                string reason = missing == "economy" ? StructuralEconomyService.InvalidReason : StructuralEditService.InvalidContextReason;
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(reason, "")));
                Assert.That(root.BannerMessage, Does.Not.Contain(reason));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void QaWalletLocalizationResolvesAndExistingKpiAndLegacyPlacementRemainUnchanged()
        {
            var f = QaNative(); var service = Service(f, () => 0);
            var go = new GameObject("QaWalletDiagnostics");
            try
            {
                var root = QaRoot(go, f, service);
                foreach (string key in new[] { "ui.dev.button.qa_mana_clear", "ui.dev.button.qa_mana_fill",
                    "ui.banner.qa_mana_cleared", "ui.banner.qa_mana_filled" })
                {
                    string value = root.Content.GetString(key, "");
                    Assert.That(value, Is.Not.Empty); Assert.That(value, Does.Not.Contain("qa_mana"));
                }
                byte[] before = f.Session.GetCurrentBytes();
                typeof(GameRoot).GetProperty("Kpi").SetValue(root, new KpiService());
                root.TrackManaGenerated(10);
                Assert.That(root.Kpi.Snapshot().AverageManaPerTick, Is.EqualTo(10));
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.Zero);
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));

                var legacy = new SaveData { dungeonLayout = DungeonBuilder.M0.Gameplay.DungeonLayout.DungeonLayoutState.CreateEmpty(1, 1) };
                typeof(GameRoot).GetProperty("Save").SetValue(root, legacy);
                // Keep this legacy regression isolated from a canonical disk/session.
                root.AttachSaveServiceForTests(null);
                Assert.That(root.TryPlaceSelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out string placementKey), Is.True);
                Assert.That(placementKey, Is.EqualTo("ui.banner.place_success"));
                Assert.That(root.GetSelectedSlotStructureId(), Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
                Assert.That(root.TryPlaceSelectedStructure(StructureSimulationPass.HeatScrubberBasicId, out placementKey), Is.False);
                Assert.That(placementKey, Is.EqualTo("ui.banner.place_failed"));
                Assert.That(legacy.structureRuntime.ManaReserve, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static Fixture QaNative(double mana = 0)
        {
            var f = Fixture.Create(null);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var native = NativeCanonicalSaveCreator.Create(f.ActivePath, f.FileSystem,
                new SaveData { createdUtcUnix = 1, lastSavedUtcUnix = 1,
                    dungeonLayout = new DungeonBuilder.M0.Gameplay.DungeonLayout.DungeonLayoutState(),
                    structureRuntime = new StructureRuntimeState { ManaReserve = mana } },
                f.Compatibility, f.Production, LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile);
            Assert.That(native.IsSuccess, Is.True, native.Reason);
            f.Session = native.Session; f.State = native.Validation.State; f.Runtime = native.RuntimeProjection;
            f.Reopen(); return f;
        }

        private static GameRoot QaRoot(GameObject go, Fixture f, SaveService service)
        {
            var root = go.AddComponent<GameRoot>(); var content = new ContentService();
            const string dir = "Assets/_Project/Data/Bootstrap/";
            var assets = new[] { "content_bootstrap", "build_config", "schema_versions", "content_manifest",
                "dev_commands", "string_table_en", "heat_runtime" }.Select(name =>
                UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(dir + name + ".json")).ToArray();
            content.LoadAll(assets[0], assets[1], assets[2], assets[3], assets[4], assets[5], assets[6],
                new SimpleLogger(false), out string warning);
            Assert.That(warning, Is.Empty);
            typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(content, f.Production);
            typeof(GameRoot).GetProperty("Content").SetValue(root, content);
            typeof(GameRoot).GetProperty("Save").SetValue(root, f.Runtime);
            typeof(GameRoot).GetProperty("DevPanelEnabled").SetValue(root, true);
            root.AttachSaveServiceForTests(service); return root;
        }

        private static void AssertOnlyQaManaChanged(byte[] before, byte[] after)
        {
            var wallet = new System.Text.RegularExpressions.Regex("\"ManaReserve\":[^,}]+");
            string a = Encoding.UTF8.GetString(before), b = Encoding.UTF8.GetString(after);
            Assert.That(wallet.Matches(a).Count, Is.EqualTo(1));
            Assert.That(wallet.Matches(b).Count, Is.EqualTo(1));
            Assert.That(wallet.Replace(b, "<wallet>"), Is.EqualTo(wallet.Replace(a, "<wallet>")));
        }

        private static SaveService Service(Fixture f, Func<double> clock)
        {
            var service = new SaveService(new SimpleLogger(false), null, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                Encoding.UTF8.GetBytes(JsonUtility.ToJson(f.Configuration)));
            service.ConfigureStructuralEconomy(f.Economy, clock); service.ConfigureStructuralRemovalPolicy(f.RemovalPolicy);
            typeof(SaveService).GetProperty("SavePath").SetValue(service, f.ActivePath);
            foreach (var pair in new[] { Tuple.Create("_canonicalSession", (object)f.Session), Tuple.Create("_canonicalFileSystem", (object)f.FileSystem),
                Tuple.Create("_validationContext", (object)f.Context) })
                typeof(SaveService).GetField(pair.Item1, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, pair.Item2);
            return service;
        }
    }
}
#endif
