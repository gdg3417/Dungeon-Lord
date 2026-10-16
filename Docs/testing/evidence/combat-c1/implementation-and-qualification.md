# Combat C1 authoritative graphical replay

Current presentation correction (2026-10-16): owner Editor UAT identified obscured monster art and missing map-local C0 damage. The [readability correction and affected qualification](readability-20261016/qualification.md) supersede the initial label/feedback choices below. Original implementation reports retain their historical source applicability. Independent review and the short owner retest are next.

Status (2026-10-16): implementation and required automated qualification complete. Independent ChatGPT PR review is next. Owner UAT has not started and acceptance is not claimed. Do not merge.

Baseline: clean local `main` and freshly fetched `origin/main` both `22e7ce1b08ae17391bfd3542a45821a46f02bbe1`, containing merged PR #238. Implementation branch: `codex/combat-c1-graphical-replay`. Live GitHub records for #230, #231, #236, #237 and #238 were reviewed, including #236 and #238 final owner acceptance. Historical pre-merge reports retain their dated meaning.

Initial focused-qualified implementation: `e6bbd68f65d10a7e09cf0c2f6994cf23c30dd6da`. Full suites qualified `f501344b4900a2bc70731acd7fa24fa53c85dd38` (ordinary no-run sheet height correction). Final Unity source is `a4211d7435139feb6146fb7c5dcb8e9b226b496a` (read-only position validation and regression variants), with affected follow-up qualification below. Later documentation/tooling commits do not change the four Unity input trees. `source-identity.json` records their exact Git hashes and the frozen B1 fixture hash.

Recommended Codex configuration: GPT-6.1 Sol, High. Classification: Complex. Reason: locked integration across C0/B1 evidence, spatial rendering, localization and lifecycle boundaries. Fallback if unavailable: GPT-6 Sol High, then GPT-6 Astra Low only under the supplied availability/capability conditions. No fallback or delegation was requested. The recommendation does not claim independent verification of runtime model metadata.

## Projection and authority

Changed files by purpose (complete path inventory: `changed-files.json`):

- Projection: new `DungeonCombatReplay.cs`; the existing `DungeonRunReplay.cs` dispatch, frame and cursor extensions.
- Production display: `ProductionDungeonReplay.cs`, `DungeonFloorWorldReplay.cs`, and material cleanup in `DungeonFloorWorldView.cs`.
- Presentation configuration/localization: `DungeonPresentationPolicy.cs`, `Presentation.asset`, `Dungeon.uxml`, `Dungeon.uss`, and bootstrap `string_table_en.json`.
- Validation: new `CombatC1ReplayTests.cs`, Editor wrapper and production-scene cases; PlayMode adapters; narrowly updated C0 eligibility assertions; one `DungeonVisualBuildQualification` build entry. New C# files include Unity metadata. No exact consumer allowlist or production gate was broadened.
- Qualification/documentation: the two C1 preserving/collection helpers, current roadmap addendum, this report and original evidence archives. Historical reports and frozen B1 fixtures remain unchanged.

`DungeonRunReplay.TryCreate` dispatches C0 to a detached projection in `DungeonCombatReplay.cs`. The B1 builder, historical one-sided approach/damage behavior and frozen executable/checkpoint fixtures remain separate. No C0 event is sent through B1's monster-damage animation.

Committed `PresentationSteps` supply traversal order. Each combat boundary is joined against its attached immutable events and the outcome's globally stamped `CombatEvents`. Event ordinal, run boundary, run/floor/room/encounter identity, round, action ordinal, directed eligibility and source/target identities/positions must agree. Aggregate spatial records are indexed by stable floor/room/assignment and checked against the matching encounter; their storage order never supplies chronology. The route's first monster assignment is only a boundary identifier, not its whole participant set.

Actors are keyed by full immutable run/member or run/monster identity. Labels A/M are stable aliases linked to those keys, not a current display-list selection. Party HP starts from the captured roster maximum; monster HP starts from captured `CombatMonsters.MaxHealth`. Only exact recorded attack/trap before/after values advance a preparation-only ledger. Each before value must join the preceding state, dead actors cannot attack, legal directed links must support attacks, skips must agree with recorded health/targets, and final HP/survivor/death/wipe evidence must agree. This does not calculate damage, targeting, initiative, range, LOS or gameplay positions. Existing no-encounter route success/survival flag conventions are preserved rather than equated.

