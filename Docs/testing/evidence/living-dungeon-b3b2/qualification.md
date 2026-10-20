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

## Live-readability and sustained-performance correction (2026-10-20)

Owner testing at `78a94fe3fff6cfb5b8e83952c6a2905c50dba3c8` reported an unexplained jumping party marker and Unity slowdown during autonomous runs. Earlier cached-idle timings do **not** qualify event-processing frames or sustained performance. The earlier ready-for-review statements apply to their dated corrections; the final corrected-source results below supersede the initial pending status for this correction. Configuration recommendation: GPT-6.1 Sol High, Complex; the owner-specified fallbacks remain unchanged, with no delegation or model escalation.

Read-only findings: the marker snaps to `InspectForeground`'s last committed position; the foreground terminal path does not retain the `PresentationSteps` required by C1 replay. Each pump repeatedly stages a complete save, opens it, validates it again and projects it, reconstructing checkpoint prefixes in validation and explicit scheduling/occupancy lookups. These are profiling candidates, not yet measured bottleneck conclusions.

Baseline campaign: `TestResults/living-dungeon-b3b2-20261019-live-before/`, isolated product `Dungeon Lord B3B2 Smoke 20261019 Live Before`. Preparation preserved 1,386 Unity inputs, inventoried 9,567 protected save/build files and 7,871 historical evidence files, retained raw historical evidence and redirected fixture/capture outputs. Existing archives are hashed in place. Do not reopen the closed original or review campaigns. The installed CLI is 1.0.0-beta.12; its check reports optional beta.13 with explicit approval required. No upgrade was performed.

The development-only profile uses actual Bootstrap, production configuration, foreground scheduler, native canonical writer and controller. An explicit injected clock advances .25 foreground seconds per observed sample to reach 68 settlements; it does not change arrival intervals, interaction durations, work limits or combat. It separates empty idle, eligible idle, between-event, admission, event, passive and settlement frames. Scope timings and allocation counts are inclusive (nested scopes must not be added); the raw sample list itself contributes qualification-only managed memory. Main Thread recorder availability must be verified in results. At this baseline point no production optimization had been made.

| Baseline attempt | Source | Result |
| --- | --- | --- |
| 01 Editor profile | `a461f34597d02b49a1e016c7739723422d6725fd` | Failed. Report directory missing masked a recovery fault. During the run Codex read the disposable live save, which can deny atomic replacement on Windows; this was an avoidable measurement interference. The failed XML/log and isolated save are retained. Owner saves were not used. Future measurements must never inspect a live save file. |
| 02 Editor profile | `83836ad7f470fa5f6c79d4d5fb3df02d92f8212d` | Failed: UnityTest's default 180,000 ms timeout expired before 68 runs. No complete performance baseline is claimed. |
| 03 Editor profile | `8f45bd25c2e8cbbb1719eae007c022b110e9bd80` | **Passed, 1/1**, 68 settlements. Only this profiling test's timeout was raised to 1,800,000 ms; gameplay budgets are unchanged. Raw 10 MB frame report retained in `outputs-editorbaseline03/`; `editor-supported-summary.json` treats unsupported allocation readings as unavailable. |

Measured Editor baseline (UpdateForeground wall time on the main thread): 544 event frames mean **796.314 ms**, p95 **957.012 ms**, max **1,108.944 ms**; 68 admissions mean 886.116 ms; 68 settlements mean 795.623 ms; 75 passive frames mean 685.896 ms. Idle update means are below .001 ms. Main Thread recorder corroborates the event stalls. Stage consumes 327,978.676 ms of event update's approximately 433,195 ms (75.7%); its Prepare/Open/Validate scopes are the dominant nested work. Across the measured run there are 4,120 stages, 19,040 restores and 755 atomic-persistence calls. Lifetime probing totals only 418.054 ms across 68 admissions; publication totals about 2,778 ms and map reconstruction count is zero. UI rebuild is not the dominant stall. These results establish a feature-path problem in the Editor; standalone comparison is still pending.

The current-thread allocation API returned zero throughout this Mono Editor run despite **2,143 GC collections**, so those raw zero values are **unsupported/unavailable, not evidence of zero allocations**. Managed used memory ranges 723,136,512–955,658,240 bytes and ends below its initial sample; reported native allocated memory grows approximately .82 MB. Both include Editor/qualification overhead; the raw sample list is retained intentionally. A supported profiler frame-allocation counter and explicit availability calibration were added for the standalone baseline and final runs, without changing gameplay. A late external process sample found only Unity Hub because the test had already exited; it is not presented as an Editor CPU sample.

