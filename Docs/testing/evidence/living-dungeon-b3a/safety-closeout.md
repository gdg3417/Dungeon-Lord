# PR #242 preservation safety closeout

Status: **tooling correction verified; preservation unresolved; PR remains Draft and must not merge.** No owner exception has been granted. Controlled B3A qualification passed at `b5502291e9da3d229bbcc3d152f8bd56954d84eb`; full EditMode remains non-green. This follow-up does not run Unity, regenerate screenshots, modify gameplay/tests, or replace the original qualification reports/status.

Reviewed head: `36da2f80ef31febbd01e63d8ee5c5ffa2fd38532`. Actual configuration: GPT-6.1 Sol High, Complex well-defined preservation tooling correction; no model fallback or delegation. The source-identity and immutable preservation anchors intentionally reference that reviewed head and the existing qualified runtime commit. Tooling and evidence are the only changes.

Exact follow-up files: `Tools/Presentation/run_b3a_qualification.ps1`, `Tools/Presentation/qualify_b3a.py`, `Tools/Presentation/collect_b3a_evidence.py`, `Tools/Presentation/b3a_preservation_preflight.py`, `Tools/Presentation/test_b3a_preservation_preflight.py`, and this evidence directory's `qualification.md`, `safety-closeout.md`, `safety-closeout.json`, `safety-closeout-reports.zip`. The earlier 30-file inventory is retained as its original snapshot; this follow-up adds five files to the PR and modifies four existing ones.

## Fail-closed launch gate

`run_b3a_qualification.ps1` invokes Python with `-B` before it creates command records or invokes Unity. The preflight performs no source installation, metadata generation, cleanup, evidence writes or Unity launch. A rejection exits the runner. The runner also uses the project working directory and create-new command records.

The gate requires all preparation artifacts, absence of the restoration marker, and original baseline bytes matching the reviewed committed evidence archive. It derives exact prepared SHA-256 identities from the frozen 1,340-input snapshot and verified original-source backups. It checks the build-config and SaveService namespace, all three evidence-output sources (including shared `ProductionDungeonScreenshots`), all three static-fixture source substitutions, and the five explicitly recorded local asset overrides. Missing, untracked, ignored extra, byte-changed or redirected/reparse inputs fail. No newline or JSON normalization masks mismatches.

Every protected save/player and historical-evidence hash must still match. There is no allowlist for the known screenshot loss. Report, log and command-record destinations must be unused; fixed capture and Windows-fixture directories must not exist, even empty. The isolated save/fixture prefixes must be unused. A build requires the entire fixed build destination to be absent, not merely its executable. Preflight never deletes or rotates an existing destination. The historical `baseline-ui` stage is closed and rejected; its prior reproduction remains evidence.

Preparation now diagnoses missing reviewed `.cs.meta` inputs before creating preparation output and never invents GUIDs. The collector rejects any existing or partial generated evidence set, even without its final ZIP.

The original campaign is closed: its restoration marker, consumed namespaces/build destination and unresolved preservation losses prevent another launch. Removing one marker cannot authorize it: all other independent checks must pass. A future campaign requires separately reviewed fresh isolation/destinations and resolution or explicit owner review of preservation blockers. This correction deliberately offers no bypass switch or automatic setup.

## Checks actually executed

1. `python -B Tools/Presentation/test_b3a_preservation_preflight.py`: **24 tests passed**, exit 0. Positive checks exercise all six supported stages using disposable text fixtures, with mocked immutable anchors and synthetic owner/evidence bytes. They are not a Unity project copy. Both successful and rejected checks assert byte-identical filesystem snapshots. Negative cases cover partial/restored preparation, baseline tampering, changed source/commit/overrides, missing metadata, shared/scene screenshots, static fixtures, owner/evidence mismatches, reused report/log/record/capture/save/build destinations, closed baseline stage and partial collector output. Another test verifies immutable-anchor archive loading.
2. Actual inactive invocation: `Set-PSDebug -Trace 1; & Tools/Presentation/run_b3a_qualification.ps1 -Stage focused -Attempt safety-closeout-unused`. Runner **exit 1**; Python preflight exit 2. Diagnostic: `Isolation was restored/inactive. Do not launch Unity. A separately reviewed fresh preparation is required; preserve this closed campaign.` Executed-statement trace ends at the rejection before Unity. Before/after SHA-256 inventories of **11,036 files** record **zero new, removed or changed files**. No new screenshot, save, build, Unity log, NUnit report or runner command record was produced. Only the separate closeout auditor writes its new report after comparison.
3. Preservation audit re-read the immutable original baselines: **6,839/6,839 protected save/player files match**, **1,986/1,991 historical evidence files match**, and **1,340/1,340 restored Unity inputs match**. Existing incident ZIP and all ten replacement captures remain hash-indexed and untouched. The five earlier exact restorations still match their original hashes; none was newly restored in this follow-up.
4. Final source applicability commands (empty output, exit 0): `git diff --name-only b5502291e9da3d229bbcc3d152f8bd56954d84eb HEAD -- Assets Packages ProjectSettings ContentAuthoring` and the working-tree comparison over `Assets/_Project/Scripts`, `Assets/_Project/Editor`, `Assets/_Project/Tests`, `Packages`, `ContentAuthoring`. Raw snapshot hashes additionally verify exact bytes, including preserved owner overrides. No schema, executable/checkpoint contract, gameplay configuration, localization or runtime/test source changed.

