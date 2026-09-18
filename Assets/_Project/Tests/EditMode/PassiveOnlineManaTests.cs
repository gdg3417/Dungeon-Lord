#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PassiveOnlineManaTests
    {
        private sealed class FakeTimeSource : ITimeSource
        {
            public long Now = 1000;
            public long UtcNowUnixSeconds() => Now;
        }

        private static byte[] ProductionBytes() =>
            File.ReadAllBytes("Assets/_Project/Resources/passive_online_mana.json");

        private static ContentBootstrap Bootstrap() => JsonUtility.FromJson<ContentBootstrap>(
            File.ReadAllText("Assets/_Project/Data/Bootstrap/content_bootstrap.json"));

        private static PassiveOnlineManaConfigurationSnapshot Configuration(Fixture fixture) =>
            PhaseFourTestSupport.PassiveMana(fixture.Profile.Canonical);

        private static CanonicalPassiveManaService Service(Fixture fixture)
        {
            ContentBootstrap bootstrap = Bootstrap();
            return new CanonicalPassiveManaService(Configuration(fixture), fixture.Economy,
                new FormulaEngine(), fixture.Profile.Canonical.Spatial, bootstrap.tickSeconds);
        }

        [Test]
        public void ProductionConfigurationOwnsApprovedMvpInputsAndDisabledSoftCap()
        {
            Fixture fixture = Fixture.Create(null);
            PassiveOnlineManaConfigurationLoadResult loaded =
                PassiveOnlineManaConfigurationSnapshot.Load(ProductionBytes(), fixture.Profile.Canonical);

            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            Assert.That(loaded.Value.MvpBaselineCoreLevel, Is.EqualTo(1));
            Assert.That(loaded.Value.ManaPerCoreLevelPerMinute, Is.EqualTo(2d));
            Assert.That(loaded.Value.ManaPerActiveFloorPerMinute, Is.EqualTo(1d));
            Assert.That(loaded.Value.BaseOfflineEfficiency, Is.EqualTo(0.15d));
            Assert.That(loaded.Value.TryGetHeatEfficiency(CurrentHeatTierResolver.PeaceTierId,
                out double peace), Is.True);
            Assert.That(loaded.Value.TryGetHeatEfficiency(CurrentHeatTierResolver.NoticeTierId,
                out double notice), Is.True);
            Assert.That(loaded.Value.TryGetHeatEfficiency(CurrentHeatTierResolver.ConcernTierId,
                out double concern), Is.True);
            Assert.That(peace, Is.EqualTo(1d));
            Assert.That(notice, Is.EqualTo(0.95d));
            Assert.That(concern, Is.EqualTo(0.85d));
            Assert.That(loaded.Value.SoftCapEnabled, Is.False);
            Assert.That(loaded.Value.SoftCapStartManaPerHour.HasValue, Is.False);
            Assert.That(loaded.Value.SoftCapSlopeManaPerHour.HasValue, Is.False);
        }

        [TestCase("missing")]
        [TestCase("malformed")]
        [TestCase("schema")]
        [TestCase("version")]
        [TestCase("core_zero")]
        [TestCase("core_negative")]
        [TestCase("core_fractional")]
        [TestCase("coefficient_negative")]
        [TestCase("coefficient_infinite")]
        [TestCase("floor_nan")]
        [TestCase("offline_missing")]
        [TestCase("offline_zero")]
        [TestCase("offline_over_one")]
        [TestCase("offline_infinite")]
        [TestCase("heat_missing")]
        [TestCase("heat_duplicate")]
        [TestCase("heat_order")]
        [TestCase("heat_negative")]
        [TestCase("heat_infinite")]
        [TestCase("disabled_parameters")]
        [TestCase("enabled_missing_parameters")]
        [TestCase("enabled_negative_start")]
        [TestCase("enabled_zero_slope")]
        [TestCase("extra_field")]
        public void InvalidConfigurationFailsClosed(string scenario)
        {
            Fixture fixture = Fixture.Create(null);
            string json = NormalizeLineEndings(Encoding.UTF8.GetString(ProductionBytes()));
            switch (scenario)
            {
                case "missing":
                    Assert.That(PassiveOnlineManaConfigurationSnapshot.Load(null,
                        fixture.Profile.Canonical).Error,
                        Is.EqualTo(PassiveOnlineManaConfigurationError.Missing));
                    return;
                case "malformed": json = "{broken"; break;
                case "schema": json = json.Replace("passive_online_mana\"", "wrong\""); break;
                case "version": json = json.Replace("\"SchemaVersion\": 2", "\"SchemaVersion\": 3"); break;
                case "core_zero": json = json.Replace("\"MvpBaselineCoreLevel\": 1", "\"MvpBaselineCoreLevel\": 0"); break;
                case "core_negative": json = json.Replace("\"MvpBaselineCoreLevel\": 1", "\"MvpBaselineCoreLevel\": -1"); break;
                case "core_fractional": json = json.Replace("\"MvpBaselineCoreLevel\": 1", "\"MvpBaselineCoreLevel\": 1.5"); break;
                case "coefficient_negative": json = json.Replace("\"ManaPerCoreLevelPerMinute\": 2", "\"ManaPerCoreLevelPerMinute\": -2"); break;
                case "coefficient_infinite": json = json.Replace("\"ManaPerCoreLevelPerMinute\": 2", "\"ManaPerCoreLevelPerMinute\": 1e999"); break;
                case "floor_nan": json = json.Replace("\"ManaPerActiveFloorPerMinute\": 1", "\"ManaPerActiveFloorPerMinute\": NaN"); break;
                case "offline_missing":
                    json = ReplaceFixtureFragment(json,
                        "  \"BaseOfflineEfficiency\": 0.15,\n",
                        string.Empty);
                    break;
                case "offline_zero":
                    json = ReplaceFixtureFragment(json,
                        "\"BaseOfflineEfficiency\": 0.15",
                        "\"BaseOfflineEfficiency\": 0");
                    break;
                case "offline_over_one":
                    json = ReplaceFixtureFragment(json,
                        "\"BaseOfflineEfficiency\": 0.15",
                        "\"BaseOfflineEfficiency\": 1.01");
                    break;
                case "offline_infinite":
                    json = ReplaceFixtureFragment(json,
                        "\"BaseOfflineEfficiency\": 0.15",
                        "\"BaseOfflineEfficiency\": 1e999");
                    break;
                case "heat_missing":
                    json = ReplaceFixtureFragment(json,
                        "    { \"HeatTierId\": \"heat_tier.notice\", \"Multiplier\": 0.95 },\n",
                        string.Empty);
                    break;
                case "heat_duplicate": json = json.Replace("heat_tier.notice", "heat_tier.concern"); break;
                case "heat_order":
                    json = ReplaceFixtureFragment(json,
                        "    { \"HeatTierId\": \"heat_tier.concern\", \"Multiplier\": 0.85 },\n    { \"HeatTierId\": \"heat_tier.notice\", \"Multiplier\": 0.95 },",
                        "    { \"HeatTierId\": \"heat_tier.notice\", \"Multiplier\": 0.95 },\n    { \"HeatTierId\": \"heat_tier.concern\", \"Multiplier\": 0.85 },");
                    break;
                case "heat_negative": json = json.Replace("\"Multiplier\": 0.85", "\"Multiplier\": -0.85"); break;
                case "heat_infinite": json = json.Replace("\"Multiplier\": 0.85", "\"Multiplier\": 1e999"); break;
                case "disabled_parameters": json = json.Replace("\"Enabled\": false", "\"Enabled\": false, \"StartManaPerHour\": 100, \"SlopeManaPerHour\": 50"); break;
                case "enabled_missing_parameters": json = json.Replace("\"Enabled\": false", "\"Enabled\": true"); break;
                case "enabled_negative_start": json = json.Replace("\"Enabled\": false", "\"Enabled\": true, \"StartManaPerHour\": -1, \"SlopeManaPerHour\": 50"); break;
                case "enabled_zero_slope": json = json.Replace("\"Enabled\": false", "\"Enabled\": true, \"StartManaPerHour\": 100, \"SlopeManaPerHour\": 0"); break;
                case "extra_field": json = json.Replace("\"Schema\":", "\"Unexpected\": 1,\n  \"Schema\":"); break;
            }

            PassiveOnlineManaConfigurationLoadResult loaded =
                PassiveOnlineManaConfigurationSnapshot.Load(Encoding.UTF8.GetBytes(json),
                    fixture.Profile.Canonical);
            Assert.That(loaded.IsSuccess, Is.False);
            Assert.That(loaded.Value, Is.Null);
            Assert.That(loaded.Error, Is.EqualTo(scenario == "version"
                ? PassiveOnlineManaConfigurationError.UnsupportedVersion
                : PassiveOnlineManaConfigurationError.Malformed));
        }

        private static string NormalizeLineEndings(string value) =>
            value.Replace("\r\n", "\n").Replace("\r", "\n");

        private static string ReplaceFixtureFragment(string value, string oldValue, string newValue)
        {
            Assert.That(value, Does.Contain(oldValue), "Fixture mutation source was not found.");
            string mutated = value.Replace(oldValue, newValue);
            Assert.That(mutated, Is.Not.EqualTo(value), "Fixture mutation did not change the source.");
            return mutated;
        }

        [Test]
        public void EnabledSoftCapRequiresAndUsesAuthoredParameters()
        {
            Fixture fixture = OneFloorFixture();
            string json = Encoding.UTF8.GetString(ProductionBytes()).Replace(
                "\"Enabled\": false",
                "\"Enabled\": true, \"StartManaPerHour\": 100, \"SlopeManaPerHour\": 50");
            PassiveOnlineManaConfigurationLoadResult loaded =
                PassiveOnlineManaConfigurationSnapshot.Load(Encoding.UTF8.GetBytes(json),
                    fixture.Profile.Canonical);
            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            Assert.That(loaded.Value.SoftCapEnabled, Is.True);
            Assert.That(loaded.Value.SoftCapStartManaPerHour, Is.EqualTo(100d));
            Assert.That(loaded.Value.SoftCapSlopeManaPerHour, Is.EqualTo(50d));

            var service = new CanonicalPassiveManaService(loaded.Value, fixture.Economy,
                new FormulaEngine(), fixture.Profile.Canonical.Spatial, Bootstrap().tickSeconds);
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            PassiveManaRateSummary rate = service.ResolveRate(fixture.Runtime, fixture.Configuration);
            Assert.That(rate.RuleResolved, Is.True);
            Assert.That(rate.AfterClampAndSoftCapManaPerHour, Is.LessThan(rate.AfterEventOrSeasonManaPerHour));
        }

        [TestCase(CurrentHeatTierResolver.PeaceTierId, 180d)]
        [TestCase(CurrentHeatTierResolver.NoticeTierId, 171d)]
        [TestCase(CurrentHeatTierResolver.ConcernTierId, 153d)]
        public void ApprovedOneFloorMvpRatesResolvePerHeatTier(string tierId, double expected)
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.Heat = HeatFor(fixture.Configuration, tierId);

            PassiveManaRateSummary rate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);

            Assert.That(rate.RuleResolved, Is.True, rate.Error.ToString());
            Assert.That(rate.MvpBaselineCoreLevel, Is.EqualTo(Configuration(fixture).MvpBaselineCoreLevel));
            Assert.That(rate.ActiveFloorCount, Is.EqualTo(fixture.State.Floors.Length));
            Assert.That(rate.CoreContributionManaPerHour, Is.EqualTo(120d));
            Assert.That(rate.ActiveFloorContributionManaPerHour, Is.EqualTo(60d));
            Assert.That(rate.BaseManaPerHour, Is.EqualTo(180d));
            Assert.That(rate.ManaPerHour, Is.EqualTo(expected));
            Assert.That(rate.AfterHeatManaPerHour, Is.EqualTo(expected).Within(0.0000001d));
            Assert.That(rate.AfterResearchManaPerHour, Is.EqualTo(rate.AfterHeatManaPerHour));
            Assert.That(rate.AfterEventOrSeasonManaPerHour, Is.EqualTo(rate.AfterResearchManaPerHour));
            Assert.That(rate.AfterClampAndSoftCapManaPerHour, Is.EqualTo(rate.AfterEventOrSeasonManaPerHour));
            Assert.That(rate.SoftCapEnabled, Is.False);
        }

        [Test]
        public void CanonicalFloorCountUsesValidatedStateAndSupportsMoreThanOneFloor()
        {
            Fixture fixture = OneFloorFixture();
            SavedSpatialFloor first = fixture.State.Floors.Single();
            SavedSpatialFloor second = JsonUtility.FromJson<SavedSpatialFloor>(
                JsonUtility.ToJson(first).Replace("floor.00", "floor.01"));
            second.FloorIndex = first.FloorIndex + 1;
            FloorStructuralIdentityLifecycle firstLifecycle = fixture.State.LifecycleAndOwnership.Floors.Single();
            FloorStructuralIdentityLifecycle secondLifecycle = JsonUtility.FromJson<FloorStructuralIdentityLifecycle>(
                JsonUtility.ToJson(firstLifecycle).Replace("floor.00", "floor.01"));
            var source = new DetachedCanonicalSpatialSaveState
            {
                Authority = fixture.State.Authority,
                Floors = new[] { first, second },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    Floors = new[] { firstLifecycle, secondLifecycle },
                    ReturnedContents = Array.Empty<ReturnedStructuralContent>()
                }
            };
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(source,
                fixture.Profile.Canonical.Spatial, out DetachedCanonicalSpatialSaveState canonical), Is.True);
            Assert.That(CanonicalSpatialSaveContracts.Validate(canonical,
                fixture.Profile.Canonical.Spatial, true).IsValid, Is.True);
            SaveData save = Runtime(canonical, 0d, fixture.Configuration.HeatPeaceMinimum);

            Assert.That(CanonicalActiveFloorResolver.TryResolve(save,
                fixture.Profile.Canonical.Spatial, out int count), Is.True);
            Assert.That(count, Is.EqualTo(2));
            PassiveManaRateSummary rate = Service(fixture).ResolveRate(save, fixture.Configuration);
            Assert.That(rate.RuleResolved, Is.True, rate.Error.ToString());
            Assert.That(rate.ActiveFloorCount, Is.EqualTo(2));
            Assert.That(rate.ActiveFloorContributionManaPerHour, Is.EqualTo(120d));
            Assert.That(rate.ManaPerHour, Is.EqualTo(240d));
        }

        [Test]
        public void ContradictoryCanonicalStateFailsWithoutReadingLegacyFloorCounters()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.dungeonLayout = DungeonLayoutState.CreateEmpty(5, 1);
            fixture.Runtime.spatialFloors = new[] { fixture.State.Floors[0], fixture.State.Floors[0] };
            double before = fixture.Runtime.structureRuntime.ManaReserve;

            PassiveManaTickResult result = Service(fixture).ApplyTick(
                fixture.Runtime, fixture.Configuration, 1);

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Rate.Error, Is.EqualTo(PassiveManaResolutionError.CanonicalSpatialStateInvalid));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(before));
        }

        [Test]
        public void FractionalTickAwardsAccumulateWithoutPerTickRoundingInflation()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            CanonicalPassiveManaService service = Service(fixture);

            PassiveManaTickResult first = service.ApplyTick(fixture.Runtime, fixture.Configuration, 1);
            PassiveManaTickResult second = service.ApplyTick(fixture.Runtime, fixture.Configuration, 2);

            Assert.That(first.PotentialMana, Is.EqualTo(0.5d));
            Assert.That(first.NewMana, Is.EqualTo(0.5d));
            Assert.That(second.NewMana, Is.EqualTo(1d));
            Assert.That(second.NewMana, Is.Not.EqualTo(2d));
        }

        [Test]
        public void DeterministicRepeatedTimelineProducesSameFractionalResult()
        {
            Fixture fixture = OneFloorFixture();
            double heat = fixture.Configuration.HeatNoticeMinimum;
            SaveData left = Runtime(fixture.State, 0d, heat);
            SaveData right = Runtime(fixture.State, 0d, heat);
            CanonicalPassiveManaService service = Service(fixture);

            for (int tick = 1; tick <= 360; tick++)
            {
                service.ApplyTick(left, fixture.Configuration, tick);
                service.ApplyTick(right, fixture.Configuration, tick);
            }

            Assert.That(left.structureRuntime.ManaReserve, Is.EqualTo(right.structureRuntime.ManaReserve));
            Assert.That(left.structureRuntime.ManaReserve, Is.EqualTo(171d).Within(0.000000001d));
        }

        [TestCase(500d, 500.5d, false, false)]
        [TestCase(999.5d, 1000d, false, true)]
        [TestCase(999.75d, 1000d, true, true)]
        [TestCase(1000d, 1000d, true, true)]
        public void TickUsesStructuralEconomyCapacity(double before, double expected,
            bool clamped, bool full)
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.ManaReserve = before;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            CanonicalPassiveManaService service = Service(fixture);

            PassiveManaTickResult result = service.ApplyTick(fixture.Runtime,
                fixture.Configuration, 1);

            Assert.That(service.ManaCapacity, Is.EqualTo(fixture.Economy.ManaCapacity));
            Assert.That(result.NewMana, Is.EqualTo(expected));
            Assert.That(result.ClampedToCapacity, Is.EqualTo(clamped));
            Assert.That(result.StorageFull, Is.EqualTo(full));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve,
                Is.LessThanOrEqualTo(fixture.Economy.ManaCapacity));
        }

        [Test]
        public void TimeServiceActiveTickAwardsOnceAndPauseResumeDoesNotDuplicateStream()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            double initialHeat = fixture.Runtime.structureRuntime.Heat;
            CanonicalPassiveManaService passive = Service(fixture);
            ContentBootstrap bootstrap = Bootstrap();
            var time = new TimeService(new SimpleLogger(false), bootstrap.tickSeconds,
                bootstrap.timeRules.detectClockSkewSeconds, new FakeTimeSource());
            time.AttachSave(fixture.Runtime);
            int callbacks = 0;
            time.OnTick += tick =>
            {
                callbacks++;
                passive.ApplyTick(fixture.Runtime, fixture.Configuration, tick);
            };

            time.Update(bootstrap.tickSeconds);
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(0.5d));
            Assert.That(fixture.Runtime.structureRuntime.Heat, Is.EqualTo(initialHeat));
            time.OnPause();
            time.Update(bootstrap.tickSeconds * 3);
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(0.5d));
            Assert.That(fixture.Runtime.structureRuntime.Heat, Is.EqualTo(initialHeat));
            time.OnResume();
            time.Update(bootstrap.tickSeconds);
            Assert.That(callbacks, Is.EqualTo(2));
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(1d));
            Assert.That(fixture.Runtime.structureRuntime.Heat, Is.EqualTo(initialHeat));
        }

        [Test]
        public void CanonicalActiveTickAtConcernMinimumPreservesHeatAndUsesConcernEfficiency()
        {
            Fixture fixture = OneFloorFixture();
            ContentBootstrap bootstrap = Bootstrap();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatConcernMinimum;
            PassiveManaRateSummary expectedRate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);
            Assert.That(expectedRate.RuleResolved, Is.True, expectedRate.Error.ToString());
            Assert.That(expectedRate.HeatTierId, Is.EqualTo(CurrentHeatTierResolver.ConcernTierId));
            Assert.That(Configuration(fixture).TryGetHeatEfficiency(
                CurrentHeatTierResolver.ConcernTierId, out double concernEfficiency), Is.True);
            Assert.That(expectedRate.HeatEfficiencyMultiplier, Is.EqualTo(concernEfficiency));

            GameObject go = new GameObject("Canonical active tick Concern boundary");
            try
            {
                GameRoot root = ConfigureTickRoot(go, fixture, bootstrap);

                InvokeSimulationTick(root, 1);

                Assert.That(root.CurrentHeat, Is.EqualTo(fixture.Configuration.HeatConcernMinimum));
                Assert.That(root.Save.structureRuntime.Heat,
                    Is.EqualTo(fixture.Configuration.HeatConcernMinimum));
                Assert.That(root.Save.structureRuntime.ManaReserve,
                    Is.EqualTo(expectedRate.ManaPerHour * bootstrap.tickSeconds / 3600d)
                        .Within(0.000000001d));
                Assert.That(CurrentHeatTierResolver.Resolve(fixture.Configuration,
                    root.Save.structureRuntime.Heat).TierId,
                    Is.EqualTo(CurrentHeatTierResolver.ConcernTierId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MultipleCanonicalActiveTicksAtNoticeMinimumPreserveHeatAndTier()
        {
            Fixture fixture = OneFloorFixture();
            ContentBootstrap bootstrap = Bootstrap();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            PassiveManaRateSummary expectedRate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);
            Assert.That(expectedRate.RuleResolved, Is.True, expectedRate.Error.ToString());
            Assert.That(expectedRate.HeatTierId, Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            Assert.That(Configuration(fixture).TryGetHeatEfficiency(
                CurrentHeatTierResolver.NoticeTierId, out double noticeEfficiency), Is.True);
            Assert.That(expectedRate.HeatEfficiencyMultiplier, Is.EqualTo(noticeEfficiency));

            GameObject go = new GameObject("Canonical active ticks Notice boundary");
            try
            {
                GameRoot root = ConfigureTickRoot(go, fixture, bootstrap);
                const int tickCount = 4;

                for (int tick = 1; tick <= tickCount; tick++)
                {
                    InvokeSimulationTick(root, tick);
                    Assert.That(root.CurrentHeat,
                        Is.EqualTo(fixture.Configuration.HeatNoticeMinimum));
                    Assert.That(root.Save.structureRuntime.Heat,
                        Is.EqualTo(fixture.Configuration.HeatNoticeMinimum));
                }

                Assert.That(root.Save.structureRuntime.ManaReserve,
                    Is.EqualTo(expectedRate.ManaPerHour * bootstrap.tickSeconds * tickCount / 3600d)
                        .Within(0.000000001d));
                Assert.That(CurrentHeatTierResolver.Resolve(fixture.Configuration,
                    root.Save.structureRuntime.Heat).TierId,
                    Is.EqualTo(CurrentHeatTierResolver.NoticeTierId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ProductionActiveSaveIntervalSchedulesThirdConfiguredTickOnly()
        {
            ContentBootstrap bootstrap = Bootstrap();
            var scheduler = new ActivePlaySaveScheduler(bootstrap.tickSeconds,
                bootstrap.timeRules.activeSaveIntervalSeconds);

            Assert.That(scheduler.AdvanceTick(), Is.False);
            Assert.That(scheduler.AdvanceTick(), Is.False);
            Assert.That(scheduler.AdvanceTick(), Is.True);
            Assert.That(scheduler.AdvanceTick(), Is.False);
        }

        [Test]
        public void LegacyManaGeneratorIsSuppressedInCanonicalAuthorityMode()
        {
            StructureSimulationConfig config = LegacyStructureConfig();
            var layout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(layout, 0, 0,
                StructureSimulationPass.ManaGeneratorBasicId);
            var runtime = new StructureRuntimeState();

            new StructureSimulationPass(new HeatSystem(), config).SimulateTick(layout,
                runtime, 1, StructureManaAuthorityMode.CanonicalPassive);

            Assert.That(runtime.ManaReserve, Is.EqualTo(0d));
            Assert.That(runtime.Heat, Is.EqualTo(config.Structures[0].HeatDeltaPerTick));
        }

        [Test]
        public void CanonicalAdventureActionDoesNotApplyLegacyOrPassiveTick()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            fixture.Runtime.dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1);
            new PlacementService().PlaceStructure(fixture.Runtime.dungeonLayout, 0, 0,
                StructureSimulationPass.ManaGeneratorBasicId);
            fixture.Runtime.runHistory = new RunHistoryState();
            double manaBefore = fixture.Runtime.structureRuntime.ManaReserve;
            long ticksBefore = fixture.Runtime.totalTicks;
            var go = new GameObject("Canonical adventure passive-mana boundary");
            SaveService saveService = null;
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                SetField(root, "<Save>k__BackingField", fixture.Runtime);
                saveService = new SaveService(new SimpleLogger(false, (level, message) => { }), new SaveConfig
                {
                    fileName = "canonical_adventure_passive_boundary_" + Guid.NewGuid().ToString("N") + ".json",
                    useAtomicWrites = false
                });
                SetField(root, "<SaveService>k__BackingField", saveService);
                SetField(root, "_runSimulationService", new RunSimulationService(fixture.Configuration));
                SetField(root, "_structureSimulationPass",
                    new StructureSimulationPass(new HeatSystem(), LegacyStructureConfig()));
                var content = new ContentService();
                SetField(root, "<Content>k__BackingField", content);
                SetField(content, "<ProductionSpatialContent>k__BackingField", fixture.Production);

                bool ran = root.SimulateMvpActiveLoopOnce(out bool structureTick);

                Assert.That(ran, Is.True);
                Assert.That(structureTick, Is.False);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
                Assert.That(root.Save.totalTicks, Is.EqualTo(ticksBefore));
                Assert.That(root.Save.runHistory.RecentOutcomes.Length, Is.EqualTo(1));
            }
            finally
            {
                if (saveService != null && File.Exists(saveService.SavePath))
                    File.Delete(saveService.SavePath);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OfflineSummaryDoesNotAwardPassiveMana()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Runtime.structureRuntime.ManaReserve = 12.5d;
            fixture.Runtime.lastSavedUtcUnix = 900;
            var time = new FakeTimeSource { Now = 1000 };
            double before = fixture.Runtime.structureRuntime.ManaReserve;

            OfflineSummary summary = new OfflineSummaryResolver(time).Resolve(
                fixture.Runtime, Bootstrap().timeRules);

            Assert.That(summary.RuleResolved, Is.True);
            Assert.That(summary.WouldProcessOfflineProgress, Is.False);
            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(before));
        }

        [Test]
        public void FractionalManaPersistsThroughCanonicalSaveAndReopenWithoutSchemaChange()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.ManaReserve = 0d;
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatPeaceMinimum;
            CanonicalPassiveManaService service = Service(fixture);
            service.ApplyTick(fixture.Runtime, fixture.Configuration, 1);
            service.ApplyTick(fixture.Runtime, fixture.Configuration, 2);

            DetachedCanonicalWriteResult saved = fixture.Authority.SaveRecognizedState(
                fixture.ActivePath, fixture.FileSystem, fixture.Session, fixture.Runtime);
            fixture.Accept(saved);
            fixture.Reopen();
            string persisted = Encoding.UTF8.GetString(fixture.Session.GetCurrentBytes());

            Assert.That(fixture.Runtime.structureRuntime.ManaReserve, Is.EqualTo(1d));
            Assert.That(persisted, Does.Contain("\"schemaVersion\":9"));
            Assert.That(persisted, Does.Not.Contain("MvpBaselineCoreLevel"));
            Assert.That(persisted, Does.Not.Contain("ManaPerHour"));
        }

        [Test]
        public void LocalizedPresentationShowsCanonicalRateBreakdownAndNoRawTierId()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            PassiveManaRateSummary rate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);
            var strings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PassiveManaPresenter.BalanceAndRateFormatKey] = "Balance {0}/{1}; rate {2}",
                [PassiveManaPresenter.ContributionsFormatKey] = "Core {0}; floors {1} give {2}",
                [PassiveManaPresenter.HeatFormatKey] = "Heat {0}; efficiency {1:P0}",
                [PassiveManaPresenter.StorageFullKey] = "Full",
                [PassiveManaPresenter.UnavailableKey] = "Unavailable",
                [PassiveManaPresenter.UnknownHeatKey] = "Unknown",
                [CurrentHeatTierResolver.NoticeTierId] = "Notice"
            };

            string text = PassiveManaPresenter.Build(rate, fixture.Economy.ManaCapacity,
                fixture.Economy.ManaCapacity,
                PassiveManaPresenter.ResolveFormatProvider("en"),
                key => strings.TryGetValue(key, out string value) ? value : key);

            Assert.That(text, Does.Contain(rate.ManaPerHour.ToString()));
            Assert.That(text, Does.Contain(rate.CoreContributionManaPerHour.ToString()));
            Assert.That(text, Does.Contain(rate.ActiveFloorContributionManaPerHour.ToString()));
            Assert.That(text, Does.Contain("Notice"));
            Assert.That(text, Does.Contain("Full"));
            Assert.That(text, Does.Not.Contain(CurrentHeatTierResolver.NoticeTierId));
        }

        [Test]
        public void ProductionLocalizationContainsEveryPassiveManaPresentationKey()
        {
            StringTable table = JsonUtility.FromJson<StringTable>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/string_table_en.json"));
            HashSet<string> keys = table.entries.Select(entry => entry.key)
                .ToHashSet(StringComparer.Ordinal);
            string[] required =
            {
                PassiveOnlineManaConfigurationSnapshot.ConfigurationUnavailableKey,
                PassiveManaPresenter.UnavailableKey,
                PassiveManaPresenter.BalanceAndRateFormatKey,
                PassiveManaPresenter.ContributionsFormatKey,
                PassiveManaPresenter.HeatFormatKey,
                PassiveManaPresenter.StorageFullKey,
                PassiveManaPresenter.UnknownHeatKey,
                "ui.dev.save.periodic"
            };
            CollectionAssert.IsSubsetOf(required, keys);
        }

        [Test]
        public void MissingPresentationLocalizationDoesNotExposeRawKeys()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            PassiveManaRateSummary rate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);

            string unavailable = PassiveManaPresenter.Build(null, 0d,
                fixture.Economy.ManaCapacity,
                PassiveManaPresenter.ResolveFormatProvider("en"), key => key);
            string resolved = PassiveManaPresenter.Build(rate, 0d,
                fixture.Economy.ManaCapacity,
                PassiveManaPresenter.ResolveFormatProvider("en"), key => key);

            Assert.That(unavailable, Is.Empty);
            Assert.That(resolved, Does.Not.Contain("ui.passive_mana"));
            Assert.That(resolved, Does.Not.Contain("heat_tier."));
        }

        [Test]
        public void PassiveManaPresentationUsesSelectedEnglishLocaleNotMachineLocale()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            PassiveManaRateSummary rate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);
            Dictionary<string, string> strings = PresentationStrings();
            CultureInfo originalCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                IFormatProvider provider = PassiveManaPresenter.ResolveFormatProvider("en");
                string text = PassiveManaPresenter.Build(rate, 12.5d,
                    fixture.Economy.ManaCapacity, provider,
                    key => strings.TryGetValue(key, out string value) ? value : key);

                Assert.That(text, Does.Contain("12.5"));
                Assert.That(text, Does.Contain(rate.HeatEfficiencyMultiplier.ToString("P0", provider)));
                Assert.That(text, Does.Not.Contain("12,5"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void PassiveManaPresentationUsesSelectedCommaDecimalLocale()
        {
            Fixture fixture = OneFloorFixture();
            fixture.Runtime.structureRuntime.Heat = fixture.Configuration.HeatNoticeMinimum;
            PassiveManaRateSummary rate = Service(fixture).ResolveRate(
                fixture.Runtime, fixture.Configuration);
            Dictionary<string, string> strings = PresentationStrings();
            IFormatProvider provider = PassiveManaPresenter.ResolveFormatProvider("de-DE");

            string text = PassiveManaPresenter.Build(rate, 12.5d,
                fixture.Economy.ManaCapacity, provider,
                key => strings.TryGetValue(key, out string value) ? value : key);

            Assert.That(text, Does.Contain("12,5"));
            Assert.That(text, Does.Contain(rate.HeatEfficiencyMultiplier.ToString("P0", provider)));
        }

        [Test]
        public void JapaneseLanguageResolvesFormatProviderWithoutPresenterSpecialCase()
        {
            CultureInfo provider = PassiveManaPresenter.ResolveFormatProvider("ja") as CultureInfo;

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.Name, Is.EqualTo("ja-JP"));
            Assert.DoesNotThrow(() => 12.5d.ToString("0.###", provider));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-valid-language")]
        public void MissingOrInvalidLanguageUsesEnglishSafeFormatProvider(string language)
        {
            CultureInfo provider = PassiveManaPresenter.ResolveFormatProvider(language) as CultureInfo;

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.Name, Is.EqualTo("en-US"));
            Assert.That(12.5d.ToString("0.###", provider), Is.EqualTo("12.5"));
        }

        private static Dictionary<string, string> PresentationStrings() =>
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PassiveManaPresenter.BalanceAndRateFormatKey] = "Balance {0:0.###}/{1:0.###}; rate {2:0.###}",
                [PassiveManaPresenter.ContributionsFormatKey] = "Core {0:0.###}; floors {1} give {2:0.###}",
                [PassiveManaPresenter.HeatFormatKey] = "Heat {0}; efficiency {1:P0}",
                [PassiveManaPresenter.StorageFullKey] = "Full",
                [PassiveManaPresenter.UnavailableKey] = "Unavailable",
                [PassiveManaPresenter.UnknownHeatKey] = "Unknown",
                [CurrentHeatTierResolver.NoticeTierId] = "Notice"
            };

        private static SaveData Runtime(DetachedCanonicalSpatialSaveState state,
            double mana, double heat) => new SaveData
        {
            canonicalSpatialAuthority = state.Authority,
            spatialFloors = state.Floors,
            validatedCanonicalSpatialState = state,
            structureRuntime = new StructureRuntimeState { ManaReserve = mana, Heat = heat },
            runHistory = new RunHistoryState()
        };

        private static double HeatFor(RunSimulationConfig config, string tierId) =>
            tierId == CurrentHeatTierResolver.PeaceTierId ? config.HeatPeaceMinimum :
            tierId == CurrentHeatTierResolver.NoticeTierId ? config.HeatNoticeMinimum :
            config.HeatConcernMinimum;

        private static StructureSimulationConfig LegacyStructureConfig() =>
            JsonUtility.FromJson<StructureSimulationConfig>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/structure_simulation_config.json"));

        private static Fixture OneFloorFixture()
        {
            Fixture fixture = Fixture.Create(null);
            fixture.Accept(fixture.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            return fixture;
        }

        private static void SetField(object target, string name, object value)
        {
            Assert.That(target, Is.Not.Null);
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static GameRoot ConfigureTickRoot(GameObject go, Fixture fixture,
            ContentBootstrap bootstrap)
        {
            GameRoot root = go.AddComponent<GameRoot>();
            SetField(root, "<Save>k__BackingField", fixture.Runtime);
            SetField(root, "<CurrentHeat>k__BackingField", fixture.Runtime.structureRuntime.Heat);
            SetField(root, "_runSimulationService", new RunSimulationService(fixture.Configuration));
            Assert.That(root.ConfigureCanonicalPassiveManaForTests(Configuration(fixture),
                fixture.Economy, fixture.Profile.Canonical.Spatial, bootstrap.tickSeconds,
                bootstrap.timeRules.activeSaveIntervalSeconds), Is.True);
            return root;
        }

        private static void InvokeSimulationTick(GameRoot root, long tickIndex)
        {
            MethodInfo method = typeof(GameRoot).GetMethod("HandleSimulationTick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(root, new object[] { tickIndex });
        }
    }
}
#endif