The corrected-harness standalone baseline also completed **68 runs**, source `cf1010704ca5f69a2edab87b466f98449325ce14`, build `Windows-living-dungeon-b3b2-20261019-live-before-profile-fix/` (attempt 06 passed). Actual event frames average **910.609 ms**, p95 **1,086.018 ms**, max **1,155.701 ms**; admissions mean 1,041.315 ms and settlements 929.588 ms. The supported profiler frame counter reports event-frame allocations averaging **395,136,127 bytes**. Idle update means remain below .001 ms. Managed used memory is 7.16–21.99 MB; native allocated memory grows approximately .82 MB, including the retained sample list. GC collection count rises by 145,599. This confirms a feature-path bottleneck, not an Editor-only slowdown. Raw report, complete terminal save snapshot, command, log and `native-summary.json` are retained in the BEFORE campaign.

Native attempt 04's initial profile failed before gameplay because the harness indexed an absent first floor; its report/log/save are retained. The harness now constructs the missing floor and uses a fresh report-derived save name. Attempt 05 was safely refused before Unity launch because the nested adapter's existing-output gate still pointed to the first baseline build; no output was overwritten. The adapter now asserts the separate retry target before launch. Both baseline builds and every failed/provisional report remain preserved.

The BEFORE campaign is now closed: **9,567/9,567 protected save/build files and 7,871/7,871 historical evidence files unchanged, zero new exceptions**. Generated Unity inputs and product/save/fixture/capture substitutions were restored; committed profiling-harness corrections were retained. Only ordinary Unity Hub service remained at the next process check. No new owner-save exception occurred; the live-read interference affected only the disposable profiling fixture.

Correction source `966fb9f1` / orientation-independent test refinement `0b37cfe99c983f24851e506d71af2591b99b905e` is committed but **not yet qualified**. A fresh AFTER campaign is being prepared. Next: compile/run focused corrections, affected regressions and measured Editor/native qualification from final runtime; inspect portrait/Large captures, close preservation audits, and update this record/roadmap/PR #245. Leave unmerged.

### Corrected-source campaign (2026-10-20)

AFTER preparation inventories **1,388 Unity inputs, 10,173 protected files and 7,936 historical evidence files** under `TestResults/living-dungeon-b3b2-20261019-live-after/`; isolated product `Dungeon Lord B3B2 Smoke 20261019 Live After`. The BEFORE campaign is not reopened. Source correction updates preserve schema 15, both checkpoint formats, production timing/work limits, settlement reconciliation and atomic failure classifications.

| Attempt | Source | Actual result |
| --- | --- | --- |
| 01 focused | `0b37cfe9` | Compilation failed: wrong fingerprint class name, with two cascading definite-assignment errors. No tests ran. Corrected to existing `StructuralEditService.TryFingerprint`. |
| 02 focused | `e2fd7219a15ae8b38d2be9daac552e8a1609dcd4` | **57 passed / 1 failed / 0 skipped**. Only the new layout case failed: resizing delivered focus callbacks while the test fixture's root was disabled. |
| 03 two-case probe | `41885b4a6cee75ba51fe7da137fba69d865c5910` | **0 passed / 2 failed**. Reproduced lost injected terminal save boundary (1,000 rather than 2,000) in the new optimization; also captured the hidden-marker layout case. |
| 04 focused | `588a88a9464cdcb6bf825bd3781a31d8ed4b3ec7` | **58 passed / 1 failed / 0 skipped**. Terminal metadata now passes; controlled test focus restored. Label passes 720×1280 and 1280×720 but is hidden by the floor rail at 360×640 Large, where original chrome leaves a 64-pixel map. |
| 05 two-case probe | `964eed06110d575f22b209c87c87b823296fe717` | **2/2 passed**. Label placement avoids rail, activity remains visible below normal-play scrolling HUD, compact Large metrics use two columns. Image inspection still found insufficient narrow map height; adjusted compact-only map reservation and redundant heading. |
| 06 focused | `cc215955d44347d6a13586dcfabe9d106ed0b158` | **59/59 passed / 0 skipped**. Checkpoint bytes/work/receipts/outcome observer equality, session evidence versus historical saves, active-idle work gating, adjacent interpolation, snap/hide cases, real-root labels/replay, three Large layouts, existing rollback/lifecycle/edit/settlement/configuration tests. Final layout captures inspected. |
| 08 affected boundary/edit | `cc215955d44347d6a13586dcfabe9d106ed0b158` | **168/168 passed / 0 skipped**. Existing graphical/editor, foreground, time/lifecycle, offline-passive and PhaseSixA4 cases. |
| 07 sustained Editor profile | `cc215955d44347d6a13586dcfabe9d106ed0b158` | **1/1 passed**, 68 completed runs, 544 event frames. Corrected measurements below. |

