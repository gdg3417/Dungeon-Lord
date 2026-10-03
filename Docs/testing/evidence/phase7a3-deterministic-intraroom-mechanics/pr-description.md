# Phase 7A3: Deterministic intraroom mechanics

Canonical schema-13 content positions now affect run paths, trap encounters, physical loot reach, and monster engagement. Moving a trap off the selected path removes its damage and reached effects; moving loot changes the route and can expose different traps; moving a monster changes its transient movement and engagement step. The graphical editor and visual run presentation remain deferred.

## Baseline and authority

Depends on merged and qualified PR #223 at `1a38bacb32b54fbd9152ac8121bb9a054a652b10`, verified as exact local and fetched `main` at task start. The stale planning status is reconciled with the merged baseline and A3 contract/evidence.

`RunSimulationService` remains the run coordinator. `ActiveFloorRunSnapshot` captures assignment identity/category/option/sequence/position, derived production traversable cells, translated live occupancy, and unique required ingress/egress before party formation. Its private plan serialization supplies defensive copies; subsequent canonical edits affect future snapshots only. `DetachedCanonicalSpatialSaveState`, the production spatial catalog, and `RoomContentSpatialOccupancyAuthority` retain their respective canonical/geometry/occupancy ownership.

The read-only saved-edge adapter reuses `StructuralRenovationService` endpoint pairing, socket compatibility, and transforms. It matches the saved connection kind and exact physical-corridor footprint, rejects ambiguous/invalid geometry, and introduces no second socket algorithm or persisted connection-point IDs. Existing renovation behavior and tests remain intact.

## Exact MVP semantics

- Actual rectangular room geometry is `GrossFootprint` minus `ReservedTileOffsets`, expressed in canonical base-local coordinates. Supported orientation/socket transforms use the existing authorities. Ordinary content does not block terrain.
- Adventurers enter at required ingress, visit loot objectives by sequence then ordinal assignment ID, and reach required egress using shortest legal four-neighbour paths. BFS neighbours use `TileCoordinate.CompareTo` (X then Y); equally near interaction destinations use coordinate order. Segment boundary tiles appear once.
- Traps trigger once at their first crossed configured occupied/effect tile, including multi-tile footprints. Existing damage, severity, lead-active targeting, trap expertise/mitigation, and chilling-sigil zero damage remain configuration-owned.
- Loot supplies reached effects only after a physical interaction tile is reached. Room clear, survival, extraction, and settlement still gate reward. Existing room reward identity/one roll per cleared content room remains; no per-node economy is added. Empty or wholly unreached spatial content cannot create a content reward roll.
- Monsters start at canonical configured positions, validate their starting occupancy, and select the nearest reachable tile on the planned adventurer path by shortest-path distance, then earliest adventurer step and coordinate. Their recorded shortest movement and encounter are transient. No legal path yields diagnostic evidence instead of teleportation.
- Shared-step event order is Monster, Trap, Loot, then sequence and ordinal assignment ID. Wipe stops subsequent interactions and records only the actual reached path prefix.
- Room placement is reached at entry; monster/trap/loot effects are reached at engagement/trigger/interaction. Spatial encounter severity uses effects reached through that event. Final success, casualties, generated/extracted loot modifiers, Heat, attraction/forecast/demand, and settlement use actual reached effects. Configured route aggregates and branch forecasts retain their configured-state role.

## Persistence, compatibility, and workload

Schema remains **13**. No migration or new durable save fields exist. Transient movement never rewrites canonical `RoomLocalPosition`. `RunOutcomeRecord.SpatialEvents` records identities, stable ordinal, actual adventurer paths, monster configured start/movement, trigger/interaction positions, ingress/egress, and encounter ordinal links; it adds no damage authority. The established retention helper preserves it during same-session publication. Reopen leaves detail unavailable and does not reconstruct it from current layout/tuning.

Optional corridor source-distance ordering, choice, damage, loot, learning, automatic return, and no-retrigger remain Phase 5B behavior. Phase 6 retains run identity, roster/HP continuity, objectives, knowledge-backed transitions, and one complete-run settlement; single-floor runs keep their existing no-transition-event contract. There is no cross-floor pathfinding.

Search queues and geometry/occupancy materialization consume the existing typed canonical materialized-tile limit (production 64; maximum current room materialization 30 + 12). Path length derives from objective and cell counts. Queue/parent/distance/neighbour arrays are bounded and reused; inner BFS performs no allocations. Resolution is synchronous at authoritative boundaries with no frame, animation, physics, coroutine, or rendering timing inputs. No new numeric tuning, config assets, speed, range, initiative, or balance values exist.

## Validation

- Focused A3: **36 passed**, 0 failed/skipped.
- Focused A3/Phase 5B/Phase 6 integration: **140 passed**, 0 failed/skipped.
- Full EditMode: **1,409 total; 1,408 passed; 0 failed; 1 established Windows-inverse skip**.
- Full PlayMode: **2,851 total; 2,841 passed; 0 failed; 10 established skips**, identical by test name to the qualified A2 baseline. All 36 A3 tests passed in PlayMode.
- Production spatial build gate: **65/65 passed** in the full EditMode suite.
- Production loading **37/37**, export **112/112**, recovery **57/57**, migration profiles **19/19** passed in their full suites.
- Windows x86_64 Development Build via `DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment`: **Succeeded**, Development StandaloneWindows64, Bootstrap scene, Unity 6000.3.2f1; 0 errors, 1 build warning (Unity Cloud native-symbol upload skipped because no access token).
- `git diff --check`: **passed**, including the final staged change set.

Tests cover production room definitions/orientations and route sockets; reserved cells and path ties/insertion independence; positional snapshot isolation; crossed/bypassed/multi-tile traps and reached aggregates; physical/ordered/unreachable loot and clear gates; canonical monster starts, movement, event order and placement sensitivity; wipe prefixes; multi-room/two-floor run/party/HP/knowledge; same-session retention and reopen omission; schema and workload boundaries. Existing structural and production-content coverage runs in the full suite. Regression fixtures now use a physically crossed lethal trap and compare one-floor execution with its derived spatial plan. Optional branch loot remains one reached branch roll; the old empty-room content roll is removed by the A3 physical-reach contract.

Qualification uses an isolated checkout to protect the owner's original project files. The five explicitly protected local files are excluded from the changes and commit, with original-checkout SHA-256 preservation recorded in the evidence.

## Qualification boundary and limitations

No owner manual Unity/UI UAT is required for this headless packet: automated authoritative simulation evidence and Windows player compilation are the qualification boundary. The build does not prove gameplay feel. Low-end device profiling remains a later gate.

This is minimum MVP objective/engagement sequencing, not general tactical combat/personality AI. No production graphical editor, whole-dungeon draft/recovery, UI-framework choice, run animation/camera/art, Bootstrap retirement, new content/floors, schema 14, migration, balance changes, or Phase 7 closeout is included. Do not merge this PR as part of the implementation task.
