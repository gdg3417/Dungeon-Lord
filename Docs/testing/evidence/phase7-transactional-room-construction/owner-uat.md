# Owner UAT — final closeout, 2026-10-08

**Current status:** Owner UAT passed for the agreed PR #229 scope. The implementation and agreed qualification are complete; the owner accepted the two limitations below and approved the PR for merge on October 8, 2026. External review is complete. PR #229 remains unmerged until GitHub confirms the merge.

## Owner-reported Editor and standalone results

The owner reported completing the following in Unity Editor and the corrected Windows Development player:

- Found the production Dungeon Rooms construction workflow, selected authored rooms and legal placement anchors, and previewed footprints, orientation, connectivity, terminal relocation and economic consequences.
- Found the corrected Confirm Placement and Cancel Placement actions and distinguished placement confirmation from session-level Save Changes.
- Confirmed construction into a durable draft without immediately spending mana; restarted the Editor, resumed the draft with the room and its cost preserved, and saved the complete dungeon.
- Observed the quoted construction charge, consistent with passive mana generation. After restarting the Editor, the committed room, corridor and terminal changes remained, with no draft prompt or duplicate charge.
- Ran the corrected Windows Development player, saw the saved construction, verified existing room movement, discarded an edit without publishing it, then closed and reopened the player. The committed layout remained and discarded changes did not return.

Screenshots support the visible production workflow, placement choices and previews, corrected controls, and rendered layout at the captured states. They do not independently prove exact wallet arithmetic. Exact-value accounting, no-spend-on-draft, one-time charge, discard, durability and atomic publication are supported by automated tests; the owner's charge observation is recorded as an observation, not an exact-value assertion.

The owner did not report manually exercising every negative/invalid state, every localization key, every resolution/text-size combination or every persistence edge case. Those remain covered by the automated qualifications recorded in [qualification.md](qualification.md) and [ui-correction.md](ui-correction.md), not claimed as manual owner tests.

## Owner-accepted limitations

**First-room onboarding — accepted for PR #229 only.** Production Dungeon Edit Mode supports subsequent graphical construction once an established required-route tail exists. Fresh-game starter setup still requires the retained Bootstrap controls. This remains an outstanding production-onboarding capability and must be reconsidered during subsequent Phase 7 planning.

**Clipboard qualification exception — accepted for PR #229.** The historical full PlayMode result remains 3,044 total: 3,032 passed, 2 existing clipboard failures and 10 established skips. The failures remain failures; no tests were deleted, skipped, weakened or represented as passing. The owner accepted the narrow qualification exception. Production-specific, regression and Windows qualifications, together with owner Editor/standalone UAT, support proceeding with this exception documented.

## Visual scope and future assessment

The production Dungeon screen remains visually provisional compared with [the production mobile UI vision](../../../planning/production-dungeon-mobile-ui-vision.md). The owner accepted this capability-specific construction UI for PR #229, not as the final visual design. Based on the owner screenshots, the next development assessment should prioritize a dedicated production Dungeon UI composition pass: dungeon visual dominance, portrait/landscape hierarchy, vertical floor navigation, collapsible contextual information, category presentation, room focus and recognizable authored object sprites. No such work is included here. Reassess the exact successor PR boundary after this PR merges, against the new `main` commit and dependency graph.

## Retest reference

For any later repeat of the agreed comprehension flow, use the isolated setup and disposable-save instructions in the historical procedure below; never use or clean the owner's primary save. The completed UAT above supersedes that procedure's old pending status.

1. In the production Dungeon screen, enter Edit Mode, open Rooms, select an authored room and identify a legal anchor.
2. Preview its footprint, orientation, connection, terminal relocation, total proposed cost and resulting mana. Confirm Placement and Cancel Placement must remain visible with details collapsed and long text; Save Changes and Discard Draft must remain visibly separate.
3. Confirm to the durable draft and verify there is no immediate mana spend. Restart, resume and save; verify the committed geometry and one charge.
4. Discard a separate edit and verify it does not return after closing and reopening the Windows player.

The detailed historical setup used Unity `6000.3.2f1`, the disposable project `C:/Dev/Dungeon-Lord/Temp/room-construction-validation`, and player `C:/Dev/Dungeon-Lord/Builds/Phase7RoomConstruction-UI-482632b-20261008/Windows/Dungeon Lord.exe`. These paths are retained as evidence, not a request for additional testing.
