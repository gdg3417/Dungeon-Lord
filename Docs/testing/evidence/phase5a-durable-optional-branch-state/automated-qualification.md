# Phase 5A automated qualification

## Provenance rule

A result is authoritative only when no relevant production, test, or configuration source changed after it. Results from the interrupted run that were followed by source edits are recorded as superseded or failed, not reused as final qualification.

## Interrupted-run reconstruction

- Initial continuation HEAD: `ad026a29b1f8020a7ab8c682ac3c98da1ebf341c`.
- Phase 5A work was entirely uncommitted on `codex/phase5a-durable-optional-branch-state`.
- Earlier full EditMode attempts progressed from 414/946 to 479/946 to 902/946 to 940/946. Those invocations were superseded by later corrections; the last had six genuine failures that were corrected before continuation.
- A subsequent invocation was interrupted at compile time by a sealed-fixture inheritance error; the fixture was corrected before continuation.
- Known unrelated modified files were preserved: `ProjectSettings/UnityConnectSettings.asset` and `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`.

## Current focused results

| Suite | Result | Evidence |
|---|---:|---|
| Phase 5A durable branch fixture | 6/6 passed | `TestResults/phase5a-focused-current.xml` |
| Atomic canonical writes | 59/59 passed | `TestResults/phase5a-writes-current.xml` |
| Strict complete-save contract | 10/10 passed | `TestResults/phase5a-complete-save-current.xml` |
| GameRoot/bootstrap structural integration | 28/28 passed | `TestResults/phase5a-bootstrap-current.xml` |
| Structural economy | 70/70 passed | `TestResults/phase5a-economy-current.xml` |
| Content acquisition economy | 58/58 passed | `TestResults/phase5a-acquisition-current.xml` |
| Returned-content redeployment | 23/23 passed | `TestResults/phase5a-redeployment-current.xml` |
| Direct content unassignment | 31/31 passed | `TestResults/phase5a-unassignment-current.xml` |
| Canonical load/migration coordinator | 52/52 passed | `TestResults/phase5a-load-current.xml` |
| Save workload accounting | 14/14 passed | `TestResults/phase5a-focused-workload.xml` |
| Structural/spatial validation | 76/76 passed | `TestResults/phase5a-focused-spatial-rerun.xml` |

The Phase 5A fixture covers schema 9 → 10, research allowance resolution, construction cost and identity, corridor acquisition/custody/redeployment/unassignment, deletion blocking, reopen, one-tile occupancy conflict, topology fingerprint applicability, and unchanged required-route outcomes with branch content present.

## Final qualification

The final authoritative qualification was run against implementation HEAD
`6d400c359970c09a002533eeeefdcc74414637a3` under Unity `6000.3.2f1`.

| Qualification | Result | Duration / output |
|---|---:|---|
| Full EditMode | 952 total; 952 passed; 0 failed; 0 skipped | 120.412 seconds |
| Full PlayMode | 2,452 total; 2,442 passed; 0 failed; 10 intentionally skipped | 115.118 seconds |
| Windows x86_64 Development Build | Success; exit code 0 | `Builds/Phase5A/DungeonLord.exe` |

### PlayMode discovery correction

The first full PlayMode invocation discovered two stale test-only reason-range assertions:

- `FloorLayoutValidatorTests.ReasonCodeValuesRemainStableAndAppendExactlyFortyThroughFortySix`
- `SpatialContentValidationTests.ExactContentAndGd64ReasonMapsArePreserved`

Both still expected the pre-Phase-5 append range `1..46`, while Phase 5A correctly
appends stable reasons `47..55`. Commit
`6d400c359970c09a002533eeeefdcc74414637a3` updated those assertions without
changing production behavior. Each corrected assertion passed in isolation, the
full EditMode suite then passed 952/952, and the full PlayMode rerun passed with
zero failures.

The ten final PlayMode skips are expected existing fixture/platform boundaries:

| Skipped test | Existing reason |
|---|---|
| `Gd66GameRootBootIntegrationTests.BootstrapDeletionPresentationLocalizesReturnedRemovedAndAllBlockingContentWithoutRawIds` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.BootstrapRenovationPresentationDisclosesLocalizedMovementReplacementAndCapacityConsequences` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralDeletionMissingRuntimePolicyFailsClosedThroughRealRoot` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(6,"north",DirectDoorway,"Direct Doorway","")` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(7,"east",PhysicalCorridor,"Straight Stone Corridor","(1,6)")` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(False)` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(True)` | `gd66.test.synchronous_edit_mode_fixture` |
| `Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed` | `gd66.test.windows_only_inverse` |
| `Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification` | `gd66.test.windows_player_only` |

