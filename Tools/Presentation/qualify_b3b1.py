"""B3B1 campaign adapter for the existing B3A preservation helpers; no project copy.

Only reviewed campaign names are substituted. The closed B3A campaign/launch gate
is not reopened. Each Unity launch independently checks exact committed inputs,
all namespace substitutions, protected files and unused outputs.
"""
from pathlib import Path
import json, subprocess, sys, types, zipfile

ROOT = Path(__file__).resolve().parents[2]
source = (Path(__file__).with_name('qualify_b3a.py')).read_text(encoding='utf-8')
source = source.replace('living-dungeon-b3a-20261017', 'living-dungeon-b3b1-20261018')
source = source.replace('living-dungeon-b3a-qualification-20261017', 'living-dungeon-b3b1-qualification-20261018')
source = source.replace('b3a_20261017', 'b3b1_20261018')
STARTER_TEST = 'Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs'
STARTER_NAME = 'living-dungeon-b3b1-owner-uat-starter-"+System.Guid.NewGuid().ToString("N")+".json'
fixture_tail = "('mvp_canonical_journey_test.json','mvp_canonical_journey_b3b1_20261018.json')"
if source.count(fixture_tail) != 2: raise ValueError('Reviewed fixture substitution helpers changed')
source = source.replace(fixture_tail, fixture_tail+', '+repr(('owner-uat-starter.json', STARTER_NAME)))
q = types.ModuleType('b3b1_campaign')
q.__file__ = str(Path(__file__).with_name('qualify_b3a.py'))
exec(compile(source, q.__file__, 'exec'), q.__dict__)
if (q.OUT/'starter-substitution.json').exists(): q.FIXTURE_TESTS.append(STARTER_TEST)
BUILD_UTILITY = 'Assets/_Project/Scripts/BuildTools/DevelopmentBuildUtility.cs'
PRODUCT = 'Dungeon Lord B3B1 Smoke 20261018'
PRODUCT_SETTINGS = 'ProjectSettings/ProjectSettings.asset'
SHADER_RELATIVE = 'Unity/ShaderVariantAnalytics/ShaderRuntimeInfoEvent.json'

def accepted_cache_exception(row):
    approval = q.OUT/'owner-cache-exception.json'
    if not approval.exists(): return False
    value = json.loads(approval.read_text())
    return value.get('ownerApproved') is True and row['path'] == str(q.SAVE/SHADER_RELATIVE) and row['sha256'] == value.get('originalSha256')

def apply_product():
    for name, old, new in [
        (PRODUCT_SETTINGS, '  productName: Dungeon Lord', '  productName: '+PRODUCT),
        (BUILD_UTILITY, 'public const string ProductName = "Dungeon Lord";', 'public const string ProductName = "'+PRODUCT+'";')]:
        data=(ROOT/name).read_bytes()
        if data.count(new.encode()) == 1: continue
        if data.count(old.encode()) != 1: raise ValueError('Unexpected product identity source '+name)
        (ROOT/name).write_bytes(data.replace(old.encode(),new.encode()))

if (q.OUT/'product-isolation.json').exists():
    original_normalized=q.inputs.normalized
    def product_normalized(name, data):
        if name in (PRODUCT_SETTINGS,BUILD_UTILITY): data=data.replace(PRODUCT.encode(),b'Dungeon Lord')
        return original_normalized(name,data)
    q.inputs.normalized=product_normalized

def checkpoint(label):
    if (q.OUT/'substitutions-restored.json').exists(): raise ValueError('Campaign is closed')
    q.checkpoint(label)
    if (q.OUT/'product-isolation.json').exists(): apply_product()

