# Existing Windows C2 player: prepared two-floor UAT save

This is an environment/save preparation, not a gameplay correction or owner UAT approval. Branch `combat-c2-monster-continuity`; reviewed/qualified player source `62929c35c6300fcc8af9b29e7d9dd8ac9f6d9896`. Recommended configuration: GPT-6.1 Sol Medium, focused UAT environment repair/save-safety verification (policy Standard), fallback GPT-6 Sol Medium.

## Launch now

Double-click the existing **Dungeon Lord.exe**:

`C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2\Builds\Development\Windows-combat-c2-reserve-ui-20261016\Dungeon Lord.exe`

The player is closed after the smoke check. It should show **Floor 1 and Floor 2, both Active**, with rooms, defenders, traps and loot. No manual copying, JSON editing, folder creation, PowerShell, reconstruction or new build is needed. Initial total mana is 1000, required reserve 27, usable mana 973. The manual action is **Development tools → Observe Dungeon** (scroll the existing action panel if needed). Return with the bottom-right graphical-dungeon button. No run was consumed by this smoke check, so saved history still has NextRunSequence 79. Detailed historical replay is unavailable after restart under the established C1 contract; that message is expected.

## Verified root cause

The primary repository config uses `save_primary.json`; the retained r2 config and packed player use **`combat-c2-reserve-ui-20261016.json`**. All 303 player files match the qualified build manifest. Its adjacent `Dungeon Lord_Data` exists; `build-report.json` says Succeeded, Unity 6000.3.2f1, Windows x64 Development Build. The isolated namespace is present in `Dungeon Lord_Data/sharedassets0.assets`.

Both project identities are company `gdg3417`, product `Dungeon Lord`. SaveService uses `Application.persistentDataPath`, giving the verified directory `C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord`. The normal native creator explicitly initializes `Floors = Array.Empty<SavedSpatialFloor>()`. A qualified executable does not bundle or automatically select a user's developed dungeon. The first launch of this new namespace therefore created a valid schema-15 save with zero floors, rather than loading the protected C0 dungeon. No separate runtime defect was found.

## Source, destination and preservation

Selected source, read-only throughout:

`C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord\combat-c0-diagnostics-validation-20261015.json`

Its SHA-256 **before copying and after both launches/normal closes** is:

`54c7744e7ee92b1597daa88df995ef07c120e34667398377be78591f7fb7aa13`

This legitimate schema-14 owner dungeon has two active configured floors (`compat.floor.00`, `canonical.floor.01`), two rooms, ten deployed assignments (four monsters, two traps, four loot nodes), and four returned items (two monsters, one trap, one loot node). It retains wallet 1000, Heat 4.2, completed research `ac_100` / `ac_300`, and NextRunSequence 79. Both floor layouts render as Valid in the actual Windows player. The six owned monsters are five Skeletons and one Goblin. This existing authorized source is sufficient; no unrelated save or `save_primary.json` was selected.

Exact destination:

`C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord\combat-c2-reserve-ui-20261016.json`

Before preparation it was schema 15, 5,663 bytes, zero floors, zero owned monsters/items, no research, NextRunSequence 1 and wallet 42.33333333333335 from bootstrap ticking. SHA-256 was `b0cdc750e9501191ed63ae9d2f03a48b8e41796de33d07506ddc8dacf76456ba`. Its only two sidecars were a matching sequence-zero draft candidate/commit pair with no commands. No meaningful C2 progress was overwritten.

Preparation used one-time scripts retained locally under repository `Temp`, with the existing `qualification_inputs.no_redirect` filesystem guard. Exact source/destination hashes and schemas were required; all destination sidecars were inspected, candidate/commit hashes matched, and no Unity Editor or Dungeon Lord player was running. The Unity Hub helper was distinguished from a Unity Editor. Source/destination/backup paths and parent chains were checked for symlinks/reparse points; only the named destination was writable.

