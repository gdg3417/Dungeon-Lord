# Phase 7A4 transactional resolution and validation

This continuation preserves the original [persistence stop](blocked-persistence-evidence.md) and [acknowledgement reassessment](acknowledgement-boundary-reassessment.md). They describe the earlier failed designs, not the final protocol.

Baseline: `64d31257f06d1dafbf49593ade88f32b808bd556`, branch `codex/phase-7a4-transactional-editor-production-dungeon`. All implementation remains intentional uncommitted work. No PR or merge has been created. Canonical writable schema remains **13**, with no migration or new package.

## Approved decision and root causes

The first implementation embedded acknowledged sequence N in candidate bytes before its barrier succeeded. Failed flush plus failed rollback could leave that payload active; recovery trusted its embedded acknowledgement. The original regression failed while canonical bytes and mana remained unchanged.

The second stop established that successful and throwing final receipt/marker calls can leave identical surviving bytes. Reconstructing whether the previous process historically received success is not a property the existing filesystem interface can provide. The five storage-evidence probes remain unchanged as evidence of that uncertain-result boundary.

The owner approved distinguishing live acknowledgement from restart commit evidence. An uncertain live result does not advance live acknowledgement or permit Save Changes. Restart may resolve it as committed only from a complete, internally consistent transaction chain. Candidate bytes alone have no authority. INV-12, A0 decisions 16-20 and 85/87, and Specs 28/38 were reconciled narrowly to express this decision; unrelated approved design and historical evidence were preserved.

## Final state machine and storage

`accepted/presented -> Pending -> candidate durable write/check/readback -> separate commit durable write/check/readback -> Acknowledged`

An exception after storage begins instead enters `Unknown`, retaining the old live acknowledged prefix and blocking new commands and Save Changes. A conclusively rejected mock/compare operation can enter `Failed`; its explicit safe retry remains available. Neither failure path changes canonical gameplay or reserves/spends resources.

The independent whole-dungeon command-journal format is version **2**. On disk, record format **1** uses immutable sparse generations under the canonical save's separate `.editor-draft` path:

`<save>.editor-draft.draft-<32-lowercase-GUID>.<six-digit-sequence>.candidate`

`<save>.editor-draft.draft-<32-lowercase-GUID>.<six-digit-sequence>.commit`

Generation zero establishes the empty baseline. Each later candidate carries exactly one ordered command. Identity, relevant canonical baseline, sequence, predecessor commit SHA-256 and exact candidate SHA-256 bind each separate commit record. Candidates contain no acknowledged-sequence declaration. Writes use the existing injected durable filesystem interface, supported-path boundary check and exact readback; there is no new platform-specific persistence dependency.

The predecessor is retained before and after the next commit. Correctness uses no destructive rollback, replacement, or claim of multi-file atomicity. Obsolete evidence is removed only on explicit discard or after successful canonical publication. Interrupted/failed deletion cannot be reported as permanently discarded. Maximum commands/files/bytes and parser work remain bounded by the production workload authorities.

Recovery reads explicit commit records in canonical sequence order, requires generation zero and every predecessor, verifies identities, exact serialization, hashes and matching candidates, reconstructs only the committed prefix, then uses current domain/production validation and baseline comparison. An immediate orphan candidate has no commit authority and is ignored. Missing/corrupt/conflicting commit-chain evidence fails closed with `ui.dungeon.draft.recovery_failed`; it is preserved. Recovery does not write, advance the transaction, merge, rebase, or choose the newest parseable payload. Repeated reads/recovery are idempotent. A surviving post-canonical-commit draft fails the relevant-baseline check.

Live success may expose acknowledged N. Live unknown keeps acknowledgement N-1. Restart independently resolves N if its complete commit chain survives, N-1 if only that chain survives, or an explicit recovery failure if no unique committed chain is proven. Historical callback reconstruction is intentionally absent.

## Fault matrix

