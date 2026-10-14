"""Exact disposable replay inputs and preservation audit; never restore owner saves."""
from pathlib import Path
import hashlib, json, sys, shutil, uuid, zipfile, re, xml.etree.ElementTree as ET
import qualification_inputs as inputs

ROOT = Path(__file__).resolve().parents[2]
NAME = 'ui-composition-visual-replay-validation'
OUT = ROOT/'TestResults/visual-replay'
SAVE = Path.home()/'AppData/LocalLow/gdg3417/Dungeon Lord'
inputs.NAMESPACE = 'phase7-visual-replay-validation.json'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def protected():
    files = {p for p in SAVE.rglob('*') if p.is_file() and p.suffix not in ('.log', '.xml')}
    for base in [ROOT/'Builds', *ROOT.glob('Temp/*/Builds')]:
        if base.exists(): files.update(p for p in base.rglob('*') if p.is_file())
    return sorted(files)
def write(name, value):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/name).write_text(json.dumps(value, indent=2), encoding='utf-8')
def prepare():
    for path in inputs.source_inventory(ROOT):
        if path.endswith('.cs') and not (ROOT/(path+'.meta')).exists():
            (ROOT/(path+'.meta')).write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8')
    # Static established test filenames are disposable; backup only these before suite writes.
    fixtures = [p for p in SAVE.iterdir() if p.is_file() and
        (p.name.startswith(('bootstrap_overlay_', 'run_sim_test_')) or p.name == 'mvp_canonical_journey_test.json')]
    for path in fixtures:
        target = OUT/'preexisting-test-fixtures'/path.name
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists(): shutil.copy2(path, target)
    if not (OUT/'preservation-before.json').exists():
        write('preservation-before.json', [dict(path=str(p), sha256=sha(p), fixture=p in fixtures) for p in protected()])
    result = inputs.prepare(ROOT, NAME); write('inputs-prepared.json', result)
    print(result['expected_count'], 'exact inputs prepared')
def check():
    result = inputs.compare(ROOT, NAME); write('inputs-current.json', result)
    print(result['expected_count'], 'inputs;', len(result['failures']), 'failures')
    for failure in result['failures']: print(failure)
    return bool(result['failures'])
def audit():
    rows = json.loads((OUT/'preservation-before.json').read_text())
    for row in rows:
        p = Path(row['path']); row['current_sha256'] = sha(p) if p.exists() else None
        row['equal'] = row['sha256'] == row['current_sha256']
    write('preservation-after.json', rows)
    changed = [r for r in rows if not r['equal']]
    print(len(rows), 'protected files;', len(changed), 'changed')
    for row in changed: print(row['path'])
    return bool(changed)
def checkpoint(label):
    result = inputs.compare(ROOT, NAME); write('inputs-'+label+'.json', result)
    if result['unexpected']: raise ValueError('Unexpected input; preserve and inspect: '+str(result['unexpected']))
    changed = [row['path'] for row in result['files'] if not row['equal']]
    allowed = lambda name: name in ('ProjectSettings/ProjectSettings.asset', 'ProjectSettings/UnityConnectSettings.asset',
        'Assets/Settings/UniversalRP.asset', 'Assets/UniversalRenderPipelineGlobalSettings.asset') or (
        name.endswith('.asset') and (name.startswith('Assets/TextMesh Pro/') or 'UniversalRenderPipelineGlobalSettings' in name))
    if any(not allowed(name) for name in changed): raise ValueError('Inspect changed input before refresh: '+str(changed))
    if changed:
        with zipfile.ZipFile(OUT/('generated-inputs-'+label+'.zip'), 'w', zipfile.ZIP_DEFLATED) as archive:
            for name in changed:
                target = ROOT/'Temp'/NAME/name
                archive.write(target, name)
                target.write_bytes(inputs.transformed(name, (ROOT/name).read_bytes()))
    final = inputs.compare(ROOT, NAME); write('inputs-'+label+'-exact.json', final)
    if final['failures']: raise ValueError(str(final['failures']))
    print(final['expected_count'], 'exact inputs;', len(changed), 'generated differences archived/refreshed')
def restore_test_fixtures():
    rows = json.loads((OUT/'preservation-before.json').read_text())
    restored = []
    for row in rows:
        if not row['fixture']: continue
        path = Path(row['path'])
        if path.resolve().parent != SAVE.resolve() or not (path.name.startswith(('bootstrap_overlay_', 'run_sim_test_')) or path.name == 'mvp_canonical_journey_test.json'):
            raise ValueError('Not an approved disposable fixture: '+str(path))
        inputs.no_redirect(path)
        backup = OUT/'preexisting-test-fixtures'/path.name
        if sha(backup) != row['sha256']: raise ValueError('Fixture backup mismatch')
        if path.exists() and sha(path) == row['sha256']: continue
        if path.exists():
            archive = OUT/'test-modified-fixtures'/path.name
            archive.parent.mkdir(parents=True, exist_ok=True)
            if archive.exists(): raise ValueError('Preserve existing modified-fixture archive')
            shutil.copy2(path, archive)
        path.write_bytes(backup.read_bytes()); restored.append(path.name)
    write('restored-test-fixtures.json', restored)
    print(len(restored), 'preexisting disposable test fixtures restored; owner saves never restored')
