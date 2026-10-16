#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class ContentAcquisitionEconomyTests
    {
        private const string ConfigPath = "Assets/_Project/Resources/content_acquisition_economy.json";
        private static ContentAcquisitionEconomyConfiguration Config() =>
            JsonUtility.FromJson<ContentAcquisitionEconomyConfiguration>(File.ReadAllText(ConfigPath));
        private static Fixture Room()
        {
            var f = Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            return f;
        }
        private static string Target(Fixture f) => f.State.Floors[0].Layout.Rooms.Single().RoomInstanceId;
        private static SaveService PresentationService(Fixture f)
        {
            byte[] legacy = (byte[])typeof(DetachedCurrentTargetValidationContext).GetField("legacyConfiguration",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(f.Context);
            var service = new SaveService(new SimpleLogger(false, (level, message) => { }),
                new SaveConfig { fileName = Path.GetFileName(f.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration, legacy, f.PositionProfiles, f.Occupancy);
            service.ConfigureStructuralEconomy(f.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            service.ConfigureStructuralRemovalPolicy(f.RemovalPolicy);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, f.FileSystem, path));
            Assert.That(service.LoadOrCreate("reserve-ui-test", out string banner), Is.Not.Null, banner);
            return service;
        }

        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 24, false, false)]
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 26, false, true)]
        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, 31, false, false)]
        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, 32, false, true)]
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 32, true, false)]
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 33, true, true)]
        [TestCase(MvpDungeonPlacementIds.SpikeTrapOptionId, 20, true, false)]
        [TestCase(MvpDungeonPlacementIds.SpikeTrapOptionId, 27, true, true)]
        [TestCase(MvpDungeonPlacementIds.HiddenCacheOptionId, 10, true, false)]
        [TestCase(MvpDungeonPlacementIds.HiddenCacheOptionId, 17, true, true)]
        [TestCase(MvpDungeonPlacementIds.HiddenCacheOptionId, 5, true, false)]
        [TestCase(MvpDungeonPlacementIds.SpikeTrapOptionId, 20, false, true)]
        public void PositionedAcquisitionFeedbackAgreesWithCanonicalDraftCommit(string option, double wallet, bool existing, bool allowed)
        {
            var f = Room();
            if (existing) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, Target(f), null, new TileCoordinate(0, 0))));
            var service = PresentationService(f); f.Runtime.structureRuntime.ManaReserve = wallet;
            var context = service.DungeonDraftContext;
            var draft = TransactionalDungeonDraft.Create(f.State, context, new PhaseSevenA4TransactionalEditorTests.Store());
            var intent = context.ContentIntent(DungeonDraftContentOperation.Acquire, f.State.Floors[0].FloorInstanceId,
                Target(f), categoryId: Category(option), optionId: option, position: new TileCoordinate(2, 2));
            var spatial = draft.PreviewContent(intent); Assert.That(spatial.IsValid, Is.True, spatial.Reason);
            var quote = service.PreviewDungeonDraft(f.Runtime, draft, spatial.Candidate);
            Assert.That(quote, Is.Not.Null); Assert.That(quote.IsAffordable, Is.EqualTo(allowed), quote.Reason);
            string visible = service.PresentContentAcquisition(Category(option), option, f.Runtime, key => key, quote);
            Assert.That(visible.Contains("ui.content_acquisition.affordable"), Is.EqualTo(allowed));
            if (!allowed) Assert.That(visible, Does.Contain(quote.Reason));
            Assert.That(draft.ChangeContent(intent), Is.True);
            while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
            var committed = service.CommitDungeonDraft(f.Runtime, draft, new GateEvaluationInput(RestrictedActionType.Purchase, true, false));
            Assert.That(committed.IsSuccess, Is.EqualTo(allowed), committed.Reason);
        }

        [TestCase(false)] [TestCase(true)]
        public void ZeroAdditionalObligationQuotesRemainAffordableDuringDeficit(bool unassign)
        {
            var f = Room(); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId, Target(f), null, new TileCoordinate(0, 0))));
            var service = PresentationService(f); f.Runtime.structureRuntime.ManaReserve = 0;
            var context = service.DungeonDraftContext;
            var draft = TransactionalDungeonDraft.Create(f.State, context, new PhaseSevenA4TransactionalEditorTests.Store());
            var intent = context.ContentIntent(unassign ? DungeonDraftContentOperation.Unassign : DungeonDraftContentOperation.Reposition,
                f.State.Floors[0].FloorInstanceId, unassign ? null : Target(f), f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId,
                position: unassign ? default : new TileCoordinate(2, 2));
            var spatial = draft.PreviewContent(intent); Assert.That(spatial.IsValid, Is.True, spatial.Reason);
            var quote = service.PreviewDungeonDraft(f.Runtime, draft, spatial.Candidate);
            Assert.That(quote.IsAffordable, Is.True, quote.Reason);
            Assert.That(quote.Cost, Is.Zero); Assert.That(quote.RequiredReserveAfter, Is.EqualTo(unassign ? 0 : 6));
            Assert.That(StructuralEconomyPresenter.Present(quote, key => key), Does.Contain("ui.structural.economy.affordable"));
            Assert.That(draft.ChangeContent(intent), Is.True);
            while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
            Assert.That(service.CommitDungeonDraft(f.Runtime, draft).IsSuccess, Is.True);
        }
        private static string Category(string option)
        {
            Assert.That(MvpDungeonPlacementIds.TryGetCategoryForOption(option, out string category), Is.True);
            return category;
        }

        [TestCase(false)] [TestCase(true)]
        public void InactiveFloorAndReturnedCustodyDoNotAddDeployedReserveToAcquisitionQuote(bool returnedCustody)
        {
            var f = PhaseSixA4Tests.Eligible();
            // The owned defender is either on the inactive floor or in returned custody.
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            if (returnedCustody) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(
                f.State.Floors[1].RoomContents.Assignments.Single().AssignmentId, f.State.Floors[1].FloorInstanceId)));
            var service = PresentationService(f); f.Runtime.structureRuntime.ManaReserve = 20;
            var context = service.DungeonDraftContext;
            var draft = TransactionalDungeonDraft.Create(f.State, context, new PhaseSevenA4TransactionalEditorTests.Store());
            var floor = f.State.Floors[1];
            var intent = context.ContentIntent(DungeonDraftContentOperation.Acquire, floor.FloorInstanceId,
                floor.Layout.Rooms[0].RoomInstanceId, categoryId: MvpDungeonPlacementIds.MonsterCategoryId,
                optionId: MvpDungeonPlacementIds.GoblinOptionId, position: new TileCoordinate(2, 2));
            var spatial = draft.PreviewContent(intent); Assert.That(spatial.IsValid, Is.True, spatial.Reason);
            var quote = service.PreviewDungeonDraft(f.Runtime, draft, spatial.Candidate);
            Assert.That(quote.RequiredReserveBefore, Is.Zero); Assert.That(quote.RequiredReserveAfter, Is.Zero);
            Assert.That(quote.IsAffordable, Is.True, quote.Reason);
            Assert.That(service.PresentContentAcquisition(intent.CategoryId, intent.OptionId, f.Runtime, key => key, quote),
                Does.Contain("ui.content_acquisition.affordable"));
            Assert.That(draft.ChangeContent(intent), Is.True);
            while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True);
            Assert.That(service.CommitDungeonDraft(f.Runtime, draft, new GateEvaluationInput(RestrictedActionType.Purchase, true, false)).IsSuccess, Is.True);
        }
        private static string[] Investment(Fixture f) => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
            f.Session.GetCurrentBytes(), f.Context).Investment.Select(JsonUtility.ToJson).ToArray();
        private static void Unchanged(Fixture f, DetachedCanonicalMutationRequest request, string reason = null)
        {
            byte[] bytes = f.Session.GetCurrentBytes();
            string runtime = JsonUtility.ToJson(f.Runtime), state = JsonUtility.ToJson(f.State);
            var session = f.Session; var originalRuntime = f.Runtime;
            var result = f.Execute(request);
            Assert.That(result.IsSuccess, Is.False);
            if (reason != null) Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.RuntimeProjection, Is.Null); Assert.That(result.Session, Is.Null);
            Assert.That(f.Session, Is.SameAs(session)); Assert.That(f.Runtime, Is.SameAs(originalRuntime));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
            Assert.That(JsonUtility.ToJson(f.State), Is.EqualTo(state));
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
        }

        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, 25)]
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 20)]
        [TestCase(MvpDungeonPlacementIds.SpikeTrapOptionId, 20)]
        [TestCase(MvpDungeonPlacementIds.SnareTrapOptionId, 15)]
        [TestCase(MvpDungeonPlacementIds.ChillingSigilOptionId, 20)]
        [TestCase(MvpDungeonPlacementIds.BasicLootNodeOptionId, 15)]
        [TestCase(MvpDungeonPlacementIds.HiddenCacheOptionId, 10)]
        [TestCase(MvpDungeonPlacementIds.GlitteringHoardOptionId, 25)]
        public void ProductionPricePurchasesAtomicallyAndSurvivesReopen(string option, double approved)
        {
            var f = Room();
            Assert.That(f.Acquisition.StartingMana, Is.EqualTo(40));
            Assert.That(f.Acquisition.OrderedOptionIds.Count, Is.EqualTo(8));
            Assert.That(f.Acquisition.TryPrice(Category(option), option, out double price), Is.True);
            Assert.That(price, Is.EqualTo(approved));
            double before = f.Runtime.structureRuntime.ManaReserve;
            string[] ledger = Investment(f);
            var request = DetachedCanonicalMutationRequest.Place(Category(option), option, Target(f));
            var oldSession = f.Session; var oldRuntime = f.Runtime; var oldState = f.State;
            var result = f.Execute(request);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(oldRuntime.structureRuntime.ManaReserve, Is.EqualTo(before));
            Assert.That(oldState.Floors[0].RoomContents.Assignments, Is.Empty);
            Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(before - price));
            f.Accept(result); f.Reopen();
            var assigned = f.State.Floors[0].RoomContents.Assignments.Single();
            Assert.That(assigned.OptionId, Is.EqualTo(option)); Assert.That(assigned.CategoryId, Is.EqualTo(Category(option)));
            Assert.That(assigned.AssignmentId, Is.Not.Empty);
            Assert.That(f.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(assigned.Sequence + 1));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(before - price));
            CollectionAssert.AreEqual(ledger, Investment(f));
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
            byte[] durable = f.Session.GetCurrentBytes();
            var stale = f.Authority.Execute(f.ActivePath, f.FileSystem, oldSession, oldState, oldRuntime, request);
            Assert.That(stale.IsSuccess, Is.False); Assert.That(stale.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(durable, f.FileSystem.ReadAllBytes(f.ActivePath));
            {
                var second = DetachedCanonicalMutationRequest.Place(Category(option), option, Target(f), null,
                    new TileCoordinate(3, 1));
                f.Accept(f.Execute(second));
                Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(before - 2 * price));
                Assert.That(f.State.Floors[0].RoomContents.Assignments.Length, Is.EqualTo(2));
                Unchanged(f, DetachedCanonicalMutationRequest.Place(Category(option), option, Target(f), null,
                    new TileCoordinate(3, 2)), DetachedSpatialMigrationPreparer.CapacityReason);
            }
        }

        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, MvpDungeonPlacementIds.SkeletonOptionId)]
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, MvpDungeonPlacementIds.GoblinOptionId)]
        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, MvpDungeonPlacementIds.GoblinOptionId)]
        public void BasicRoomMonsterInstancesPurchaseToCapacityAndReopenInCanonicalOrder(string firstOption, string secondOption)
        {
            var f = Room();
            f.Runtime.structureRuntime.ManaReserve = 100; // Explicit test wallet; production prices remain configured.
            long next = f.State.Floors[0].RoomContents.NextSequence;
            string[] investment = Investment(f);
            string custody = JsonUtility.ToJson(f.State.LifecycleAndOwnership);
            var oldSession = f.Session; var oldState = f.State; var oldRuntime = f.Runtime;
            var first = DetachedCanonicalMutationRequest.Place(Category(firstOption), firstOption, Target(f), null,
                new TileCoordinate(1, 1));
            f.Accept(f.Execute(first));
            Assert.That(f.Acquisition.TryPrice(Category(firstOption), firstOption, out double firstPrice), Is.True);
            Assert.That(firstPrice, Is.EqualTo(firstOption == MvpDungeonPlacementIds.SkeletonOptionId ? 25 : 20));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(100 - firstPrice));
            Assert.That(f.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(next + 1));
            var firstAssignment = f.State.Floors[0].RoomContents.Assignments.Single();
            byte[] afterFirst = f.Session.GetCurrentBytes();
            string afterRuntime = JsonUtility.ToJson(f.Runtime), afterState = JsonUtility.ToJson(f.State);
            var stale = f.Authority.Execute(f.ActivePath, f.FileSystem, oldSession, oldState, oldRuntime, first);
            Assert.That(stale.IsSuccess, Is.False); Assert.That(stale.RuntimeProjection, Is.Null); Assert.That(stale.Session, Is.Null);
            Assert.That(oldRuntime.structureRuntime.ManaReserve, Is.EqualTo(100));
            Assert.That(oldState.Floors[0].RoomContents.Assignments, Is.Empty);
            Assert.That(oldState.Floors[0].RoomContents.NextSequence, Is.EqualTo(next));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(afterRuntime));
            Assert.That(JsonUtility.ToJson(f.State), Is.EqualTo(afterState));
            CollectionAssert.AreEqual(afterFirst, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(afterFirst, f.Session.GetCurrentBytes());

            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(Category(secondOption), secondOption,
                Target(f), null, new TileCoordinate(2, 1))));
            Assert.That(f.Acquisition.TryPrice(Category(secondOption), secondOption, out double secondPrice), Is.True);
            Assert.That(secondPrice, Is.EqualTo(secondOption == MvpDungeonPlacementIds.SkeletonOptionId ? 25 : 20));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(100 - firstPrice - secondPrice));
            Assert.That(f.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(next + 2));
            byte[] durable = f.Session.GetCurrentBytes();
            string[] ids = f.State.Floors[0].RoomContents.Assignments.Select(a => a.AssignmentId).ToArray();
            Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2));
            Assert.That(ids[0], Is.EqualTo(firstAssignment.AssignmentId));
            f.Reopen();
            var assignments = f.State.Floors[0].RoomContents.Assignments;
            CollectionAssert.AreEqual(ids, assignments.Select(a => a.AssignmentId).ToArray());
            CollectionAssert.AreEqual(new[] { firstOption, secondOption }, assignments.Select(a => a.OptionId).ToArray());
            CollectionAssert.AreEqual(new[] { next, next + 1 }, assignments.Select(a => a.Sequence).ToArray());
            CollectionAssert.AreEqual(durable, f.Session.GetCurrentBytes());
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(100 - firstPrice - secondPrice));
            var route = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production);
            CollectionAssert.AreEqual(new[] { firstOption, secondOption }, route.Rooms.Single().AssignedMonsterOptionIds);
            var runInputs = route.Rooms.Single().ToOrderedPlacements();
            CollectionAssert.AreEqual(new[] { firstOption, secondOption }, runInputs.Where(p =>
                p.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).Select(p => p.OptionId).ToArray());
            var effects = MvpPlacementEffectsResolver.ResolvePlacements(runInputs, f.Configuration);
            CollectionAssert.AreEqual(new[] { firstOption, secondOption }, effects.ContributingOptionIds.Where(id =>
                Category(id) == MvpDungeonPlacementIds.MonsterCategoryId).ToArray());
            CollectionAssert.AreEqual(investment, Investment(f));
            Assert.That(JsonUtility.ToJson(f.State.LifecycleAndOwnership), Is.EqualTo(custody));
            Unchanged(f, first, DetachedSpatialMigrationPreparer.CapacityReason);
            Unchanged(f, DetachedCanonicalMutationRequest.Place(Category(secondOption), secondOption, Target(f),
                    null, new TileCoordinate(3, 1)),
                DetachedSpatialMigrationPreparer.CapacityReason);
        }

        [TestCase("missing")] [TestCase("duplicate")] [TestCase("unknown")] [TestCase("extra")]
        [TestCase("blank")] [TestCase("category")] [TestCase("negative")]
        [TestCase("nan")] [TestCase("infinite")] [TestCase("starting_negative")]
        [TestCase("starting_nan")] [TestCase("starting_infinite")] [TestCase("starting_over_capacity")]
        public void InvalidConfigurationFailsClosed(string kind)
        {
            var f = Fixture.Create(null); var c = Config();
            if (kind == "missing") c.Prices = c.Prices.Skip(1).ToArray();
            if (kind == "duplicate") c.Prices[1].OptionId = c.Prices[0].OptionId;
            if (kind == "unknown") c.Prices[0].OptionId = "test.unsupported";
            if (kind == "extra") c.Prices = c.Prices.Concat(new[] { new ContentAcquisitionPrice {
                OptionId = MvpDungeonPlacementIds.BasicRoomOptionId, CategoryId = MvpDungeonPlacementIds.RoomCategoryId, Mana = 0 } }).ToArray();
            if (kind == "blank") c.Prices[0].OptionId = " ";
            if (kind == "category") c.Prices[0].CategoryId = MvpDungeonPlacementIds.MonsterCategoryId;
            if (kind == "negative") c.Prices[0].Mana = -1;
            if (kind == "nan") c.Prices[0].Mana = double.NaN;
            if (kind == "infinite") c.Prices[0].Mana = double.PositiveInfinity;
            if (kind == "starting_negative") c.StartingMana = -1;
            if (kind == "starting_nan") c.StartingMana = double.NaN;
            if (kind == "starting_infinite") c.StartingMana = double.PositiveInfinity;
            if (kind == "starting_over_capacity") c.StartingMana = f.Economy.ManaCapacity + 1;
            Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(c, f.Economy, f.Profile.Canonical, out var invalid), Is.False);
            Assert.That(invalid, Is.Null);
        }

        [TestCase("{}")] [TestCase("{\"StartingMana\":40,\"Prices\":[]}")]
        [TestCase("{\"StartingMana\":NaN,\"Prices\":[]}")] [TestCase("{broken")]
        public void MalformedOrIncompleteJsonFails(string json)
        {
            var f = Fixture.Create(null);
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(Encoding.UTF8.GetBytes(json), f.Economy,
                f.Profile.Canonical, out _), Is.False);
        }

        [TestCase("string_price")] [TestCase("missing_price")] [TestCase("extra_field")]
        [TestCase("duplicate_field")] [TestCase("overflow")]
        public void MalformedPriceRecordsFailBeforeDeserialization(string kind)
        {
            var f = Fixture.Create(null); string json = File.ReadAllText(ConfigPath);
            if (kind == "string_price") json = json.Replace("\"Mana\": 15", "\"Mana\": \"15\"");
            if (kind == "missing_price") json = json.Replace(", \"Mana\": 15", "");
            if (kind == "extra_field") json = json.Replace("\"StartingMana\": 40", "\"Unexpected\": 0, \"StartingMana\": 40");
            if (kind == "duplicate_field") json = json.Replace("\"StartingMana\": 40", "\"StartingMana\": 40, \"StartingMana\": 40");
            if (kind == "overflow") json = json.Replace("\"StartingMana\": 40", "\"StartingMana\": 1e999");
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(Encoding.UTF8.GetBytes(json), f.Economy, f.Profile.Canonical, out _), Is.False);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ConfigurationEnforcesExplicitSerializedBudgets(int dimension)
        {
            var f = Fixture.Create(null); var existing = f.Profile.Canonical.Serialized;
            byte[] bytes = File.ReadAllBytes(ConfigPath);
            var restricted = new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(
                dimension == 0 ? bytes.Length - 1 : existing.MaximumInputBytes,
                dimension == 1 ? 1 : existing.MaximumParsedNodes,
                dimension == 2 ? 7 : existing.MaximumCollectionRecords,
                dimension == 3 ? 1 : existing.MaximumStringCharacters, existing.MaximumDiagnostics), f.Profile.Canonical.Spatial);
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(bytes, f.Economy, restricted, out _), Is.False);
            if (dimension == 2) Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(Config(), f.Economy, restricted, out _), Is.False);
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(bytes, f.Economy, f.Profile.Canonical, out _), Is.True);
        }

        [Test]
        public void MissingConfigurationPreventsFreshCreationWithoutWritingOrMutatingInputs()
        {
            var f = Fixture.Create(null); f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string before = JsonUtility.ToJson(f.Runtime);
            var failed = NativeCanonicalSaveCreator.Create(f.ActivePath, f.FileSystem, f.Runtime, f.Compatibility,
                f.Production, LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile,
                null, f.Occupancy);
            Assert.That(failed.Reason, Is.EqualTo(ContentAcquisitionEconomySnapshot.InvalidReason));
            Assert.That(failed.IsSuccess, Is.False); Assert.That(f.FileSystem.Exists(f.ActivePath), Is.False);
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(before));
        }

        [Test]
        public void SnapshotIsImmutableAndOrdinalAndUsesInjectedPrices()
        {
            var f = Room(); var c = Config();
            c.Prices = c.Prices.Reverse().ToArray();
            c.Prices.Single(p => p.OptionId == MvpDungeonPlacementIds.SkeletonOptionId).Mana = 7; // Test-only tuning.
            Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(c, f.Economy, f.Profile.Canonical, out f.Acquisition), Is.True);
            string[] ordered = f.Acquisition.OrderedOptionIds.ToArray();
            CollectionAssert.AreEqual(ordered.OrderBy(id => id, StringComparer.Ordinal), ordered);
            c.Prices.Single(p => p.OptionId == MvpDungeonPlacementIds.SkeletonOptionId).Mana = 999;
            f.Runtime.structureRuntime.ManaReserve = 14; // Injected price plus configured readiness reserve.
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, Target(f))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(7));
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(File.ReadAllBytes(ConfigPath), f.Economy,
                default, out _), Is.False);
            Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(Config(), null, f.Profile.Canonical, out _), Is.False);
        }

        [TestCase(32, true)] [TestCase(31, false)] [TestCase(25, false)] [TestCase(24, false)] [TestCase(0, false)]
        public void ExactBalanceSucceedsAndInsufficientBalanceChangesNothing(double balance, bool success)
        {
            var f = Room(); f.Runtime.structureRuntime.ManaReserve = balance;
            var request = DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, Target(f));
            if (!success) { Unchanged(f, request, balance < 25 ? ContentAcquisitionEconomySnapshot.InsufficientReason : MonsterRecoveryRules.Insufficient); return; }
            f.Accept(f.Execute(request)); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(7));
        }

        [Test]
        public void UnpositionedPriceDoesNotClaimAffordabilityAndCommitRechecksMana()
        {
            var f = Room(); f.Runtime.structureRuntime.ManaReserve = 25;
            string preview = ContentAcquisitionEconomyPresenter.Present(f.Acquisition, MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, f.Runtime.structureRuntime.ManaReserve, key => key);
            Assert.That(preview, Does.Not.Contain("ui.content_acquisition.affordable"));
            f.Runtime.structureRuntime.ManaReserve = 24; // Authoritative current wallet changed after preview.
            Unchanged(f, DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, Target(f)), ContentAcquisitionEconomySnapshot.InsufficientReason);
        }

        [TestCase("target")] [TestCase("option")] [TestCase("category")]
        [TestCase("config")] [TestCase("unavailable")] [TestCase("persistence")]
        [TestCase("stale_state")] [TestCase("snapshot")]
        public void RejectedPurchaseNeverChargesOrChangesAnyAuthority(string kind)
        {
            var f = Room(); string option = MvpDungeonPlacementIds.SkeletonOptionId;
            string category = MvpDungeonPlacementIds.MonsterCategoryId; string target = Target(f);
            if (kind == "target") target = "test.missing.room";
            if (kind == "option") option = "test.missing.option";
            if (kind == "category") category = MvpDungeonPlacementIds.TrapCategoryId;
            if (kind == "config") f.Acquisition = null;
            if (kind == "unavailable") f.Configuration.MvpPlacementEffects = f.Configuration.MvpPlacementEffects.Where(e => e.OptionId != option).ToArray();
            if (kind == "persistence") f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            if (kind == "stale_state") f.State.Floors[0].RoomContents.NextSequence++;
            if (kind == "snapshot") f.Runtime.structureRuntime.Heat = double.NaN;
            Unchanged(f, DetachedCanonicalMutationRequest.Place(category, option, target));
        }

        [Test]
        public void FullCategoryAndOverlapNeverCharge()
        {
            var f = Room();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, Target(f))));
            Unchanged(f, DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SpikeTrapOptionId, Target(f)), DetachedCanonicalSpatialMutation.PositionInvalidReason);
            Assert.That(CanonicalRoomCapacityResolver.TryResolve(f.Production, f.State.Floors[0].Layout.Rooms[0].RoomDefinitionId,
                out var capacity, out _), Is.True);
            var state = f.State;
            state.Floors[0].RoomContents.Assignments = Enumerable.Range(0, capacity.TrapCapacity).Select(i =>
                new RoomContentAssignment { AssignmentId = "test.full.trap." + i, RoomInstanceId = Target(f),
                    CategoryId = MvpDungeonPlacementIds.TrapCategoryId,
                    OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId, Sequence = i,
                    RoomLocalPosition = new TileCoordinate(i, 0) }).ToArray();
            state.Floors[0].RoomContents.NextSequence = capacity.TrapCapacity;
            f = f.Rebase(state); // Explicit test-only full-capacity fixture.
            Unchanged(f, DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SnareTrapOptionId, Target(f)), DetachedSpatialMigrationPreparer.CapacityReason);
        }

        [Test]
        public void OwnedReuseIsFreeAndNewAcquisitionKeepsCustodyAndStructuralBasis()
        {
            var f = ReturnedContentRedeploymentTests.Owned();
            var owned = f.State.LifecycleAndOwnership.ReturnedContents.First(i => i.OptionId == MvpDungeonPlacementIds.SkeletonOptionId);
            string[] ledger = Investment(f); int count = f.State.LifecycleAndOwnership.ReturnedContents.Length;
            f.Runtime.structureRuntime.ManaReserve = 39; // Price plus both individual reserve obligations.
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(owned.CategoryId, owned.OptionId, Target(f))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(14));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(count));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId, Is.Not.EqualTo(owned.AssignmentId));
            CollectionAssert.AreEqual(ledger, Investment(f));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(owned.AssignmentId, Target(f), null,
                new TileCoordinate(2, 1))));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(14));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Count(a => a.OptionId == owned.OptionId), Is.EqualTo(2));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Any(a => a.AssignmentId == owned.AssignmentId), Is.True);
            Unchanged(f, DetachedCanonicalMutationRequest.Redeploy(owned.AssignmentId, Target(f)),
                DetachedCanonicalSpatialMutation.ReturnedItemMissingReason);
            var trap = f.State.LifecycleAndOwnership.ReturnedContents.Single(i => i.CategoryId == MvpDungeonPlacementIds.TrapCategoryId);
            Unchanged(f, DetachedCanonicalMutationRequest.Redeploy(trap.AssignmentId, "test.missing.room"));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(trap.AssignmentId, Target(f), null,
                new TileCoordinate(1, 2)))); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(14));
            var assigned = f.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == trap.AssignmentId);
            Assert.That(assigned.CategoryId, Is.EqualTo(trap.CategoryId)); Assert.That(assigned.OptionId, Is.EqualTo(trap.OptionId));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(count - 2));
            CollectionAssert.AreEqual(ledger, Investment(f));
            Unchanged(f, DetachedCanonicalMutationRequest.Redeploy(trap.AssignmentId, Target(f)), DetachedCanonicalSpatialMutation.ReturnedItemMissingReason);
        }

        private static Fixture Native()
        {
            var f = Fixture.Create(null);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var recognized = new SaveData { contentVersion = "test", createdUtcUnix = 1, lastSavedUtcUnix = 1,
                structureRuntime = new StructureRuntimeState { ManaReserve = 123 } };
            var result = NativeCanonicalSaveCreator.Create(f.ActivePath, f.FileSystem, recognized,
                f.Compatibility, f.Production, LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration),
                f.Profile, f.Acquisition, f.Occupancy);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(recognized.structureRuntime.ManaReserve, Is.EqualTo(123));
            f.Session = result.Session; f.State = result.Validation.State; f.Runtime = result.RuntimeProjection;
            f.Reopen(); return f;
        }

        [Test]
        public void FreshNativeSaveStartsAtConfiguredManaAndSchemaNineAndCanBuyStarterPackage()
        {
            var f = Native();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(40));
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(15));
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(15));
            Assert.That(Encoding.UTF8.GetString(f.Session.GetCurrentBytes()), Does.Contain("\"schemaVersion\":15"));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId)));
            Unchanged(f, DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.BasicLootNodeOptionId, Target(f)), MonsterRecoveryRules.Insufficient);
            f.Runtime.structureRuntime.ManaReserve += 7; // Explicit test-only funding; starting mana and prices remain unchanged.
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, Target(f))));
            f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(7));
            Assert.That(Investment(f).Length, Is.GreaterThan(0));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment.All(i => i.ConstructionMana == 0), Is.True);
        }

        [Test]
        public void FreshCreationUsesInjectedStartingManaWhileMigrationRetainsItsOriginalBalance()
        {
            var f = Fixture.Create(null);
            var migrated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(migrated, f.Production, out var legacyRuntime, out _), Is.True);
            Assert.That(legacyRuntime.structureRuntime.ManaReserve, Is.Zero); // Empty schema-6 fixture; no acquisition charge or starting grant.
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var injected = PhaseFourTestSupport.Acquisition(f.Economy, f.Profile.Canonical, 17); // Test-owned starting tuning.
            var fresh = NativeCanonicalSaveCreator.Create(f.ActivePath, f.FileSystem, legacyRuntime, f.Compatibility,
                f.Production, LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile,
                injected, f.Occupancy);
            Assert.That(fresh.IsSuccess, Is.True, fresh.Reason);
            Assert.That(fresh.RuntimeProjection.structureRuntime.ManaReserve, Is.EqualTo(17));
            Assert.That(legacyRuntime.structureRuntime.ManaReserve, Is.Zero);
        }

        [TestCase(0)] [TestCase(37)]
        public void ExistingSchemaNineLoadNeverGrantsStartingMana(double balance)
        {
            var f = Native(); f.Runtime.structureRuntime.ManaReserve = balance;
            var result = f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime);
            f.Accept(result); byte[] before = f.Session.GetCurrentBytes();
            var service = new SaveService(new SimpleLogger(false), null, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration),
                f.PositionProfiles, f.Occupancy);
            service.ConfigureStructuralEconomy(f.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            typeof(SaveService).GetProperty("SavePath").SetValue(service, f.ActivePath);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, f.FileSystem, path));
            var loaded = service.LoadOrCreate("test", out string banner);
            Assert.That(loaded, Is.Not.Null, banner); Assert.That(loaded.structureRuntime.ManaReserve, Is.EqualTo(balance));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void AllAcquisitionLocalizationResolvesAndAffordabilityIsVisible()
        {
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            Func<string, string> text = key => table.entries.Single(e => e.key == key).text;
            var f = Room();
            foreach (string key in new[] { ContentAcquisitionEconomySnapshot.InvalidReason,
                ContentAcquisitionEconomySnapshot.InsufficientReason, "ui.content_acquisition.cost", "ui.content_acquisition.affordable" })
                Assert.That(text(key), Is.Not.Empty);
            string visible = ContentAcquisitionEconomyPresenter.Present(f.Acquisition, MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, 24, text);
            Assert.That(visible, Does.Not.Contain(text("ui.content_acquisition.affordable")));
            Assert.That(visible, Does.Contain("25").And.Contain("24"));
        }
    }
}
#endif
