"""Preserving B2 qualification in the existing disposable Unity checkout."""
import argparse, json, sys, zipfile, uuid, re, xml.etree.ElementTree as ET
from pathlib import Path
from collections import Counter
sys.dont_write_bytecode = True
import phase_a_qualification as a

a.q.NAMESPACE = 'living-dungeon-phase-b2-validation.json'
PREFIX = 'phase-b2-'
EVIDENCE = a.ROOT / 'Docs/testing/evidence/living-dungeon-phase-b2'

def audit():
    savedir = Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord')
    files = {}
    for p in savedir.iterdir():
        if p.is_file() and p.name not in ('Player.log', 'Player-prev.log', 'TestResults.xml') and not p.name.startswith(a.q.NAMESPACE):
            files[str(p)] = dict(bytes=p.stat().st_size, sha256=a.sha(p))
    for p in (a.PROJECT/'Builds').rglob('*'):
        if p.is_file() and 'Windows-living-dungeon-phase-b2' not in str(p):
            files[str(p)] = dict(bytes=p.stat().st_size, sha256=a.sha(p))
    return files

def prepare(name):
    result = a.q.compare(a.ROOT, a.NAME)
    a.write(a.OUT/(PREFIX+name+'-before-inputs.json'), result)
    if result['failures']:
        with zipfile.ZipFile(a.OUT/(PREFIX+name+'-preserved-input-differences.zip'), 'w', zipfile.ZIP_DEFLATED) as z:
            for row in result['files']:
                p = a.PROJECT/row['path']
                if not row['equal'] and p.is_file(): z.write(p, row['path'])
    result = a.q.prepare(a.ROOT, a.NAME)
    a.write(a.OUT/(PREFIX+name+'-inputs.json'), result)
    if result['failures']: raise ValueError(result['failures'])
    print(name, result['expected_count'], 'exact disposable inputs')

def initial():
    # No owner-save loading. Retain byte snapshots of disposable fixtures that old PlayMode tests may overwrite.
    before = audit(); a.write(a.OUT/(PREFIX+'owner-before.json'), before)
    backup = a.ROOT/'Temp/phase-b2-preserved-test-saves'
    backup.mkdir(parents=True, exist_ok=True)
    for name, record in before.items():
        p = Path(name)
        if p.parent == Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord'):
            dest = backup/p.name
            if dest.exists() and a.sha(dest) != record['sha256']: raise ValueError('Existing backup differs: '+str(dest))
            if not dest.exists(): dest.write_bytes(p.read_bytes())
    print('Preserved', len(before), 'existing saves/build artifacts')

def metadata():
    paths = [a.ROOT/'Assets/_Project/Scripts/Gameplay/DungeonSpatial'/n for n in
        ('LivingDungeonContracts.cs','LivingDungeonScheduling.cs','SchemaThirteenToFourteenUpgrade.cs')]
    paths += [a.ROOT/'Assets/_Project/Data/Production/Save/living_dungeon_qualification_profile.json',
        a.ROOT/'Assets/_Project/Resources/save_spatial_migration_limits_schema13.json',
        a.ROOT/'Assets/_Project/Tests/EditMode/LivingDungeonPhaseB2Tests.cs',
        a.ROOT/'Assets/_Project/Tests/PlayMode/LivingDungeonPhaseB2PlayModeTests.cs',
        a.ROOT/'Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonPhaseB2EditModeFixtures.cs']
    for p in paths:
        m = Path(str(p)+'.meta')
        if not m.exists():
            importer = 'MonoImporter' if p.suffix == '.cs' else 'TextScriptImporter'
            suffix = '\n  externalObjects: {}\n' + ('  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n' if p.suffix == '.cs' else '') + '  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
            m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+importer+':'+suffix)

def redact_log(content):
    content = re.sub(r'(?i)(-accessToken\s+)\S+', r'\1[redacted]', content)
    content = re.sub(r'(?i)(authorization[^\n]*bearer\s+)\S+', r'\1[redacted]', content)
    return re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',
        r'\1[redacted]', content)

def archive_log(name):
    p = a.OUT/(PREFIX+'windows-build.log') if name == 'windows-build' else Path('C:/Users/gdg34/AppData/Local/Unity/Editor/Editor.log')
    content = redact_log(p.read_text(errors='replace'))
    (a.OUT/(PREFIX+name+'-editor.log')).write_bytes(content.encode())

def restore_fixtures():
    before = json.loads((a.OUT/(PREFIX+'owner-before.json')).read_text()); now = audit()
    changed = [Path(p) for p,v in before.items() if now.get(p) != v]
    savedir = Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord')
    backup = a.ROOT/'Temp/phase-b2-preserved-test-saves'
    for p in changed:
        allowed = ((p.name.startswith('bootstrap_overlay_') and p.name.endswith('_test.json')) or
            (p.name.startswith('run_sim_test_') and p.name.endswith('.json')) or p.name == 'mvp_canonical_journey_test.json')
        if p.parent.resolve() != savedir.resolve() or not allowed:
            raise ValueError('Protected owner/player changed; no automatic restoration: '+str(p))
        a.q.no_redirect(p)
        if a.sha(backup/p.name) != before[str(p)]['sha256']: raise ValueError('Backup hash mismatch: '+str(p))
    with zipfile.ZipFile(a.OUT/(PREFIX+'changed-disposable-test-saves.zip'), 'w', zipfile.ZIP_DEFLATED) as z:
        for p in changed:
            if p.is_file(): z.write(p,p.name)
    for p in changed: p.write_bytes((backup/p.name).read_bytes())
    now = audit(); assert all(now.get(p)==v for p,v in before.items())
    a.write(a.OUT/(PREFIX+'owner-preservation.json'), dict(unchanged=True, verified_files=len(before),
        restored_disposable_fixture_files=[p.name for p in changed], primary_restored=False, manual_uat_restored=False, before=before))
    print('Exact preservation of',len(before),'entries; restored only',len(changed),'disposable fixture files')

