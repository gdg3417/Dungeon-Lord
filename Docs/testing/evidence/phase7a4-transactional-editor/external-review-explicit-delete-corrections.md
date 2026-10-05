# Phase 7A4 explicit-delete re-review corrections

Reviewed baseline: PR #226 / `b1daea8530166150806343bdf22fb06e749c4747`. Owner recommendation: GPT-6.1 Sol / Medium, Standard; GPT-6 Sol / Medium fallback. The accepted recovery-resolution/preview fixes and candidate/commit-chain protocol are preserved. Schema remains 13; no migration, package or graphical-scope expansion.

## Findings and correction

The production controller assumed an initialized GameRoot always retained Save. Established explicit deletion quiesces the root and clears Save/TimeService even when deletion reports failure. The controller's next Update dereferenced the absent Save, and pending lifecycle flushing could recreate draft evidence after deletion.

A controller-local runtime guard now runs before pending persistence, canonical-dependent presentation and world interaction. Losing Save after initialization marks the controller quiesced, drops only its transient draft/recovery/selection references, cancels gestures, hides its world/camera and disables its UIDocument/controller. It never creates, flushes, commits or discards a draft in that state. Pause/Quit use the same guard, including before the next Update; destruction still restores the Bootstrap camera mask and unsubscribes normally. GameRoot's delete/quiesce semantics are unchanged. A fresh normal root/scene boot may create a fresh save through its existing authority.

SaveService previously removed canonical migration/write evidence and SavePath, but omitted its non-authoritative editor draft. Canonical explicit deletion now reuses FileDungeonDraftStore.Delete(null), its existing bounded ownership/containment and verification boundary. This removes the supported legacy draft path and owned candidate/commit evidence; no duplicate filename parser is introduced.

Ordering is qualified filesystem/preflight and canonical evidence discovery, then verified owned-draft cleanup, then the existing canonical evidence/active-save deletion and directory flush/verification. Canonical verification also confirms the active path is absent. A draft cleanup failure returns the existing failed-delete banner before canonical bytes or canonical evidence are removed. Draft cleanup may be partial after a storage fault; no total-delete, rollback or multi-file atomicity claim is made. The current GameRoot still quiesces on that failure. Unrelated files remain untouched. Normal Discard Draft and unknown-outcome recovery are unchanged.

## Added regressions

Six A4 deterministic integration cases cover successful cleanup of a committed command chain, the legacy draft path and canonical recovery evidence; fresh boot without a draft; failure before/after draft deletion; containment failure; excessive owned evidence; and failed cleanup directory durability. They assert failure truthfulness, canonical preservation and unrelated-file protection.

Two actual-scene scenarios also run through genuine PlayMode adapters: successful explicit deletion and fresh boot, and failed draft cleanup with the established root quiesce. Both begin with a committed draft plus a pending next command, verify no post-quiesce filesystem mutation over subsequent frames and lifecycle calls, reject further editor actions, and destroy the controller safely. Success tests Update observing the missing Save; failure tests a Pause/Quit boundary before that next Update. Existing GameRoot success/failure tests are rerun unchanged.

The first focused run was 9 total / 8 passed / 1 failed because the scene's strict unexpected-log check lacked an expectation for the established successful dev-command deletion warning. That warning is now explicitly expected rather than suppressed. No production fix, skip or failure reclassification was needed for that fixture issue.

The second focused run was also 9 total / 8 passed / 1 failed: the fresh-boot modal check read the unset inline style rather than its USS-resolved hidden state. The fixture now waits one layout frame and checks resolvedStyle.display; runtime behavior was unchanged. The final focused run passes 9/9. No test was skipped or failure reclassified.

Earlier persistence-stop/reassessment evidence remains unchanged. Owner visual/gameplay UAT remains outstanding.

## Final qualification

Validation uses the established isolated checkout with byte-identical changed source/test files, never owner saves. No test was ignored, suppressed or reclassified to obtain a passing result.

| Gate | Final result | Ignored report |
| --- | --- | --- |
| Explicit-delete focus | 9 passed, 0 failed, 0 skipped | TestResults/phase7a4-delete-focused-editmode-final.xml |
| A4 durability/delete | 29 passed, 0 failed, 0 skipped | TestResults/phase7a4-delete-lifecycle-durability-editmode.xml |
| Affected SaveService + actual scene | 65 passed, 0 failed, 0 skipped (56 + 9) | TestResults/phase7a4-delete-service-scene-editmode.xml |
| Existing coordinator + GameRoot + genuine shell | 88 total, 80 passed, 0 failed, 8 established GameRoot skips (56 + 14/8 skipped + 10) | TestResults/phase7a4-delete-root-shell-playmode.xml |
| Complete A4 EditMode | 75 passed, 0 failed, 0 skipped | TestResults/phase7a4-delete-complete-focused-editmode.xml |
| Full EditMode | 1,484 total, 1,483 passed, 0 failed, 1 skipped | TestResults/phase7a4-delete-full-editmode.xml |
| Full PlayMode | 2,927 total, 2,917 passed, 0 failed, 10 skipped | TestResults/phase7a4-delete-full-playmode.xml |
| Explicit production gates | 280 passed, 0 failed, 0 skipped | TestResults/phase7a4-delete-production-gates.xml |
| Windows Development Build | Succeeded; 0 errors, 1 cloud-symbol credential warning; 171,640,435 bytes | TestResults/phase7a4-delete-windows-build-report.json |

Both exact skipped-test fullname sets match the retained Phase 7A3 XML baseline (difference count zero). All 160 canonical integration cases are matched and passed in the full EditMode rerun. Existing successful-delete and failed-delete GameRoot tests pass unchanged. The production gates comprise 65 build gate, 112 export, 57 recovery, 37 loading and nine actual-scene cases. The real production pre-build gate also ran during the successful player build.

Windows build: required DevelopmentBuildUtility.BuildWindowsDevelopment, StandaloneWindows64 Development, Unity 6000.3.2f1, Bootstrap-only. Log: Temp/phase7a4-delete-windows-development-build.log. Complete player: Builds/Phase7A4-DeleteReview-2026-10-05/Windows/Dungeon Lord.exe. Four recaptured deterministic PNGs: TestResults/phase7a4-delete-review-screenshots. These generated artifacts remain ignored and are not committed. They do not qualify owner usability or mobile performance. Existing empty test-assembly, debugger/Mono shutdown diagnostics were not suppressed or changed.

Schema remains 13; no migration, packages, new localization entries or unrelated ProjectSettings changes. No scene/assets were reserialized. GameRoot and FileDungeonDraftStore are unchanged. git diff --check passes; configured LF/CRLF advisories are informational. Canonical data/evidence is preserved on draft cleanup failure; successful explicit deletion removes it through the existing authority. No post-quiescence draft or canonical writes occur in either scene test.

## Review disposition

The two current findings are qualified by these regressions. The outdated recovery finding was corrected by b1daea8 and its accepted tests remain green. Replies and resolution are performed on the existing PR #226 after the correction commit is pushed; no new PR or merge is authorized. Owner visual/gameplay UAT remains required after review.
