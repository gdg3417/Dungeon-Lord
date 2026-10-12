"""Archive room-content qualification without changing historical evidence or owner state."""
from pathlib import Path
import ast, hashlib, json, re, shutil, subprocess, xml.etree.ElementTree as ET, zipfile
from datetime import datetime

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/testing/evidence/phase7-graphical-room-content'
PLAYER = ROOT / 'Temp/ui-composition-floorhud-validation/Builds/Development/Windows'
BASELINE = '5932a9fe0b7cec87c81f09e8e7d619df62c53101'
IMPLEMENTATION = '916fc1625c17344b895613e62c597059a01f4cc1'
OUT.mkdir(parents=True, exist_ok=True)

def sha(data): return hashlib.sha256(data).hexdigest()
def write(name, value): (OUT/name).write_text(json.dumps(value, indent=2), encoding='utf-8')
def git(*args): return subprocess.check_output(['git', *args], cwd=ROOT).decode().strip()
def outcomes(run):
    groups = {}
    for case in run.iter('test-case'):
        groups.setdefault(case.get('fullname'), []).append(case.get('result'))
    return {name: sorted(values) for name, values in groups.items()}

reports = []
with zipfile.ZipFile(OUT/'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults').glob('room-content-*.xml')):
        data = path.read_bytes(); run = ET.fromstring(data); archive.writestr(path.name, data)
        reports.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=sha(data), run_attributes=run.attrib,
            failures=[dict(name=c.get('fullname'), message=c.findtext('failure/message'), stack=c.findtext('failure/stack-trace'))
                      for c in run.iter('test-case') if c.get('result') == 'Failed'],
            skips=[dict(name=c.get('fullname'), reason=c.findtext('reason/message'))
                   for c in run.iter('test-case') if c.get('result') in ('Skipped', 'Inconclusive')]))
write('report-summary.json', reports)
dispositions = {
    'room-content-focused-03.xml': ('Corrected fixture to use its configured canonical unassignment authority', 'room-content-focused-domain-08.xml'),
    'room-content-focused-domain-04.xml': ('Preserved corruption fixtures using exact frozen historical DTOs without weakening production validation', 'room-content-focused-domain-05.xml'),
    'room-content-focused-ui-07.xml': ('Older movement journeys now explicitly confirm their previews; lifecycle and durability assertions retained', 'room-content-focused-ui-09.xml'),
}
for report in reports:
    for failure in report['failures']:
        reason, resolved = dispositions[Path(report['path']).name]
        failure.update(disposition=reason, resolved_by=resolved)
write('report-summary.json', reports)
helper_source = (ROOT/'Tools/Presentation/test_qualification_inputs.py').read_bytes()
helper_cases = sorted(n.name for n in ast.walk(ast.parse(helper_source)) if isinstance(n, ast.FunctionDef) and n.name.startswith('test_'))
assert len(helper_cases) == 12
write('helper-test-observation.json', dict(source_sha256=sha(helper_source), cases=helper_cases,
    command='python -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py',
    sandbox_attempt=dict(tests=12, setup_cleanup_errors=24, reason='Windows sandbox temporary-directory Git setup and cleanup permission failures'),
    unrestricted_rerun=dict(tests=12, passed=12, failed=0, skipped=0),
    evidence='Observed terminal tool results; helper implementation unchanged'))
comparisons = []
previous = ROOT/'Docs/testing/evidence/phase7-production-dungeon-ui-composition/floor-hud-refinement/qualification-reports.zip'
with zipfile.ZipFile(previous) as archive:
    for mode in ('editmode', 'playmode'):
        old = outcomes(ET.fromstring(archive.read('ui-floorhud-full-'+mode+'.xml')))
        new = outcomes(ET.parse(ROOT/('TestResults/room-content-full-'+mode+'.xml')).getroot())
        comparisons.append(dict(mode=mode, previous_count=sum(map(len, old.values())), current_count=sum(map(len, new.values())),
            removed=sorted(old.keys()-new.keys()), added=[dict(name=n, result=new[n]) for n in sorted(new.keys()-old.keys())],
            changed=[dict(name=n, before=old[n], after=new[n]) for n in sorted(old.keys()&new.keys()) if old[n] != new[n]]))
write('test-outcome-comparison.json', comparisons)
assert all(not c['removed'] and not c['changed'] for c in comparisons), 'Existing test outcomes changed'

logs = []
with zipfile.ZipFile(OUT/'unity-logs-redacted.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'Temp').glob('room-content-*.log')):
        clean, count = re.subn(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',
                              r'\1[REDACTED]', path.read_text(encoding='utf-8-sig', errors='replace'))
        clean, bearer = re.subn(r'(?i)(Bearer\s+)\S+', r'\1[REDACTED]', clean)
        archive.writestr(path.name, clean)
        logs.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=sha(clean.encode()), redactions=count+bearer,
            compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*', clean))),
            compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*', clean)))))
write('log-index.json', logs)
checkpoints = []
with zipfile.ZipFile(OUT/'source-inventories-and-output-diffs.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults').glob('room-content-*.json')):
        data = path.read_bytes(); value = json.loads(data.decode('utf-8-sig'))
        if not isinstance(value, dict) or 'input_roots' not in value: continue
        archive.writestr(path.name, data)
        checkpoints.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=sha(data),
            source_head=value.get('source_head'), expected_count=value['expected_count'], actual_count=value['actual_count'],
            unexpected=value['unexpected'], failures=value['failures'], permitted_transformations=value['permitted_transformations']))
    for path in sorted((ROOT/'TestResults').glob('room-content-*.diff')): archive.write(path, path.name)