| Boundary | Live result | Restart result |
| --- | --- | --- |
| Candidate and commit complete normally | Acknowledged N | N |
| Candidate write before/partial/after mutation fails | Unknown, old acknowledgement, Save blocked | N-1; candidate alone ignored |
| Candidate boundary fails, including original attempted-rollback-failure setup | Unknown, old acknowledgement, Save blocked | N-1; protocol performs no rollback |
| Commit write fails before mutation | Unknown, old acknowledgement, Save blocked | N-1 |
| Commit write throws after full mutation | Unknown, old acknowledgement, Save blocked | N, only with valid complete commit chain |
| Commit boundary/readback fails with complete surviving evidence | Unknown, old acknowledgement, Save blocked | N, only with valid complete commit chain |
| Missing predecessor commit/candidate; wrong predecessor/hash/identity; malformed/duplicate commit field; conflicting session | No canonical publication | Explicit fail-closed recovery |
| Initial candidate survives without initial commit | Unknown, Save blocked | Explicit recovery failure and durable-evidence discard option |
| Initial complete commit survives unknown result | Unknown, Save blocked | Empty committed generation zero |
| Changed canonical baseline | Save blocked | Stale-draft outcome, no overlay/rebase |
| Repeated recovery | No advancement | Identical bytes, prefix, result and retained paths |
| Discard deletion fails before/after removal or at its boundary | Unknown, unchanged acknowledgement, Save blocked; no permanent-discard claim | Remaining committed prefix or no draft when deletion completed; never canonical mutation |

Every injected failure asserts canonical bytes and mana unchanged and rejected canonical publication. N+1 cannot recover without N. Normally acknowledged and conclusively recovered changed drafts can save only after final validation, current-session protection and the separate atomic canonical transaction.

## Production foundation

UI Toolkit UIDocument/UXML/USS/PanelSettings supplies localized HUD, floor selection, safe-area chrome, Edit/Save/Discard, contextual Move, recovery/failure surfaces and text modes. Grid/Tilemap and pooled SpriteRenderers instantiate only the selected floor. A dedicated presentation layer prevents the retained Bootstrap camera from also drawing the production world. Input System device reads are separate from pure viewport/gesture calculations. Real PlayMode touch injection qualifies two-contact pinch and chrome-origin suppression; device feel remains unqualified.

The scene composition defect was a missing serialized PanelSettings reference. Authoring now opens Bootstrap before loading its assets, assigns the persistent panel explicitly, saves and verifies it by reopening. Only the verified panel-reference line was carried into the existing narrow scene change. The retained uGUI/TMP/EventSystem and legacy capabilities remain accessible through development tools.

Same-room Move then tile tap changes only the existing assignment's exact configured starting/placement position. Identity, room, category, option, sequence and custody are preserved; charge/refund is zero. Draft state never becomes simulation, passive mana, lifecycle or wallet authority. Existing active runs retain their immutable snapshot; later snapshots consume the committed position.

## Initial qualification results before external UI review