### Windows Development Build

- Editor: Unity `6000.3.2f1`, x86_64.
- Target: `StandaloneWindows64`.
- Build option: `-development`.
- Result: `Success`, exit code `0`.
- Output: `Builds/Phase5A/DungeonLord.exe`.
- Complete build size: 99.7 MB.
- Output inventory: 217 files, 104,807,832 bytes.
- Provenance: `Builds/Phase5A/DungeonLord.provenance.json`.
- Provenance source revision: `6d400c359970c09a002533eeeefdcc74414637a3`.
- Provenance reports `dirty: true` only because excluded local/generated Unity and
  editor files were present in the working tree; none is part of the Phase 5A
  committed diff.

Observed nonfatal diagnostics were unavailable Unity Cloud credentials for
symbol upload, a player-connection multicast permission warning, an empty test
asmdef notice, and shutdown thread/debugger cleanup messages. The build reported
no compile failure, build failure, ComputeBuffer failure, or new player runtime
error.

## Evidence ownership and outstanding validation

This committed document records the qualification conclusions. The source XML
and build log under untracked `TestResults/` remain raw local artifacts and are
not committed. The player and provenance under ignored `Builds/Phase5A/` remain
local build output and are not committed.

Manual UAT remains outstanding and is tracked in `manual-uat.md`. No manual UAT
pass is claimed by Phase 5A automated evidence.

## Post-review correction qualification

External review corrections were qualified against the exact production, test,
configuration, and asset state committed as
`74f797320d2a21969050171e13422774fc90fc64`. No relevant source changed between
the final focused pass, the full suites, the Windows build, and that commit.

The correction set:

- restores the frozen schema 7/8/9 node-kind domain to historical values 1–5
  while schema 10 and later use the current runtime enum domain;
- supplies the authoritative Architecture research exports to `GameRoot` as
  serialized `TextAsset` dependencies from the single relocated production
  asset tree, with no runtime filesystem lookup or duplicated research value;
- attaches structural-economy consequences and affordability to optional-branch
  construction and removal previews through `StructuralEconomyService`;
- verifies that branch removal deletes its matching live-branch knowledge record
  in the same detached atomic mutation.

### Focused and affected suites

| Suite | Result | Duration | Raw local evidence |
|---|---:|---:|---|
| Explicit authoritative research asset source | 1/1 passed | 0.063 seconds | `TestResults/phase5a-research-source.xml` |
| Phase 5A review fixture | 10/10 passed | 0.903 seconds | `TestResults/phase5a-review-fixes.xml` |
| Complete save and migration | 171/171 passed | 5.553 seconds | focused save/migration result set |
| Structural economy | 70/70 passed | 3.686 seconds | focused economy result set |
| GameRoot/bootstrap and production content | 65/65 passed | 2.755 seconds | focused bootstrap/content result set |
| Workload, spatial, and compatibility | 149/149 passed | 86.588 seconds | focused workload/spatial result set |
| Final Phase 5A plus save/migration rerun after the schema-boundary refinement | 181/181 passed | 6.340 seconds | `TestResults/phase5a-review-focused-save-final.xml` |

The final 181-test pass directly covers rejection of `DeadEnd = 6` in schemas
7, 8, and 9, rejection during migration of malformed frozen input carrying that
value, and acceptance of valid schema-10 optional-branch topology. The economy
coverage includes affordable, exact-balance, and insufficient-mana construction;
side-effect-free failed preview; removal based on recorded investment rather
than catalog repricing; configured refund flooring; and wallet-capacity-limited
credit. Research bootstrap coverage loads the relocated canonical JSON assets as
Unity `TextAsset` objects, parses `ac_300` and its authored effect, and confirms
missing inputs fail closed.

### Full suites after review corrections

The full EditMode run launched before the Codex usage-window interruption did
complete successfully. Its XML result was recovered afterward and accepted
without rerunning because timestamps and the working-tree audit proved that no
production, configuration, asset, or test source changed after the authoritative
181/181 focused pass or before/after the full result was written.

| Qualification | Result | Duration / output |
|---|---:|---|
| Full EditMode | 957 total; 957 passed; 0 failed; 0 skipped | 121.035 seconds; `TestResults/phase5a-review-final-editmode.xml` |
| Full PlayMode | 2,457 total; 2,447 passed; 0 failed; 10 intentionally skipped | 121.269 seconds; `TestResults/phase5a-review-final-playmode.xml` |
| Windows x86_64 Development Build | Success; exit code 0 | `Builds/Phase5A/DungeonLord.exe` |

