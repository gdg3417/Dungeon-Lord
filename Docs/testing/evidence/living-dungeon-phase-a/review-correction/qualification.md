# PR #233 nonspatial loot boundary correction

Reviewed head: `c210b46eac1777d9c1bd319a3d9d58b7a12950b2`. Clean branch `codex/living-dungeon-phase-a`; local main, fetched origin/main and live GitHub main remain `a9a9c8e98a0360c1b01224044eb78015200e4a40`. Existing PR remains open and unmerged. Review thread: `PRRT_kwDOQxUKE86rpsiy`.

Recommendation: GPT-6 Luna, Medium, Routine. This is one isolated missing boundary with approved semantics; no gameplay formula change. Availability fallback: GPT-5.6 Luna Medium, then GPT-5.6 Terra Medium; no fallback selected. AGENTS.md, model policy, owner lock, Phase A contract, PR diff, review thread and the surrounding route/continuation/interaction/test contracts were reviewed.

The nonspatial required-room assignment loop now yields the existing `InteractionStep(room, assignment, null)` for loot nodes after the wipe guard. Existing category/Sequence/ordinal AssignmentId traversal and RunId/event ordinal stamping are retained. Inspection increments reached-loot count at that boundary. No encounter, reward roll, loot award or shared-state publication occurs there. Qualified reward generation remains at RoomComplete. Spatial traversal and aggregate-only SimulateOnce remain unchanged. The existing assignment-cardinality workload ceiling already includes these boundaries.

Five new EditMode cases cover pause-before/reach-after counts, no HP/loot change at the interaction, one boundary per repeated-option identity before completion, reward generation at completion, complete event and outcome determinism at budgets 1/2/7, and no false interaction after wipe. One new PlayMode coroutine holds a frame pause immediately before the interaction and verifies count stability. Frozen pre-refactor fixtures are not regenerated: SHA-256 `53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198`.

Qualification uses the existing disposable project `Temp/ui-composition-floorhud-validation`, existing Library and save namespace `living-dungeon-phase-a-validation.json`. A distinct `Windows-living-dungeon-phase-a-review` output preserves the original Phase A player and all earlier players. Strict input checking includes all four Unity input roots; generated differences are archived before refreshing inputs. Owner primary project and saves are not used as test fixtures. No caches, saves or prior players are deleted/reset.

Commands (Unity CLI `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; Python is the bundled runtime):

```text
python Tools/Simulation/phase_a_review_qualification.py audit
python Tools/Simulation/phase_a_review_qualification.py prepare --name focused
unity test <isolated-project> --mode EditMode --filter LivingDungeonPhaseA --output <repo>/TestResults/phase-a-review-focused-editmode.xml --timeout 600 --no-color
unity test <isolated-project> --mode PlayMode --filter LivingDungeonPhaseAPlayModeTests --output <repo>/TestResults/phase-a-review-focused-playmode.xml --timeout 600 --no-color
python -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py
python -m py_compile Tools/Simulation/phase_a_review_qualification.py
unity test <isolated-project> --mode EditMode --output <repo>/TestResults/phase-a-review-full-editmode.xml --timeout 1800 --no-color
unity test <isolated-project> --mode PlayMode --output <repo>/TestResults/phase-a-review-full-playmode.xml --timeout 1800 --no-color
unity run <isolated-project> --timeout 1800 --no-color -- -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonPhaseAReviewWindows -logFile <repo>/Temp/phase-a-review-build.log
python Tools/Simulation/phase_a_review_qualification.py collect
git diff --check
```

Full suites/build are each preceded by `prepare --name <stage>` and followed by archived logs/input checks. A strict check after focused PlayMode found only Unity reordering unchanged Android/Standalone application-ID mapping entries in disposable ProjectSettings. An initial full EditMode launch overlapped that check; its report is retained separately and the final full EditMode rerun starts from exact refreshed inputs. The checker is not weakened. Primary settings remain unchanged.

## Actual qualification

| Check | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Focused EditMode, including frozen differential | 42 | 42 | 0 | 0 |
| Focused PlayMode | 2 | 2 | 0 | 0 |
| Initial full EditMode, retained | 1,747 | 1,746 | 0 | 1 |
| Final exact-input full EditMode | 1,747 | 1,746 | 0 | 1 |
| Full PlayMode | 3,180 | 3,170 | 0 | 10 |
| Production spatial build gates (within full EditMode) | 65 | 65 | 0 | 0 |
| Strict-input Python tests | 12 | 12 | 0 | 0 |

All **38 frozen structured scenarios** remain equivalent. Original fixture content/hash is unchanged and no capture/regeneration operation was invoked. Comparison with the original Phase A qualification finds **zero removed tests and zero changed existing results**, with five new EditMode cases and six new PlayMode cases; all established skips remain. [Exact test outcome comparison](test-outcome-comparison.json), [reports and skip reasons](report-summary.json), [frozen evidence](baseline.json).

Windows **StandaloneWindows64 Development**, Unity **6000.3.2f1**, Bootstrap: **Succeeded, zero errors, one warning**. The warning is a Unity services native-symbol upload exception (`Invalid URI: The URI is empty`), not a C# compile error. All **52** referenced art assets are packed. The new output contains **303 files / 181,890,041 bytes**, including reports; BuildReport totalBuildSize is **181,418,176 bytes**. Player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-living-dungeon-phase-a-review/Dungeon Lord.exe`. [Build report and file hashes](windows-build.json).

Six archived qualification/build logs have **zero C# compiler errors and warnings**. Python syntax checks and `git diff --check` passed; Git emitted only normal LF-to-CRLF checkout notices. No test/build run was blocked. The sandboxed initial Git fetch/live-ref reads failed on Git-write/network permissions and succeeded after authorized host access. The strict input check rejection described above was addressed by archiving/restoring the disposable settings and rerunning full EditMode; it is retained in input evidence.

Final strict comparison verifies **1,228 exact source inputs/metadata** with the established save-namespace substitutions. Hash/inventory audit confirms all **2,131** protected owner save/settings and seven prior-player files are unchanged, including the original Phase A build. This is a scoped file audit, not a whole-registry claim. [Input checkpoints](input-checkpoints.zip), [preservation audit](owner-preservation.json), [redacted logs](unity-logs-redacted.zip), [static results](static-checks.json), [production gates](production-gates.json).

Self-review confirms the added event is one bounded step per already-validated assignment, using existing ordering/stamping and an already-sufficient work ceiling. It performs no encounter/reward resolver call, retains no new event history, and adds no spatial/timing/settlement authority. No reliable allocation-byte claim is made. Schema 13, migrations, production timing, localization, formulas, implementation sequence and deferred Phase B/C scope remain unchanged. No broader regression was detected. [Exact correction file list](changed-files.json).

The identified review concern is covered by the new boundary/counter/ordering/wipe tests and frozen equivalence. Resolve its thread only after the qualified correction is pushed. Independent review and owner Unity Editor/Windows qualification remain required; no new automated blocker was found and no merge-readiness claim is made. Owner checks should include unchanged manual completed runs, localized results, one history/sequence/objective update, two-stage Heat, branches/floors and isolated save/reload behavior. Persistent parties, arrivals, offline scheduling and graphical activity are not implemented.
