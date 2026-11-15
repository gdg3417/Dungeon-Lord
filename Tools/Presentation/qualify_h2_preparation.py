"""Plan and score the PR 1 Windows Bootstrap reference and owned interruption."""
import argparse
import collections
import json
import math
import os
from pathlib import Path
import shutil
import statistics
import subprocess
import sys
import time
import uuid
import zipfile

import qualification_inputs as inputs
import qualification_planned_stop as stop
import qualification_supervisor as q
import qualification_windows as win

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve()
SEEDS = {'v1': 'bd72748faa586c6648faf6f4608fc36969744eba871b7317439ccf75cc8bc6f3',
         'v2': '35f80768d0b9a8f5223547bd645669aaa3c5a8cfb1d17fd44d1df8086169bd73'}
MODES = {'interrupt': 'h2-interrupt', 'reopen-one': 'h2-reopen', 'reopen-two': 'h2-reopen',
         'live-v1': 'h2-live', 'live-v2': 'h2-live', 'memory': 'h2-memory'}


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def player_tree(directory):
    return {name: q.digest(directory / name) for name in q.sorted_file_names(directory)}


def windows_process(command, *, hidden=False):
    startup = subprocess.STARTUPINFO()
    if hidden:
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = subprocess.SW_HIDE
    return subprocess.Popen(command, cwd=ROOT, stdin=subprocess.DEVNULL, startupinfo=startup)