The ten PlayMode skips are the same expected existing fixture/platform boundaries
listed above: eight synchronous EditMode-only `Gd66GameRootBootIntegrationTests`,
the non-Windows inverse filesystem test, and the Windows-player-only standalone
qualification test. No Phase 5A test was skipped.

### Post-review Windows Development Build

- Editor: Unity `6000.3.2f1`, x86_64.
- Target: `StandaloneWindows64`.
- Build option: `-development`.
- Result: `Success`, exit code `0`.
- Output: `Builds/Phase5A/DungeonLord.exe`.
- Complete build size reported by Unity: 99.7 MB.
- Build payload: 182 files, 104,739,983 bytes, excluding provenance.
- Local output including provenance: 183 files, 104,743,643 bytes.
- Provenance: `Builds/Phase5A/DungeonLord.provenance.json`.

The build was intentionally produced from the final corrected but then-uncommitted
working tree. Its provenance therefore names the prior reviewed revision
`751355ed68625a6006af259f5b4d62acdfde9bf4` and reports `dirty: true`. The
post-build audit established that the qualified corrections were committed
unchanged as `74f797320d2a21969050171e13422774fc90fc64`; the remaining dirty files are
excluded Unity/editor-generated state and raw local evidence.

Observed nonfatal diagnostics were an unavailable Unity Cloud credential/symbol
upload, an empty `Assets/_Project/Tests 1/Tests 1.asmdef` notice, and shutdown
thread/debugger-agent cleanup messages. There was no compile failure, build
failure, ComputeBuffer failure, or new player runtime error. A supplemental
Android build was not run, at owner direction, and is not required for PR #209.

The XML/log artifacts under untracked `TestResults/` and the ignored
`Builds/Phase5A/` output remain local and are not committed. Manual UAT remains
outstanding; this post-review automated qualification does not claim it passed.

## Owner-UAT presentation correction qualification

Owner UAT found two presentation-only issues after the verified Phase 5A
research/preview/construction checks: raw double-precision wallet text in the
branch economy preview and singular `1 tiles` grammar. The correction does not
modify save state, economy calculations, passive generation, research, branch
rules, or Phase 5B behavior.

`StructuralEconomyPresenter` now owns the reusable display policy. Transaction
amounts use at most one decimal; balances use at most one decimal below 3,600
authoritatively resolved mana/hour and whole mana at or above that rate, except
when the preview contains a meaningful fractional transaction. `GameRoot`
supplies that existing canonical passive-rate result directly to presentation;
no passive-mana formula was duplicated. Bootstrap uses localized singular and
plural branch-summary keys.

| Qualification | Result | Duration / output |
|---|---:|---|
| Focused structural economy/presentation | 77 total; 77 passed; 0 failed; 0 skipped | 3.867 seconds; `TestResults/phase5a-mana-presentation-structural-economy.xml` |
| Focused Phase 5A branch fixture | 10 total; 10 passed; 0 failed; 0 skipped | 0.948 seconds; `TestResults/phase5a-mana-presentation-branch.xml` |
| Focused Bootstrap/production-localization | 37 total; 37 passed; 0 failed; 0 skipped | 0.382 seconds; `TestResults/phase5a-mana-presentation-bootstrap.xml` |
| Full EditMode | 964 total; 964 passed; 0 failed; 0 skipped | 123.272 seconds; `TestResults/phase5a-mana-presentation-final-editmode.xml` |
| Full PlayMode | 2,464 total; 2,454 passed; 0 failed; 10 intentionally skipped | 114.234 seconds; `TestResults/phase5a-mana-presentation-final-playmode.xml` |
| Windows x86_64 Development Build | Success; exit code 0 | `Builds/Phase5A/DungeonLord.exe` |

The ten PlayMode skips remain the established existing boundaries: eight
`gd66.test.synchronous_edit_mode_fixture` tests, one
`gd66.test.windows_only_inverse` test, and one
`gd66.test.windows_player_only` test. No Phase 5A presentation test was
skipped.

The final Windows build used Unity `6000.3.2f1`, target
`StandaloneWindows64`, and `-development`. The known nonfatal empty-test-asmdef,
shutdown thread/debugger-agent, and player-connection diagnostics remained;
there was no compile failure, build failure, ComputeBuffer failure, or new
player runtime error. Android qualification was not run and is not required.