The terminal metadata defect was introduced by removing initial redundant staging: authorized history composition intentionally copies unrelated predecessor metadata, including the older save timestamp. Closure now reasserts the **already injected** `savedUtcUnix` before its ordinary Stage; no new UTC sampling, extra grant, new stage or weakened history authorization. The derived-period unit regression proves current boundary retention through reopen without a coincident passive tick masking the issue. All provisional failures remain retained.

Presentation changes retain all four metric cards and existing controls; only normal-play narrow Large arrangement changes. Editing keeps the existing chrome arrangement. Current authoritative party counts and pending announcements are distinct from committed recent interaction receipts. Historical completed saves still omit transient replay; survival/route/extracted-loot summaries remain visible. Independent review and the remaining regression/performance/build/native/preservation gates are still required.

Corrected Editor measurements (`editor-summary.json`, raw report `outputs-profile07/foreground-profile-637dd91a464f42a38cb11edaa0424112.json`): event update mean **219.204 ms**, p95 **304.523 ms**, max **455.567 ms**; admissions mean 331.091 ms, settlements 326.552 ms and passive frames 208.445 ms. Mean event time falls 72.5% against the measured baseline; p95 falls 68.2%. Stage calls fall from 4,120 to 1,168 and restores from 19,040 to 7,072; atomic calls remain **755**, preserving event-boundary durability. Between-event frames record zero Stage/Restore/AtomicWrite/Publication/Present/WorldRender calls. GC collections fall from 2,143 to 535. Supported profiler event-frame allocation mean is 88,598,269 bytes; the baseline Editor lacked that counter, so a direct Editor allocation percentage is not claimed. Current-thread allocation API remains unavailable. Managed used memory ranges 714,440,704–940,331,008 bytes; native allocation grows 845,264 bytes, including retained samples. Remaining 200–500 ms durable-boundary stalls are real and must not be described as smooth-frame performance; native comparison and regression gates remain pending.

### Exact live-correction file manifest (relative to reviewed `78a94fe3`)

| Path | Purpose |
| --- | --- |
| `Assets/_Project/Data/Bootstrap/string_table_en.json` | Localization-owned adventurer/pending/interaction/outcome text; no gameplay tuning. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundReadabilityTests.cs` | Recorded movement, discontinuity/hide, real-root activity/replay, Large layout and idle-work regressions. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundReadabilityTests.cs.meta` | Unity asset identity for the adjacent new source file. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs` | Opt-in 68-run production profile; isolated report and failure capture. |
| `Assets/_Project/Editor/DungeonVisualBuildQualification.cs` | Unused before/after/final Windows build destinations, preserving older builds. |
| `Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs` | Disabled-by-default development scope counts/timings; never gameplay input. |
| `Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs.meta` | Unity asset identity for the adjacent new source file. |
| `Assets/_Project/Scripts/Core/GameRoot.Foreground.cs` | Read-only existing clock/time/focus presentation gate. |
| `Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs` | Isolated sustained production instrumentation; category/GC/memory/frame samples. |
| `Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs.meta` | Unity asset identity for the adjacent new source file. |
| `Assets/_Project/Scripts/Core/GameRoot.ForegroundSmoke.cs` | Strict isolated profile entry and session-byte terminal snapshot, no live-file reads. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs` | Return the fresh foreground validation and session from the existing exact-byte round trip. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs` | Reuse only matching fresh stage validation; always freshly validate durable readback. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs` | Timing scope only; atomic algorithm and classifications unchanged. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonContracts.cs` | Optional transient observation through supported restoration; timing scope. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs` | Remove redundant same-pump stages; preserve closure UTC, stage HP/revival changes before admission and retain compatible session replay. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs` | Optional committed-step observer; never checkpoint bytes/digest/rules or pending consequences. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/RunExecution.cs` | Optional committed-step observer; never checkpoint bytes/digest/rules or pending consequences. |
| `Assets/_Project/Scripts/Services/ProductionDungeonPresenter.cs` | Localized committed encounter/trap/loot/casualty and saved outcome summaries. |
| `Assets/_Project/Scripts/Services/SaveService.Foreground.cs` | Single inspection and read-only verified-prefix presentation projection. |
| `Assets/_Project/Scripts/Services/TimeService.Foreground.cs` | Read-only existing clock/time/focus presentation gate. |
| `Assets/_Project/Scripts/UI/DungeonFloorWorldForeground.cs` | Interpolate only recorded adjacent edges; snap/hide discontinuities and incompatible geometry. |
| `Assets/_Project/Scripts/UI/DungeonFloorWorldView.cs` | Disabled development map-reconstruction scope. |
| `Assets/_Project/Scripts/UI/ProductionDungeonController.cs` | Existing narrow-layout policy class plus publication/presentation timing scopes. |
| `Assets/_Project/Scripts/UI/ProductionDungeonForeground.cs` | Cached localized adventurer label and recent activity; rail-safe placement and normal-play activity position. |
| `Assets/_Project/Tests/EditMode/LivingDungeonB3B2Tests.cs` | Observer byte/work/receipt/outcome equality, session replay, terminal UTC, committed feedback and condition-only recovery/admission regressions. |
| `Assets/_Project/Tests/PlayMode/LivingDungeonB3B2PlayModeTests.cs` | Existing reflection integration bridge also exercises observer/replay/UTC regressions. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uss` | Existing UITK activity/marker styles; compact Large map and metric arrangement. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uxml` | Existing UITK activity/marker styles; compact Large map and metric arrangement. |
| `Docs/planning/living-dungeon-phase-b3b2-implementation-contract.md` | Exact validation, observation/interpolation and replay limitations. |
| `Tools/Presentation/qualify_b3b2_live.py` | Isolated campaign or explicit supported performance-report tooling. |
| `Tools/Presentation/summarize_foreground_profile.py` | Isolated campaign or explicit supported performance-report tooling. |
| `Docs/testing/evidence/living-dungeon-b3b2/qualification.md` | Actual baseline/corrected results, failures, source applicability, preservation and this manifest. |
| `docs/planning/post-gd60-mvp-execution-plan.md` | Dated correction status linked to this qualification record. |

