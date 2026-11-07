# H1-F4 PlayMode preservation blocker — 2026-10-28

**REVIEW ONLY / NOT EXECUTED. Separate campaign-specific recovery authorization required.**

PR #259; branch codex/h1-f4-recovery; unchanged source 3245f69d757fe50316a1967463ab7061a548f2f6; base 3eb6ccf93558b8145d64e9e63cbefcbd73484602. This report is uncommitted to preserve recovery HEAD.

## Completed recovery and lifecycle corrections

The owner-approved b481fef13741433dbb359899f757c09e recovery completed once. Independent verification checked the complete receipt, all four restored files and preserved current copies, 1,494 original inputs, exact reservation release, nine tools and 85 prior evidence directories. Original INCOMPLETE / UNCERTAIN is unchanged. Operation receipt SHA c049cbf64898244d608e565d838d79d82eb845ad78c6e4dd65b48040883981dc; independent receipt SHA e754c7c6988abfc28e0d9ab6a20b9dea8f03bca3d282e932d22bb26731a92bad. [Published receipt](https://github.com/gdg3417/Dungeon-Lord/blob/872ce751/Docs/testing/h1-f4-lifecycle-recovery-receipt-20261028.json).

The three lifecycle failures were fixture defects: focus return bypassed root observation/episode closure; the suspended test expected background processing and an older resume flag; the uncertainty test supplied a cut below accepted A before reaching its intended authority guard. Corrections retain exact durable W/A/C, gameplay and no-duplicate/no-IO assertions. Production behavior, tuning, schemas and validators are unchanged.

Commit872ce751 contains corrections/recovery evidence; fb9f1979 the focused selector; 3245f69d the qualified milestone and 16 direct PlayMode wrappers. At fb9f19795410ae2412a0bed5c980ab7b8e4c64d9, a0e6d370cee54cd7bed21b75b88cda60 passed 3/0/0 and its superset f1719fcee34142f8ac2814817bab5cc7 passed 20/0/0. All five stages and independent verification passed. Counts overlap. [Complete results](https://github.com/gdg3417/Dungeon-Lord/blob/3245f69d757fe50316a1967463ab7061a548f2f6/Docs/testing/h1-f4-lifecycle-results-20261028.json).

## New blocker and exact failed condition

Campaign d16c05a430f7404189ab22034383ec50, attempt complete-scene-play01, is permanently INCOMPLETE / UNCERTAIN; reservation held. Raw NUnit: 28 total, 28 passed, 0 failed/skipped/inconclusive. Preflight passed; restoration and final preservation did not run. Raw passes do not constitute supervisor qualification.

qualify_h1_f4.py:369–372 requires a zero-exit CLI, exactly the known TMP font change and its exact hash before automatic generated-input restoration. This run changed TMP plus ProjectSettings/ProjectSettings.asset. The unchanged guard raised ValueError: Unapproved input change; preserved without restoration. Host0/helper1 are recorded. Missing cli-exit.json remains missing. Unity log records Test run completed. Exiting with code 0 (Ok). Run completed. A standalone CLI return acknowledgement was not durably recorded; do not infer it from XML or the Unity log.

Exact byte reconstruction proves ProjectSettings changed only by normalizing 890 CRLFs to LF and reordering applicationIdentifier entries Standalone/Android to Android/Standalone. Both values remain com.gdg3417.dungeonlord; no semantic value changed. The precise Unity/native writer callback is not proven. No speculative runtime change or preservation exception was introduced.

Qualified settings SHA fd84aefcb36368b5581ec092ee41faacfd851bc66358a4ee63e8eebb40de3477; exact reconstructed/current SHA 9748e7d76082d943f194386d65ed978175f30fe013f96f360815eb82df4ea56d.

## Four restoration targets

| Exact path / archive member | Original SHA-256 | Current SHA-256 |
| --- | --- | --- |
| Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset | 6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d | f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52 |
| ProjectSettings/EditorBuildSettings.asset | 53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9 | f72e8fd550133184822260c85b62c371189f88e1d390fb95839292e9247ee7cf |
| ProjectSettings/ProjectSettings.asset | 34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993 | 9748e7d76082d943f194386d65ed978175f30fe013f96f360815eb82df4ea56d |
| ProjectSettings/ShaderGraphSettings.asset | 6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72 | 5c432c9c73fdf73fee91c8823ea02ab98e3b7a8edf341b5ae6901d699cce52a7 |

Archive: TestResults/h1-f4-20261027-complete-scene-play01/original-inputs.zip. Every one of its 1,494 members matches the original input map. Complete current inputs match the diagnostic; only the font and ProjectSettings differ from qualified inputs.

| Evidence | SHA-256 / signature |
| --- | --- |
| Original archive | b226a6b6c4092d9fa5b510dd8ccf8f7ce8c91905e4b9c582dc767007abb054f8 |
| Original map | c922765f91ebb926a0726c6d5653e74bbe20a7863d42ad13e08e2c61fedfec27 |
| Qualified map | 73e6ab49665c20274718888691f6875ee2741ab2afd2beddd4c22e700087833e |
| Current input identity | b3868a74f2c064ce5a8d29d2740d9049c2e1fdf96875141d7132d9ea41bb37da |
| Qualified input identity | da698e78050a8f87c1eb7e6fe63f4b8b2f8ebf8463a4ae6bbcd4a893f50e3efb |
| Original input identity | a32c39b15b4bd2a414b1bf40083b3bc7daee3e488bb827a6335c1b14b560ffd9 |
| NUnit XML | 9dfe80ee6185eecb1cba10623b3637425ecddc22f592183fb3413b1f6619eeee |
| Protected map | af4947804008a429c6b3b7aef94dcc5b19d633fc853a857ed6dc58dcf104f302 |

Product: Dungeon Lord B3B2 Smoke H1F4 20261027 complete-scene-play01. Save namespace: C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F4 20261027 complete-scene-play01. Campaign tree, current saves, owner/historical evidence and all 91 prior evidence directories match. Full maps are in diagnostic.json.

## Reservation, process and Job identities

Reservation TestResults/qualification-supervisor-reservation/active.json SHA 008cb65ad2cc9aaeec012a61168dfe2bba6cc7a98562974038457f6f9d9a70a1. Contents:
```json
{
  "campaign": "C:\\Users\\gdg34\\.codex\\worktrees\\h1-f4-recovery\\Dungeon-Lord\\TestResults\\qualification-supervisor\\d16c05a430f7404189ab22034383ec50"
}
```

Execution lock TestResults/qualification-supervisor-reservation/execution.lock SHA e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855; metadata {"dev": 10958016848787023586, "ino": 7318349395614852, "size": 0}.

| Role | PID | Creation FILETIME | Current |
| --- | ---: | ---: | --- |
| scene-play-preflight/child_process | 49256 | 134376710092750684 | absent |
| scene-play-preflight/process | 55264 | 134376710091014947 | absent |
| scene-play/child_process | 37752 | 134376710190077289 | absent |
| scene-play/process | 38184 | 134376710188789688 | absent |
| supervisor | 16312 | 134376710054636795 | absent |

Recorded executables are the pinned Python below. Job queries:
- Local\DLQ-712d5636525e4622a26ee8c8aa0fb6ac: absent, Windows error2.
- Local\DLQ-3e235f35561d423abb1583c6eff2eb54: absent, Windows error2.

Supervisor owned-Job drain precedes recorded child identity/exit; input applicability failed afterward. Later absence does not repair missing CLI acknowledgement or change uncertainty. No process was terminated.

## Pinned tools

| Path | SHA-256 | Version |
| --- | --- | --- |
| C:\Program Files\Git\cmd\git.EXE | 81ef35ae005ca9318018d18e3327578ce939fb99feaad6b2d7c8ab15f3de8db5 | git version 2.54.0.windows.1 |
| C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe | c2f3d552a9f9a4639b2e54e9d41f360614d512ba6a9816c8c934568ecc2ad93d | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Users\gdg34\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe | 73dbf1f40507275d6fa636b49eb80b4753e313f649f52591abba86b31f1a1bce | 3.12.14 (main, Aug 25 2026, 14:01:42) [MSC v.1944 64 bit (AMD64)] |
| C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_inputs.py | 34d3ff45cf7512e21f242bc0e4ede876e5c8b68eddba853ad9b1e69192270c92 | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_supervisor.py | 957b83f2b60be9790e4ec952c457372d93516ee9ee34b6c56990c9ed713e996a | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_windows.py | 234bce5f0ca8c7087bb999c603cfbb3d87f0eb4499501a1644dcbbabf3104db7 | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualify_h1_f4.py | 0c0d2a406f4d0922a59219b537afc5f2b6bc7df5a0a2e3e2af8648fbe02c569f | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe | 78761a0547cd4a9bc212e27a217c66e6a621bf48c753ba440e4b2327ee7ff8ac | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| C:\Windows\System32\WindowsPowerShell\v1.0\powershell.EXE | 60c9b29843624dd8af6b6a7b26147753be6aeef65098b9a503cc50850afe410f | Windows PowerShell 5.1; process-inspection dependency |

## Exact proposal and safeguards

This is a new campaign and new script. Prior authorization applied only to b481fef13741433dbb359899f757c09e. Proposed: preserve and hash-verify all four current versions, restore only their exact original archive bytes, verify all original inputs and protected evidence, then release only d16c05a430f7404189ab22034383ec50 through the existing exclusive Lease. Preserve every original verdict and receipt.

Preflight: exact branch/HEAD, diagnostic, campaign tree, reservation bytes/owner, nine tools, all recorded PID absences, no Unity processes, both Job absences, archive/full member map, original/qualified/current inputs, protected map, current save tree, all 91 prior evidence trees, four target hashes and an unused approved receipt directory. Any mismatch stops before restoration.

Hash execution.lock before Lease. Under exclusive Lease verify its held-handle bytes and metadata, without a second handle reading the locked range; repeat applicable guards. Before any target write, copy/fsync/hash-verify all four current versions. Restore exact archived bytes sequentially, verifying each hash.

Before release, verify the entire original input map, campaign/current-save/protected/prior evidence and exact reservation ownership. Call only lease.release(CAMPAIGN) under the same held Lease. Verify actual reservation campaign=null, previous=exact campaign, original inputs and evidence afterward. Independent post-execution verification of the complete final receipt and actual state remains required.

Partial failure stops without automatic retry, force unlock, rollback or subsequent release. Receipt records phase, attempted/restored files, error and actual target/reservation state. All current versions are copied before first restoration, originals remain in archive. Crash may leave only pre-modification receipt: inspect actual hashes, never infer no writes. New exact-state review is required for partial recovery. Report actual ownership if failure occurs after release. No deletion, termination, supervisor change or verdict rewrite.

Exact proposed command — NOT EXECUTED:
```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B TestResults/h1-f4-20261027-complete-scene-play01/preservation-review01/proposed_recovery01.py --execute-approved-recovery --receipt owner-approved-recovery01
```

Receipt: TestResults/h1-f4-20261027-complete-scene-play01/owner-approved-recovery01/receipt.json (unused). Read-only proposal preflight passed; ten disposable guards passed, including conflicting Lease and changed reservation/input/protected/prior evidence. No active Lease or restoration/release occurred.

| Review artifact | SHA-256 |
| --- | --- |
| preservation-review01/proposed_recovery01.py | f4dec6843df4fec9723eba15e909a75daf517174f975a13259ff6b32fde8a9a2 |
| preservation-review01/diagnostic.json | 61be35c89f44993f9b2dc87dd27e65ae96e5d0aba4876b9f2225eca77cf55883 |
| preservation-review01/guard-tests01.json | b5a0a0fb23153ec39a61dfb2ea46bd8052c3ec10295b059c443a7334d102dcbc |
| preservation-review01/test_proposed_guards01.py | c3b3134a24617f9e8cd55d2d4f20e3498bcfb7eef0a42ad76eafa16e1da3064c |
| preservation-review01/capture_diagnostic01.py | ecabaeaa2937339bd9ee29791b50f7ec95a2e4755fd7401c36e3f76312a64dfa |
| review-check01/receipt.json | c6fab83256cf7ef3ef91c12ad6780186c6cc6c5a3c3742cd90922b9234267891 |

## Raw NUnit leaf inventory

- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.AnalysisForegroundContinuity: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2ActiveIdleFramesDoNotReconstructSaveOrRebuildUi: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2AutomaticSceneLoopSettlesAndAdmitsAgainstCommittedEdit: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2LiveFloorActionsValidateAfterTheirElapsedTimeBarrier: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2PresentationSnapsGapsAndInterruptionsAndHidesUnknownGeometry: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2ProvenRollbackRetriesThroughRealSaveBarrier: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2RecordedAdjacentMovementInterpolatesWithoutCanonicalEffects: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2ResumeCompletesSeveralBoundedPassesBeforeRunningAgain: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.LivingDungeonB3B2UncertainRollbackKeepsRealRootRecoveryLatch: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchClaimProvenRollback: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchClaimUncertainRecovery: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchForegroundRollbackBlocksAction: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchPrerequisiteAutomaticSettlement: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchPrerequisiteNativeShellBlocks: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchStartProgressClaimAndReopen: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchStartProvenRollback: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.R0ResearchStartUncertainRecovery: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.RoomContentLootRejectionReasons: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.RoomContentMapReassignmentCapacity: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.RoomContentMapReassignmentCategories: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.SceneClockEligibilityRejectsOwnerProductAndNonBatchContext: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.SceneClockInvalidMonotonicFailsClosed: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.SceneClockInvalidUtcFailsClosed: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.SceneClockNativeFractionAndOwnerRemainBound: Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.SceneClockRepeatedFractionalOrdinaryCutsDoNotDuplicateCredit: Passed

## Complete proposed recovery script

```python
"""Review proposal only. Execution requires separate owner approval for this campaign."""
import argparse
import ctypes
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import traceback
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'Tools/Presentation'))
import qualify_h1_f4 as q
import qualification_supervisor as supervisor
import qualification_windows as win

EXPECTED_DIAGNOSTIC = '61be35c89f44993f9b2dc87dd27e65ae96e5d0aba4876b9f2225eca77cf55883'
D = json.loads((HERE / 'diagnostic.json').read_text(encoding='utf-8'))
CAMPAIGN = Path(D['campaign'])
PREP = HERE.parent
FILES = q.SETTINGS + [q.FONT]

def require(condition, message):
    if not condition:
        raise ValueError(message)

def read(path):
    return json.loads(path.read_text(encoding='utf-8'))

def tree(path):
    return {k.replace('\\', '/'): v for k, v in q.tree(path).items()}

def guards(lease=None, expected_lock_stat=None):
    require(subprocess.check_output(['git','rev-parse','HEAD'], cwd=ROOT, text=True).strip() == D['source_sha'], 'Source changed')
    require(subprocess.check_output(['git','branch','--show-current'], cwd=ROOT, text=True).strip() == D['branch'], 'Branch changed')
    require(q.sha(HERE / 'diagnostic.json') == EXPECTED_DIAGNOSTIC, 'Diagnostic changed')
    state = read(CAMPAIGN / 'campaign.json')
    require(state['status'] == 'INCOMPLETE' and state['stages']['scene-play']['failure'] == 'UNCERTAIN', 'Verdict changed')
    require(tree(CAMPAIGN) == {k.replace('\\','/'): v for k,v in D['campaign_file_hashes'].items()}, 'Campaign evidence changed')
    require(q.sha(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == D['reservation_sha256'], 'Reservation bytes changed')
    require(read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == {'campaign': str(CAMPAIGN)}, 'Reservation owner changed')
    legacy = ROOT / 'Temp/qualification-supervisor/active.json'
    require(not legacy.exists() or read(legacy).get('campaign') is None, 'Legacy reservation present')
    require(supervisor.signature(q.inventory(ROOT)) == D['current_input_identity'], 'Current input set/hash changed')
    for path, digest in D['tool_checks'].items():
        require(digest['expected'] == digest['current'] == q.sha(path), 'Pinned tool changed: ' + path)
    for record in D['processes'].values():
        require(win.identity(record['recorded']['pid']) is None, 'Recorded PID is live or reused; stop for review')
    win.assert_no_unity()
    for job in D['all_job_queries']:
        ctypes.set_last_error(0)
        handle = win.open_job(4, False, job['name'])
        if handle:
            win.close(handle)
            raise ValueError('Previously absent named Job Object now exists')
        require(ctypes.get_last_error() == 2, 'Unexpected Job Object query result')
    require(q.sha(PREP / 'original-inputs.zip') == D['original_archive_sha256'], 'Archive changed')
    require(q.sha(PREP / 'original-inputs.json') == D['original_input_map_sha256'], 'Original map changed')
    require(q.sha(PREP / 'qualified-inputs.json') == D['qualified_input_map_sha256'], 'Qualified map changed')
    original = read(PREP / 'original-inputs.json')
    with zipfile.ZipFile(PREP / 'original-inputs.zip') as archive:
        require(sorted(archive.namelist()) == sorted(original), 'Archive file set changed')
        for name, digest in original.items():
            require(hashlib.sha256(archive.read(name)).hexdigest() == digest, 'Archive member mismatch: ' + name)
        replacements = {name: archive.read(name) for name in FILES}
    _, _, save = q.identities('complete-scene-play01')
    require(q.sha(PREP / 'protected.json') == D['protected']['map_sha256'], 'Protected map changed')
    q.check_protected(PREP, save)
    lock = ROOT / 'TestResults/qualification-supervisor-reservation/execution.lock'
    supervisor.inputs.no_redirect(lock)
    if lease is None:
        require(q.sha(lock) == D['execution_lock_sha256'], 'Execution-lock bytes changed')
    else:
        # Windows denies a separate-handle read of the byte range we own.
        # Retain the pre-Lease hash and prove the same file via held-handle metadata.
        require(isinstance(lease, supervisor.Lease) and lease.base == lock.parent and
                not lease.stream.closed and Path(lease.stream.name) == lock,
                'Require the existing held Lease')
        held = os.fstat(lease.stream.fileno())
        require(expected_lock_stat is not None and os.path.samestat(held, expected_lock_stat) and
                os.path.samestat(held, os.stat(lock)) and held.st_size == expected_lock_stat.st_size,
                'Execution-lock identity or size changed')
        position = lease.stream.tell()
        try:
            lease.stream.seek(0)
            require(hashlib.sha256(lease.stream.read()).hexdigest() == D['execution_lock_sha256'],
                    'Held execution-lock bytes changed')
        finally:
            lease.stream.seek(position)

    require(tree(save) == {k.replace('\\','/'):v for k,v in D['own_save_tree'].items()}, 'Isolated save namespace changed')
    for path, manifest in D['prior_campaign_file_hashes'].items():
        require(tree(Path(path)) == {k.replace('\\','/'):v for k,v in manifest.items()}, 'Prior incomplete evidence changed')
    for path, manifest in D['prior_receipt_file_hashes'].items():
        require(tree(Path(path)) == {k.replace('\\','/'):v for k,v in manifest.items()}, 'Prior receipt evidence changed')
    for name in FILES:
        supervisor.inputs.no_redirect(ROOT / name)
        require(q.sha(ROOT / name) == D['proposed_restoration_inputs'][name]['current'], 'Restoration target changed: ' + name)
    return original, replacements, save

def preserved_evidence(save):
    require(tree(CAMPAIGN) == {k.replace('\\','/'):v for k,v in D['campaign_file_hashes'].items()}, 'Campaign evidence changed')
    require(tree(save) == {k.replace('\\','/'):v for k,v in D['own_save_tree'].items()}, 'Owned failed-save namespace changed')
    for path, manifest in D['prior_campaign_file_hashes'].items():
        require(tree(Path(path)) == {k.replace('\\','/'):v for k,v in manifest.items()}, 'Prior evidence changed')
    q.check_protected(PREP, save)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--execute-approved-recovery', action='store_true')
    parser.add_argument('--receipt', required=True, choices=['review-check01', 'owner-approved-recovery01'])
    args = parser.parse_args()
    require(args.execute_approved_recovery == (args.receipt == 'owner-approved-recovery01'), 'Separate review/approved receipt required')
    output = PREP / args.receipt
    output.mkdir(exist_ok=False)
    receipt = {'utc': datetime.now(timezone.utc).isoformat(), 'campaign': str(CAMPAIGN),
               'execute_requested': args.execute_approved_recovery, 'modification_attempts': [], 'restored': [], 'released': False,
               'proposal_script_sha256': q.sha(__file__), 'diagnostic_sha256': q.sha(HERE / 'diagnostic.json')}
    try:
        receipt['phase'] = 'initial-guards'
        original, replacements, save = guards()
        receipt['prechecks_passed'] = True
        if args.execute_approved_recovery:
            lock_stat = os.stat(ROOT / 'TestResults/qualification-supervisor-reservation/execution.lock')
            receipt['phase'] = 'acquire-lease'
            with supervisor.Lease(ROOT) as lease:
                # Repeat all checks under the existing exclusive Lease before any restoration.
                receipt['phase'] = 'locked-guards'
                original, replacements, save = guards(lease=lease, expected_lock_stat=lock_stat)
                receipt['phase'] = 'preserve-current-settings'
                for name in FILES:
                    destination = output / 'before-restoration' / name
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    with destination.open('xb') as stream:
                        stream.write((ROOT / name).read_bytes())
                        stream.flush()
                        os.fsync(stream.fileno())
                    require(q.sha(destination) == D['proposed_restoration_inputs'][name]['current'], 'Preservation copy failed')
                q.write(output / 'pre-modification.json', receipt)
                require(supervisor.signature(q.inventory(ROOT)) == D['current_input_identity'], 'Input changed immediately before modification')
                receipt['phase'] = 'restore-settings'
                for name in FILES:
                    require(q.sha(ROOT / name) == D['proposed_restoration_inputs'][name]['current'], 'Target changed immediately before restoration')
                    receipt['modification_attempts'].append(name)
                    (ROOT / name).write_bytes(replacements[name])
                    receipt['restored'].append(name)
                    require(q.sha(ROOT / name) == original[name], 'Restored hash mismatch')
                receipt['phase'] = 'verify-before-release'
                require(q.inventory(ROOT) == original, 'Restored complete input map mismatch')
                q.check_protected(PREP, save)
                require(tree(CAMPAIGN) == {k.replace('\\','/'):v for k,v in D['campaign_file_hashes'].items()}, 'Campaign evidence changed')
                require(read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == {'campaign': str(CAMPAIGN)}, 'Reservation ownership changed before release')
                preserved_evidence(save)
                receipt['phase'] = 'lease-release'
                lease.release(CAMPAIGN)
                receipt['released'] = True
                receipt['phase'] = 'post-release-verification'
                require(read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == {'campaign': None, 'previous': str(CAMPAIGN)}, 'Released ownership mismatch')
                require(q.inventory(ROOT) == original, 'Post-release input mismatch')
                q.check_protected(PREP, save)
                preserved_evidence(save)
                receipt['postchecks_passed'] = True
        receipt['phase'] = 'completed' if args.execute_approved_recovery else 'review-check-completed'
        receipt['success'] = True
    except Exception as error:
        receipt['success'] = False
        receipt['error'] = str(error)
        receipt['error_type'] = type(error).__name__
        receipt['error_path'] = getattr(error, 'filename', None)
        receipt['traceback'] = traceback.format_exc()
    receipt['final_target_hashes'] = {name: q.sha(ROOT / name) for name in FILES}
    receipt['final_reservation'] = read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json')
    q.write(output / 'receipt.json', receipt)
    print(json.dumps(receipt, indent=2))
    return 0 if receipt['success'] else 1

if __name__ == '__main__':
    raise SystemExit(main())
```

## Next dependency-correct action

Stop for campaign-specific recovery review and owner authorization. After approved successful recovery and independent verification, use a fresh campaign. A prospective preparation-only correction can preapply this proven applicationIdentifier order and LF serialization before sealing PlayMode qualified inputs, keep the original archive untouched and restore original bytes in the existing restoration stage. This is proposed only, not implemented; no post-run allowlist relaxation or retroactive qualification is proposed.

Shared configuration/nullable-state/historical-current fixtures, full EditMode inventory/testing, applicable full PlayMode, interruption/exact-once/reopen, Windows startup/gameplay/ordinary-abrupt reopen/full exit and matched main-versus-H1-F4 performance remain pending. No performance conclusion, merge, owner UAT or H1-F5/H2/O1 work.

The [qualification runbook](https://github.com/gdg3417/Dungeon-Lord/blob/3245f69d757fe50316a1967463ab7061a548f2f6/Docs/testing/qualification-supervisor.md) explicitly requires owner approval for recovery releasing an uncertain campaign. This preservation blocker is the reason for stopping.