Steps performed:

1. Preserve the empty target and move its two zero-command sidecars to `C:\Dev\Dungeon-Lord\TestResults\combat-c2-uat-save-preparation\empty-bootstrap-before-preparation\`. No stale editor draft was carried into the copied dungeon.
2. Read the protected source bytes, recheck its approved hash and unchanged destination, write/fsync an exclusive temporary file, atomically replace only the named C2 target and verify exact byte-for-byte readback. At copy time the target was the identical schema-14 save, including identities, layout, assignments, wallet, progression and history.
3. Preserve that prepared schema-14 baseline locally, then launch the **existing qualified standalone**. Its normal `SaveService.LoadOrCreate` / `DetachedSpatialSaveLoadCoordinator` path performs strict supported 14→15 migration and durable canonical publication. No alternate serializer or manually fabricated schema-15 payload was used.
4. Inspect the actual UI, close normally, reopen the same executable, inspect again and close normally. Keep the final schema-15 baseline locally. No restoration of the prepared target was necessary: no combat or editing was performed, only normal lifecycle/tick metadata advanced.
5. Verify the source hash again, retained floor/content identities, progression/history, all continuity records, all 303 qualified player files, all 1,324 qualified repository Unity inputs and all 1,312 warm-owner input/log/settings files. All match their expected state; the warm project was never opened/refreshed.

The backup directory is narrowly scoped to this repair. Raw save baselines and startup logs remain local and are not committed to the PR. [preparation-and-smoke.json](preparation-and-smoke.json) records hashes, counts and assertions. Existing qualified/history players were not overwritten. **Zero new Unity projects, zero rebuilds, zero runtime changes, zero full test cycles.**

## Schema 15 and smoke evidence

The copied target is now schema 15 with the original two floors, two rooms, ten deployed assignments and four custody items intact. The normal migration created exactly six continuity records matching all owned monster AssignmentIds, including custody. Each has level 1, configured full HP (Goblin 8, Skeleton 10) and RevivalRequired false. Only normal lifecycle timestamp/tick members differ from the source's existing primary members; ownership, spatial layout, research, investment, knowledge, dormant Living Dungeon state, wallet/Heat and run history are retained.

Automated native Computer Use smoke observed:

- Startup and reopen without an invalid-save banner or canonical-load error.
- Both active floors and their configured room/content rendering; Floor 2 [capture](floor-two.png).
- Total/usable/required mana 1000/973/27, consistent with three deployed Skeletons and one deployed Goblin; custody adds no obligation.
- [Skeleton inspection](skeleton-inspection.png): level 1, HP 10/10, readiness 7, living/combat eligible.
- Existing [manual-run control](manual-run-control.png) available; it was **not clicked**.
- [Reopened dungeon](reopened-dungeon.png) retains both floors/content. NextRunSequence remains 79.

The Computer Use launch helper initially timed out waiting for a target window on each launch, but rediscovering the already-running player's unique window immediately succeeded. No duplicate player was launched. Actual UI, canonical save readback and clean startup logs establish this limited smoke result. Owner consecutive-run/recovery/affordability/custody/save/replay UAT and independent ChatGPT review remain pending. PR #240 stays draft and unmerged.

## Required preparation rule for future UAT

Before asking the owner to launch any isolated Windows build, verify the **actual packed save filename and expected floor count**. A new native save has zero floors, and a build does not supply the owner's dungeon. UAT needs a verified existing dungeon copied to the build's isolated namespace and migrated only there through the normal loader. Preserve the original and any meaningful destination progress; if the source hash changes unexpectedly or destination disposability is uncertain, stop for owner choice.

Reuse existing Unity projects and qualified players. **Additional Unity project copies require explicit owner approval.** Do not seed production startup, import unrelated saves, use `save_primary.json` as the target or reseed over subsequent C2 UAT progress. Preserve this prepared destination for the owner's next launch.
