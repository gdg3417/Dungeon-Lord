# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0C7 approval update is main through merged PR #177 at `e1bae81649e73452c76946689b93ba48eaebcb7d`. GD64 completed the inactive spatial contract; GD65A completed the inactive serializable schema and bounded deterministic validator/canonicalizer; PRs #168–#176 established and progressively approved the production-value gate; and PR #177 / GD65B0C6 approved production path, loading, validation, and recoverable export ownership. No production spatial records, production files, exporter, loader, assignment, or runtime spatial-catalog consumer exists. Save schema remains version 6, the catalog remains inactive, and the existing abstract placement selections, ordered two-room layout, and room-slot assignments remain runtime and save authority.

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

## Active GD65B0 approval gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD65B0C7 approves exactly rows 66–70 and 72 and closes the register at 72 of 72 `APPROVED`. The single future authority, `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`, will provide `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768` to export, pre-build, runtime-load validation, and canonicalization. These are configuration-owned workload safety bounds—not gameplay, floor-count, floor-space, schema, save, or permanent post-MVP ceilings—and missing or invalid configuration fails closed without a hardcoded or test-default fallback. The 4,096 tile bound applies to one materialized footprint or floor boundary, never cumulative dungeon capacity or Floor 1 capacity 60.

The future **Production Spatial Content Pipeline EditMode Suite** must cover unit, deterministic export, recoverable transaction, loading, pre-build, and scalability stages. Its test-only 80-floor fixture (indexes 0–79, current record shape, four allowlist references per floor, and a test-only 64 by 64 boundary) must validate and canonicalize under the approved caller-supplied limits without code, schema, enum, save, or API changes and must fail under a deliberately lower test limit. This approves neither 80 production floors nor 64 by 64 production geometry. GD65B0 approval is complete and the dependency gate for GD65B implementation is open, but GD65B is not implemented: this packet adds no code, JSON, tests, assets, records, pipeline, runtime behavior, save state, or migration. The next dependency-correct packet is GD65B implementation; GD66 remains after production records and pipeline evidence exist.
