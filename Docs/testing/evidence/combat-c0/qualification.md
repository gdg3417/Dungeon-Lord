# Historical C0 prerequisite qualification

The approved PR #238 continuation implements production C0. Current integrated qualification is recorded in [integration/qualification.md](integration/qualification.md). The unchanged reports below qualify only the earlier prerequisite source; their former approval blocker is closed by the dated owner addendum.

# Combat C0 prerequisite qualification

Historical status at c0dba584: **Automated prerequisite qualification passed; production C0 remains blocked and this PR is not merge-ready.** See the [unapproved tuning/technical packet](../../../planning/combat-c0-implementation-and-tuning-proposal.md) and [implemented boundary / acceptance gaps](implementation-review.md). No owner Unity gameplay test is requested.

Baseline: `eb80df1e3fd09c18aedb062afc108eb941deb8a3`, merged #237. Branch: `codex/combat-c0-deterministic-combat`. Validated Unity source: `a6c9d6e13013e9fa6993b2f4b094da6139a483e4`; subsequent changes are documentation/evidence only. Final fetch still found no newer main commit.

Host: Windows 11 Professional x64, Unity **6000.3.2f1 (a9779f353c9b)**. CLI **1.0.0-beta.12**. Qualification project `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-validation`; unique save filename `combat-c0-prerequisite-validation-20261014.json`. No owner project or prior disposable project was reset/reused. Original Library, builds, UAT evidence and backups remain in place.

| Executed check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Final focused C0 kernel EditMode | 42 | 0 | 0 |
| Relevant historical/spatial/HP/replay/production-gate EditMode | 404 | 0 | 0 |
| Full EditMode | 1,939 | 0 | 1 existing |
| Full PlayMode | 3,366 | 0 | 10 existing |
| Production spatial build gates (included above, not an extra suite) | 65 | 0 | 0 |
| Established strict-input Python helper tests | 12 | 0 | 0 |

Final focused, regression, full-suite and build logs have zero C# compiler errors/warnings. Original failed attempts remain recorded below and in the report/log archives. Both full-suite comparisons against PR #236's final `visual-feedback` reports show **zero removed tests, zero changed existing results/skip reasons and exactly 42 added passing cases**. Multiplicity of duplicate existing fullnames is retained in comparisons. Frozen A fixtures/projectors and all B1 implementation/encoding remain unchanged; the regression suite runs the existing 38-scenario frozen equivalence and checkpoint cases.

EditMode's existing skip is the Windows inverse-platform test. PlayMode retains the same ten skip occurrences (synchronous EditMode fixture/platform/player preflight restrictions). Exact names/reasons, pass totals and duration attributes are in [report-summary.json](report-summary.json); full XML reports, including the zero-test discovery attempt, are in [qualification-reports.zip](qualification-reports.zip). No test is counted as executed based on a missing/zero report.

## Actual invocations

PowerShell uses `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe` for the following supported CLI invocations. Paths below are abbreviated only by `P = C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-validation` and `R = C:/Dev/Dungeon-Lord`:

```text
unity test P --mode EditMode --filter CombatC0KernelTests --output R/TestResults/combat-c0-focused-editmode-03.xml --timeout 1200 --no-color -- -logFile R/Temp/combat-c0-focused-editmode-03.log
unity test P --mode EditMode --filter "PhaseFiveB;PhaseSevenA3;LivingDungeonPhaseA;LivingDungeonPhaseB1;LivingDungeonPhaseB2;VisualRunReplay;ProductionSpatialContentBuildGate" --output R/TestResults/combat-c0-regression-editmode.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-regression-editmode.log
unity test P --mode EditMode --output R/TestResults/combat-c0-full-editmode.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-full-editmode.log
unity test P --mode PlayMode --output R/TestResults/combat-c0-full-playmode.xml --timeout 1800 --no-color -- -logFile R/Temp/combat-c0-full-playmode.log
unity run P --timeout 1800 --no-color -- -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC0PrerequisiteWindows -logFile R/Temp/combat-c0-build.log
```

