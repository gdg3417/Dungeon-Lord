# Living Dungeon Phase B1 qualification

Verified starting baseline: `eeaf9d1516c2ffa5d6600238a3e46fcb3787ecc8` on clean local main, origin/main and live GitHub main; PR #233 merged including its nonspatial loot boundary correction. Branch: `codex/living-dungeon-phase-b1`. PR #234 did not exist at the pre-publication check; the actual created number must be reported. Actual session metadata confirms GPT-6.1 Sol, High; classification Complex, locked deterministic continuation/deadline scope; no fallback selected.

AGENTS.md/model policy, active roadmap, owner lock, Phase A contract/implementation/review correction, 38 frozen scenarios/evidence, Specs 17/25/28/29/36 and glossary invariants were inspected. Canonical validation, frozen schemas/migrations, detached writer/session, both Heat stages and save/readback boundaries were reviewed. The [technical contract](../../../planning/living-dungeon-phase-b1-implementation-contract.md) preceded runtime changes; [complete-diff self-review](self-review.md) records the review concerns and remaining independent qualification.

## Implemented scope

Explicit format-1 bytes retain complete immutable start inputs, executable identity, ordinal, elapsed logical units, accepted time bound, terminal/consumed flags, rolling committed event/consequence digest and exact boundary HP/carry/reach evidence. A fresh engine/party/iterator is created from owned bytes and replays only the committed prefix. Independent complete-checkpoint/start identities and expected ordinal, bounded decoding/replay, exact evidence comparison and canonical recapture are mandatory. No original iterator/object references or reflection serialization are used. No finished run record or separately writable event history is retained.

The single existing iterator path announces kind/distance before consequences. Checked whole-boundary due time is elapsed units plus traversed-edge units and interaction/transfer duration. Inclusive deadlines commit only due boundaries; equal times keep ordinals and a finite event budget. Preparing an announcement computes pure plans/static inputs, not future damage/reward/choice. Pending announcements are derived and regenerated after restore. Untimed manual drains preserve original results. Explicit ordinal commits after deadline calls advance the recorded minimum accepted time to committed elapsed time; the regression restores after each such step and after consumed terminal retrieval. Future deadline-governed schedulers must exclusively use the deadline API.

Only the qualified B1 executable behavior/serializer is supported; unsupported historical rules fail closed. A hash/version/configuration does not implement old executable semantics. No production timing or clock conversion, persistent active party, admission/concurrency, offline catch-up, settlement, schema or migration is activated. Schema **13**, SaveService/GameRoot, canonical writer/order and production filenames are unchanged.

## Final automated results

| Check | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Focused EditMode, B1 plus Phase A/frozen comparison | 76 | 76 | 0 | 0 |
| Focused PlayMode, fresh restoration/deadline/frame pause plus Phase A | 3 | 3 | 0 | 0 |
| Full EditMode, final corrected source | 1,781 | 1,780 | 0 | 1 |
| Full PlayMode, final corrected source | 3,215 | 3,205 | 0 | 10 |
| Production spatial build gates within full EditMode | 65 | 65 | 0 | 0 |
| Existing strict-input Python tests | 12 | 12 | 0 | 0 |

All **38 frozen Phase A structured scenarios** remain equivalent; fixture SHA-256 `53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198`, unchanged and never regenerated. Compared with final merged #233 qualification: **zero removed tests, zero changed existing outcomes**, 34 added EditMode and 35 added PlayMode cases; all established skips remain. [Exact comparisons](test-outcome-comparison.json), [reports/failures/skips/measurements](report-summary.json), [original XML](qualification-reports.zip), [production gates](production-gates.json), [fixture evidence](baseline.json).

