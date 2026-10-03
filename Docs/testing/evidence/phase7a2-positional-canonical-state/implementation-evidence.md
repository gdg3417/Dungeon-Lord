# Phase 7A2 positional canonical state implementation evidence

## Scope and baseline

- Required ancestor and merged Phase 7A1 baseline: `5d76f88bdd02ab9083e94f36a091f36ea6d66806` (PR #222).
- Implementation branch: `codex/phase7a2-positional-canonical-state`.
- Objective: make exact room-local monster, trap, and loot placement authoritative canonical state without adding intraroom run simulation or a production graphical editor.
- Writable save schema: 12 -> 13.
- Historical schema 12 remains a frozen five-field `RoomContentAssignment` input.
- External review identified two blocking correctness defects at reviewed HEAD
  `7910ee5b5f05c23dc8180bbcba618b6d92078524`; the corrections and regression
  evidence are recorded below. External re-review and owner manual Unity/UAT remain pending.

## External-review correction

The reviewed implementation omitted `RoomContentSpatialOccupancySnapshot` from
`DetachedSpatialMigrationRecoveryContext`. Normal current-target validation received the
snapshot, but no-sidecar recovery reconstructed a current-target validation context without it;
the Editor-only fixture fallback masked the player-build failure. The coordinator now passes the
same validated immutable snapshot into recovery that it passes into normal schema-13 validation.
Recovery disables the Editor fixture fallback at this boundary, so a missing snapshot still fails
closed. A player-equivalent regression proves a populated schema-13 payload with no sidecars is
recognized as trusted current state, remains byte-identical, and preserves assignment identities
and positions; the missing-dependency case returns `gd66.authority.contradictory_state` without
changing the payload. The existing real Windows `SaveService` fixture also migrates, persists,
closes, and reopens through this corrected coordinator path.

The reviewed schema-12 upgrader also copied the Phase 7A1 orientation-specific slot anchor into
schema-13 `RoomLocalPosition`, whose authority is canonical base-local space. That applied room
orientation twice. `RoomLocalCoordinateTransform` is now the single bidirectional authority:
base-local coordinates transform to oriented room-relative coordinates and floor coordinates, and
frozen oriented coordinates invert back to base-local coordinates with checked bounds. The
upgrader performs this inverse before writing schema 13. Maximum-envelope upgrade tests cover
Basic Zero, Rectangle Zero/Ninety, and Large Chamber Zero/Ninety, validate the current result with
production occupancy, and prove that every saved canonical coordinate resolves back to the exact
frozen physical tile. Rectangle Ninety explicitly covers the later monster slot at oriented
`(3,1)`.

External re-review subsequently found that the owner UAT checklist still described direct
stored-coordinate-to-A1-anchor comparison from before this canonical-frame correction. The
checklist now distinguishes Zero-orientation direct comparison from rotated-room forward
transformation before comparing to the orientation-specific frozen anchor. This is a
documentation-only clarification; it makes no production-code, test, configuration, or frozen
profile change.

The reviewed live occupancy asset also contained an unapproved independent
`MaximumValidationMaterializedTiles = 66`. No specification or approved sizing evidence owns that
number. It has been removed. Live occupancy parsing and per-floor positional validation now consume
the existing production validation-safety authority from
`Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`:
`MaximumMaterializedTiles = 4096`, approved by Spec 36 for a footprint/floor validation boundary.
This remains a workload guard, not gameplay capacity. A configuration-derived conservative
maximum calculation using current Floor 1/2 capacities and all allowed production room envelopes
produces per-floor bounds of 84 and 110 materialized usable/assignment tiles (194 aggregate), all
below the configured per-floor envelope. The calculation deliberately ignores fixed/corridor space
consumption, so it over-approximates rather than admitting an invalid gameplay state.

Occupancy readiness now compares the canonical occupancy records with every configured ordinary
monster, trap, and loot option. Missing, duplicate, extra, category-mismatched, malformed, or
noncanonical records fail runtime composition and the production build gate. Footprints remain
one tile for current production content; synthetic multi-tile coverage remains test-only.

## Canonical schema and migration

Schema 13 adds `RoomLocalPosition` after the five stable assignment fields. The serializer selects fields explicitly by schema: frozen schemas through 12 continue to require exactly `AssignmentId`, `RoomInstanceId`, `CategoryId`, `OptionId`, and `Sequence`, while schema 13 requires the position. Canonical assignment identity and ordering are unchanged.

`SchemaTwelveToThirteenUpgrade` validates and round-trips frozen schema-12 bytes, canonicalizes a detached state, groups assignments by stable room identity, and delegates assignment-to-slot mapping to the frozen Phase 7A1 planner. It copies only the selected room-local coordinate into each assignment, then reconstructs the complete save while preserving recognized unrelated primary/root members. The source bytes are never mutated. Missing, malformed, incompatible, overlapping, or insufficient profile data returns failure before publication. Earlier supported schemas still advance sequentially through schema 12 and then schema 13.

The existing `DetachedSpatialSaveLoadCoordinator`, canonical session, `ExactCompleteSaveAtomicPersistence`, and recovery authorities remain the only live load/write path. Recovery now recognizes valid frozen schema 12 as a trusted canonical predecessor, so a durable schema-12 native save without live migration sidecars can reach the coordinator's atomic 12-to-13 upgrade. Current schema-13 saves validate and reopen directly and do not remigrate.

Migration preserves assignment, room, and floor identities; category, option, and sequence; room layout and orientation; lifecycle and returned-custody state; structural investment; mana; and unrelated recognized or preservable extension state. It performs no economic mutation, random selection, time lookup, content deletion, replacement, or relocation outside the frozen mapping.

## Frozen Phase 7A1 dependency

Runtime composition receives the validated `RoomContentPositionMigrationProfilesSnapshot` from the reviewed production asset. It does not use `AssetDatabase` or editor filesystem access. `SaveService` and the coordinator fail closed when the snapshot is absent. The production build gate requires the exact assigned asset and validates it intrinsically; schema-12-only source-boundary conformance remains separate from historical intrinsic compatibility validation.

The Phase 7A1 compatibility asset is unchanged:

- Git blob: `78e2520cdc286d88df44b71faa23de9aabcf63e5`
- SHA-256: `599DB0C16F1B3414BB4D887EF953D2C8CB1ACBA03DBAB68A60FD65A83E1468F1`

No runtime-generated coordinate, first-free fallback, nearest-valid fallback, or use of migration slots as live gameplay tuning was added.

## Live position, occupancy, and transforms

Native placement and returned-content redeployment require a supplied `TileCoordinate`. A missing coordinate returns `content.position.required`; an invalid coordinate or footprint returns `content.position.invalid` before persistence. Failed writes retain mana, custody, the canonical session, and durable bytes.

`RoomContentSpatialOccupancyAuthority` owns live option/category footprints independently from the A1 migration profiles and placement-effect tuning. The production asset declares one-tile footprints for current content; its validation workload is injected from the existing production spatial validation-limits authority rather than duplicated in the occupancy asset. The contract supports multi-tile offsets and category-sharing rules; synthetic tests exercise multi-tile validity and overlap rejection. Runtime validation checks room ownership, transformed usable tiles, reserved tiles, footprint bounds, production option/category compatibility, overlap, configured room capacity, and bounded materialization.

`RoomLocalCoordinateTransform` is the single room-local-to-floor transform used by both content validation and room reserved-tile geometry. Room translation therefore changes only the resolved floor tiles, not saved local coordinates. Orientation uses the shared transform. Structural mutation revalidates retained assignments and rejects replacements that would invalidate them rather than relocating content.

The Bootstrap surface adds only narrow localized X/Y coordinate controls needed to exercise canonical placement. It does not add a draft owner or production graphical editor.

## Floor knowledge

Floor-knowledge applicability continues to serialize the canonical floor while excluding only `ActivationState`. Because schema-13 assignments serialize `RoomLocalPosition`, changing a material content position changes the applicability fingerprint. Activation-only changes continue to produce the same fingerprint. Migration does not rewrite historical knowledge records to make them appear current; old records may naturally become inapplicable until normal qualifying evidence reconfirms them.

## Automated qualification

| Run | Result |
| --- | --- |
| Focused positional canonical tests | 6/6 passed |
| Focused Phase 7A2 plus Windows integration selection | 37 passed, 0 failed, 1 expected Windows-inverse skip |
| Real Windows SaveService schema-12 migration/save/reopen test | 1/1 passed |
| Production spatial build-gate selection | 64/64 passed |
| Full EditMode | 1,365 total; 1,364 passed; 0 failed; 1 expected Windows-inverse skip |
| Full PlayMode | 2,808 total; 2,798 passed; 0 failed; 10 established environment/fixture skips |
| Windows Development Build | succeeded; StandaloneWindows64; Unity 6000.3.2f1; exit code 0 |

The EditMode skip is `CurrentNonWindowsRuntimeFailsClosed` (`gd66.test.windows_only_inverse`) because qualification ran on Windows. The ten PlayMode skips are the eight established synchronous EditMode-only GameRoot fixtures, the same Windows-inverse case, and the Windows-player-only fixture. No Phase 7A2 failure was suppressed.

The Windows Editor integration uses the real `SaveService`, `Application.persistentDataPath`, coordinator, native filesystem, atomic persistence, durable readback, reopen, and delete paths. It proves a populated legacy save reaches schema 13, then proves a valid frozen schema-12 five-field assignment migrates to the expected A1 position with exact stable identity, persists byte-identically to the expected current representation, reopens without replay, and deletes through the live service. The Windows Development Build proves player compilation with editor-only compatibility/test helpers absent.

## Failures found and corrected during qualification

- The first complete EditMode run exposed schema-12-era fixtures that omitted the new runtime dependencies or reused implicit positions. Fixtures were updated to inject the authoritative snapshots and supply deliberate non-overlapping coordinates; validation was not weakened.
- The live occupancy workload initially reused a historical migration accounting limit. The first
  Phase 7A2 implementation then introduced a separate value of 66 without approved authority.
  External review caught that gap; the correction removes 66 and injects the existing approved
  production spatial validation workload instead, leaving historical save limits unchanged.
- PlayMode feedback fixtures placed multiple objects at one implicit coordinate. They now exercise explicit coordinates.
- The real schema-12 Windows service test found that recovery recognized frozen canonical schemas 7-11 and current schema 13 but omitted schema 12. The existing trusted-frozen validation chain was extended to schema 12, allowing the established coordinator and atomic persistence path to run.
- A temporary disk-full compilation failure was resolved by deleting only generated `Library/BurstCache`. No source, save, user asset, or intended worktree change was removed.

## Boundaries and remaining validation

- No Phase 7A3 intraroom navigation, movement, trap triggering, loot reachability, combat AI, or broader run simulation is included.
- No production graphical editor, dungeon-wide draft transaction, draft recovery, or Bootstrap retirement is included.
- No owner save was reset, deleted, replaced, or used for automated qualification.
- The Windows-player-only test remains an intentional Editor skip; player assembly compatibility is covered by the successful Windows Development Build. Owner standalone/manual UAT remains required.
- Automated qualification does not constitute owner acceptance or merge readiness. External PR review and the checklist in `manual-uat.md` remain open.

## External-review correction qualification

| Run | Result |
| --- | --- |
| Corrected focused positional/recovery/workload fixture | 13/13 passed |
| Detached spatial migration transaction fixture | 95/95 passed |
| Detached save/load coordinator fixture | 56/56 passed |
| Complete-save/current-target validation selection | 37/37 passed |
| Real Windows SaveService migration/save/reopen fixture | 1/1 passed |
| Production spatial build-gate fixture | 65/65 passed |
| Full EditMode | 1,373 total; 1,372 passed; 0 failed; 1 established Windows-inverse skip |
| Full PlayMode | 2,815 total; 2,805 passed; 0 failed; 10 established environment/fixture skips |
| Windows Development Build | succeeded; StandaloneWindows64; Unity 6000.3.2f1; 170,782,603 bytes; 0 errors; 1 existing warning |

The correction build proves the player assembly compiles without the Editor occupancy fallback.
The available Windows-player-only qualification fixture exercises native filesystem durability but
does not yet drive a full schema-13 `SaveService` create/save/close/reopen sequence. That owner
standalone runtime check remains explicitly pending and is not claimed as passed. Owner manual UAT
also remains pending.
