# GD62 spatial save migration proposal

## Status and authority

This remains a **proposal**, not authorization or implementation. The current save root schema version remains **6**. GD62 added no `SaveData` field and did not change migration implementation; GD63 changes documentation only. The inactive `DungeonSpatial` graph is not runtime or save authority. Existing ordered two-room models retain both authorities until a separately reviewed migration.

The current compatibility precedence remains: persisted `mvpRoomSlotAssignments`; `mvpDungeonFloorLayout`; `mvpDungeonPlacements` as its legacy fallback; then `dungeonLayout`. That observed order must be frozen in approved fixtures before migration.

## Resolved policy direction

A later migration will read compatibility authority once and produce a deterministic, simple straight-line route in this semantic order:

1. distinct entrance node, which may display to the player as Room 0;
2. legacy internal Room 0 as the first buildable room, displayed as player-facing Room 1;
3. legacy internal Room 1, when present, as the second buildable room, displayed as player-facing Room 2; and
4. completion terminal.

Player-facing numbering is a derived label and never persistent identity authority. The entrance is not legacy room-index authority. New graph state uses stable entrance, room-instance, node, edge, and terminal IDs. Adjacent compatible structures may use direct-doorway edges without inventing corridor content or footprint; physical separation requires authored corridor content.

The migration remains deterministic, idempotent, atomic, rollback-safe, canonically ordered, and a single transition between writable authorities. It cannot change current deterministic outcomes unless explicitly versioned and evidenced.

## Schema-version boundary

Schema version **7** remains a provisional candidate only; neither GD62 nor GD63 approves it. No schema marker advances until the entire candidate payload is durable and valid. Exact new-version selection is part of the later implementation review.

## Canonical serialization and idempotence

The proposed graph writes canonical arrays as follows:

1. Floors by configured floor index, then floor ID using ordinal comparison.
2. Rooms by room instance ID using ordinal comparison.
3. Nodes by explicit node-kind value, then node ID using ordinal comparison.
4. Edges by route classification, source node ID, destination node ID, then edge ID using ordinal comparison.
5. Occupied tiles by X, then Y.

Repeated migration of the same logical input must produce byte-equivalent canonical graph data. An already-current save is a no-op. Runtime hashes, `GetHashCode`, incidental enumeration, UI labels, and collection order cannot derive identities or ordering.

## Legacy authority and fixture gate

GD66 must approve fixtures for schema versions 1–6; empty/populated `dungeonLayout`; legacy placements only; floor-layout nodes only; disagreements among representations; assignments only; legacy Room 0 only; both legacy rooms; duplicate/out-of-range records; missing room/corridor content; malformed/duplicate IDs; partial graph-shaped data if applicable; and current-version idempotence. Each fixture identifies the winning compatibility field and expected semantic route.

Missing content requires an explicitly approved migration map or safe fallback. No fallback ID is approved. Corrupt state stays recoverable and diagnosable rather than being silently deleted, partially rewritten, or accepted. Failure to produce a valid graph must preserve the original save and backup, return a stable internal failure classification, and prevent a competing partial graph from becoming writable.

## Backup, rollback, and recovery

Before a future version commit, persist and verify a recoverable version-6 backup. Build the candidate separately; validate IDs, footprints, one-tile-one-space capacity, endpoints, reachability, connections, terminal behavior, and canonical ordering; then replace the active payload atomically. On write or validation failure, retain version 6 and its backup. Recovery must be repeatable.

## Authority transition

Old models become read-only compatibility adapters only when the validated graph commits successfully. They stop accepting writes before any runtime system writes the graph. The graph becomes sole writable authority only after all gameplay readers, save lifecycle paths, and ordered-route regressions use its canonical view. Cleanup waits for rollback and fixture support. There must never be two writable layout authorities.

INV-12 requires immediate save safety for later committed tile placement/movement. The older standalone Spec 28 interval-save statement conflicts with the audited lock summary, INV-12, and Spec 38; INV-12 and Spec 38 control future editing.

## Genuinely unresolved implementation gates

GD64 must first align the inactive contracts and validator with rectangular bounds, footprint-derived used space, and separately represented direct doorways. GD65 may then approve inactive MVP spatial content IDs, rectangular footprints, connection points, capacities, and export/schema validation. GD66 can then approve exact migration coordinates/orientations, stable textual ID derivation, edge IDs, direct-doorway compatibility, legacy fixtures, missing-content/fallback IDs, and recovery UX/telemetry. Exact floor bounds, construction/invested-mana state, corridor content, and recovery evidence also remain unapproved where migration requires them.

No exact coordinate, orientation, footprint, textual ID template, content ID, fallback ID, or recovery UI is approved by this proposal. Migration implementation, schema change, runtime-reader switch, writable-authority transition, rollback, and migration evidence belong exclusively to Phase 2.
