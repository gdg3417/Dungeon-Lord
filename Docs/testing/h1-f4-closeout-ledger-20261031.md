# H1-F4 continuous closeout ledger — 2026-10-31

PR #259 stays draft. No owner UAT, ready-state change or merge is authorized.

## Proven PlayMode baseline correction and diagnostic removal

- Owner-authorized co09 recovery (`01863577888d4b8780b96999b80dee68`) ran once at `fc6d024c1a3048e94bf94cd3571936274ce436eb`: receipt `TestResults/h1-f4-20261027-co09/owner-approved-recovery01/receipt.json`, SHA `aaf4e7cd78d9022de76d66e4926b0475ff72bac8a02aea6011cc236709a88244`. Independent receipt `post-recovery-independent01.json`, SHA `0848ad9d1013badb62a9b51abdd14cb8bea894f0d08bf5734ab1c38e2245a61e`, verifies all four originals/current backups, all 1,494 inputs/archive members, 367 prior evidence directories, protected owner/failed saves, correct reservation release and OS lock/process/Job state. Historical INCOMPLETE / UNCERTAIN and raw 2/1/0 remain unchanged.
- Exact source-pinned diagnostic proof is [published with the complete recovery evidence](https://github.com/gdg3417/Dungeon-Lord/pull/259#issuecomment-6550453836). One ordinary `Update/UpdateForeground/PumpForeground/AdvanceForeground` publication followed a `foregroundDirty=True` baseline. Entire bytes differ only by `.0` appended to the integer ManaReserve token: 9,660→9,662, first360, before SHA `da3310238149e9b607e026cb9163e8befba1ca479c69f2c1e10dfc365d32b63f`, after SHA `662afdcd79474a5703677eaa8f31625527f9aadac8207098c3a2ff0c9ffb47ef`. Full gameplay serialization, the time owner and session/disk agreement checks passed before the original strict byte failure. Direct Research/mana fixture setup was unchanged by that publication.
- Correction: use the existing bounded `StabilizeReadOnlySceneBaseline()` immediately after floor construction, before raw byte capture. Preserve all strict raw-byte, mouse/touch, floor identity, camera, pooling and save-integrity assertions. Retain the extra runtime/time-owner/session-disk integrity checks; remove temporary publication listeners, stack and byte-delta logs. Remove the temporary co02 phone/pointer, encounter, wheel and content-preview traces, retaining canonical overlay funding. No production or authority change. Qualification of the corrected/cleaned source is pending.
- Prior verified source `4c832847` retains focused12/0/0 and complete ordinary EditMode108/0/0 with all five stages and independent preservation; complete109 PlayMode raw108/1/0 remains historical INCOMPLETE / UNCERTAIN after its separate approved recovery. Original-84 remains84/84 exact-name coverage, not full-suite qualification. All raw failed/uncertain evidence remains retained.

### Recurring generated-input escalation: existing procedure and follow-up

Brief inspection of `Tools/Presentation/qualify_h1_f4.py` confirms preparation already seals only the measured PlayerSettings newline/identifier-order change and two other settings rewrites, retaining exact originals. Its `run_child` accepts/restores the known TMP rewrite only after a zero exit; nonzero plus changed font deliberately fails closed before its CLI-exit receipt is written. Existing guarded normal-FAILED finalization requires unchanged qualified inputs and cannot be substituted for this case. There is no approved existing automatic path that prevents a failed run with changed TMP from requiring exceptional preservation review.

Follow-up, separately scoped and reviewed: retain the actual CLI exit before surfacing input-change refusal, and assess prospective handling of only the known font serialization with complete process/Job, exact input/archive and preservation proof. No supervisor/policy change, presealed font substitution, relaxed checks or automatic reservation release is implemented here. Continue the established procedure; a new uncertain campaign still stops qualification for separate owner authorization.

## Ordered-scene continuation after approved co02 recovery

- Starting source: `87de9780c170b09c751d79a770cd3f22fa237b43`, main `3eb6ccf93558b8145d64e9e63cbefcbd73484602`; fetched refs agree. Existing managed worktree retained.
- Discovery co01's separately approved alternative completion review is accepted; retrospective PID access remains inaccessible, without new lookup or general policy change. It discovered 2,819 EditMode /4,297 PlayMode leaves and executed zero tests.
- Ordered ordinary scene co02, campaign `c4c53a7c03a447668bf3b6992ef55000`, retains **103/5/0, INCOMPLETE / UNCERTAIN**. The sustained method was explicitly deferred. Owner-authorized hash-verified recovery ran once and independently passed: all four originals and 1,494 inputs restored, preserved current copies verified, 357 prior evidence directories and protected saves unchanged, correct reservation released, source clean. [Complete recovery receipt and independent evidence](https://github.com/gdg3417/Dungeon-Lord/pull/259#issuecomment-6549395886). No historical verdict changed.
- Five broader/order failures are not a reversal or subtraction from original-84 exact-name passing evidence. New causes remain subject to current-source qualification.
- `OverlayCompactFootprintCardsAndRuntimeCaptures`: **B, fixture modernization**. It directly assigned live mana=1000 while its disk baseline retained the canonical wallet=15; discard restores canonical state. Use the existing canonical QA funding action and strict normal-publication stabilization before the original raw-byte baseline. No expected value or gameplay assertion changed. Qualification pending.
- `AnalysisPhoneValidDraftChoicesUseTouchButtons`, `LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState`, `OverlayFullWidthSafeAreasAndTransparentInput`, `RoomContentRepeatedTrapInstances`: classification unresolved. Bounded test-only traces record actual click/down/up and modal rejection; scheduler boundaries/observation flags; wheel device/value/zoom; and draft preview/publication rejection. Existing assertions remain. No production defect is presumed.
- Compile-only diagnostic `TestResults/h1-f4-scene-compile-20261031-09` passed using existing Roslyn response and isolated outputs, with unchanged input/protected maps, reservation and lock. This is not NUnit qualification.
- New preparation/diagnostic artifacts remain closeout-owned under `TestResults/h1-f4-*`; purpose is five-failure reproduction and canonical baseline correction. Retain failed states, raw logs, original archives and independent receipts. No cleanup or owner-save operation is authorized.
- Continue with one exact selected scene cohort containing the five targets, relevant predecessors, adjacent content/focus and clock controls. No full-suite/native/performance result is claimed. All required later gates remain pending.

## Source and model

- Starting local/pushed source: `efc81e1a4a664741a6d2a09cfe944b56d39001e3`.
- Fetched main: `3eb6ccf93558b8145d64e9e63cbefcbd73484602`; existing managed worktree retained. The owner's `C:/Dev/Dungeon-Lord` checkout is preserved.
- Recommendation: GPT-6.1 Sol Medium, Standard bounded fixture corrections; GPT-6 Sol Medium availability fallback. No deliberate model substitution or escalation. Exact selected SKU/effort is not independently exposed by the execution tools; no change to the app's model selection was requested or performed. Complex production/native/performance work will be reassessed under the model policy before correction.
- Initial preservation: original 1,494 inputs and protected owner/historical maps match the independently verified Analysis campaign; reservation null; prior processes/Jobs absent; no conflicting Unity workload.

## Current source work

Packet A, **QUALIFIED** at `654c02119acc43c4846f4474d231d5a36c03004a`: complete six-method C1 cohort plus two clock controls **8/0/0 EditMode** (`e77e46ba480d4ff6add230f5c1e52f87`) and **8/0/0 actual PlayMode wrappers** (`ccc0e4dfe3664ea687a45e0ebc92eb8e`). All five stages and independent checks passed. Bootstrap inputs are authored before ownership; feedback scenarios receive fresh isolated owners. The failed 3/1/0 focus is retained, with guarded normal restoration and independent verification. Its successive-boot filename readiness defect was corrected. No production or frozen-contract change. [Report](h1-f4-combat-c1-qualification-20261031.md).

## Retained gates and reconciliation

- Original-84 coverage ledger at entry: **78 qualified /6 outstanding**, not current full-suite failure totals.
- Analysis 21/0/0 at `596837f29377d4c3a9312384b7b8be975b306b2a`, campaign `8a8dc6e0e4ca448dab9a193940d40792`, retained with all five stages and independent verification.
- R1 reporting 306/0/0 at its pinned source remains accepted; not reopened.
- All historical FAILED/INCOMPLETE/UNCERTAIN verdicts and evidence are retained. The installed runner's failed-predecessor/domain-reload continuation risk remains a triage constraint, not permission to skip or suppress failures.

All outstanding names have prefix `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.`:

| Original identity | Current status |
| --- | --- |
| CombatC1PartyFollowsRoomsCorridorsAndFloors | PASSED exact identity and PlayMode wrapper at Packet A source |
| CombatC1TargetFeedbackTracksRecordedHitsAndDefeats | PASSED exact identity and PlayMode wrapper at Packet A source |
| GraphicalFloorReviewMeaningfulInvalidations | PASSED EditMode and actual PlayMode counterpart at `ed5af6f3` |
| GraphicalFloorReviewOpenConfirmationRejectsUnpublishedChanges | PASSED EditMode and actual PlayMode counterpart at `ed5af6f3` |
| RoomContentMapReassignmentProductionMouseTouchRecovery | PASSED EditMode and actual PlayMode counterpart at `ed5af6f3` |
| VisualRunReplayProductionLifecycleAndResponsiveControls | PASSED EditMode and actual PlayMode counterpart at `ed5af6f3` |

## Mandatory remaining sequence

1. Packet A complete; current original-84 coverage **80 qualified /4 outstanding**. Its historical missing-label risk remains explicitly unproven.
2. Packets B/C/D/E complete: **14/0/0 EditMode** (`3d35ed78a25d489eb700a5e280a11cbf`) and **14/0/0 actual PlayMode counterparts** (`1286fbafcdd8471190dca29f854e8fe1`) at `ed5af6f3473b95eabacfa2993a15ac49257d43b1`, all five stages and independent preservation. Exact-name original-84 reconciliation **84 qualified /0 outstanding**, not a full-suite result. Three replay diagnostic/failed attempts remain FAILED with guarded normal restoration and independent verification. [Report](h1-f4-scene-packets-qualification-20261031.md).
3. Complete applicable ordinary production-scene/PlayMode integration; explicitly schedule the sustained 68-run method rather than silently omitting it from a claimed complete class.
4. Fresh full discovery and complete EditMode/PlayMode suites with exact skips and final-source applicability.
5. Remaining original/residual, accepted-work, rollback/uncertainty, repeated interruption and historical/current reopen gates using existing approved owned-process harnesses.
6. Isolated Windows Development Player build **and** actual execution/reopen/native exit. The intermittent shutdown issue remains open.
7. Matched main/final H1-F4 one/two-floor performance and memory, then entire-PR architecture/evidence review.
8. Prepare isolated owner-UAT handoff after automated stability; independent review authorizes UAT. Do not launch it.

Unit/SIT scope and exact source/campaign results will be appended at completed gates. No new full-suite, Player, performance or UAT result is claimed here.

## Owned preparation artifacts

### 2026-11-02 boot/starter packet qualified

Implementation `9381f86103e5d6ae36e48a9767799e13d8a68888`; co18 `4669ef904f6448a9a53d4b9faa22eec6` PASSED13 stages. Complete boot/schema PlayMode18/0/8 existing skips; consecutive starter EditMode1/0/0 and PlayMode1/0/0. All14 co12 failed names now passed. Independent SHA `9df6529eb54b305a0cb60dc21efe82e57d3607b13eab8654f8804a16f6f35636`,1494 inputs/archive and395 prior directories preserved, reservation released. [Source-pinned report and exact-name JSON](h1-f4-boot-starter-qualification-20261102.md).

Bounded production fix shares ordinary loaded-save recovery barrier with Narrow Hall repair; it prevents Home before episode closure. Fixture fixes preserve one prebound clock, consumed configuration, frozen reader refusal, schema17 writer and GUID-owned starter copies. Co15 FAILED9/8/0 and co17 FAILED132/1/8 retain their verdicts and independently verified normal-failure restoration. Co16 passed17/0/0; co17's115 recovery cases retain applicability across the final test-only logger fix. No new exceptional recovery. Original84 remains84/84 focused evidence; full suites/native/performance/UAT gates remain pending. Next: fresh full discovery and complete mandatory suites including sustained workload, then remaining lifecycle/native/performance and accumulated review. Owner UAT NOT STARTED.

### 2026-11-02 continuation — co13 recovered; boot corrections in qualification

- Starting local `affcb6281056989b8a0672869b8bb6a232bdb4ab`, pushed `ee3528821afbb21282bb1152cd19d715b204a8af`; refreshed main remains `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. No owner-checkout operations.
- Owner-authorized co13 recovery02 completed exactly once, exit0. Execution receipt SHA `21b24010fadbe48b7ebbc626a8eacf83a12bd1221471bca4252e559681bd7566`; independent receipt SHA `4fad0d40914e3e0a441e4b82b5140065166e2b7cb824ebc5066598244824ca6c`. Resume verification `TestResults/h1-f4-20261027-co13/post-recovery-resume03.json`, SHA `eb1bf8039e05f6dbc21d5af3e7ddbebd077acf15418374f9b38973def4029c78`, passed:1494 original inputs, TMP,377 prior directories, saves/receipts, clean source, correct released owner and OS-lock/process state. Historical co13 remains INCOMPLETE/UNCERTAIN, no NUnit execution.
- Test-only commit `45fa90221c1e637d423380db50928a5bb916553f` fixes the missing helper namespace and its DirectoryInfo.FullName argument. Runtime and Editor compile-only verification both exit0, `TestResults/h1-f4-packet14-compile-03/result.json`, SHA `e6c68772875088e05bbde2ffd75a488ebe16bccf84610ad48e74f6d5a0074776`. The earlier Editor CS1503 diagnostic remains retained.
- Co14 `3ee120b4808749f9b836c411865b0697` at `45fa90221c1e637d423380db50928a5bb916553f`: exact two negative boot probes **2/0/0 PlayMode**, all seven stages PASSED. Independent receipt `TestResults/h1-f4-20261027-co14/independent-completion01.json`, SHA `f8f61f386e8cde43a72b138a3043105d745a1011bcdafc1e4d1d3fa82bf2a8ae`. Archive/input/tool, process/Job, prior evidence, restored settings/TMP and released reservation independently verified.
- Distinct observed first guards: unbound native creation `time.recovery.invalid_state`; the old minimally authored Narrow Hall fixture fails after repair at `schema8.upgrade.invalid_schema7`. Do not attribute both solely to recovery binding.
- Candidate positive-fixture changes remain test-only: complete consumed recovery inputs before first load, same TimeService attached to the synthetic root, production workload profile, explicit synthetic legacy timestamp/absent Research state, coordinator-driven clock advancement, and configured native funding asserted instead of directly mutating the wallet. No passing positive boot claim yet. Runtime and Editor compilation exit0 in `TestResults/h1-f4-packet14-compile-05/result.json`, SHA `e7272f6e148ce8a19365080d75348b54cbbab13220b51356aca2aea20f2ba9bd`.
- Compile04 stopped before compiler invocation on an obsolete co13 protected-map comparison. Read-only reconciliation found only the authorized co14 save namespace added; latest co14 protected map matches exactly. No owner or historical evidence changed; no mismatch was waived.
- Next: qualify the remaining boot/schema targets and adjacent negatives, then consecutive owned-starter EditMode/PlayMode exports and complete affected/full gates. Original84 retains84/84 historical exact-name coverage; co12 raw2818/0/1 EditMode and4273/14/10 PlayMode remain historical, not final preserved qualification.
- Model recommendation GPT-6.1 Sol Medium, Standard fixture correction; GPT-6 Sol Medium fallback. No requested model substitution, production authority change or scope expansion. Unit/SIT qualification pending for candidate corrections; owner UAT NOT STARTED. Windows runtime/exit, matched performance/memory, remaining interruption/reopen, final accumulated review and UAT package remain outstanding.
- All co13/co14, compile02–05 and their scripts are owned by this H1-F4 continuation and retained under exact TestResults paths above. No cleanup or reclassification.

`TestResults/h1-f4-scene-compile-20261031-03.py`, `TestResults/h1-f4-scene-compile-20261031-03`, `TestResults/h1-f4-scene-compile-20261031-04.py` and `TestResults/h1-f4-scene-compile-20261031-04` are owned by this closeout push for compile-only proof and retained for review. Campaign-owned preparations, captures, original archives, isolated save namespaces and process evidence remain under the existing supervisor's ownership model. No cleanup of historical/uncertain artifacts or owner saves is permitted.
