# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

Main is merged through PR 120 / GD26. The repository now contains manual smoke runs using resolved adventurer intent after the GD20-GD26 gameplay prototype expansion.

## Operating rules

- Keep PRs small and evidence-backed.
- Keep resolver behavior deterministic.
- Keep gameplay tuning in config or typed config assets, not runtime logic.
- Keep player-facing text localization-owned.
- Preserve legacy-safe saves and additive compatibility patterns.

## Validation expectations

- Run Unity EditMode tests for code PRs.
- Run Bootstrap smoke tests for UI or diagnostics PRs.
- Attach validation evidence under `docs/testing/evidence`.
- Documentation-only PRs should run available text or formatting checks and confirm that no runtime, tuning, scene, prefab, asset, or `.meta` changes were introduced.

VS4 first-session MVP smoke documentation:

- [VS4 first-session MVP smoke test runbook](docs/testing/runbooks/vs4-first-session-mvp-smoke-test-runbook.md)
- [VS4 first-session MVP smoke test evidence template](docs/testing/evidence/vs/vs4-first-session-mvp-smoke-test-evidence-template.md)

## Next intended work

The next intended work is AR1 through AR4 architecture debt containment before GD27 gameplay resumes.

- [AR1 architecture debt containment plan after GD26](docs/planning/architecture-debt-containment-plan-after-gd26.md)
