"""The single H2 PR 1 native post-replace rendezvous. Evidence has no save authority."""
import json
import os
from pathlib import Path
import re
import shutil
import time

import qualification_inputs as inputs
import qualification_windows as win

CUT = 'canonical-post-replace'
PLAYER = 'Dungeon Lord.exe'
ADAPTER = Path(__file__).with_name('qualify_h2_preparation.py')
MARKER = 'cut.json'
RECEIPT = 'planned-stop.json'
STOP_CODE = 125  # Existing Windows Job.stop exit code.


def validate(stage, plan):
    spec = stage['planned_stop']
    if (plan['purpose'] != 'qualification' or stage['id'] != 'interrupt' or
            stage['role'] != 'report' or 'xml' in stage or
            set(spec) != {'cut', 'nonce'} or spec['cut'] != CUT or
            not re.fullmatch('[0-9a-f]{32}', spec['nonce'])):
        raise ValueError('Unsupported planned-stop contract')
    if (len(stage['command']) != 6 or Path(stage['command'][1]).resolve() != ADAPTER.resolve() or
            stage['command'][2:4] != ['interrupt', '{stage}'] or stage['command'][5] != spec['nonce']):
        raise ValueError('Planned stop requires the fixed H2 player adapter')
    if not set((MARKER, RECEIPT, 'stop-intent.json', 'player.json', 'raw-save')).issubset(stage['artifacts']):
        raise ValueError('Planned stop must retain marker, player, receipt and raw save')
    for selection in plan['selections'].values():
        if stage['id'] in selection and not {'build', 'reopen-one', 'reopen-two'}.issubset(selection):
            raise ValueError('Planned stop requires the build and both ordinary reopens')


def slot_path(state, nonce):
    product = state['plan']['isolation']['product']
    if not re.fullmatch(r'DL B3B2 Smoke Two Floor Q[0-9a-f]{8}', product):
        raise ValueError('Invalid H2 qualification product')
    return (Path.home() / 'AppData/LocalLow/gdg3417' / product / nonce[:12] /
            ('living-dungeon-b3b2-qualification-' + nonce + '.json'))


def read_marker_value(path):
    inputs.no_redirect(path)
    with path.open('rb') as stream:
        data = stream.read(4097)
    if len(data) > 4096:
        raise ValueError('Oversized planned-stop marker')
    return json.loads(data)


def verify_marker(directory, state, stage, digest, read):
    spec = stage['planned_stop']
    marker = read_marker_value(directory / MARKER)
    if not isinstance(marker, dict) or set(marker) != {
            'Cut', 'Nonce', 'Pid', 'Created', 'SavePath', 'Predecessor', 'Candidate', 'Ordinal'}:
        raise ValueError('Malformed planned-stop marker')
    if (marker['Cut'] != CUT or marker['Nonce'] != spec['nonce'] or
            type(marker['Ordinal']) is not int or marker['Ordinal'] != 1):
        raise ValueError('Planned-stop cut, nonce or ordinal mismatch')
    save = slot_path(state, spec['nonce'])
    inputs.no_redirect(save)
    if os.path.normcase(marker['SavePath']) != os.path.normcase(str(save)):
        raise ValueError('Planned-stop save path mismatch')
    player = directory.parent / 'build/player' / PLAYER
    inputs.no_redirect(player)
    seal = read(directory.parent / 'build/build-seal.json')
    child = read(directory / 'player.json')
    if seal['source'] != state['source_sha'] or digest(player) != seal['files'][PLAYER]:
        raise ValueError('Planned-stop player image mismatch')
    expected = {'pid': marker['Pid'], 'created': marker['Created'],
                'executable': os.path.normcase(str(player))}
    if (type(marker['Pid']) is not int or marker['Pid'] <= 0 or
            type(marker['Created']) is not int or marker['Created'] <= 0 or
            child['process'] != expected or child['nonce'] != spec['nonce'] or
            child['save'] != str(save) or child['source'] != state['source_sha'] or
            child['image_sha256'] != seal['files'][PLAYER]):
        raise ValueError('Planned-stop process identity mismatch')
    original, candidate = marker['Predecessor'], marker['Candidate']
    if (not all(isinstance(x, str) and re.fullmatch('[0-9a-f]{64}', x) for x in (original, candidate))
            or original == candidate):
        raise ValueError('Planned-stop byte identities invalid')
    rollback = save.with_name(save.name + f'.canonical-write-{original[:16]}-{candidate[:16]}.rollback')
    inputs.no_redirect(rollback)
    if (save.stat().st_nlink != 1 or rollback.stat().st_nlink != 1 or
            digest(save) != candidate or digest(rollback) != original):
        raise ValueError('Planned-stop durable active/rollback bytes mismatch')
    return marker, expected, save, rollback


def wait_for_cut(host, event, timeout, job, directory, state, stage, digest, read):
    deadline = time.monotonic() + timeout
    rejection = None
    while True:
        remaining = deadline - time.monotonic()
        outcome = win.wait(host, event, min(0.05, max(0, remaining)))
        if outcome != 'timeout' or remaining <= 0:
            return outcome, None, rejection
        if not (directory / MARKER).is_file() or not (directory / 'player.json').is_file():
            continue
        try:
            mark, identity, save, rollback = verify_marker(directory, state, stage, digest, read)
            handle = job.hold_member(identity)
        except (OSError, ValueError, KeyError, TypeError) as error:
            rejection = str(error)
            continue
        outcome = win.wait(host, event, 0)
        if outcome != 'timeout':
            win.close(handle)
            return outcome, None, rejection
        return 'planned-stop', (mark, identity, save, rollback, handle), rejection


def capture(directory, stop, host_exit, digest):
    marker, identity, save, rollback, handle = stop
    exit_code = win.process_exit_code(handle)
    if exit_code != STOP_CODE or host_exit != STOP_CODE:
        raise ValueError('Unexpected OS exit from owned planned-stop job')
    raw = directory / 'raw-save'
    raw.mkdir()
    files = {}
    for path in sorted(save.parent.iterdir()):
        inputs.no_redirect(path)
        if not path.is_file() or path.stat().st_nlink != 1:
            raise ValueError('Unexpected entry in owned save directory')
        target = raw / path.name
        shutil.copyfile(path, target)
        files[path.name] = {'bytes': target.stat().st_size, 'sha256': digest(target)}
        if files[path.name]['sha256'] != digest(path):
            raise ValueError('Raw save changed during capture')
    if (files[save.name]['sha256'] != marker['Candidate'] or
            files[rollback.name]['sha256'] != marker['Predecessor']):
        raise ValueError('Raw capture does not match the pre-stop identities')
    return dict(cut=marker['Cut'], nonce=marker['Nonce'], process=identity, player_exit_code=exit_code,
                host_exit_code=host_exit, job_empty=True, save=str(save), files=files)


def completed(stage, record):
    return (record.get('exit_code') == STOP_CODE and record.get('completion') == 'PLANNED_STOP'
            if 'planned_stop' in stage else record.get('exit_code') == 0)
