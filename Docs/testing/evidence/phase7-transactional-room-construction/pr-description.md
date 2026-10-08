# Phase 7: Add transactional graphical room construction

## Status

Implementation and agreed qualification are complete. Owner Editor and Windows standalone UAT passed for the agreed scope on October 8, 2026. External review is complete, and the owner approved this PR for merge with the two limitations below. PR #229 remains open and unmerged; GitHub merge confirmation establishes the merged baseline.

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

The production Dungeon screen remains visually provisional relative to [the production UI vision](https://github.com/gdg3417/Dungeon-Lord/blob/codex/phase-7-transactional-graphical-room-construction/docs/planning/production-dungeon-mobile-ui-vision.md). The owner accepted this capability-specific UI, not the final visual design. A dedicated production Dungeon UI composition pass should be prioritized for future assessment, including viewport hierarchy, floor navigation, contextual details, category presentation, room focus and authored object sprites. Its exact packet boundary remains provisional until after merge and reassessment of current `main` and dependencies.

Bootstrap controls for still-unmigrated replacement, deletion, floor lifecycle, optional branches, content acquisition/custody, Research and Run/Observe remain available under the established diagnostics visibility policy. This PR does not implement those capabilities, fresh-game graphical setup, floor lifecycle, arbitrary route insertion, room content placement, full visual redesign, new content/tuning, or a canonical save migration.
