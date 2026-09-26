# Dungeon-Lord

**Current repository status (2026-09-26):** Phase 3 is closed through merged PR #200, Phase 4 is complete through PR #207, the Phase 5 design lock is merged in PR #208, and Phase 5A is complete in PR #209. Current main is PR #210 at `75781a4cfb7a7e837c855608f9a0753139a1bf77`; canonical spatial state is active writable route/content authority, and current writable schema 10 owns optional-branch topology, `corridorContent`, and `sharedBranchKnowledge`. `DeadEnd = 6` is implemented. Phase 5B is the next gameplay packet; route choice, optional traversal, branch encounter resolution, branch-specific outcomes, run-driven knowledge learning, and production tuning remain unimplemented. Its locked design is in the [Phase 5 branching and route-choice design lock](docs/planning/phase-5-branching-and-route-choice-design.md), and owner-approved initial configuration and implementation authority is in the [Phase 5B production tuning and run-condition contract](docs/planning/phase-5b-production-tuning-and-run-condition-contract.md), pending final external review. PR #210 makes `Docs/process/AI_Model_Selection_Policy.md` canonical.

**Historical Phase 2 status (2026-07-31), superseded by later Phase 2–4 packets:** PR #187 is merged and GD66 is approved. Phase 2 is active, and PR #188 is the current Phase 2A inactive compatibility-profile configuration implementation packet. Save schema remains 6; no future target save schema is selected; no migration or writable-authority transition is active; production spatial gameplay remains inactive.


Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

**Historical GD65B5 final status, superseded by later activation and migration packets:** Implementation and required owner validation passed at `c5eefae61e9bf3b7bf0a200e343f383f0122743b` in PR #186. PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. At that recorded point, the production spatial catalog remained inactive, existing runtime/save authority was unchanged, and save schema remained 6.

The current prototype supports a deterministic, player-completable first-session loop; configurable room/monster/trap/loot choices; canonical physical footprints, corridors, spatial capacity and saved route/content state; Phase 3 construction, movement, replacement and leaf deletion; Phase 4 structural/content economy; canonical online and offline passive mana; Phase 5A optional-branch construction/content/persistence; run analysis and required-route outcomes; research progress; heat, mana, and spoils feedback; and development-build validation. It does **not** yet implement Phase 5B route choice/traversal/outcomes/learning, multiple floors, or production dungeon-building UI. Floor 2 is only the first multi-floor foundation; the locked MVP remains one main dungeon with up to five floors.

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

- [VS4 first-session MVP smoke test runbook](docs/testing/runbooks/vs4-first-session-mvp-smoke-test-runbook.md)
- [VS4 first-session MVP smoke test evidence template](docs/testing/evidence/vs/vs4-first-session-mvp-smoke-test-evidence-template.md)

## Historical GD65B implementation gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD65B0C7 approves exactly rows 66–70 and 72 and closes the register at 72 of 72 `APPROVED`. The sole production workload-limit configuration authority, `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`, will provide `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768` to export, pre-build, runtime-load validation, and canonicalization. These are configuration-owned workload safety bounds—not gameplay, floor-count, floor-space, schema, save, or permanent post-MVP ceilings—and missing or invalid configuration fails closed without a hardcoded or test-default fallback. The 4,096 tile bound applies to one materialized footprint or floor boundary, never cumulative dungeon capacity or Floor 1 capacity 60.

The **Production Spatial Content Pipeline EditMode Suite** now covers all six GD65B responsibilities, and the final complete EditMode suite passed 202/202 at the tested SHA. Its test-only 80-floor fixture remains a scalability contract, not approval for production floors or geometry. PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. Save schema remains 6, the catalog remains inactive, and runtime/save authority is unchanged.


## Dungeon Spatial authoring source

[GD65B2A](docs/planning/gd65b-production-authoring-source-contract.md) historically approved `ContentAuthoring/DungeonSpatial/` as the single logical writable authority for normalized CSV records, package metadata, machine-readable schema, and production English spatial localization. Generated Unity JSON remains deterministic derived output; `validation_limits.json` remains separately authored configuration authority. GD65B2B implemented that source package and its strict editor-only projection boundary; deterministic generated-set construction followed in PR #182, recoverable publication in PR #183, and export invocation plus committed outputs in PR #184. Workbooks, Google Sheets, CMSs, and web editors may only propose normalized, validated, Git-reviewable changes and are never production authority or runtime/build dependencies.
