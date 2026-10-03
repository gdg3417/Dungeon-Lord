# Phase 7A2 owner manual Unity/UAT checklist

Status: **not yet run by the owner**.

Do not use Clean MVP Validation Reset and do not replace the owner's existing save solely for this qualification.

1. Open the project in Unity 6000.3.2f1 and confirm import/compilation completes without a new relevant Console error.
2. Back up the existing schema-12 save outside the game save directory before first migration.
3. Load the existing save through the ordinary Bootstrap flow and confirm it migrates once to schema 13 without a mana charge/refund or changes to room/floor identity, layout, orientation, structural investment, lifecycle state, returned custody, or unrelated state.
4. Record representative migrated monster, trap, and loot room-local positions and compare them with the reviewed Phase 7A1 profile for each room/orientation in use.
5. Close Unity completely, reopen, reload, and confirm the same assignment identities and positions persist without another migration.
6. Place new monster, trap, and loot content using deliberate valid room-local coordinates. Confirm each persists across save/reopen.
7. Attempt an out-of-bounds, reserved, unsupported, and occupied coordinate. Confirm each fails visibly with no mana/custody/canonical-state change.
8. Return content to custody and redeploy it to a deliberate valid coordinate. Confirm identity survives and positioning alone causes no mana charge/refund. Attempt invalid redeployment and confirm returned custody is retained.
9. Translate a room and confirm saved room-local positions remain unchanged while resolved floor positions move with the room. If a supported orientation change is available through the existing diagnostic surface, confirm deterministic transform behavior; do not infer a new player-facing rotation feature.
10. Attempt a structural replacement that would invalidate retained content and confirm the replacement is rejected without relocation or partial mutation.
11. Confirm a material position change makes prior floor knowledge inapplicable through the normal authority, while activation-only changes retain applicability.
12. Run the Windows Development player and exercise create/save/reopen on a disposable qualification save. The automated player build passed, but this manual runtime check remains owner-owned.

Record Unity version, platform, save backup path, before/after schema, representative assignment IDs/positions, mana/custody/investment checks, reopen result, Console observations, and any screenshots in the PR review record. Do not mark this checklist passed until the owner performs it.
