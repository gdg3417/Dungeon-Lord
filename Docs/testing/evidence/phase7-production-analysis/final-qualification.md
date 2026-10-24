# PR #249 final qualification

Status: **blocked for independent review of campaign preparation failures**. The complete EditMode invocation finished with seven failures. No final pass or owner acceptance is claimed; subsequent gates were not launched.

## Source and baseline reconciliation

Approved qualification source: `d4071924993c67f2b22722a539be37eea781017e`, branch `feature/phase7-production-analysis`. Its Unity inputs are identical to previously tested implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. Original implementation baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`.

Current main was fetched and verified as `fbe4bdff0cba9fea72b42fb6a05714327f880df4`. The owner explicitly approved qualification without incorporating #250: that merge adds only `Docs/testing/foreground-performance-architecture-audit.md`. It changes no Unity inputs or existing qualification contracts. No merge, rebase or cherry-pick was performed. Historical source identities remain unchanged.

Standard task; recommended configuration GPT-6.1 Sol Medium, with GPT-6 Sol Medium then GPT-5.6 Sol Medium availability fallback. No fallback was used.

## Campaign and preservation plan

Fresh output root: `TestResults/phase7-analysis-final-20261023-01`. Fresh disposable project: `Temp/ui-composition-analysis-final-20261022-01`. Save namespace: `p7-analysis-final-20261022-01.json`. These are unique campaign identities, not historical outputs. The established `qualification_inputs.py` helper prepares all four Unity input roots and substitutes only copied config/SaveService save filenames. Every input, including metadata, is inventoried.

Before any Unity operation, all original player persistent files are backed up with verified SHA-256, including Unity's default persistent-data `TestResults.xml`. Prior builds, reports and committed evidence are separately hashed. Generated changes must be archived and provenance checked after process exit before restoration. Save/recovery files are never replaced merely to obtain a passing result.

The #248 supervisor limitations were read. Frozen #245/#247 adapters are not reopened or source-substituted. Direct bounded established Unity CLI invocations are used because this fresh campaign has no independently reviewed supervisor plan with exact discovery counts. Each suite gets one complete invocation, a fresh report/log identity and a finite timeout. Existing processes are inspected before launching.

## Qualification scope and performance decision

Run the complete EditMode and PlayMode suites against the same source, retaining all tests and named platform skips. This includes content/build, localization, stable-ID, migration/save/recovery, Analysis/drafts/replay/foreground and responsive UI coverage. No schema, migration, checkpoint, history-writer or simulation input differs from the original baseline.

The supervisor contract explicitly states that existing full-suite methods contain sustained 68-run workloads and may not be split or silently excluded. Those workloads are selected only by the owner's explicit requirement for full suites. No separate historical sustained Windows comparison campaign is automatically selected.

The #250 audit sections 10–13 are advisory measurement guidance. Existing cache/source checks and bounded UI/foreground tests are assessed first. Record supported timing/allocation counters with source/workload attribution; overlapping inclusive scopes are not independent percentages. Unsupported allocation/origin/self-time information remains unavailable. Desktop results do not establish phone performance. No instrumentation, optimization, reduced validation or modified workload is authorized here.

After automated gates pass, prepare a fresh Windows x86_64 Development build through the established production build authority, seal artifacts and run isolated native continuation/reopen checks. Prepare owner fixtures only after required technical gates pass. Owner Moonlight acceptance remains a separate manual gate; no Editor capture or synthetic touch counts as physical phone acceptance. PR #249 remains open, draft and unmerged.

## Actual execution and failure classification

Unity 6000.3.2f1, Unity CLI 1.0.0-beta.14. The CLI skill required checking the latest version; the installed beta.13 was updated to beta.14 before launch. Existing beta.13 evidence is not relabeled. Tool hashes are retained with the campaign.

Preflight inventoried/backed up 60 persistent files, hashed 25,483 historical build/report/evidence files, and verified all 1,418 copied Unity inputs. No Editor/player was active apart from Hub's service. The full EditMode invocation selected every test, with no filter or exclusions and a 10,800-second timeout.

| Selection | Exact source | Passed | Failed | Skipped | Total |
| --- | --- | ---: | ---: | ---: | ---: |
| Complete EditMode | `d4071924993c67f2b22722a539be37eea781017e` | 2,345 | 7 | 1 | 2,353 |

Report: `TestResults/phase7-analysis-final-20261023-01/01-full-edit.xml`; log: `01-full-edit.log`; CLI response: `01-full-edit-command.json`; source/timing/exit record: `01-full-edit-exit.json`. All 2,353 unique NUnit leaf IDs match the root counts; zero inconclusive cases. The only skip is `PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`, reason `gd66.test.windows_only_inverse`.

The CLI invocation ran 2026-10-22 23:02:05.522–23:34:11.529 UTC (32m06s including import/startup/shutdown). Domain reload resets the NUnit root duration; its 6.379-second value is not total suite wall time. The CLI recorded `TESTS_FAILED`, exit 8, and Unity process exit 2. Additional licensing messages in the launcher output do not replace the complete failing NUnit verdict.

All seven failures are retained, with full names/stacks in the machine-readable summary:

1. `LivingDungeonTwoFloor.HistoricalGoldenRestoresAndReencodesExactPinnedBytes("historical-b1-format1.json",1)` and the corresponding `historical-c2-format2.json` case: the disposable omitted `Tools/Presentation/Fixtures`. The four-root helper covers Unity inputs but does not copy these external full-suite fixtures. Both fail with `DirectoryNotFoundException`.
2. `PhaseSevenA4ProductionScene.ForegroundPerformanceSustainedProduction68Runs`, `LivingDungeonTwoFloorProductionProfileSceneTests.LivingDungeonSingleFloorEditorProductionProfile`, and `.LivingDungeonTwoFloorEditorProductionProfile`: `b3b2.profile.isolation_required`. The copied project retained the production product identity rather than the opt-in qualification identity required by these full-suite methods.
3. `LivingDungeonTwoFloorProductionProfileSceneTests.LivingDungeonTwoFloorWaitingSelectedFloorLayoutsPreserveCanonicalState`: `two_floor.smoke.isolation_required`, from the same incomplete product isolation preparation.
4. `PhaseSevenA4ProductionScene.OwnerUsabilityConstructionAndContentReachability`: its non-overwriting copy refused the pre-existing `owner-uat-starter.json` in the production persistent-data directory. That original file remains unchanged.

These are campaign-preparation/fixture prerequisites, not demonstrated production Analysis defects. The preflight should have accounted for them before selecting the full suite. No test/guard was weakened, production correction made, retry launched or failure silently omitted. The owner required stopping for independent review when actual qualification failure requires it; later stages therefore remain gated. A reviewed replacement preparation must include hash-pinned external fixtures, the established opt-in isolated product and aligned identity checks, plus unused owner-starter/native/report paths. It must not overwrite owner files or relax the isolation guards.

## Passing coverage and performance limits

All 21 selected Analysis scene/presenter cases inside the full run passed, including phone touch/draft/replay, unavailable localization, independent aggregate denominators, foreground admission/settlement and reopen byte preservation. All 65 production spatial content build gates passed. No C# compiler errors were found in the final log; the complete suite imported and executed.

The existing bounded tests `CompositionLayoutsAndPresentationWorkRemainBounded`, `LivingDungeonB3B2ActiveIdleFramesDoNotReconstructSaveOrRebuildUi` and `LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish` all passed. The latter reports 120 cached pump gates in 0.08 ms with 0 bytes on its test thread. The active-idle test retains its 120-frame zero Stage/Restore/Inspect/AtomicWrite/Publication/Present/WorldRender assertions. These are bounded Editor checks, not an end-to-end Windows or mobile performance score. `PresentAnalysis` still captures only on retained-array publication/reference changes; no per-frame complete-save validation or new reporting owner was added.

The three sustained profiles rejected product isolation before completing their workloads; none receives a passing performance verdict. No separate historical 68-run campaign was launched. No instrumentation recommendations from #250 were implemented, and no speedup or unavailable exclusive/allocation measurement is claimed.

## Preservation closeout

The qualification Editor and import workers exited before restoration; only Hub's background service remained. All original save/recovery bytes were unchanged before restoration. The only changed original persistent file was Unity's default `TestResults.xml`; its generated version was archived, then the exact SHA-verified pre-run backup restored. Its original SHA-256 is `f9eccd6bb418bc6f4f03b71dd0272384262aa088d59675e7c99fd9d6362efc6e`.

Three new GUID-named two-floor qualification saves remained after failed guarded scenarios. Each was absent from preflight and mapped uniquely to a manifest created in this disposable campaign. After checking resolved paths, hashes and manifest provenance, they were moved intact to `generated-qualification-saves` under the fresh output root. No original save/recovery file was restored, deleted, moved or replaced. The persistent directory now contains exactly the original 60 files, all byte-identical.

Unity also reserialized the disposable TMP fallback font. Its generated version was archived; the original source SHA was checked against the preflight inventory and its bytes restored after exit. All 1,418 disposable inputs then match, with no extras or mismatches. Original source Unity inputs are unchanged.

The historical comparison checked all 25,483 inventory rows. Its raw report retains one difference: this newly authored `final-qualification.md`, drafted while the preflight enumeration was still running and then intentionally updated with results. It did not exist at the approved source and is current campaign output, not historical evidence. All 25,482 pre-existing build/report/evidence files are byte-identical. The raw difference is retained and explicitly classified rather than silently erased or counted as an unchanged historical file.

See [exact source, report and preservation hashes](final-source-applicability.json) and [complete failed-run XML and applicability packet](final-attempt-reports.zip). Raw Unity logs, private backups, generated font/save evidence and complete historical inventories remain in the fresh local output directory.

## Remaining gates and owner handoff

Full PlayMode, a passing complete EditMode qualification, sustained methods currently blocked by isolation, Windows Development build/content packing/startup, native continuation/reopen and owner fixtures remain outstanding. No new Windows build or owner launcher was produced, and no Moonlight session was launched. Sequential owner instructions and fixture identities must be supplied only after those technical gates pass; there is currently no qualified owner build to launch. Recommendation: **blocked**, pending independent review of corrected campaign preparation.