Two additional presentation-only unit cases at `28efcce9` verify committed receipt party damage/casualty accounting, pending-result suppression, localized bypass reasons, trap casualty and carried-loot text; the existing PlayMode bridge invokes them too. No runtime/asset change follows `cc215955`; its performance measurement remains source-applicable. **Attempt09 focused passed 61/61, zero failures/skips**, exact source `28efcce96e852affb6a6215193a2668ee5585e8e`. **Attempt10 readability passed 131/131**, zero failures/skips, same source: VisualRun/C1 replay, B3B1 timed checkpoints, TimeService and SimulationClock. The affected boundary/edit run at cc215955 remains applicable because only these two test methods and their PlayMode bridge were added afterward. Static checks retained in `static-checks.json`: 1,296 unique English keys and clean committed diff. No expensive full-suite rerun is claimed.

**Attempt11 legacy/save EditMode regression passed 340/340, zero failures/skips**, source `28efcce96e852affb6a6215193a2668ee5585e8e`: A/B1/B2/B3A, manual C0/C2, detached canonical/Windows atomic persistence, migration/history/Heat/loot cases. A final scene-only test extension at `4b6b6236bb0d5c5330de6f575582bc3a3ee01837` captures actual resolved encounter feedback in all three Large layouts and checks map/action visibility plus unchanged canonical bytes. No runtime/asset change follows cc215955. **Attempt12 probe passed 2/2, zero failures/skips**, exact source `4b6b6236bb0d5c5330de6f575582bc3a3ee01837`. Actual resolved Skeleton feedback (6 attacks / 2 party HP lost / 0 fallen) is captured at 720×1280, 1280×720 and 360×640 Large in `outputs-encounter12/phase7a4-screenshots/`; narrow image inspected. Main Edit/Replay actions and the map remain visible; metrics remain scrollable and the party caption can ellipsize at the narrowest size. Existing zoom/Hide floors remains useful there; no full HUD redesign or new artwork is claimed. Presentation leaves canonical bytes unchanged. **Attempt13 PlayMode passed 10/10, zero failures/skips**, exact source `4b6b6236bb0d5c5330de6f575582bc3a3ee01837`. B3B2/B3B1/A/B1/B2/B3A/C2 reflection integration includes the new observer, session replay, terminal UTC and feedback cases. **Attempt14 Windows Development Build passed**, exact source `4b6b6236bb0d5c5330de6f575582bc3a3ee01837`, Unity6000.3.2f1 / StandaloneWindows64 / Development / real Bootstrap. BuildReport: **181,849,198 bytes, 0 errors, 1 warning** (empty-URI native-symbol upload). The raw Editor log separately reports LicensingClient validation and missing Cloud Diagnostics credentials; both are reproduced in BEFORE build06 logs. They are retained, not suppressed or called compilation errors. Packed artwork verification passed. No subsequent runtime/asset change is planned.