def collect():
    EVIDENCE.mkdir(parents=True, exist_ok=True); summaries=[]
    with zipfile.ZipFile(EVIDENCE/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(a.OUT.glob(PREFIX+'*.xml')):
            root = ET.parse(p).getroot(); z.write(p,p.name)
            summaries.append(dict(file=p.name,sha256=a.sha(p),attributes=root.attrib,
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace'))
                    for c in root.iter('test-case') if c.get('result')=='Failed'],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in root.iter('test-case')
                    if c.get('result') in ('Skipped','Inconclusive')],
                measurements=[dict(name=c.get('fullname'),output=c.findtext('output')) for c in root.iter('test-case')
                    if c.find('output') is not None and 'LivingDungeonPhaseB2' in c.get('fullname','')]))
    a.write(EVIDENCE/'report-summary.json', summaries)
    with zipfile.ZipFile(EVIDENCE/'input-checkpoints.zip','w',zipfile.ZIP_DEFLATED) as z:
        for pattern in (PREFIX+'*-inputs.json',PREFIX+'*-preserved-input-differences.zip'):
            for p in sorted(a.OUT.glob(pattern)): z.write(p,p.name)
    with zipfile.ZipFile(EVIDENCE/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(a.OUT.glob(PREFIX+'*-editor.log')):
            z.writestr(p.name, redact_log(p.read_text(errors='replace')))
    owner = a.OUT/(PREFIX+'owner-preservation.json')
    if owner.exists(): (EVIDENCE/'owner-preservation.json').write_bytes(owner.read_bytes())
    helper = a.OUT/(PREFIX+'input-helper-tests.txt')
    if helper.exists(): (EVIDENCE/'input-helper-tests.txt').write_bytes(helper.read_bytes())
    a.write(EVIDENCE/'baseline.json',dict(baseline='af464e05d63acfa73a57786888266d5901feac2f',previous_pr=234,
        phase_a_cases=38,phase_a_fixture_sha256=a.sha(a.ROOT/'Assets/_Project/Tests/EditMode/phase-a-baseline.txt'),regenerated=False))
    build = a.PROJECT/'Builds/Development/Windows-living-dungeon-phase-b2'
    for name in ('build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt'):
        if (build/name).exists():
            if name in ('build-messages.txt','packed-source-assets.txt'):
                (EVIDENCE/name).write_bytes(('\n'.join(line.rstrip() for line in (build/name).read_text().splitlines())+'\n').encode())
            else: (EVIDENCE/name).write_bytes((build/name).read_bytes())
    print('Collected',len(summaries),'qualification reports')

def regression_compare():
    previous = a.ROOT/'Docs/testing/evidence/living-dungeon-phase-b1/export-limit-correction-reports.zip'
    results = []
    with zipfile.ZipFile(previous) as archive:
        for mode, current in [('editmode','phase-b2-final-safeguard-full-editmode.xml'),
                ('playmode','phase-b2-final-ui-qualified-full-playmode.xml')]:
            old = ET.fromstring(archive.read('phase-b1-export-limit-full-'+mode+'.xml'))
            new = ET.parse(a.OUT/current).getroot()
            before = {c.get('fullname'):c.get('result') for c in old.iter('test-case')}
            after = {c.get('fullname'):c.get('result') for c in new.iter('test-case')}
            occurrences = lambda root: Counter((c.get('fullname'), c.get('result')) for c in root.iter('test-case'))
            skips = lambda root: Counter((c.get('fullname'), c.findtext('reason/message')) for c in root.iter('test-case')
                if c.get('result') in ('Skipped', 'Inconclusive'))
            row = dict(mode=mode, baseline_count=len(before), current_count=len(after),
                baseline_report=old.attrib, current_report=new.attrib, unchanged_skips=skips(old)==skips(new),
                removed_occurrences=[dict(name=n,result=r,count=count) for (n,r),count in sorted((occurrences(old)-occurrences(new)).items())],
                removed=sorted(set(before)-set(after)), added=sorted(set(after)-set(before)),
                changed=[dict(name=n,before=before[n],after=after[n]) for n in before.keys() & after.keys() if before[n]!=after[n]])
            if row['removed_occurrences'] or not row['unchanged_skips']:
                raise ValueError('Established baseline cases/skips changed: '+mode)
            results.append(row)
    a.write(EVIDENCE/'baseline-test-comparison.json', results)
    print(json.dumps([dict(mode=r['mode'],baseline=r['baseline_count'],current=r['current_count'],
        removed=len(r['removed']),added=len(r['added']),changed=len(r['changed'])) for r in results]))

if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('action', choices=['initial','metadata','prepare','compare','archive-log','restore-fixtures','collect','regression-compare']); p.add_argument('--name', default='checkpoint')
    args = p.parse_args()
    if args.action == 'initial': initial()
    elif args.action == 'metadata': metadata()
    elif args.action == 'prepare': prepare(args.name)
    elif args.action == 'archive-log': archive_log(args.name)
    elif args.action == 'restore-fixtures': restore_fixtures()
    elif args.action == 'collect': collect()
    elif args.action == 'regression-compare': regression_compare()
    else: print(json.dumps(a.q.compare(a.ROOT,a.NAME), indent=2))
