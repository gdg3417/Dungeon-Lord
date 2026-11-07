# H1-F4 living-fixture qualification blocker — 2026-10-27

Branch `codex/h1-f4-recovery`; base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`;
current/pushed/tested head `eaec790f58ddf8248353c529afad90423a6d9d2d`.
Existing draft PR [#259](https://github.com/gdg3417/Dungeon-Lord/pull/259) remains open.
Recommended model: GPT-6.1 Sol High, Complex; GPT-6 Sol High availability fallback.

## Actual result and smallest correction

The 356-case recovery/Research/save/one-floor/two-floor attempt
`80df1a3213bb4a93b3182030bdd754ad` is **INCOMPLETE / UNCERTAIN** permanently.
Preflight passed. Compilation failed before NUnit results: this new fixture uses
`Is.AnyOf`, which the installed NUnit does not expose (`CS0117` at
`Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs:665`). This is an
agent-introduced test-source error. The smallest correction after approved
reservation recovery is `Is.EqualTo("living-regressions").Or.EqualTo("full-edit").Or.EqualTo("full-play")`.
No runtime change or policy decision is needed for this compiler error.

The Editor log reports exit 1, CLI/runner exit is 6, host exit is 0. At
`2026-10-27T22:35:10.290820+00:00` the supervisor recorded
“Owned descendants still active; completion uncertain” after the unchanged
five-second Job Object drain. `results.xml` does not exist; there are **no new
pass/fail/skip counts**. Restoration and final-preservation stages did not run.
Do not call this FAILED-only or a passed compilation gate. The exact surviving
descendant at the failed drain is not identified by the retained result; no cause
is inferred. This was an Editor attempt, not Windows Player qualification. The
known intermittent native Windows shutdown problem remains separately unresolved.

Latest passing source remains `b9e455a430095326b02a704036296d5a54b52549`:
268 passed, 0 failed, 0 skipped, all five supervisor stages passed, including
111 recovery, 77 structural, 59 canonical-write and all 21 Research cases.
The reviewed head `eb95f250` was requalified 105/105; P0 ordinary-save correction
source `5c52f301` passed 107/107. The last full report at `16955152` remains
1,966 passed, 805 failed, 1 named skip (2,772 total), campaign INCOMPLETE.
Seventeen preparation-tool tests passed at the new head; they are not Unity tests.
No arithmetic reduction of the 805 is claimed from affected-suite results.

Confirmed runtime corrections already qualified: foreground-only rollback no
longer starts observed-background recovery or retries each frame; dormant
zero-prefix acknowledgement no longer dereferences a missing scheduler; the
pending-publication gate also blocks reentrant ordinary publications. Lost-ack
coverage proves refusal of corrupt active authority without writes/deletion and
exact candidate resolution without recomputing mana/healing. Previously corrected
Research/shared fixtures bind complete authored inputs before owner creation;
production validation and frozen history were not weakened. Three of the four
old null references pass in the qualified affected cohort; the scene-test null
reference and remaining lifecycle/configuration/schema assertions are still open.

The new, not-yet-qualified fixture-only commit authors one/two-floor configuration
before ownership, uses consumed policies/loot and supported readers, and exports
current synthetic seeds only into a fresh supervisor-owned artifact directory.
Retained historical two-floor inputs are a protected manifest. Its compilation
must be corrected and the whole affected cohort rerun after recovery.

## Exact read-only diagnosis

[Diagnostic JSON](h1-f4-living-qualification-blocker-20261027.json) is a byte-identical
sanitized copy of the owned diagnostic; it includes every campaign-file hash,
prior incomplete-campaign manifests, nine pinned tools, exact process identities,
archive/map identities and restoration targets. It contains no raw Unity log text.
Raw logs are retained locally because they can contain authentication arguments.

The checks below were made at UTC `2026-10-27T22:37:24.211498+00:00`. These later
absence checks **do not prove descendant completion** and never change the verdict.

| Role | PID | Creation FILETIME | Later handle query |
| --- | --- | --- | --- |
| host | 5948 | 134376140935829604 | absent |
| runner | 20472 | 134376140936944803 | absent |
| supervisor | 14612 | 134376140798000076 | absent |

All three recorded executable identities are the pinned bundled Python executable.
The existing no-Unity guard passed. Query-only access 4 to
`Local\DLQ-5f516ef5069144158c4da8f8a16e1c19` returned no handle, Windows error 2.
No process was terminated. Persistent reservation ownership is exactly the absolute
managed-worktree campaign path above, under
`TestResults/qualification-supervisor-reservation/active.json`.
Execution lock: `TestResults/qualification-supervisor-reservation/execution.lock`.
Its empty-file hash is an identity check, not proof of an unlocked OS byte range;
the proposed execution must acquire the existing Lease without force.

Preparation: `TestResults/h1-f4-20261027-living01`.
Product: `Dungeon Lord B3B2 Smoke H1F4 20261027 living01`.
Save: `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F4 20261027 living01/save_primary.json`.
The isolated save namespace is empty. No benchmark or Player build ran.
All 1,494 current inputs equal the sealed qualified map. Only the three temporary
settings differ from the original map. Every archive member and exact archive
file set matched the original map. All nine pinned tool hashes matched.
Unity remains 6000.3.2f1, CLI beta.14. Protected owner checkout/planning/default XML,
other saves, stopped campaigns, historical fixture corpus, historical two-floor
inputs, qualified F2 executable and review evidence matched. Exact manifests for
both earlier INCOMPLETE campaigns `9ed872...` and `61c2a2...` also matched.

| Identity | SHA-256 |
| --- | --- |
| Reservation | `51bd30ef34e59f8a848cce2cedc5f51906b165dc84aa204ac4b00a95547d556c` |
| Execution lock | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Original archive | `87f14fb244461df941b55680a174956038ae431721858ec4193150cf8dbaa493` |
| Original map file | `d1008d4b5c196e24c816ff536603e8c75420eca96613b528d3d3b4e42cd666bc` |
| Qualified map file | `ae990d5e6f553ddbebdbdd26faba7dd2dd883034b67eb3cdb6cadb4a8925b89a` |
| Original map signature | `3ef1cb795cfd5a7227312ad82d9358b80be8d9c2950abf82abd9ebc8bb8f6896` |
| Qualified/current map signature | `7160da60151859522c5f58754a1ad9962020f5588709130aaa50d8d0377eb139` |
| Protected map file | `0bc3d1a2988d25c8daaa8b1079eb3f913fda6338d9d4bb15e333a76e41b0d368` |

## Proposed recovery — not executed

Separate approval is required for this campaign. Earlier approvals named different
campaigns and do not authorize this release. Restore **only** these three members
from `TestResults/h1-f4-20261027-living01/original-inputs.zip`, after preserving all
three current versions with flushed, hash-verified copies. Do not restore the font.

| File | Original/archive SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `ProjectSettings/EditorBuildSettings.asset` | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` | `f72e8fd550133184822260c85b62c371189f88e1d390fb95839292e9247ee7cf` |
| `ProjectSettings/ProjectSettings.asset` | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` | `0b2c231762c75ef71648c6c1a738949e955da4fcfbfd7355e840a2002e39d91b` |
| `ProjectSettings/ShaderGraphSettings.asset` | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` | `5c432c9c73fdf73fee91c8823ea02ab98e3b7a8edf341b5ae6901d699cce52a7` |

