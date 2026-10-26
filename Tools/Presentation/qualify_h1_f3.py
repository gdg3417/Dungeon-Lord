"""Campaign-specific preparation/preservation for the inactive H1-F3 foundation.

Uses the existing supervisor and exact inventory authority; no test runner,
process manager, source substitution wrapper or owner-UAT launcher is added.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
import zipfile

import qualification_inputs as inputs

ROOT = Path(__file__).resolve().parents[2]
PREP = ROOT / 'TestResults/h1-f3-recovery-20261026'
PRODUCT = 'Dungeon Lord B3B2 Smoke H1F3 Recovery 20261026'
SAVE = Path.home() / 'AppData/LocalLow/gdg3417' / PRODUCT
OWNER = Path('C:/Dev/Dungeon-Lord')
CLI = Path('C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe')
ORIGINAL_CAMPAIGN = ROOT / 'TestResults/qualification-supervisor/b343351f516b4a19b8992ad00910ae40'
SETTINGS = ['ProjectSettings/ProjectSettings.asset', 'ProjectSettings/EditorBuildSettings.asset',
            'ProjectSettings/ShaderGraphSettings.asset']
TEST_GATES = [
    ('focused', 'H1F3;H1F2;FrozenSchemaFifteen', 188, []),
    ('integration', r'R0Research;LivingDungeonB3B2\.;LivingDungeonTwoFloor\.;PhaseFourPassiveOnlineMana;PhaseFourOfflinePassiveMana;PhaseFourOfflineClock;PhaseFourOfflineSaveLifecycle;PhaseFourAcquisition;PhaseFourEconomy', 436, []),
    ('migration', 'FrozenSchemaFifteenMigrationRegression;PhaseSevenA2WindowsSpatialMigration;PhaseFourSessions;PhaseFourWrites;PhaseFourLoad;PhaseFourCompleteSave;PhaseFourSaveSemantics', 224,
     ['DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed'])]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2, sort_keys=True)


def inventory(root):
    return {name: sha(root / name) for name in sorted(inputs.target_inventory(root))}


def tree(path):
    inputs.no_redirect(path)
    if not path.exists(): return {}
    result = {}
    for item in sorted(path.rglob('*')):
        inputs.no_redirect(item)
        if item.is_file(): result[str(item.relative_to(path))] = sha(item)
    return result


def protected():
    proposal = OWNER / 'Docs/planning/living-dungeon-hybrid-persistence-proposal.md'
    # Read-only seals. No command in this helper writes to these identities.
    return dict(owner_proposal=sha(proposal),
                other_saves={p.name: tree(p) for p in SAVE.parent.iterdir()
                             if p.is_dir() and p.name != PRODUCT},
                original_incomplete_campaign=tree(ORIGINAL_CAMPAIGN),
                original_preparation=tree(ROOT / 'TestResults/h1-f3-preparation'))


def verify_protected():
    baseline = json.loads((PREP / 'protected.json').read_text())
    if protected() != baseline: raise ValueError('Protected owner/save preservation mismatch')


def prepare():
    if ROOT == OWNER or '.codex/worktrees/h1-f3-stable-recovery/' not in ROOT.as_posix():
        raise ValueError('Require the isolated managed worktree')
    if PREP.exists() or SAVE.exists(): raise ValueError('Fresh preparation and product identities required')
    inputs.no_redirect(ROOT)
    PREP.mkdir(parents=True)
    write(PREP / 'protected.json', protected())
    write(PREP / 'inputs-before.json', inventory(ROOT))
    with zipfile.ZipFile(PREP / 'original-settings.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
        for name in SETTINGS: archive.write(ROOT / name, name)
    settings = ROOT / 'ProjectSettings/ProjectSettings.asset'
    original = settings.read_bytes()
    (PREP / 'ProjectSettings.asset.original').write_bytes(original)
    text = original.decode('utf-8-sig')
    if text.count('  productName: Dungeon Lord\n') != 1 and text.count('  productName: Dungeon Lord\r\n') != 1:
        raise ValueError('Unexpected product identity')
    settings.write_bytes(text.replace('  productName: Dungeon Lord', '  productName: ' + PRODUCT).encode('utf-8'))
    # Match the two known import-only LF rewrites before sealing, so startup
    # cannot silently change the exact input identity during a test stage.
    for name in SETTINGS[1:]:
        path = ROOT / name
        path.write_bytes(path.read_bytes().replace(b'\r\n', b'\n'))
    print(json.dumps(dict(product=PRODUCT, save=str(SAVE), preparation=str(PREP))))


def seal():
    # Called only after reviewing import-only changes. Pin every file, including
    # generated metadata, before any supervised qualification stage is launched.
    verify_protected()
    rows = inventory(ROOT)
    with zipfile.ZipFile(PREP / 'qualified-inputs.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
        for name in rows: archive.write(ROOT / name, name)
    write(PREP / 'qualified-inputs.json', rows)
    print(json.dumps(dict(count=len(rows), input_identity=hashlib.sha256(
        json.dumps(rows, sort_keys=True).encode()).hexdigest())))
    cli = CLI
    editor = Path('C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Unity.exe')
    helper = Path(__file__).resolve()
    tools = {str(Path(sys.executable)): dict(sha256=sha(sys.executable), version=sys.version),
             str(cli): dict(sha256=sha(cli), version='1.0.0-beta.14; verified current'),
             str(editor): dict(sha256=sha(editor), version='6000.3.2f1')}
    for path in [helper, helper.with_name('qualification_supervisor.py'),
                 helper.with_name('qualification_inputs.py'), helper.with_name('qualification_windows.py')]:
        tools[str(path)] = dict(sha256=sha(path), version='committed H1-F3 campaign tool')
    stages = []
    # Counts derive from the retained #256 exact-filter XML (154/436/224),
    # plus the 34 explicitly declared new NUnit cases. No sustained profiles.
    previous = []
    for name, pattern, count, skips in TEST_GATES:
        pre = name + '-preflight'
        stages.append(dict(id=pre, role='preflight', depends=previous,
            command=[sys.executable, '-B', str(helper), 'preflight', '--stage', '{campaign}/' + pre],
            timeout_seconds=120, artifacts=['preservation.json']))
        stages.append(dict(id=name, role='test', depends=[pre],
            command=[sys.executable, '-B', str(helper), 'run-test', '--gate', name, '--stage', '{stage}'],
            timeout_seconds=960, xml='results.xml', expected_tests=count, allowed_skips=skips,
            artifacts=['unity.log', 'invocation.json', 'cli-exit.json']))
        previous = [name]
    restored = json.loads((PREP / 'inputs-before.json').read_text())
    restored_identity = hashlib.sha256(json.dumps(restored, sort_keys=True).encode()).hexdigest()
    stages.append(dict(id='restore-preflight', role='preflight', depends=previous,
        command=[sys.executable, '-B', str(helper), 'preflight', '--stage', '{stage}'],
        timeout_seconds=120, artifacts=['preservation.json']))
    stages.append(dict(id='restore', role='report', depends=['restore-preflight'],
        command=[sys.executable, '-B', str(helper), 'restore', '--stage', '{stage}'],
        timeout_seconds=120, artifacts=['restoration.json'], after_input_identity=restored_identity))
    stages.append(dict(id='preservation', role='final', depends=[s['id'] for s in stages],
        command=[sys.executable, '-B', str(helper), 'finish', '--stage', '{stage}'],
        timeout_seconds=120, artifacts=['preservation.json'], input_identity=restored_identity))
    plan = dict(purpose='qualification',
        source_sha=subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
        tools=tools, isolation=dict(product=PRODUCT, save=str(SAVE / 'save_primary.json'), project=str(ROOT)),
        approved_substitutions=['Temporary isolated productName only and the two previously measured import-only LF settings rewrites. All three original files archived; exact restoration and final preservation are selected gates. No runtime source/config/test substitutions.',
            'UNITY_NO_AUTH_BROKER=1 only in the test CLI child environment: installed CLI documented direct OS-keyring mode; supervisor ownership/drain/timeouts unchanged.'],
        stages=stages, selections=dict(required=[s['id'] for s in stages]))
    write(PREP / 'plan.json', plan)


def run_test(gate, stage):
    """Synchronous CLI invocation; OS descendant ownership stays with supervisor.

    The installed beta.14 auth reference documents direct-keyring mode. Avoid
    creating a resident two-minute broker inside the test stage's owned job.
    Never loosen the supervisor drain, detach a child, or kill a process here.
    """
    pattern = next((pattern for name, pattern, _, _ in TEST_GATES if name == gate), None)
    if pattern is None: raise ValueError('Unknown H1-F3 gate')
    stage = Path(stage).absolute()
    inputs.no_redirect(stage)
    parent = ROOT / 'TestResults/qualification-supervisor'
    if stage.name != gate or stage.parent.parent != parent or not stage.is_dir():
        raise ValueError('Require an existing supervisor stage directory')
    command = [str(CLI), 'test', str(ROOT), '--mode', 'EditMode', '--filter', pattern,
               '--output', str(stage / 'results.xml'), '--timeout', '900', '--no-color',
               '--', '-logFile', str(stage / 'unity.log')]
    import os
    environment = os.environ.copy()
    environment['UNITY_NO_AUTH_BROKER'] = '1'
    write(stage / 'invocation.json', dict(command=command, environment_override={'UNITY_NO_AUTH_BROKER': '1'},
        completion='Synchronous subprocess; enclosing supervisor still requires owned job empty'))
    result = subprocess.run(command, cwd=ROOT, env=environment, stdin=subprocess.DEVNULL)
    write(stage / 'cli-exit.json', dict(returncode=result.returncode))
    return result.returncode


def check(stage, restored=False):
    verify_protected()
    expected = json.loads((PREP / ('inputs-before.json' if restored else 'qualified-inputs.json')).read_text())
    if inventory(ROOT) != expected: raise ValueError('Exact qualified inputs changed')
    settings = (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
    product = 'Dungeon Lord' if restored else PRODUCT
    if '  productName: ' + product + '\n' not in settings: raise ValueError('Expected product identity lost')
    report = dict(passed=True, inputs=len(expected), product=PRODUCT, save=str(SAVE),
                  settings_restored=restored, owner_proposal_unchanged=True,
                  other_save_namespaces_unchanged=True, original_incomplete_evidence_unchanged=True)
    write(Path(stage) / 'preservation.json', report)
    print(json.dumps(report))


def restore(stage):
    verify_protected()
    qualified = json.loads((PREP / 'qualified-inputs.json').read_text())
    before = json.loads((PREP / 'inputs-before.json').read_text())
    if inventory(ROOT) != qualified: raise ValueError('Unexpected inputs before restoration')
    with zipfile.ZipFile(PREP / 'original-settings.zip') as archive:
        originals = {name: archive.read(name) for name in SETTINGS}
    for name, data in originals.items():
        if hashlib.sha256(data).hexdigest() != before[name]: raise ValueError('Original settings backup mismatch')
    for name, data in originals.items(): (ROOT / name).write_bytes(data)
    if inventory(ROOT) != before: raise ValueError('Exact original inputs were not restored')
    write(Path(stage) / 'restoration.json', dict(passed=True, exact_original_settings={name: before[name] for name in SETTINGS}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['prepare', 'seal', 'preflight', 'finish', 'run-test', 'restore'])
    parser.add_argument('--stage')
    parser.add_argument('--gate', choices=[row[0] for row in TEST_GATES])
    args = parser.parse_args()
    if args.mode == 'prepare': prepare()
    elif args.mode == 'seal': seal()
    elif args.mode == 'run-test': sys.exit(run_test(args.gate, args.stage))
    elif args.mode == 'restore': restore(args.stage)
    else: check(args.stage, restored=args.mode == 'finish')
