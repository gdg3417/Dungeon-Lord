# Production Combat C0 qualification — 2026-10-14

> **2026-10-15 owner-approved acceptance addendum:** raw combat diagnostics navigation is not a player-facing C0 requirement. Authoritative ordered event records and automated HP/geometry/transaction tests establish mechanical correctness; C1 owns visible attacks, HP changes and graphical playback. The later overlay paging/masking correction is reverted and has focused validation only. Earlier full-suite/build results below remain historical. Final required qualification is to be determined by external review before any new full suites or Windows build. See [current evidence and limitations](../authoritative-20261015/qualification.md).

Status: **Production integration and automated qualification passed; ready for external review. PR #238 remains draft and unmerged. Owner Editor/standalone gameplay UAT is pending.** Configuration and technical policies were explicitly approved in the owner's continuation instruction; no C0 tuning decision remains unapproved. C1/C2 are deferred.

Repository `gdg3417/Dungeon-Lord`, branch `codex/combat-c0-deterministic-combat`, main baseline `eb80df1e3fd09c18aedb062afc108eb941deb8a3`, reviewed prerequisite head `c0dba584f1d1b9f10ccc50bf51fc26c831c5f81e`. Final fetch confirmed both remote references unchanged before publication.

Final runtime, configuration and test logic: `b13c76fc28484dde70f72656375dc9fd6b929612`. Metadata-qualified source: `d2e272f6dc5b12db26f1129c4c31faf295021ace`, whose only difference is three trailing spaces removed from the frozen ZIP's Unity metadata. Full EditMode and regression ran on b13c76f; a follow-up focused run, full PlayMode and Windows build ran on d2e272f. No runtime/config/test logic changed between them. Model recommendation: GPT-6.1 Sol, High; Complex, approved gameplay integration. No fallback or Astra escalation was requested; actual runtime model/reasoning metadata is not independently exposed by this task host. Subsequent closeout changes are documentation/evidence/tooling outside Unity inputs.

Host Windows 11 x64; Unity **6000.3.2f1 (a9779f353c9b)**; Unity CLI **1.0.0-beta.12**. Isolated project `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-integration-validation`; save filename `combat-c0-integration-validation-20261014.json`. Earlier prerequisite projects, owner UAT projects, saves, players and backups were preserved.

| Executed check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Final focused C0 EditMode, including production scene | 82 | 0 | 0 |
| Historical/integration/replay/build-gate regression | 405 | 0 | 0 |
| Full EditMode | 1,979 | 0 | 1 existing |
| Full PlayMode | 3,405 | 0 | 10 existing |
| Production spatial build-gate cases, included above | 65 | 0 | 0 |
| Established strict-input Python tests | 12 | 0 | 0 |

The original 42 kernel cases are retained and pass. New coverage includes production selection/configuration, actual canonical manual transactions and scene boot, geometry/range/supercover, overlap, shared rounds, separate same-room encounters, duplicate receipts, bypass, HP continuity, immutable inputs, exact budgets, technical failure/atomic rollback, save/reopen, B2 isolation and B1 replay. Frozen pre-integration inputs verify **115** checkpoint boundaries and consumed states byte-for-byte, original inspection/outcome evidence and historical config JSON. They were not regenerated under C0. See [frozen-b1-origin.json](frozen-b1-origin.json).

Comparisons with the prerequisite full-suite reports preserve test multiplicity and show **zero removed tests and zero changed existing results or skip reasons**. EditMode adds 40 passing cases; PlayMode adds 39. The existing Windows inverse-platform EditMode skip and ten existing PlayMode restrictions remain. Exact names/reasons/counts/durations are in [report-summary.json](report-summary.json), [baseline-editmode.json](baseline-editmode.json) and [baseline-playmode.json](baseline-playmode.json). All XMLs, including unsuccessful intermediate runs, are retained in [qualification-reports.zip](qualification-reports.zip).

## Exact invocations

PowerShell executable paths: Unity `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; Python `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Below P is the isolated project above and R is `C:/Dev/Dungeon-Lord`.

```text
unity test P --mode EditMode --filter CombatC0 --output R/TestResults/combat-c0-integration-focused-final-source.xml --timeout 1200 --no-color -- -logFile R/Temp/combat-c0-integration-focused-final-source.log
unity test P --mode EditMode --filter "PhaseFiveB;PhaseSevenA3;LivingDungeonPhaseA;LivingDungeonPhaseB1;LivingDungeonPhaseB2;VisualRunReplay;ProductionSpatialContentBuildGate" --output R/TestResults/combat-c0-integration-regression-final.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-integration-regression-final.log
unity test P --mode EditMode --output R/TestResults/combat-c0-integration-full-editmode.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-integration-full-editmode.log
unity test P --mode EditMode --filter CombatC0 --output R/TestResults/combat-c0-integration-focused-metadata.xml --timeout 1200 --no-color -- -logFile R/Temp/combat-c0-integration-focused-metadata.log
unity test P --mode PlayMode --output R/TestResults/combat-c0-integration-full-playmode.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-integration-full-playmode.log
unity run P --timeout 1800 --no-color -- -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC0IntegrationWindows -logFile R/Temp/combat-c0-integration-build.log
python Tools/Presentation/qualify_combat_c0_integration.py baseline-prepare
unity run R/Temp/ui-composition-combat-c0-b1-baseline --timeout 1200 --no-color -- -executeMethod DungeonBuilder.M0.EditorTools.CombatC0B1GoldenCapture.CaptureBeforeIntegration -logFile R/Temp/combat-c0-b1-baseline.log
python Tools/Presentation/qualify_combat_c0_integration.py prepare
python Tools/Presentation/qualify_combat_c0_integration.py refresh
python -m unittest test_qualification_inputs.py   (cwd R/Tools/Presentation; outside sandbox)
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint final-focused
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint final-regression
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint full-editmode
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint metadata-qualified
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint full-playmode
python Tools/Presentation/qualify_combat_c0_integration.py checkpoint windows-build
python Tools/Presentation/qualify_combat_c0_integration.py restore-fixtures
python Tools/Presentation/qualify_combat_c0_integration.py audit
python Tools/Presentation/collect_combat_c0_integration_evidence.py
git diff --check main
```

