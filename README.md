# Dungeon-Lord

**Phase 6 final status (2026-10-01):** PR #220 is merged at `e9f93b8d742ccaba38c7de32b776970006791d93`; Phase 6 is complete. It implements knowledge-backed transitions, localized aggregate reporting, and the schema-12 durable-knowledge validation correction. Production content remains Floors 1 and 2, writable schema remains 12, durable reporting is coarse, and detailed transition causes remain same-session/transient. See [A5B qualification evidence](Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md).

**Historical Phase 6A5A implementation status (2026-09-30):** PR #218 / Phase 6A4 is merged at `2505c679afd5172dc82dd08b44aedf3add96001c` and passed external review, automated validation, owner Editor qualification, and Windows Development Build qualification. A5A advances the writable save schema to 12 with one explicit 11-to-12 migration that adds empty `sharedFloorKnowledge` while preserving schema 11 state. Floor knowledge remains separate from `sharedBranchKnowledge`, learns only from completed floors with final survivors, is captured in the pre-run snapshot, and becomes inapplicable after material floor changes; activation alone does not invalidate it. At that historical point A4 unknown transition perception remained in force and richer knowledge-backed explanation was deferred to A5B.

**Historical Phase 6A3 implementation status (2026-09-29):** A deterministic read-only authority now reports whether the constructed production Floor 2 is eligible to become Active. It consumes the existing schema-11 canonical state, `ac_100` research permission, production semantic validation, and `ActivationValid` layout authorities; it also requires a same-floor required Entrance-to-Completion route containing at least one room. Results expose stable reason codes, deterministic blocker precedence, and resolved target identity without mutating activation, spatial, research, runtime, session, or persistent state. This packet adds no activation/deactivation writer or UI. `CanonicalMvpRouteProjection` still rejects a second Active floor, so multi-floor runs remain deferred.

**Phase 6A2 merged status (2026-09-29):** PR #216 merged at `45ac7da34bb4527b9b45fcaeb9ec5ec01d816e7f`. External review, automated qualification, Editor qualification, and Windows standalone qualification passed. Schema 11 remains the writable target; frozen schemas 7–10 and migration semantics are unchanged. Validated `ac_100` completion grants permission only. The player can construct production Floor 2 for configured 450 mana as a persistent Inactive shell, then select either floor for independent structural/content editing. Inactive construction validity permits an unfinished required route while retaining geometry, ownership, lifecycle, investment, ordering, and workload checks; Active floors retain full route validation. The existing investment owner retains one non-refundable historical shell record. Only Active Floor 1 contributes passive mana or current run projection; a second Active floor remains unsupported by that run projection. Activation controls and multi-floor runs are deferred. See [Phase 6A2 evidence](Docs/testing/evidence/phase6a2-floor2-construction-inactive-editing/implementation-evidence.md).

**Historical Phase 6A1 implementation status (2026-09-28):** Schema 11 is the current writable target. Each persisted floor owns one explicit `ActivationState` (Active or Inactive), the sole writable activation authority. Frozen schemas 7–10 retain their historical shapes; the sequential migration chain maps every schema 10 floor to Active, preserves existing state and extension members, and creates no floors. Validation requires Floor 1 active and deeper active floors to form a contiguous prefix. Online and offline passive mana count only validated Active floors. Production still contains only Floor 1; Floor 2 construction, lifecycle actions, and multi-floor runs remain deferred.

**Historical Phase 2 status (2026-07-31), superseded by later Phase 2–4 packets:** PR #187 is merged and GD66 is approved. Phase 2 is active, and PR #188 is the current Phase 2A inactive compatibility-profile configuration implementation packet. Save schema remains 6; no future target save schema is selected; no migration or writable-authority transition is active; production spatial gameplay remains inactive.


Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

**Historical GD65B5 final status, superseded by later activation and migration packets:** Implementation and required owner validation passed at `c5eefae61e9bf3b7bf0a200e343f383f0122743b` in PR #186. PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. At that recorded point, the production spatial catalog remained inactive, existing runtime/save authority was unchanged, and save schema remained 6.

The current prototype supports a deterministic, player-completable first-session loop; configurable room/monster/trap/loot choices; canonical physical footprints, corridors, spatial capacity and saved route/content state; Phase 3 construction, movement, replacement and leaf deletion; Phase 4 structural/content economy; canonical online and offline passive mana; Phase 5A optional-branch construction/content/persistence; the Phase 5B1 transient party/HP prerequisite; PR #213 Phase 5B2 route choice, corridor traversal/outcomes, survivor learning, transient diagnostics, Decision 30 v2, and atomic complete-run publication; and constructed Inactive Floor 2 editing with read-only activation eligibility. Activation mutation, multi-floor runs, and production dungeon-building UI remain unimplemented. Floor 2 is only the first multi-floor foundation; the locked MVP remains one main dungeon with up to five floors.

Normal play still depends on the temporary Bootstrap overlay and simple MVP screen. These are validation surfaces, not the intended production editor, and will be replaced only after spatial contracts and editing behavior stabilize.

## Operating rules

- Keep resolver behavior deterministic and changes evidence-backed.
- Keep gameplay tuning in config and player-facing text in localization.
- Preserve additive, legacy-safe saves.

## Validation expectations

- Run Unity EditMode tests for code PRs.
- Run Bootstrap smoke tests for UI or diagnostics PRs.
- Attach validation evidence under `docs/testing/evidence`.
- Documentation-only PRs should run available text or formatting checks and confirm that no runtime, tuning, scene, prefab, asset, or `.meta` changes were introduced.

VS4 first-session MVP smoke documentation:

- [VS4 first-session MVP smoke test runbook](Docs/testing/runbooks/vs4-first-session-mvp-smoke-test-runbook.md)
- [VS4 first-session MVP smoke test evidence template](Docs/testing/evidence/vs/vs4-first-session-mvp-smoke-test-evidence-template.md)

## Historical GD65B implementation gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](Docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD65B0C7 approves exactly rows 66–70 and 72 and closes the register at 72 of 72 `APPROVED`. The sole production workload-limit configuration authority, `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`, will provide `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768` to export, pre-build, runtime-load validation, and canonicalization. These are configuration-owned workload safety bounds—not gameplay, floor-count, floor-space, schema, save, or permanent post-MVP ceilings—and missing or invalid configuration fails closed without a hardcoded or test-default fallback. The 4,096 tile bound applies to one materialized footprint or floor boundary, never cumulative dungeon capacity or Floor 1 capacity 60.

The **Production Spatial Content Pipeline EditMode Suite** now covers all six GD65B responsibilities, and the final complete EditMode suite passed 202/202 at the tested SHA. Its test-only 80-floor fixture remains a scalability contract, not approval for production floors or geometry. PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. Save schema remains 6, the catalog remains inactive, and runtime/save authority is unchanged.


## Dungeon Spatial authoring source

[GD65B2A](Docs/planning/gd65b-production-authoring-source-contract.md) historically approved `ContentAuthoring/DungeonSpatial/` as the single logical writable authority for normalized CSV records, package metadata, machine-readable schema, and production English spatial localization. Generated Unity JSON remains deterministic derived output; `validation_limits.json` remains separately authored configuration authority. GD65B2B implemented that source package and its strict editor-only projection boundary; deterministic generated-set construction followed in PR #182, recoverable publication in PR #183, and export invocation plus committed outputs in PR #184. Workbooks, Google Sheets, CMSs, and web editors may only propose normalized, validated, Git-reviewable changes and are never production authority or runtime/build dependencies.