TMP fallback font: `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`. Original and current SHA-256 are both
`6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d`. It is unchanged and excluded from restoration.

Proposed script: `TestResults/h1-f4-20261027-living01/incomplete-investigation/proposed_recovery.py`.
SHA-256: `48d9114d61afca854ab9f22cbd5f93e06401c3c54e4445727812592d4905754a`.
Diagnostic SHA-256: `2d0da301466f6903f3a0443c294c28414dc553fdbcf9c572d2571a05207e5952`.
The exact script is reproduced below. Check-only invocation passed at
`2026-10-27T22:37:44.169905+00:00`: zero modification attempts, no restored files,
`released=false`. This proves only that prechecks passed at that observation.
The original archive, all campaign files and reservation remain unchanged.

Proposed command, **only after separate approval**, run by Codex:

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B TestResults/h1-f4-20261027-living01/incomplete-investigation/proposed_recovery.py --execute-approved-recovery --receipt owner-approved-recovery01
```

Before any write, the script checks exact branch/HEAD, INCOMPLETE/UNCERTAIN verdict,
all campaign evidence, reservation bytes/owner, absent legacy conflicts, complete
current input map, pinned tools, every recorded PID (including PID reuse), no Unity,
absent named Job Object, archive/map hashes and every archive member, protected
inputs/evidence, execution-lock bytes, isolated save namespace, prior incomplete
campaigns and each target hash. It repeats all guards under the existing exclusive
Lease. Any unexpected state stops without termination or force-unlocking.

Before restoration, preserve the three current versions in the new receipt's
`before-restoration` directory using exclusive creation, flush/fsync and hash
verification. Restore the three exact archive members, verifying each original
hash and the complete original input map. Recheck protected and campaign evidence
and exact reservation owner. Only then call existing `Lease.release(CAMPAIGN)`;
require `campaign=null` and `previous=<exact campaign path>`. Verify full inputs and
protected evidence again. Preserve receipt and all original campaign files/verdicts.

After any approved execution, independently reread the actual receipt (attempted
and confirmed modifications, success/prechecks/postchecks/released fields), all
three restored hashes, complete original input map, unchanged font, protected
manifest, prior/current campaign manifests, and released reservation ownership.
A script exit alone is insufficient. Do not reuse this campaign.

Partial restoration remains recoverable from the immutable original archive and
preserved current copies; there is no automatic retry. On a write or verification
failure before release, the reservation stays assigned. A crash after release
requires rereading actual ownership and hashes rather than assuming the receipt
was written. Preserve partial state, error and receipt evidence, stop, and obtain
review for an exact follow-up. Do not overwrite the original archive or old receipt.

No restoration, release, termination, deletion, verdict change or new launch has
occurred. The three temporary settings are deliberately still present. This new
blocker document and diagnostic remain uncommitted while exact source/inputs are
held for review; runtime/test milestone `eaec790f` is pushed to the same draft PR.
After approved recovery, correct the NUnit constraint, commit the focused change
and run a fresh isolated 356-case attempt; then continue the remaining correctness
cohorts. Broad Windows, final performance comparison and owner UAT remain pending.

The [qualification supervisor runbook](qualification-supervisor.md) requires:
“Codex must investigate and obtain owner approval for any recovery that would
release an uncertain campaign or terminate an orphan.” That explicit rule is why
this new exceptional release cannot proceed under earlier campaign-specific approval.

## Exact proposal script

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
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'Tools/Presentation'))
import qualify_h1_f4 as q
import qualification_supervisor as supervisor
import qualification_windows as win

D = json.loads((HERE / 'diagnostic.json').read_text(encoding='utf-8'))
CAMPAIGN = Path(D['campaign'])
PREP = HERE.parent
FILES = q.SETTINGS

def require(condition, message):
    if not condition:
        raise ValueError(message)

def read(path):
    return json.loads(path.read_text(encoding='utf-8'))

def tree(path):
    return {k.replace('\\', '/'): v for k, v in q.tree(path).items()}

def guards():
    require(subprocess.check_output(['git','rev-parse','HEAD'], cwd=ROOT, text=True).strip() == D['source_sha'], 'Source changed')
    require(subprocess.check_output(['git','branch','--show-current'], cwd=ROOT, text=True).strip() == D['branch'], 'Branch changed')
    state = read(CAMPAIGN / 'campaign.json')
    require(state['status'] == 'INCOMPLETE' and state['stages']['living-regressions']['failure'] == 'UNCERTAIN', 'Verdict changed')
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
    ctypes.set_last_error(0)
    handle = win.open_job(4, False, D['job_query']['name'])
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
    _, _, save = q.identities('living01')
    require(q.sha(PREP / 'protected.json') == D['protected']['map_sha256'], 'Protected map changed')
    q.check_protected(PREP, save)
    require(q.sha(ROOT / 'TestResults/qualification-supervisor-reservation/execution.lock') == D['execution_lock_sha256'], 'Execution-lock bytes changed')
    require(tree(save) == {k.replace('\\','/'):v for k,v in D['own_save_tree'].items()}, 'Isolated save namespace changed')
    for path, manifest in D['prior_campaign_file_hashes'].items():
        require(tree(Path(path)) == {k.replace('\\','/'):v for k,v in manifest.items()}, 'Prior incomplete evidence changed')
    for name in FILES:
        supervisor.inputs.no_redirect(ROOT / name)
        require(q.sha(ROOT / name) == D['proposed_restoration_inputs'][name]['current'], 'Restoration target changed: ' + name)
    return original, replacements, save

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
        original, replacements, save = guards()
        receipt['prechecks_passed'] = True
        if args.execute_approved_recovery:
            with supervisor.Lease(ROOT) as lease:
                # Repeat all checks under the existing exclusive Lease before any restoration.
                original, replacements, save = guards()
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
                for name in FILES:
                    require(q.sha(ROOT / name) == D['proposed_restoration_inputs'][name]['current'], 'Target changed immediately before restoration')
                    receipt['modification_attempts'].append(name)
                    (ROOT / name).write_bytes(replacements[name])
                    receipt['restored'].append(name)
                    require(q.sha(ROOT / name) == original[name], 'Restored hash mismatch')
                require(q.inventory(ROOT) == original, 'Restored complete input map mismatch')
                q.check_protected(PREP, save)
                require(tree(CAMPAIGN) == {k.replace('\\','/'):v for k,v in D['campaign_file_hashes'].items()}, 'Campaign evidence changed')
                require(read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == {'campaign': str(CAMPAIGN)}, 'Reservation ownership changed before release')
                lease.release(CAMPAIGN)
                receipt['released'] = True
                require(read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json') == {'campaign': None, 'previous': str(CAMPAIGN)}, 'Released ownership mismatch')
                require(q.inventory(ROOT) == original, 'Post-release input mismatch')
                q.check_protected(PREP, save)
                receipt['postchecks_passed'] = True
        receipt['success'] = True
    except Exception as error:
        receipt['success'] = False
        receipt['error'] = str(error)
    receipt['final_target_hashes'] = {name: q.sha(ROOT / name) for name in FILES}
    receipt['final_reservation'] = read(ROOT / 'TestResults/qualification-supervisor-reservation/active.json')
    q.write(output / 'receipt.json', receipt)
    print(json.dumps(receipt, indent=2))
    return 0 if receipt['success'] else 1

if __name__ == '__main__':
    raise SystemExit(main())
```

BLOCKED — SPECIFIC CORRECTION REQUIRED
