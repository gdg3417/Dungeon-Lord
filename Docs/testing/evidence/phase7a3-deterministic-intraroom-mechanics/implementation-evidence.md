# Phase 7A3 deterministic intraroom mechanics

## Baseline and scope

- Branch: `codex/phase-7a3-deterministic-intraroom-mechanics`.
- Required ancestor: `1a38bacb32b54fbd9152ac8121bb9a054a652b10`, exact local/fetched `main` and merged PR #223 when work began.
- PR #223's external-review corrections and owner Editor/Windows qualification were reviewed. Its planning status is reconciled without rewriting historical records.
- Recommended configuration: GPT-5.6 Sol, Medium; Standard bounded feature integration. Repository policy treats this as advice, not an instruction to switch the executing session.
- Schema remains 13. No migration, durable field, invariant change, second run engine, second geometry owner, or connection-point persistence was introduced.

## Authorities and behavior

`CanonicalRunnableFloorProjection` preserves canonical `RoomLocalPosition`. Current-target validation exposes its already validated occupancy dependency internally to run-start snapshot creation. `RunnableFloorSnapshot` privately serializes the derived `IntraroomSnapshot` alongside the existing plan; inspection/materialization returns defensive copies. Production geometry, translated occupancy, sockets, configuration, loot, runtime inputs, and knowledge are captured before party formation. Live canonical changes affect new snapshots only.

The saved-edge adapter in `StructuralRenovationService` reuses the existing `TryEndpoint`, `Pairs`, rotation, compatibility, and world-transform helpers. It matches saved connection kind and exact physical-corridor tiles and requires one endpoint pair. No geometry extraction or duplicate socket algorithm was needed; renovation mutation behavior is unchanged.

Paths use canonical base-local coordinates over actual gross room tiles excluding authored reserved tiles. Four-neighbour BFS enumerates neighbours by `TileCoordinate.CompareTo` (X then Y); equal-distance interaction destinations use coordinate order. Loot objectives use sequence then ordinal assignment ID. Segment boundaries are appended once. Each monster starts at its configured anchor with a validated configured footprint, searches once for distances to the planned path, selects minimum distance then earliest adventurer step (the tile at a step is unique), and records a canonical shortest movement path. Event order is path step, Monster/Trap/Loot, sequence, ordinal assignment ID. Movement is transient and never writes canonical content state.

Traps trigger once at the first intersected occupied/effect tile, including multi-tile footprints. Loot must reach an interaction tile. Unreachable monster/loot produces stable enum diagnostic evidence with no engagement/reach, damage, reached effects, or fabricated movement. Wipe stops at the actual reached path prefix and omits later interaction/egress evidence.

Damage still belongs to `RunEncounterResolver`: existing profiles, severity formula, lead-active targeting, expertise, mitigation, and chilling-sigil zero damage. Spatial severity uses effects reached through the current event, so later or bypassed content cannot influence earlier damage. Final room success and downstream composition use actual reached effects. The existing room reward seed and one roll per cleared content room remain; reached loot supplies its existing configured modifiers, with no new per-node economy. A spatial room with no reached content supplies no content reward roll. Clear/survival/extraction/settlement gates still apply. Regression comparisons cover bypassed traps across success, reached/reward effects, casualty composition, generated/extracted loot, Heat, attraction, and demand. Configured aggregates and optional-branch forecasts still describe configured potential content; actual branch traversal, return, no-retrigger, damage, loot, and learning retain Phase 5B authority.

Phase 6 retains one run identity, party roster/HP, objectives, knowledge-backed EXIT/DESCEND, and final settlement. Every floor/room resolves locally; there is no cross-floor pathfinding. Actual reached room effects feed the existing floor-knowledge learner.

`SpatialEvents` is a transient property on `RunOutcomeRecord`, like party/encounters/branches/transitions/objective. It records identities, ordinal, actual adventurer path, configured monster start/movement, trigger/interaction/ingress/egress positions, and encounter ordinal links without duplicating damage. The established retention helper preserves it on same-session publication. Reopen leaves it null; canonical bytes omit detailed spatial history.

## Workload and configuration

