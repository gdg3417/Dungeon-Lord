# GD62 spatial save migration proposal

## Status and authority

This is a proposal for the next design gate, not authorization or implementation. The current save root schema version remains **6**. GD62 adds no field to `SaveData`, changes neither migration implementation, and does not make the inactive graph a runtime authority. Spec 38 and INV-12 control future committed spatial editing; Spec 28 supplies the general versioning and recovery rules.

The current competing compatibility fields are resolved in this order: persisted `mvpRoomSlotAssignments` for ordered room instances; `mvpDungeonFloorLayout` for current placement nodes; `mvpDungeonPlacements` as its legacy fallback; and `dungeonLayout` as the older generic slot scaffold. That observed order must be frozen in approved fixtures before migration.

## Provisional next version and mapping

Schema version **7** is a provisional candidate only. A later reviewed migration would read the compatibility authority once and emit:

`entrance -> Room 0 -> Room 1 (when present) -> completion`

Room 0 and Room 1 become room nodes in their existing numeric order. A gate must approve deterministic textual derivation rules for the floor instance, room instances, room nodes, corridor instances, entrance, and completion IDs. IDs must be derived only from approved stable source IDs and explicit position semantics—never runtime hashes, `GetHashCode`, dictionary/list enumeration, or incidental collection order.

Default coordinates, orientations, footprints, corridor content IDs, and structure costs are deliberately unspecified. Migration cannot ship until approved content makes that mapping valid without guessing tuning.

## Canonical serialization and idempotence

The proposed graph writes canonical arrays as follows:

1. Floors by configured floor index, then floor ID using ordinal comparison.
2. Rooms by room instance ID using ordinal comparison.
3. Nodes by explicit node-kind value, then node ID using ordinal comparison.
4. Edges by route classification, source node ID, destination node ID, then edge ID using ordinal comparison.
5. Occupied tiles by X, then Y.

Repeated migration of the same logical input must produce byte-equivalent canonical graph data. Migrating an already-current save must be a no-op. The migration must not create new IDs or reorder data based on input enumeration.

## Legacy authority and fixture matrix

The design gate must approve fixtures covering: schema versions 1 through 6; empty and populated `dungeonLayout`; legacy placements only; floor-layout nodes only; both legacy representations with disagreement; room assignments only; Room 0 only; Room 0 and Room 1; duplicate/out-of-range room records; missing room/corridor content; malformed IDs; duplicate IDs; partial graph-shaped data if applicable; and a current-version idempotence fixture. Each fixture must state which compatibility field wins and the expected ordered route without inventing unapproved coordinates or content IDs.

Missing content must use an explicitly approved migration map or safe content fallback. No fallback ID is approved by GD62. Corrupt state must remain recoverable and diagnosable; it must not be silently deleted, partially rewritten, or treated as valid. If a valid graph cannot be produced, migration must fail atomically, preserve the original save and backup, return a stable internal failure classification, and keep gameplay from writing a competing partial graph.

## Backup, rollback, and recovery

Before committing the future version change, persist and verify a recoverable version-6 backup. Build the version-7 candidate separately, validate all IDs, footprints, capacity, endpoints, reachability, connection limits, terminal behavior, and canonical ordering, then atomically replace the active payload. On write/validation failure, retain version 6 and the backup. Recovery must be repeatable and must never advance the schema marker before the full payload is durable.

## Authority transition

During the migration release, old models become read-only compatibility adapters at the point the validated graph is successfully committed. They must stop accepting writes before any runtime system writes the graph. The graph becomes the sole writable authority only after all gameplay readers, save lifecycle paths, and ordered-route regressions use its canonical view. A later cleanup may remove compatibility data only after rollback support and fixture coverage permit it; there must never be two writable layout authorities.

Every later committed tile placement or movement must save immediately under INV-12. The older standalone Spec 28 statement that tile edits use interval saves conflicts with the audited lock summary, INV-12, and Spec 38; INV-12 and Spec 38 control future spatial editing.

## Unresolved approval gates

The following must be approved before implementation: default migration coordinates and orientations; room and corridor footprint definitions; corridor content IDs; floor dimensions/bounds; the cost-to-footprint projection and schema/export validation rule; safe missing-content/fallback IDs; stable textual ID templates; exact legacy fixtures; and recovery UX/telemetry ownership. The later content contract/export packet owns validation that configured costs agree with authored footprint rules. GD62 intentionally supplies no formula converting tile count into floor-space cost.
