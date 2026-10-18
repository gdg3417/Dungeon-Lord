"""Fresh PR242 graphical campaign. Explicit preparation; read-only launch gate.

Never reuses the closed B3A campaign. No project copy or Editor installation.
"""
from pathlib import Path
import hashlib, json, os, re, subprocess, sys, zipfile
import qualification_inputs as inputs

ROOT = Path(__file__).resolve().parents[2]
NAME = 'pr242-graphics-20261018'
OUT = ROOT/'TestResults'/NAME
OWNER = Path.home()/'AppData/LocalLow/gdg3417'
UNITY = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
SOURCE_OVERRIDES = ['ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset',
    'Assets/Settings/UniversalRP.asset','Assets/UniversalRenderPipelineGlobalSettings.asset',
    'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset']
CAPTURES = ['Assets/_Project/Scripts/BuildTools/ProductionDungeonScreenshots.cs',
    'Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonFloorSceneTests.cs',
    'Assets/_Project/Editor/DungeonSpatial/Tests/CombatC0ProductionEvidenceSceneTests.cs']
FIXTURES = ['Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs',
    'Assets/_Project/Tests/EditMode/MvpCanonicalJourneyIntegrationTests.cs',
    'Assets/_Project/Tests/EditMode/RunSimulationTests.cs']
BUILD_HELPER = 'Assets/_Project/Editor/DungeonVisualBuildQualification.cs'
BUILD_UTILITY = 'Assets/_Project/Scripts/BuildTools/DevelopmentBuildUtility.cs'
SUBSTITUTED = [inputs.CONFIG, inputs.SERVICE, SOURCE_OVERRIDES[0], BUILD_HELPER, BUILD_UTILITY]+CAPTURES+FIXTURES
FILTERS = {
    'focused': ('EditMode','RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated;TransactionalRoomConstructionLayoutsLocalizationAndResume'),
    'ui-edit': ('EditMode','PhaseSevenA4;ProductionDungeon;BootstrapOverlay;DungeonPresentation;CombatC1;CombatC2'),
    'ui-play': ('PlayMode','PhaseSevenA4ProductionShellPlayModeTests;CombatC2PlayModeTests'),
}

def sha(path):
    value=hashlib.sha256()
    with path.open('rb') as stream:
        while chunk:=stream.read(1024*1024): value.update(chunk)
    return value.hexdigest()
def write(path,value):
    with path.open('x',encoding='utf-8') as stream: json.dump(value,stream,indent=2)
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def unused(path):
    inputs.no_redirect(path)
    if path.exists(): raise ValueError('Preserve existing destination: '+str(path))
def verify(rows):
    for row in rows:
        path=Path(row['path']); inputs.no_redirect(path)
        if not path.is_file() or sha(path)!=row['sha256']: raise ValueError('Preservation/source mismatch: '+str(path))
def protected():
    paths=set()
    for base in [OWNER,ROOT/'Builds',ROOT/'TestResults',ROOT/'Docs/testing/evidence',ROOT/'Temp']:
        if base.exists():
            for p in base.rglob('*'):
                if p.is_file() and OUT not in p.parents:
                    inputs.no_redirect(p); paths.add(p)
    return sorted(paths)
def inventory(): return sorted(inputs.target_inventory(ROOT))
def source_rows(): return [dict(path=str(ROOT/n),sha256=sha(ROOT/n)) for n in inventory()]
def verify_authored_scope():
    changed=git('diff','--name-only','HEAD','--',*inputs.INPUT_ROOTS).splitlines()
    if set(changed)-set(SOURCE_OVERRIDES): raise ValueError('Commit reviewed UI corrections before preparation: '+str(changed))
    qualified=read(ROOT/'Docs/testing/evidence/living-dungeon-b3a/source-applicability.json')
    allowed=set(SOURCE_OVERRIDES+['Assets/_Project/UI/ProductionDungeon/Dungeon.uss',
        'Assets/_Project/Scripts/UI/ProductionDungeonController.cs',
        'Assets/_Project/Scripts/UI/ProductionDungeonConstruction.cs',
        'Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs',
        'Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonRoomContentSceneTests.cs',
        'Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs'])
    for row in qualified['unityInputs']:
        if row['path'] not in allowed and sha(ROOT/row['path'])!=row['sha256']:
            raise ValueError('Qualified runtime/test source changed: '+row['path'])
