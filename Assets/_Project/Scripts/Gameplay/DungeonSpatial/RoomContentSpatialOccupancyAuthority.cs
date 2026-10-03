using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class RoomContentSpatialOccupancyRecord
    {
        public string CategoryId;
        public string OptionId;
        public TileCoordinate[] OccupiedTileOffsets = Array.Empty<TileCoordinate>();
        public string[] ShareableCategoryIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class RoomContentSpatialOccupancyConfiguration
    {
        public string Schema;
        public int SchemaVersion;
        public RoomContentSpatialOccupancyRecord[] Records = Array.Empty<RoomContentSpatialOccupancyRecord>();
    }

    public sealed class RoomContentSpatialOccupancySnapshot
    {
        internal RoomContentSpatialOccupancySnapshot(RoomContentSpatialOccupancyConfiguration value,
            int maximumValidationMaterializedTiles)
        { Value = value; MaximumValidationMaterializedTiles = maximumValidationMaterializedTiles; }
        internal RoomContentSpatialOccupancyConfiguration Value { get; }
        internal int MaximumValidationMaterializedTiles { get; }
    }

    public static class RoomContentSpatialOccupancyAuthority
    {
        public const string ProductionPath =
            "Assets/_Project/Data/Production/Save/room_content_spatial_occupancy.json";
        public const string InvalidReason = "content.position.occupancy_configuration_invalid";

        public static bool TryParse(byte[] bytes, SpatialContentValidationWorkloadLimits limits,
            RunSimulationConfig configuration,
            out RoomContentSpatialOccupancySnapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (!limits.IsValid || configuration == null) return false;
                string text = new UTF8Encoding(false, true).GetString(bytes ?? Array.Empty<byte>());
                RoomContentSpatialOccupancyConfiguration value =
                    JsonUtility.FromJson<RoomContentSpatialOccupancyConfiguration>(text);
                if (value == null || value.Schema != "room_content_spatial_occupancy" ||
                    value.SchemaVersion != 1 || value.Records == null ||
                    value.Records.LongLength > limits.MaximumNestedRecords) return false;
                RoomContentSpatialOccupancyRecord[] canonical = value.Records.OrderBy(
                    item => item?.CategoryId, StringComparer.Ordinal).ThenBy(
                    item => item?.OptionId, StringComparer.Ordinal).ToArray();
                long tileCount = 0L;
                foreach (RoomContentSpatialOccupancyRecord record in canonical)
                {
                    if (record == null || string.IsNullOrWhiteSpace(record.CategoryId) ||
                        string.IsNullOrWhiteSpace(record.OptionId) ||
                        !MvpDungeonPlacementIds.TryGetCategoryForOption(record.OptionId,
                            out string category) || category != record.CategoryId ||
                        record.OccupiedTileOffsets == null || record.OccupiedTileOffsets.Length == 0 ||
                        record.ShareableCategoryIds == null ||
                        record.OccupiedTileOffsets.Distinct().Count() != record.OccupiedTileOffsets.Length ||
                        record.ShareableCategoryIds.Any(item => item != MvpDungeonPlacementIds.MonsterCategoryId &&
                            item != MvpDungeonPlacementIds.TrapCategoryId &&
                            item != MvpDungeonPlacementIds.LootNodeCategoryId) ||
                        record.ShareableCategoryIds.Distinct(StringComparer.Ordinal).Count() !=
                            record.ShareableCategoryIds.Length) return false;
                    tileCount += record.OccupiedTileOffsets.LongLength;
                    if (tileCount > limits.MaximumMaterializedTiles) return false;
                    Array.Sort(record.OccupiedTileOffsets);
                    Array.Sort(record.ShareableCategoryIds, StringComparer.Ordinal);
                }
                if (canonical.GroupBy(item => item.OptionId, StringComparer.Ordinal)
                    .Any(group => group.Count() != 1)) return false;
                string[] configuredOrdinaryOptions = (configuration.MvpPlacementEffects ??
                    Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null &&
                        IsOrdinaryCategory(value.CategoryId) &&
                        MvpDungeonPlacementIds.TryGetCategoryForOption(value.OptionId,
                            out string category) && category == value.CategoryId)
                    .Select(value => value.OptionId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                string[] occupancyOptions = canonical.Select(value => value.OptionId)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if (configuredOrdinaryOptions.Distinct(StringComparer.Ordinal).Count() !=
                        configuredOrdinaryOptions.Length ||
                    !configuredOrdinaryOptions.SequenceEqual(occupancyOptions, StringComparer.Ordinal))
                    return false;
                value.Records = canonical;
                byte[] canonicalBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(value, true) + "\n");
                if (!bytes.SequenceEqual(canonicalBytes)) return false;
                snapshot = new RoomContentSpatialOccupancySnapshot(value,
                    limits.MaximumMaterializedTiles);
                return true;
            }
            catch { return false; }
        }

        public static bool TryResolve(RoomContentSpatialOccupancySnapshot snapshot, string categoryId,
            string optionId, out RoomContentSpatialOccupancyRecord record)
        {
            record = (snapshot?.Value.Records ?? Array.Empty<RoomContentSpatialOccupancyRecord>())
                .SingleOrDefault(value => value != null &&
                    string.Equals(value.CategoryId, categoryId, StringComparison.Ordinal) &&
                    string.Equals(value.OptionId, optionId, StringComparison.Ordinal));
            return record != null;
        }

        public static bool CategoriesMayShare(RoomContentSpatialOccupancyRecord left,
            RoomContentSpatialOccupancyRecord right) => left != null && right != null &&
            left.ShareableCategoryIds.Contains(right.CategoryId, StringComparer.Ordinal) &&
            right.ShareableCategoryIds.Contains(left.CategoryId, StringComparer.Ordinal);

        private static bool IsOrdinaryCategory(string categoryId) =>
            categoryId == MvpDungeonPlacementIds.MonsterCategoryId ||
            categoryId == MvpDungeonPlacementIds.TrapCategoryId ||
            categoryId == MvpDungeonPlacementIds.LootNodeCategoryId;

#if UNITY_EDITOR
        // Explicit editor-test adapter for pre-Phase-7 fixtures. Player builds have no fallback.
        internal static RoomContentSpatialOccupancySnapshot CreateEditorTestSnapshot(
            RunSimulationConfig configuration)
        {
            RoomContentSpatialOccupancyRecord[] records = (configuration?.MvpPlacementEffects ??
                Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null &&
                MvpDungeonPlacementIds.TryGetCategoryForOption(value.OptionId, out string ignored))
                .Select(value =>
                {
                    MvpDungeonPlacementIds.TryGetCategoryForOption(value.OptionId, out string category);
                    return new RoomContentSpatialOccupancyRecord
                    {
                        CategoryId = category, OptionId = value.OptionId,
                        OccupiedTileOffsets = new[] { new TileCoordinate(0, 0) },
                        ShareableCategoryIds = new[]
                        {
                            MvpDungeonPlacementIds.MonsterCategoryId,
                            MvpDungeonPlacementIds.TrapCategoryId,
                            MvpDungeonPlacementIds.LootNodeCategoryId
                        }
                    };
                }).OrderBy(value => value.CategoryId, StringComparer.Ordinal)
                .ThenBy(value => value.OptionId, StringComparer.Ordinal).ToArray();
            return new RoomContentSpatialOccupancySnapshot(
                new RoomContentSpatialOccupancyConfiguration
                {
                    Schema = "room_content_spatial_occupancy", SchemaVersion = 1,
                    Records = records
                }, int.MaxValue);
        }
#endif
    }
}