Every-position restoration covers 33-event clear, 6-event wipe (partial and terminal casualties), 17-event nonspatial and retreat routes, 25-event optional branch approach/choice/assignments/return, 22-event two-floor traversal/choice/transfer and legacy null/empty input families. Each restored instance compares exact inspection, remaining event sequence and full structured terminal outcomes to uninterrupted references. Repeated cycles, consumed terminal retrieval, canonical/config edits, independent commit pins, unsupported rules, damaged/truncated encoding, altered start/ordinal/time/HP and replay/byte limits are covered. Deadline tests compare no-effects inspection before due boundaries, exact eligibility, all remaining events, zero durations, equal times, budgets 1/2/7, backward/invalid calls and pre-effect overflow. Failed restoration never exposes an execution or publishes a save/outcome.

Windows **StandaloneWindows64 Development**, Unity **6000.3.2f1**, Bootstrap: **Succeeded, zero errors, one warning**. Unity native-symbol upload could not connect: `Invalid URI: The URI is empty`; Cloud Diagnostics upload credentials were unavailable. All **52** referenced artwork assets packed. Output: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-living-dungeon-phase-b1/Dungeon Lord.exe`. **303 files / 181,913,716 bytes** including reports; BuildReport totalBuildSize **181,441,664 bytes**. Five final focused/full/build logs have zero C# compiler errors/warnings. [Build report/file inventory](windows-build.json), [compiler log index](log-index.json), [redacted retained logs](unity-logs-redacted.zip), [static checks](static-checks.json).

## Commands and source preservation

Unity CLI: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; bundled Python runtime. Project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation`, existing Library retained. Default and fallback save namespace: `living-dungeon-phase-b1-validation.json`.

```text
python Tools/Simulation/phase_b1_qualification.py audit
python Tools/Simulation/phase_b1_qualification.py prepare --name <stage>
unity test <isolated-project> --mode EditMode --filter LivingDungeonPhaseB1;LivingDungeonPhaseA --output <repo>/TestResults/phase-b1-focused-editmode.xml --timeout 600 --no-color -- -logFile <repo>/Temp/phase-b1-focused-editmode.log
unity test <isolated-project> --mode PlayMode --filter LivingDungeonPhaseB1PlayModeTests;LivingDungeonPhaseAPlayModeTests --output <repo>/TestResults/phase-b1-focused-playmode.xml --timeout 600 --no-color -- -logFile <repo>/Temp/phase-b1-focused-playmode.log
unity test <isolated-project> --mode EditMode --output <repo>/TestResults/phase-b1-full-editmode.xml --timeout 1800 --no-color -- -logFile <repo>/Temp/phase-b1-full-editmode.log
unity test <isolated-project> --mode PlayMode --output <repo>/TestResults/phase-b1-full-playmode.xml --timeout 1800 --no-color -- -logFile <repo>/Temp/phase-b1-full-playmode.log
unity run <isolated-project> --timeout 1800 --no-color -- -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonPhaseB1Windows -logFile <repo>/Temp/phase-b1-build.log
python -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py
python Tools/Simulation/phase_b1_qualification.py restore-test-saves
python Tools/Simulation/phase_b1_qualification.py collect
git diff --check
```

The semicolon-separated test filter is one quoted shell argument. Every stage starts with exact four-root input comparison/preparation; source changes and disposable Unity serialization changes are archived before refreshing inputs. Final **1,238** inputs match exactly under established filename/text-normalization rules. Post-PlayMode drift was TMP fallback serialization and ProjectSettings ordering; post-build drift was URP/global settings, PlayerSettings and UnityConnect derived serialization. These strict-check rejections are retained, not exempted. Root owner settings/assets were unchanged. [Input checkpoints/differences](input-checkpoints.zip).

All **2,434** original audited owner save/settings and prior-player files remain unchanged in inventory/hashes, preserving eight earlier players. Expanded audit preserves **2,462** files, including all preexisting non-B1 saves. Existing PlayMode tests deliberately overwrite their own 22 disposable fixture filenames; their exact pre-run bytes were retained and restored after testing. The restore helper prevalidates every target/backup, only permits those fixture-name families and refuses owner-primary/prior-player changes. No primary save was restored, reset or used as a destructive fixture. No existing cache or historical player was deleted. [Original audit](owner-preservation.json), [expanded audit and exact fixture restoration](expanded-save-preservation.json). This is a scoped file audit, not a registry or whole-device claim.

