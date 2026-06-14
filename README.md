# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

Main is merged through PR 117 / GD23. The repository now contains the deterministic Adventurer Run Intent resolver and presenter; the next intended work is GD24 autonomous adventurer arrival pressure preview.

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

The next intended PR is GD24 autonomous adventurer arrival pressure preview as a read-only, deterministic player-facing signal.