def collect():
    target = ROOT/'Docs/testing/evidence/phase7-visual-run-replay'
    target.mkdir(parents=True, exist_ok=True)
    reports = []
    with zipfile.ZipFile(target/'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted((ROOT/'TestResults').glob('visual-replay*.xml')):
            data = path.read_bytes(); run = ET.fromstring(data); archive.writestr(path.name, data)
            reports.append(dict(file=path.name, sha256=hashlib.sha256(data).hexdigest(), attributes=run.attrib,
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace')) for c in run.iter('test-case') if c.get('result')=='Failed'],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')],
                measurements=[dict(name=c.get('fullname'),output=c.findtext('output')) for c in run.iter('test-case') if 'Pure playback:' in (c.findtext('output') or '')]))
    (target/'report-summary.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
    with zipfile.ZipFile(target/'qualification-support.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(OUT.glob('*')):
            if path.is_file(): archive.write(path, path.name)
        helper = ROOT/'TestResults/visual-replay-input-helper-tests.txt'
        if helper.exists(): archive.write(helper, helper.name)
        for path in sorted((ROOT/'Temp').glob('visual-replay*.log')):
            log = path.read_text(encoding='utf-8-sig', errors='replace')
            log = re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]', log)
            log = re.sub(r'(?i)(Bearer\s+)[A-Za-z0-9._~-]+', r'\1[REDACTED]', log)
            archive.writestr('logs/'+path.name, log)
        for path in sorted((ROOT/'Temp/ui-composition-visual-replay-owner-uat/Logs').glob('build-*.log')):
            log = path.read_text(encoding='utf-8-sig', errors='replace')
            log = re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',log)
            log = re.sub(r'(?i)(Bearer\s+)[A-Za-z0-9._~-]+',r'\1[REDACTED]',log)
            archive.writestr('logs/'+path.name,log)
    screenshots = sorted((ROOT/'Temp'/NAME/'TestResults/phase7a4-screenshots').glob('composition-visual-replay*.png'))
    with zipfile.ZipFile(target/'screenshots.zip', 'w', zipfile.ZIP_STORED) as archive:
        for path in screenshots: archive.write(path, path.name)
    (target/'screenshots.json').write_text(json.dumps([dict(file=p.name,sha256=sha(p),bytes=p.stat().st_size) for p in screenshots],indent=2),encoding='utf-8')
    for mode in ('editmode','playmode'):
        path = ROOT/('TestResults/visual-replay-full-'+mode+'.xml')
        if not path.exists(): continue
        run = ET.parse(path).getroot()
        if run.get('failed') != '0': raise ValueError('Full suite has failures')
        with zipfile.ZipFile(ROOT/'Docs/testing/evidence/living-dungeon-phase-b2/alias-correction-reports.zip') as archive:
            old = ET.fromstring(archive.read('phase-b2-alias-full-'+mode+'.xml'))
        def cases(report):
            result = {}
            for case in report.iter('test-case'):
                result.setdefault(case.get('fullname'),[]).append((case.get('result'),case.findtext('reason/message')))
            return {k:sorted(v) for k,v in result.items()}
        before, after = cases(old), cases(run)
        comparison = dict(removed=sorted(before.keys()-after.keys()),added=sorted(after.keys()-before.keys()),
            changed=[dict(name=k,before=before[k],after=after[k]) for k in sorted(before.keys()&after.keys()) if before[k]!=after[k]])
        (target/('baseline-'+mode+'.json')).write_text(json.dumps(comparison,indent=2),encoding='utf-8')
        if comparison['removed'] or comparison['changed']: raise ValueError('Existing baseline outcomes changed')
    print(len(reports), 'reports;', len(screenshots), 'replay screenshots archived')
def build_evidence():
    build = ROOT/'Temp/ui-composition-visual-replay-owner-uat/Builds/Development/Windows-visual-run-replay'
    target = ROOT/'Docs/testing/evidence/phase7-visual-run-replay'
    for name in ('build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt'):
        shutil.copy2(build/name, target/name)
    report = json.loads((build/'build-report.json').read_text())
    if report['buildResult'] != 'Succeeded' or report['errorCount'] != 0 or not report['developmentBuild']: raise ValueError('Build qualification failed')
    files = sorted(p for p in build.rglob('*') if p.is_file())
    art = (build/'packed-dungeon-art.txt').read_text().splitlines()
    namespace = 'dungeon-lord-visual-run-manual-uat-20261014.json'
    packed_namespace = namespace.encode() in (build/'Dungeon Lord_Data/sharedassets0.assets').read_bytes()
    if len(art) != 52 or not packed_namespace: raise ValueError('Packed art/namespace verification failed')
    summary = dict(executable=str(build/'Dungeon Lord.exe'),report=report,files=len(files),bytes=sum(p.stat().st_size for p in files),
        required_art_assets=len(art),namespace=namespace,namespace_in_sharedassets=packed_namespace,player_launched=False,
        inventory=[dict(path=p.relative_to(build).as_posix(),bytes=p.stat().st_size,sha256=sha(p)) for p in files])
    write('build-verification.json', summary)
    (target/'build-verification.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(report['buildResult'],len(files),'files;',len(art),'required art assets; isolated namespace packed')
if __name__ == '__main__':
    mode = sys.argv[1]
    if mode.startswith('owner-'):
        inputs.NAMESPACE = 'dungeon-lord-visual-run-manual-uat-20261014.json'
        NAME = 'ui-composition-visual-replay-owner-uat'
    if mode == 'prepare': prepare()
    elif mode == 'check': sys.exit(check())
    elif mode == 'audit': sys.exit(audit())
    elif mode == 'checkpoint': checkpoint(sys.argv[2])
    elif mode == 'restore-test-fixtures': restore_test_fixtures()
    elif mode == 'collect': collect()
    elif mode == 'owner-prepare':
        result = inputs.prepare(ROOT, NAME); write('inputs-owner-prepared.json', result)
        print(result['expected_count'], 'exact owner-UAT project inputs prepared; save not opened')
    elif mode == 'owner-checkpoint': checkpoint(sys.argv[2])
    elif mode == 'build-evidence': build_evidence()