def plan(prep, editor, v1, v2, transitions):
    require(not q.git(ROOT, 'status', '--porcelain'), 'A clean qualification worktree is required')
    win.assert_no_unity()
    prep.mkdir(parents=True)
    source = q.git(ROOT, 'rev-parse', 'HEAD')
    product = 'DL B3B2 Smoke Two Floor Q' + uuid.uuid4().hex[:8]
    for key, seed in [('v1', v1), ('v2', v2)]:
        require(q.digest(seed) == SEEDS[key], 'PR #266 fixture mismatch')
        shutil.copyfile(seed, prep / f'{key}.json')
    shutil.copyfile(transitions, prep / 'generated-transitions.json')
    inventory = q.fingerprint(ROOT)
    with zipfile.ZipFile(prep / 'inputs.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
        for name in inventory:
            archive.write(ROOT / name, name)
    q.atomic(prep / 'inputs.json', inventory)
    lanes = {'build': None, **{name: uuid.uuid4().hex for name in MODES}}
    lanes['reopen-two'] = lanes['reopen-one']
    settings = dict(editor=str(editor), source=source, product=product, lanes=lanes)
    q.atomic(prep / 'settings.json', settings)
    state = {'plan': {'isolation': {'product': product}}}
    product_dir = stop.slot_path(state, lanes['interrupt']).parent.parent
    inputs.no_redirect(product_dir)
    require(not product_dir.exists(), 'The isolated product directory already exists')
    commands = []

    def stage(name, role, mode, timeout, depends, evidence, extra=(), **options):
        command = [sys.executable, str(HERE), mode, '{stage}', str(prep), *extra]
        commands.append(dict(id=name, role=role, command=command, timeout_seconds=timeout,
                             depends=depends, artifacts=evidence, **options))

    for name, nonce in lanes.items():
        pre = name + '-preflight'
        stage(pre, 'preflight', 'preflight', 120, [commands[-1]['id']] if commands else [],
              ['preservation.json'], [pre])
        if name == 'build':
            stage(name, 'build', name, 1800, [pre],
                  ['build-report.json', 'build-seal.json', 'editor-command.json', 'unity.log', 'generated-inputs', 'player'])
        elif name == 'interrupt':
            stage(name, 'report', name, 180, [pre],
                  ['launch.json', 'launch.save.json', 'manifest.json', 'player.json', 'cut.json', 'stop-intent.json',
                   'planned-stop.json', 'raw-save', 'player.log'], [nonce],
                  planned_stop={'cut': stop.CUT, 'nonce': nonce})
        else:
            stage(name, 'report', 'player', 180, [pre],
                  ['launch.json', 'launch.save.json', 'manifest.json', 'player.json', 'player.log', 'report.json',
                   'report.json.save.json', 'post-exit.save.json', 'verification.json'], [name])
    stage('preservation', 'final', 'finish', 180, [s['id'] for s in commands], ['preservation.json'])
    files = (HERE, editor, Path(sys.executable), prep / 'inputs.json', prep / 'inputs.zip', prep / 'settings.json',
             prep / 'v1.json', prep / 'v2.json', prep / 'generated-transitions.json')
    data = dict(purpose='qualification', source_sha=source,
                isolation=dict(project=str(ROOT), product=product, save=str(product_dir), owner='H2 PR 1',
                               retention='Retain qualified build, raw incident, clones, receipts and tooling inputs'),
                tools={str(p): {'sha256': q.digest(p), 'version': 'H2 PR 1 pinned tool or input'} for p in files},
                approved_substitutions=[
                    'PR #266 v1 seed and previously qualified v2 wait-state, cloned byte-for-byte into fresh slots.',
                    'Existing Bootstrap and native writer in a Windows Development build using a per-build define.',
                    'Only predeclared exact Unity-generated transitions may be restored from the input archive.'
                ], stages=commands, selections={'windows': [s['id'] for s in commands]})
    path = prep / 'plan.json'
    q.validate_plan(data)
    q.atomic(path, data)
    print(path)


def preservation(stage, prep, settings, state, final=False):
    expected = q.read(prep / 'inputs.json')
    require(expected == q.fingerprint(ROOT), 'Exact project inputs changed')
    require(q.git(ROOT, 'rev-parse', 'HEAD') == settings['source'], 'Source changed')
    product_root = stop.slot_path(state, settings['lanes']['interrupt']).parent.parent
    inputs.no_redirect(product_root)
    slots = []
    for nonce in sorted({n for n in settings['lanes'].values() if n is not None}):
        slot = stop.slot_path(state, nonce)
        inputs.no_redirect(slot)
        if slot.exists():
            slots.append({'path': str(slot), 'files': player_tree(slot.parent), 'retention': 'retained'})
    record = dict(source=settings['source'], input_count=len(expected), input_identity=q.signature(expected),
                  stage=stage.name, isolated_slots=slots, product=str(product_root))
    if final:
        incident = q.read(stage.parent / 'interrupt/planned-stop.json')
        raw = {name: row['sha256'] for name, row in incident['files'].items()}
        require(player_tree(stage.parent / 'interrupt/raw-save') == raw, 'Raw incident changed')
        require(player_tree(Path(incident['save']).parent) == raw, 'Original stop directory changed')
        require(player_tree(stage.parent / 'build/player') == q.read(stage.parent / 'build/build-seal.json')['files'],
                'Player tree changed')
        record['raw_incident_preserved'] = True
    q.atomic(stage / 'preservation.json', record)


def build(stage, prep, settings):
    command = [settings['editor'], '-batchmode', '-nographics', '-quit', '-projectPath', str(ROOT),
               '-executeMethod', 'H2PlayerQualificationBuild.Build', '-h2Product', settings['product'],
               '-h2Output', str(stage), '-logFile', str(stage / 'unity.log')]
    q.atomic(stage / 'editor-command.json', {'arguments': command, 'identity': q.digest(command[0])})
    code = windows_process(command, hidden=True).wait()
    before, after = q.read(prep / 'inputs.json'), q.fingerprint(ROOT)
    changes = {name: {'before': before.get(name), 'after': after.get(name)}
               for name in sorted(before.keys() | after.keys()) if before.get(name) != after.get(name)}
    generated = stage / 'generated-inputs'
    generated.mkdir()
    for name in changes:
        if name in after:
            target = generated / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)
    q.atomic(generated / 'observed.json', {'changes': changes, 'editor_exit': code})
    allowed = q.read(prep / 'generated-transitions.json')
    require(all(allowed.get(name) == delta and name in before for name, delta in changes.items()),
            'Unknown generated input transition, preserved for review')
    with zipfile.ZipFile(prep / 'inputs.zip') as archive:
        for name in changes:
            target = ROOT / name
            require(q.digest(target) == after[name], 'Input drift during restoration')
            target.write_bytes(archive.read(name))
    require(q.fingerprint(ROOT) == before, 'Exact build input restoration failed')
    q.atomic(generated / 'restored.json', {'identity': q.signature(before), 'files': list(changes)})
    if code:
        return code
    result = q.read(stage / 'build-report.json')
    require(result['Result'] == 'Succeeded' and result['Errors'] == 0 and result['Development'] and
            result['Product'] == settings['product'] and result['Platform'] == 'StandaloneWindows64' and
            result['Scenes'] == ['Assets/_Project/Scenes/Bootstrap.unity'] and
            result['Define'] == 'DUNGEON_LORD_H2_QUALIFICATION', 'Invalid build report')
    files = player_tree(stage / 'player')
    require(stop.PLAYER in files, 'Build player image is absent')
    q.atomic(stage / 'build-seal.json', {'source': settings['source'], 'report': result, 'files': files})
    return 0