## Retained failures and corrections

The sandboxed initial Unity launch stalled without producing results; only its isolated test processes were stopped, then host validation was used. First host compile aborted on a missing route namespace and PlayMode's prohibited direct assembly reference; corrected import and existing reflection bridge resolved them. A second compile aborted on two struct-to-object method-group conversions; explicit lambdas corrected them. Initial focused report: **67/69 passed, two failed** because the new two-floor fixture had not constructed Floor 2; it now uses the established eligible-floor fixture. Next report: **73/74 passed, one failed** because a new corruption test used an incorrect binary offset; fixed without changing runtime/assertions.

The next **75/75** focused and full **1,779 EditMode / 3,204 PlayMode** reports passed with existing skips. Complete-diff self-review then found that explicit ordinal advancement following a deadline could leave checkpoint elapsed time beyond its recorded accepted bound. Corrected that metadata invariant, added the every-step restore regression and repeated focused/full validation from final exact source. All prior reports/source evidence remain retained. No existing assertion was removed, weakened or skipped. Strict-input tests initially had sandbox temporary-Git setup/cleanup errors; the authorized host run passed 12/12. Python syntax and diff checks passed; Git's LF-to-CRLF checkout notices are ordinary text normalization, not C# warnings.

## Measured workload and limits

Final focused Windows Editor observations, synthetic timing/configuration only:

| Fixture | Events | Checkpoint bytes | Advance/drain ms | Encode ms | Restore + verification ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Single room | 17 | 19,004 | 0.3094 | 0.2630 | 15.8586 |
| Canonical branch | 25 | 39,011 | 0.6505 | 0.5827 | 35.4269 |
| Canonical two floors | 22 | 41,386 | 0.9494 | 0.5668 | 34.1491 |
| 100-room, long terminal prefix | 1,601 | 225,322 | 24.2471 | 3.1503 | 234.8208 |

Restore timings include export/byte recapture/structured inspection checks in the test helper; they are conservative restore-and-verification observations, not isolated API timings or worst-device guarantees. The longest fixture replays its whole 1,601-event committed prefix. Runtime does not retain the test's full observed event collection. No trustworthy allocation-byte, zero-allocation, mobile responsiveness, multi-month catch-up or multi-party throughput claim is made. Tests inject a 4,000,000-byte/10,000-replay-event envelope; these are **test values**, not production policy. An unacceptable prefix rejects with `run.checkpoint.replay_limit` before input reconstruction/replay; oversized bytes reject before parsing. Work is captured-input planning plus committed events and existing bounded resolver/party work; memory is retained inputs/current plans/reached diagnostics and encoding/hash/member-copy temporaries.

## Remaining activation/review work

B2/B3 require reviewed canonical schema/migration owning retained starts/checkpoints, scheduler identity/admission reservation, accepted time basis, pending parties and proven committed prefix in one transaction. That transaction must supply trusted pins/freshness, order canonical edits/settlements, enforce duplicate RunIds, merge applicable learning, retain both Heat stages against latest state, resolve mixed-period aggregate attribution, classify objective/reward eligibility individually and recover without duplicated grants. No second save/journal or background settlement is approved here. Production timing/world-clock mapping and historical executable preservation/rejection remain gates; exact bounded catch-up and concurrency need separate performance qualification. Phase C presentation remains deferred.

Independent review and owner Unity Editor/Windows UAT remain required. In an isolated profile, confirm existing manual postures/clear/retreat/wipe, repeated content, optional branches/two floors, localized results, both Heat stages, exactly one history/sequence/objective update and schema-13 save/reopen/failure preservation. Inspect the new API in qualification tests; production UI retains completed manual runs. Automated success establishes this B1 packet, not merge readiness or completion of Living Dungeon Phase B. Do not merge in this task.

See [exact changed files](changed-files.json) for runtime, tests/metadata/registration, preserving build slot, tooling, roadmap, contract and evidence scope.
