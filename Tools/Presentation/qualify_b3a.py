"""Qualify in the existing project. Preserve inputs, saves, evidence and players."""
from pathlib import Path
import hashlib, json, shutil, subprocess, sys, uuid, zipfile
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
                'Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonFloorSceneTests.cs']

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
    print('Archived/restored generated inputs:',changed)
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
            (ROOT/name).write_text(text,encoding='utf-8')
    print(len(rows),'historical evidence files hashed; test captures redirected to the unique B3A directory')
def finish():
    checkpoint('final-preservation')
    baseline=json.loads((OUT/'baseline.json').read_text())
    with zipfile.ZipFile(OUT/'root-inputs-before.zip') as source:
        for name in [inputs.CONFIG,inputs.SERVICE]: (ROOT/name).write_bytes(source.read(name))
        if (OUT/'historical-evidence-before.json').exists():
            for name in OUTPUT_TESTS: (ROOT/name).write_bytes(source.read(name))
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
    else: raise ValueError(mode)