def read_save(path):
    data = q.read(path)
    require(data['schemaVersion'] == 17, 'Expected precise schema 17')
    primary = data['primary']
    owner = primary['livingDungeon']['Scheduler']
    time = primary['timeProvenance']
    require(owner and owner['ClosedThrough'] <= owner['AcceptedThrough'] and
            time['Mode'] == 'preciseRecovery', 'Invalid scheduler or provenance shape')
    return primary, owner, time


def cut_state(path):
    primary, owner, time = read_save(path)
    return dict(A=owner['AcceptedThrough'], C=owner['ClosedThrough'],
                W=[primary['lastSavedUtcUnix'], time['AccountedFraction100ns']], episode=time['Episode'],
                context=time['RecoveryContextSha256'], ticks=primary['totalTicks'],
                sequence=primary['runHistory']['NextRunSequence'])


def reference_score(report):
    frames = report['Frames']
    require(len(frames) == report['FrameCount'] >= 120 and
            sum(f['Seconds'] for f in frames) >= 15 and all(
                math.isfinite(f['Seconds']) and f['Seconds'] > 0 and f['Closed'] <= f['Accepted'] for f in frames),
            'Incomplete or invalid frame series')
    require(all(f['Focused'] for f in frames) and frames[-1]['EligibleThrough'] > frames[0]['EligibleThrough'],
            'Live timing requires observed focus and advancing production time')
    categories = collections.defaultdict(list)
    for prior, frame in zip(frames, frames[1:]):
        category = ('settlement' if frame['HistoryCount'] != prior['HistoryCount'] else
                    'admission' if frame['NextSequence'] != prior['NextSequence'] else
                    'tick' if frame['Tick'] != prior['Tick'] else
                    'movement_or_run_work' if frame['Ordinal'] != prior['Ordinal'] else 'ordinary')
        categories[category].append(frame['Seconds'] * 1000)

    def summary(values):
        ordered = sorted(values)
        return dict(n=len(ordered), median=statistics.median(ordered), mean=statistics.fmean(ordered),
                    p95=ordered[math.ceil(.95 * len(ordered)) - 1], max=ordered[-1])

    counters = {}
    for key in ('MainThreadNs', 'RenderThreadNs', 'AllocatedBytes'):
        values = [f[key] for f in frames]
        counters[key] = summary(values) if min(values) >= 0 and max(values) > 0 else None
    memory = report.get('Memory') or []
    return dict(frame_ms=summary([f['Seconds'] * 1000 for f in frames]),
                workload_ms={key: summary(values) for key, values in categories.items()}, counters=counters,
                writes=frames[-1]['AttemptedWrites'], publications=frames[-1]['Publications'],
                elapsed_eligible_units=frames[-1]['EligibleThrough'] - frames[0]['EligibleThrough'],
                peak_checked_arrays=max((m['CheckedPayloadBytes'] for m in memory), default=None),
                peak_sampled_managed=max((m['ManagedHeap'] for m in memory), default=None) if report['ManagedCalibrated'] else None,
                peak_sampled_unity_allocated=max((m['UnityAllocated'] for m in memory), default=None) if report['NativeCalibrated'] else None,
                memory_boundaries=dict(collections.Counter(m['Phase'] for m in memory)))