The frozen output contains per-actor bounded HP/position transitions and immutable display frames. Binary lookup replaces rescanning the event stream during display. All events are retained and validated. Eligibility rows are grouped as encounter position reasons rather than individually delaying playback; attacks, skips, starts/completions and bypasses remain chronological display boundaries. Next action pauses and advances one display boundary. The ordinary route, traps, loot, branches, floors and terminal result share that sequence.

Recorded room-local endpoints use the existing anchor/orientation transform. At encounter start, defenders take their recorded eligibility positions directly. C1 deliberately does not interpolate a defender approach or invent an unseen path. Party members share the one recorded anchor; identity, action and HP text distinguishes them. Floor transfers have no interpolated connecting motion.

## Production presentation and lifecycle

The existing production Dungeon screen, floor renderer, camera and controls are reused. Canonical defender sprites are bound once per selected floor, with no duplicate replay monster. One pooled line joins the recorded attacker and target. Compact world aliases avoid obscuring sprites; exact damage, target HP before/after, defeat, encounter, round and action order appear in the action description. The scrollable roster supplies individual party and defender HP. Position reasons exposes recorded denials with room-local coordinates. All player strings use localization keys and existing locale-aware formatting.

Pause/Resume/Close, Edit suspension, floor selection, pan/zoom/Fit, publication and changed-layout invalidation retain their production lifecycle. An older open replay stays pinned to its source when a newer run publishes. Close/edit restores original canonical sprite positions/colors/orders. Only the selected floor owns graphical bindings and the attack line. Pools are reused; no per-frame pathfinding, resolving or event-array scan is performed.

No gameplay resolver, capture, B1 encoding, B2 scheduling, save owner, schema 14, migration, recovery, reserve, Heat, mana, loot or run-history settlement implementation changes. Replay has no save/service/resolver reference. Tests disable ordinary foreground ticking only to measure playback side effects separately from legitimate passive mana.

## Fingerprints, transient evidence and bounds

Both the full canonical-layout fingerprint and traversed-corridor fingerprint remain required. Stable combat metadata and route relationships are additionally validated. Mismatches fail closed without mutation. Existing same-session `RunTransientEvidence.Retain` is reused unchanged. Restart keeps aggregate history but loses detailed steps/party/combat data and cannot recreate replay. No journal or durable HP is introduced.

The resumed integrity check confirmed a real defect: a corridor trap step could name the correct assignment but present a different tile. Final C0 preparation now indexes the captured traversed assignment by canonical floor index and assignment identity, verifies its floor instance, and requires Floor-space trap/loot positions to equal the captured tile. Every Floor-space route position must also lie inside bounds selected by both the canonical floor definition and index. Missing/duplicate catalog matches fail closed. The B1 builder and common coordinate transform are unchanged. Movement still carries branch identity; it is not incorrectly treated as a content assignment. Nonlocated floor transfers retain their prior behavior. No pathfinding, LOS, range, damage or settlement is recomputed.

The initial bounds regression incorrectly required its first movement to have an index greater than zero. It now accepts index zero, proves the valid one/two-floor replay first, and replaces an actual movement on the selected floor with `(int.MaxValue,int.MaxValue)` in Floor space. Parameterized regressions cover floors 0 and 1 and both corridor trap and loot interactions. The original reproduction is retained as two failures: one confirmed runtime defect and one fixture setup failure.

Presentation policy/production asset owns all new display tuning. Existing ceilings remain 8,192 route steps and 256 markers/content bindings. C1 adds explicit ceilings of 32,768 combat events, 64 actors and 131,072 actor-state transitions. Oversized streams fail with a localized capacity explanation; no chronological gameplay evidence is silently truncated. Per-update work is bounded by selected-floor bindings and actor lookups. This is not a physical-device performance or allocation-free-whole-frame claim.

## Temporary projects and saves

The pre-implementation inventory found 18 Unity qualification projects. Retain `Temp/ui-composition-combat-c0-diagnostics-validation-warm` for owner Editor/Windows UAT, with its existing `combat-c0-diagnostics-validation-20261015.json` namespace and two-floor dungeon. Initial focused qualification uses `Temp/ui-composition-combat-c0-access-validation`. Final broad qualification reuses the existing `Temp/ui-composition-combat-c0-diagnostics-validation-r2`; only this automated copy changes its fallback to `combat-c0-access-validation-20261015.json`. Scene fixtures override before Boot to unique per-fixture names. No additional disposable project was created.

