"""Run original-reader attestation in the actual, preserved Unity project."""
import datetime
import json
import subprocess


def capture(m, label):
    if not label.replace('-', '').isalnum():
        raise ValueError('Unsafe label')
    m.preflight(label)
    log, command = [m.q.OUT / (label + suffix) for suffix in ('.log', '-command.json')]
    for path in (log, command):
        m.q.inputs.no_redirect(path)
        if path.exists():
            raise ValueError('Preserve previous capture evidence')
    active = subprocess.run(['powershell', '-NoProfile', '-Command',
        'Get-CimInstance Win32_Process -Filter "name = \'Unity.exe\' OR name = \'Dungeon Lord.exe\'" | '
        'Where-Object { $_.CommandLine -notin @(\'"C:\\Program Files\\Unity Hub\\resources\\unity.exe" serve\', '
        '\'"C:\\Users\\gdg34\\AppData\\Local\\Unity\\bin\\unity.exe" --internal-auth-broker-serve\') } | Select-Object -ExpandProperty ProcessId'],
        capture_output=True, text=True)
    if active.stderr.strip() or active.returncode not in (0, 1) or active.stdout.strip():
        raise ValueError('Unity/player active or process inspection unavailable')
    cli = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
    args = ['run', str(m.q.ROOT), '--timeout', '600', '--no-tail', '--log-file', str(log), '--',
            '-executeMethod', 'DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonHistoricalCheckpointCapture.CaptureFormatOne']
    record = dict(executable=cli, arguments=args,
                  commit=subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=m.q.ROOT, text=True).strip(),
                  startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
    record['exitCode'] = subprocess.call([cli, *args], cwd=m.q.ROOT)
    record['finishedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    with command.open('x', encoding='utf-8') as stream:
        json.dump(record, stream, indent=2)
    print(json.dumps(record))
    if record['exitCode']:
        raise ValueError('Original-reader attestation failed; preserve output')
