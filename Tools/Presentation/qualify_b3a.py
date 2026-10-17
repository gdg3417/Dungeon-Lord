"""Qualify in the existing project. Preserve inputs, saves, evidence and players."""
from pathlib import Path
import hashlib, json, shutil, subprocess, sys, uuid, zipfile
import io
import qualification_inputs as inputs

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'TestResults/living-dungeon-b3a-20261017'
SAVE = Path.home() / 'AppData/LocalLow/gdg3417/Dungeon Lord'
NAMESPACE = 'living-dungeon-b3a-qualification-20261017.json'
MUTABLE = [inputs.CONFIG, inputs.SERVICE, 'ProjectSettings/ProjectSettings.asset',
           'ProjectSettings/UnityConnectSettings.asset', 'Assets/Settings/UniversalRP.asset',
           'Assets/UniversalRenderPipelineGlobalSettings.asset',
           'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset']
OUTPUT_TESTS = ['Assets/_Project/Editor/DungeonSpatial/Tests/CombatC0ProductionEvidenceSceneTests.cs',
                'Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonFloorSceneTests.cs',
                'Assets/_Project/Scripts/BuildTools/ProductionDungeonScreenshots.cs']
FIXTURE_TESTS = ['Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs',
                 'Assets/_Project/Tests/EditMode/MvpCanonicalJourneyIntegrationTests.cs',
                 'Assets/_Project/Tests/EditMode/RunSimulationTests.cs']

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(name, value):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/name).write_text(json.dumps(value, indent=2), encoding='utf-8')
def protected():
    paths = {p for p in SAVE.rglob('*') if p.is_file() and p.suffix not in ('.log', '.xml')}
    for folder in [ROOT/'Builds', *ROOT.glob('Temp/*/Builds')]:
        paths.update(p for p in folder.rglob('*') if p.is_file())
    return sorted(paths)
