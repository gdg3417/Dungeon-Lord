# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0B approval update is main through merged PR #170 at `bfeaca9f9df137a05126ec9a47d6bf05cfb0d959`. GD64 completed the inactive spatial contract and deterministic layout-validation alignment; GD65A completed the inactive serializable spatial content schema and bounded deterministic export validator/canonicalizer; PR #168 established the production-value approval gate; PR #169 approved the first identity group; and PR #170 / GD65B0A preserved optional-branch allowance extensibility. No production spatial records, export registration, or runtime spatial-catalog consumer exist. The save schema remains version 6, the spatial domain remains non-authoritative, and ordered two-room models remain runtime and save authority.

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

GD64 and GD65A are complete, and the first GD65B0 identity group plus the GD65B0B metadata and Floor 1 reference group are approved: seven production definition IDs, one initial socket ID, the production corridor semantic resolution, and the contract-defined connection-point namespace mapping. This does **not** complete GD65B0 or authorize production implementation. **GD65B remains blocked** by the incomplete [GD65B production spatial content approval record](docs/planning/gd65b-production-spatial-content-approval.md): remaining Floor 1 bounds, capacity, geometry/capacities, localization, pipeline ownership, and workload limits require explicit approval before records or registration may be implemented. GD66 remains the final migration-design gate after GD65B, and Phase 2 exclusively owns migration implementation and authority transition. The catalog introduced by GD65A is not loaded or consumed at runtime.
