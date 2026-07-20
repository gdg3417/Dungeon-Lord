using System;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed class RunSimulationService
    {
        private readonly RunSimulationConfig _config;
        private readonly LootConfig _lootConfig;
        private readonly string _lootTableId;
        public RunSimulationConfig Config => _config;

        public RunSimulationService(RunSimulationConfig config, LootConfig lootConfig = null, string lootTableId = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _lootConfig = lootConfig;
            _lootTableId = !string.IsNullOrEmpty(lootTableId) ? lootTableId : _config.LootTableId;
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence)
        {
            return SimulateOnce(runtime, tickStarted, runSequence, RunPostureResolver.BalancedId, null);
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId)
        {
            return SimulateOnce(runtime, tickStarted, runSequence, postureId, null);
        }

        public RunOutcomeRecord SimulateRoute(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId, MvpOrderedRouteRoom[] route)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (route == null || route.Length <= 1)
            {
                MvpPlacementEffectsSummary effects = route != null && route.Length == 1
                    ? MvpPlacementEffectsResolver.ResolvePlacements(route[0].ToOrderedPlacements(), _config) : null;
                RunOutcomeRecord compatible = SimulateOnce(runtime, tickStarted, runSequence, postureId, effects);
                compatible.ConfiguredRoutePlacementEffects = ClonePlacementEffects(effects);
                compatible.ReachedRoutePlacementEffects = ClonePlacementEffects(effects);
                compatible.ClearedRewardPlacementEffects = compatible.Success ? ClonePlacementEffects(effects) : EmptyPlacementEffects();
                compatible.ConfiguredRoomCount = route?.Length ?? 0;
                AddCompatibleRoomMetadata(compatible, route);
                return compatible;
            }

            RunPostureConfig posture = RunPostureResolver.Resolve(_config, postureId);
            double heatAtStart = runtime.Heat;
            double manaAtStart = runtime.ManaReserve;
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            RunSurvivalSummary partyRoll = BuildSurvivalSummary(runSequence, tickStarted, true);
            int initialParty = partyRoll.PartySize;
            int currentSurvivors = initialParty;
            var rooms = new System.Collections.Generic.List<RunRoomResolutionSummary>();
            var generatedItems = new System.Collections.Generic.List<string>();
            int generatedValue = 0, generatedReserve = 0, generatedTradeable = 0, rollCount = 0;
            bool hasActiveEncounter = false;
            bool lootResolverSuccess = true;
            int lootResolverErrorCode = (int)LootRollResolverErrorCode.None;
            MvpPlacementEffectsSummary configuredEffects = EmptyPlacementEffects();
            for (int i = 0; i < route.Length; i++) AddPlacementEffects(configuredEffects, MvpPlacementEffectsResolver.ResolvePlacements(route[i].ToOrderedPlacements(), _config));
            MvpPlacementEffectsSummary reachedEffects = EmptyPlacementEffects();
            MvpPlacementEffectsSummary clearedRewardEffects = EmptyPlacementEffects();
            RunCompositionOutcomeSummary finalComposition = BuildCompositionOutcomeSummary(reachedEffects, manaAtStart);
            bool finalSuccess = true;
            double finalChance = _config.BaseSuccessChance;
            double finalHeatPenalty = 0d, finalManaBonus = 0d, finalCrisisPenalty = 0d;

            for (int i = 0; i < route.Length; i++)
            {
                MvpOrderedRouteRoom routeRoom = route[i];
                MvpPlacementEffectsSummary localEffects = MvpPlacementEffectsResolver.ResolvePlacements(routeRoom.ToOrderedPlacements(), _config);
                AddPlacementEffects(reachedEffects, localEffects);
                int entrants = currentSurvivors;
                int roomSeed = DeriveRoomSeed(seed, routeRoom.FloorIndex, routeRoom.RoomIndex);
                if (!routeRoom.HasActiveContent)
                {
                    rooms.Add(BuildEmptyRoomSummary(routeRoom, entrants, localEffects, roomSeed, generatedValue));
                    continue;
                }

                hasActiveEncounter = true;
                RunCompositionOutcomeSummary composition = BuildCompositionOutcomeSummary(localEffects, manaAtStart);
                finalHeatPenalty = runtime.Heat * _config.HeatPenaltyPerPoint;
                finalManaBonus = composition.EffectiveManaReserve * _config.ManaReserveBonusPerPoint;
                finalCrisisPenalty = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;
                finalChance = Math.Max(0d, Math.Min(1d, _config.BaseSuccessChance - finalHeatPenalty + finalManaBonus - finalCrisisPenalty + composition.SuccessChanceDelta));
                bool cleared = finalChance >= _config.SuccessThreshold;
                RunSurvivalSummary roomSurvival = BuildSurvivalSummaryForParty(entrants, roomSeed, cleared);
                ApplyCompositionToSurvivalSummary(roomSurvival, composition);
                ApplyCasualtyPressureToSurvivalSummary(roomSurvival, composition, posture);
                currentSurvivors = roomSurvival.SurvivorCount;

                RunLootSummary roomLoot = null;
                if (cleared)
                {
                    AddPlacementEffects(clearedRewardEffects, localEffects);
                    roomLoot = ApplyCompositionToLootSummary(ApplyPostureToLootSummary(BuildLootSummary(roomSeed), posture), composition);
                    if (roomLoot == null || !roomLoot.ResolverSuccess)
                    {
                        lootResolverSuccess = false;
                        if (lootResolverErrorCode == (int)LootRollResolverErrorCode.None)
                            lootResolverErrorCode = roomLoot?.ResolverErrorCode ?? (int)LootRollResolverErrorCode.TableIdMissing;
                    }
                    else
                    {
                        generatedItems.AddRange(roomLoot.GeneratedItemIds ?? Array.Empty<string>());
                        generatedValue += roomLoot.TotalGeneratedWorldValue;
                        generatedReserve += roomLoot.TotalGeneratedReserveCost;
                        generatedTradeable += roomLoot.TotalGeneratedTradeableWorldValue;
                        rollCount += roomLoot.RollCount;
                    }
                }

                bool stopped = !cleared || currentSurvivors <= 0;
                rooms.Add(new RunRoomResolutionSummary {
                    FloorIndex = routeRoom.FloorIndex, RoomIndex = routeRoom.RoomIndex, RoomOptionId = routeRoom.RoomOptionId,
                    Reached = true, Cleared = cleared, PartyEntering = entrants, SurvivorsLeaving = currentSurvivors,
                    Deaths = roomSurvival.DeathCount, StoppedRoute = stopped,
                    StopReasonKey = stopped ? (currentSurvivors <= 0 ? RouteWipedKey : RouteRetreatedKey) : string.Empty,
                    LocalPlacementEffects = localEffects, GeneratedLootValue = roomLoot?.TotalGeneratedWorldValue ?? 0,
                    CarriedLootValueAfterRoom = generatedValue, LocalHeatPressureDelta = composition.HeatDeltaOffset,
                    CasualtyPressureHeatDelta = roomSurvival.CasualtyHeatDelta, CasualtyPressure = roomSurvival.CasualtyPressure,
                    CasualtyLootExtractionPenalty = roomSurvival.CasualtyLootExtractionPenalty, ManaPressureCost = composition.ManaReservePressureCost, DeterministicSeed = roomSeed, RuleSourceId = composition.RuleSourceId
                });
                finalComposition = composition;
                finalSuccess = cleared;
                if (stopped) break;
            }

            if (!hasActiveEncounter)
            {
                return BuildNoEncounterRouteOutcome(runtime, tickStarted, runSequence, posture, heatAtStart, manaAtStart, partyRoll, rooms, configuredEffects, reachedEffects);
            }

            var loot = new RunLootSummary { LootTableId = _lootTableId, ResolverSeed = seed, ResolverSuccess = lootResolverSuccess, ResolverErrorCode = lootResolverErrorCode, RollCount = rollCount,
                GeneratedItemIds = generatedItems.ToArray(), TotalGeneratedWorldValue = generatedValue,
                TotalGeneratedReserveCost = generatedReserve, TotalGeneratedTradeableWorldValue = Math.Min(generatedValue, generatedTradeable) };
            var survival = BuildAggregateSurvival(partyRoll, initialParty, currentSurvivors, rooms);
            RunCompositionOutcomeSummary aggregateComposition = BuildCompositionOutcomeSummary(reachedEffects, manaAtStart);
            RunCompositionOutcomeSummary rewardComposition = BuildCompositionOutcomeSummary(clearedRewardEffects, manaAtStart);
            RunLootExtractionSummary extraction = ApplyCompositionToExtractionSummary(
                ApplyPostureToExtractionSummary(LootExtractionResolver.Resolve(_lootConfig, loot, survival, seed, _config.LootExtractionRoundingPolicyId, _config.LootExtractionRuleSourceId), loot, posture), loot, rewardComposition);
            ApplyCasualtyPressureToExtractionSummary(extraction, loot, survival);
            RunLootDropRecord[] breakdown = RunLootBreakdownResolver.Resolve(_lootConfig, extraction);
            RunAdventurerAttractionSummary attraction = ApplyCompositionToAttractionSummary(AdventurerAttractionResolver.Resolve(_config, extraction, seed), aggregateComposition);
            RunAdventurerInterestForecastSummary forecast = AdventurerInterestForecastResolver.Resolve(_config, attraction, seed);
            RunAdventurerDemandBudgetSummary demand = AdventurerDemandBudgetResolver.Resolve(_config, forecast, seed);
            RunHeatDeltaSummary heatDelta = ApplyCompositionToHeatDeltaSummary(ApplyPostureToHeatDeltaSummary(RunHeatDeltaResolver.Resolve(_config, survival, extraction, seed), posture), aggregateComposition);
            ApplyCasualtyPressureToHeatDeltaSummary(heatDelta, survival);
            RunHeatApplicationSummary heatApplication = RunHeatStateApplyResolver.Resolve(_config, heatAtStart, heatDelta);
            if (heatApplication.RuleResolved) runtime.Heat = heatApplication.HeatAfter;

            int clearedCount = rooms.FindAll(room => room.Cleared).Count;
            bool fullClear = rooms.Count == route.Length && clearedCount == route.Length;
            string routeKey = fullClear ? RouteClearedKey : (currentSurvivors <= 0 ? RouteWipedKey : rooms[rooms.Count - 1].RoomIndex == 0 ? RouteStoppedRoomOneKey : RouteStoppedRoomTwoKey);
            return new RunOutcomeRecord {
                RunId = $"run-{runSequence}", TickStarted = tickStarted, Success = fullClear,
                Score = fullClear ? _config.BaseScoreOnSuccess + (int)Math.Round(aggregateComposition.EffectiveManaReserve * _config.ScorePerManaPoint) : 0,
                ReasonKey = finalSuccess ? "run.reason.success" : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold"),
                HeatAtStart = heatAtStart, ManaAtStart = manaAtStart, CrisisActiveAtStart = runtime.IsHeatCrisisActive, HasBreakdown = true,
                BaseChance = _config.BaseSuccessChance, HeatPenaltyApplied = finalHeatPenalty,
                ManaBonusApplied = finalManaBonus, CrisisPenaltyApplied = finalCrisisPenalty, FinalChance = finalChance,
                SuccessThresholdUsed = _config.SuccessThreshold, FeedbackTagKeys = BuildFeedbackTagKeys(runtime, fullClear, aggregateComposition),
                LootSummary = loot, SurvivalSummary = survival, LootExtractionSummary = extraction, LootBreakdown = breakdown,
                AdventurerAttractionSummary = attraction, AdventurerInterestForecastSummary = forecast, AdventurerDemandBudgetSummary = demand,
                RunHeatDeltaSummary = heatDelta, RunHeatApplicationSummary = heatApplication, CompositionOutcomeSummary = finalComposition,
                RunPostureId = posture?.Id, RoomResolutions = rooms.ToArray(), HighestRoomReached = rooms[rooms.Count - 1].RoomIndex,
                ReachedRoomCount = rooms.Count, ConfiguredRoomCount = route.Length, ClearedRoomCount = clearedCount, FinalRouteOutcomeKey = routeKey,
                ConfiguredRoutePlacementEffects = configuredEffects, ReachedRoutePlacementEffects = reachedEffects,
                ClearedRewardPlacementEffects = clearedRewardEffects
            };
        }

        private RunOutcomeRecord BuildNoEncounterRouteOutcome(StructureRuntimeState runtime, long tickStarted, int runSequence, RunPostureConfig posture, double heatAtStart, double manaAtStart, RunSurvivalSummary party, System.Collections.Generic.List<RunRoomResolutionSummary> rooms, MvpPlacementEffectsSummary configured, MvpPlacementEffectsSummary reached)
        {
            party.SurvivorCount = party.PartySize; party.DeathCount = 0; party.SurvivorRatio = party.PartySize > 0 ? 1d : 0d;
            party.CasualtyPressure = 0d; party.CasualtyLootExtractionPenalty = 0d; party.CasualtyHeatDelta = 0d;
            var loot = new RunLootSummary { LootTableId = _lootTableId, ResolverSeed = ComputeResolverSeed(runSequence, tickStarted), ResolverSuccess = true, ResolverErrorCode = (int)LootRollResolverErrorCode.None, GeneratedItemIds = Array.Empty<string>() };
            var extraction = new RunLootExtractionSummary { RuleResolved = true, DeterministicErrorCode = (int)RunLootExtractionSummaryErrorCode.None, DeterministicSeed = ComputeResolverSeed(runSequence, tickStarted), ExtractedItemIds = Array.Empty<string>(), LostItemIds = Array.Empty<string>() };
            var heatDelta = new RunHeatDeltaSummary { RuleResolved = true, DeterministicErrorCode = (int)RunHeatDeltaSummaryErrorCode.None, FinalHeatDelta = 0d, DeterministicSeed = ComputeResolverSeed(runSequence, tickStarted) };
            var heatApplication = new RunHeatApplicationSummary { RuleResolved = true, DeterministicErrorCode = (int)RunHeatApplicationSummaryErrorCode.None, HeatBefore = heatAtStart, HeatAfter = heatAtStart, AppliedDelta = 0d };
            return new RunOutcomeRecord { RunId = $"run-{runSequence}", TickStarted = tickStarted, Success = true, Score = _config.BaseScoreOnSuccess, ReasonKey = "run.reason.success",
                HeatAtStart = heatAtStart, ManaAtStart = manaAtStart, CrisisActiveAtStart = runtime.IsHeatCrisisActive, HasBreakdown = true, BaseChance = _config.BaseSuccessChance, FinalChance = _config.BaseSuccessChance, SuccessThresholdUsed = _config.SuccessThreshold,
                FeedbackTagKeys = BuildFeedbackTagKeys(runtime, true, BuildCompositionOutcomeSummary(EmptyPlacementEffects(), manaAtStart)), LootSummary = loot, LootExtractionSummary = extraction, LootBreakdown = Array.Empty<RunLootDropRecord>(),
                SurvivalSummary = party, RunHeatDeltaSummary = heatDelta, RunHeatApplicationSummary = heatApplication, CompositionOutcomeSummary = BuildCompositionOutcomeSummary(reached, manaAtStart), RunPostureId = posture?.Id,
                RoomResolutions = rooms.ToArray(), HighestRoomReached = rooms[rooms.Count - 1].RoomIndex, ReachedRoomCount = rooms.Count, ConfiguredRoomCount = rooms.Count, ClearedRoomCount = rooms.Count, FinalRouteOutcomeKey = RouteClearedKey,
                ConfiguredRoutePlacementEffects = configured, ReachedRoutePlacementEffects = reached, ClearedRewardPlacementEffects = EmptyPlacementEffects() };
        }

        public const string RouteClearedKey = "run.route.outcome.cleared";
        public const string RouteStoppedRoomOneKey = "run.route.outcome.stopped_room_one";
        public const string RouteStoppedRoomTwoKey = "run.route.outcome.stopped_room_two";
        public const string RouteWipedKey = "run.route.outcome.wiped";
        public const string RouteRetreatedKey = "run.route.outcome.retreated";

        private void AddCompatibleRoomMetadata(RunOutcomeRecord outcome, MvpOrderedRouteRoom[] route)
        {
            if (route == null || route.Length == 0) return;
            outcome.RoomResolutions = new[] { BuildRoomSummary(route[0], outcome, outcome.SurvivalSummary?.PartySize ?? 0, !outcome.Success) };
            outcome.ReachedRoomCount = 1; outcome.ClearedRoomCount = outcome.Success ? 1 : 0;
            outcome.HighestRoomReached = route[0].RoomIndex;
            outcome.FinalRouteOutcomeKey = outcome.Success ? RouteClearedKey : (outcome.SurvivalSummary?.SurvivorCount ?? 0) == 0 ? RouteWipedKey : RouteRetreatedKey;
        }

        private static RunRoomResolutionSummary BuildEmptyRoomSummary(MvpOrderedRouteRoom room, int entrants, MvpPlacementEffectsSummary effects, int seed, int carriedLoot)
        {
            return new RunRoomResolutionSummary { FloorIndex = room.FloorIndex, RoomIndex = room.RoomIndex, RoomOptionId = room.RoomOptionId,
                Reached = true, Cleared = true, PartyEntering = entrants, SurvivorsLeaving = entrants, LocalPlacementEffects = effects,
                DeterministicSeed = seed, GeneratedLootValue = 0, CarriedLootValueAfterRoom = carriedLoot };
        }

        private static RunRoomResolutionSummary BuildRoomSummary(MvpOrderedRouteRoom room, RunOutcomeRecord outcome, int entrants, bool stopped)
        {
            RunSurvivalSummary survival = outcome.SurvivalSummary;
            return new RunRoomResolutionSummary { FloorIndex = room.FloorIndex, RoomIndex = room.RoomIndex, RoomOptionId = room.RoomOptionId,
                Reached = true, Cleared = outcome.Success, PartyEntering = entrants, SurvivorsLeaving = survival?.SurvivorCount ?? entrants,
                Deaths = survival?.DeathCount ?? 0, StoppedRoute = stopped, StopReasonKey = stopped ? outcome.ReasonKey : string.Empty,
                LocalPlacementEffects = outcome.CompositionOutcomeSummary?.PlacementEffects, GeneratedLootValue = outcome.Success ? outcome.LootSummary?.TotalGeneratedWorldValue ?? 0 : 0,
                CarriedLootValueAfterRoom = outcome.Success ? outcome.LootSummary?.TotalGeneratedWorldValue ?? 0 : 0,
                LocalHeatPressureDelta = outcome.CompositionOutcomeSummary?.HeatDeltaOffset ?? 0d,
                CasualtyPressureHeatDelta = survival?.CasualtyHeatDelta ?? 0d, CasualtyPressure = survival?.CasualtyPressure ?? 0d,
                CasualtyLootExtractionPenalty = survival?.CasualtyLootExtractionPenalty ?? 0d, ManaPressureCost = outcome.CompositionOutcomeSummary?.ManaReservePressureCost ?? 0d,
                DeterministicSeed = survival?.DeterministicSeed ?? 0, RuleSourceId = outcome.CompositionOutcomeSummary?.RuleSourceId };
        }

        private RunLootSummary BuildLootSummary(int deterministicSeed)
        {
            if (string.IsNullOrEmpty(_lootTableId)) return null;
            LootRollResolverResult result = LootRollResolver.Resolve(_lootConfig, _lootTableId, deterministicSeed);
            return new RunLootSummary { LootTableId = _lootTableId, ResolverSeed = deterministicSeed, ResolverSuccess = result.success,
                ResolverErrorCode = (int)result.errorCode, RollCount = result.rollCount,
                GeneratedItemIds = result.generatedItemIds != null ? new System.Collections.Generic.List<string>(result.generatedItemIds).ToArray() : Array.Empty<string>(),
                TotalGeneratedWorldValue = result.totalGeneratedWorldValue, TotalGeneratedReserveCost = result.totalGeneratedReserveCost,
                TotalGeneratedTradeableWorldValue = result.totalGeneratedTradeableWorldValue };
        }

        private static int DeriveRoomSeed(int runSeed, int floorIndex, int roomIndex)
        {
            unchecked { int hash = runSeed; hash = (hash * 31) + floorIndex; return (hash * 31) + roomIndex; }
        }

        private static MvpPlacementEffectsSummary EmptyPlacementEffects()
        {
            return new MvpPlacementEffectsSummary { RuleResolved = true, ContributingOptionIds = Array.Empty<string>(), EffectLocalizationKeys = Array.Empty<string>() };
        }

        private static void AddPlacementEffects(MvpPlacementEffectsSummary target, MvpPlacementEffectsSummary value)
        {
            if (value == null) return;
            target.RuleSourceId = value.RuleSourceId; target.PathCapacity += value.PathCapacity; target.Danger += value.Danger;
            target.ManaPressure += value.ManaPressure; target.HeatPressure += value.HeatPressure; target.LootBonus += value.LootBonus; target.Attraction += value.Attraction;
            var ids = new System.Collections.Generic.List<string>(target.ContributingOptionIds ?? Array.Empty<string>()); ids.AddRange(value.ContributingOptionIds ?? Array.Empty<string>()); target.ContributingOptionIds = ids.ToArray();
            var keys = new System.Collections.Generic.List<string>(target.EffectLocalizationKeys ?? Array.Empty<string>()); keys.AddRange(value.EffectLocalizationKeys ?? Array.Empty<string>()); target.EffectLocalizationKeys = keys.ToArray();
        }

        private static RunSurvivalSummary BuildAggregateSurvival(RunSurvivalSummary partyRoll, int initialParty, int survivors, System.Collections.Generic.List<RunRoomResolutionSummary> rooms)
        {
            double casualtyPenalty = 0d, casualtyHeat = 0d, pressure = 0d;
            // Route pressure is the maximum resolved room pressure; penalties and casualty heat are additive.
            foreach (RunRoomResolutionSummary room in rooms)
            {
                pressure = Math.Max(pressure, room.CasualtyPressure);
                casualtyPenalty += room.CasualtyLootExtractionPenalty;
                casualtyHeat += room.CasualtyPressureHeatDelta;
            }
            casualtyPenalty = Math.Max(0d, Math.Min(1d, casualtyPenalty));
            return new RunSurvivalSummary { PartySize = initialParty, SurvivorCount = survivors, DeathCount = initialParty - survivors,
                SurvivorRatio = initialParty > 0 ? (double)survivors / initialParty : 0d, DeterministicSeed = partyRoll.DeterministicSeed,
                RuleResolved = partyRoll.RuleResolved, DeterministicErrorCode = partyRoll.DeterministicErrorCode, RuleSourceId = partyRoll.RuleSourceId,
                SuccessAtResolution = survivors > 0, CasualtyLootExtractionPenalty = casualtyPenalty, CasualtyHeatDelta = casualtyHeat, CasualtyPressure = pressure };
        }

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId, MvpPlacementEffectsSummary placementEffects)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            RunPostureConfig posture = RunPostureResolver.Resolve(_config, postureId);
            RunCompositionOutcomeSummary compositionOutcome = BuildCompositionOutcomeSummary(placementEffects, runtime.ManaReserve);
            double heatAtStart = runtime.Heat;
            double baseChance = _config.BaseSuccessChance;
            double heatPenaltyApplied = heatAtStart * _config.HeatPenaltyPerPoint;
            double manaBonusApplied = compositionOutcome.EffectiveManaReserve * _config.ManaReserveBonusPerPoint;
            double crisisPenaltyApplied = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;

            double unclampedChance = baseChance - heatPenaltyApplied + manaBonusApplied - crisisPenaltyApplied + compositionOutcome.SuccessChanceDelta;
            double finalChance = Math.Max(0d, Math.Min(1d, unclampedChance));
            double successThreshold = _config.SuccessThreshold;
            bool success = finalChance >= successThreshold;

            int score = success
                ? _config.BaseScoreOnSuccess + (int)Math.Round(compositionOutcome.EffectiveManaReserve * _config.ScorePerManaPoint)
                : 0;

            string reasonKey = success
                ? "run.reason.success"
                : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold");
            string[] feedbackTagKeys = BuildFeedbackTagKeys(runtime, success, compositionOutcome);

            RunLootSummary lootSummary = ApplyCompositionToLootSummary(
                ApplyPostureToLootSummary(BuildLootSummary(runSequence, tickStarted), posture),
                compositionOutcome);
            RunSurvivalSummary survivalSummary = ApplyCasualtyPressureToSurvivalSummary(
                ApplyCompositionToSurvivalSummary(BuildSurvivalSummary(runSequence, tickStarted, success), compositionOutcome),
                compositionOutcome,
                posture);
            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            RunLootExtractionSummary extractionSummary = ApplyCompositionToExtractionSummary(
                ApplyPostureToExtractionSummary(LootExtractionResolver.Resolve(
                    _lootConfig,
                    lootSummary,
                    survivalSummary,
                    resolverSeed,
                    _config.LootExtractionRoundingPolicyId,
                    _config.LootExtractionRuleSourceId), lootSummary, posture),
                lootSummary,
                compositionOutcome);
            ApplyCasualtyPressureToExtractionSummary(extractionSummary, lootSummary, survivalSummary);
            RunLootDropRecord[] lootBreakdown = RunLootBreakdownResolver.Resolve(_lootConfig, extractionSummary);
            RunAdventurerAttractionSummary attractionSummary = ApplyCompositionToAttractionSummary(AdventurerAttractionResolver.Resolve(
                _config,
                extractionSummary,
                resolverSeed), compositionOutcome);
            RunAdventurerInterestForecastSummary forecastSummary = AdventurerInterestForecastResolver.Resolve(
                _config,
                attractionSummary,
                resolverSeed);
            RunAdventurerDemandBudgetSummary demandBudgetSummary = AdventurerDemandBudgetResolver.Resolve(
                _config,
                forecastSummary,
                resolverSeed);
            RunHeatDeltaSummary heatDeltaSummary = ApplyCompositionToHeatDeltaSummary(
                ApplyPostureToHeatDeltaSummary(RunHeatDeltaResolver.Resolve(
                    _config,
                    survivalSummary,
                    extractionSummary,
                    resolverSeed), posture),
                compositionOutcome);
            ApplyCasualtyPressureToHeatDeltaSummary(heatDeltaSummary, survivalSummary);
            RunHeatApplicationSummary heatApplicationSummary = RunHeatStateApplyResolver.Resolve(
                _config,
                runtime.Heat,
                heatDeltaSummary);
            if (heatApplicationSummary.RuleResolved)
            {
                runtime.Heat = heatApplicationSummary.HeatAfter;
            }

            return new RunOutcomeRecord
            {
                RunId = $"run-{runSequence}",
                TickStarted = tickStarted,
                Success = success,
                Score = score,
                ReasonKey = reasonKey,
                HeatAtStart = heatAtStart,
                ManaAtStart = runtime.ManaReserve,
                CrisisActiveAtStart = runtime.IsHeatCrisisActive,
                HasBreakdown = true,
                BaseChance = baseChance,
                HeatPenaltyApplied = heatPenaltyApplied,
                ManaBonusApplied = manaBonusApplied,
                CrisisPenaltyApplied = crisisPenaltyApplied,
                FinalChance = finalChance,
                SuccessThresholdUsed = successThreshold,
                FeedbackTagKeys = feedbackTagKeys,
                LootSummary = lootSummary,
                SurvivalSummary = survivalSummary,
                LootExtractionSummary = extractionSummary,
                LootBreakdown = lootBreakdown,
                AdventurerAttractionSummary = attractionSummary,
                AdventurerInterestForecastSummary = forecastSummary,
                AdventurerDemandBudgetSummary = demandBudgetSummary,
                RunHeatDeltaSummary = heatDeltaSummary,
                RunHeatApplicationSummary = heatApplicationSummary,
                CompositionOutcomeSummary = compositionOutcome,
                RunPostureId = posture?.Id
            };
        }

        private RunCompositionOutcomeSummary BuildCompositionOutcomeSummary(MvpPlacementEffectsSummary effects, double manaReserve)
        {
            MvpCompositionOutcomeTuningConfig tuning = _config.MvpCompositionOutcomeTuning;
            var summary = new RunCompositionOutcomeSummary
            {
                RuleResolved = tuning != null,
                RuleSourceId = tuning?.RuleSourceId ?? string.Empty,
                PlacementEffects = ClonePlacementEffects(effects),
                EffectiveManaReserve = manaReserve,
                GeneratedLootMultiplier = 1d,
                ExtractedLootMultiplier = 1d
            };

            if (tuning == null || effects == null || !effects.RuleResolved)
            {
                return summary;
            }

            summary.SuccessChanceDelta =
                (effects.PathCapacity * tuning.SuccessChancePerPathCapacity) -
                (effects.Danger * tuning.SuccessChancePenaltyPerDanger);
            summary.ManaReservePressureCost = Math.Max(0d, effects.ManaPressure * tuning.ManaReserveCostPerManaPressure);
            summary.EffectiveManaReserve = Math.Max(0d, manaReserve - summary.ManaReservePressureCost);
            summary.SurvivorRatioDelta =
                (effects.PathCapacity * tuning.SurvivorRatioBonusPerPathCapacity) -
                (effects.Danger * tuning.SurvivorRatioPenaltyPerDanger);
            summary.GeneratedLootMultiplier = Math.Max(0d, 1d + (effects.LootBonus * tuning.GeneratedLootMultiplierPerLootBonus));
            summary.ExtractedLootMultiplier = Math.Max(0d, 1d + (effects.LootBonus * tuning.ExtractedLootMultiplierPerLootBonus));
            summary.HeatDeltaOffset = effects.HeatPressure * tuning.HeatDeltaPerHeatPressure;
            summary.AttractionSignalBonus = Math.Max(0d, effects.Attraction * tuning.AttractionSignalPerAttraction);
            return summary;
        }

        private static MvpPlacementEffectsSummary ClonePlacementEffects(MvpPlacementEffectsSummary effects)
        {
            if (effects == null)
            {
                return new MvpPlacementEffectsSummary { RuleResolved = true, ContributingOptionIds = Array.Empty<string>(), EffectLocalizationKeys = Array.Empty<string>() };
            }

            return new MvpPlacementEffectsSummary
            {
                RuleResolved = effects.RuleResolved,
                RuleSourceId = effects.RuleSourceId,
                PathCapacity = effects.PathCapacity,
                Danger = effects.Danger,
                ManaPressure = effects.ManaPressure,
                HeatPressure = effects.HeatPressure,
                LootBonus = effects.LootBonus,
                Attraction = effects.Attraction,
                ContributingOptionIds = effects.ContributingOptionIds != null ? (string[])effects.ContributingOptionIds.Clone() : Array.Empty<string>(),
                EffectLocalizationKeys = effects.EffectLocalizationKeys != null ? (string[])effects.EffectLocalizationKeys.Clone() : Array.Empty<string>()
            };
        }

        private RunSurvivalSummary BuildSurvivalSummaryForParty(int partySize, int seed, bool success)
        {
            double ratio = success ? _config.SuccessSurvivorRatio : _config.FailureSurvivorRatio;
            int survivors = ratio < 0d || ratio > 1d ? 0 : Math.Max(0, Math.Min(partySize, (int)Math.Round(partySize * ratio)));
            return new RunSurvivalSummary { PartySize = partySize, SurvivorCount = survivors, DeathCount = partySize - survivors,
                SurvivorRatio = partySize > 0 ? (double)survivors / partySize : 0d, DeterministicSeed = seed, RuleResolved = ratio >= 0d && ratio <= 1d,
                DeterministicErrorCode = ratio >= 0d && ratio <= 1d ? (int)RunSurvivalSummaryErrorCode.None : (int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio,
                RuleSourceId = "run.survival.rule.v1", SuccessAtResolution = success };
        }

        private RunSurvivalSummary BuildSurvivalSummary(int runSequence, long tickStarted, bool success)
        {
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            int minPartySize = _config.MinPartySize;
            int maxPartySize = _config.MaxPartySize;
            int maxAllowedPartySize = _config.MaxAllowedPartySize;
            if (minPartySize < 1 || maxPartySize < minPartySize || maxAllowedPartySize < 1 || maxPartySize > maxAllowedPartySize)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidPartySizeRange,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int partySizeRange = maxPartySize - minPartySize + 1;
            int partySizeOffset = Math.Abs(seed % partySizeRange);
            int partySize = minPartySize + partySizeOffset;
            double ratio = success ? _config.SuccessSurvivorRatio : _config.FailureSurvivorRatio;
            if (ratio < 0d || ratio > 1d)
            {
                return new RunSurvivalSummary
                {
                    RuleResolved = false,
                    DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.InvalidSurvivorRatio,
                    DeterministicSeed = seed,
                    RuleSourceId = "run.survival.rule.v1",
                    SuccessAtResolution = success
                };
            }

            int survivorCount = (int)Math.Round(partySize * ratio);
            survivorCount = Math.Max(0, Math.Min(partySize, survivorCount));

            return new RunSurvivalSummary
            {
                PartySize = partySize,
                SurvivorCount = survivorCount,
                DeathCount = partySize - survivorCount,
                SurvivorRatio = partySize > 0 ? (double)survivorCount / partySize : 0d,
                DeterministicSeed = seed,
                RuleResolved = true,
                DeterministicErrorCode = (int)RunSurvivalSummaryErrorCode.None,
                RuleSourceId = "run.survival.rule.v1",
                SuccessAtResolution = success
            };
        }

        private RunLootSummary BuildLootSummary(int runSequence, long tickStarted)
        {
            if (string.IsNullOrEmpty(_lootTableId))
            {
                return null;
            }

            int resolverSeed = ComputeResolverSeed(runSequence, tickStarted);
            LootRollResolverResult result = LootRollResolver.Resolve(_lootConfig, _lootTableId, resolverSeed);

            return new RunLootSummary
            {
                LootTableId = _lootTableId,
                ResolverSeed = resolverSeed,
                ResolverSuccess = result.success,
                ResolverErrorCode = (int)result.errorCode,
                RollCount = result.rollCount,
                GeneratedItemIds = result.generatedItemIds != null ? new System.Collections.Generic.List<string>(result.generatedItemIds).ToArray() : Array.Empty<string>(),
                TotalGeneratedWorldValue = result.totalGeneratedWorldValue,
                TotalGeneratedReserveCost = result.totalGeneratedReserveCost,
                TotalGeneratedTradeableWorldValue = result.totalGeneratedTradeableWorldValue
            };
        }

        private RunLootSummary ApplyPostureToLootSummary(RunLootSummary summary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.ResolverSuccess)
            {
                return summary;
            }

            summary.TotalGeneratedWorldValue = ScaleToInt(summary.TotalGeneratedWorldValue, posture.GeneratedLootWorldValueMultiplier);
            return summary;
        }

        private RunLootSummary ApplyCompositionToLootSummary(RunLootSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.ResolverSuccess)
            {
                return summary;
            }

            summary.TotalGeneratedWorldValue = ScaleToInt(summary.TotalGeneratedWorldValue, composition.GeneratedLootMultiplier);
            summary.TotalGeneratedTradeableWorldValue = Math.Min(
                summary.TotalGeneratedWorldValue,
                ScaleToInt(summary.TotalGeneratedTradeableWorldValue, composition.GeneratedLootMultiplier));
            return summary;
        }

        private RunSurvivalSummary ApplyCompositionToSurvivalSummary(RunSurvivalSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved || summary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None)
            {
                return summary;
            }

            double ratio = Math.Max(0d, Math.Min(1d, summary.SurvivorRatio + composition.SurvivorRatioDelta));
            int survivorCount = Math.Max(0, Math.Min(summary.PartySize, (int)Math.Round(summary.PartySize * ratio)));
            summary.SurvivorCount = survivorCount;
            summary.DeathCount = summary.PartySize - survivorCount;
            summary.SurvivorRatio = summary.PartySize > 0 ? (double)survivorCount / summary.PartySize : 0d;
            return summary;
        }

        private RunSurvivalSummary ApplyCasualtyPressureToSurvivalSummary(RunSurvivalSummary summary, RunCompositionOutcomeSummary composition, RunPostureConfig posture)
        {
            if (summary == null || composition == null || !summary.RuleResolved || summary.DeterministicErrorCode != (int)RunSurvivalSummaryErrorCode.None || string.IsNullOrWhiteSpace(_config.CasualtyPressureRuleSourceId))
            {
                return summary;
            }

            MvpPlacementEffectsSummary effects = composition.PlacementEffects;
            double rawPressure =
                ((effects?.Danger ?? 0) * _config.CasualtyPressurePerDanger) -
                ((effects?.PathCapacity ?? 0) * _config.CasualtyPressureReductionPerPathCapacity) +
                ((effects?.ManaPressure ?? 0) * _config.CasualtyPressurePerManaPressure);
            double multiplier = ResolveCasualtyPressureMultiplier(posture);
            double pressure = Math.Max(_config.CasualtyPressureMinimum, Math.Min(_config.CasualtyPressureMaximum, rawPressure * multiplier));
            if (double.IsNaN(pressure) || double.IsInfinity(pressure))
            {
                return summary;
            }

            int originalDeathCount = summary.DeathCount;
            int pressureDeaths = Math.Max(0, Math.Min(summary.PartySize, (int)Math.Round(summary.PartySize * pressure)));
            if (pressure < _config.PartyWipeCasualtyPressureThreshold && pressureDeaths >= summary.PartySize && summary.PartySize > 0)
            {
                pressureDeaths = summary.PartySize - 1;
            }

            int deathCount = Math.Max(summary.DeathCount, pressureDeaths);
            summary.DeathCount = Math.Max(0, Math.Min(summary.PartySize, deathCount));
            summary.SurvivorCount = Math.Max(0, summary.PartySize - summary.DeathCount);
            summary.SurvivorRatio = summary.PartySize > 0 ? (double)summary.SurvivorCount / summary.PartySize : 0d;
            summary.CasualtyPressure = pressure;
            int pressureCasualties = Math.Max(0, summary.DeathCount - originalDeathCount);
            summary.CasualtyLootExtractionPenalty = Math.Max(0d, pressureCasualties * _config.CasualtyLootExtractionPenaltyPerCasualty);
            summary.CasualtyHeatDelta = Math.Max(0d, pressureCasualties * _config.CasualtyHeatDeltaPerCasualty);
            summary.RuleSourceId = _config.CasualtyPressureRuleSourceId;
            return summary;
        }

        private double ResolveCasualtyPressureMultiplier(RunPostureConfig posture)
        {
            string postureId = posture?.Id;
            if (string.Equals(postureId, RunPostureResolver.CautiousId, StringComparison.Ordinal)) return _config.CautiousCasualtyPressureMultiplier;
            if (string.Equals(postureId, RunPostureResolver.GreedyId, StringComparison.Ordinal)) return _config.GreedyCasualtyPressureMultiplier;
            return _config.BalancedCasualtyPressureMultiplier;
        }

        private RunLootExtractionSummary ApplyPostureToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.RuleResolved)
            {
                return summary;
            }

            int generatedWorldValue = Math.Max(0, lootSummary?.TotalGeneratedWorldValue ?? 0);
            summary.TotalExtractedWorldValue = Math.Min(
                generatedWorldValue,
                ScaleToInt(summary.TotalExtractedWorldValue, posture.ExtractedLootWorldValueMultiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(
                summary.TotalExtractedWorldValue,
                ScaleToInt(summary.TotalExtractedTradeableWorldValue, posture.ExtractedLootWorldValueMultiplier));
            return summary;
        }

        private RunLootExtractionSummary ApplyCompositionToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            int generatedWorldValue = Math.Max(0, lootSummary?.TotalGeneratedWorldValue ?? 0);
            summary.TotalExtractedWorldValue = Math.Min(
                generatedWorldValue,
                ScaleToInt(summary.TotalExtractedWorldValue, composition.ExtractedLootMultiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(
                summary.TotalExtractedWorldValue,
                ScaleToInt(summary.TotalExtractedTradeableWorldValue, composition.ExtractedLootMultiplier));
            return summary;
        }

        private void ApplyCasualtyPressureToExtractionSummary(RunLootExtractionSummary summary, RunLootSummary lootSummary, RunSurvivalSummary survival)
        {
            if (summary == null || lootSummary == null || survival == null || !summary.RuleResolved || survival.DeathCount <= 0 || survival.CasualtyLootExtractionPenalty <= 0d)
            {
                return;
            }

            double multiplier = Math.Max(0d, 1d - survival.CasualtyLootExtractionPenalty);
            summary.TotalExtractedWorldValue = Math.Min(lootSummary.TotalGeneratedWorldValue, ScaleToInt(summary.TotalExtractedWorldValue, multiplier));
            summary.TotalExtractedTradeableWorldValue = Math.Min(summary.TotalExtractedWorldValue, ScaleToInt(summary.TotalExtractedTradeableWorldValue, multiplier));
        }

        private RunAdventurerAttractionSummary ApplyCompositionToAttractionSummary(RunAdventurerAttractionSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            summary.AttractionSignalValue += composition.AttractionSignalBonus;
            return summary;
        }

        private RunHeatDeltaSummary ApplyPostureToHeatDeltaSummary(RunHeatDeltaSummary summary, RunPostureConfig posture)
        {
            if (summary == null || posture == null || !summary.RuleResolved)
            {
                return summary;
            }

            double adjusted = summary.FinalHeatDelta + posture.HeatDeltaOffset;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return summary;
            }

            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
            return summary;
        }

        private RunHeatDeltaSummary ApplyCompositionToHeatDeltaSummary(RunHeatDeltaSummary summary, RunCompositionOutcomeSummary composition)
        {
            if (summary == null || composition == null || !summary.RuleResolved)
            {
                return summary;
            }

            double adjusted = summary.FinalHeatDelta + composition.HeatDeltaOffset;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return summary;
            }

            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
            return summary;
        }

        private void ApplyCasualtyPressureToHeatDeltaSummary(RunHeatDeltaSummary summary, RunSurvivalSummary survival)
        {
            if (summary == null || survival == null || !summary.RuleResolved || survival.CasualtyHeatDelta <= 0d)
            {
                return;
            }

            double adjusted = summary.FinalHeatDelta + survival.CasualtyHeatDelta;
            if (double.IsNaN(adjusted) || double.IsInfinity(adjusted))
            {
                return;
            }

            summary.DeathHeatDelta += survival.CasualtyHeatDelta;
            summary.FinalHeatDelta = Math.Max(_config.RunHeatDeltaMinimum, Math.Min(_config.RunHeatDeltaMaximum, adjusted));
        }

        private static int ScaleToInt(int value, double multiplier)
        {
            if (value <= 0 || multiplier <= 0d)
            {
                return 0;
            }

            double scaled = value * multiplier;
            if (double.IsNaN(scaled) || double.IsInfinity(scaled))
            {
                return value;
            }

            if (scaled >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Math.Max(0, (int)Math.Round(scaled));
        }

        private static int ComputeResolverSeed(int runSequence, long tickStarted)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + runSequence;
                int tickLow = (int)(tickStarted & 0xFFFFFFFFL);
                int tickHigh = (int)((tickStarted >> 32) & 0xFFFFFFFFL);
                hash = (hash * 31) + tickLow;
                hash = (hash * 31) + tickHigh;
                return hash;
            }
        }

        private string[] BuildFeedbackTagKeys(StructureRuntimeState runtime, bool success, RunCompositionOutcomeSummary composition)
        {
            System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>(6);
            tags.Add(success ? "run.feedback.success" : "run.feedback.failure");

            if (runtime.Heat >= _config.HighHeatFeedbackThreshold)
            {
                tags.Add("run.feedback.high_heat");
            }

            double effectiveManaReserve = composition != null ? composition.EffectiveManaReserve : runtime.ManaReserve;
            if (effectiveManaReserve <= _config.LowManaFeedbackThreshold)
            {
                tags.Add("run.feedback.low_mana");
            }

            if (runtime.IsHeatCrisisActive)
            {
                tags.Add("run.feedback.heat_crisis");
            }

            if (effectiveManaReserve >= _config.StrongManaReserveFeedbackThreshold)
            {
                tags.Add("run.feedback.strong_mana_reserve");
            }

            return tags.ToArray();
        }
    }
}
