# MVP Vertical-Slice Integration Plan After M7-C2

_Date: 2026-06-04 (UTC)_

_Status basis: merged repository state through PR 73 / M7-C2_

## Purpose

This documentation-only plan defines the next phase as MVP vertical-slice integration. It is intended to stop scope drift into deeper backend scaffolding and redirect upcoming implementation PRs toward the smallest coherent player-facing loop that can be observed, evaluated, and improved.

This plan does not amend locked design specs, add gameplay behavior, add tuning data, add localization keys, or authorize production backend work.

## Current state after M7-C2

The repository has progressed through the deterministic backend and diagnostics foundation for the current MVP scope:

- M6 heat closeout is documented and preserves active-play-only heat behavior.
- M7 active research lifecycle scaffolding is documented through the developer-only single-slot lifecycle.
- PR 73 / M7-C2 adds a deterministic, read-only research verification boundary scaffold for the existing single-slot research lifecycle.
- M7-C2 verification remains intentionally blocked from production claim behavior: it does not call a server, satisfy verification, grant rewards, unlock content, charge costs, process offline progress, or change heat.
- The README was previously too minimal for handoff and now needs enough context to keep future PRs aligned.

Backend scaffolding is ahead of the original low-hour backend timeline, but playable MVP validation is not complete. Resolver coverage, save-compatible summaries, and developer diagnostics reduce technical risk; they do not prove that the core experience is understandable, motivating, or fun for a player.

## Smallest next playable loop

The next phase should integrate the smallest observable loop that lets a player make one meaningful improvement decision. The loop is complete only when a player can:

1. Place or modify a room or structure.
2. Simulate or observe an adventurer run.
3. See the mana impact of the run or action.
4. See the loot extraction impact.
5. See the heat impact.
6. See the research status impact.
7. Receive one clear next optimization choice.

The optimization choice should be narrow and immediate, such as improving placement, running again after observing risk/reward, or changing the active setup. It should not introduce broader rewards, unlocks, raids, seasons, monetization, or additional research lanes.

## Existing systems to reuse

Upcoming vertical-slice PRs should compose existing systems before adding new ones:

| Existing system | Expected reuse in vertical slice |
| --- | --- |
| Structure placement scaffold | Provide the player-visible setup action for placing or modifying a room or structure. |
| Run simulation service | Drive the observable adventurer run outcome without adding a new simulation path. |
| Loot extraction summaries | Surface extraction results as part of the loop summary. |
| Active heat application and current heat tier diagnostics | Surface heat impact and current tier using the already-scaffolded active-play heat path. |
| Single-slot research status and verification boundary summaries | Surface active research progress/status and safe verification-boundary state without adding production claim behavior. |
| Localization table patterns | Keep all player-facing text localization-owned. New UI PRs must use localization keys or table references. |
| Save compatibility patterns | Preserve legacy-safe saves and keep persistence additions additive, deterministic, and migration-safe. |

## Explicit exclusions

The vertical-slice integration phase remains locked to the MVP loop above. The following are explicitly excluded:

1. No rewards or unlock tables yet.
2. No multi-slot research.
3. No offline research progression.
4. No offline heat processing.
5. No raids.
6. No `Hostile` or `Raid` heat tiers.
7. No seasons.
8. No leaderboards.
9. No monetization.
10. No production backend calls.
11. No new monster families beyond MVP scope.
12. No feature expansion outside the locked MVP.

## Recommended next PR sequence

| PR | Scope | Required boundary |
| --- | --- | --- |
| VS0 | Documentation-only vertical-slice plan and README update. | No runtime, tuning, scene, prefab, asset, or localization-key changes. |
| VS1 | Create a read-only player loop summary presenter that composes existing run, mana, loot, heat, and research outputs without new mutation. | Presenter only; no gameplay mutation, rewards, unlocks, backend calls, or new simulation path. |
| VS2 | Add a minimal player-facing Bootstrap or Home loop panel using localization keys. | UI should consume the VS1 presenter; avoid growing diagnostics into production UX. |
| VS3 | Add one guided action path for place, run, observe, and improve. | One narrow path only; no broad tutorial system or expanded feature set. |
| VS4 | Add first-session smoke test checklist and evidence template. | Evidence should validate the integrated loop, not broaden scope. |

## Acceptance criteria for VS1 through VS4

### VS1: Read-only player loop summary presenter

- Composes existing run, mana, loot, heat, and research outputs into one summary model.
- Does not mutate save data, structures, mana, loot, heat, research, run history, or offline state.
- Does not add rewards, unlocks, costs, production backend calls, offline progression, or new tuning files.
- Uses injected, loaded, or test-scoped dependencies; no gameplay tuning values are hardcoded in runtime logic.
- Includes EditMode coverage for deterministic summary output, no-mutation behavior, missing-data fallback, and legacy-safe save handling where relevant.

### VS2: Minimal player-facing loop panel

- Displays the VS1 summary in a concise Bootstrap or Home panel.
- Uses localization keys or table references for all player-facing text.
- Keeps diagnostics separate from production-facing presentation.
- Avoids turning `GameRoot` or `BootstrapOverlay` into a large catch-all surface; extract focused presentation helpers if needed.
- Includes UI or diagnostics smoke evidence under `docs/testing/evidence`.

### VS3: One guided action path

- Provides one guided path for place, run, observe, and improve.
- Keeps the player choice to one clear next optimization decision.
- Reuses the structure placement scaffold and run simulation service instead of adding parallel behavior.
- Preserves active-play-only heat and single-slot research boundaries.
- Includes EditMode coverage for deterministic guidance state and a Bootstrap smoke checklist for the guided path.

### VS4: First-session smoke test checklist and evidence template

- Adds a reusable checklist for validating a first-session vertical-slice run.
- Adds an evidence template that records setup, observed loop outputs, player-facing text/localization checks, and explicit exclusions.
- Keeps evidence paths organized and easy to find under `docs/testing/evidence`.
- Confirms Unity tests are required only when runtime code changes are part of the PR under test.

## VS16 closeout and gameplay handoff

The player-facing vertical-slice stream is closed out by `docs/planning/vs16-player-facing-mvp-vertical-slice-closeout.md`. After VS16, return to gameplay/system development and avoid additional presentation-only slices unless a smoke blocker or usability blocker appears.

## Risk register

| Risk | Mitigation |
| --- | --- |
| `GameRoot` and `BootstrapOverlay` are growing too large. | Prefer focused presenter/helper boundaries and keep UI composition narrow. |
| Diagnostics are not production UX. | Treat diagnostics as evidence and developer support; player-facing panels should be intentionally designed and localized. |
| README was previously too thin for handoff. | Keep README concise but explicit about status, validation expectations, and next work. |
| Documentation evidence paths may become cluttered. | Use milestone- or vertical-slice-specific evidence files and templates rather than ad hoc notes. |
| Adding more scaffold depth before testing fun would be a mistake. | Require VS1 through VS4 to integrate and validate the smallest coherent playable loop before deeper systems work. |

## Testing guidance

For this VS0 documentation-only PR:

- Unity test execution is not required because no runtime code changes are introduced.
- Run text and formatting checks where available.
- Confirm the changed file set remains documentation-only.
- Ensure no CSharp, JSON tuning, Unity scene, prefab, material, asset, or `.meta` changes are introduced.

For future VS1 through VS4 implementation PRs:

- Run relevant Unity EditMode tests for code changes.
- Run Bootstrap smoke tests for UI or diagnostics changes.
- Attach evidence under `docs/testing/evidence`.
- Preserve config-owned tuning, localization-owned player-facing text, deterministic resolver behavior, and legacy-safe saves.
