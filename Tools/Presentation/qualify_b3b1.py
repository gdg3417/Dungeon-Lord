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
q = types.ModuleType('b3b1_campaign')
q.__file__ = str(Path(__file__).with_name('qualify_b3a.py'))
exec(compile(source, q.__file__, 'exec'), q.__dict__)

def preflight(label):
    if (q.OUT/'substitutions-restored.json').exists(): raise ValueError('Isolation is inactive')
    for name in ['baseline.json', 'historical-evidence-before.json', 'fixture-substitutions.json']:
        if not (q.OUT/name).is_file(): raise ValueError('Missing preservation prerequisite '+name)
    q.applicability(label)
    for name, key in [('baseline.json', 'protected'), ('historical-evidence-before.json', None)]:
        rows = json.loads((q.OUT/name).read_text())
        if key: rows = rows[key]
        for row in rows:
            p = Path(row['path']); q.inputs.no_redirect(p)
            if not p.is_file() or q.sha(p) != row['sha256']: raise ValueError('Protected file changed: '+str(p))
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
    }
    cli = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
    args = ['test', str(ROOT), '--mode', 'PlayMode' if stage in ('integration-play', 'full-play') else 'EditMode']
    if stage in filters: args += ['--filter', filters[stage]]
    elif stage not in ('full-edit', 'full-play', 'build'): raise ValueError('Unknown stage')
    args += ['--output', str(paths[0]), '--timeout', '1800', '--no-color', '--', '-logFile', str(paths[1])]
    if stage == 'build':
        target = ROOT/'Builds/Development/Windows-living-dungeon-b3b1-20261018'
        if target.exists(): raise ValueError('Preserve existing build')
        args = ['build', str(ROOT), '--target', 'StandaloneWindows64', '--execute-method',
            'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonB3B1Windows',
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
        q.prepare(); q.isolate_evidence(); q.isolate_fixtures()
        with zipfile.ZipFile(q.OUT/'historical-evidence-preserved.zip', 'x', zipfile.ZIP_DEFLATED) as z:
            for row in json.loads((q.OUT/'historical-evidence-before.json').read_text()):
                p = Path(row['path']); z.write(p, p.relative_to(ROOT/'TestResults').as_posix())
    elif mode == 'checkpoint': q.checkpoint(sys.argv[2])
    elif mode == 'finish': q.finish()
    elif mode == 'preflight': preflight(sys.argv[2])
    elif mode == 'run': sys.exit(run(sys.argv[2], sys.argv[3]))
    else: raise ValueError(mode)
