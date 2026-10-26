"""Explicit, completion-driven qualification plans. No scheduler or notification service.

Commands are trusted reviewed code, not a sandbox. See the workflow before Unity use.
Only Windows is supported. Status never launches, repairs, or relaunches anything.
"""
import argparse
import datetime as dt
import hashlib
import json
import math
import msvcrt
import os
from pathlib import Path
import re
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET

import qualification_inputs as inputs
import qualification_windows as win

_detached_processes = []  # Keep launch handles alive; ownership is in durable records.


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))


def atomic(path, data):
    """Same-directory exclusive temporary, flush then replace. Old verdict survives failure."""
    path = Path(path)
    inputs.no_redirect(path)
    temp = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    with temp.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump({k: v for k, v in data.items() if not k.startswith('_')} if isinstance(data, dict) else data,
                  stream, indent=2, sort_keys=True)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temp, path)


def git(root, *args):
    return subprocess.check_output(['git', *args], cwd=root, text=True).strip()


def fingerprint(root):
    # Reuse the exact inventory/reparse authority; include ignored generated files.
    for name in inputs.INPUT_ROOTS:
        if not (root / name).is_dir(): raise ValueError('Missing input root: ' + name)
    return {name: digest(root / name) for name in sorted(inputs.target_inventory(root))}


def signature(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True).encode()).hexdigest()


def inside(root, relative):
    path = root / relative
    inputs.no_redirect(path)
    if Path(relative).is_absolute() or not path.resolve().is_relative_to(root.resolve()) or path == root:
        raise ValueError('Output must stay inside campaign: ' + str(relative))
    return path