Sustained sequence inspection (`editor-sequence-summary.json`): first ten/last ten runs each contribute 80 real event frames. Before mean 570.908/837.156 ms; after 165.358/230.385 ms. Retained-history growth still raises complete-save cost; it is bounded by the unchanged history configuration. Corrected last-ten managed used memory ranges 735,395,840–895,135,744 bytes (first-ten peak 915,951,616); native allocation includes retained sample growth. These are observations, not a leak-free or mobile-performance certification. First admission (`first-admission-summary.json`) before/after Editor update 322.764/199.636 ms, Main Thread 323.882/203.286 ms; standalone baseline first admission391.436 ms. Native final figures remain pending.

Final player: `Builds/Development/Windows-living-dungeon-b3b2-20261019-live-after/Dungeon Lord.exe`. `build-hashes.json` records the constant Unity launcher separately from gameplay assembly SHA-256 **be4375cd8cc49d65e8126e72e6788147c142a3bdcd2cb64018208cf6ced8fe1f** and packed managers **7e819d9737115d6eaab90d4e34988a57141b46fbbaa2de527a1e99fd29d2c2b7**. **Native three-process smoke passed** (admit / settle / finish), exact same build/source `4b6b6236`. Real monotonic-source check admitted 1,200 logical units before explicit qualification acceleration; active edit committed; exact checkpoint reopened; first terminal settled once; second admission used changed geometry; second exact reopen settled once. `native-final-save.json` was copied only after process exit: 25,935 bytes, SHA-256 `11d4baf750749c58e6f03dd11fea071b7bb88908d267c507ef0b423961b4b7fd`, zero parties, run-1/run-2 once each, next sequence3. All three native logs have no error/exception/failure/leak match. Sustained final native profile remains pending. Native qualification uses the same strict isolated product with a separate baked save filename; native profile uses another fresh report-derived filename. No live save file will be opened during writing.

### Gameplay-only retest after technical closeout

1. Launch `Builds/Development/Windows-living-dungeon-b3b2-20261019-live-final/Dungeon Lord.exe` normally, without smoke arguments. This isolated product already contains an eligible Floor1 dungeon; the owner's ordinary save is separate. Stay focused on Dungeon and wait for the next automatic opportunity. Its stored interval may already be partly elapsed; later opportunities use the unchanged approved pacing. Do not use manual run controls.
2. Follow the Adventurers label and adjacent motion. Read the pending action, alive/total count, carried loot and committed encounter/trap/loot feedback. At completion check survivors/fallen, route result and extracted loot. Replay a just-completed run before changing compatible geometry. Reopened history intentionally retains its saved summary rather than detailed transient playback.
3. During a later active party, enter Edit dungeon, make a valid affordable content move and Save Changes. The current run keeps its snapshot; incompatible live geometry hides with explanation. The following party uses the edit. Pause/focus away and reopen with a pending party to check continuation; no offline adventurer time is granted.
4. Check Display > Large in portrait. Main activity and Edit/Replay controls remain available; use the existing scrollable metrics, Hide floors and zoom at the narrowest layout. The caption may ellipsize there. No technical failure injection, log capture or profiling is assigned to the owner.

### Final sustained native evidence and remaining limits

The final source/build `4b6b6236bb0d5c5330de6f575582bc3a3ee01837` completed **68/68 autonomous runs**. Raw `native-profile.json`, command/log and supported `native-summary.json` are retained in AFTER. Native logs contain no error/exception/failure/leak match. The opt-in run uses actual rendering/UI at 1280×720 in batchmode (not `-nographics`), explicit accelerated qualification time and the real native writer; this is event-frame evidence, not only idle measurements or a normal-speed frame-rate certification.

| Native category / counter | BEFORE | AFTER |
| --- | --- | --- |
| 544 event updates, mean / p95 / maximum ms | 910.609 / 1,086.018 / 1,155.701 | **233.024 / 318.901 / 382.704** |
| Event Main Thread mean / p95 ms | See baseline raw/summary | **233.405 / 319.354** |
| 68 admissions, mean ms | 1,041.315 | **359.243** |
| First admission, update / Main Thread ms | 391.436 / 391.655 | **238.258 / 238.514** |
| 68 settlements, mean ms | 929.588 | **350.450** |
| 75 passive/initialization writes, mean ms | 793.115 | **229.753** |
| Event profiler frame-allocation mean bytes | 395,136,127 | **88,137,516** |
| Stage / Restore calls | 4,120 / 19,040 | **1,168 / 7,072** |
| Atomic writes / full controller Present | 755 / 755 | **755 / 755** |
| World reconstruction | One at initialization | **One at initialization; none between events** |
| Gen0 collection-counter increment | 145,599 | **36,258** |
| Managed used memory range bytes | 7,159,808–21,987,328 | **7,094,272–22,220,800** |
| Native allocated memory first→last bytes | 85,410,435→86,232,273 | **85,140,536→86,110,401** |

