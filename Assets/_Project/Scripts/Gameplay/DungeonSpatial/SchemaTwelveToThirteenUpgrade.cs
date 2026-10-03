using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class SchemaTwelveToThirteenUpgrade
    {
        internal const string InvalidReason = "schema13.room_content_position_migration_failed";

        internal static bool TryPrepare(byte[] source, CanonicalSpatialSerializationLimits limits,
            RoomContentPositionMigrationProfilesSnapshot profiles, out byte[] candidate)
        {
            candidate = null;
            DetachedCompleteSaveValidationResult frozen =
                DetachedCompleteSaveContract.ParseValidateFrozenSchemaTwelveAndRoundTrip(source, limits);
            if (!frozen.IsValid || profiles == null || source == null) return false;
            try
            {
                if (!CanonicalSpatialSaveContracts.TryCanonicalize(frozen.State, limits.Spatial,
                        out DetachedCanonicalSpatialSaveState upgraded)) return false;
                foreach (SavedSpatialFloor floor in upgraded.Floors ?? Array.Empty<SavedSpatialFloor>())
                {
                    RoomContentAssignment[] assignments = floor?.RoomContents?.Assignments ??
                        Array.Empty<RoomContentAssignment>();
                    foreach (IGrouping<string, RoomContentAssignment> group in assignments.GroupBy(
                                 value => value.RoomInstanceId, StringComparer.Ordinal))
                    {
                        RoomSpatialInstance[] rooms = (floor.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>())
                            .Where(value => value != null && value.RoomInstanceId == group.Key).ToArray();
                        if (rooms.Length != 1 || !profiles.TryGetProfile(rooms[0].RoomDefinitionId,
                                rooms[0].Orientation, out ValidatedRoomContentPositionMigrationProfile profile))
                            return false;
                        RoomContentPositionMigrationPlanResult plan =
                            RoomContentPositionMigrationPlanner.Plan(profile, group.Key, group);
                        if (!plan.Success || plan.Entries.Count != group.Count()) return false;
                        var byIdentity = plan.Entries.ToDictionary(value => value.AssignmentId,
                            StringComparer.Ordinal);
                        foreach (RoomContentAssignment assignment in group)
                        {
                            if (!byIdentity.TryGetValue(assignment.AssignmentId,
                                    out RoomContentPositionMigrationPlanEntry entry) ||
                                entry.RoomInstanceId != assignment.RoomInstanceId ||
                                entry.CategoryId != assignment.CategoryId ||
                                entry.OptionId != assignment.OptionId ||
                                entry.Sequence != assignment.Sequence) return false;
                            if (!RoomLocalCoordinateTransform.TryFromOriented(entry.RoomLocalPosition,
                                    profile.FrozenFootprint, rooms[0].Orientation,
                                    out TileCoordinate canonicalPosition)) return false;
                            assignment.RoomLocalPosition = canonicalPosition;
                        }
                    }
                }
                SpatialContractResult<CanonicalSpatialSaveSerializer.SerializedMembers> members =
                    CanonicalSpatialSaveSerializer.SerializeMembers(upgraded, limits);
                if (!members.IsValid) return false;
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(source, limits.Serialized, issues, out ContractJsonNode root))
                    return false;
                ContractJsonNode primary = root.Fields[2].Value;
                ContractJsonNode authority = Parse(members.Value.Authority, limits);
                ContractJsonNode floors = Parse(members.Value.Floors, limits);
                ContractJsonNode lifecycle = Parse(members.Value.LifecycleAndOwnership, limits);
                if (authority == null || floors == null || lifecycle == null) return false;

                var writer = new ContractJsonWriter(limits.Serialized);
                writer.Node(); writer.Token("{");
                for (int index = 0; index < root.Fields.Count; index++)
                {
                    if (index != 0) writer.Token(",");
                    KeyValuePair<string, ContractJsonNode> field = root.Fields[index];
                    writer.String(field.Key); writer.Token(":");
                    if (field.Key == "schemaVersion") writer.Token("13");
                    else if (field.Key == "primary")
                    {
                        writer.Node(); writer.Token("{");
                        for (int member = 0; member < primary.Fields.Count; member++)
                        {
                            if (member != 0) writer.Token(",");
                            KeyValuePair<string, ContractJsonNode> current = primary.Fields[member];
                            writer.String(current.Key); writer.Token(":");
                            if (current.Key == "canonicalSpatialAuthority")
                                DetachedCompleteSaveContract.WriteCanonicalNode(writer, authority);
                            else if (current.Key == "spatialFloors")
                                DetachedCompleteSaveContract.WriteCanonicalNode(writer, floors);
                            else if (current.Key == "structuralLifecycleAndOwnership")
                                DetachedCompleteSaveContract.WriteCanonicalNode(writer, lifecycle);
                            else DetachedCompleteSaveContract.WriteCanonicalNode(writer, current.Value);
                        }
                        writer.Token("}");
                    }
                    else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}");
                byte[] prepared = writer.Finish();
                if (!DetachedCompleteSaveContract.ParseValidateAndRoundTrip(prepared, limits).IsValid)
                    return false;
                candidate = prepared;
                return true;
            }
            catch { return false; }
        }

        private static ContractJsonNode Parse(byte[] bytes, CanonicalSpatialSerializationLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            return ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode node)
                ? node : null;
        }
    }
}
