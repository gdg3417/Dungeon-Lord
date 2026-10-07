# Phase 7A4 external-review UI state corrections

Correction baseline: PR #226, `270505bc67ad438e8ef8812fa676e5a974d65c7a`, on `codex/phase-7a4-transactional-editor-production-dungeon`. The owner classified these understood production UI defects as Standard, recommending GPT-6.1 Sol / Medium with GPT-6 Sol / Medium fallback. The candidate/commit-chain protocol and approved unknown-outcome interpretation remain unchanged. Earlier persistence-stop and reassessment evidence is preserved.

## Findings and corrections

1. **Recovery resolution could dead-end Edit Mode.** A stale or fail-closed recovery had no resumable draft but retained blocking evidence. Cancel closed its only resolution surface. Confirm closed the modal before deleting evidence, and a failed deletion did not restore it. Unresolved recovery now offers a non-dismissible Discard surface; cancellation cannot strand it. Failed deletion restores that surface with the existing localized delete-failure instruction. It invalidates the pre-deletion recovered snapshot and removes Resume, because a failed deletion may have partially removed its chain. Retry explicitly discards the remaining owned evidence. Only proven successful deletion clears the recovery gate and enables Edit. Valid recovery retains Resume and Discard.
2. **Cleared selection retained a world marker.** CloseSheet cleared the selected object/tool but never deactivated the preview renderer. The renderer now provides ClearPreview and read-only PreviewVisible. Closing genuine selection/move context calls ClearPreview; floor rendering uses the same API. Empty selection, floor change, successful Discard and Commit clear the marker. Invalid targets remain red and visible while Move is active.

All player instructions reuse existing localization keys. No gameplay authority, mana rule, canonical schema, migration, package, Unity asset or ProjectSettings change is introduced by this correction.

## Added production-scene regressions

Four actual-scene scenarios run in both the EditMode scene fixture and genuine PlayMode adapter:

- `UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry`: stale and malformed committed evidence, blocked dismissal/Edit/Save, deletion failure, localized retry surface, successful cleanup, Edit re-entry, and unchanged canonical bytes/mana.
- `ValidRecoveryResumeKeepsCanonicalAndManaUnchanged`: recovered acknowledged prefix, Resume, enabled Save, and canonical/economy isolation.
- `ValidRecoveryDiscardFailureRemovesResumeAndAllowsRetry`: failed valid-draft deletion cannot reuse its recovered snapshot; retry and ordinary valid Discard both clear evidence and permit Edit without canonical/economy changes.
- `SelectionCloseEmptyTapFloorDiscardAndCommitClearPreview`: selected object then Close, empty-world tap, active red invalid Move preview, actual Floor 1/2 switch, Discard and Commit marker clearing.

The fixtures use isolated disposable save files. Buttons invoke their registered UITK Clickable, including the generic confirm callback; the existing real Input System scenario separately exercises device dispatch. A second canonical zero-cost draft changes the stale-test baseline without touching its original draft evidence. The selection fixture constructs the existing configured Floor 2 through its canonical authority, with test-only research permission and configuration-owned construction funding.

## Qualification history

The initial scene run was 7 total / 3 passed / 4 failed: synthetic NavigationSubmit events did not invoke the registered button callbacks, and trap acquisition exceeded the fixture wallet. The next run was 7 total / 6 passed / 1 failed because a new-game save contains only Floor 1. These fixture defects were corrected without skips or failure reclassification. Inspection also caught and corrected cancellation fall-through after restoring a failed-deletion modal.

## Final qualification

All runs use the established isolated Unity checkout, with the four changed source/test files copied from the reviewed implementation. No owner save is used. Results are retained in ignored locations:

| Gate | Exact result | Evidence |
| --- | --- | --- |
| Actual production-scene EditMode | 7 passed, 0 failed, 0 skipped | `TestResults/phase7a4-review-scene-editmode-qualified.xml` |
| Genuine production-shell PlayMode | 8 passed, 0 failed, 0 skipped | `TestResults/phase7a4-review-shell-playmode.xml` |
| Complete A4 focused EditMode | 67 passed, 0 failed, 0 skipped | `TestResults/phase7a4-review-focused-editmode.xml` |
| Full EditMode | 1,476 total: 1,475 passed, 0 failed, 1 skipped | `TestResults/phase7a4-review-full-editmode.xml` |
| Full PlayMode | 2,919 total: 2,909 passed, 0 failed, 10 skipped | `TestResults/phase7a4-review-full-playmode.xml` |
| Explicit production/content/actual-scene gates | 278 passed, 0 failed, 0 skipped: build gate 65, export 112, recovery 57, loading 37, actual scene 7 | `TestResults/phase7a4-review-production-gates.xml` |
| Windows x86_64 Development Build | Succeeded, StandaloneWindows64, Development=true, Bootstrap-only, Unity 6000.3.2f1; 0 errors, 1 warning; 171,639,460 bytes | `TestResults/phase7a4-review-windows-build-report.json`, `Temp/phase7a4-review-windows-development-build.log` |

Both skipped-test fullname sets exactly match the retained Phase 7A3 baseline XMLs: difference count zero. All 160 cases from the existing canonical save/load/session integration report are present and passing in the current full EditMode XML. No tests were suppressed, ignored or reclassified. The Windows build uses `DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment`; its real production pre-build gate passes. The one build warning remains missing Unity Cloud credentials for native symbol upload. Existing empty-asmdef and debugger/shutdown diagnostics were not suppressed.

`git diff --check` passes. Writable canonical schema remains 13. Draft-domain/storage/commit sources, packages, localization entries, Unity assets and owner ProjectSettings are unchanged. New runtime code only changes presentation state and recovery UI gating. Recovery UI operations preserve canonical save bytes and mana, as asserted by both runners. The initial persistence-stop and reassessment documents remain unchanged.

The complete new Windows player is retained separately at `Builds/Phase7A4-Review-2026-10-05/Windows/Dungeon Lord.exe`, alongside its dependencies. Current representative screenshots are retained separately at `TestResults/phase7a4-review-screenshots`; neither generated output is committed. Earlier builds, screenshots and failure evidence remain available.

Owner visual/gameplay UAT remains outstanding; automated scene tests do not qualify usability or real-device feel. PR #226 is updated, not replaced or merged.