Mean native event update improves **74.4%**, p95 **70.6%**, frame allocation **77.7%**. Main Thread event timings corroborate UpdateForeground measurements. Lifetime probes remain enabled; their baseline total was small and they were not removed. Idle/eligible/between-event updates remain below .001 ms mean with zero Stage/Restore/AtomicWrite/full Present calls; idle profiler-frame allocation median604 bytes includes the retained qualification row. Motion is real per-frame presentation work: between-event Main Thread mean/p95 .299/.825 ms versus .191/.574 before; allocation mean/p95 17,984/94,896 bytes versus 7,579/30,368 before. Do not claim zero rendering/layout allocation. The current-thread allocation API is unavailable in both builds; raw scope zero readings are not allocation evidence.

First-ten/last-ten native event means **178.372/241.390 ms**, down from 651.896/946.443; each group has 80 event frames. Both builds render once at frame0 initialization. The after profile's native allocation grows969,865 bytes including retained instrumentation samples; managed peaks differ by233,472 bytes. These observations do not establish a mobile or IL2CPP memory/performance qualification. The private retained report list itself contributes bounded growth.

**Exact gameplay equality:** both terminal canonical snapshots are **82,690 bytes**. Full recursive typed comparison finds only `primary.createdUtcUnix`, `primary.lastSavedUtcUnix` and `primary.lastPausedUtcUnix` different. Replacing only those three integer tokens also makes the raw canonical bytes exactly equal, SHA-256 **367fa433dd6c05baf42f6ef415af8589eb58ff9e2e87fb0157ca83ebb136da89**. Scheduler horizons/revisions/order, allocator, stable ownership, monster HP/revival, mana, Heat, loot, knowledge, research and retained run outcomes are therefore unchanged across the complete measured sequence. `native-canonical-equivalence.json`, `native-byte-equivalence.json` and sequence summaries retain the proof. This is stronger than comparing a checkpoint identity or event count alone.

The synchronous durable path still has visible **approximately230–400 ms** event/admission/settlement stalls (Editor maxima up to492ms); full-save validation and native I/O remain real costs. This correction removes the measured redundant work; it does **not** claim smooth60FPS event frames, mobile readiness, a new performance budget, or permission to bypass validation/durability. Independent review should scrutinize fresh same-transaction validation reuse, closure UTC retention, observer exclusion from authority and bounded presentation allocations. Narrow360×640 Large remains compact/scrollable with caption ellipsis; existing zoom/Hide floors helps. Historical detailed replay remains unavailable after reopen, and an edited incompatible layout cannot replay fabricated geometry.

All required corrected-source automated/build/native gates above pass. Earlier full-suite results, failures and skips remain historical at their original SHAs, and no fresh full-suite run is claimed. Schema15, migration readers, checkpoint encodings/digests, gameplay timing/work limits, C2 net-damage settlement and active-run editing permissions are unchanged. Final preservation/restoration audit and documentation-only closeout are pending; do not merge.

### Final review recovery no-op edge check (pending reproduction)

The AFTER campaign closed with **10,173/10,173 protected files and7,936/7,936 historical evidence files unchanged, zero new exceptions**; substitutions restored. Before pushing, code review identified a possible equivalence gap in the new recovery no-op predicate: double-wallet subtraction can round to the same value at a valid very large finite balance while HP/revival changes. Admission captures from the last validated owned condition, so that condition must be staged before a same-boundary opportunity. Source `a4e88981` adds an actual writer/admission test; no runtime correction is made before reproducing it. A fresh FINAL identity is used because AFTER is closed; its evidence is preserved and not reopened. Existing successful source/build results above remain valid at their stated SHAs, but final review readiness is deferred until this check completes. No recovery formula, tuning or economy change is authorized by this investigation.

The FINAL campaign preserved **10,476 protected files and8,200 historical evidence files** before installing unused product `Dungeon Lord B3B2 Smoke 20261019 Live Final`. **Probe01 reproduced a real staging defect**, source `a4e88981834aa7511da6da764b5d814647113743`: 0passed/1failed, expected admitted monsterHP6 but actual1. The fixture uses valid finite1e20 mana, revival-pendingHP1, a real45-second opportunity immediately after a non-tick horizon, and the existing recovery resolver. Its existing debit rounds away at that double magnitude while HP/revival still changes. The new optimization wrongly treated wallet equality as condition equality.

