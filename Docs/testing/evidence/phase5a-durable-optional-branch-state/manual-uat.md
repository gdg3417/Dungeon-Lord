# Phase 5A owner manual UAT plan

## Status and boundary

Status: **not run; owner evidence required**.

This UAT validates Phase 5A persistence and construction only. Adventurer branch choice, optional traversal, branch encounter resolution, automatic return, and run-driven knowledge learning belong to Phase 5B and are **not expected** in this PR. Required-route runs must continue exactly as before.

Use a Windows x86_64 Development Build at 1920×1080 and 1280×720. Enable the existing development panel so the localized Phase 5A QA controls are visible.

## Setup

1. Start from a fresh native save and record initial mana.
2. Confirm the save is schema 10 and that the dungeon has no optional branch.
3. Select an eligible Floor 1 required-route room, an authored compatible origin connection, and a valid Straight Stone Corridor length.

## Research gate and preview

1. Use **QA: Clear Basic Branching**.
2. Preview branch construction.
3. Verify the preview is rejected with localized Basic Branching feedback, not a raw key/code.
4. Confirm topology, mana, structural investment, and save state did not change.
5. Use **QA: Complete Basic Branching** to establish `ac_300` through the existing completed-research authority.
6. Preview the same branch again.
7. Verify the preview shows selected corridor geometry/length, trap and loot capacities, resulting floor-space capacity, and the existing structural-economy mana consequences.

## Construction, allowance, and persistence

1. Commit the valid preview.
2. Verify exactly one optional physical corridor and one DeadEnd are added.
3. Verify mana decreases by the configured physical-corridor per-tile price with no extra branch fee.
4. Attempt to preview/construct a second branch and verify localized rejection from the effective floor allowance.
5. Close the game normally, reopen it, and verify the branch, mana balance, and required route persist unchanged.
6. Exercise periodic save, an ordinary state-change save, and pause/resume; after each boundary reopen and verify the schema-10 branch owners remain intact and unrelated recognized state is preserved.

## Corridor trap and loot placement

1. On a one-tile branch, acquire/place an eligible trap on its only tile.
2. Attempt to place eligible loot on that occupied tile; verify deterministic localized rejection and no mana/content duplication.
3. Unassign the trap and verify the exact AssignmentId/category/option/sequence moves to returned custody, costs zero mana, and gives no acquisition refund.
4. Place loot on the terminal tile beside the DeadEnd; verify placement succeeds only on that terminal tile.
5. On a branch of at least two tiles, place a trap and loot on distinct valid tiles; verify both persist after close/reopen.
6. Verify a monster, a required-route corridor target, a Direct Doorway target, a tile outside the footprint, a duplicate occupied tile, and placement beyond authored category capacity are all rejected without partial spend or state mutation.

## Custody and removal

1. Select an exact trap or loot assignment and unassign it.
2. Verify it appears in the existing returned-content custody with unchanged identity and sequence.
3. Redeploy that returned item to a valid branch tile.
4. Verify redeployment costs zero mana and removes the custody copy, leaving exactly one active identity.
5. With any corridor content still assigned, preview branch removal and verify removal is blocked with localized feedback.
6. Explicitly unassign all branch content, preview removal, and commit it.
7. Verify only the optional edge and DeadEnd disappear; required rooms, required edges, and Completion identities do not change.
8. Verify the refund uses the existing structural investment/refund policy and never exceeds the wallet capacity policy.
9. Construct another branch and verify the retired edge/branch/DeadEnd identities are not reused.
10. Close/reopen and verify the removed branch stays removed, returned custody stays exact, and unrelated state remains intact.

## Required-route regression

1. With an optional branch, trap, and loot present, run the dungeon repeatedly from identical inputs.
2. Verify the party remains on the existing required route.
3. Verify the DeadEnd is not presented as a required room or terminal.
4. Verify branch trap/loot does not resolve and does not change required-route success, loot, Heat, or reported outcome.
5. Verify repeated identical inputs remain identical.

## Presentation and lifecycle

1. At 1920×1080 and 1280×720, verify all QA controls remain usable through scrolling and no control blocks core actions.
2. Verify every success/failure label is readable localized English and no raw localization key or internal reason code appears.
3. Repeat close/reopen, periodic save, state-change save, and pause/resume checks in the Windows Development Build.
4. Record screenshots, save hashes/logs as appropriate, exact build identity, and any shutdown diagnostics.

## Acceptance record

Do not mark this plan passed until the owner records actual observed results. The known shutdown-only ComputeBuffer/PlayerConnection diagnostics may be accepted only if they are identical to the established baseline and no new runtime error appears.