def attempt_path(attempt):
    if not re.fullmatch('[a-z0-9-]+',attempt): raise ValueError('Unsafe attempt')
    return OUT/attempt
def destinations(attempt):
    folder=attempt_path(attempt)
    product='Dungeon Lord PR242 Graphics '+attempt
    return dict(folder=folder,save=OWNER/product,build=ROOT/'Builds/Development'/(NAME+'-'+attempt),
                product=product,namespace=NAME+'-'+attempt+'.json')
def transformed(name,data,attempt,stage):
    dest=destinations(attempt); folder=dest['folder'].relative_to(ROOT).as_posix()
    if name in [inputs.CONFIG,inputs.SERVICE]:
        if name==inputs.CONFIG:
            config=json.loads(data.decode('utf-8-sig'))
            if config['save']['fileName']!='save_primary.json': raise ValueError('Unexpected config save')
            config['save']['fileName']=dest['namespace']; return json.dumps(config,indent=2).encode()
        return data.decode('utf-8-sig').replace('save_primary.json',dest['namespace']).encode()
    if name==BUILD_UTILITY and stage!='build': return data
    text=data.decode('utf-8-sig')
    if name==SOURCE_OVERRIDES[0]:
        if text.count('  productName: Dungeon Lord\n')!=1 and text.count('  productName: Dungeon Lord\r\n')!=1:
            raise ValueError('Unexpected Unity product identity')
        text=text.replace('  productName: Dungeon Lord','  productName: '+dest['product'])
    elif name in CAPTURES:
        if 'TestResults/' not in text: raise ValueError('Screenshot helper changed: '+name)
        text=text.replace('TestResults/',folder+'/screens/')
        if name==CAPTURES[0]:
            text=text.replace('Path.GetFileName(filename)', 'Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(filename)')
    elif name in FIXTURES:
        for old,new in [('bootstrap_overlay_','bootstrap_overlay_'+attempt+'_'),('run_sim_test_','run_sim_test_'+attempt+'_'),
                        ('mvp_canonical_journey_test.json','mvp_canonical_journey_'+attempt+'.json')]: text=text.replace(old,new)
    elif name==BUILD_HELPER:
        text=text.replace('Builds/Development/Windows-living-dungeon-b3a-20261017/Dungeon Lord.exe',
                          dest['build'].relative_to(ROOT).as_posix()+'/Dungeon Lord.exe')
    elif name==BUILD_UTILITY:
        expected='public const string ProductName = "Dungeon Lord";'
        if text.count(expected)!=1: raise ValueError('Build identity policy changed; review before qualification')
        # Preserve the strict identity condition; only the explicitly isolated
        # build's expected product value changes. This source is restored exactly.
        text=text.replace(expected,'public const string ProductName = "'+dest['product']+'";')
    return text.encode()
def init():
    unused(OUT)
    verify_authored_scope()
    for name in inventory():
        if name.endswith('.cs') and not (ROOT/(name+'.meta')).is_file(): raise ValueError('Missing reviewed .meta: '+name)
    # Record current replacement bytes; the five lost originals remain an accepted
    # incident, never relabeled as restored or removed from the original manifest.
    rows=[dict(path=str(p),sha256=sha(p)) for p in protected()]
    OUT.mkdir()
    write(OUT/'baseline.json',dict(startCommit=git('rev-parse','HEAD'),protected=rows,
        ownerException='Owner accepted permanent loss of five original screenshots; original hashes/incident remain unchanged.',
        scriptSha256=sha(Path(__file__)),sources=source_rows()))
    print('Fresh preservation baseline:',len(rows),'files')
