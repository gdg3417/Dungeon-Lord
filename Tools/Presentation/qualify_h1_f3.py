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
PREP = ROOT / 'TestResults/h1-f3-preparation'
PRODUCT = 'Dungeon Lord B3B2 Smoke H1F3 20261026'
SAVE = Path.home() / 'AppData/LocalLow/gdg3417' / PRODUCT
OWNER = Path('C:/Dev/Dungeon-Lord')


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
                             if p.is_dir() and p.name != PRODUCT})


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
    settings = ROOT / 'ProjectSettings/ProjectSettings.asset'
    original = settings.read_bytes()
    (PREP / 'ProjectSettings.asset.original').write_bytes(original)
    text = original.decode('utf-8-sig')
    if text.count('  productName: Dungeon Lord\n') != 1 and text.count('  productName: Dungeon Lord\r\n') != 1:
        raise ValueError('Unexpected product identity')
    settings.write_bytes(text.replace('  productName: Dungeon Lord', '  productName: ' + PRODUCT).encode('utf-8'))
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
    cli = Path('C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe')
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
    selections = [
        ('focused', 'H1F3;H1F2;FrozenSchemaFifteen', 188, []),
        ('integration', r'R0Research;LivingDungeonB3B2\.;LivingDungeonTwoFloor\.;PhaseFourPassiveOnlineMana;PhaseFourOfflinePassiveMana;PhaseFourOfflineClock;PhaseFourOfflineSaveLifecycle;PhaseFourAcquisition;PhaseFourEconomy', 436, []),
        ('migration', 'FrozenSchemaFifteenMigrationRegression;PhaseSevenA2WindowsSpatialMigration;PhaseFourSessions;PhaseFourWrites;PhaseFourLoad;PhaseFourCompleteSave;PhaseFourSaveSemantics', 224,
         ['DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed'])]
    previous = []
    for name, pattern, count, skips in selections:
        pre = name + '-preflight'
        stages.append(dict(id=pre, role='preflight', depends=previous,
            command=[sys.executable, '-B', str(helper), 'preflight', '--stage', '{campaign}/' + pre],
            timeout_seconds=120, artifacts=['preservation.json']))
        stages.append(dict(id=name, role='test', depends=[pre],
            command=[str(cli), 'test', '{project}', '--mode', 'EditMode', '--filter', pattern,
                     '--output', '{stage}/results.xml', '--timeout', '900', '--no-color',
                     '--', '-logFile', '{stage}/unity.log'],
            timeout_seconds=960, xml='results.xml', expected_tests=count, allowed_skips=skips,
            artifacts=['unity.log']))
        previous = [name]
    stages.append(dict(id='preservation', role='final', depends=[s['id'] for s in stages],
        command=[sys.executable, '-B', str(helper), 'finish', '--stage', '{stage}'],
        timeout_seconds=120, artifacts=['preservation.json']))
    plan = dict(purpose='qualification',
        source_sha=subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
        tools=tools, isolation=dict(product=PRODUCT, save=str(SAVE / 'save_primary.json'), project=str(ROOT)),
        approved_substitutions=['Temporary isolated productName only; initial import normalized two settings files to LF. Exact full input archive sealed after review. No runtime source/config/test substitutions.'],
        stages=stages, selections=dict(required=[s['id'] for s in stages]))
    write(PREP / 'plan.json', plan)


def check(stage):
    verify_protected()
    expected = json.loads((PREP / 'qualified-inputs.json').read_text())
    if inventory(ROOT) != expected: raise ValueError('Exact qualified inputs changed')
    settings = (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
    if '  productName: ' + PRODUCT not in settings: raise ValueError('Product isolation lost')
    report = dict(passed=True, inputs=len(expected), product=PRODUCT, save=str(SAVE),
                  owner_proposal_unchanged=True, other_save_namespaces_unchanged=True)
    write(Path(stage) / 'preservation.json', report)
    print(json.dumps(report))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['prepare', 'seal', 'preflight', 'finish'])
    parser.add_argument('--stage')
    args = parser.parse_args()
    if args.mode == 'prepare': prepare()
    elif args.mode == 'seal': seal()
    else: check(args.stage)