def isolate_product():
    if (q.OUT/'substitutions-restored.json').exists(): raise ValueError('Campaign is closed')
    if (q.OUT/'product-isolation.json').exists(): raise ValueError('Product isolation already installed')
    destination=q.SAVE.parent/PRODUCT
    q.inputs.no_redirect(destination)
    if destination.exists(): raise ValueError('Preserve existing product data; choose an unused identity')
    with zipfile.ZipFile(q.OUT/'root-inputs-before.zip') as z: original=z.read(BUILD_UTILITY)
    if (ROOT/BUILD_UTILITY).read_bytes()!=original: raise ValueError('Build identity guard changed')
    apply_product()
    q.write('product-isolation.json',dict(product=PRODUCT,savePath=str(destination),buildUtilityOriginalSha256=q.hashlib.sha256(original).hexdigest()))
    print('Isolated native product identity installed; original strict identity condition retained')

def isolate_starter():
    if (q.OUT/'substitutions-restored.json').exists(): raise ValueError('Campaign is closed')
    if (q.OUT/'starter-substitution.json').exists(): raise ValueError('Starter output isolation already installed')
    with zipfile.ZipFile(q.OUT/'root-inputs-before.zip') as z: original=z.read(STARTER_TEST)
    if (ROOT/STARTER_TEST).read_bytes()!=original: raise ValueError('Unexpected starter test source change')
    text=original.decode('utf-8-sig')
    if text.count('owner-uat-starter.json')!=1: raise ValueError('Starter output owner changed')
    (ROOT/STARTER_TEST).write_bytes(text.replace('owner-uat-starter.json',STARTER_NAME).encode('utf-8'))
    q.FIXTURE_TESTS.append(STARTER_TEST)
    q.write('starter-substitution.json',dict(path=STARTER_TEST,sha256=q.sha(ROOT/STARTER_TEST),
        reason='Unchanged PR #242 test uses a fixed no-overwrite starter artifact; qualification copies need unused names.'))
    q.applicability('starter-isolated')

def preflight(label):
    if (q.OUT/'substitutions-restored.json').exists(): raise ValueError('Isolation is inactive')
    for name in ['baseline.json', 'historical-evidence-before.json', 'fixture-substitutions.json']:
        if not (q.OUT/name).is_file(): raise ValueError('Missing preservation prerequisite '+name)
    if not zipfile.is_zipfile(q.OUT/'historical-evidence-preserved.zip'): raise ValueError('Historical backup is incomplete')
    q.applicability(label)
    for name, key in [('baseline.json', 'protected'), ('historical-evidence-before.json', None)]:
        rows = json.loads((q.OUT/name).read_text())
        if key: rows = rows[key]
        for row in rows:
            p = Path(row['path']); q.inputs.no_redirect(p)
            if not p.is_file() or q.sha(p) != row['sha256']:
                if key and accepted_cache_exception(row): continue
                raise ValueError('Protected file changed: '+str(p))
    if (q.OUT/'product-isolation.json').exists():
        if ('  productName: '+PRODUCT).encode() not in (ROOT/PRODUCT_SETTINGS).read_bytes() or \
            ('public const string ProductName = "'+PRODUCT+'";').encode() not in (ROOT/BUILD_UTILITY).read_bytes():
            raise ValueError('Native product identity is not isolated')
    # Independently verify every redirection; committed-source comparison alone
    # must never bless a screenshot helper still aimed at historical output.
    for name in q.OUTPUT_TESTS:
        text = (ROOT/name).read_text(encoding='utf-8-sig')
        if 'TestResults/' in text.replace('TestResults/living-dungeon-b3b1-20261018/outputs/', ''):
            raise ValueError('Unredirected output in '+name)