For the cache fallback, all differing r2 inputs were archived before refresh. Its original Library remains at `Library-before-combat-c1-20261015`. A read-only copy of the warm owner's Library copied 40,366 files / 2.194 GiB with zero failures; no owner cache/source/save was moved or deleted. Exact source/namespace checks ran before launching tests. This is a retained cache fallback, not an assertion that a particular Unity engine defect was proven.

The continuation explicitly authorized individually verified obsolete project cleanup. As separate local housekeeping, these three copied projects were removed: `ui-composition-combat-c0-access-validation`, `ui-composition-combat-c0-integration-validation`, and `ui-composition-combat-c0-validation`. Their inputs matched historical committed sources `e6bbd68`, `d2e272f` and `a6c9d6e`; integration differed only in the known generated TMP fallback asset. No root/nested reparse point, independent checkout, project-local save/recovery file or active Editor/player was found. All 5,172 non-cache files, including original source, logs, screenshots, reports and older builds, were archived and individually SHA-256 verified before exact-path removal. Only disposable Library caches were omitted. Archives and complete manifests remain at `TestResults/combat-c1/retired-projects/`; manifest copies are in `qualification-support.zip`. The warm final C0 comparison build remains at its original path.

Fifteen project directories remain. Two are active: the protected warm owner environment and the single r2 automated environment. `ui-composition-combat-c0-b1-baseline` is pending because it contains extra golden-capture scripts; `ui-composition-combat-c0-diagnostics-validation` is pending because four source files differ from its documented qualification commit. Neither was discarded or relabeled as known committed work. Eleven older environments remain pending ownership/unique-artifact review: `room-construction-validation`, `room-construction-validation-bounds-186ba37`, `ui-composition-b2-alias-validation`, `ui-composition-final-validation`, `ui-composition-floorhud-validation`, `ui-composition-overlay-validation`, `ui-composition-review-validation`, `ui-composition-uat-validation`, `ui-composition-validation`, `ui-composition-visual-replay-owner-uat`, and `ui-composition-visual-replay-validation`. Several hold historical owner players/UAT evidence. Their safety was not established, so they were retained. The desired two-project steady state is therefore not fully reached. No new project or cleanup PR was created; save-only preservation directories remain untouched.

The initial inventory remains historical. `temporary-project-inventory-final.json` gives each final disposition. The warm project's differing inputs were archived before its exact source refresh, retaining the existing namespace. Owner saves were not opened for gameplay, copied, reset, transferred or migrated.

`temporary-project-inventory.json`, exact input manifests, before/after protected-file hashes, generated-input archives and build inventory provide the audit trail. Owner saves are never automated fixture targets and are never restored, transferred, reset or migrated. Only explicitly identified preexisting disposable test fixtures may be restored after preserving their modified bytes.

## Qualification and attempts

Unity 6000.3.2f1 (a9779f353c9b), CLI 1.0.0-beta.12, Windows x64. Exact commands, NUnit reports, original unsuccessful attempts, redacted logs, screenshots and input hashes are retained alongside this report. The first sandbox launch stalled on Windows licensing access; only that verified test Editor was stopped before retrying outside the sandbox. No owner Editor/player was opened.

The first 28-case projection run passed 25; fixture lifecycle setup and a no-op corruption attempt were corrected. The first production run exposed a two-pixel safe-area overflow; replay controls now share a row. Screenshot review replaced obstructive floating HP cards with compact world aliases and action/roster HP. Synthetic fixtures use explicit test identities, while real publication fixtures still require captured canonical fingerprints. A targeted bypass diagnostic located the existing no-encounter success/survival convention; diagnostic instrumentation was removed before final qualification. Historical artifacts are not relabeled as final-head qualification.

Initial full/complementary EditMode attempts were stopped after long quiet logs and produced no verdict. Separately, all 65 production gates and all 12 catalog/reimport cases passed. The subsequent remainder actually finished before its requested stop: 1,956 passed, one existing skip, one ordinary-sheet height failure. A quiet log was insufficient proof of a hang; no engine root cause is asserted. The extra no-run label was pushing the normal sheet above its established height bound. `f501344` restores the original condition that shows availability text only for an existing run, while keeping the localized distinct no-run reason on the disabled replay button. Seven affected UI/consumer cases then passed in r2. Final full suites run on the corrected source; failed/partial partitions are retained, not combined into passing coverage.