Python executable: `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

```text
python Tools/Presentation/qualify_combat_c0.py prepare
python -m unittest test_qualification_inputs.py   (cwd: Tools/Presentation; outside sandbox)
python Tools/Presentation/qualify_combat_c0.py checkpoint focused03
python Tools/Presentation/qualify_combat_c0.py checkpoint regression
python Tools/Presentation/qualify_combat_c0.py checkpoint full-editmode
python Tools/Presentation/qualify_combat_c0.py checkpoint full-playmode
python Tools/Presentation/qualify_combat_c0.py checkpoint build
python Tools/Presentation/qualify_combat_c0.py restore-fixtures
python Tools/Presentation/qualify_combat_c0.py audit
python Tools/Presentation/collect_combat_c0_evidence.py
git diff --check
```

The exact-input workflow prepared and compared **1,276 inputs** across Assets/Packages/ProjectSettings/ContentAuthoring. It permits only the exact config/fallback save-filename substitutions and established JSON/text normalization. Final focused/regression/EditMode checks had zero generated differences. Two PlayMode and four build-generated settings/asset differences were archived before restoring exact source inputs. No unexpected input, source code mismatch or relaxed exclusion was accepted. Input manifests, differences and redacted logs are retained in [qualification-support.zip](qualification-support.zip), with originals under `TestResults/combat-c0` and `Temp/combat-c0-*.log`.

## Earlier attempts and corrections

1. Initial focused invocation wrote `Temp/combat-c0-focused-editmode.log`; Unity aborted on CS0246 because the new test fixture omitted the `Gameplay.Structures` import. No test report or passing result exists for that attempt. The fixture import was corrected.
2. Focused `-02` compiled successfully but executed zero tests. The repository requires an inherited Editor discovery wrapper for runtime-assembly EditMode fixtures; it was added without changing existing tests. The retained zero-test XML is not counted as a pass. Final `-03` discovers and passes all 42 cases.
3. The first Python invocation from repository root failed module discovery. Running from `Tools/Presentation` then hit sandbox Git/temp-fixture permissions (12 attempted tests, 24 setup/cleanup errors). The established command passed 12/12 outside the sandbox in 3.905 seconds. These infrastructure attempts are not gameplay failures and no assertion/limit was changed.
4. Intermediate exact-input checkpoint attempts `compile01`/`discovery02` correctly rejected source differences after the authorized fixture correction/new wrapper. Prepared inputs were refreshed explicitly before the final run. Their manifests are retained; no mismatch was waived.

## Workload and build evidence

The injected kernel envelope uses 5 party members + 59 monsters, 32 rounds, fixed test damage and explicit directed permissions. It resolves **2,640 chronological events**, **4,750 charged work units**, and 2,048 attack actions; the focused observation was **7.4025 ms**. Other exact observed timings are retained in report outputs. This is not real-room LOS, production encounter capacity, allocation-free execution, mobile performance or an approved production envelope. Preparation is O(P×L + P² log P + L log L), rounds O(R×P²), storage O(P+L+E) across retained receipts. No per-frame search or combat loop exists.

Windows **StandaloneWindows64 Development**, Bootstrap only, **Succeeded**, **0 errors / 1 warning**. Report size **181,558,548 bytes**; complete output **303 files / 182,031,357 bytes**. All **52** required art assets and the isolated save namespace are packed. The warning is the existing Unity services native-symbol upload failure with an empty URI. Native symbols were not uploaded; the build succeeded. Player **not launched**, so no standalone gameplay or owner acceptance is claimed.

Player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-validation/Builds/Development/Windows-combat-c0-prerequisite/Dungeon Lord.exe`. Report, messages, packed asset lists and complete hashes are in [windows-artifact-manifest.json](windows-artifact-manifest.json) and adjacent build artifacts. Previous players use distinct paths and were not overwritten.

Unity-generated build stack traces and the built-in sprite list contained trailing spaces, detected during final Git whitespace review. Readable text copies normalize only trailing whitespace/line endings. The exact original build reports/lists remain in [build-reports-original.zip](build-reports-original.zip) and the original player output; manifest hashes describe the original files. No Unity source or validation result changed.

The preserving helper backed up the 22 established disposable test fixtures before Unity execution, retained modified fixture bytes, and restored the original bytes after qualification. Owner primary saves were never reset or restored. The final audit verified **13,531 protected files, zero changed hashes**. Exact hashes are in the support archive's `preservation-after.json`; the audit covers save/settings and prior build files, and excludes evolving Unity log/XML files under LocalLow under the established helper contract. Explicit test reports and qualification evidence here use new filenames and are retained.

## Completion boundary

Implemented/tested: injection-only reciprocal C0 kernel and encounter evidence; historical compatibility and build regression checks. Proposed/unapproved: production numerical tuning, config/policy IDs, positions/grouping, LOS/opacity/range, stop conditions and limits. Blocked: production manual selection, real geometry/route/effect/settlement integration, full-route C0 retention and new C0 gameplay/reopen/standalone tests. Deferred: C1 graphical combat and C2 persistent monster state, protected reserve/recovery. No durable C0 format, save schema/migration, mana grant, automatic arrival or activation was introduced.

An owner gameplay plan must wait until production C0 is enabled after approval and its integration tests/review pass. Kernel success is not an invitation to test unimplemented C0 through the manual-run UI. Keep the same branch/PR for that integration; do not merge this prerequisite state.
