"""Opt-in isolated native launches; never read a canonical file while its player runs."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess


def launch(campaign, phase, label, previous):
    m = campaign
    root, out = m.q.ROOT, m.q.OUT
    allowed = ('two-floor-wait', 'two-floor-release', 'two-floor-finish',
               'v1-upgrade', 'two-floor-profile')
    if phase not in allowed or not label.replace('-', '').isalnum():
        raise ValueError('Unknown phase or unsafe output label')
    m.preflight(label)
    report, log, command = [out / (label + suffix) for suffix in ('.json', '.log', '-command.json')]
    for path in (report, log, command, out / (label + '.save.json')):
        m.q.inputs.no_redirect(path)
        if path.exists():
            raise ValueError('Preserve existing output ' + str(path))
    process = subprocess.run(['powershell', '-NoProfile', '-Command',
        'Get-CimInstance Win32_Process -Filter "name = \'Unity.exe\' OR name = \'Dungeon Lord.exe\'" | '
        'Where-Object { $_.CommandLine -notin @(\'"C:\\Program Files\\Unity Hub\\resources\\unity.exe" serve\', '
        '\'"C:\\Users\\gdg34\\AppData\\Local\\Unity\\bin\\unity.exe" --internal-auth-broker-serve\') } | Select-Object -ExpandProperty ProcessId'],
        capture_output=True, text=True)
    if process.stderr.strip() or process.returncode not in (0, 1) or process.stdout.strip():
        raise ValueError('Unity/player active or process inspection unavailable')
    previous = Path(previous).resolve()
    if not previous.is_relative_to(out.resolve()):
        raise ValueError('Input must belong to this qualification campaign')
    m.q.inputs.no_redirect(previous)
    prior = json.loads(previous.read_text())
    product = json.loads((out / 'product-isolation.json').read_text())
    destination = Path(product['savePath']).resolve()
    m.q.inputs.no_redirect(destination)
    initial = phase in ('two-floor-wait', 'two-floor-profile', 'v1-upgrade')
    if initial:
        if prior['Product'] != product['product']:
            raise ValueError('Seed product mismatch')
        name = prior['SaveFileName']
        seed = Path(prior['SeedPath']).resolve()
        if not seed.is_relative_to((out / 'inputs').resolve()) or m.q.sha(seed) != prior['SeedSha256']:
            raise ValueError('Seed identity mismatch')
        m.q.inputs.no_redirect(seed)
        seed_bytes = seed.read_bytes()
    else:
        if not prior.get('Passed') or prior['Product'] != product['product']:
            raise ValueError('Previous continuation did not pass')
        save = Path(prior['SavePath']).resolve()
        if save.parent != destination:
            raise ValueError('Previous product path mismatch')
        name = save.name
    if Path(name).name != name or not name.startswith('living-dungeon-b3b2-qualification-two-floor-'):
        raise ValueError('Save identity is not isolated')
    save = destination / name
    m.q.inputs.no_redirect(save)
    player = root / 'Builds/Development/Windows-living-dungeon-two-floor-20261020/Dungeon Lord.exe'
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    builds = [json.loads(p.read_text()) for p in out.glob('*-build-command.json')]
    if not player.is_file() or not any(b.get('commit') == commit and b.get('exitCode') == 0 for b in builds):
        raise ValueError('Player/source applicability requires matching successful build')
    if initial:
        # External fixture installation precedes process startup. No overwrite,
        # and no second runtime persistence or occupancy authority is created.
        destination.mkdir(parents=True, exist_ok=True)
        with save.open('xb') as stream:
            stream.write(seed_bytes)
            stream.flush()
            os.fsync(stream.fileno())
    else:
        prior_command = json.loads(previous.with_name(previous.stem + '-command.json').read_text())
        if m.q.sha(save) != prior_command['postExitSaveSha256']:
            raise ValueError('Continuation save changed since prior normal exit')
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    args = [str(player), '-batchmode', '-screen-width', '1280', '-screen-height', '720',
            '-logFile', str(log), '-b3b2ForegroundSmoke', phase, str(report), str(previous)]
    record = dict(commit=commit, playerSha256=m.q.sha(player), arguments=args,
                  preLaunchSaveSha256=m.q.sha(save),
                  startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  rendering='Development player batchmode; production scene/controller, no human visual assessment')
    with command.open('x', encoding='utf-8') as stream:
        json.dump(record, stream, indent=2)
    record['exitCode'] = subprocess.call(args, cwd=root, startupinfo=startup)
    record['finishedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    # Only inspect/copy the canonical file after the process has exited.
    record['postExitSaveSha256'] = m.q.sha(save) if save.is_file() else None
    if save.is_file():
        with (out / (label + '-post-exit.save.json')).open('xb') as stream:
            stream.write(save.read_bytes())
    command.write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(json.dumps(record))
    if record['exitCode'] or not report.is_file() or not json.loads(report.read_text()).get('Passed'):
        raise ValueError('Native qualification failed; preserve all outputs')