Final applicable results (all failures zero unless explicitly described as an earlier attempt):

| Report | Qualified source | Passed | Existing skips |
| --- | --- | ---: | ---: |
| `combat-c1-full-editmode.xml` | `f501344` | 2,034 | 1 |
| `combat-c1-full-playmode.xml` | `f501344` | 3,460 | 10 |
| `combat-c1-corrected-focused.xml` | initial uncommitted position fix | 160 | 0 |
| `combat-c1-final-head-editmode.xml` | `a4211d7` | 227 | 0 |
| `combat-c1-final-head-playmode.xml` | `a4211d7` | 163 | 0 |

The final EditMode filter is `CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests`; it includes all 43 C1 projection cases, C0 kernel/integration/diagnostics, 22 historical graphical replay cases, exact consumer checks and all 65 production gates. The final PlayMode filter is `CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved`; it includes the same projection/core regressions and six production adapters, including all three C1 scenes, retained B1 lifecycle/responsive controls and casualty behavior. The production C1 scenes exercise actual manual-run eligibility, two visible defenders, attack/HP/defeat, pause/resume/close/Next, pinned publication, edit suspension/layout invalidation, floor selection, viewport operations, nine resolution/text-size captures and stable visual count. Save/runtime bytes stay unchanged through playback with independent passive ticking disabled for measurement.

Only the detached C0 validator and its test file changed after the full suites. Affected final-source regression, production scenes and gates passed afterward. The full suites remain applicable to unchanged areas; they are not represented as reruns of `a4211d7`. This input-based decision and every exact skip are recorded in `qualification-applicability.json` inside the support archive. EditMode's existing skip is the Windows-only inverse. PlayMode keeps that inverse, eight synchronous EditMode boot fixtures and the Windows-player-only fixture. Historical C0 outcome/skip comparisons show no unexpected removal or result change. Strict-input helper tests remain 12/12 passed on unchanged helper/test inputs. Frozen B1 fixtures were never regenerated.

Exactly one final Windows x64 Development Build succeeded using `a4211d7` inputs: zero errors, one preexisting native-symbol upload warning (`UriFormatException`, empty Unity services URI). Bootstrap is the sole scene; all 52 required artwork dependencies are packed. Reported build size: 181,668,772 bytes. Executable: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c1/Dungeon Lord.exe`. Its existing disposable namespace is verified in packed assets. It was not launched. The final qualified C0 comparison player under `Windows-combat-c0-final-20261015` remains preserved. Build reports, complete artifact hashes and source identity are adjacent to this document.

Each automated and owner build checkpoint matches all 1,306 Unity inputs with zero unexpected files or remaining differences after archiving/restoring known generated settings/assets. Final PlayMode generated one such difference; the build generated four. The earlier full PlayMode checkpoint generated two. No primary source settings were changed. The original 14,745-file protected snapshot has zero byte differences: 14,139 files remain at their original paths, and 606 retired historical build files are preserved inside their verified local archives, with original-path relocation explicitly recorded. The 22 preexisting disposable test fixtures touched by broad suites were archived and restored from their original verified backups. Owner saves are excluded from that restoration mechanism.

The preserved C0 full reports predate #238's final owner-approved BootstrapOverlay reversion. Their one `C0FocusedDiagnosticsUseExistingPagingAndConstrainTextToSafeArea` method was already replaced on merged main by two `FocusedDiagnosticsPreserveHistoricalOverflowAndIgnoreStalePaging` cases. The historical comparison identifies this exact pre-C1 change separately, with its commit-diff provenance; it does not attribute it to C1 or hide arbitrary test removals. Two C1 guard-test renames keep their assertion coverage while changing the expected replay eligibility behavior.

Exact invocation conventions: Unity executable `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; bundled Python `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`; R is `C:/Dev/Dungeon-Lord`. A is `R/Temp/ui-composition-combat-c0-access-validation`; P is the existing `R/Temp/ui-composition-combat-c0-diagnostics-validation-r2`.

