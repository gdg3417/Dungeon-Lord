# Phase 7A4 owner UAT

Run after external code review and resolution of blocking findings. Automated tests and screenshots do not qualify usability. Use a backed-up owner save or a disposable test profile; do not delete or replace the owner's save to create test content. No PR has been merged.

## Preparation

- Open Bootstrap with Unity 6000.3.2f1, or use the qualified Windows Development Build noted in the validation evidence.
- Complete Windows player: `Builds/Phase7A4-2026-10-05/Windows/Dungeon Lord.exe` (retain its adjacent Data folder and DLLs). Build report: `TestResults/phase7a4-windows-build-report.json`.
- Retained Development tools exposes existing Bootstrap capabilities to acquire/place content and unlock existing Floor 2 when eligible. Return to Dungeon for graphical testing. No graphical construction/acquisition parity is claimed.
- Prepare an existing room with a monster, trap and loot assignment, including at least one invalid overlap target. Use only existing production Floors 1/2.

## Production Dungeon and input

1. Review portrait and landscape on phone/tablet representative view sizes. Confirm Total Mana, Usable Mana, Mana/hour, Heat and current floor are understandable. Normal Mode hides the editor grid.
2. Test Small, Default and Large text, including long values. Critical information/actions must wrap or remain accessible rather than truncate. Check cutout/safe-area margins.
3. Select an entity and inspect its contextual sheet. Check selection visibility and the clarity of its localized name/actions.
4. Pan, zoom and Focus floor. Confirm initial fit and bounds, deliberate tap selection, and no world interaction from UI-origin gestures or modal/bottom-sheet interaction.
5. Switch existing floors; confirm only the selected floor is rendered and its changed-floor indication is appropriate.

## Editing and canonical publication

1. Enter Edit Mode. Confirm visible tile grid and used/remaining capacity, with no changes applied to canonical gameplay or mana.
2. Select each supported entity category, choose Move, then tap a valid tile in the same room. Confirm exact target, preserved identity and no resource charge/refund. No drag or cross-room reassignment is supported.
3. Try outside-room, reserved and occupied tiles. Confirm unchanged draft position and understandable invalid feedback; no nearby substitution or movement/deletion of another entity.
4. Observe pending protection and completion. Save Changes must be disabled while pending, failed or unknown. Failure injection is automated; do not corrupt owner storage to simulate it manually.
5. Discard and confirm canonical position is unchanged. Repeat the move and Save Changes, confirm the review surface, then reopen and verify the exact committed position.
6. Make a durable edit without saving, close/reopen and choose Resume Editing. Confirm no automatic canonical publication. Repeat recovery and choose Discard Draft.
7. If a stale-draft outcome is produced using a disposable profile, confirm its explanation and absence of silent merge/rebase. A failed deletion must not claim permanent discard.
8. During an active run, make/save a move. Confirm the active run uses its previous immutable layout and the next run uses the new position.

## Windows standalone and acceptance

Check the standalone's startup, HUD, contextual sheet, Move/tap, invalid feedback, Save/Discard/recovery, floor switching, text modes and mouse pan/wheel/Focus. Record resolution, profile, screenshots, defects and subjective input/readability observations in owner review evidence.

Real Android/iOS touch feel, platform filesystem qualification and native hit-target conversion require later device builds if they are outside the established pipeline. Device Simulator alone is not proof of multi-touch correctness. Full Research/Analysis/More, final art, structural editing, custody browser, drag convenience and Bootstrap retirement remain later scope.
