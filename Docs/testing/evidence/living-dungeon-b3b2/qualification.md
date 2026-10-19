# B3B2 foreground C2 qualification

Status: Windows Editor/Mono and Windows Development standalone technical qualification completed; preservation audit passed. Independent PR review is required; the PR remains unmerged. Recommended GPT-6.1 Sol High; complex foreground/save/combat integration. No delegation or model escalation.

## Source and authority

Base `5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d` (merged #244), branch `codex/living-dungeon-b3b2-autonomous-foreground-c2`. GitHub main was rechecked on October 19 and remains at that base; no open PR conflicts were found. The owner approved implementation, an unmerged PR, initial 30–90-second pacing and the explicit interaction/unit values in the [contract](../../../planning/living-dungeon-phase-b3b2-implementation-contract.md). Active-run editing remains authorized under Decisions 57/63; the proposed occupied-floor restriction was rejected.

Full EditMode source: `174526b3b94f617e9e3704ff768ea4b76e9c14af`. Boundary reproduction source: `91c12722cca0db630810d547de0516adfa7b7429`. Corrected gameplay source: `894a6caf548708534e288e78cf20b19b986e7803`; development smoke assertions: `8415492041c78005cec5621642dad00c14adf97a`; campaign adapter correction: `4c5f571a636f076d1bd3c19baeb850b69ce1fa51`. Final runtime/build/native source: `a1ae2eafbf6e9c339dc4c6a80afce5cbe4ab19f7`; final Editor-only integration-test source: `9c8415414f8831342f529245a6ad56a36307edd2`. Their only committed difference is the additional Editor scene test: no packed runtime/configuration changed. Later documentation-only commits are not described as having been built. Source applicability is checked against committed Unity inputs (1,380 initially; 1,382 after the two new scene-regression files), with reviewed save-name, fixture, capture, native-product and positive identity-test substitutions and five preserved local assets. These substitutions do not change gameplay configuration. Documentation-only final commits require a separate applicability statement, not a claim that their commit was already built.

## Implemented behavior and ownership

`GameRoot` samples absolute monotonic integer milliseconds through the existing `TimeService`/`SimulationClock`; application focus and pause determine eligible intervals. Accepted/closed `livingDungeon` horizons are the only durable elapsed owner. UTC remains the separate existing offline passive-mana/save boundary. Previously accepted-time draining cannot move that boundary. A bounded unfinished horizon resumes before new observed time is accepted.

`living.foreground.c2.v1` admits one party on one active Floor 1. Existing configured attraction, Heat, arrival and traffic formulas determine eligibility and the configured band interval. Installed opportunities stay fixed; full/ineligible opportunities are consumed without a queue or RunId allocation. A private bounded lifetime probe qualifies a start before allocation. Unsupported starts retain the editable dungeon and do not publish combat damage.

At each boundary the order is passive/research tick, existing party events and terminal consequences, current vacant recovery, opportunity and newborn zero-time events. Staging uses existing detached validated sessions; only one final predecessor-to-candidate atomic write publishes a pump. Terminal settlement applies verified captured net HP damage to each still-owned stable identity's current condition, preserves moves/custody/recovery, never recreates a retired identity and protects newer knowledge. Retirement, history, Heat/cooling and actual existing recovery debit share the transaction. Generic B2 qualification cannot configure the production profile; B3A remains Editor-only/B1-only.

Configuration: `Assets/_Project/Resources/living_dungeon_foreground.json`; combat, recovery and economics retain their existing owners. New visible text is in the existing bootstrap English string table and referenced through localization keys. UI observes committed inspection and pooled geometry; Edit Mode, Replay and incompatible geometry hide the live marker without changing execution.

## Compatibility

| Checkpoint/executable | Existing qualified reader | New production admission/settlement | B3A synthetic settlement |
| --- | --- | --- | --- |
| Format 1 / `run.executable.living-dungeon-b1.v1` | Frozen encoding/fold and Profile A semantics retained | Not adopted | Existing qualified B1 scope only |
| Format 2 / `run.executable.combat-c0.v1` | Explicit combat-capable contexts only | Rejected | Rejected |
| Format 2 / `run.executable.combat-c2.v1` | Explicit combat-capable contexts only | Exact production profile, timing, snapshot and owned-prefix checks | Rejected |
| Unsupported/corrupt/contradictory input | Fail closed; no usable execution/publication | Fail closed | Fail closed |

Writable schema is **15**. No schema, frozen reader or migration source is changed; no additional save, party registry, writable event journal or allocator is introduced. Checkpoint replay does not publish canonical monster condition or any settlement effect. Historical detailed receipts remain transient after terminal history publication, as in existing manual C2; active checkpoints reconstruct them exactly. This does not make historical detailed Replay Run durable.

## Work and byte envelopes

Configured technical ceilings (not balance): 64 boundaries/pump, 4,096 events/equal-time boundary, at most 64 synchronous flush pumps; one active party, four retained records; 65,536 bytes/checkpoint; 2,048 replay events/party, 8,192 aggregate replay; 1,000,000 combat work; 262,144 base-save bytes and 357,996 owner bytes. Existing byte/parser/replay budgets were not enlarged. Over-budget input/admission fails closed.

Representative one-room production-path technical fixture (Goblin, spike trap and hidden cache): maximum **23,624 checkpoint bytes**, **13 committed events**, **3 events at one timestamp**, **2,051 combat work**, **11,000 logical units**. Attempt 15 measured 763 ms for admission plus replay, including fixture transactions. The fixture loads combat content through `CombatC0IntegrationTests.Config` and uses the existing `PhaseSixA4Tests.ForceDescent` test overrides for success/Heat/mana route factors and descent thresholds. It is not a measurement of unmodified production configuration, worst-case geometry, native frame time or allocation ceilings. Standalone smoke uses the actual packed production configuration; its persisted checkpoint sizes are reported below. The final real Editor scene, using loaded production configuration without ForceDescent, measured a maximum **23,070 checkpoint bytes**, **11 committed run events**, **18 combat receipts** and **1,935 combat work**. Its detached reconstruction has no publication and automatic terminal receipts/work match exactly. These representative small layouts do not establish worst-case admission latency or mobile FPS; finite configured ceilings and fail-closed lifetime admission remain authoritative.

Partition/reopen comparison requires identical durable bytes except `livingDungeon.Revision`, which counts authoritative acceptances/writes and intentionally varies with pump partitioning. Prefixes, ordinals, deadlines, identities, receipts, HP, outcomes, clocks, allocator and other durable bytes must match. Reopen comparisons distinguish durable fields from the existing intentionally transient completed-history evidence.

## Validation history

All raw artifacts are preserved under `TestResults/living-dungeon-b3b2-20261018/`. Later passing attempts do not erase these failures.

| Attempt | Source | Result and resolution |
| --- | --- | --- |
| 01 focused | `6701cec4` | Compilation failed: malformed new GUID, direct PlayMode assembly reference and definite-assignment guard. Corrected. No tests ran. |
| 02 focused | `22672b52` | 2/22 passed; 20 rejected the configuration because reflection order differed from authored JSON order. Replaced with explicit format field order. |
| 03 focused | `eb43a000` | 16/22 passed. Reproduced missing production profiles in loaded/native save-session contexts. Also found a too-large test horizon timeout, inappropriate transient/Revision equality assertions and stale absolute failure counters. Corrected contexts and test preconditions/contract assertions. |
| 04 focused | `aa7f5ad3` | **23/23 passed**, including native-session activation, load/reopen, exact receipts, active editing, rollback and recovery-required latch. |
| 05 focused | `8704073e` | 25/27 passed; both cross-floor cases omitted the source-floor ID required by the existing multi-floor edit API. Added that explicit test target. |
| 06 full sequence | `fb21d684` | Compilation failed in the new capacity test: required expected ordinal omitted. Corrected; no test suite/build success claimed. |
| 07 full sequence | `f78aade4` | 2,142/2,259 passed; 116 failed, one skipped. 107 failures shared automatic production-profile registration against independently injected historical byte budgets. Other failures exposed disabled-service clock credit, added-header layout pressure and two new test/rejection-path issues. No build/PlayMode pass claimed. |
| 08 focused | `8bc7a87` | 30/31 passed. Legacy-limit compatibility and cross-floor cases passed. Reproduced `InvalidDataException` escaping the bounded-start rejection filter; added that specific exception without swallowing general filesystem failures. |
| 09 focused | `cf42b5ca` | **34/34 passed**, including manual C2 finishing before an older reserved automatic RunId, no duplicate recovery, rejection without allocation and contradictory receipt/monster-set rejection. |
| 10 full sequence | `6c2bc101` | 2,258/2,263 passed; four failed, one skipped. Two floor-activation failures reproduced a redundant closed-time action-barrier publication invalidating a fresh preview. Disabled-component pause callbacks could change offline mana during an unrelated rejected edit. The construction header left a 30px text viewport below its measured 35px minimum. Corrected those paths without changing assertions. PlayMode/build did not run. |
| 11 focused | `174526b3` | **36/36 passed**, including a newly observed pre-pause horizon accepted after an older bounded horizon drains, disabled-component pause handling, re-enable reconciliation and a dirty but closed action barrier with zero publications. |
| 12 affected UI | `174526b3` | **5/5 passed**, including all four previously failing assertions: fresh floor activation, stale confirmation handling, rejected offline edit preserving canonical bytes and construction reachability across resolutions/localization sizes. |
| 13 full EditMode | `174526b3` | **2,264 passed / 0 failed / 1 skipped**, including 65 production build gates and all 36 B3B2 cases. The inverse non-Windows runtime test is skipped on Windows (`gd66.test.windows_only_inverse`). After completion, the between-stage Python runner was stopped with no Unity Editor/player active so the final boundary regressions could be added. PlayMode/build did not launch; the runner interruption is not a test failure or a completed full sequence. |
| 14 focused reproductions | `91c12722` | **36 passed / 2 failed** exactly at the new live-clock floor click and completed suspended-pause-boundary assertions. The original 36 cases remained passing. Failures and source are retained before correction. |
| 15 affected boundaries | `894a6caf548708534e288e78cf20b19b986e7803` | **147/147 passed**: all 38 B3B2 cases (including both reproduced failures), original floor/UI assertions, clocks, offline passive mana and Phase Six A4 lifecycle regression. Strict modal stale-input checks and offline exact-once/failure tests remain passing. |
| 16 full PlayMode | `84154920` | **3,673 passed / 1 failed / 10 skipped**. All discovered B3B2 cases passed. The isolation adapter changed the build utility's required product identity but missed the positive BuildReadiness fixture's literal `Dungeon Lord` input; that assertion consequently failed. This is a campaign-induced fixture mismatch, not a clean full-suite pass. The raw failure is retained. |
| 17 identity PlayMode | `4c5f571a` | **17/17 passed**, after aligning only the positive fixture's product input with the already isolated product. The assertions and production identity guard are unchanged; the qualified fixture is archived and was restored exactly. No gameplay source changed after attempt 16, so the other 3,673 passes remain applicable; the failed full-suite report is not relabeled as passing. |
| 18 Windows Development Build | `4c5f571a` | **Passed**, Unity 6000.3.2f1 / StandaloneWindows64 / Development, 181,819,250 bytes, zero errors and one warning (empty Unity Services URI prevented native-symbol upload). The log also retains debugger-port and cleanup diagnostics; no unsupported pre-existing attribution is made. |
| 19 affected EditMode | `a1ae2eafbf6e9c339dc4c6a80afce5cbe4ab19f7` | **150/150 passed**. Adds immediate resume of a still-open horizon through multiple bounded passes, repeated pause/resume without duplicate ticks/mana, isolated reopen-clock opt-in checks, and the cached foreground gate. 120 pre-boundary samples: 0.0507 ms / zero measured test-thread allocations; no canonical publication. This excludes boundary processing and is not mobile FPS qualification. |
| 20 affected PlayMode | `a1ae2eaf` | **9/9 passed**: B3B2, historical A/B1/B2/B3A, B3B1 compatibility and manual C2 integration wrappers. |
| 21 replacement Windows Development Build | `a1ae2eaf` | **Passed**, Unity 6000.3.2f1 / StandaloneWindows64 / Development, 181,819,957 bytes, zero errors / one symbol-upload warning. Unused `Windows-living-dungeon-b3b2-20261018-smoke-fix` output and native save identity. |
| 22 final Editor integration and boundaries | `9c8415414f8831342f529245a6ad56a36307edd2` | **151/151 passed**, including real Bootstrap/controller automatic admission, live marker, valid occupied-floor edit, unchanged frozen checkpoint, exact receipt/work terminal equivalence and changed second admission. Production-config measurements below; cached gate 120 samples / 0.0509 ms / zero thread allocations. |

Exact commands and UTC start/end, exit codes and full source SHAs are in `*-command.json`; corresponding XML/logs contain counts, skips, assertions and warnings. The installed Unity 6000.3.2f1 and existing root project are reused. No project copy is used.

## Preservation

Before the first launch, 1,378 Unity inputs, 8,658 protected owner save/build files and 6,176 existing historical evidence files were inventoried. Root inputs, disposable original fixtures and historical evidence were backed up; original gate documents were separately archived in `predecision-documents.zip`. New source files increased the later applicability inventory to 1,380. An unused native product identity, `Dungeon Lord B3B2 Smoke 20261018`, was installed **before** Editor startup. Capture/fixture/default-save names use the campaign namespace; native smoke has a separate unused save filename and existing outputs cannot be overwritten.

The adapter archives/restores generated settings and retains deliberate committed SaveService corrections when removing qualification substitutions. Its original root-input archive is never rewritten. Protected files and evidence were rechecked before every launch and again after qualification. No new preservation exception is authorized. The five absent #242 historical screenshots and previously accepted #244 shader-cache exception remain historical; no restoration is claimed.

Final preservation audit **passed: 8,658/8,658 protected owner saves/build files and 6,176/6,176 historical evidence files unchanged; zero new exceptions**. All product/default-save, fixture, positive identity-test, screenshot and generated-setting substitutions were restored. Only intentional contract/roadmap/report edits remain before their final commit. `preservation-after.json`, `historical-evidence-after.json`, `accepted-preservation-exceptions.json` and `substitutions-restored.json` retain the audit.

## Review scope and limitations

Attempt 14 reproduced a nonmodal floor action passing a pre-barrier preview as an exact pinned predecessor: its own positive monotonic flush invalidated the fresh click. The correction requests fresh post-barrier writer eligibility for a direct click; modal confirmations retain their strict captured-preview check. It also reproduced a completed suspended drain not setting the existing durable-pause guard after the initial bounded AppPause save remained pending. A successful closed suspended transaction now marks that guard because it persisted the pause fields and capped UTC; the initial save preserves that proof instead of overwriting it with its earlier pending result. This changes no offline formula and grants no adventurer time. Attempt 15 establishes both corrections and their affected regressions; the broader PlayMode results, corrected native loop and subsequent affected checks are recorded above and below.

Full-suite results, affected follow-ups, both Windows builds and corrected native multi-process smoke are recorded above and below. Final preservation/source restoration passed; independent review is the next gate. Preserved landscape and large-text portrait C2 recovery/activity captures were visually inspected; the localized header, warnings and controls remain readable. Standalone qualification explicitly distinguishes a real monotonic-source check from injected clock/focus acceleration; it cannot be described as certification of every OS/platform foreground callback or secure foreground provenance.

The lifecycle review reproduced two boundary defects before correction: a dirty closed action barrier rewrote the session despite no earlier elapsed work, and a newly accepted pre-pause sample could use a later suspended pump's UTC. Attempt 11 verifies that closed barriers retain the predecessor without publication and that the deferred sample retains the pause UTC after bounded older-horizon draining. Re-enabling a paused component also reconciles pause state. These changes derive no adventurer time from UTC and add no elapsed owner. Disabled roots remember pause/focus flags without publishing gameplay or offline mana.

No owner manual checkpoint testing is requested. Independent review must examine causal tick/event ordering, immutable profile support, current-state HP/revival reconciliation, failure suspension and exact persistence. The player-visible target is one foreground C2 run, not descent, offline adventurer progression, broader automatic capacity, authenticated foreground/UTC provenance, sub-dungeons or Mana Farm. The PR must remain unmerged.

## Exact changed files

Relative to verified base `5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d`; generated qualification substitutions are excluded.

| File | Purpose |
| --- | --- |
| `Assets/_Project/Data/Bootstrap/string_table_en.json` | Localized activity, failure and recorded-action text. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonB3B2EditModeFixtures.cs` | Discover focused B3B2 tests through the established Editor fixture. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonB3B2EditModeFixtures.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs` | Qualify automatic scene admission/edit/settlement/second-run behavior, live-clock floor actions, bounded resume and cached-frame allocations. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Editor/DungeonVisualBuildQualification.cs` | Create a new unused Windows qualification build output. |
| `Assets/_Project/Resources/living_dungeon_foreground.json` | Own approved pacing/timing and separate technical envelopes as versioned content. |
| `Assets/_Project/Resources/living_dungeon_foreground.json.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Core/GameRoot.Foreground.cs` | Sample/pump/flush eligible foreground time and expose committed inspection; reconcile lifecycle suspension. |
| `Assets/_Project/Scripts/Core/GameRoot.Foreground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Core/GameRoot.ForegroundSmoke.cs` | Opt-in development-only native admission/edit/reopen/settlement and changed-second-run checks. |
| `Assets/_Project/Scripts/Core/GameRoot.ForegroundSmoke.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Core/GameRoot.cs` | Integrate foreground services with boot, Update, pause/resume, canonical actions and explicit deletion quiescence. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs` | Prepare a trusted foreground replacement through the existing complete-save owner. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs` | Keep production terminal records occupied until atomic retirement; retain historical vacancy semantics. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DurableSettlementHistory.cs` | Authorize exact C2 terminal history from the pinned predecessor and retired owner. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonContracts.cs` | Require C2 executable, one Floor 1, posture and exact timing for the distinct production profile. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs` | Order ticks, checkpoint advancement, terminal reconciliation, recovery, opportunities and retirement in one detached pump. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundConfig.cs` | Strict bounded versioned configuration and explicit production-profile construction. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundConfig.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundReconciliation.cs` | Verify immutable receipts/net damage and apply it to current stable identities without recreating retired monsters. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundReconciliation.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonOutcomeConsequences.cs` | Reuse the existing pure Heat/cooling, applicable knowledge, history and individually verified objective composition. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonOutcomeConsequences.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonScheduling.cs` | Prevent the generic qualification pathway from configuring or mutating production C2 scheduling. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonTerminalSettlement.cs` | Retain Editor-only B1 eligibility while calling the extracted consequence composition. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/RunExecution.cs` | Add a pure exact timing comparison; checkpoint formats and combat kernel stay unchanged. |
| `Assets/_Project/Scripts/Services/SaveService.Foreground.cs` | Use the existing canonical session/writer, latch recovery-required failure and provide bounded read-only inspection. |
| `Assets/_Project/Scripts/Services/SaveService.Foreground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Services/SaveService.cs` | Register supported production contexts in load/create and flush earlier elapsed work before canonical actions. |
| `Assets/_Project/Scripts/Services/SimulationClock.cs` | Inject/use an absolute monotonic source independently of frame delta. |
| `Assets/_Project/Scripts/Services/TimeService.Foreground.cs` | Convert absolute samples to whole logical units and gate focus/pause without offline adventurer credit. |
| `Assets/_Project/Scripts/Services/TimeService.Foreground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/Services/TimeService.cs` | Add monotonic injection and avoid duplicate legacy ticks while the foreground clock owns progression. |
| `Assets/_Project/Scripts/UI/DungeonFloorWorldForeground.cs` | Reuse pooled presentation for a committed party marker with geometry/edit/replay visibility guards. |
| `Assets/_Project/Scripts/UI/DungeonFloorWorldForeground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Scripts/UI/ProductionDungeonController.cs` | Invoke the read-only foreground presenter. |
| `Assets/_Project/Scripts/UI/ProductionDungeonFloors.cs` | Revalidate fresh direct clicks after their barrier while retaining exact modal confirmation binding. |
| `Assets/_Project/Scripts/UI/ProductionDungeonForeground.cs` | Localize committed activity/latest results; rendering never resolves combat. |
| `Assets/_Project/Scripts/UI/ProductionDungeonForeground.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Tests/EditMode/LivingDungeonB3B2Tests.cs` | Exercise deterministic admission, immutable inputs, current-state reconciliation, reopen, limits and atomic failures. |
| `Assets/_Project/Tests/EditMode/LivingDungeonB3B2Tests.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/Tests/PlayMode/LivingDungeonB3B2PlayModeTests.cs` | Run the established production-writer/clock cases through PlayMode. |
| `Assets/_Project/Tests/PlayMode/LivingDungeonB3B2PlayModeTests.cs.meta` | Stable Unity importer identity for the paired new source/config asset. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uss` | Fit activity in the existing scrollable header and preserve construction text reachability. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uxml` | Add the localized activity label to the existing header. |
| `Docs/planning/living-dungeon-phase-b3b2-implementation-contract.md` | Record approved timing, exact reconciliation/order, lifecycle rules and qualified scope. |
| `Docs/testing/evidence/living-dungeon-b3b2/preimplementation.md` | Preserve inspection, authority gates and the superseded predecision findings. |
| `Docs/testing/evidence/living-dungeon-b3b2/qualification.md` | Record exact source applicability, failures/corrections, tests/build, limits, preservation and review risks. |
| `Tools/Presentation/qualify_b3b2.py` | Reuse the existing preservation/Unity adapter with isolated product, fixtures, outputs and native phases. |
| `docs/planning/post-gd60-mvp-execution-plan.md` | Update the active roadmap with this packet and explicit remaining dependencies. |

## Native qualification correction retained for review

The original source `4c5f571a` native admission passed: automatic run-1, visible marker, active same-floor edit committed, unchanged frozen checkpoint, 1,200 measured real monotonic units, accepted/closed 91,629 and ordinal 2. Save size 39,766 bytes; decoded format-2 checkpoint 23,060 bytes (SHA-256 equals its pinned checkpoint identity). `native-admit*.json/log` and the exact save copy are preserved.

The original native settle phase failed `b3b2.smoke.exact_pending_reopen`. Its retained save proves identical start identity, checkpoint bytes and ordinal 2, with accepted/closed 93,445: 1,816 legitimate startup foreground units accrued before the coroutine asserted equality. This is a qualification-clock race, not evidence of a changed combat prefix or offline adventurer credit. The corrected source freezes only explicit isolated development settle/finish boot probes until the exact reopen assertion, then installs the existing advancing test clock. Normal startup, admission real-source checks and Release builds retain their original clocks. Assertions are not weakened. The old failed report, save and build are retained; the replacement build/save/report paths use `-smoke-fix`.

## Corrected native three-process result

All three `native-smoke-fix-*` phases passed at source `a1ae2eafbf6e9c339dc4c6a80afce5cbe4ab19f7`:

| Phase | Durable result |
| --- | --- |
| admit | Real monotonic source 1,201 units, automatic run-1, marker visible, active-floor draft committed, original checkpoint unchanged. Accepted/closed 91,813; ordinal 2; checkpoint 23,060 bytes; complete save 39,766 bytes. |
| settle | Exact reopen assertion passed before new test time. Run-1 settled once; automatically admitted run-2 captured the current edited geometry, different from run-1. Accepted/closed 135,813; ordinal 0; checkpoint 23,065 bytes; complete save 49,724 bytes. |
| finish | Exact second reopen passed, live marker visible, run-2 settled. Accepted/closed 146,813; zero retained parties, history exactly run-1/run-2 once each, next sequence 3. Complete save 25,933 bytes. |

The moved monster keeps stable AssignmentId `compat.floor.00.legacy-room.00.content.monster.0000`, level 1, current HP 6 and revival false after final vacancy recovery. Final mana is 966.38 and Heat 0 under unchanged configured formulas (including passive ticks, injury recovery and loot cooling). These are observed fixture results, not new tuning or guaranteed outcomes for other dungeons. Detailed native logs retain GPU buffer cleanup diagnostics; no new preservation exception is inferred.

Exact saves, SHA-256 measurements, commands and source SHAs are retained as `native-smoke-fix-*-save.json`, `*-sizes.json`, `*-command.json`, JSON reports and logs. Build output is `Builds/Development/Windows-living-dungeon-b3b2-20261018-smoke-fix/Dungeon Lord.exe`. Launcher SHA-256 is `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`; the actual gameplay assembly `Dungeon Lord_Data/Managed/Assembly-CSharp.dll` is `1d34a253ef0502c3feaf1a26699d626115a47df90368dae04fbda181c28e9df4`. The launcher alone is not a gameplay identity; assembly/global managers hashes are in `native-smoke-fix-build-hashes.json`.

Only the first admission includes a real elapsed-source check. Subsequent progression uses the explicit opt-in development clock/focus injection to accelerate approved production durations. Actual Bootstrap, UI, canonical filesystem/session, captured combat, settlement and normal close/reopen remain in use. This does not certify all physical OS focus callbacks, mobile/IL2CPP performance, or authenticated foreground provenance.

## Platform skips and source applicability

The full EditMode inverse non-Windows test skips on Windows. Full PlayMode skips eight explicitly synchronous EditMode-only boot fixtures, that Windows-inverse test, and the standalone-player-only filesystem fixture. Those skips are explicit reasons in the raw XML; they are not passing cases. Existing native filesystem/atomic paths are exercised by isolated real-save integration and native smoke, with synthetic write/rollback/readback failures covered separately.

The full suites remain at their recorded SHAs. Later relevant behavior changes were covered by 147 affected EditMode cases, the complete PlayMode campaign, 17 identity cases, then 150/151 affected EditMode and nine integration PlayMode cases. The final development reopen change affects only explicit isolated settle/finish command-line probes; normal clocks are unchanged. No expensive full-suite repetition is claimed after the final test-only commit. Production runtime/config files are identical between build source `a1ae2eaf` and Editor test source `9c841541`; final documentation does not affect that applicability.

Independent review should focus on equal-time passive/combat/settlement/recovery/admission ordering; incomplete accepted horizons and separate offline passive-mana boundaries; stable-identity damage/revival reconciliation after current edits; trusted production-profile admission; and atomic failure/recovery-required suspension. No blocking test failure remains in the affected follow-ups. Earlier failed reports remain failed and preserved.

## Gameplay-only owner checks after independent review

No owner checkpoint, command-line, technical failure-injection or build qualification is required. For gameplay feedback in a test game:

1. Keep only Floor 1 active; build a connected valid room with a Goblin and Hidden Cache, using normal affordable controls. Keep the game focused and running; an eligible opportunity occurs in the configured 30–90 seconds. Do not press a manual run control.
2. Observe the localized live activity and party marker. During that run, enter Edit Mode, move the monster to another valid room tile and Save Changes. The activity continues against the old snapshot; incompatible old geometry hides its marker.
3. Watch the outcome/history, monster injury/readiness and normal mana/recovery effects. A later party uses the edited layout. Review whether the provisional pacing feels useful.
4. Pause or close/reopen during an active party. It resumes from the committed prefix; time outside the app does not advance adventurers. Existing offline passive mana remains its separate policy.

The isolated qualified Windows player can be launched without smoke arguments for these ordinary gameplay checks; its product/save namespace is separate from owner production data. Broader automatic floors/descent, offline adventurers, authenticated UTC/foreground provenance, mobile/IL2CPP performance and durable detailed historical replay remain outside this slice. These are scope limitations, not claims that the whole Living Dungeon roadmap is activated.


## PR #245 independent-review corrections (2026-10-19)

This addendum concerns the two inline findings at reviewed HEAD `53e85f0baa9e141c564c4363ab8890d075003ffa`. The original campaign and source-applicability statements above remain historical evidence; none of its failed reports is relabeled. Recommended configuration for this bounded correction: GPT-6.1 Sol Medium, Standard; fallback GPT-6 Sol Medium then GPT-5.6 Sol Medium. No model escalation or delegation. Validation status for this addendum: **technical qualification complete; ready for another independent review**. The PR remains unmerged; no owner gameplay testing is requested.

### Corrected behavior

`GameRoot.PumpForeground` previously passed every unsuccessful write result to `ForegroundFault`, permanently latching `foregroundFailure` even when `ExactCompleteSaveAtomicPersistence` proved that the exact original bytes had been restored. The root now recognizes only `gd66.write.atomic_save_failed` as retryable. It retains the committed save/session/checkpoint/allocator/horizon and displays `ui.living.retryable`; frame updates perform no further storage attempts while pending. An existing save/edit/lifecycle barrier may attempt the transaction again. A successful pump clears the transient retry gate. Repeated failures return after one failed attempt per explicit barrier; no retry timer or new UI control is introduced. Uncertain durability (`gd66.transaction.recovery_failed`) and invalid-state failures keep the permanent root latch; the independent SaveService recovery-required latch is unchanged. Atomic persistence and its failure classification were not modified.

Production `LivingDungeonForegroundConfig` now requires `MaximumCombatWork > 0` before constructing its production profile. Zero and negative work produce `living.foreground.invalid_configuration`; malformed, fractional, quoted, null and overflowing inputs fail bounded parsing. Positive test envelopes are accepted. The authored value remains 1,000,000. Historical B1/Profile A zero-work semantics, checkpoint codecs, gameplay timings, settlement reconciliation, schema 15 and migrations are unchanged.

### Correction evidence

Separate campaign: `TestResults/living-dungeon-b3b2-20261019-review/`; isolated native product `Dungeon Lord B3B2 Smoke 20261019 Review`, with distinct Editor and native save names. Existing original campaign/build/save outputs are retained. Preparation inventories 1,382 Unity inputs, 9,264 protected save/build files and 7,753 historical evidence files. The existing adapter redirects every known capture/fixture output before launching Unity. Raw historical evidence is backed up; existing archive containers remain in place and are included in the immutable hash inventory rather than recursively copied into another archive.

| Attempt | Exact source | Result |
| --- | --- | --- |
| 01 before-fix focused | `0bf8580272a6b053d14e8428ac33310d73d327f0` | Test compilation failed: the deterministic filesystem Operations enumerable requires LINQ Count(). No test success claimed; corrected only the test expressions. |
| 02 before-fix focused | `88bfe1dd128573f8a718b05731a5a1cb693d857e` | 49 passed / 3 failed / 0 skipped. Reproduced refused explicit retry after proven rollback, accepted zero work, and negative work's generic profile diagnostic. The uncertain-rollback control and original tests passed. |
| 03 corrected affected EditMode | `9227f78e2e9502985a87dff811fd9440c2ad7e41` | **161/161 passed / 0 skips**. All same 52 before-fix cases now pass (recorded in before-after-equivalence.json), including the real-root accepted-horizon retry, no per-frame I/O retries, exactly-once terminal receipts/resources, ordinary save after retry and permanent uncertain-rollback latch. |
| 04 atomic/manual/legacy EditMode | `9227f78e` | **340/340 passed / 0 skips**. Existing A/B1/B2/B3A, manual C0/C2, canonical-save, migration, history/Heat/loot regression filter. |
| 05 affected PlayMode | `9227f78e` | **10/10 passed / 0 skips**. B3B2, B3B1, A/B1/B2/B3A and C2 integration; includes cross-mode production-work validation. |
| 06 Windows Development Build | `b582a0bdccefbba4fe8872f0f3d8fa987bfa7b44` | **Passed**; Unity 6000.3.2f1, StandaloneWindows64, Development, 181,820,680 bytes, 0 errors / 1 warning. Separate unused review output; identical tested runtime. |
| Native admit / settle / finish | `b582a0bd` | **All three passed**. Real monotonic-source check 1,200 units; automatic admission/live marker/active edit; exact reopen/run-1 settlement/changed run-2 snapshot; second exact reopen/run-2 settlement. Final accepted/closed 146,829, zero retained parties, history run-1/run-2 exactly once, next sequence 3. |

Corrected runtime source: `9227f78e2e9502985a87dff811fd9440c2ad7e41`. Build/tooling source: `b582a0bdccefbba4fe8872f0f3d8fa987bfa7b44`. Its only difference from tested runtime `9227f78e` is an adapter guard rejecting the older smoke-fix variant before loading or launching anything; all Unity inputs are identical. The guard rejection is recorded in output-guard-check.json. Build identity, standalone and preservation closeout are recorded below. Both threads may be addressed from this completed evidence.

### Exact correction files relative to reviewed HEAD

| File | Purpose |
| --- | --- |
| `Assets/_Project/Scripts/Core/GameRoot.Foreground.cs` | Gate frame retries after proven rollback; allow existing explicit barriers to retry; preserve permanent faults. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundConfig.cs` | Reject nonpositive production combat-work envelopes. |
| `Assets/_Project/Data/Bootstrap/string_table_en.json` | Localize retryable save feedback. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs` | Real-root accepted-horizon/rollback/retry, exact-once settlement/resource and ordinary-save checks; uncertain-durability control. |
| `Assets/_Project/Tests/EditMode/LivingDungeonB3B2Tests.cs` | Positive/nonpositive/malformed/overflow production-work cases. |
| `Assets/_Project/Tests/PlayMode/LivingDungeonB3B2PlayModeTests.cs` | Confirm production-work validation across execution modes. |
| `Assets/_Project/Editor/DungeonVisualBuildQualification.cs` | New unused correction Windows build output. |
| `Tools/Presentation/qualify_b3b2_review.py` | Thin reuse of the established isolated qualification adapter with fresh identities, outputs and nonrecursive historical backup. |
| `Docs/testing/evidence/living-dungeon-b3b2/qualification.md` | Retain original evidence and record the correction campaign/source applicability. |

No owner gameplay testing is requested for this correction. Independent review remains required; do not merge.


Correction build identity: `Builds/Development/Windows-living-dungeon-b3b2-20261019-review/Dungeon Lord.exe`; gameplay assembly SHA-256 `fdc8afc8dda7cda8e3fa3650f05d51f2daa0cf6d73e599070c9e995fb3add4f1`. Launcher and packed managers hashes are retained in `build-hashes.json`. The single build warning is the Unity Services empty-URI native-symbol-upload warning, not a compilation or gameplay failure. Cleanup diagnostics remain in the native logs without unsupported attribution.

Native checkpoint sizes are 23,060 bytes (run-1 ordinal 2) and 23,063 bytes (run-2 ordinal 0); complete saves are 39,766 / 49,718 / 25,943 bytes. Exact saves, SHA-256 checks, JSON reports, logs and commands are retained separately in the correction campaign. Standalone smoke uses the existing explicit development-only clock/focus acceleration after its real monotonic check; normal gameplay clocks are unchanged. Deterministic failure injection is exercised through the real root with the established in-memory filesystem, not by disrupting owner storage. No claim of native hardware-failure injection, mobile performance or authenticated foreground provenance is made.

Source applicability: all correction EditMode/PlayMode results use `9227f78e`; build and three native processes use `b582a0bd`, with no intervening Unity input changes. Later documentation-only closeout does not change runtime applicability. Earlier full-suite results above are preserved at their original source SHAs; no new full-suite run or relabeling of older failed reports is claimed.

The original B3B2 isolated product has a supplemental 40-file before/after inventory (`prior-isolated-product-before/after.json`), all unchanged. Both prior canonical native saves also match their published original-campaign snapshots exactly. Final main preservation audit **passed: 9,264/9,264 protected save/build files and 7,753/7,753 historical evidence files unchanged; zero new exceptions**. Product identity, default-save, fixture/capture and generated Unity-setting substitutions were restored. Only this documentation closeout remained before its final commit; all runtime and test inputs match the qualified source. `preservation-after.json`, `historical-evidence-after.json`, `accepted-preservation-exceptions.json` and `substitutions-restored.json` retain the result. No external test/player process remains running; ordinary Unity Hub/licensing services are not terminated. No new technical blocker was found.