def prepare(attempt,stage):
    if stage not in {*FILTERS,'build'}: raise ValueError('Unsupported stage')
    verify_authored_scope()
    baseline=read(OUT/'baseline.json'); verify(baseline['protected'])
    for folder in OUT.iterdir():
        if folder.is_dir() and (folder/'prepared.json').exists() and not (folder/'restored.json').exists():
            raise ValueError('Restore prior attempt first: '+str(folder))
        if folder.is_dir() and (folder/'artifact-index.json').exists(): verify(read(folder/'artifact-index.json'))
    dest=destinations(attempt)
    for key in ['folder','save','build']: unused(dest[key])
    names=inventory(); originals={n:(ROOT/n).read_bytes() for n in names}
    prepared={n:transformed(n,data,attempt,stage) if n in SUBSTITUTED else data for n,data in originals.items()}
    dest['folder'].mkdir()
    with zipfile.ZipFile(dest['folder']/'original-inputs.zip','x',zipfile.ZIP_DEFLATED) as archive:
        for n,data in originals.items(): archive.writestr(n,data)
    write(dest['folder']/'prepared.json',dict(stage=stage,commit=git('rev-parse','HEAD'),scriptSha256=sha(Path(__file__)),baselineSha256=sha(OUT/'baseline.json'),
        original=[dict(path=str(ROOT/n),sha256=hashlib.sha256(d).hexdigest()) for n,d in originals.items()],
        prepared=[dict(path=str(ROOT/n),sha256=hashlib.sha256(d).hexdigest()) for n,d in prepared.items()]))
    for n in SUBSTITUTED: (ROOT/n).write_bytes(prepared[n])
    print('Explicit isolation prepared:',attempt,stage,dest['product'])
def preflight(attempt):
    dest=destinations(attempt); folder=dest['folder']; inputs.no_redirect(folder)
    if (folder/'restored.json').exists(): raise ValueError('Restored/inactive attempt')
    plan=read(folder/'prepared.json'); baseline=read(OUT/'baseline.json')
    if plan['baselineSha256']!=sha(OUT/'baseline.json'): raise ValueError('Preservation baseline changed')
    if plan['scriptSha256']!=sha(Path(__file__)): raise ValueError('Qualification script changed after preparation')
    if git('rev-parse','HEAD')!=plan['commit']: raise ValueError('Source commit changed')
    if {str(ROOT/n) for n in inventory()}!={r['path'] for r in plan['prepared']}: raise ValueError('Unexpected Unity inputs')
    verify(plan['prepared']); verify(baseline['protected'])
    with zipfile.ZipFile(folder/'original-inputs.zip') as archive:
        for row in plan['original']:
            if hashlib.sha256(archive.read(Path(row['path']).relative_to(ROOT).as_posix())).hexdigest()!=row['sha256']:
                raise ValueError('Original input archive mismatch')
    for p in [folder/'screens',folder/'results.xml',folder/'editor.log',folder/'command.json',dest['save'],dest['build']]: unused(p)
    # Recompute every reviewed transformation, not just a caller-provided stamp.
    with zipfile.ZipFile(folder/'original-inputs.zip') as archive:
        for name in SUBSTITUTED:
            if (ROOT/name).read_bytes()!=transformed(name,archive.read(name),attempt,plan['stage']): raise ValueError('Isolation mismatch: '+name)
    print('PREFLIGHT PASS:',attempt,'exact inputs',len(plan['prepared']),'protected',len(baseline['protected']))
    return plan
