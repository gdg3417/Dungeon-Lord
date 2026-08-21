using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    /// <summary>Stable player-localization ownership for the locked GD66 reason table.</summary>
    public static class Gd66MigrationReasonRegistry
    {
        public const string PlayerLocalizationPrefix = "save.migration.spatial.";
        private static readonly string[] PlayerReasons =
        {
            "gd66.authority.contradictory_state",
            "gd66.content.category_mismatch",
            "gd66.content.duplicate_assignment",
            "gd66.content.invalid_legacy_room",
            "gd66.content.invalid_option",
            "gd66.content.invalid_production_room",
            "gd66.content.migration_blocked_narrow_hall",
            "gd66.content.missing_production_room",
            "gd66.content.outcome_mismatch",
            "gd66.content.room_capacity_exceeded",
            "gd66.content.room_semantics_invalid",
            "gd66.floor.duplicate_binding",
            "gd66.floor.index_mismatch",
            "gd66.floor.invalid_definition",
            "gd66.floor.missing_definition",
            "gd66.geometry.adjacency",
            "gd66.geometry.bounds",
            "gd66.geometry.capacity",
            "gd66.geometry.overlap",
            "gd66.geometry.socket",
            "gd66.graph.endpoint",
            "gd66.graph.ordering",
            "gd66.graph.reachability",
            "gd66.graph.terminal",
            "gd66.id.collision",
            "gd66.id.malformed",
            "gd66.layout_contract.selection_duplicate",
            "gd66.layout_contract.selection_missing",
            "gd66.marker.layout_contract_missing",
            "gd66.marker.layout_contract_unsupported",
            "gd66.payload.ambiguous_envelope",
            "gd66.payload.invalid_primary",
            "gd66.payload.invalid_schema",
            "gd66.payload.missing_primary",
            "gd66.payload.missing_schema",
            "gd66.payload.missing_schema_version",
            "gd66.payload.newer_than_application",
            "gd66.payload.nonintegral_schema_version",
            "gd66.payload.null_primary",
            "gd66.payload.unknown_member_unpreservable",
            "gd66.payload.unreadable",
            "gd66.payload.unsupported_legacy_version",
            "gd66.payload.workload_exceeded",
            "gd66.preflight.native_probe_failed",
            "gd66.preflight.path_invalid",
            "gd66.preflight.path_redirected",
            "gd66.preflight.platform_unsupported",
            "gd66.preflight.volume_unsupported",
            "gd66.profile.duplicate",
            "gd66.profile.invalid",
            "gd66.profile.missing",
            "gd66.profile.version_mismatch",
            "gd66.route.duplicate_floor_node_revision",
            "gd66.route.duplicate_placement_revision",
            "gd66.route.duplicate_room_slot",
            "gd66.route.gap",
            "gd66.route.record_out_of_range",
            "gd66.starter_profile.duplicate",
            "gd66.starter_profile.invalid",
            "gd66.starter_profile.marker_mismatch",
            "gd66.starter_profile.missing",
            "gd66.starter_profile.version_mismatch",
            "gd66.success.recovered_original",
            "gd66.transaction.active_payload_unknown",
            "gd66.transaction.backup_failed",
            "gd66.transaction.candidate_invalid",
            "gd66.transaction.commit_failed",
            "gd66.transaction.durability_failed",
            "gd66.transaction.input_fingerprint_mismatch",
            "gd66.transaction.journal_malformed_with_verified_original",
            "gd66.transaction.multiple_live_attempts",
            "gd66.transaction.no_trusted_active_payload",
            "gd66.transaction.path_invalid",
            "gd66.transaction.pinned_input_hash_mismatch",
            "gd66.transaction.pinned_input_missing",
            "gd66.transaction.pinned_profile_hash_mismatch",
            "gd66.transaction.pinned_profile_missing",
            "gd66.transaction.pinned_spatial_input_hash_mismatch",
            "gd66.transaction.pinned_spatial_input_missing",
            "gd66.transaction.recovery_failed",
            "gd66.transaction.rollback_source_missing",
            "gd66.write.atomic_save_failed",
            "gd66.write.capacity_reduction_invalid",
            "gd66.write.first_write_validation_failed",
            "gd66.write.native_save_persist_failed",
            "gd66.write.room_removal_has_contents",
            "gd66.write.unsupported_room_selection",
        };

        public static IReadOnlyList<string> PlayerReasonCodes => Array.AsReadOnly(PlayerReasons);
        public static IReadOnlyList<string> RequiredPlayerLocalizationKeys =>
            Array.AsReadOnly(PlayerReasons.Select(reason => PlayerLocalizationPrefix + reason).ToArray());
        public static bool RequiresPlayerMessage(string reason) =>
            reason != null && Array.BinarySearch(PlayerReasons, reason, StringComparer.Ordinal) >= 0;
        public static string PlayerLocalizationKey(string reason) =>
            RequiresPlayerMessage(reason) ? PlayerLocalizationPrefix + reason : string.Empty;
    }
}
