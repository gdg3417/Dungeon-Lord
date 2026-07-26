# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The implementation baseline is main through merged PR #180 at `308bfc7cc9b0e387fb2003d059f885201333a56c`. GD65B1 completed the separately authored production workload-limits asset, strict parser/conversion boundary, and initial workload/scalability EditMode tests. GD65B2A remains the approved architecture contract, and GD65B2B now implements normalized version-controlled text tables, the machine-readable schema, the approved Floor 1 records, and an editor-only parser/projector at `ContentAuthoring/DungeonSpatial/`. Generated runtime JSON remains absent, and workbooks/cloud editors remain optional non-authoritative tools. No generated production spatial records, exporter, loader, assignment, or runtime spatial-catalog consumer exists. Save schema remains version 6, the catalog remains inactive, and existing abstract placement selections, ordered two-room layout, and room-slot assignments remain runtime/save authority.

The current prototype supports a deterministic, player-completable first-session loop; configurable room/monster/trap/loot choices; an ordered, persistent two-room route; run analysis and route outcomes; research progress; heat, mana, and spoils feedback; and development-build validation. It does **not** yet activate physical tile footprints, corridors, a saved route graph, spatial capacity, multiple floors, or production dungeon-building UI. Floor 2 is only the first multi-floor foundation; the locked MVP remains one main dungeon with up to five floors.

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

## Active GD65B implementation gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD65B0C7 approves exactly rows 66–70 and 72 and closes the register at 72 of 72 `APPROVED`. The sole production workload-limit configuration authority, `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`, will provide `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768` to export, pre-build, runtime-load validation, and canonicalization. These are configuration-owned workload safety bounds—not gameplay, floor-count, floor-space, schema, save, or permanent post-MVP ceilings—and missing or invalid configuration fails closed without a hardcoded or test-default fallback. The 4,096 tile bound applies to one materialized footprint or floor boundary, never cumulative dungeon capacity or Floor 1 capacity 60.

The future **Production Spatial Content Pipeline EditMode Suite** must cover unit, deterministic export, recoverable transaction, loading, pre-build, and scalability stages. Its test-only 80-floor fixture (indexes 0–79, current record shape, four allowlist references per floor, and a test-only 64 by 64 boundary) must validate and canonicalize under the approved caller-supplied limits without code, schema, enum, save, or API changes and must fail under a deliberately lower test limit. This approves neither 80 production floors nor 64 by 64 production geometry. GD65B0 and GD65B1 are complete, while GD65B implementation remains incomplete. The approved limits asset, strict fail-closed parsing/conversion boundary, and initial workload-limit and test-only 80-floor scalability tests now exist; GD65B remains incomplete. Generated catalog, English table, and manifest; exporter and deterministic bytes; recovery; loading and composition-root assignment; pre-build integration; and complete evidence remain later GD65B work. Save schema remains 6, the catalog remains inactive, and runtime/save authority is unchanged. GD66 remains blocked until every required GD65B production record, pipeline stage, test, and item of evidence exists.


## Approved future Dungeon Spatial authoring source

[GD65B2A](docs/planning/gd65b-production-authoring-source-contract.md) approves `ContentAuthoring/DungeonSpatial/` as the future single logical writable authority for normalized CSV records, package metadata, machine-readable schema, and production English spatial localization. Generated Unity JSON remains deterministic derived output; `validation_limits.json` remains separately authored configuration authority. GD65B2B implements that source package and its strict editor-only projection boundary but creates no generated Unity JSON. Workbooks, Google Sheets, CMSs, and web editors may only propose normalized, validated, Git-reviewable changes and are never production authority or runtime/build dependencies.

After GD65B2B, the recommended next packet is deterministic runtime-output serialization and reparse/revalidation without yet activating runtime loading. GD66 remains blocked until all GD65B implementation and evidence are complete.