class Lease:
    """OS lifetime lock plus persistent unresolved-campaign ownership."""
    def __init__(self, project):
        # Unity owns and may clear Temp during Editor startup. Reservation and
        # execution-lock identity must outlive that lifecycle, including failure.
        self.base = project / 'TestResults/qualification-supervisor-reservation'
        self.legacy_base = project / 'Temp/qualification-supervisor'
        inputs.no_redirect(self.base)
        inputs.no_redirect(self.legacy_base)
        self.base.mkdir(parents=True, exist_ok=True)

    def __enter__(self):
        self.stream = (self.base / 'execution.lock').open('a+b')
        self.stream.seek(0)
        try:
            msvcrt.locking(self.stream.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError:
            self.stream.close()
            raise ValueError('Another campaign supervisor owns this project') from None
        return self

    def __exit__(self, *unused):
        self.stream.seek(0)
        msvcrt.locking(self.stream.fileno(), msvcrt.LK_UNLCK, 1)
        self.stream.close()

    def claim(self, campaign):
        legacy = self.legacy_base / 'active.json'
        if legacy.exists() and read(legacy).get('campaign') is not None:
            raise ValueError('Legacy unresolved campaign requires approved recovery; no automatic migration')
        path = self.base / 'active.json'
        current = read(path) if path.exists() else {}
        if current.get('campaign') not in (None, str(campaign)):
            raise ValueError('Conflicting unresolved campaign: ' + current['campaign'])
        atomic(path, dict(campaign=str(campaign)))

    def release(self, campaign):
        atomic(self.base / 'active.json', dict(campaign=None, previous=str(campaign)))


def validate_plan(plan):
    if plan['purpose'] not in ('fixture', 'qualification'):
        raise ValueError('Unknown plan purpose')
    stages = plan['stages']
    by_id = {s['id']: s for s in stages}
    if not stages or len(by_id) != len(stages):
        raise ValueError('Empty or duplicate stage IDs')
    seen, commands = set(), set()
    for stage in stages:
        if not re.fullmatch('[a-zA-Z0-9_-]+', stage['id']):
            raise ValueError('Unsafe stage ID')
        if not set(stage['depends']).issubset(seen):
            raise ValueError('Dependencies must precede the stage')
        seen.add(stage['id'])
        command = stage['command']
        if not isinstance(command, list) or not command or any(not isinstance(v, str) for v in command):
            raise ValueError('Expected executable and argument array')
        if not Path(command[0]).is_absolute() or not Path(command[0]).is_file():
            raise ValueError('Executable must be an existing absolute path')
        if plan['purpose'] == 'fixture':
            fixture_path = Path(__file__).with_name('test_qualification_supervisor.py').resolve()
            if len(command) < 3 or Path(command[1]).resolve() != fixture_path or command[2] != '--fixture':
                raise ValueError('Fixture purpose is restricted to the disposable test helper')
        key = tuple(command)
        if key in commands:
            raise ValueError('Duplicate equivalent command')
        commands.add(key)
        if not math.isfinite(stage['timeout_seconds']) or not 0 < stage['timeout_seconds'] < 4294967:
            raise ValueError('Invalid timeout')
        if stage['role'] not in ('preflight', 'test', 'build', 'report', 'final'):
            raise ValueError('Unknown stage role')
        if stage['role'] == 'test' and ('xml' not in stage or stage['expected_tests'] <= 0):
            raise ValueError('Test stage requires XML and explicit positive discovery count')
        if stage['role'] in ('build', 'report') and not stage.get('artifacts'):
            raise ValueError('Build/report stage needs artifacts')
        for name in [*stage.get('artifacts', []), *([stage['xml']] if 'xml' in stage else [])]:
            if not name or Path(name).is_absolute() or '..' in Path(name).parts or ':' in name:
                raise ValueError('Artifact must belong to its own fresh stage directory')
    for selection in plan['selections'].values():
        if not selection or len(set(selection)) != len(selection) or not set(selection).issubset(by_id):
            raise ValueError('Invalid explicit stage selection')
    if plan['purpose'] == 'qualification':
        if not plan.get('tools') or not plan.get('isolation') or not plan.get('approved_substitutions'):
            raise ValueError('Qualification needs pinned tools, isolation and approved substitutions')
        for stage in stages:
            if stage['role'] in ('test', 'build', 'report'):
                if not any(by_id[d]['role'] == 'preflight' for d in stage['depends']):
                    raise ValueError('Each workload needs a direct preservation/exact-input preflight dependency')
        if not any(s['role'] == 'final' for s in stages):
            raise ValueError('Final preservation check required')


def create(project, plan, selection):
    project = Path(project).absolute()
    inputs.no_redirect(project)
    validate_plan(plan)
    if git(project, 'rev-parse', 'HEAD') != plan['source_sha']:
        raise ValueError('Invalid source SHA')
    if not re.fullmatch('[0-9a-f]{40}', plan['source_sha']):
        raise ValueError('Full source SHA required')
    selected = set(plan['selections'][selection])
    for stage in plan['stages']:
        if stage['id'] in selected and not set(stage['depends']).issubset(selected):
            raise ValueError('Select dependencies explicitly: ' + stage['id'])
    ordered = [s for s in plan['stages'] if s['id'] in selected]
    if plan['purpose'] == 'qualification':
        if ordered[-1]['role'] != 'final' or set(ordered[-1]['depends']) != selected - {ordered[-1]['id']}:
            raise ValueError('Selected final preservation stage must depend on every selected stage')
        if plan['isolation']['product'] == 'Dungeon Lord' or plan['isolation']['save'] == 'save_primary.json':
            raise ValueError('Owner product/save forbidden')
    campaign = project / 'TestResults/qualification-supervisor' / uuid.uuid4().hex
    state = dict(schema=1, id=campaign.name, project=str(project), campaign=str(campaign),
                     source_sha=plan['source_sha'], inputs=fingerprint(project), plan=plan,
                     selection=selection, selected=[s['id'] for s in ordered], created_utc=utc(),
                     status='READY', stages={}, host=dict(python=sys.version, platform=sys.platform),
                     supervisor_tools={str(Path(__file__).resolve()): digest(__file__),
                                       str(Path(win.__file__).resolve()): digest(win.__file__),
                                       str(Path(inputs.__file__).resolve()): digest(inputs.__file__)})
    # Validate before claiming project ownership; bad plans cannot strand a lease.
    paths = {s['command'][0] for s in ordered} | set(plan.get('tools', {}))
    state['tools'] = {str(Path(p).absolute()): digest(p) for p in paths}
    for path, metadata in plan.get('tools', {}).items():
        if state['tools'][str(Path(path).absolute())] != metadata['sha256'] or not metadata['version']:
            raise ValueError('Tool hash/version mismatch: ' + path)
    with Lease(project) as lease:
        lease.claim(campaign)
        campaign.mkdir(parents=True, exist_ok=False)
        atomic(campaign / 'campaign.json', state)
    return campaign


def load(campaign):
    campaign = Path(campaign).absolute()
    inputs.no_redirect(campaign)
    state = read(campaign / 'campaign.json')
    if state['id'] != campaign.name or state['campaign'] != str(campaign):
        raise ValueError('Campaign identity mismatch')
    expected = Path(state['project']) / 'TestResults/qualification-supervisor' / state['id']
    if campaign != expected or not re.fullmatch('[0-9a-f]{32}', state['id']):
        raise ValueError('Unexpected campaign location/identity')
    return campaign, state


def applicable(state, expected=None):
    project = Path(state['project'])
    if git(project, 'rev-parse', 'HEAD') != state['source_sha']:
        raise ValueError('Source no longer applicable')
    current = fingerprint(project)
    if signature(current) != (expected or signature(state['inputs'])):
        raise ValueError('Inputs no longer applicable')
    for path, sha in (state['tools'] | state['supervisor_tools']).items():
        if digest(path) != sha:
            raise ValueError('Tool changed: ' + path)
    return signature(current)


def current_input_identity(state):
    expected = None
    for stage in state['plan']['stages']:
        if stage['id'] not in state['selected']: continue
        record = state['stages'].get(stage['id'])
        if not record: break
        if record['status'] == 'PASSED':
            expected = stage.get('after_input_identity', stage.get('input_identity'))
        else:
            expected = stage.get('input_identity')
            break
    return expected


class NUnitError(ValueError):
    def __init__(self, message, counts):
        super().__init__(message)
        self.counts = counts


def nunit(path, expected, allowed_skips):
    root = ET.parse(path).getroot()
    cases = list(root.iter('test-case'))
    names = [c.get('fullname') for c in cases]
    ids = [c.get('id') for c in cases]
    if None in names or None in ids or len(set(ids)) != len(ids):
        raise ValueError('Missing or duplicate test identity')
    counts = dict(discovered=len(cases), passed=0, failed=0, skipped=0)
    skips = []
    for case in cases:
        result = case.get('result')
        if result == 'Passed': counts['passed'] += 1
        elif result == 'Failed': counts['failed'] += 1
        elif result == 'Skipped':
            counts['skipped'] += 1
            skips.append(case.get('fullname'))
        else: raise NUnitError('Incomplete/inconclusive case result', counts)
    counts['skips'] = skips
    def reject(message): raise NUnitError(message, counts)
    if root.tag != 'test-run' or root.get('result') not in ('Passed', 'Failed', 'Skipped', 'Skipped:Ignored') or not root.get('end-time'):
        reject('Incomplete NUnit test-run')
    if counts['discovered'] != expected or int(root.get('total', '-1')) != len(cases):
        reject('Incorrect discovered test count')
    for name in ('passed', 'failed', 'skipped'):
        if int(root.get(name, '-1')) != counts[name]:
            reject('Root and case count mismatch')
    if int(root.get('inconclusive', '-1')) != 0:
        reject('Inconclusive tests')
    if sorted(skips) != sorted(allowed_skips):
        reject('Unexpected or missing expected skips')
    # Unity's retained full suites report Skipped:Ignored at root when named,
    # expected ignored cases are present. Leaf counts/skip identities remain exact.
    if counts['failed'] or root.get('result') == 'Failed' or (root.get('result') != 'Passed' and not skips):
        reject('Failed tests')
    return counts


def evidence(campaign, stage, record):
    rows = {}
    missing = []
    for name in ['stdout.log', 'started.json', 'child.json', 'exit.json',
                 *stage.get('artifacts', []), *([stage['xml']] if 'xml' in stage else [])]:
        path = inside(campaign / stage['id'], name)
        if path.is_dir():
            rows[name] = {n: digest(path / n) for n in sorted_file_names(path)}
            if not rows[name]: raise ValueError('Empty artifact directory')
        elif path.is_file():
            rows[name] = digest(path)
        else: missing.append(str(path))
    if record.get('evidence') is not None and rows != record['evidence']:
        raise ValueError('Completed evidence changed')
    record['evidence'] = rows
    if missing: raise ValueError('Missing evidence: ' + ', '.join(missing))
    if 'xml' in stage:
        record['tests'] = nunit(inside(campaign / stage['id'], stage['xml']),
                                stage['expected_tests'], stage.get('allowed_skips', []))
    return rows


def sorted_file_names(root):
    names = []
    for folder, dirs, files in os.walk(root):
        for name in dirs + files: inputs.no_redirect(Path(folder) / name)
        names.extend((Path(folder) / n).relative_to(root).as_posix() for n in files)
    return sorted(names)


def child_host(campaign, stage_id):
    """Blocked until parent has assigned the job and durably recorded ownership."""
    if sys.stdin.readline().strip() != 'GO':
        return 124
    campaign, state = load(campaign)
    record = read(campaign / stage_id / 'started.json')
    process = subprocess.Popen(record['command'], cwd=state['project'], stdin=subprocess.DEVNULL)
    actual = dict(command=record['command'], process=win.identity(process.pid), started_utc=utc())
    if not actual['process']:
        raise ValueError('Child creation identity unavailable; completion cannot be certified')
    atomic(campaign / stage_id / 'child.json', actual)
    code = process.wait()  # OS completion, no model activity
    atomic(campaign / stage_id / 'exit.json', dict(**actual, exit_code=code, finished_utc=utc()))
    return 0


def execute(campaign, state, stage):
    directory = campaign / stage['id']
    directory.mkdir(exist_ok=False)
    values = dict(project=state['project'], campaign=str(campaign), stage=str(directory), id=state['id'])
    command = list(stage['command'])
    for key, value in values.items():
        command = [arg.replace('{' + key + '}', value) for arg in command]
    record = dict(campaign=state['id'], stage=stage['id'], command=command,
                  source_sha=state['source_sha'], input_identity=applicable(state, stage.get('input_identity')),
                  started_utc=utc(), status='STARTING', exit_code=None,
                  tests=dict(discovered=None, passed=None, failed=None, skipped=None),
                  job='Local\\DLQ-' + uuid.uuid4().hex)
    atomic(directory / 'started.json', record)
    state['stages'][stage['id']] = record
    atomic(campaign / 'campaign.json', state)
    job = win.Job(record['job'])
    process = None
    try:
        with (directory / 'stdout.log').open('xb') as log:
            process = subprocess.Popen([sys.executable, str(Path(__file__).resolve()), '_host',
                                        str(campaign), stage['id']], cwd=state['project'],
                                       stdin=subprocess.PIPE, stdout=log, stderr=subprocess.STDOUT,
                                       creationflags=subprocess.CREATE_NO_WINDOW)
            # Host cannot start workload before assignment and ownership publication.
            job.assign(process)
            record.update(process=win.identity(process.pid), status='RUNNING')
            if not record['process']: raise ValueError('Host vanished before ownership established')
            state['stages'][stage['id']] = record
            atomic(directory / 'started.json', record)
            atomic(campaign / 'campaign.json', state)
            process.stdin.write(b'GO\n'); process.stdin.flush(); process.stdin.close()
            outcome = win.wait(process, state['_event'], stage['timeout_seconds'])
            if outcome != 'exit':
                # Handle still owns the exact job assigned before GO; descendants only.
                job.stop()
            host_code = process.wait()
        record['host_exit_code'] = host_code
        record['finished_utc'] = utc()
        if not job.wait_empty():
            raise ValueError('Owned descendants still active; completion uncertain')
        if outcome != 'exit':
            record.update(status='FAILED', failure=outcome.upper())
        elif host_code != 0 or not (directory / 'exit.json').is_file():
            record.update(status='INCOMPLETE', failure='HOST_TERMINATED_WITHOUT_EXIT')
        else:
            result = read(directory / 'exit.json')
            actual = read(directory / 'child.json')
            if result['process'] != actual['process'] or result['command'] != command:
                raise ValueError('Child completion identity mismatch')
            record.update(child_process=result['process'], exit_code=result['exit_code'],
                          child_started_utc=result['started_utc'], child_finished_utc=result['finished_utc'])
            record['status'] = 'PASSED' if result['exit_code'] == 0 else 'FAILED'
            record['failure'] = None if result['exit_code'] == 0 else 'CHILD_NONZERO'
            # Always retain available raw evidence, even on failure.
        try:
            record['evidence'] = evidence(campaign, stage, record)
        except (ValueError, OSError, ET.ParseError) as error:
            if isinstance(error, NUnitError): record['tests'] = error.counts
            if record.get('status') == 'PASSED':
                record.update(status='FAILED', failure='INVALID_EVIDENCE', detail=str(error))
            else: record['evidence_error'] = str(error)
        applicable(state, stage.get('after_input_identity', stage.get('input_identity')))
    except Exception as error:
        record.update(status='INCOMPLETE', failure='UNCERTAIN', detail=str(error), finished_utc=utc())
        # Do not kill on crash/error. Preserve uncertain work and block replacements.
    finally:
        if process and process.stdin and not process.stdin.closed: process.stdin.close()
        job.close()
    atomic(directory / 'result.json', record)
    return record


def recover_completed(campaign, stage, old):
    """Only adopt an atomic passing terminal record, never infer exit from a report."""
    path = campaign / stage['id'] / 'result.json'
    if old['status'] not in ('RUNNING', 'STARTING') or not path.is_file(): return old
    terminal = read(path)
    if terminal['status'] != 'PASSED' or terminal.get('exit_code') != 0 or not terminal.get('finished_utc'):
        return old
    for field in ('campaign', 'stage', 'command', 'source_sha', 'input_identity', 'process', 'job'):
        if terminal.get(field) != old.get(field): raise ValueError('Recovery identity mismatch: ' + field)
    if win.matches(terminal.get('process')) or win.matches(terminal.get('child_process')):
        raise ValueError('Completed record has an unexpectedly live owned process')
    evidence(campaign, stage, terminal)
    return terminal


def run(campaign):
    campaign, state = load(campaign)
    with Lease(Path(state['project'])) as lease:
        campaign, state = load(campaign)
        if state['status'] in ('PASSED', 'FAILED', 'INCOMPLETE'):
            raise ValueError('Terminal/uncertain campaign cannot be relaunched; inspect status')
        lease.claim(campaign)
        state['supervisor_process'] = win.identity(os.getpid())
        state['event'] = 'Local\\DLQ-cancel-' + uuid.uuid4().hex
        event = win.checked(win.create_event(None, True, False, state['event']))
        # _event is a local handle, never serialized.
        try:
            for stage in state['plan']['stages']:
                if stage['id'] not in state['selected']: continue
                old = state['stages'].get(stage['id'])
                if old:
                    old = recover_completed(campaign, stage, old)
                    state['stages'][stage['id']] = old
                    if old['status'] != 'PASSED':
                        state.update(status='INCOMPLETE', detail='Prior stage has no verified passing terminal result')
                        break
                    if read(campaign / stage['id'] / 'result.json') != old or old.get('exit_code') != 0:
                        raise ValueError('Cannot reuse a missing/mismatched passing terminal manifest')
                    evidence(campaign, stage, old)
                    continue
                if (campaign / stage['id']).exists():
                    state.update(status='INCOMPLETE', detail='Uncertain prior stage directory; no relaunch')
                    break
                applicable(state, stage.get('input_identity'))
                if state['plan']['purpose'] == 'qualification': win.assert_no_unity()
                state['status'] = 'RUNNING'
                atomic(campaign / 'campaign.json', state)
                record = execute(campaign, dict(state, _event=event), stage)
                state['stages'][stage['id']] = record
                atomic(campaign / 'campaign.json', state)
                if record['status'] != 'PASSED':
                    state['status'] = record['status']
                    break
            else:
                state['status'] = 'PASSED'
            state['finished_utc'] = utc()
            atomic(campaign / 'campaign.json', state)
            if state['status'] in ('PASSED', 'FAILED'):
                lease.release(campaign)
        finally:
            win.close(event)
    return status(campaign)


def status(campaign):
    campaign, state = load(campaign)
    result = dict(campaign=state['id'], path=str(campaign), recorded_status=state['status'],
                  status=state['status'], stages={})
    try:
        if state['status'] == 'RUNNING' and not win.matches(state.get('supervisor_process')):
            result['status'] = 'INCOMPLETE'
        if state['status'] == 'READY' and (campaign / 'launch.claim').exists():
            receipt = campaign / 'launch.json'
            result['status'] = ('RUNNING' if receipt.is_file() and
                                win.matches(read(receipt).get('process')) else 'INCOMPLETE')
        selected = [s for s in state['plan']['stages'] if s['id'] in state['selected']]
        # After final preservation, a separately pinned restored input identity may apply.
        applicable(state, current_input_identity(state))
        for stage in selected:
            record = state['stages'].get(stage['id'])
            if not record:
                result['stages'][stage['id']] = 'NOT_STARTED'
                continue
            result['stages'][stage['id']] = record['status']
            if record['status'] == 'PASSED':
                terminal = read(campaign / stage['id'] / 'result.json')
                if terminal != record or record.get('exit_code') != 0 or not record.get('finished_utc'):
                    raise ValueError('Missing/mismatched terminal stage manifest')
                evidence(campaign, stage, record)
            elif record['status'] in ('RUNNING', 'STARTING', 'INCOMPLETE'):
                alive = win.matches(record.get('process'))
                result['stages'][stage['id']] = dict(status='INCOMPLETE', owned_host_running=alive)
                result['status'] = ('RUNNING' if state['status'] == 'RUNNING' and
                                    win.matches(state.get('supervisor_process')) else 'INCOMPLETE')
        if state['status'] == 'PASSED' and any(v != 'PASSED' for v in result['stages'].values()):
            raise ValueError('Incomplete selected stages')
        if state['status'] == 'PASSED' and not state.get('finished_utc'):
            raise ValueError('Missing campaign completion time')
    except Exception as error:
        result.update(status='INCOMPLETE', detail=str(error))
    return result


def cancel(campaign):
    campaign, state = load(campaign)
    # No process-id kill. An active supervisor waits on its private cancellation event.
    if not win.matches(state.get('supervisor_process')) or state['status'] != 'RUNNING':
        raise ValueError('No verified active supervisor; preserve uncertain child for owner-reviewed recovery')
    win.signal(state['event'])
    return dict(campaign=state['id'], cancellation='REQUESTED', utc=utc())


def launch(campaign, resume=False):
    campaign, state = load(campaign)
    with Lease(Path(state['project'])) as lease:
        campaign, state = load(campaign)
        if state['status'] not in ('READY', 'RUNNING'):
            raise ValueError('Cannot launch terminal campaign')
        lease.claim(campaign)
        if win.matches(state.get('supervisor_process')):
            raise ValueError('Supervisor already running; inspect once later')
        if not resume and (campaign / 'launch.claim').exists():
            raise ValueError('Already dispatched; inspect or explicitly resume, never dispatch twice')
        if resume:
            for receipt in [campaign / 'launch.json', *campaign.glob('resume-*.json')]:
                if receipt.is_file() and win.matches(read(receipt).get('process')):
                    raise ValueError('An independently dispatched supervisor is still running')
            for stage in state['plan']['stages']:
                if stage['id'] not in state['selected']: continue
                old = state['stages'].get(stage['id'])
                if old and recover_completed(campaign, stage, old)['status'] != 'PASSED':
                    raise ValueError('Uncertain previous stage; no replacement supervisor or workload')
                if not old and (campaign / stage['id']).exists():
                    raise ValueError('Uncertain stage directory; no relaunch')
                if old: state['stages'][stage['id']] = recover_completed(campaign, stage, old)
        applicable(state, current_input_identity(state))
        prefix = 'resume-' + uuid.uuid4().hex if resume else 'launch'
        # Exclusive launch receipt prevents duplicate dispatch before worker acquires lease.
        with (campaign / (prefix + '.claim')).open('x', encoding='utf-8') as reservation, \
                (campaign / (prefix + '.log')).open('xb') as log:
            reservation.write(utc())
            process = subprocess.Popen([sys.executable, str(Path(__file__).resolve()), '_dispatch', str(campaign)],
                cwd=state['project'], stdin=subprocess.PIPE,
                stdout=log, stderr=subprocess.STDOUT,
                creationflags=subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP,
                close_fds=True)
            atomic(campaign / (prefix + '.json'), dict(process=win.identity(process.pid), launched_utc=utc()))
    # Release project lease before the detached dispatcher attempts to acquire it.
    process.stdin.write(b'GO\n'); process.stdin.flush(); process.stdin.close()
    _detached_processes.append(process)
    return dict(campaign=str(campaign), launched=True, pid=process.pid)


def resume(campaign):
    return launch(campaign, resume=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['create', 'launch', 'resume', 'run', 'status', 'cancel', '_host', '_dispatch'])
    parser.add_argument('path')
    parser.add_argument('extra', nargs='?')
    parser.add_argument('--selection')
    args = parser.parse_args()
    try:
        if args.action == '_host': return child_host(args.path, args.extra)
        if args.action == '_dispatch':
            if sys.stdin.readline().strip() != 'GO': return 124
            result = run(args.path)
            print(json.dumps(result, indent=2))
            return 0 if result['status'] == 'PASSED' else 1
        if args.action == 'create':
            result = dict(campaign=str(create(args.path, read(args.extra), args.selection)))
        else: result = globals()[args.action](args.path)
        print(json.dumps(result, indent=2))
        return 0 if result.get('status', 'PASSED') in ('PASSED', 'READY', 'RUNNING') else 1
    except Exception as error:
        print(json.dumps(dict(status='INCOMPLETE', error=str(error))), file=sys.stderr)
        return 2


if __name__ == '__main__':
    sys.exit(main())
