# Phase 7: Add transactional graphical room construction

## Status

Implementation and agreed qualification are complete. Owner Editor and Windows standalone UAT passed for the agreed scope, including the final bounds-fix recovery check, on October 8, 2026. External review is complete, and the owner approved this PR for merge with the documented limitations below. PR #229 remains open and unmerged; GitHub merge confirmation establishes the merged baseline.

Starting main baseline: `72924bdf54d222332b256cab254a22126f3b4bfd` (PR #228, after #227). Branch: `codex/phase-7-transactional-graphical-room-construction`.

## Capability

Players with an established required-route room tail can construct authored rooms from production Dungeon Edit Mode: Rooms → select an authored room and its supported orientation/terminal side → choose a graphical legal anchor → inspect the placement preview → Confirm Placement → review and Save Changes. Placement confirmation creates a durable whole-dungeon draft without spending mana; Save publishes the complete valid dungeon atomically. The production UI keeps local placement actions visible independently of detail scrolling/collapse and separates them from session-level Save/Discard. Proposal cost/resulting mana and current acknowledged-draft economics are distinct, sourced from the existing economic authority, with affordability feedback.

The implementation reuses StructuralEditService and the existing structural economic, transactional draft, persistence, SaveService and production presentation authorities. It adds no independent placement, draft, or economic authority. Schema remains 13 with no canonical migration. The draft journal advances to format 4 and candidate/commit records to version 3; exact historical A4 format-2 and A5 format-3 records and predecessor chains remain recoverable. Replay is deterministic; economics reconcile canonical state to final geometry, retain surviving investment, avoid charging experimental gestures, and publish geometry, identities, investment and wallet together. Active runs retain immutable start snapshots.

## Qualification

- Affected A4/A5/construction/scene regressions: 162/162 passed.
- Final production EditMode scene tests: 20/20 passed.
- Final genuine production PlayMode/input tests: 22/22 passed.
- Historical full EditMode: 1,599 passed, 0 failed, 1 established skip (1,600 total).
- Historical full PlayMode: 3,032 passed, **2 existing clipboard failures**, 10 established skips (3,044 total). The two failures remain failures; the owner accepted a narrow qualification exception for this PR. Tests were not deleted, skipped, weakened or represented as passing.
- Corrected Windows Development Build: succeeded, 0 errors, 1 Unity Cloud symbol-upload warning (`Access token is empty. Native symbols will not be uploaded for this build.`). Player: `Builds/Phase7RoomConstruction-UI-482632b-20261008/Windows/Dungeon Lord.exe` (complete adjacent build files retained).
- Owner Editor and Windows standalone UAT passed for the agreed scope October 8, 2026. Automated tests cover exact accounting and persistence assertions; manual observations are distinguished in [owner-uat.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/owner-uat.md).

Detailed reports, command lines, artifact paths and historical failure records are in [qualification.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/qualification.md) and [ui-correction.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/ui-correction.md).

## Accepted limitations and future work

1. **Fresh-game starter setup:** StructuralEditService supports subsequent construction once an established route tail exists. Fresh-game setup still uses retained Bootstrap starter controls. The owner accepted this limitation for PR #229 only; it remains an outstanding production-onboarding capability for later Phase 7 planning.
2. **Clipboard tests:** The owner accepted the qualification exception while retaining both failures in the full PlayMode result above.
3. **Landscape contextual panel:** During active landscape construction the contextual editing panel occupies the right side; after returning to a recovered invalid construction state it returns to a bottom sheet. The owner accepted this temporary behavior for PR #229 only. It does not block the qualified workflow and does not change the Phase 7A0 contextual bottom-panel design lock. The future UI composition assessment must address consistent contextual-sheet behavior across Normal, Edit, Preview, Invalid and Recovery states.

The production Dungeon screen remains visually provisional relative to [the production UI vision](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/docs/planning/production-dungeon-mobile-ui-vision.md). The owner accepted this capability-specific UI, not the final visual design. A dedicated production Dungeon UI composition pass should be prioritized for future assessment, including viewport hierarchy, floor navigation, contextual details, category presentation, room focus and authored object sprites. It must address consistent contextual-sheet behavior across Normal, Edit, Preview, Invalid and Recovery states, while retaining the approved Phase 7A0 design lock. Its exact packet boundary remains provisional until after merge and reassessment of current `main` and dependencies.

Bootstrap controls for still-unmigrated replacement, deletion, floor lifecycle, optional branches, content acquisition/custody, Research and Run/Observe remain available under the established diagnostics visibility policy. This PR does not implement those capabilities, fresh-game graphical setup, floor lifecycle, arbitrary route insertion, room content placement, full visual redesign, new content/tuning, or a canonical save migration.

## Additional review correction after the earlier approval

A newly opened review finding identified that confirmed invalid construction footprints outside the configured legal floor could become inaccessible after draft recovery. The presentation-only correction expands rendered `Bounds` to include persisted invalid construction footprint cells, following the existing invalid-room-movement behavior; `LegalBounds`, legal grid/boundary, committed geometry and all validation/economic/persistence authorities remain unchanged.

- Focused Phase 7A4 EditMode regressions (including production scene): **87/87 passed**; Phase 7A5 room-movement regressions: **52/52 passed**; transactional construction regressions: **57/57 passed**.
- Genuine production-shell PlayMode/input tests: **23/23 passed**.
- Corrected Windows Development Build: succeeded, **0 errors / 1 Unity Cloud symbol-upload warning**.
- Player artifact: `Builds/Phase7RoomConstruction-BoundsFix-186ba37-20261008-final/Windows/Dungeon Lord.exe`.

The owner subsequently confirmed the final bounds-fix recovery workflow passed in Windows standalone on October 8, 2026, including visibility and correction of a recovered out-of-bounds footprint and preservation of committed state. In that UAT the owner explicitly accepted, temporarily for PR #229 only, that the contextual editing panel appears at the right during active landscape construction but returns to a bottom sheet after resuming a recovered invalid construction. This does not revise or supersede the Phase 7A0 bottom-panel design lock. The earlier approved first-room Bootstrap limitation and full PlayMode clipboard exception remain unchanged. Full PlayMode remains 3,032 passed / 2 existing clipboard failures / 10 established skips; clipboard tests were not rerun or modified. Detailed reports, build warning, artifact report and isolation evidence are in [presentation-bounds-correction.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/presentation-bounds-correction.md) and [owner-uat.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/owner-uat.md).

### Validation player save isolation

The replacement Windows Development player was built from the isolated validation checkout, whose Bootstrap scene references a `build_config.json` that explicitly sets `save.fileName` to `phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json`. The checkout's `SaveService` fallback matches. With identity `gdg3417` / `Dungeon Lord`, the current account's effective path is `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord/phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json`; draft sidecars use the same prefix. The player's save and draft paths therefore exclude `save_primary.json`. Delivered executable SHA-256: `ABC8179E345B70C9D7CA423C54E02739ED9B9ADCA668C2C745FFB81AADC05CC6`. No rebuild was needed and the player was not launched. Owner save/draft before/after hashes match. Supporting inspection is recorded in [presentation-bounds-correction.md](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/Docs/testing/evidence/phase7-transactional-room-construction/presentation-bounds-correction.md).
