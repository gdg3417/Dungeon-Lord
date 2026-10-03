# Phase 7A2 owner manual Unity/UAT checklist

Status: **owner qualification PASS** — Editor/manual and Windows standalone qualification completed on Unity 6000.3.2f1.

Do not use Clean MVP Validation Reset and do not replace the owner's existing save solely for this qualification.

1. Open the project in Unity 6000.3.2f1 and confirm import/compilation completes without a new relevant Console error.
2. Back up the existing schema-12 save outside the game save directory before first migration.
3. Load the existing save through the ordinary Bootstrap flow and confirm it migrates once to schema 13 without a mana charge/refund or changes to room/floor identity, layout, orientation, structural investment, lifecycle state, returned custody, or unrelated state.
4. Record representative migrated monster, trap, and loot positions against the reviewed Phase 7A1 profile for each room/orientation in use. Schema 13 `RoomLocalPosition` is canonical base-local state, while Phase 7A1 profile anchors are orientation-specific room-relative coordinates. For a Zero-orientation room, compare the stored canonical coordinate directly with the corresponding frozen profile anchor. For a rotated room, do **not** compare the stored `RoomLocalPosition` directly with the frozen anchor: transform the stored canonical base-local coordinate forward using the production room definition's base `GrossFootprint` and the saved room orientation, then compare that oriented room-relative coordinate with the frozen profile anchor. Use assignment ID, category, option, and sequence in canonical assignment order to identify the matching frozen slot. Do not change or regenerate the frozen Phase 7A1 profile asset.
5. Close Unity completely, reopen, reload, and confirm the same assignment identities and positions persist without another migration.
6. Place new monster, trap, and loot content using deliberate valid room-local coordinates. Confirm each persists across save/reopen.
7. Attempt an out-of-bounds, unsupported, and occupied coordinate. Confirm each fails visibly with no mana/custody/canonical-state change. Current production Basic Room, Rectangle Room, and Large Chamber reserved-tile collections are empty, so do not fabricate or modify production data to test reserved-tile rejection. Reserved-tile rejection is covered by automated synthetic coverage for this PR; synthetic multi-tile footprint behavior is likewise automated coverage rather than a required production manual case.
8. Return content to custody and redeploy it to a deliberate valid coordinate. Confirm identity survives and positioning alone causes no mana charge/refund. Attempt invalid redeployment and confirm returned custody is retained.
9. Translate a room and confirm saved room-local positions remain unchanged while resolved floor positions move with the room. If a supported orientation change is available through the existing diagnostic surface, confirm deterministic transform behavior; do not infer a new player-facing rotation feature.
10. Retained content must never be silently relocated by structural replacement. If the current saved layout exposes a normal renovation replacement that would invalidate retained positional content, test it and confirm rejection with unchanged content state and no partial mutation. If the existing diagnostic surface cannot exercise that case without changing production data or risking the preserved owner save, mark this individual manual case not applicable and cite the automated replacement-rejection coverage; do not manufacture a production state solely for this checklist.
11. Confirm a material position change makes prior floor knowledge inapplicable through the normal authority, while activation-only changes retain applicability.
12. The Windows Development Build compilation passed. For repeat standalone qualification, use a disposable qualification save, not the owner's preserved save: launch the built Windows player; create or load canonical schema-13 state; place content at an explicit valid coordinate; save; fully exit the player; relaunch; and confirm the same assignment identity and `RoomLocalPosition` reopen correctly. Save, exit, and relaunch once more where useful to demonstrate no migration replay or positional drift. Record any player-visible error/banner and relevant log error.

Record Unity version, platform, save backup path, schema before and after migration, room definition and orientation, assignment ID/category/option/sequence, stored canonical base-local `RoomLocalPosition`, and—for rotated migrated rooms—the transformed oriented room-relative coordinate used for the A1 comparison. Record the matching A1 slot ID/anchor where applicable, mana, returned custody, structural investment, reopen result, standalone reopen result, Console/log observations, and useful screenshots in the PR review record. Do not mark this checklist passed until the owner performs it.

## Owner qualification result — PASS

The owner completed Editor/manual qualification and Windows standalone qualification on Unity
6000.3.2f1 and the Windows Development Build.

### Editor/manual cases passed

- The existing owner save loaded as schema 13 after migration. It retained both floors,
  lifecycle state, structural investment, returned custody, room/content identity, and
  authoritative room-local positions.
- The save was opened before a pre-migration schema-12 backup was created. No owner-preserved
  schema-12 byte-for-byte backup exists; owner-provided historical schema-6 backups were not
  used as substitutes. The automated schema-12-to-13 migration qualification remains the exact
  source-byte-preservation and migration-contract evidence.
- Representative migrated Basic Room Zero-orientation positions matched the frozen Phase 7A1
  slot contract. A full Unity close/reopen retained state and positions without remigration,
  recovery warning, drift, or loss.
- Explicit placement passed, including Floor 1 Spike Trap at `(1,2)` and Floor 1 Hidden Cache at
  `(3,3)`. Normal acquisition mana costs behaved as configured, with no additional positioning
  cost.
- Out-of-bounds and occupied-coordinate rejection passed atomically. Unassignment/redeployment
  retained ownership with no positioning mana charge/refund, and invalid redeployment retained
  returned custody.
- Material room-content position changes made applicable Floor 2 knowledge unknown; a surviving
  Floor 2 completion relearned it, and a subsequent run again reported known information.
  Activation-only deactivate/reactivate preserved applicable floor knowledge.
- The 1280x720 Editor presentation, position controls, and player-facing localized text were
  readable. No raw localization keys or new Phase 7A2 save, migration, recovery,
  positional-validation, or structural errors were observed.

### Preserved-topology manual cases not exercisable

- Room movement/content preservation was not manually exercisable: X+, X-, Y+, and Y- around the
  current anchor were rejected, so no invalid move was committed. Automated
  movement/content-preservation coverage passed.
- Retained-position replacement rejection was not manually exercisable: Large Chamber replacement
  was rejected by connection geometry and Rectangle Room replacement earlier by structural layout
  validation; no replacement was committed. The direct automated regression
  `ReplacementRejectsRetainedContentWhoseRoomLocalPositionWouldBecomeInvalid` passed.

### Accepted expected warnings

Two existing TimeService clock-skew warnings were observed after pauses of 327 and 361 seconds:
`Time delta looks large: 327 seconds.` and `Time delta looks large: 361 seconds.` The configured
`detectClockSkewSeconds` value is 300, so both are expected and are not Phase 7A2 failures.

### Windows standalone qualification passed

The owner used an isolated disposable standalone save rather than the preserved owner save. The
Windows Development Build created/loaded canonical schema-13 state, accepted explicit room-local
placement, and preserved assignment identity and `RoomLocalPosition` through full process exit and
relaunch. A second save/full-exit/relaunch also passed without migration replay, recovery failure,
positional drift, or a player-visible corruption/recovery failure.