Validation uses the established isolated checkout at `C:/Users/gdg34/.codex/worktrees/phase7a4-validation/Dungeon-Lord`, with the intentional source changes copied into the same exact baseline. Owner save files and unrelated ProjectSettings are not used or copied back. The connected Unity CLI is `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; Unity remains 6000.3.2f1.

| Gate | Exact result |
| --- | --- |
| Protocol compilation | Succeeded |
| Retained storage-evidence probes | 5 passed, 0 failed, 0 skipped |
| New durability/unknown-outcome matrix, including failed discard | 23 passed, 0 failed, 0 skipped |
| Corrected original reproduction | 1 passed, 0 failed, 0 skipped |
| Prior domain/presenter cases | 37 passed, 0 failed, 0 skipped |
| Canonical save/load/session/complete-save integration | 160 passed, 0 failed, 0 skipped |
| A4 plus production-content focused EditMode before final discard correction | 97 passed, 0 failed, 0 skipped |
| Final A4 focused EditMode after discard correction | 63 passed, 0 failed, 0 skipped |
| Genuine production-shell PlayMode, including real Input System contacts and long localization | 4 passed, 0 failed, 0 skipped |
| Full EditMode | 1,472 total: 1,471 passed, 0 failed, 1 skipped; exact skip set matches A3 baseline |
| Full PlayMode | 2,915 total: 2,905 passed, 0 failed, 10 skipped; exact skip set matches A3 baseline |
| Explicit production content/build gates plus actual scene | 274 passed, 0 failed, 0 skipped: build gate 65, export 112, recovery 57, loading 37, actual scene 3 |
| Windows x86_64 Development Build | Succeeded, StandaloneWindows64, Development=true, Bootstrap-only; 0 build errors, 1 build warning; 171,638,812 bytes |

The earlier broad integration run recorded 1,040 total: 1,038 passed, one failed consumer allowlist, one existing skip. The new read-only production controller was added to the allowlist with explicit assertions that it cannot load or configure production publication; the affected content suite then passed. No failure was ignored or reclassified to obtain a passing result. The touch scenario is qualified in the genuine PlayMode runner because an EditMode coroutine does not reliably advance MonoBehaviour device reads; pure gesture/mapping tests remain in EditMode.

Review also found that failed discard could leave the live Save button enabled even though canonical revalidation would reject changed evidence. Deletion now enters an unknown storage result before mutation, and failed discard changes live durability to Failed/Unknown. Three additional injected deletion boundaries qualify immediate blocking and restart recovery. Both previously passing full suites are rerun after this narrow correction.

Reports are retained under ignored `TestResults/phase7a4-*.xml`, compilation/build logs under `Temp`, and screenshots under `TestResults/phase7a4-screenshots`. Screenshot evidence has no pixel-golden merge requirement and proves neither usability nor hardware performance.

The Windows build used `DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment` through the current Unity CLI with `-buildTarget Win64`. The production pre-build gate completed as part of the successful player build. Its report is retained at `TestResults/phase7a4-windows-build-report.json`; the complete player is copied to `Builds/Phase7A4-2026-10-05/Windows/Dungeon Lord.exe` without overwriting existing owner build directories. The one build warning is unavailable Unity Cloud credentials for native symbol upload. The log also reports the existing empty `Tests 1.asmdef`, debugger-port and Mono shutdown diagnostics; these were not suppressed or addressed by unrelated changes.

Four inspected screenshot artifacts: `phase7a4-edit-large-1080x1920.png`, `phase7a4-edit-large-1920x1080.png`, `phase7a4-edit-large-1536x2048.png`, and `phase7a4-long-localization-1080x1920.png`. The first three include representative injected safe-area margins. They are captured after bounded frame warm-up in isolated deterministic scene state, with canonical tick advancement disabled only in the test fixture. Critical HUD/actions remain in the safe root; the long fixture wraps without truncating those values. This is visual evidence, not owner usability qualification.

Exact case names, stable localization keys/English entries, and complete unchanged skip sets are in [test and localization inventory](test-and-localization-inventory.md). `git diff --check` passes. Packages, Unity version and owner ProjectSettings remain unchanged. No required automated/build gate remains unrun; external review and owner UAT remain outstanding.

## External-review UI correction qualification

Both production UI findings and their corrections are documented in [the companion evidence](external-review-ui-state-corrections.md). The persistence protocol above is unchanged. Current reruns pass: actual scene 7/7, complete A4 EditMode 67/67, genuine shell PlayMode 8/8; full EditMode 1,476 total / 1,475 passed / 0 failed / 1 skipped; full PlayMode 2,919 total / 2,909 passed / 0 failed / 10 skipped. Both exact skipped-test sets match Phase 7A3. All 160 canonical integration cases pass in the full rerun. Explicit production gates pass 278/278. Windows Development Build succeeds with 0 errors, 1 cloud symbol-upload credential warning and 171,639,460 bytes. Reports use the ignored `phase7a4-review-*` XML/JSON/log paths. Schema remains 13; no migration, packages or owner ProjectSettings changes. Owner UAT remains outstanding.
## Latest explicit-delete re-review qualification

PR #226 remains open and unmerged. Both remaining findings are corrected; GameRoot lifecycle and the accepted draft protocol are unchanged. See [explicit-delete correction evidence](external-review-explicit-delete-corrections.md) for root causes, ordering, exact regressions and preserved qualification history.

- Explicit-delete focus: 9/9; A4 durability/delete: 29/29; affected service/actual-scene EditMode: 65/65; lifecycle/shell PlayMode: 88 total, 80 passed, 0 failed, 8 established skips. Actual scene: 9/9; complete A4 EditMode: 75/75; genuine shell PlayMode: 10/10.
- Full EditMode: 1,484 total, 1,483 passed, 0 failed, 1 skipped. Full PlayMode: 2,927 total, 2,917 passed, 0 failed, 10 skipped. Exact skip sets match Phase 7A3; all 160 canonical integration cases pass in the full EditMode rerun.
- Explicit production gates: 280/280 (65 build, 112 export, 57 recovery, 37 loading, 9 actual scene). Windows x86_64 Development Build: Succeeded, Bootstrap-only, Unity 6000.3.2f1, 0 errors, 1 cloud symbol-upload credential warning, 171,640,435 bytes.
- Schema 13; no migration, packages, localization entries, scene/asset serialization, GameRoot, FileDungeonDraftStore or unrelated ProjectSettings changes. Diff whitespace checks pass. Ignored builds, XML/logs and screenshots are excluded from the commit.
- The controller stops canonical-dependent presentation/input and pending persistence when Save disappears, including before the next Update at Pause/Quit. Successful explicit deletion removes and verifies all owned draft evidence before canonical cleanup. Cleanup failure reports failure and preserves canonical data/evidence; existing root quiesce remains binding.
- Owner visual/gameplay UAT remains outstanding; no merge readiness is claimed. The required build and screenshots are retained at the updated ignored paths in owner UAT instructions.

## Latest owner-UAT theme correction qualification

Owner UAT stopped on an unresolved runtime PanelSettings theme. The generated theme in the validation checkout had masked a dangling committed GUID. The panel now resolves project-owned DungeonTheme.tss/meta, importing Unity's built-in runtime theme. Authoring validates before save and after reopen; no warning suppression is used. See [theme correction evidence](owner-uat-theme-correction.md) for the reproduction, root cause and render assertions.

- Theme focus 1/1; genuine shell PlayMode 11/11; complete A4 EditMode 76/76. Full EditMode: 1,485 total, 1,484 passed, 0 failed, 1 skipped. Full PlayMode: 2,928 total, 2,918 passed, 0 failed, 10 skipped. Exact skip sets match A3; 160/160 canonical integration cases pass.
- Production gates 281/281, including 10 actual-scene cases. Windows x86_64 Development Build: Succeeded, Bootstrap-only, Unity 6000.3.2f1, 0 errors, 1 cloud symbol-upload credential warning, 171,640,420 bytes. The build packed-asset log includes DungeonTheme.tss.
- Missing-theme warning is absent in the monitored runtime load and current test/build evidence. Fresh actual-shell Normal screenshot visibly renders mode, all four HUD metrics, Edit dungeon, Focus floor and text-size controls. Remaining owner visual/gameplay UAT is not completed by that targeted rendering check.
- Schema 13; no migration, packages, localization, runtime authority/protocol or unrelated ProjectSettings changes. Bootstrap/UXML/USS remain unchanged. New assets are the TSS and its normal Unity-generated meta.
- Correction/commit whitespace checks pass. Whole working-tree check reports six pre-existing TMP fallback-font trailing-whitespace lines. That owner modification and an untracked test-folder meta remain untouched and excluded. Ignored screenshots, XML/logs and builds are excluded.
- New player: Builds/Phase7A4-ThemeUAT-2026-10-06/Windows/Dungeon Lord.exe. Report: TestResults/phase7a4-theme-windows-build-report.json; screenshots: TestResults/phase7a4-theme-screenshots. PR #226 is updated without merging; owner UAT resumes after review.

## Remaining owner qualification

External review and [owner UAT](owner-uat.md) remain required after automated/build gates and blocking review findings. No visual/gameplay-experience qualification, low-end mobile performance, Android/iOS build, real-device multi-touch feel, Japanese translation or complete accessibility hierarchy is claimed. The existing canonical filesystem platform qualification remains unchanged. Primitive art, Bootstrap coexistence and the single reposition mutation are intentional scope limits.