The initial sandbox tooling-test attempt failed in setup because Windows denied its temporary directory (22 tests, 44 setup/cleanup errors, exit 1). The approved unsandboxed disposable-fixture run passed 22 tests; adding immutable-anchor cases produced the final 24-pass run. Neither attempt launched Unity or wrote owner files. Existing B3A/Unity reports were not edited. No whole Unity suite or Windows build was rerun for this correction; their prior evidence remains applicable only to the unchanged runtime/tests.

New actual command output, negative trace, preservation audit, incident hashes, previous-restoration verification, read-only recovery search and the closeout auditor source are retained in [safety-closeout-reports.zip](safety-closeout-reports.zip). [safety-closeout.json](safety-closeout.json) records incomplete status and evidence hashes. The local auditor lives in `TestResults/b3a-preservation-closeout-20261018`; its four explicit modes were `negative`, `tests`, `audit`, `search`.

## Recovery search and exact outstanding originals

Read-only search covered repository Docs/TestResults and their retained archives, `C:/Dev/Dungeon-Lord-Backups`, `M:/pre-PR223 Dungeon Lord`, the manual-UAT backup directory, the existing user-folder project and the OneDrive GitHub project copy. It examined **834 PNG files**, **233 ZIP archives** and **1,222 archived PNG entries**, with no exact matches for the five missing hashes, no scan errors and no unsupported archives encountered. Library/Git/generated dependency directories and unrelated personal content were excluded. No cloud placeholder was hydrated. No files were restored or replaced.

Windows File History's configuration directory was absent. `Win32_ShadowCopy` inspection was unavailable: the approved read-only retry returned CIM initialization failure. This is an access/availability limitation, not proof that no Previous Versions exist. No snapshot was mounted or modified. Further recovery needs an owner-identified original backup or accessible version-history source.

All five filenames are under `TestResults/phase7a4-screenshots`:

| Missing original | Required SHA-256 |
| --- | --- |
| `combat-c2-reserve-ui-1920-Default.png` | `0957c9196f7202c68bb3d69b69315256d6089892d1e5cbd2c702d0b77b91f1d3` |
| `combat-c2-reserve-ui-1920-Large.png` | `ffe861491f8d09c2599416334b4b7d3871997cf592b75cae432aee299e1ca69f` |
| `combat-c2-reserve-ui-720-Default.png` | `5f812a2668e34ebb3a72e6aeb208edb17ee6c1ef6c15fde87682a108ef774881` |
| `combat-c2-reserve-ui-720-Large.png` | `fcf284148832be7a8d858a65b83ab12d7babb2cb202ebf8f72aff85a15694a03` |
| `combat-c2-reserve-ui-placement-1920.png` | `c6f15c1d0b3c65140ad4b6d0419572a302e31928682201e2bb248f6b09b5434e` |

## Unchanged graphical blockers

The original assertions/messages and baseline-source reproduction remain in the existing reports:

* `RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated`: expected True, received False (`ProductionDungeonRoomContentSceneTests.cs:879`, content preview).
* `TransactionalRoomConstructionLayoutsLocalizationAndResume`: expected greater than 150, received 122 (`PhaseSevenA4ProductionSceneTests.cs:341`, viewport height).

Both also reproduce with baseline production source in the preserved environment. Neither test was weakened, skipped, renamed, deleted or fixed. Controlled B3A remains 41 passed/0 failed; full EditMode remains 2,175 passed/2 failed/1 existing skip. Overall qualification remains incomplete until outstanding requirements are resolved or explicitly accepted by the owner. This follow-up grants no exception and does not mark the PR ready.
