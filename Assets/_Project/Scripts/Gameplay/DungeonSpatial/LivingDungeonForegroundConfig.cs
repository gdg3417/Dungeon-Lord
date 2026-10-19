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
        internal const string Invalid = "living.foreground.invalid_configuration";
        [Serializable] private sealed class Data
        {
            public int FormatVersion, UnitsPerSecond, GameplayPartyCapacity, MaximumBoundariesPerPump, MaximumEventsPerBoundary;
            public int MaximumRetained, MaximumCheckpointBytes, MaximumBaseSaveBytes, MaximumOwnerBytes;
            public long MaximumPartyReplay, MaximumTotalReplay, MaximumCombatWork;
            public long UnitsPerTileEdge, MonsterInteractionUnits, TrapInteractionUnits, LootInteractionUnits;
            public long MinimumArrivalUnits, MaximumArrivalUnits, LowInterval, BuildingInterval, SteadyInterval, HeavyInterval, ChurnInterval;
            public string Profile;
        }
        private readonly Data data;
        public int UnitsPerSecond => data.UnitsPerSecond;
        internal int Capacity => data.GameplayPartyCapacity;
        internal int MaximumBoundaries => data.MaximumBoundariesPerPump;
        internal int MaximumBoundaryEvents => data.MaximumEventsPerBoundary;
        internal long MinimumArrival => data.MinimumArrivalUnits;
        internal RunLogicalTiming Timing { get; }
        private LivingDungeonForegroundConfig(Data value)
        {
            data = value;
            if (data == null || data.FormatVersion != 1 || data.Profile != ProfileId || data.UnitsPerSecond <= 0 ||
                data.GameplayPartyCapacity != 1 || data.MaximumBoundariesPerPump <= 0 || data.MaximumEventsPerBoundary <= 0 ||
                data.MaximumRetained < data.GameplayPartyCapacity || data.MinimumArrivalUnits <= 0 || data.MaximumArrivalUnits < data.MinimumArrivalUnits ||
                data.LowInterval < data.BuildingInterval || data.BuildingInterval < data.SteadyInterval || data.SteadyInterval < data.HeavyInterval ||
                new[] { data.LowInterval, data.BuildingInterval, data.SteadyInterval, data.HeavyInterval, data.ChurnInterval }
                    .Any(n => n < data.MinimumArrivalUnits || n > data.MaximumArrivalUnits) ||
                data.UnitsPerTileEdge <= 0 || data.MonsterInteractionUnits <= 0 || data.TrapInteractionUnits <= 0 || data.LootInteractionUnits <= 0) throw new ArgumentException(Invalid);
            // Transfers are not admitted in this single-floor contract. Zero is a
            // structural unused value, not an invented production descent duration.
            Timing = new RunLogicalTiming(data.UnitsPerTileEdge, data.MonsterInteractionUnits, data.TrapInteractionUnits, data.LootInteractionUnits, 0);
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
            if (!ContractJson.TryParse(bytes, budget, issues, out var root, true) || !ContractJson.ValidateShape(root, names, issues) ||
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
        internal LivingDungeonProfile CreateProfile(SpatialSerializedInputLimits baseBudget) =>
            new LivingDungeonProfile(ProfileId, data.GameplayPartyCapacity, data.MaximumRetained, data.MaximumCheckpointBytes,
                data.MaximumPartyReplay, data.MaximumTotalReplay, data.MaximumBaseSaveBytes, data.MaximumOwnerBytes,
                Timing, RunPostureResolver.BalancedId, baseBudget, data.MaximumCombatWork);
        internal long Interval(string band) => band == AdventurerTrafficPressureResolver.BandLowId ? data.LowInterval :
            band == AdventurerTrafficPressureResolver.BandBuildingId ? data.BuildingInterval :
            band == AdventurerTrafficPressureResolver.BandSteadyId ? data.SteadyInterval :
            band == AdventurerTrafficPressureResolver.BandHeavyId ? data.HeavyInterval :
            band == AdventurerTrafficPressureResolver.BandDangerousChurnId ? data.ChurnInterval : 0;
        internal bool Matches(RunExecution e) => e?.Combat?.Kernel.RulesId == MonsterRecoveryRules.Executable &&
            e.Snapshot?.Floors.Count == 1 && e.Snapshot.Floors[0].FloorIndex == 0 &&
            e.Snapshot.PostureId == RunPostureResolver.BalancedId && e.TimingMatches(Timing);
    }
}
