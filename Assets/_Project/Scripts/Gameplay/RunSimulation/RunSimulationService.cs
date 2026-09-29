using System;
using System.Linq;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public sealed partial class RunSimulationService
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
            => SimulateRoute(runtime, tickStarted, runSequence, postureId, route, null);

        internal RunOutcomeRecord SimulateRoute(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId, MvpOrderedRouteRoom[] route, BranchTraversal traversal)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            route = (route ?? Array.Empty<MvpOrderedRouteRoom>()).OrderBy(r => r.FloorIndex).ThenBy(r => r.RoomIndex).ToArray();
            ValidateAssignments(route);
            if (traversal != null && traversal.Forks.Any(f => f.Assignments.Any(a =>
                a.CategoryId != MvpDungeonPlacementIds.TrapCategoryId && a.CategoryId != MvpDungeonPlacementIds.LootNodeCategoryId)))
                throw new ArgumentException(PhaseFiveBRouteProjection.InvalidRoute);
            // Preserve the existing zero/one-room compatibility outcome when there are no
            // assignments to execute. SimulateOnce now uses the same authoritative roster and HP
            // model, so this path cannot reintroduce aggregate casualty authority.
            if (traversal == null && (route.Length == 0 || (route.Length == 1 && route[0].OrderedAssignments().Length == 0)))
            {
                MvpPlacementEffectsSummary effects = route.Length == 1
                    ? MvpPlacementEffectsResolver.ResolvePlacements(route[0].ToOrderedPlacements(), _config)
                    : null;
                RunOutcomeRecord compatible = SimulateOnce(runtime, tickStarted, runSequence, postureId, effects);
                compatible.ConfiguredRoutePlacementEffects = ClonePlacementEffects(effects);
                compatible.ReachedRoutePlacementEffects = ClonePlacementEffects(effects);
                compatible.ClearedRewardPlacementEffects = compatible.Success
                    ? ClonePlacementEffects(effects)
                    : EmptyPlacementEffects();
                compatible.ConfiguredRoomCount = route.Length;
                AddCompatibleRoomMetadata(compatible, route);
                return compatible;
            }

            RunParty party = RunPartyGenerator.Create(_config.PhaseFiveB, RunId(runSequence));
            var events = new List<RunEncounterEvent>();

            RunPostureConfig posture = RunPostureResolver.Resolve(_config, postureId);
            double heatAtStart = runtime.Heat;
            double manaAtStart = runtime.ManaReserve;
            int seed = ComputeResolverSeed(runSequence, tickStarted);
            RunSurvivalSummary partyRoll = party.DeriveSurvival(true);
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
                int entrants = currentSurvivors;
                // Preserve the pre-Phase-5B one-room loot identity; multi-room identity is unchanged.
                int roomSeed = route.Length == 1 ? seed : DeriveRoomSeed(seed, routeRoom.FloorIndex, routeRoom.RoomIndex);
                RunRoomAssignment[] assignments = routeRoom.OrderedAssignments();
                if (assignments.Length == 0 && !(traversal != null && route.Length == 1))
                {
                    AddPlacementEffects(reachedEffects, localEffects);
                    rooms.Add(BuildEmptyRoomSummary(routeRoom, entrants, localEffects, roomSeed, generatedValue + (traversal?.Value ?? 0)));
                    if (traversal != null)
                    {
                        VisitFork(traversal, routeRoom, party, false, events, posture, manaAtStart);
                        currentSurvivors = party.ActiveCount;
                        if (party.IsWiped) break;
                    }
                    continue;
                }

                hasActiveEncounter = true;
                RunCompositionOutcomeSummary composition = BuildCompositionOutcomeSummary(localEffects, manaAtStart);
                finalHeatPenalty = runtime.Heat * _config.HeatPenaltyPerPoint;
                finalManaBonus = composition.EffectiveManaReserve * _config.ManaReserveBonusPerPoint;
                finalCrisisPenalty = runtime.IsHeatCrisisActive ? _config.CrisisFailurePenalty : 0d;
                finalChance = Math.Max(0d, Math.Min(1d, _config.BaseSuccessChance - finalHeatPenalty + finalManaBonus - finalCrisisPenalty + composition.SuccessChanceDelta));
                bool cleared = finalChance >= _config.SuccessThreshold;
                double pressure = ResolveCasualtyPressure(composition, posture);
                var executed = new List<MvpDungeonPlacementEntry>();
                if (routeRoom.IncludeRoomPlacement)
                    executed.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.RoomCategoryId, routeRoom.RoomOptionId, 0));
                foreach (RunRoomAssignment assignment in assignments)
                {
                    if (party.IsWiped) break;
                    if (assignment.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId) continue;
                    executed.Add(new MvpDungeonPlacementEntry(assignment.CategoryId, assignment.OptionId, executed.Count));
                    events.Add(RunEncounterResolver.Resolve(party, _config.PhaseFiveB, assignment,
                        routeRoom.FloorIndex, routeRoom.RoomIndex, pressure));
                }
                if (party.IsWiped)
                {
                    // Unreached later assignments cannot contribute loot, attraction, or Heat effects.
                    // Severity remains the one room-level value calculated above.
                    localEffects = MvpPlacementEffectsResolver.ResolvePlacements(executed.ToArray(), _config);
                    composition = BuildCompositionOutcomeSummary(localEffects, manaAtStart);
                }
                AddPlacementEffects(reachedEffects, localEffects);
                currentSurvivors = party.ActiveCount;
                RunSurvivalSummary roomSurvival = party.DeriveSurvival(cleared);
                // Room evidence is the delta of the same live roster, not a second casualty roll.
                roomSurvival.PartySize = entrants;
                roomSurvival.DeathCount = entrants - currentSurvivors;
                roomSurvival.SurvivorRatio = (double)currentSurvivors / entrants;
                ApplyCasualtyEvidence(roomSurvival, pressure);

                RunLootSummary roomLoot = null;
                if (cleared && !party.IsWiped)
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
                    CarriedLootValueAfterRoom = generatedValue + (traversal?.Value ?? 0), LocalHeatPressureDelta = composition.HeatDeltaOffset,
                    CasualtyPressureHeatDelta = roomSurvival.CasualtyHeatDelta, CasualtyPressure = roomSurvival.CasualtyPressure,
                    CasualtyLootExtractionPenalty = roomSurvival.CasualtyLootExtractionPenalty, ManaPressureCost = composition.ManaReservePressureCost, DeterministicSeed = roomSeed, RuleSourceId = composition.RuleSourceId
                });
                finalComposition = composition;
                finalSuccess = cleared;
                if (traversal != null)
                {
                    VisitFork(traversal, routeRoom, party, stopped, events, posture, manaAtStart);
                    currentSurvivors = party.ActiveCount;
                    stopped |= party.IsWiped;
                }
                if (stopped) break;
            }

            if (traversal != null)
            {
                generatedItems.AddRange(traversal.Items); generatedValue += traversal.Value;
                generatedReserve += traversal.Reserve; generatedTradeable += traversal.Tradeable; rollCount += traversal.Rolls;
                if (traversal.HasEncounter) AddPlacementEffects(reachedEffects, traversal.Reached);
                if (traversal.Reward.ContributingOptionIds.Length != 0) AddPlacementEffects(clearedRewardEffects, traversal.Reward);
                hasActiveEncounter |= traversal.HasEncounter;
                if (!traversal.LootSuccess) { lootResolverSuccess = false; lootResolverErrorCode = traversal.LootError; }
            }

            if (!hasActiveEncounter)
            {
                RunOutcomeRecord empty = BuildNoEncounterRouteOutcome(runtime, tickStarted, runSequence, posture, heatAtStart, manaAtStart, partyRoll, rooms, configuredEffects, reachedEffects);
                empty.Party = party; empty.EncounterEvents = events.ToArray();
                return empty;
            }

            var loot = new RunLootSummary { LootTableId = _lootTableId, ResolverSeed = seed, ResolverSuccess = lootResolverSuccess, ResolverErrorCode = lootResolverErrorCode, RollCount = rollCount,
                GeneratedItemIds = generatedItems.ToArray(), TotalGeneratedWorldValue = generatedValue,
                TotalGeneratedReserveCost = generatedReserve, TotalGeneratedTradeableWorldValue = Math.Min(generatedValue, generatedTradeable) };
            var survival = party.DeriveSurvival(finalSuccess);
            survival.CasualtyPressure = Math.Max(rooms.Select(r => r.CasualtyPressure).DefaultIfEmpty().Max(), traversal?.MaximumPressure ?? 0d);
            survival.CasualtyLootExtractionPenalty = Math.Min(1d, rooms.Sum(r => r.CasualtyLootExtractionPenalty) + (traversal?.CasualtyPenalty ?? 0d));
            survival.CasualtyHeatDelta = rooms.Sum(r => r.CasualtyPressureHeatDelta) + (traversal?.CasualtyHeat ?? 0d);
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
            bool partyWiped = currentSurvivors <= 0;
            bool allRoomsReached = rooms.Count == route.Length;
            bool allReachedRoomsCleared = rooms.Count > 0 && rooms.TrueForAll(room => room.Cleared);
            bool fullClear = !partyWiped && allRoomsReached && allReachedRoomsCleared;
            string routeKey = partyWiped ? RouteWipedKey : fullClear ? RouteClearedKey :
                rooms[rooms.Count - 1].RoomIndex == 0 ? RouteStoppedRoomOneKey :
                rooms[rooms.Count - 1].RoomIndex == 1 ? RouteStoppedRoomTwoKey : RouteRetreatedKey;
            return new RunOutcomeRecord {
                Party = party, EncounterEvents = events.ToArray(),
                RunId = RunId(runSequence), TickStarted = tickStarted, Success = fullClear,
                Score = fullClear ? _config.BaseScoreOnSuccess + (int)Math.Round(aggregateComposition.EffectiveManaReserve * _config.ScorePerManaPoint) : 0,
                ReasonKey = partyWiped ? PartyWipedReasonKey : fullClear ? "run.reason.success" : (runtime.IsHeatCrisisActive ? "run.reason.crisis_failure" : "run.reason.failed_threshold"),
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
            return new RunOutcomeRecord { RunId = $"run-{runSequence}", TickStarted = tickStarted, Success = false, Score = 0, ReasonKey = NoEncounterReasonKey,
                HeatAtStart = heatAtStart, ManaAtStart = manaAtStart, CrisisActiveAtStart = runtime.IsHeatCrisisActive, HasBreakdown = true, BaseChance = _config.BaseSuccessChance, FinalChance = _config.BaseSuccessChance, SuccessThresholdUsed = _config.SuccessThreshold,
                FeedbackTagKeys = BuildFeedbackTagKeys(runtime, false, BuildCompositionOutcomeSummary(EmptyPlacementEffects(), manaAtStart)), LootSummary = loot, LootExtractionSummary = extraction, LootBreakdown = Array.Empty<RunLootDropRecord>(),
                SurvivalSummary = party, RunHeatDeltaSummary = heatDelta, RunHeatApplicationSummary = heatApplication, CompositionOutcomeSummary = BuildCompositionOutcomeSummary(reached, manaAtStart), RunPostureId = posture?.Id,
                RoomResolutions = rooms.ToArray(), HighestRoomReached = rooms.Count == 0 ? -1 : rooms[rooms.Count - 1].RoomIndex, ReachedRoomCount = rooms.Count, ConfiguredRoomCount = rooms.Count, ClearedRoomCount = rooms.Count, FinalRouteOutcomeKey = RouteNoEncounterKey,
                ConfiguredRoutePlacementEffects = configured, ReachedRoutePlacementEffects = reached, ClearedRewardPlacementEffects = EmptyPlacementEffects() };
        }

        public const string RouteClearedKey = "run.route.outcome.cleared";
        public const string RouteStoppedRoomOneKey = "run.route.outcome.stopped_room_one";
        public const string RouteStoppedRoomTwoKey = "run.route.outcome.stopped_room_two";
        public const string RouteWipedKey = "run.route.outcome.wiped";
        public const string RouteRetreatedKey = "run.route.outcome.retreated";
        public const string RouteNoEncounterKey = "run.route.outcome.no_encounter";
        public const string PartyWipedReasonKey = "run.reason.party_wiped";
        public const string NoEncounterReasonKey = "run.reason.no_encounter";

        private static void AddCompatibleRoomMetadata(RunOutcomeRecord outcome, MvpOrderedRouteRoom[] route)
        {
            if (route == null || route.Length == 0) return;
            outcome.RoomResolutions = new[] {
                BuildRoomSummary(route[0], outcome, outcome.SurvivalSummary?.PartySize ?? 0, !outcome.Success)
            };
            outcome.ReachedRoomCount = 1;
            outcome.ClearedRoomCount = outcome.Success ? 1 : 0;
            outcome.HighestRoomReached = route[0].RoomIndex;
            outcome.FinalRouteOutcomeKey = outcome.Success
                ? RouteClearedKey
                : (outcome.SurvivalSummary?.SurvivorCount ?? 0) == 0 ? RouteWipedKey : RouteRetreatedKey;
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
                LocalPlacementEffects = outcome.CompositionOutcomeSummary?.PlacementEffects,
                GeneratedLootValue = outcome.Success ? outcome.LootSummary?.TotalGeneratedWorldValue ?? 0 : 0,
                CarriedLootValueAfterRoom = outcome.Success ? outcome.LootSummary?.TotalGeneratedWorldValue ?? 0 : 0,
                LocalHeatPressureDelta = outcome.CompositionOutcomeSummary?.HeatDeltaOffset ?? 0d,
                CasualtyPressureHeatDelta = survival?.CasualtyHeatDelta ?? 0d,
                CasualtyPressure = survival?.CasualtyPressure ?? 0d,
                CasualtyLootExtractionPenalty = survival?.CasualtyLootExtractionPenalty ?? 0d,
                ManaPressureCost = outcome.CompositionOutcomeSummary?.ManaReservePressureCost ?? 0d,
                DeterministicSeed = survival?.DeterministicSeed ?? 0,
                RuleSourceId = outcome.CompositionOutcomeSummary?.RuleSourceId };
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

        public RunOutcomeRecord SimulateOnce(StructureRuntimeState runtime, long tickStarted, int runSequence, string postureId, MvpPlacementEffectsSummary placementEffects)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            RunParty party = RunPartyGenerator.Create(_config.PhaseFiveB, RunId(runSequence));
            var events = new List<RunEncounterEvent>();

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

            // Compatibility callers supply aggregate placement evidence, not canonical room assignments.
            // Resolve its available content IDs through the same HP authority; absent content cannot kill.
            var compatibilityRoom = new MvpOrderedRouteRoom {
                Assignments = (placementEffects?.ContributingOptionIds ?? Array.Empty<string>())
                    .Select((id, index) => new { id, index, profile = _config.PhaseFiveB.DamageProfiles.SingleOrDefault(p => p.OptionId == id) })
                    .Where(x => x.profile != null).Select(x => new RunRoomAssignment {
                        AssignmentId = "legacy.assignment." + x.index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        OptionId = x.id, CategoryId = x.profile.CategoryId, Sequence = x.index }).ToArray()
            };
            double pressure = ResolveCasualtyPressure(compositionOutcome, posture);
            foreach (RunRoomAssignment assignment in compatibilityRoom.OrderedAssignments())
            {
                if (party.IsWiped) break;
                events.Add(RunEncounterResolver.Resolve(party, _config.PhaseFiveB, assignment, 0, 0, pressure));
            }
            if (party.IsWiped) { success = false; score = 0; reasonKey = PartyWipedReasonKey; }
            string[] feedbackTagKeys = BuildFeedbackTagKeys(runtime, success, compositionOutcome);
            RunLootSummary lootSummary = party.IsWiped ? new RunLootSummary { ResolverSuccess = true } : ApplyCompositionToLootSummary(
                ApplyPostureToLootSummary(BuildLootSummary(runSequence, tickStarted), posture),
                compositionOutcome);
            RunSurvivalSummary survivalSummary = party.DeriveSurvival(success);
            ApplyCasualtyEvidence(survivalSummary, pressure);
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
                Party = party, EncounterEvents = events.ToArray(),
                RunId = RunId(runSequence),
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
            // Derived legacy diagnostic only. Never used to mutate HP or casualty counts.
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

        private static string RunId(int sequence) => "run-" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private void ValidateAssignments(MvpOrderedRouteRoom[] route)
        {
            if (route.GroupBy(r => new { r.FloorIndex, r.RoomIndex }).Any(g => g.Count() != 1))
                throw new ArgumentException("run.phase5b.invalid_route");
            foreach (MvpOrderedRouteRoom room in route)
            {
                RunRoomAssignment[] assignments = room.OrderedAssignments();
                if (assignments.Any(a => a == null || string.IsNullOrWhiteSpace(a.AssignmentId)) ||
                    assignments.Select(a => a.AssignmentId).Distinct(StringComparer.Ordinal).Count() != assignments.Length)
                    throw new ArgumentException("run.phase5b.invalid_assignment");
                foreach (RunRoomAssignment a in assignments)
                {
                    if (a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId) continue;
                    if (!_config.PhaseFiveB.DamageProfiles.Any(p => p.OptionId == a.OptionId && p.CategoryId == a.CategoryId))
                        throw new ArgumentException(PhaseFiveBConfigValidation.InvalidConfiguration);
                }
            }
        }

        private double ResolveCasualtyPressure(RunCompositionOutcomeSummary composition, RunPostureConfig posture)
        {
            MvpPlacementEffectsSummary effects = composition.PlacementEffects;
            double raw = ((effects?.Danger ?? 0) * _config.CasualtyPressurePerDanger) -
                ((effects?.PathCapacity ?? 0) * _config.CasualtyPressureReductionPerPathCapacity) +
                ((effects?.ManaPressure ?? 0) * _config.CasualtyPressurePerManaPressure);
            double pressure = Math.Max(_config.CasualtyPressureMinimum,
                Math.Min(_config.CasualtyPressureMaximum, raw * ResolveCasualtyPressureMultiplier(posture)));
            if (double.IsNaN(pressure) || double.IsInfinity(pressure))
                throw new ArgumentException(PhaseFiveBConfigValidation.InvalidConfiguration);
            return pressure;
        }

        private void ApplyCasualtyEvidence(RunSurvivalSummary summary, double pressure)
        {
            // Pressure is severity only. Existing per-casualty costs now consume actual HP deaths.
            summary.CasualtyPressure = pressure;
            summary.CasualtyLootExtractionPenalty = Math.Max(0d, summary.DeathCount * _config.CasualtyLootExtractionPenaltyPerCasualty);
            summary.CasualtyHeatDelta = Math.Max(0d, summary.DeathCount * _config.CasualtyHeatDeltaPerCasualty);
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