Correction source `4ee533c5c1240a2d8b12caefb86526c1b2a2a77a` adds a bounded allocation-free comparison of copied monsterHP/revival before calling a recovery pass a no-op. Changed condition is staged before same-boundary admission; true no-ops remain skipped. No recovery formula, debit arithmetic, C2 net-damage reconciliation or gameplay tuning changes. The new test also runs through the existing PlayMode integration bridge. Final affected tests/profiles/build/native qualification are pending. Earlier successful captures,131replay and340manual/legacy/save cases remain source-applicable because their runtime paths did not change; they are not relabeled as new runs. Foreground/failure/lifecycle scene coverage will rerun in the focused62 filter.

**FINAL02 focused passed 62/62, zero failures/skips**, source `4ee533c5c1240a2d8b12caefb86526c1b2a2a77a`. The before-fix single test failed with admitted HP1; the same test now admits HP6 and clears revival as the existing resolver requires. No blanket extra recovery stage or debit-formula change was introduced. FINAL03 PlayMode, refreshed profiles, final Windows build/native and preservation closeout are pending.

**FINAL03 PlayMode integration passed10/10, zero failures/skips**, source `4ee533c5c1240a2d8b12caefb86526c1b2a2a77a`, including the new large-wallet recovery/admission regression. FINAL04 refreshes Editor68-run performance; a new Windows/native source qualification follows.

**FINAL04 Editor sustained profile passed1/1,68 completed runs**, exact runtime source `4ee533c5c1240a2d8b12caefb86526c1b2a2a77a`. Raw `outputs-profile04/foreground-profile-3cbb62a5d43b473aa9d8ef29d26cf27f.json`, `editor-summary.json`:544 event frames mean216.261ms / p95 296.458ms / max399.762ms; Main Thread mean218.167/p95298.637ms. Admissions mean328.560ms, settlements320.272ms, passive208.811ms. Stage1168 / Restore7072 / atomic755 / fullPresent755 / WorldRender0: ordinary-work counts remain identical to AFTER. Event-frame allocation88,597,698bytes; current-thread counter remains unavailable. Managed used714,485,760–948,604,928bytes; native allocated340,591,765→341,440,029bytes including retained samples; Gen0 counter increment534. Between-event UpdateForeground mean.000323ms with no full-save work; Main Threadp953.545ms includes Editor activity. Editor event mean/p95 were796.314/957.012ms before; the refreshed final figures supersede earlier corrected-source Editor timing for final-source claims.

Remaining build/native stages run through `complete-native-qualification.ps1`, a fixed sequential invocation of the existing adapter. It stops on any nonzero result, retains each normal command/provenance report, and copies the isolated native canonical save only after each launched player has exited. No live-save read or new qualification framework is introduced. Final build/native qualification remains pending.

**FINAL05 Windows Development Build passed**, exact source `4ee533c5c1240a2d8b12caefb86526c1b2a2a77a`:181,849,258bytes /0BuildReporterrors /1native-symbol-upload warning; same recorded licensing/CloudDiagnostics environment diagnostics as baseline. Gameplay assembly SHA-256 `157a010a34b9c4c44da299a6f12d04d56c08f47476b16a29a1357d4825090a5a`; packed managers `e57d113a9a224dc9bc22942dac7772345128c16ea13f3c24c277833a21c13dea`. Source/provenance/art hashes retained. Native admit and settle passed; frozen snapshots copied only after exit. The measured admitted format2 checkpoint is23,060bytes atordinal2; complete snapshot39,766bytes. Final native finish/profile remain pending.

FINAL native admit/settle/finish **all passed** at `4ee533c5`. Frozen post-exit complete saves measure **39,766 /49,718 /25,923 bytes**, schemas parsed as15. Format2 checkpoints measure **23,060 bytes atordinal2** and **23,063 bytes atordinal0**, below the unchanged65,536-byte limit. Final save SHA-256 `c4ef0bb7b6423c1beaece94cd85a4fcbc593751488e1f90ef0c3bc393784afcf`; zero retained parties, allocator3, histories run-1/run-2 once each. All three native logs have no error/exception/failure/leak match. The sustained profile uses a separate fresh filename and cannot overwrite this prepopulated native gameplay save. Its result remains pending.

### Final corrected-source closeout (supersedes pending statements above)

Final runtime/test/build/native source: **`4ee533c5c1240a2d8b12caefb86526c1b2a2a77a`**. Later commits update documentation only. Main baseline remains **`5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d`**; branch and existing PR245 are unchanged. No source input changed after the final Windows build. GitHub pre-push inspection confirms PR245 remains open/unmerged at the previously reviewed78a94fe3 head; both earlier inline threads remain resolved.