```text
python Tools/Presentation/qualify_combat_c1.py inventory
python Tools/Presentation/qualify_combat_c1.py prepare
unity test A --mode EditMode --filter "CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-focused-final.xml --timeout 900 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-focused-final.log
unity test A --mode PlayMode --filter "CombatC1MultiDefender|CombatC1EditSuspension|CombatC1FloorSelection|VisualRunReplayProductionLifecycle|VisualRunFeedbackCasualty" --output R/TestResults/combat-c1-production-final.xml --timeout 900 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-production-final.log
python Tools/Presentation/qualify_combat_c1.py alternate-prepare
python Tools/Presentation/qualify_combat_c1.py alternate-refresh
unity test P --mode EditMode --filter "UatContentHeightCardsDisplayAndSafeActions|CombatC1MultiDefender|CombatC1EditSuspension|CombatC1FloorSelection|VisualRunReplayProductionLifecycle|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-ui-correction.xml --timeout 900 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-ui-correction.log
python Tools/Presentation/qualify_combat_c1.py alternate-checkpoint final-source
unity test P --mode EditMode --output R/TestResults/combat-c1-full-editmode.xml --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-full-editmode-final.log
python Tools/Presentation/qualify_combat_c1.py alternate-checkpoint full-editmode
unity test P --mode PlayMode --output R/TestResults/combat-c1-full-playmode.xml --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-full-playmode.log
python Tools/Presentation/qualify_combat_c1.py alternate-checkpoint full-playmode
python -m unittest test_qualification_inputs.py  (cwd R/Tools/Presentation)
unity test P --mode EditMode --filter "CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests" --output R/TestResults/combat-c1-final-head-editmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-final-head-editmode.log
python Tools/Presentation/qualify_combat_c1.py alternate-checkpoint final-head-editmode
unity test P --mode PlayMode --filter "CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-final-head-playmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-final-head-playmode.log
python Tools/Presentation/qualify_combat_c1.py alternate-checkpoint final-head-playmode
python Tools/Presentation/qualify_combat_c1.py owner-prepare
unity run R/Temp/ui-composition-combat-c0-diagnostics-validation-warm --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC1Windows -logFile R/Temp/combat-c1-build.log
python Tools/Presentation/qualify_combat_c1.py owner-checkpoint final-build
python Tools/Presentation/qualify_combat_c1.py restore-fixtures
python Tools/Presentation/qualify_combat_c1.py audit
python Tools/Presentation/collect_combat_c1_evidence.py
```

The final sequential full-suite command stops on any nonzero test/check exit. The cache copy is preserved in `warm-cache-copy.log`; its r2 target was checked for reparse redirection and containment before the original Library was moved. Earlier partial/failing invocations keep their unique `combat-c1-*` report/log names. No unsuccessful attempt is counted twice or as a pass.

## Accepted limitations and deferred work

Session-local, latest available manual-run replay; conservative whole-layout compatibility; shared party anchor; direct boundary placement of defenders; compact aliases with detailed HP/denials in scrollable existing details; fixed original artwork and pooled attack cue. Dense labels can overlap at overview zoom and aliases can wrap at Large text; zoom/Focus/Fit and the roster retain identity/HP access. No independent member motion, numerical initiative display, historical layout overlay, durable replay, live/streaming combat, C2 persistence/recovery/reserves or Living Dungeon activation. Translated-font and physical-mobile qualification remain deferred.

## Owner UAT instructions (only after independent ChatGPT PR review confirms no remaining blockers)

1. Close every Editor/player using this namespace before opening the refreshed warm project. Do not reset, copy or migrate saves. Confirm the existing two-floor dungeon/progression loads.
2. Complete a normal C0 manual run. Replay it on the production Dungeon screen. Observe stable A/M identities, simultaneous defenders where present, attack lines, exact HP transitions, defeat, encounter/round/action order and position reasons.
3. Pause, Next action, Resume, Close and restart replay. Inspect traps, loot, branches, room/floor progression and final recorded outcome. Try floor selection, pan, zoom, Focus/Fit and all text sizes in portrait and landscape.
4. Enter Edit during replay; verify suspension. Discard an unchanged draft and resume. A committed layout/content change must close the incompatible replay and explain why it is unavailable. A newer run must not replace an already open older replay.
5. Check normal save/history/progression, then close/reopen. Aggregate history persists; old detailed replay is unavailable until a new manual run. Passive foreground mana can continue independently of playback.
6. Close Unity Editor before testing the final Windows player under the same existing disposable namespace. Repeat the checks and save/reopen parity. Do not run both concurrently or claim acceptance from automated results.

The final report/build manifest identifies the qualified source and exact player location. No owner gameplay acceptance or merge is claimed.