def run(stage, attempt):
    label = attempt+'-'+stage
    if not label.replace('-', '').isalnum(): raise ValueError('Unsafe stage/attempt')
    preflight(label)
    paths = [q.OUT/(label+suffix) for suffix in ['.xml', '.log', '-command.json']]
    for p in paths:
        q.inputs.no_redirect(p)
        if p.exists(): raise ValueError('Preserve existing output '+str(p))
    processes = subprocess.run(['powershell', '-NoProfile', '-Command',
        "Get-CimInstance Win32_Process -Filter \"name = 'Unity.exe' OR name = 'Dungeon Lord.exe'\" | "
        "Where-Object { $_.CommandLine -notin @('\"C:\\Program Files\\Unity Hub\\resources\\unity.exe\" serve', "
        "'\"C:\\Users\\gdg34\\AppData\\Local\\Unity\\bin\\unity.exe\" --internal-auth-broker-serve') } | Select-Object -ExpandProperty ProcessId"], capture_output=True, text=True)
    if processes.stderr.strip() or processes.returncode not in (0, 1): raise ValueError('Cannot inspect Unity processes')
    if processes.stdout.strip():
        raise ValueError('Unity or player is active')
    filters = {
        'focused': 'LivingDungeonB3B1',
        'regression': 'LivingDungeonPhaseA;LivingDungeonPhaseB1;LivingDungeonPhaseB2;LivingDungeonPhaseB3A;CombatC0;CombatC2;DetachedCanonical;SaveSpatialMigration;Gd66Windows;RunHistory;RunHeat;LootHeat',
        'integration-play': 'LivingDungeonB3B1PlayModeTests;LivingDungeonPhaseAPlayModeTests;LivingDungeonPhaseB1PlayModeTests;LivingDungeonPhaseB2PlayModeTests;LivingDungeonPhaseB3APlayModeTests;CombatC2PlayModeTests',
        'starter': 'OwnerUsabilityConstructionAndContentReachability',
    }
    cli = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
    args = ['test', str(ROOT), '--mode', 'PlayMode' if stage in ('integration-play', 'full-play') else 'EditMode']
    if stage in filters: args += ['--filter', filters[stage]]
    elif stage not in ('full-edit', 'full-play', 'build', 'build-smoke-fix'): raise ValueError('Unknown stage')
    args += ['--output', str(paths[0]), '--timeout', '1800', '--no-color', '--', '-logFile', str(paths[1])]
    if stage in ('build', 'build-smoke-fix'):
        suffix = '-smoke-fix' if stage == 'build-smoke-fix' else ''
        target = ROOT/('Builds/Development/Windows-living-dungeon-b3b1-20261018'+suffix)
        if target.exists(): raise ValueError('Preserve existing build')
        args = ['build', str(ROOT), '--target', 'StandaloneWindows64', '--execute-method',
            'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.'+('BuildLivingDungeonB3B1SmokeFixWindows' if suffix else 'BuildLivingDungeonB3B1Windows'),
            '--allow-dirty-build', '--timeout', '1800', '--no-tail', '--log-file', str(paths[1]), '--no-color']
    import datetime
    record = dict(executable=cli, arguments=args, commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(), startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
    record['exitCode'] = subprocess.call([cli, *args], cwd=ROOT)
    record['finishedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    paths[2].write_text(json.dumps(record, indent=2))
    print(json.dumps(record)); return record['exitCode']

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'prepare':
        if list(q.SAVE.glob(q.NAMESPACE+'*')): raise ValueError('Save namespace already exists; preserve it and select a fresh campaign')
        q.prepare(); q.isolate_evidence(); q.isolate_fixtures(); isolate_starter()
        with zipfile.ZipFile(q.OUT/'historical-evidence-preserved.zip', 'x', zipfile.ZIP_DEFLATED) as z:
            for row in json.loads((q.OUT/'historical-evidence-before.json').read_text()):
                p = Path(row['path']); z.write(p, p.relative_to(ROOT/'TestResults').as_posix())
    elif mode == 'checkpoint': checkpoint(sys.argv[2])
    elif mode == 'isolate-starter': isolate_starter()
    elif mode == 'isolate-product': isolate_product()
    elif mode == 'finish': q.finish()
    elif mode == 'preflight': preflight(sys.argv[2])
    elif mode == 'run': sys.exit(run(sys.argv[2], sys.argv[3]))
    else: raise ValueError(mode)