There are **1,292 exact Unity inputs** across Assets, Packages, ProjectSettings and ContentAuthoring. Permitted transformations remain the exact build-config/SaveService save-filename substitutions, JSON formatting and existing text normalization. No exclusion or parser/event/checkpoint limit was relaxed. PlayMode produced two and build produced four known generated settings/asset differences, archived before exact source restoration. Earlier mismatches correctly rejected evolving source and incomplete ZIP importer metadata; complete importer metadata was committed, then its whitespace was normalized and verified by an additional 82-test run. Checkpoints and redacted logs are in [qualification-support.zip](qualification-support.zip).

The preserving workflow backs up established static disposable fixtures before tests, archives their modified versions, restores only those fixtures, and hashes all previously present saves and builds. Twenty-two disposable fixtures were restored; owner saves were never restored or reset. The final audit verified **13,834 previously present files with zero changes**. Final audit output and before/after hashes are retained in the support archive. Prior owner/development artifacts and the pre-integration B1 capture remain in their original directories.

## Work and build

The actual catalog/space-capacity upper-envelope test reports **602,024** charged work units, **32** participants including party and **27** encounters, beneath approval. It includes pessimistic repeated geometry collection and conservative planner/kernel charges, and uses integer packing that ignores fixed structures/corridors. Full run records remain bounded by 32,768 events. Geometry/matrices have structural limits; retained receipts/chronology are O(E). No per-frame or per-attack path search controls outcomes. The original synthetic 64-participant qualification remains separate from ordinary production layouts and mobile performance.

Unity Mono's thread allocation counter returned zero; the fixture explicitly reports it unavailable rather than claiming zero allocation. No measured mobile/phone readiness or exact heap-byte ceiling is qualified.

Windows **StandaloneWindows64 Development**, Bootstrap scene, **Succeeded**, **0 errors / 1 warning**. Report size **181,616,624 bytes**. The warning is the existing Unity services native-symbol upload exception with an empty URI; native symbols were not uploaded. Required artwork and the isolated namespace are verified against the packed output. Complete output hashes/counts/sizes are in [windows-artifact-manifest.json](windows-artifact-manifest.json); raw build reports remain in [build-reports-original.zip](build-reports-original.zip).

Player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-integration-validation/Builds/Development/Windows-combat-c0-integration/Dungeon Lord.exe`. The player was **not launched**. Editor UAT, standalone gameplay/lifecycle parity and qualitative gameplay acceptance are not claimed. Sequential instructions are in [owner-uat.md](owner-uat.md), for use after external review.

## Intermediate findings and qualification limits

Initial compile attempts found a missing namespace, a test referring to an absent historical event field and a nullable-coordinate comparison; they executed no tests and are not passing evidence. Later regression found a PR #236 fixture expecting B1 animation after a new C0 manual run. That fixture now explicitly selects its supported B1 executable, keeps its original assertions, and a separate production-scene test verifies C0. A test measured mana before its setup purchase; the measurement now starts after purchases. Static review found and corrected a dead ranged member admitting a defender that no living actor could engage, with a dedicated regression.

Final reports have zero failing cases and zero C# compiler errors/warnings. Logs may include existing licensing/debugger/native-service messages and expected test diagnostics. Earlier failed reports and available logs remain archived; default Editor.log from the first compile attempts was overwritten by subsequent supported runs, so no complete raw log is claimed for those attempts.

No blocking implementation/static-review finding remains. External review and owner UAT remain pending. Shared party anchor, fixed basic attacks, confirmed-boundary-only production LOS, developer evidence presentation, no durable C0 checkpoint and no detailed evidence after reopen are deliberate C0 limitations. C1 presentation, C2 persistence/recovery/reserve and automatic Living Dungeon activation remain separate.


## 2026-10-15 diagnostics correction addendum

The owner's first Editor UAT loaded both existing floors and completed reciprocal C0 `run-68`. It exposed numeric-coordinate formatting and diagnostics overflow defects. The focused correction, new tests, later qualification attempts and final results are recorded separately in [diagnostics-readability.md](../diagnostics-readability.md). Earlier integration reports/artifacts above remain historical and are not overwritten. PR #238 remains draft/unmerged; external re-review precedes further owner UAT. No C1 replay or C2 work is included.