| Final-source gate | Actual result |
| --- | --- |
| FINAL02 focused foreground/EditMode, including live scene/layout and recovery edge | **62/62 passed; 0 failed/skipped** |
| FINAL03 affected PlayMode integration | **10/10 passed; 0 failed/skipped** |
| FINAL04 sustained Editor production profile | **1/1 passed; 68 completed runs** |
| FINAL05 Windows Development Build | **Succeeded;181,849,258 bytes;0 BuildReport errors /1 symbol warning** |
| Native admit / settle / finish / profile | **All passed;68 profile runs** |
| FINAL06 manual C0/C2, A/B1/B2/B3A, detached/Windows atomic-save, migration/history/economy EditMode regression | **340/340 passed;0 failed/skipped** |

The earlier168 boundary/edit and131 replay/checkpoint/clock cases and their Large captures retain their stated source applicability: presentation, clock and codecs did not change after those runs; final62 reruns the affected B3B2 scene/lifecycle/save cases, including six pending/resolved Large captures. Overlapping filters must not be summed into a unique test total. The final340 rerun supersedes the earlier planned reuse of manual/save results because manual barriers can transit foreground draining. Static checks at the final runtime verify34 changed files,1,296 unique localization keys, clean committed diff and unchanged schema/migration/kernel/recovery-rule/reconciliation/tuning files.

| Sustained measured event frame | BEFORE | FINAL |
| --- | --- | --- |
| Editor update mean /p95 ms | 796.314 /957.012 | **216.261 /296.458** |
| Native update mean /p95 ms | 910.609 /1,086.018 | **235.995 /325.777** |
| Native Main Thread mean /p95 ms | Baseline raw recorder retained | **236.373 /326.243** |
| Native event profiler allocation mean bytes | 395,136,127 | **88,137,517** |
| Stage /Restore /atomic calls | 4,120 /19,040 /755 | **1,168 /7,072 /755** |

Final native544 event frames include actual combat/movement processing. Admissions68 average363.345ms; settlements68 average354.683ms; passive/initialization75 average232.406ms. First admission update216.602ms versus391.436 before; first-ten/last-ten event means179.430/243.006ms versus651.896/946.443. Between-event UpdateForeground mean remains below.001ms with zero Stage/Restore/atomic/full Present work; Main Thread mean/p95 .302/.824ms, and profiler allocation mean/p95 17,984/94,896 bytes reflects motion/UI work rather than zero allocation. Both native profiles render the world once at initialization, with755 full Present calls at persisted boundaries. Current-thread allocation remains unavailable; supported frame counters are used.

Final managed used memory ranges6,307,840–22,319,104 bytes; native allocated85,141,964→86,111,777 bytes including retained instrumentation samples. Gen0 collection-counter increment36,225 versus145,599 before. No mobile/IL2CPP, leak-free, normal-speed60FPS or zero-GC certification is claimed. The final native mean/p95 event reductions are approximately74%/70%; remaining synchronous durable stalls around236–424ms and motion allocation remain explicit independent-review risks. Editor maxima reach484ms on admissions.

**Final gameplay equivalence:** both68-run terminal snapshots measure82,690 bytes. Recursive typed comparison and raw-byte comparison differ only at the three integer UTC fields `createdUtcUnix`, `lastSavedUtcUnix`, `lastPausedUtcUnix`. Replacing only those tokens yields identical bytes, SHA-256 **367fa433dd6c05baf42f6ef415af8589eb58ff9e2e87fb0157ca83ebb136da89**. Thus all saved ownership/IDs, HP/revival, Heat/mana/loot, knowledge/research, scheduler/order/allocator and retained outcomes match the original measured production path. `native-canonical-equivalence.json` retains this exact proof. Final native logs contain no error/exception/failure/leak match. Build licensing/symbol-upload diagnostics remain retained and reproduced in baseline logs.

The large-wallet staging regression remains explicit: FINAL01 at a4e88981 fails expected6HP/actual1HP; FINAL02 and03 pass after the bounded condition comparison. Existing double debit arithmetic is unchanged; this packet fixes admission ordering of the resolver's actual condition result, not money precision or pricing. The earlier introduced UTC regression and layout/qualification failures remain preserved at their original SHAs.

The schema target remains **15**, with **no migration changes**, no new historical replay schema/journal, no tuning/budget change and no C2 net-damage reconciliation change. B1 format1/executable and ProfileA remain frozen; manual C0/C2 and the restricted B3A path remain qualified. Production scope stays one active Floor1/one party; no descent/queues/offline adventurers/secure foreground provenance or broader Living Dungeon completion is claimed.

All required corrected-source technical gates pass. Final preservation/restoration audit is pending; independent review remains required and the PR must remain unmerged. Gameplay-only owner steps above target the new isolated FINAL player after closeout.
