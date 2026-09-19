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