def run(attempt):
    plan=preflight(attempt); dest=destinations(attempt); folder=dest['folder']; stage=plan['stage']
    if stage=='build':
        args=['build',str(ROOT),'--target','StandaloneWindows64','--execute-method',
              'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonB3AWindows',
              '--allow-dirty-build','--timeout','1800','--no-tail','--log-file',str(folder/'editor.log'),'--no-color']
    else:
        mode,selector=FILTERS[stage]
        args=['test',str(ROOT),'--mode',mode,'--filter',selector,'--output',str(folder/'results.xml'),
              '--timeout','1800','--no-color','--','-logFile',str(folder/'editor.log')]
    process=subprocess.run([UNITY,*args],cwd=ROOT)
    write(folder/'command.json',dict(executable=UNITY,arguments=args,exitCode=process.returncode,sourceCommit=plan['commit']))
    print('Unity exit:',process.returncode); return process.returncode
def finish(attempt):
    dest=destinations(attempt); folder=dest['folder']; unused(folder/'restored.json'); plan=read(folder/'prepared.json')
    expected={r['path']:r['sha256'] for r in plan['prepared']}; originals={r['path']:r['sha256'] for r in plan['original']}
    if set(expected)!={str(ROOT/n) for n in inventory()}: raise ValueError('Unexpected inputs; inspect before restore')
    generated=[]
    for name in inventory():
        if sha(ROOT/name)!=expected[str(ROOT/name)]:
            if name not in SOURCE_OVERRIDES: raise ValueError('Unexpected source change: '+name)
            generated.append(name)
    with zipfile.ZipFile(folder/'generated-inputs.zip','x',zipfile.ZIP_DEFLATED) as archive:
        for name in generated: archive.write(ROOT/name,name)
    with zipfile.ZipFile(folder/'original-inputs.zip') as archive:
        for name in set(SUBSTITUTED+generated):
            data=archive.read(name)
            if hashlib.sha256(data).hexdigest()!=originals[str(ROOT/name)]: raise ValueError('Bad original input')
            (ROOT/name).write_bytes(data)
    verify(plan['original']); verify(read(OUT/'baseline.json')['protected'])
    write(folder/'restored.json',dict(restored=True,exactOriginalInputs=len(originals),protectedFilesUnchanged=True,generatedPreserved=generated))
    index_attempt(attempt)
    print('Restored exact authored inputs/local overrides; all original saves/evidence/builds unchanged')
def index_attempt(attempt):
    dest=destinations(attempt); folder=dest['folder']; unused(folder/'artifact-index.json')
    paths=[]
    for base in [folder,dest['save'],dest['build']]:
        if base.exists(): paths.extend(p for p in base.rglob('*') if p.is_file())
    write(folder/'artifact-index.json',[dict(path=str(p),sha256=sha(p)) for p in sorted(paths)])
def selftest():
    import tempfile
    with tempfile.TemporaryDirectory(prefix='pr242-gate-') as name:
        root=Path(name); p=root/'protected'; p.write_bytes(b'owner fixture'); rows=[dict(path=str(p),sha256=sha(p))]
        before=p.read_bytes(); verify(rows)
        try: unused(p)
        except ValueError: pass
        else: raise AssertionError('Collision accepted')
        p.write_bytes(b'mismatch')
        try: verify(rows)
        except ValueError: pass
        else: raise AssertionError('Preservation mismatch accepted')
        assert before!=p.read_bytes()
    print('Disposable guard checks passed; actual prepared preflight must also pass before Unity')
if __name__=='__main__':
    try:
        mode=sys.argv[1]
        if mode=='init': init()
        elif mode=='prepare': prepare(sys.argv[2],sys.argv[3])
        elif mode=='preflight': preflight(sys.argv[2])
        elif mode=='run': sys.exit(run(sys.argv[2]))
        elif mode=='finish': finish(sys.argv[2])
        elif mode=='index': index_attempt(sys.argv[2])
        elif mode=='selftest': selftest()
        else: raise ValueError(mode)
    except (ValueError,OSError,KeyError,zipfile.BadZipFile) as error:
        print('PR242 qualification rejected:',error,file=sys.stderr); sys.exit(2)