No new numeric tuning or configuration assets exist. Geometry/search queues and derived occupancy materialization consume the existing typed `CanonicalSpatialSaveWorkloadLimits.MaximumMaterializedTiles` boundary. Production's canonical profile is 64; the largest current room has 30 gross tiles plus at most 12 ordinary occupied tiles, safely below that boundary. Assignments and rooms also enter through existing canonical record/capacity validation. Traversal length is bounded by `(loot objectives + 1) * (legal cells - 1) + 1`. Each search queue visits each legal cell at most once; neighbour arrays, queue, parent, and distance arrays are created per room and reused for searches. The inner BFS allocates nothing. Searches happen at authoritative run/snapshot boundaries, never in Update, FixedUpdate, a coroutine, animation, physics, or rendering callback.

Future legal-cell providers can use `DeterministicRoomPaths`; future objective scoring can select among its legal paths without replacing geometry authority. No speed, initiative, range, patrol, line of sight, collision reservation, tactical targeting, or balance additions exist. Device profiling remains a later qualification gate; this packet does not claim measured low-end frame performance.

## Qualification

Qualification runs in an isolated managed checkout at the same baseline with the exact changed source/meta files copied from the implementation checkout. This avoids Unity import/build writes to the owner's protected files. Test XML is retained under the implementation checkout's ignored `TestResults` directory.

- Focused Phase 7A3: 36 passed, 0 failed, 0 skipped.
- Focused A3/Phase 5B/Phase 6 integration after regression corrections: 140 passed, 0 failed/skipped.
- Full EditMode: 1,409 total; 1,408 passed; 0 failed; 1 established Windows-inverse skip. Unity CLI exit 0.
- Full PlayMode: 2,851 total; 2,841 passed; 0 failed; 10 established skips. Unity CLI exit 0. All 36 A3 tests passed in PlayMode.
- Production spatial build gate: 65/65 passed in the full EditMode suite.
- Production loading: 37/37 passed in full PlayMode. Production export: 112/112, recovery: 57/57, and migration profiles: 19/19 passed in full EditMode.
- Windows x86_64 Development Build through `DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment`: **Succeeded**, Development StandaloneWindows64, Bootstrap scene, Unity 6000.3.2f1; 0 errors, 1 build warning (Unity Cloud native-symbol upload skipped because no access token).
- `git diff --check`: **passed**, including the final staged change set.

The first focused run had no discovery adapter and found zero tests; it was not accepted as qualification. Compilation/assertion setup corrections and two initial focused behavioral/fixture failures were corrected before the passing run. The first full EditMode run found four regressions: two old automatic-empty-room reward expectations, an off-path lethal trap in the Floor 2 wipe fixture, and an unintended single-floor final transition event. The transition contract was restored; reward fixtures now assert physical content semantics, the wipe fixture selects a legal crossed trap tile, and the one-floor comparison consumes the same derived spatial plan. The 140-test integration run and final full EditMode rerun passed. No failure was suppressed.

The PlayMode skipped-test set was compared by exact full test name with `phase7a2-review-correction-full-playmode.xml` (2,805 passed, 10 skipped); the sets are identical. Eight synchronous GameRoot boot fixtures are established EditMode-only qualification, one inverse platform fixture applies on non-Windows only, and one native standalone qualification fixture applies in a Windows player only. The EditMode skip is the Windows-inverse fixture. No A3 test is skipped and no skip was introduced.

## Owner files and qualification boundary

The five protected paths in `C:/Dev/Dungeon-Lord` were never reset, restored, deleted, overwritten, stashed, staged, normalized, or committed. Git reported a clean tree at entry, despite the prompt's historical note about local modifications. Their initial SHA-256 values are:

| Protected file | SHA-256 |
|---|---|
| `Assets/Settings/UniversalRP.asset` | `8115A6A750C739DCBCBF71737D1733EAFE068AD9FBDC3D4CA0DEF2723F47311F` |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | `6CD2FAA131F346F601036104691F0223744C5806A51CEEC454E6D4FF3BB6831D` |
| `Assets/UniversalRenderPipelineGlobalSettings.asset` | `C0C660CA9F592748753B6A409B174936F003930BD32C1BD0B07AC1C2DFCA0642` |
| `ProjectSettings/ProjectSettings.asset` | `34DA6D701E4C4629CA7B1CECB638F801D9C5EA4F40D33B09FECA46777073B993` |
| `ProjectSettings/UnityConnectSettings.asset` | `EDBC2C8B4E180CA0734F320B51D6F560342563862923509D94D15492B200AD27` |

This packet is headless and adds no player-facing presentation. Owner manual Unity/UI UAT is not required for A3 qualification; simulation evidence and player compilation are the qualification boundary. A player build does not prove gameplay feel. Production graphical editing, visual run presentation, and Phase 7 closeout remain unfinished.
