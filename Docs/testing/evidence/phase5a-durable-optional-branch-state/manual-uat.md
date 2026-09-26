# Phase 5A owner manual UAT record

## Final status

Status: **PASSED**.

The owner completed this record against production HEAD
`7f7dc2cbc040a3485fbcbf0067d6caeb1ac4660f`. This validates Phase 5A
persistence and construction only. Phase 5B branch scoring, selection,
traversal, encounters, rewards, automatic return, and knowledge learning were
correctly absent and were not observed.

## Editor UAT observed by the owner

- Fresh Bootstrap start passed. Final passive/offline player-facing mana text
  showed no raw floating-point tails, no unnecessary `.0`, and no more than one
  decimal where applicable.
- With Basic Branching cleared, construction preview failed with localized
  research-required feedback and without topology or mana mutation. Completing
  `ac_300` enabled a valid preview with correct `1 tile` grammar and visible
  geometry, capacity, floor-space, and economy consequences.
- One valid branch constructed; a second was rejected by the floor allowance
  without mutation. Close/reopen preserved branch, research, and mana state.
- On a one-tile corridor, Spike Trap acquisition succeeded. Basic Loot Node on
  the occupied tile was rejected without duplicate spend or state. Trap
  unassignment returned the exact owned item to custody without cost or refund;
  Basic Loot Node then succeeded on the empty terminal tile. Content and custody
  persisted through reopen.
- Removal was blocked while corridor content was assigned. Redeployment to an
  occupied tile was safely rejected; free redeployment to an empty valid tile
  succeeded; unassignment remained free. The one-tile removal preview reported
  historical investment 5 mana, nominal refund 3 mana, credited refund 3 mana,
  and a resulting balance matching the preview apart from ordinary passive
  timing. Removal preserved the required route, and reopen preserved removed
  topology, custody, mana, and the required route. A subsequent preview proved
  the branch allowance was available again.
- A two-tile branch preview reported `2 tiles`, cost 10 mana, accepted trap and
  loot on distinct valid tiles, enforced terminal-tile loot placement, preserved
  free custody redeployment and content through reopen, and remained blocked for
  removal while content was assigned.
- Ordinary runs remained required-route-only: no Phase 5B decision or branch
  encounter occurred; DeadEnd was not a required room or terminal; assigned
  branch content stayed assigned after the run.
- Save Now, pause/resume, and normal close/reopen preserved Phase 5A state
  without new errors. Editor checks at 1920×1080 and 1280×720 passed; the action
  panel stayed scrollable and branch/content/removal/run controls remained
  reachable. The Systems Diagnostics page is cramped/readability-imperfect but
  usable for QA and accepted as non-blocking UI/QA debt for PR #209.

The owner did not inspect hidden stable branch/edge/DeadEnd identities after
retirement, and did not manually reproduce byte-identical repeated-run inputs.
Existing automated identity-non-reuse and deterministic-run coverage remains
authoritative for those invariants.

## Standalone build procedure correction

The generic-wrapper player at `Builds/Phase5A-Final/DungeonLord.exe` was not
accepted for final UAT. It was created through `unity.exe build ... --target
StandaloneWindows64 --output-path Builds\\Phase5A-Final\\DungeonLord.exe --args
'-development'`, not through the repository's canonical development-build
method. Its provenance did not establish `BuildOptions.Development`; F1 did not
expose the Dev Panel and Optional Branch (Phase 5A QA) was absent. This was a
build-procedure issue, not a Phase 5A gameplay defect.

The accepted player was built from the same production HEAD with
`DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment`:

- Unity `6000.3.2f1`, target `StandaloneWindows64`, `BuildOptions.Development`.
- Output: `Builds/Development/Windows/Dungeon Lord.exe`.
- `build-report.json`: `developmentBuild: true`, `buildResult: Succeeded`,
  `targetPlatform: StandaloneWindows64`, `errorCount: 0`, and Bootstrap scene
  included.
- `build-provenance.json`: exact source SHA
  `7f7dc2cbc040a3485fbcbf0067d6caeb1ac4660f`, canonical execute method, Unity
  version, success, and exit code 0. Its dirty flag is only for excluded local
  Unity/editor-generated files, none of which is Phase 5A compiled/runtime
  source.

## Canonical Windows Development Build UAT observed by the owner

- F1 opened the Dev Panel and Optional Branch (Phase 5A QA) appeared.
- Delete Save followed by normal close/relaunch created a fresh native canonical
  test state. The Basic Branching gate was localized; completion enabled a
  one-tile preview showing `1 tile`, 5 mana cost, and approved one-decimal mana
  presentation.
- Construction, one-time Spike Trap charge, occupied-tile loot rejection,
  zero-cost trap custody return, terminal-tile loot acquisition, and persistence
  across relaunch all passed. Branch, loot, returned trap, `ac_300` completion,
  and mana (apart from legitimate offline gain) were preserved.
- Removal blocked with assigned content. After unassignment, the preview showed
  investment 5, refund 3, credited refund 3; removal committed and persisted
  across relaunch without damaging required structures or offline mana behavior.
  A new branch with corridor content could be constructed afterward.
- Required-route-only runs remained unchanged: no Phase 5B branch decision or
  encounter resolution occurred and branch content stayed assigned. The
  1280×720 standalone smoke check passed: the right panel scrolled, controls
  remained reachable, and no blocking overlap occurred. No new
  implementation-related runtime or shutdown error was observed.

Android was intentionally not run and is not required for PR #209.
