# MVP Vertical-Slice Integration Plan After M7-C2

> **Historical / substantially completed / superseded after GD61.** The VS and GD10-GD15 sequences remain useful traceability for the prototype delivered and extended through GD60, but they are no longer the forward execution order. See the [post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md).

_Date: 2026-06-04 (UTC)_

_Status basis: merged repository state through PR 73 / M7-C2_

## Purpose

This documentation-only plan defines the next phase as MVP vertical-slice integration. It is intended to stop scope drift into deeper backend scaffolding and redirect upcoming implementation PRs toward the smallest coherent player-facing loop that can be observed, evaluated, and improved.

This plan does not amend locked design specs, add gameplay behavior, add tuning data, add localization keys, or authorize production backend work.


## Post-GD9 gameplay direction update

_Status basis update: PR #99 has merged; GD9 is complete._

GD9 added the MVP dungeon placement category scaffold: Room, Monster, Trap, and Loot node. That scaffold is not the final dungeon editor and should not be treated as sufficient playable dungeon-building depth. The next phase must make those categories mechanically meaningful by tying composition to placement effects, run outcomes, loot, heat, mana pressure, and eventually one research unlock bridge.

The next recommended implementation PR is **GD10: Deterministic MVP placement effects resolver**. After GD9, future PRs should usually answer: **“What can the player do now that feels more like building or running a dungeon?”** Avoid scaffold-only PRs unless they directly unlock a playable feature in the next one or two PRs. Diagnostics, smoke helpers, internal-only labels, and future-ready models should not dominate the roadmap.

### GD10-GD15 implementation sequence

| GD | Goal | Player-facing value | Merge boundary |
| --- | --- | --- | --- |
| GD10 | Deterministic MVP placement effects resolver. | Room, Monster, Trap, and Loot node visibly change mana, loot, heat, danger, path/capacity, attraction, and/or adventurer-result context. | Next recommended implementation PR; no combat AI, grid editor, drag/drop, new monster families, research unlocks, raids, `Hostile`/`Raid` heat tiers, or production UI rewrite. |
| GD11 | Run outcome uses dungeon composition. | The player understands how their room/monster/trap/loot setup affected loot, heat, mana, success/failure, and adventurer results. | Consume placement effects, posture, and party deterministically; no animated combat, advanced AI, full encounter timeline, or unsafe hero/elite expansion. |
| GD12 | Basic floor/node layout representation. | The dungeon starts to feel spatial instead of being only a category list. | One floor, small fixed node count, one placement per node, ordered path affects resolver; no production grid editor, drag/drop, pathfinding UI, or multi-floor UI. |
| GD13 | First simple dungeon editor view. | The player can select a node, choose Room/Monster/Trap/Loot node, place or replace a starter option, and see current composition. | Keep visuals simple; no art-polish requirement, full production UI framework, expanded content library, or non-trivial drag/drop scope. |
| GD14 | Loot table MVP. | Loot node produces visible deterministic loot from a real MVP table instead of a generic output number. | Basic loot table only; generated/extracted/tradeable values and heat/economy hooks; no inventory UI, monetization, marketplace, or advanced crafting. |
| GD15 | Research unlock bridge. | Research unlocks or improves one real dungeon-building option. | Single-slot research remains; one unlock/upgrade only, such as Skeleton, Spike Trap, or Basic Loot Node; no full tech tree UI, multi-queue research, online verification expansion, or monetization. |

Acceptance criteria and test expectations for the full GD10-GD15 sequence live in `docs/planning/actionable-backlog.md`. GD10 must include deterministic resolver tests, empty/legacy placement fallback coverage, summary/run-explanation coverage including compact and copied smoke text when touched, localization guard coverage for player-facing text, and save-compatibility checks.

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
| GD9 placement category scaffold | Provide the current Room, Monster, Trap, and Loot node setup categories that GD10 must make mechanically meaningful. |
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

## Historical VS0-VS4 PR sequence

The VS0-VS4 presentation sequence below is historical context. It has been superseded by the post-GD9 gameplay sequence above for active implementation planning. Do not continue Bootstrap/presentation-only expansion unless it fixes a smoke blocker or directly supports GD10-GD15 playable mechanics.

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
- Reuses the placement category scaffold and run simulation service instead of adding parallel behavior.
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
| Adding more scaffold depth before testing fun would be a mistake. | Require GD10-GD15 to convert the GD9 placement category scaffold into playable mechanics before deeper systems work. |

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