def prepare():
    if (OUT/'baseline.json').exists(): raise ValueError('Preserve the existing baseline')
    OUT.mkdir(parents=True, exist_ok=True)
    for p in ROOT.glob('Assets/**/*.cs'):
        if not Path(str(p)+'.meta').exists():
            Path(str(p)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8')
    inventory = inputs.source_inventory(ROOT)
    with zipfile.ZipFile(OUT/'root-inputs-before.zip', 'x', zipfile.ZIP_DEFLATED) as z:
        for name in inventory: z.write(ROOT/name, name)
    fixtures = [p for p in SAVE.iterdir() if p.is_file() and
                (p.name.startswith(('bootstrap_overlay_', 'run_sim_test_')) or p.name == 'mvp_canonical_journey_test.json')]
    with zipfile.ZipFile(OUT/'test-fixtures-before.zip', 'x', zipfile.ZIP_DEFLATED) as z:
        for p in fixtures: z.write(p, p.name)
    write('baseline.json', dict(commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
        inputs=[dict(path=n,sha256=sha(ROOT/n)) for n in inventory],
        protected=[dict(path=str(p),sha256=sha(p),fixture=p in fixtures) for p in protected()]))
    inputs.NAMESPACE = NAMESPACE
    for name in [inputs.CONFIG, inputs.SERVICE]:
        (ROOT/name).write_bytes(inputs.transformed(name, (ROOT/name).read_bytes()))
    print(len(inventory), 'inputs backed up; unique namespace installed;',len(protected()),'protected files')
def checkpoint(label):
    if not label.replace('-','').isalnum(): raise ValueError('Unsafe label')
    baseline=json.loads((OUT/'baseline.json').read_text())
    changed=[]
    with zipfile.ZipFile(OUT/('generated-'+label+'.zip'), 'x', zipfile.ZIP_DEFLATED) as z:
        with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source:
            for row in baseline['inputs']:
                name=row['path']; original=source.read(name)
                if name not in MUTABLE: continue
                if name in (inputs.CONFIG,inputs.SERVICE):
                    inputs.NAMESPACE=NAMESPACE; original=inputs.transformed(name, original)
                current=(ROOT/name).read_bytes()
                if current == original: continue
                z.writestr(name,current); (ROOT/name).write_bytes(original); changed.append(name)
    write('inputs-'+label+'.json', [dict(path=n,sha256=sha(ROOT/n)) for n in inputs.source_inventory(ROOT)])
    captures=OUT/'outputs'; retained=OUT/('outputs-'+label)
    if captures.exists():
        inputs.no_redirect(captures); inputs.no_redirect(retained)
        if captures.resolve().parent!=OUT.resolve() or retained.resolve().parent!=OUT.resolve() or retained.exists():
            raise ValueError('Unsafe or existing capture retention target')
        captures.rename(retained)
    print('Archived/restored generated inputs:',changed)
def applicability(label):
    tree=subprocess.check_output(['git','ls-tree','-r','-z','HEAD','--',*inputs.INPUT_ROOTS],cwd=ROOT)
    entries=[]
    for row in filter(None,tree.split(b'\0')):
        header,name=row.split(b'\t',1); entries.append((name.decode(),header.split()[2]))
    blob_stream=io.BytesIO(subprocess.check_output(['git','cat-file','--batch'],cwd=ROOT,
                         input=b'\n'.join(sha for name,sha in entries)+b'\n'))
    expected={}
    for name,blob in entries:
        size=int(blob_stream.readline().split()[2]); expected[name]=blob_stream.read(size); blob_stream.read(1)
    actual=inputs.source_inventory(ROOT); rows=[]; failures=[]
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as original:
        for name in actual:
            data=expected.get(name)
            if data is None: failures.append(name+': uncommitted input'); continue
            if name in MUTABLE[2:]: data=original.read(name)
            if name in (inputs.CONFIG,inputs.SERVICE): inputs.NAMESPACE=NAMESPACE; data=inputs.transformed(name,data)
            if name in OUTPUT_TESTS and (OUT/'historical-evidence-before.json').exists():
                data=data.decode('utf-8-sig').replace('TestResults/','TestResults/living-dungeon-b3a-20261017/outputs/').encode()
            if name in FIXTURE_TESTS and (OUT/'fixture-substitutions.json').exists():
                text=data.decode('utf-8-sig')
                for old,new in [('bootstrap_overlay_','bootstrap_overlay_b3a_20261017_'),('run_sim_test_','run_sim_test_b3a_20261017_'),
                                ('mvp_canonical_journey_test.json','mvp_canonical_journey_b3a_20261017.json')]: text=text.replace(old,new)
                data=text.encode()
            left=inputs.normalized(name,data); right=inputs.normalized(name,(ROOT/name).read_bytes())
            rows.append(dict(path=name,equal=left==right,expected=hashlib.sha256(left).hexdigest(),actual=hashlib.sha256(right).hexdigest()))
            if left!=right: failures.append(name+': mismatch')
    failures.extend(name+': missing' for name in expected if name not in actual)
    write('applicability-'+label+'.json',dict(commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
          files=rows,failures=failures,preservedLocalAssets=MUTABLE[2:]))
    if failures: raise ValueError(str(failures))
    print(len(rows),'Unity inputs match committed source plus documented save/evidence substitutions and five preserved local assets')
def isolate_evidence():
    if (OUT/'historical-evidence-before.json').exists(): raise ValueError('Evidence namespace already installed')
    rows=[dict(path=str(p),sha256=sha(p)) for p in (ROOT/'TestResults').rglob('*')
          if p.is_file() and OUT not in p.parents]
    write('historical-evidence-before.json',rows)
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source:
        for name in OUTPUT_TESTS:
            original=source.read(name)
            if (ROOT/name).read_bytes()!=original: raise ValueError('Unexpected test source change')
            text=original.decode('utf-8-sig').replace('TestResults/', 'TestResults/living-dungeon-b3a-20261017/outputs/')
            (ROOT/name).write_bytes(text.encode('utf-8'))
    print(len(rows),'historical evidence files hashed; test captures redirected to the unique B3A directory')
def isolate_fixtures():
    if (OUT/'fixture-substitutions.json').exists(): raise ValueError('Fixture namespaces already installed')
    rows=[]
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source:
        for name in FIXTURE_TESTS:
            original=source.read(name)
            if (ROOT/name).read_bytes()!=original: raise ValueError('Unexpected fixture test source change')
            text=original.decode('utf-8-sig')
            for old,new in [('bootstrap_overlay_','bootstrap_overlay_b3a_20261017_'),
                            ('run_sim_test_','run_sim_test_b3a_20261017_'),
                            ('mvp_canonical_journey_test.json','mvp_canonical_journey_b3a_20261017.json')]:
                text=text.replace(old,new)
            (ROOT/name).write_bytes(text.encode('utf-8')); rows.append(dict(path=name,sha256=sha(ROOT/name)))
    write('fixture-substitutions.json',rows)
    print('Static disposable fixture names redirected; original save files will not be used')
def normalize_fixture_sources():
    # Initial Windows write_text translated CRLF a second time. Preserve that exact
    # qualification input, then remove the extra CR whitespace before later stages.
    with zipfile.ZipFile(OUT/'temporary-test-sources-initial.zip','x',zipfile.ZIP_DEFLATED) as z:
        for name in OUTPUT_TESTS+FIXTURE_TESTS:
            data=(ROOT/name).read_bytes(); z.writestr(name,data)
            (ROOT/name).write_bytes(data.replace(b'\r\r\n',b'\r\n'))
    print('Initial transformed test inputs retained; duplicate CR whitespace corrected')
def isolate_shared_capture():
    name=OUTPUT_TESTS[-1]
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source: original=source.read(name)
    if (ROOT/name).read_bytes()!=original: raise ValueError('Unexpected shared screenshot helper change')
    (ROOT/name).write_bytes(original.decode('utf-8-sig').replace('TestResults/','TestResults/living-dungeon-b3a-20261017/outputs/').encode())
    print('Shared screenshot helper redirected; original retained in the input archive')
def repair_evidence():
    rows=json.loads((OUT/'historical-evidence-check-after-05.json').read_text())
    wanted={r['sha256'] for r in rows}; found={}
    for base in [ROOT/'Docs',ROOT/'TestResults']:
        for p in base.rglob('*.png'):
            if OUT in p.parents or p.parent==ROOT/'TestResults/phase7a4-screenshots': continue
            digest=sha(p)
            if digest in wanted and digest not in found: found[digest]=(str(p),p.read_bytes())
        names={Path(r['path']).name for r in rows}
        for p in base.rglob('*.zip'):
            if OUT in p.parents: continue
            with zipfile.ZipFile(p) as z:
                for item in z.infolist():
                    if not item.filename.lower().endswith('.png'): continue
                    data=z.read(item); digest=hashlib.sha256(data).hexdigest()
                    if digest in wanted and digest not in found: found[digest]=(str(p)+'#'+item.filename,data)
    missing=wanted-found.keys()
    result=[]; archive=OUT/'overwritten-captures-before-repair'; archive.mkdir(exist_ok=True)
    for row in rows:
        p=Path(row['path']); inputs.no_redirect(p)
        if p.resolve().parent!=(ROOT/'TestResults/phase7a4-screenshots').resolve(): raise ValueError('Unsafe capture restore target')
        if not (archive/p.name).exists(): shutil.copy2(p,archive/p.name)
        if row['sha256'] not in found:
            result.append(dict(**row,source=None,restored=False)); continue
        source,data=found[row['sha256']]
        if hashlib.sha256(data).hexdigest()!=row['sha256']: raise ValueError('Original hash mismatch')
        p.write_bytes(data); result.append(dict(**row,source=source,restored=sha(p)==row['sha256']))
    write('screenshot-repair.json',result)
    write('missing-original-screenshots.json',[r for r in result if not r['restored']])
    if missing: raise ValueError('Restored available exact originals; still missing: '+str(sorted(missing)))
    print(len(result),'original screenshots restored from independently retained, exact-hash copies; new captures preserved')
def backup_historical_current():
    rows=json.loads((OUT/'historical-evidence-before.json').read_text())
    with zipfile.ZipFile(OUT/'historical-evidence-after-incident.zip','x',zipfile.ZIP_DEFLATED) as z:
        for row in rows:
            p=Path(row['path'])
            if p.is_file(): z.write(p,p.relative_to(ROOT/'TestResults').as_posix())
    print(len(rows),'current historical-evidence files backed up; original hash manifest remains unchanged')
def baseline_ui(begin):
    changed=['Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs',
             'Assets/_Project/Editor/DungeonVisualBuildQualification.cs']
    added=['Assets/_Project/Scripts/Gameplay/DungeonSpatial/DurableSettlementHistory.cs',
           'Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonTerminalSettlement.cs',
           'Assets/_Project/Tests/EditMode/LivingDungeonPhaseB3ATests.cs',
           'Assets/_Project/Tests/PlayMode/LivingDungeonPhaseB3APlayModeTests.cs',
           'Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonPhaseB3AEditModeFixtures.cs']
    archive=OUT/'baseline-ui-feature-inputs.zip'
    if begin:
        with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as z:
            for name in changed+added: z.write(ROOT/name,name)
        for name in changed:
            (ROOT/name).write_bytes(subprocess.check_output(['git','show','e5abe082a8a51a00845dfff5c735fbb3eb9edb8d:'+name],cwd=ROOT))
        for name in added: (ROOT/name).write_bytes(b'#if false\n'+(ROOT/name).read_bytes()+b'\n#endif\n')
        write('baseline-ui-context.json',dict(productionBaseline='e5abe082a8a51a00845dfff5c735fbb3eb9edb8d',
              restoredProductionFiles=changed,excludedAdditions=added,otherSubstitutions='save/evidence paths and five preserved local assets'))
    else:
        with zipfile.ZipFile(archive) as z:
            for name in changed+added: (ROOT/name).write_bytes(z.read(name))
    print('Baseline UI inputs installed' if begin else 'Feature inputs restored byte-for-byte')
def finish():
    checkpoint('final-preservation')
    baseline=json.loads((OUT/'baseline.json').read_text())
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source:
        for name in [inputs.CONFIG,inputs.SERVICE]: (ROOT/name).write_bytes(source.read(name))
        if (OUT/'historical-evidence-before.json').exists():
            for name in OUTPUT_TESTS: (ROOT/name).write_bytes(source.read(name))
        if (OUT/'fixture-substitutions.json').exists():
            for name in FIXTURE_TESTS: (ROOT/name).write_bytes(source.read(name))
    # Only preexisting, explicitly disposable test fixtures can be restored.
    with zipfile.ZipFile(OUT/'test-fixtures-before.zip') as source:
        for row in baseline['protected']:
            if not row['fixture']: continue
            p=Path(row['path']); inputs.no_redirect(p)
            if p.parent.resolve()!=SAVE.resolve(): raise ValueError('Unsafe fixture')
            if sha(p)==row['sha256']: continue
            backup=OUT/'test-modified-fixtures'/p.name
            backup.parent.mkdir(exist_ok=True); shutil.copy2(p,backup)
            data=source.read(p.name)
            if hashlib.sha256(data).hexdigest()!=row['sha256']: raise ValueError('Bad fixture backup')
            p.write_bytes(data)
    rows=[dict(**r,equal=Path(r['path']).exists() and sha(Path(r['path']))==r['sha256']) for r in baseline['protected']]
    write('preservation-after.json',rows)
    if any(not r['equal'] for r in rows): raise ValueError('Protected files changed')
    print(len(rows),'protected saves/player files preserved')
    if (OUT/'historical-evidence-before.json').exists():
        rows=json.loads((OUT/'historical-evidence-before.json').read_text())
        for row in rows: row['equal']=Path(row['path']).exists() and sha(Path(row['path']))==row['sha256']
        write('historical-evidence-after.json',rows)
        if any(not r['equal'] for r in rows): raise ValueError('Historical evidence changed')
        print(len(rows),'historical evidence files preserved')
if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='prepare': prepare()
    elif mode=='checkpoint': checkpoint(sys.argv[2])
    elif mode=='finish': finish()
    elif mode=='isolate-evidence': isolate_evidence()
    elif mode=='isolate-fixtures': isolate_fixtures()
    elif mode=='applicability': applicability(sys.argv[2])
    elif mode=='normalize-fixtures': normalize_fixture_sources()
    elif mode=='isolate-shared-capture': isolate_shared_capture()
    elif mode=='repair-evidence': repair_evidence()
    elif mode=='backup-historical-current': backup_historical_current()
    elif mode=='begin-baseline-ui': baseline_ui(True)
    elif mode=='end-baseline-ui': baseline_ui(False)
    else: raise ValueError(mode)