def play(stage, prep, settings, state, nonce=None):
    name = stage.name
    require(name in MODES, 'Unrecognized player lane')
    expected = settings['lanes'][name]
    require(nonce is None or nonce == expected, 'Unexpected interruption nonce')
    slot = stop.slot_path(state, expected)
    inputs.no_redirect(slot)
    player = stage.parent / 'build/player' / stop.PLAYER
    seal = q.read(stage.parent / 'build/build-seal.json')
    require(seal['source'] == settings['source'] and player_tree(player.parent) == seal['files'], 'Unsealed player')
    if name == 'reopen-one':
        captured = stage.parent / 'interrupt'
        incident = q.read(captured / 'planned-stop.json')
        raw = captured / 'raw-save'
        require(player_tree(raw) == {n: d['sha256'] for n, d in incident['files'].items()}, 'Raw incident changed')
        old_name = Path(incident['save']).name
        slot.parent.mkdir(parents=True)
        for file in raw.iterdir():
            require(file.name.startswith(old_name), 'Unexpected owned sidecar identity')
            shutil.copyfile(file, slot.parent / (slot.name + file.name[len(old_name):]))
        before = cut_state(slot)
        rollback = next(p for p in raw.iterdir() if p.name.endswith('.rollback'))
        prior = cut_state(rollback)
        require(before['A'] > prior['A'] and before['C'] >= prior['C'] and before['W'] >= prior['W'] and
                before['episode'] is None and prior['episode'] is None and before['context'] == prior['context'],
                'Interruption missed the scheduled foreground replacement')
    elif name == 'reopen-two':
        record = q.read(stage.parent / 'reopen-one/verification.json')
        require(q.digest(slot) == record['exit_sha256'], 'Reopen clone changed')
    else:
        seed = prep / ('v1.json' if name == 'live-v1' else 'v2.json')
        slot.parent.mkdir(parents=True)
        with slot.open('xb') as stream:
            stream.write(seed.read_bytes())
            stream.flush()
            os.fsync(stream.fileno())
    initial = q.digest(slot)
    shutil.copyfile(slot, stage / 'launch.save.json')
    manifest = stage / 'manifest.json'
    q.atomic(manifest, {'Nonce': expected, 'Product': settings['product']})
    report_path = stage / ('cut.json' if name == 'interrupt' else 'report.json')
    command = [str(player), '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
               '-logFile', str(stage / 'player.log'), '-b3b2ForegroundSmoke', MODES[name], str(report_path), str(manifest)]
    q.atomic(stage / 'launch.json', {'arguments': command, 'source': settings['source'], 'input_sha256': initial,
                                   'seed': str(slot), 'product': settings['product'], 'cache': 'OS caches not flushed',
                                   'process': 'fresh', 'disk_free_bytes': shutil.disk_usage(slot.parent).free,
                                   'started_utc': q.utc(), 'stage': name})
    started = time.monotonic()
    child = windows_process(command)
    identity = win.identity(child.pid)
    require(identity is not None, 'No live qualifying player identity')
    q.atomic(stage / 'player.json', {'process': identity, 'nonce': expected, 'save': str(slot),
                                    'source': settings['source'], 'image_sha256': seal['files'][stop.PLAYER]})
    code = child.wait()
    if code or name == 'interrupt':
        return code
    report = q.read(report_path)
    require(report['Passed'] and report['Product'] == settings['product'] and
            report['Mode'] == MODES[name], 'Player scenario failed')
    final = stage / 'post-exit.save.json'
    shutil.copyfile(slot, final)
    at_report = stage / 'report.json.save.json'
    result = dict(exit_sha256=q.digest(final), report_sha256=q.digest(at_report), initial_sha256=initial,
                  utc=q.utc(), launch_through_exit_seconds=time.monotonic() - started, final=cut_state(final))
    if name.startswith('reopen-'):
        before = stage / 'launch.save.json'
        initial_cut, report_cut = cut_state(before), cut_state(at_report)
        require(report['LoadedSha256'] == initial and report_cut['A'] == initial_cut['A'] and
                report_cut['C'] == initial_cut['A'] and report_cut['episode'] is None and
                report_cut['W'] >= initial_cut['W'] and report_cut['context'] == initial_cut['context'],
                'Reopen did not select and drain the exact captured cut')
        domains = ['livingDungeon', 'runHistory', 'researchPending', 'researchProgress', 'completedResearch']
        if initial_cut['A'] == initial_cut['C']:
            a, b, z = (q.read(p)['primary'] for p in (before, at_report, final))
            require(all(a.get(key) == b.get(key) == z.get(key) for key in domains),
                    'Unexpected replay, allocation, Research or settlement on reopen')
        result.update(boot_sha256=report['LoadedSha256'], before=initial_cut, recovered=report_cut)
    else:
        result['reference'] = reference_score(report)
    q.atomic(stage / 'verification.json', result)
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('plan', 'preflight', 'build', 'interrupt', 'player', 'finish'))
    parser.add_argument('paths', nargs='+')
    args = parser.parse_args()
    if args.mode == 'plan':
        plan(*(Path(p).absolute() for p in args.paths))
        return 0
    stage, prep = (Path(p).absolute() for p in args.paths[:2])
    campaign, state = q.load(stage.parent)
    settings = q.read(prep / 'settings.json')
    require(campaign == stage.parent and state['project'] == str(ROOT) and
            settings['product'] == state['plan']['isolation']['product'], 'Campaign adapter identity mismatch')
    if args.mode in ('preflight', 'player'):
        require(args.paths[2] == stage.name, 'Unexpected declared stage')
    if args.mode == 'build':
        return build(stage, prep, settings)
    if args.mode in ('preflight', 'finish'):
        preservation(stage, prep, settings, state, args.mode == 'finish')
        return 0
    return play(stage, prep, settings, state, args.paths[2] if args.mode == 'interrupt' else None)


if __name__ == '__main__':
    sys.exit(main())
