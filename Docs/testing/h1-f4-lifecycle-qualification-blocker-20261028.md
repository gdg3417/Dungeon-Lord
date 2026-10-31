# H1-F4 lifecycle qualification preservation blocker — 2026-10-28

**BLOCKED — separate owner approval required before exceptional recovery.** No restoration, reservation release, process termination, supervisor change or new Unity campaign has occurred.

Branch `codex/h1-f4-recovery`; draft PR [#259](https://github.com/gdg3417/Dungeon-Lord/pull/259). Source and pushed HEAD `4b5f9067cd313d5ee8b92a9b18102b8511a65ac8`; base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. This review document is uncommitted so the source identity required by recovery remains unchanged.

## Exact blocker and results

Campaign `b481fef13741433dbb359899f757c09e`, attempt `complete-lifecycle01`, is permanently **INCOMPLETE / UNCERTAIN**. Raw NUnit: **20 total, 17 passed, 3 failed, 0 skipped, 0 inconclusive**. Preflight passed. Test stage is uncertain; restoration and final preservation stages did not run. Reservation remains held by this campaign.

The CLI reported Unity exit 2 after test failures. `qualify_h1_f4.run_child` observed the TMP fallback font changed and raised `ValueError: Unapproved input change; preserved without restoration` at line349. The approved adapter permits retaining/restoring this known font rewrite only after a zero-exit CLI. This failed invocation does not satisfy that rule. No rule is being bypassed or weakened. The helper exited1, recorded host exited0. Missing cli-exit.json and input mismatch remain part of the original sealed evidence. A licensing warning is present in the raw log; no licensing cause for the assertion failures is established.

The separately complete scene-clock campaign `29c8369fa6d24768aded762b44355beb` at `94c8a2d7cbfab6fefd1c5c3f31c97d179a4fae9d` remains **9 passed / 0 failed / 0 skipped**, all five stages and independent preservation verification passed. It does not replace full qualification.

## Failed identities and complete stacks

### DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState

```text
Expected: True
  But was:  False
at DungeonBuilder.M0.Tests.PhaseSevenA4ProductionSceneTests+<LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState>d__127.MoveNext () [0x00646] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Editor\DungeonSpatial\Tests\ProductionDungeonForegroundReadabilityTests.cs:154
at UnityEngine.TestTools.TestEnumerator+<Execute>d__7.MoveNext () [0x0003a] in .\Library\PackageCache\com.unity.test-framework@0b7a23ab2e1d\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:44
```

### DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary

```text
Expected: True
  But was:  False
at DungeonBuilder.M0.Tests.PhaseSevenA4ProductionSceneTests+<LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary>d__149.MoveNext () [0x0011f] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Editor\DungeonSpatial\Tests\ProductionDungeonForegroundSceneTests.cs:488
at UnityEngine.TestTools.TestEnumerator+<Execute>d__7.MoveNext () [0x0003a] in .\Library\PackageCache\com.unity.test-framework@0b7a23ab2e1d\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:44
```

### DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.LivingDungeonB3B2UncertainRollbackKeepsRealRootRecoveryLatch

```text
System.ArgumentException : living.foreground.observation_invalid
at DungeonBuilder.M0.ForegroundObservation..ctor (System.Byte[] predecessor, System.Int64 captured, System.Int64 accepted, System.Int64 closed, DungeonBuilder.M0.UnixTimePoint wall, DungeonBuilder.M0.ForegroundObservationContext context) [0x00086] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Scripts\Services\ForegroundObservation.cs:27 
  at DungeonBuilder.M0.ForegroundObservation.ForDeterministicServiceTest (System.Byte[] predecessor, System.Int64 through, System.Int64 accepted, System.Int64 closed, DungeonBuilder.M0.UnixTimePoint wall, DungeonBuilder.M0.ForegroundObservationContext context) [0x00000] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Scripts\Services\ForegroundObservation.cs:39 
  at DungeonBuilder.M0.SaveService.AdvanceForeground (DungeonBuilder.M0.SaveData current, System.Int64 through, System.Int32 tickSeconds, System.Action`2[T1,T2] passiveTick, System.Nullable`1[T] suspendedSaveUtc) [0x00000] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Scripts\Services\SaveService.Foreground.cs:39 
  at DungeonBuilder.M0.Tests.PhaseSevenA4ProductionSceneTests+<ForegroundFailureRetry>d__144.MoveNext () [0x00429] in C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Assets\_Project\Editor\DungeonSpatial\Tests\ProductionDungeonForegroundSceneTests.cs:308 
  at UnityEngine.TestTools.TestEnumerator+<Execute>d__7.MoveNext () [0x0003a] in .\Library\PackageCache\com.unity.test-framework@0b7a23ab2e1d\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:44
```

The first failure concerns visibility after a direct TimeService focus-return call (readability line154); the second expects the older `_resumeOfflineBoundaryPersisted` flag (foreground line488); the third supplies a raw deterministic foreground cut that is rejected before the intended recovery-required assertion (foreground line308). These are source-backed failing locations, not completed diagnoses. No expected values were rewritten and no fixes were attempted after the preservation stop. Research start/claim/rollback/uncertain cases otherwise passed in this raw XML.

## Restoration targets and immutable archive

Original archive: `TestResults/h1-f4-20261027-complete-lifecycle01/original-inputs.zip`. Archive members are the exact paths below. All 1,494 members were verified against the original inventory. Current input set differs from the qualified set only at the TMP font. The three settings retain their planned isolated values.

| Path | Original SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | `6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d` | `f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52` |
| `ProjectSettings/EditorBuildSettings.asset` | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` | `f72e8fd550133184822260c85b62c371189f88e1d390fb95839292e9247ee7cf` |
| `ProjectSettings/ProjectSettings.asset` | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` | `73bd698973a29d89461ce9e05e4bae48fca9684b5f1954d5954226995f4d71ef` |
| `ProjectSettings/ShaderGraphSettings.asset` | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` | `5c432c9c73fdf73fee91c8823ea02ab98e3b7a8edf341b5ae6901d699cce52a7` |

| Evidence | SHA-256 / identity |
| --- | --- |
| Original archive | `7c6d98a4823f7d5cb3bd3c36ce5bf1fd994ab482c1fabdafefab1145d71ddf60` |
| original-inputs.json | `e2a65fbce41aecebb2b3ae677d3cbd8b296e4682454e08b676f6fed8bc8c7aae` |
| qualified-inputs.json | `da04934d12e945d864f744b876ec9816ac2b0d8d70c132ff49bad8a529b760ea` |
| Qualified input map signature | `525b4bde57c490cb740083c97ab6e51e17b0ca2f47d0d92599a652430d08022b` |
| Current input map signature | `e354d82f32855c5bc04b4ac249a3e71797b2294dc1f90077fd249052a63bcc98` |
| Original input map signature | `1f5a968ed1196f145d7ac51865df31085daa0c377a361a08e4f456d13f8a6c65` |
| Raw NUnit XML | `1e8c1a51ad8681358623dea23ad1fe22a26e23adb0c793b0062b908969c2186a` |
| Protected inventory | `a851982a635dd49ca3c139a52c5ba9651878b140a507c48ad46df1321c045d6e` |

Full original/current maps, all campaign-file hashes, exact source/tool identities and protected/prior evidence maps are retained in `preservation-review01/diagnostic.json`. The failed saves and canonical snapshots are sealed under the campaign `scene-lifecycle/fixture-inputs/failed-scene-*` paths and retained in its isolated product namespace.

Isolated product: `Dungeon Lord B3B2 Smoke H1F4 20261027 complete-lifecycle01`. Save namespace: `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F4 20261027 complete-lifecycle01`. Owner checkout, normal owner saves, prior builds and historical campaigns are protected; all checks passed at diagnostic capture.

## Reservation and process evidence

Reservation path: `TestResults/qualification-supervisor-reservation/active.json`. Contents:

```json
{
  "campaign": "C:\\Users\\gdg34\\.codex\\worktrees\\h1-f4-recovery\\Dungeon-Lord\\TestResults\\qualification-supervisor\\b481fef13741433dbb359899f757c09e"
}
```

Reservation SHA-256: `b7516e87b3197d02d594c5392817235cc68940b5de74bedd170741120868f401`. Execution lock: `TestResults/qualification-supervisor-reservation/execution.lock`; SHA-256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`; metadata {"dev": 10958016848787023586, "ino": 7318349395614852, "size": 0}.

| Recorded role | PID | Creation FILETIME | Current identity |
| --- | ---: | ---: | --- |
| scene-lifecycle-preflight/child_process | 45508 | 134376669190042721 | absent |
| scene-lifecycle-preflight/process | 25392 | 134376669188968608 | absent |
| scene-lifecycle/child_process | 50712 | 134376669289576666 | absent |
| scene-lifecycle/process | 45380 | 134376669288421481 | absent |
| supervisor | 13920 | 134376669152686117 | absent |

All recorded executables are the pinned Python path below. Named Job checks:
- `Local\DLQ-9948b07bf8d849448355458f00f03702`: absent, Windows error2.
- `Local\DLQ-14d59fd75afa4ace99c2c9fe74abf040`: absent, Windows error2.

The supervisor records child identity/exit only after its existing Job accounting drain succeeds. This run reached the later input-applicability check: host0, helper1, child identity and timestamps were recorded. Later process/Job absence corroborates current absence; it does not repair missing CLI evidence or change the uncertain campaign verdict. No standalone historical Unity/CLI identity or missing acknowledgement has been fabricated.

## Pinned tools

| Exact path | SHA-256 | Version |
| --- | --- | --- |
| `C:\Program Files\Git\cmd\git.EXE` | `81ef35ae005ca9318018d18e3327578ce939fb99feaad6b2d7c8ab15f3de8db5` | git version 2.54.0.windows.1 |
| `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe` | `c2f3d552a9f9a4639b2e54e9d41f360614d512ba6a9816c8c934568ecc2ad93d` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Users\gdg34\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe` | `73dbf1f40507275d6fa636b49eb80b4753e313f649f52591abba86b31f1a1bce` | 3.12.14 (main, Aug 25 2026, 14:01:42) [MSC v.1944 64 bit (AMD64)] |
| `C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_inputs.py` | `34d3ff45cf7512e21f242bc0e4ede876e5c8b68eddba853ad9b1e69192270c92` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_supervisor.py` | `957b83f2b60be9790e4ec952c457372d93516ee9ee34b6c56990c9ed713e996a` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualification_windows.py` | `234bce5f0ca8c7087bb999c603cfbb3d87f0eb4499501a1644dcbbabf3104db7` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Users\gdg34\.codex\worktrees\h1-f4-recovery\Dungeon-Lord\Tools\Presentation\qualify_h1_f4.py` | `5e3c6c63b1d4de667ecbbfa63d9a9c833c9fc518fe11defda25d5477f32d8311` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe` | `78761a0547cd4a9bc212e27a217c66e6a621bf48c753ba440e4b2327ee7ff8ac` | H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1 |
| `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.EXE` | `60c9b29843624dd8af6b6a7b26147753be6aeef65098b9a503cc50850afe410f` | Windows PowerShell 5.1; process-inspection dependency |

## Reviewed proposal — not executed

Proposed scope: preserve and hash-verify the four current versions, restore only the four archive members listed above, verify the complete original input map and protected evidence, then release only this campaign through the existing exclusive Lease. Preserve every original campaign file and its INCOMPLETE/UNCERTAIN classification.

Preflight checks source/branch, exact diagnostic, complete campaign tree, reservation bytes/owner, every tool, all recorded PID absences, no Unity processes, both Job absences, complete archive/member hashes, original/qualified/current input maps, protected map, current save namespace, all 85 prior evidence directories and each current target hash. An unexpected result stops before restoration. The approved receipt path must be unused.

Before Lease, verify lock bytes through the normal path. Under Lease, verify the same lock file through held-handle bytes and metadata; never reread its exclusively locked range through another handle. Repeat applicable guards while holding exclusive ownership. Preserve all four current versions with fsync and exact hashes before touching a target.

After restoration, verify all original inputs, campaign tree, owned failed saves, protected/prior evidence and exact reservation owner before Lease.release. Verify released contents `{campaign:null, previous:<exact campaign>}` and all original inputs/protected evidence again. A separate independent completion check is still required after any approved execution. No process termination, force unlock, deletion, verdict rewrite or supervisor-policy modification is proposed.

Partial failure: the fresh receipt records phase, modification attempts, restored files, errors and actual final reservation. Every current target has already been copied before the first restoration; the original full archive remains untouched. A partial restoration is recoverable from these artifacts, but must stop for a new exact-state review. There is no automatic retry, rollback or release after a failed pre-release check. A crash could leave only the pre-modification receipt; inspect actual target hashes rather than assuming that no writes occurred. If failure occurs after release, report the actual released ownership.

Exact proposed command, **requires separate owner approval**:

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B TestResults/h1-f4-20261027-complete-lifecycle01/preservation-review01/proposed_recovery01.py --execute-approved-recovery --receipt owner-approved-recovery01
```

Proposed receipt: `TestResults/h1-f4-20261027-complete-lifecycle01/owner-approved-recovery01/receipt.json` (does not exist). Read-only proposal preflight passed at `review-check01/receipt.json`, with no modification attempts/restoration/release. Ten disposable Windows guard cases passed, including held-handle checking and conflicting Lease, reservation, input, protected-evidence and prior-evidence rejection. This is guard proof only, not Unity qualification or a completed recovery.

| Review artifact | SHA-256 |
| --- | --- |
| `preservation-review01/proposed_recovery01.py` | `9a9abb70783a9f7fd111aa0424e07087b7ab489d531b14d6fe3cb0dee57ff13a` |
| `preservation-review01/diagnostic.json` | `5f25371046a4d5de2c67df0281cd3c5531fc2e47375eed39137a05241835a48a` |
| `preservation-review01/guard-tests01.json` | `adfb82d26405964386f5cc722094e400234d0394f3231f8a7eecbc6aef63a032` |
| `preservation-review01/test_proposed_guards01.py` | `e61a8a45e7e10d4dfe8bbd5faeb9693e037442d59117d37e47a680f78c3b97e6` |
| `preservation-review01/capture_diagnostic01.py` | `e33c91a8e6b187725164001c0183767317976db6d5b80e3ff0e3d761ce41a91b` |
| `review-check01/receipt.json` | `7a6bea90fa3a9c70077ecbb2e683d837ee806c04eb9b324c3e040e64df98b8cc` |

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

EXPECTED_DIAGNOSTIC = '5f25371046a4d5de2c67df0281cd3c5531fc2e47375eed39137a05241835a48a'
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
    require(state['status'] == 'INCOMPLETE' and state['stages']['scene-lifecycle']['failure'] == 'UNCERTAIN', 'Verdict changed')
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
    _, _, save = q.identities('complete-lifecycle01')
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

Independent review and explicit campaign-specific approval of this exact proposal are required. After approved recovery and independent completion verification, trace/correct the three failed cases, rerun a fresh affected cohort, then continue shared fixture corrections/full qualification/Windows and matched performance. No new campaign may overlap the held reservation. Full EditMode, applicable PlayMode, interruption/reopen, Windows runtime and matched performance remain incomplete. Do not merge or begin owner UAT.

The repository runbook explicitly states: “Codex must investigate and obtain owner approval for any recovery that would release an uncertain campaign or terminate an orphan.” See [qualification-supervisor.md](qualification-supervisor.md). This is the reason for stopping despite authorization to continue ordinary test corrections.