Raw XML/log artifacts remain untracked under `TestResults/`; build output and
provenance remain ignored under `Builds/Phase5A/`. Owner manual UAT is partially
recorded in `manual-uat.md` and remains outstanding.

## Final player-facing mana presentation consistency correction

The final presentation-only correction was qualified from the working tree based
on `2e88dde6f8e110f758f167d10a914fac4ca1c6fb`. It changes no save, economy,
passive-generation, research, or route-choice behavior. A shared
`PlayerManaPresentationFormatter` now supplies locale-aware player-facing mana
text: discrete values use at most one decimal with no trailing `.0`; a live
wallet uses whole mana at an authoritative passive rate of 3,600 mana/hour or
above and at most one decimal below that threshold. Structural-economy preview
presentation continues to retain its approved fractional-transaction exception.

| Focused qualification | Result | Duration | Raw local evidence |
|---|---:|---:|---|
| Passive online mana and localization | 55/55 passed | 1.207 seconds | `TestResults/phase5a-mana-presentation-passive.xml` |
| Offline passive mana and localization | 33/33 passed | 1.407 seconds | `TestResults/phase5a-mana-presentation-offline.xml` |
| Structural economy preview presentation | 77/77 passed | 3.891 seconds | `TestResults/phase5a-mana-presentation-structural.xml` |

The focused fixtures cover below-threshold, exactly-3,600, and above-threshold
live balances; rate, contribution, award, current, and capacity formatting;
whole-value suppression of `.0`; locale-aware decimal separators; non-leakage
of raw floating-point tails; and unchanged underlying authoritative values.

### Final Windows Development Build

- Editor: Unity `6000.3.2f1`, x86_64.
- Target: `StandaloneWindows64`; option: `-development`.
- Result: `Success`, exit code `0`.
- Output: `Builds/Phase5A/DungeonLord.exe`.
- Provenance: `Builds/Phase5A/DungeonLord.provenance.json` records source
  revision `2e88dde6f8e110f758f167d10a914fac4ca1c6fb` and `dirty: true` because
  this final correction and excluded local/generated Unity/editor files were
  present before commit.

The known nonfatal Unity Cloud credential, player-connection multicast,
empty-test-asmdef, and shutdown/debugger cleanup diagnostics were observed. The
build reported no compile failure, build failure, ComputeBuffer failure, or new
player runtime error. Android was not run at owner direction and is not required
for PR #209. Raw XML/log artifacts remain untracked under `TestResults/`, and
build output/provenance remain ignored under `Builds/Phase5A/`. Manual UAT is
still outstanding.

## Final owner-UAT and canonical Windows Development Build closeout

The generic-wrapper artifact at `Builds/Phase5A-Final/DungeonLord.exe` is
rejected for final UAT. It was created by a generic `-buildWindows64Player`
workflow with a `-development` argument, not by the repository's
`DevelopmentBuildUtility.BuildWindowsDevelopment` path. Its provenance recorded
the source revision but could not establish use of `BuildOptions.Development`;
the owner observed no F1 Dev Panel or Optional Branch QA controls. This is a
build-procedure issue, not a Phase 5A gameplay defect.

The authoritative accepted standalone player was rebuilt from
`7f7dc2cbc040a3485fbcbf0067d6caeb1ac4660f` using
`DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment`.
The canonical output is `Builds/Development/Windows/Dungeon Lord.exe`.
`Builds/Development/Windows/build-report.json` confirms
`developmentBuild: true`, `targetPlatform: StandaloneWindows64`,
`buildResult: Succeeded`, `errorCount: 0`, and Bootstrap as the included scene.
Its provenance records the exact source SHA, canonical execute method, Unity
`6000.3.2f1`, success, and exit code 0; `dirty: true` is limited to excluded
local Unity/editor-generated files, not Phase 5A compiled/runtime source.

No production, test, configuration, asset, or localization source changed after
the final focused correction qualification. No additional automated suite was
run for this documentation closeout. The latest valid
automated results remain: focused final correction **165/165 passed**;
preceding full EditMode **964/964 passed**; preceding full PlayMode **2,454/2,464
passed, 0 failed, 10 expected skips**.

Owner Editor UAT and owner canonical Windows standalone UAT both **PASSED** on
the final production head. The owner verified the required Phase 5A gate,
construction, content/custody/removal, persistence, lifecycle, required-route
regression, and presentation behavior, including 1920×1080 and 1280×720
usability. No Phase 5B behavior was expected or observed. The Systems
Diagnostics readability remains cramped but usable and is accepted as
non-blocking QA debt. Android was intentionally not run and is not required.
