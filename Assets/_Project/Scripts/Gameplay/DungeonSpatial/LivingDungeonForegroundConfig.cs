using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Versioned production policy; B2's qualification profile remains independent.
    public sealed class LivingDungeonForegroundConfig
    {
        internal const string ProfileId = "living.foreground.c2.v1";
        internal const string TwoFloorProfileId = "living.foreground.c2.v2";
        internal static bool IsProduction(string id) => id == ProfileId || id == TwoFloorProfileId;
        internal const string Invalid = "living.foreground.invalid_configuration";
        [Serializable] private sealed class Data
        {
            public int FormatVersion, UnitsPerSecond, GameplayPartyCapacity, MaximumBoundariesPerPump, MaximumEventsPerBoundary;
            public int MaximumRetained, MaximumCheckpointBytes, MaximumBaseSaveBytes, MaximumOwnerBytes;
            public long MaximumPartyReplay, MaximumTotalReplay, MaximumCombatWork;
            public long UnitsPerTileEdge, MonsterInteractionUnits, TrapInteractionUnits, LootInteractionUnits, FloorTransferUnits;
            public long MinimumArrivalUnits, MaximumArrivalUnits, LowInterval, BuildingInterval, SteadyInterval, HeavyInterval, ChurnInterval;
            public string Profile;
        }
        private readonly Data data;
        internal string Id => data.Profile;
        internal bool TwoFloor => Id == TwoFloorProfileId;
        public int UnitsPerSecond => data.UnitsPerSecond;
        internal int Capacity => data.GameplayPartyCapacity;
        internal int MaximumBoundaries => data.MaximumBoundariesPerPump;
        internal int MaximumBoundaryEvents => data.MaximumEventsPerBoundary;
        internal long MinimumArrival => data.MinimumArrivalUnits;
        internal RunLogicalTiming Timing { get; }
        private LivingDungeonForegroundConfig(Data value)
        {
            data = value;
            if (data == null || !IsProduction(data.Profile) || data.FormatVersion != (TwoFloor ? 2 : 1) || data.UnitsPerSecond <= 0 ||
                data.GameplayPartyCapacity != (TwoFloor ? 2 : 1) || TwoFloor && data.FloorTransferUnits <= 0 ||
                data.MaximumCombatWork <= 0 || data.MaximumBoundariesPerPump <= 0 || data.MaximumEventsPerBoundary <= 0 ||
                data.MaximumRetained < data.GameplayPartyCapacity || data.MinimumArrivalUnits <= 0 || data.MaximumArrivalUnits < data.MinimumArrivalUnits ||
                data.LowInterval < data.BuildingInterval || data.BuildingInterval < data.SteadyInterval || data.SteadyInterval < data.HeavyInterval ||
                new[] { data.LowInterval, data.BuildingInterval, data.SteadyInterval, data.HeavyInterval, data.ChurnInterval }
                    .Any(n => n < data.MinimumArrivalUnits || n > data.MaximumArrivalUnits) ||
                data.UnitsPerTileEdge <= 0 || data.MonsterInteractionUnits <= 0 || data.TrapInteractionUnits <= 0 || data.LootInteractionUnits <= 0) throw new ArgumentException(Invalid);
            // Transfers are not admitted in this single-floor contract. Zero is a
            // structural unused value, not an invented production descent duration.
            Timing = new RunLogicalTiming(data.UnitsPerTileEdge, data.MonsterInteractionUnits, data.TrapInteractionUnits, data.LootInteractionUnits, data.FloorTransferUnits);
            CreateProfile(default);
        }
        public static LivingDungeonForegroundConfig Parse(byte[] bytes)
        {
            var budget = new SpatialSerializedInputLimits(4096, 64, 32, 2048, 8);
            var issues = new SpatialIssueCollector(8);
            // Freeze the authored format's order explicitly. Reflection declaration
            // order is neither the JSON schema nor a cross-runtime contract.
            string[] names = { "FormatVersion", "Profile", "UnitsPerSecond", "GameplayPartyCapacity",
                "UnitsPerTileEdge", "MonsterInteractionUnits", "TrapInteractionUnits", "LootInteractionUnits",
                "MinimumArrivalUnits", "MaximumArrivalUnits", "LowInterval", "BuildingInterval", "SteadyInterval",
                "HeavyInterval", "ChurnInterval", "MaximumBoundariesPerPump", "MaximumEventsPerBoundary", "MaximumRetained",
                "MaximumCheckpointBytes", "MaximumPartyReplay", "MaximumTotalReplay", "MaximumCombatWork", "MaximumBaseSaveBytes", "MaximumOwnerBytes" };
            if (!ContractJson.TryParse(bytes, budget, issues, out var root, true)) throw new ArgumentException(Invalid);
            if (root.Fields.Any(f => f.Key == "Profile" && f.Value.Text == TwoFloorProfileId))
                names = names.Take(8).Concat(new[] { "FloorTransferUnits" }).Concat(names.Skip(8)).ToArray();
            if (!ContractJson.ValidateShape(root, names, issues) ||
                root.Fields.Any(f => f.Key == nameof(Data.Profile) ? f.Value.Kind != ContractJsonKind.String :
                    typeof(Data).GetField(f.Key).FieldType == typeof(int) ? !ContractJson.Int(f.Value, out _) : !ContractJson.Long(f.Value, out _)))
                throw new ArgumentException(Invalid);
            return new LivingDungeonForegroundConfig(JsonUtility.FromJson<Data>(System.Text.Encoding.UTF8.GetString(bytes)));
        }
        public static LivingDungeonForegroundConfig Load()
        {
            var asset = Resources.Load<TextAsset>("living_dungeon_foreground");
            if (asset == null) throw new ArgumentException(Invalid);
            return Parse(asset.bytes);
        }
        internal static LivingDungeonForegroundConfig LoadTwoFloor()
        {
            var asset = Resources.Load<TextAsset>("living_dungeon_foreground_v2");
            if (asset == null) throw new ArgumentException(Invalid);
            return Parse(asset.bytes);
        }
        internal LivingDungeonProfile CreateProfile(SpatialSerializedInputLimits baseBudget) =>
            new LivingDungeonProfile(Id, data.GameplayPartyCapacity, data.MaximumRetained, data.MaximumCheckpointBytes,
                data.MaximumPartyReplay, data.MaximumTotalReplay, data.MaximumBaseSaveBytes, data.MaximumOwnerBytes,
                Timing, RunPostureResolver.BalancedId, baseBudget, data.MaximumCombatWork);
        internal long Interval(string band) => band == AdventurerTrafficPressureResolver.BandLowId ? data.LowInterval :
            band == AdventurerTrafficPressureResolver.BandBuildingId ? data.BuildingInterval :
            band == AdventurerTrafficPressureResolver.BandSteadyId ? data.SteadyInterval :
            band == AdventurerTrafficPressureResolver.BandHeavyId ? data.HeavyInterval :
            band == AdventurerTrafficPressureResolver.BandDangerousChurnId ? data.ChurnInterval : 0;
        internal bool Matches(RunExecution e) => e?.Combat?.Kernel.RulesId == MonsterRecoveryRules.Executable &&
            e.Snapshot?.Floors.Count >= 1 && e.Snapshot.Floors.Count <= (TwoFloor ? 2 : 1) && e.Snapshot.Floors[0].FloorIndex == 0 &&
            e.TransferContract == TwoFloor &&
            e.Snapshot.PostureId == RunPostureResolver.BalancedId && e.TimingMatches(Timing);
    }
}