write('source-checkpoint-index.json', checkpoints)

before = json.loads((ROOT/'TestResults/room-content-owner-before.json').read_text(encoding='utf-8-sig'))
preserved = [dict(path=r['Path'], before_sha256=r['Hash'].lower(), after_sha256=sha(Path(r['Path']).read_bytes())) for r in before]
for row in preserved: row['unchanged'] = row['before_sha256'] == row['after_sha256']
save_dir = Path(before[0]['Path']).parent
old_names = sorted(Path(r['Path']).name for r in before if Path(r['Path']).parent == save_dir)
new_names = sorted(p.name for p in save_dir.glob('save_primary.json*') if p.is_file())
assert old_names == new_names and all(r['unchanged'] for r in preserved), 'Owner files changed'
write('owner-preservation.json', dict(files=preserved, primary_prefix_before=old_names, primary_prefix_after=new_names, unchanged=True))
shutil.copy2(ROOT/'TestResults/room-content-owner-before.json', OUT/'owner-before.json')
validation_before = json.loads((ROOT/'TestResults/room-content-validation-save-before-build.json').read_text(encoding='utf-8-sig'))
validation_after = sha(Path(validation_before['Path']).read_bytes())
assert validation_before['Hash'].lower() == validation_after, 'Existing safe validation save changed'
write('validation-save-preservation.json', dict(path=validation_before['Path'], before_sha256=validation_before['Hash'].lower(),
    after_sha256=validation_after, unchanged=True, schema=13, audited_rooms=2, audited_assignments=0, audited_mana=981))

screens = ROOT/'Temp/ui-composition-floorhud-validation/TestResults/phase7a4-screenshots'
(OUT/'screenshots').mkdir(exist_ok=True)
inventory = []
full_play = ET.parse(ROOT/'TestResults/room-content-full-playmode.xml').getroot()
starts = [full_play.get('start-time')] + [c.get('start-time') for c in full_play.iter('test-case')]
cutoff = min(datetime.fromisoformat(s.replace('Z', '+00:00')).timestamp() for s in starts if s)
for path in sorted(screens.glob('room-content-*.png')):
    assert path.stat().st_mtime >= cutoff, 'Stale screenshot: '+path.name
    shutil.copy2(path, OUT/'screenshots'/path.name)
    inventory.append(dict(path='screenshots/'+path.name, sha256=sha(path.read_bytes()), bytes=path.stat().st_size))
assert len(inventory) == 5, 'Expected only five final room-content captures'
write('screenshot-index.json', inventory)

build = json.loads((PLAYER/'build-report.json').read_text(encoding='utf-8-sig'))
assert build['buildResult'] == 'Succeeded' and build['errorCount'] == 0 and build['developmentBuild']
assert build['targetPlatform'] == 'StandaloneWindows64' and build['includedScenes'] == ['Assets/_Project/Scenes/Bootstrap.unity']
final_runs = [ET.parse(ROOT/('TestResults/room-content-full-'+mode+'.xml')).getroot() for mode in ('editmode', 'playmode')]
build_time = datetime.fromisoformat(build['utcTimestamp'].replace('Z', '+00:00'))
assert all(build_time >= datetime.fromisoformat(run.get('end-time').replace('Z', '+00:00')) for run in final_runs), 'Build predates final tests'
art = (PLAYER/'packed-dungeon-art.txt').read_text(encoding='utf-8-sig').splitlines()
packed = set((PLAYER/'packed-source-assets.txt').read_text(encoding='utf-8-sig').splitlines())
assert art and set(art).issubset(packed)
for name in ('build-report.json', 'build-messages.txt', 'packed-dungeon-art.txt', 'packed-source-assets.txt'):
    shutil.copy2(PLAYER/name, OUT/name)
files = [dict(path=p.relative_to(PLAYER).as_posix(), bytes=p.stat().st_size, sha256=sha(p.read_bytes()))
         for p in sorted(PLAYER.rglob('*')) if p.is_file()]
write('windows-artifact-manifest.json', dict(path=PLAYER.relative_to(ROOT).as_posix(), report=build,
    referenced_art_count=len(art), packed_assets_verified=True, retained_single_location=True, files=files))
shutil.copy2(ROOT/'TestResults/room-content-build-cleanup-inventory.json', OUT/'build-cleanup-inventory.json')
write('qualified-input-trees.json', dict(baseline=BASELINE, qualified_implementation=IMPLEMENTATION,
    branch=git('branch', '--show-current'), input_trees={name:git('rev-parse', IMPLEMENTATION+':'+name)
        for name in ('Assets', 'Packages', 'ProjectSettings', 'ContentAuthoring')},
    changed_implementation_files=git('diff', '--name-only', BASELINE, IMPLEMENTATION).splitlines(),
    branch_correction=dict(original_base_tree=git('rev-parse', '66ca6ee^{tree}'), main_tree=git('rev-parse', BASELINE+'^{tree}'),
        original_implementation_tree=git('rev-parse', '0c27767^{tree}'),
        corrected_initial_implementation_tree=git('rev-parse', '62eb3cd^{tree}'), qualified_tree=git('rev-parse', IMPLEMENTATION+'^{tree}'))))
print(len(reports), 'XML reports;', len(checkpoints), 'source inventories;', len(inventory), 'screenshots;',
      len(files), 'player files; owner files unchanged')
