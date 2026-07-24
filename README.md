# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0C2 approval update is main through merged PR #172 at `b4dfd741bb2c07af5ad5c2de496f4f9dde3117c1`. GD64 completed the inactive spatial contract and deterministic layout-validation alignment; GD65A completed the inactive serializable spatial content schema and bounded deterministic export validator/canonicalizer; PR #168 established the production-value approval gate; PR #169 approved the first identity group; PR #170 / GD65B0A preserved optional-branch allowance extensibility; PR #171 / GD65B0B approved catalog metadata and Floor 1 references; and PR #172 / GD65B0C1 approved the initial room footprints and category capacities. No production spatial records, export registration, or runtime spatial-catalog consumer exist. The save schema remains version 6, the spatial domain remains non-authoritative, and ordered two-room models remain runtime and save authority.

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

GD64 and GD65A are complete. PR #169 approved the first GD65B0 identity group; PR #170 corrected generic optional-branch field extensibility; PR #171 completed GD65B0B by approving catalog metadata, Floor 1 references, its authored branch allowance, and the limited current-scope Save boundary; PR #172 completed GD65B0C1, and GD65B0C2 approves Floor 1 legal bounds and structural capacity-accounting policy while leaving exact final capacity unapproved. This does **not** complete GD65B0, implement content-tile placement, or authorize production implementation. **GD65B remains blocked** by the incomplete [GD65B production spatial content approval record](docs/planning/gd65b-production-spatial-content-approval.md): final floor-space capacity; room reserved tile offsets, allowed orientations, maximum connection counts, and connection points; corridor geometry, remaining capacities, orientations, and sockets; entrance and completion geometry and connection data; socket compatibility; localization; export, manifest, registry, loading, validation, and canonical serialization ownership; workload limits; and production pipeline test ownership require explicit approval. The next dependency-correct packet is **GD65B0C3: approve straight-corridor geometry and required fixed-structure footprints** (rows 33–35, 43, and 48), without choosing those values here. GD65B0C2 authors no records and activates no spatial behavior. GD66 remains the final migration-design gate after GD65B, and Phase 2 exclusively owns migration implementation and authority transition. The catalog introduced by GD65A is not loaded or consumed at runtime.
