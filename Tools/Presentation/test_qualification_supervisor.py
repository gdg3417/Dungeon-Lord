"""Windows-only disposable fake-process regressions; never opens owner saves/Unity."""
import copy
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

import qualification_supervisor as q
import qualification_windows as win

SELF = str(Path(__file__).resolve())


def fixture():
    """A tiny real child, not a mocked Popen. Writes only its supplied stage directory."""
    directory, mode, delay = Path(sys.argv[2]), sys.argv[3], float(sys.argv[4])
    with (directory / 'invocations.log').open('a') as stream: stream.write(str(os.getpid()) + '\n')
    print('disposable fixture started', flush=True)
    time.sleep(delay)
    if mode == 'abnormal': os._exit(77)
    if mode == 'partial':
        (directory / 'results.xml').write_text('<test-run>')
        return 0
    if mode == 'missing': return 0
    result = 'Failed' if mode == 'failed-tests' else 'Passed'
    skip = mode in ('skip', 'skiproot')
    xml = ('<test-run result="' + result + '" total="1" passed="' + ('0' if skip or result == 'Failed' else '1') +
           '" failed="' + ('1' if result == 'Failed' else '0') + '" skipped="' + ('1' if skip else '0') +
           '" inconclusive="0" end-time="2026-10-22T00:00:01Z">' +
           '<test-case id="1" fullname="Disposable.Case" result="' + ('Skipped' if skip else result) + '"/></test-run>')
    if mode == 'incomplete': xml = xml.replace(' end-time="2026-10-22T00:00:01Z"', '')
    if mode == 'counts': xml = xml.replace('total="1"', 'total="2"')
    if mode == 'skiproot': xml = xml.replace('test-run result="Passed"', 'test-run result="Skipped:Ignored"')
    (directory / 'results.xml').write_text(xml)
    if mode == 'changeinput':
        (directory.parents[3] / 'Assets/fixture.txt').write_bytes(b'reviewed restored input\n')
    print('disposable fixture finished', flush=True)
    return 13 if mode == 'nonzero' else 0


def repository(root):
    subprocess.run(['git', 'init', '-q', str(root)], check=True)
    for name in q.inputs.INPUT_ROOTS:
        (root / name).mkdir()
        (root / name / 'fixture.txt').write_text('disposable input\n')
    subprocess.run(['git', 'add', '.'], cwd=root, check=True)
    subprocess.run(['git', '-c', 'user.name=Fixture', '-c', 'user.email=fixture@invalid',
                    'commit', '-qm', 'disposable qualification fixture'], cwd=root, check=True)


def plan(root, modes=('ok',), delay=0):
    stages = []
    for index, mode in enumerate(modes):
        sid = 'stage' + str(index)
        stages.append(dict(id=sid, role='test', depends=[stages[-1]['id']] if stages else [],
                           command=[sys.executable, SELF, '--fixture', '{stage}', mode, str(delay), sid],
                           timeout_seconds=20, xml='results.xml', expected_tests=1, allowed_skips=[]))
    return dict(purpose='fixture', source_sha=q.git(root, 'rev-parse', 'HEAD'),
                stages=stages, selections={'focused': [s['id'] for s in stages]},
                tools={SELF: dict(sha256=q.digest(SELF), version='disposable fixture v1')})


def wait_file(path, process):
    # Test coordination only, bounded to ten seconds. Production waits on OS handles.
    deadline = time.monotonic() + 10
    while time.monotonic() < deadline:
        if path.exists(): return
        if process.poll() is not None: raise AssertionError('Supervisor exited before fixture launch')
        time.sleep(0.01)
    raise AssertionError('Timed out waiting for disposable fixture startup')


class SupervisorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='dlq-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        repository(self.root)

    def create(self, modes=('ok',), delay=0, transform=None):
        value = plan(self.root, modes, delay)
        if transform: transform(value)
        return q.create(self.root, value, 'focused')

    def test_single_and_multistage_success_and_read_only_status(self):
        for modes in (('ok',), ('ok', 'ok')):
            campaign = self.create(modes)
            self.assertEqual('PASSED', q.run(campaign)['status'])
            before = {str(p): q.digest(p) for p in campaign.rglob('*') if p.is_file()}
            self.assertEqual('PASSED', q.status(campaign)['status'])
            self.assertEqual('PASSED', q.status(campaign)['status'])
            self.assertEqual(before, {str(p): q.digest(p) for p in campaign.rglob('*') if p.is_file()})
            with self.assertRaises(ValueError): q.run(campaign)
            for sid in q.read(campaign / 'campaign.json')['selected']:
                self.assertEqual(1, len((campaign / sid / 'invocations.log').read_text().splitlines()))

    def test_child_nonzero_and_abnormal_exit_preserve_logs_reports(self):
        for mode in ('nonzero', 'abnormal'):
            campaign = self.create((mode,))
            self.assertEqual('FAILED', q.run(campaign)['status'])
            result = q.read(campaign / 'stage0/result.json')
            self.assertEqual('CHILD_NONZERO', result['failure'])
            self.assertEqual(13 if mode == 'nonzero' else 77, result['exit_code'])
            self.assertTrue((campaign / 'stage0/stdout.log').read_text())
            if mode == 'nonzero': self.assertTrue((campaign / 'stage0/results.xml').exists())

    def test_malformed_missing_incomplete_counts_and_failed_tests(self):
        for mode in ('partial', 'missing', 'incomplete', 'counts', 'failed-tests'):
            campaign = self.create((mode,))
            self.assertEqual('FAILED', q.run(campaign)['status'], mode)
            self.assertEqual('INVALID_EVIDENCE', q.read(campaign / 'stage0/result.json')['failure'])

    def test_expected_and_unexpected_skips(self):
        campaign = self.create(('skip',))
        self.assertEqual('FAILED', q.run(campaign)['status'])
        campaign = self.create(('skip',), transform=lambda p: p['stages'][0].update(allowed_skips=['Disposable.Case']))
        self.assertEqual('PASSED', q.run(campaign)['status'])
        campaign = self.create(transform=lambda p: p['stages'][0].update(allowed_skips=['Missing.Skip']))
        self.assertEqual('FAILED', q.run(campaign)['status'])
        campaign = self.create(('skiproot',), transform=lambda p: p['stages'][0].update(allowed_skips=['Disposable.Case']))
        self.assertEqual('PASSED', q.run(campaign)['status'])

    def test_wrong_expected_discovery(self):
        campaign = self.create(transform=lambda p: p['stages'][0].update(expected_tests=2))
        self.assertEqual('FAILED', q.run(campaign)['status'])

    def test_nunit_duplicate_display_names_require_distinct_test_ids(self):
        xml = ('<test-run result="Passed" total="2" passed="2" failed="0" skipped="0" '
               'inconclusive="0" end-time="2026-10-22T00:00:01Z">'
               '<test-case id="1" fullname="Same.Parameter(5)" result="Passed"/>'
               '<test-case id="2" fullname="Same.Parameter(5)" result="Passed"/></test-run>')
        path = self.root / 'report.xml'
        path.write_text(xml)
        self.assertEqual(2, q.nunit(path, 2, [])['passed'])
        path.write_text(xml.replace('id="2"', 'id="1"'))
        with self.assertRaisesRegex(ValueError, 'duplicate'): q.nunit(path, 2, [])

    def test_timeout_terminates_owned_job_only(self):
        campaign = self.create(delay=5, transform=lambda p: p['stages'][0].update(timeout_seconds=0.25))
        self.assertEqual('FAILED', q.run(campaign)['status'])
        self.assertEqual('TIMEOUT', q.read(campaign / 'stage0/result.json')['failure'])
        record = q.read(campaign / 'stage0/started.json')
        self.assertFalse(win.matches(record['process']))

    def test_invalid_source_and_changed_input(self):
        value = plan(self.root)
        value['source_sha'] = '0' * 40
        with self.assertRaisesRegex(ValueError, 'source'): q.create(self.root, value, 'focused')
        campaign = self.create()
        (self.root / 'Assets/fixture.txt').write_text('changed')
        with self.assertRaisesRegex(ValueError, 'Inputs'): q.run(campaign)
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
        self.assertFalse((campaign / 'stage0').exists())

    def test_ignored_inputs_and_changed_tools_fail_closed(self):
        campaign = self.create()
        (self.root / 'Assets/generated.meta').write_text('extra')
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])

    def test_atomic_publication_keeps_old_manifest_on_failed_replace(self):
        path = self.root / 'manifest.json'
        q.atomic(path, {'status': 'RUNNING'})
        with patch.object(q.os, 'replace', side_effect=OSError('interruption')):
            with self.assertRaises(OSError): q.atomic(path, {'status': 'PASSED'})
        self.assertEqual({'status': 'RUNNING'}, q.read(path))
        self.assertEqual(1, len(list(self.root.glob('manifest.json.*.tmp'))))

    def test_safe_resume_after_completed_stage(self):
        campaign = self.create(('ok', 'ok'))
        real_execute = q.execute
        def interrupt(c, state, stage):
            if stage['id'] == 'stage1': raise KeyboardInterrupt()
            return real_execute(c, state, stage)
        with patch.object(q, 'execute', side_effect=interrupt):
            with self.assertRaises(KeyboardInterrupt): q.run(campaign)
        self.assertEqual('PASSED', q.read(campaign / 'stage0/result.json')['status'])
        self.assertEqual('PASSED', q.run(campaign)['status'])
        self.assertEqual(1, len((campaign / 'stage0/invocations.log').read_text().splitlines()))

    def test_recover_atomic_result_before_campaign_publication(self):
        campaign = self.create(('ok', 'nonzero'))
        actual = q.execute
        def interrupted_publish(c, state, stage):
            record = actual(c, state, stage)
            if stage['id'] == 'stage0': raise KeyboardInterrupt()
            return record
        with patch.object(q, 'execute', side_effect=interrupted_publish):
            with self.assertRaises(KeyboardInterrupt): q.run(campaign)
        self.assertEqual('RUNNING', q.read(campaign / 'campaign.json')['stages']['stage0']['status'])
        self.assertEqual('FAILED', q.run(campaign)['status'])
        self.assertEqual(1, len((campaign / 'stage0/invocations.log').read_text().splitlines()))

    def test_detached_explicit_resume_after_completed_stage(self):
        campaign = self.create(('ok', 'nonzero'))
        actual = q.execute
        def interrupt(c, state, stage):
            if stage['id'] == 'stage1': raise KeyboardInterrupt()
            return actual(c, state, stage)
        with patch.object(q, 'execute', side_effect=interrupt):
            with self.assertRaises(KeyboardInterrupt): q.run(campaign)
        # The synchronous test process is still alive; a real interrupted supervisor
        # would be gone. A deliberately stale creation identity simulates that fact.
        state = q.read(campaign / 'campaign.json')
        state['supervisor_process']['created'] += 1
        q.atomic(campaign / 'campaign.json', state)
        dispatched = q.resume(campaign)
        handle = win.checked(win.open_process(0x100000, False, dispatched['pid']))
        try: self.assertEqual(0, win.wait_many(1, (win.W.HANDLE * 1)(handle), False, 15000))
        finally: win.close(handle)
        self.assertEqual('FAILED', q.status(campaign)['status'])
        self.assertEqual(1, len((campaign / 'stage0/invocations.log').read_text().splitlines()))

    def test_duplicate_and_conflicting_campaign_rejected(self):
        campaign = self.create()
        with self.assertRaisesRegex(ValueError, 'Conflicting'): self.create()
        with q.Lease(self.root):
            with self.assertRaisesRegex(ValueError, 'Another'): q.run(campaign)
        duplicate = plan(self.root, ('ok', 'ok'))
        duplicate['stages'][1]['command'] = duplicate['stages'][0]['command']
        with self.assertRaisesRegex(ValueError, 'Duplicate'): q.validate_plan(duplicate)

    def test_dependencies_selection_and_no_implicit_profiles(self):
        value = plan(self.root, ('ok', 'nonzero'))
        value['selections']['focused'] = ['stage0']
        campaign = q.create(self.root, value, 'focused')
        self.assertEqual('PASSED', q.run(campaign)['status'])
        self.assertFalse((campaign / 'stage1').exists())
        value['selections']['focused'] = ['stage1']
        with self.assertRaisesRegex(ValueError, 'dependencies'): q.create(self.root, value, 'focused')

    def test_report_tampering_and_missing_exit_status_rejected(self):
        campaign = self.create()
        q.run(campaign)
        xml = campaign / 'stage0/results.xml'
        xml.write_text(xml.read_text() + '\n')
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
        xml.write_text(xml.read_text().rstrip())
        (campaign / 'stage0/exit.json').unlink()
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])

    def test_counts_and_failure_evidence_retained(self):
        campaign = self.create(('failed-tests',))
        q.run(campaign)
        record = q.read(campaign / 'stage0/result.json')
        self.assertEqual(1, record['tests']['discovered'])
        self.assertEqual(1, record['tests']['failed'])
        self.assertIn('results.xml', record['evidence'])

    def test_detached_launch_and_inspection_do_not_relaunch(self):
        campaign = self.create(delay=0.5)
        result = q.launch(campaign)
        with self.assertRaises(ValueError): q.launch(campaign)
        handle = win.checked(win.open_process(0x100000, False, result['pid']))
        try:
            handles = (win.W.HANDLE * 1)(handle)
            self.assertEqual(0, win.wait_many(1, handles, False, 15000))
        finally: win.close(handle)
        for _ in range(2): self.assertEqual('PASSED', q.status(campaign)['status'])
        self.assertEqual(1, len((campaign / 'stage0/invocations.log').read_text().splitlines()))

    def test_real_cancellation_limited_to_owned_job(self):
        campaign = self.create(delay=15)
        unrelated = subprocess.Popen([sys.executable, '-c', 'import time;time.sleep(20)'])
        worker = subprocess.Popen([sys.executable, str(Path(q.__file__).resolve()), 'run', str(campaign)],
                                  stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            wait_file(campaign / 'stage0/child.json', worker)
            self.assertEqual('REQUESTED', q.cancel(campaign)['cancellation'])
            worker.wait(timeout=10)
            self.assertEqual('FAILED', q.status(campaign)['status'])
            self.assertEqual('CANCEL', q.read(campaign / 'stage0/result.json')['failure'])
            self.assertIsNone(unrelated.poll())
        finally:
            if worker.poll() is None: worker.terminate(); worker.wait(timeout=10)
            unrelated.terminate(); unrelated.wait(timeout=10)

    def test_crashed_supervisor_with_live_child_is_incomplete_no_relaunch(self):
        campaign = self.create(delay=15)
        worker = subprocess.Popen([sys.executable, str(Path(q.__file__).resolve()), 'run', str(campaign)],
                                  stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        record = None
        job_handle = None
        try:
            wait_file(campaign / 'stage0/child.json', worker)
            record = q.read(campaign / 'stage0/started.json')
            job_handle = win.checked(win.open_job(8, False, record['job']))
            worker.terminate(); worker.wait(timeout=10)  # exact fixture Popen handle
            self.assertTrue(win.matches(record['process']))
            self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
            self.assertEqual('INCOMPLETE', q.run(campaign)['status'])
            with self.assertRaises(ValueError): q.cancel(campaign)
            with self.assertRaises(ValueError): self.create()
            self.assertEqual(1, len((campaign / 'stage0/invocations.log').read_text().splitlines()))
        finally:
            if worker.poll() is None: worker.terminate(); worker.wait(timeout=10)
            if record and win.matches(record['process']):
                # Test cleanup only: exact verified fixture job, never a global PID search.
                win.checked(win.terminate_job(job_handle, 125))
                process_handle = win.open_process(0x100000, False, record['process']['pid'])
                if process_handle:
                    win.wait_many(1, (win.W.HANDLE * 1)(process_handle), False, 10000)
                    win.close(process_handle)
            if job_handle: win.close(job_handle)

    def test_stale_pid_reuse_and_executable_protection(self):
        real = win.identity(os.getpid())
        self.assertTrue(win.matches(real))
        self.assertFalse(win.matches(dict(real, created=real['created'] + 1)))
        self.assertFalse(win.matches(dict(real, executable='wrong.exe')))
        self.assertFalse(win.matches(dict(real, pid=4294967295)))

    def test_cancellation_refuses_unowned_or_reused_supervisor(self):
        campaign = self.create()
        path = campaign / 'campaign.json'
        state = q.read(path)
        state.update(status='RUNNING', supervisor_process=dict(win.identity(os.getpid()), created=1))
        q.atomic(path, state)
        with self.assertRaisesRegex(ValueError, 'verified'): q.cancel(campaign)
        self.assertTrue(win.identity(os.getpid()))

    def test_uncertain_stage_never_relaunches(self):
        campaign = self.create()
        directory = campaign / 'stage0'
        directory.mkdir()
        (directory / 'partial.log').write_text('retained')
        self.assertEqual('INCOMPLETE', q.run(campaign)['status'])
        for _ in range(2):
            self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
            with self.assertRaises(ValueError): q.run(campaign)
        self.assertEqual('retained', (directory / 'partial.log').read_text())
        self.assertFalse((directory / 'invocations.log').exists())
        with self.assertRaises(ValueError): self.create()

    def test_path_and_reparse_protection(self):
        campaign = self.create()
        for path in ('../escape', str(self.root / 'outside')):
            with self.assertRaises(ValueError): q.inside(campaign, path)
        value = plan(self.root)
        for name in ('../stage0/results.xml', 'C:/historical.xml', 'results.xml:stale'):
            value['stages'][0]['xml'] = name
            with self.assertRaisesRegex(ValueError, 'Artifact'): q.validate_plan(value)

    def test_qualification_selection_requires_gates_and_final_preservation(self):
        value = plan(self.root)
        value['purpose'] = 'qualification'
        value['isolation'] = dict(product='Disposable Product', save='disposable.json')
        value['approved_substitutions'] = ['fixture only']
        with self.assertRaisesRegex(ValueError, 'preflight'): q.validate_plan(value)
        gate = copy.deepcopy(value['stages'][0])
        gate.update(id='preflight', role='preflight', command=gate['command'] + ['preflight'])
        final = copy.deepcopy(gate)
        final.update(id='final', role='final', command=gate['command'] + ['final'], depends=['preflight', 'stage0'])
        value['stages'][0]['depends'] = ['preflight']
        value['stages'] = [gate, value['stages'][0], final]
        value['selections']['focused'] = ['preflight', 'stage0', 'final']
        campaign = q.create(self.root, value, 'focused')
        with patch.object(win, 'assert_no_unity') as guard:
            self.assertEqual('PASSED', q.run(campaign)['status'])
        self.assertEqual(3, guard.call_count)
        value['selections']['focused'] = ['preflight', 'stage0']
        with self.assertRaisesRegex(ValueError, 'final preservation'): q.create(self.root, value, 'focused')

    def test_bad_tool_metadata_does_not_claim_campaign(self):
        value = plan(self.root)
        value['tools'][SELF]['sha256'] = '0' * 64
        with self.assertRaisesRegex(ValueError, 'Tool hash'): q.create(self.root, value, 'focused')
        self.assertEqual('PASSED', q.run(self.create())['status'])

    def test_predeclared_input_transition_and_unapproved_change(self):
        value = plan(self.root, ('changeinput', 'ok'))
        changed = q.fingerprint(self.root)
        changed['Assets/fixture.txt'] = q.hashlib.sha256(b'reviewed restored input\n').hexdigest()
        identity = q.signature(changed)
        value['stages'][0]['after_input_identity'] = identity
        value['stages'][1]['input_identity'] = identity
        campaign = q.create(self.root, value, 'focused')
        self.assertEqual('PASSED', q.run(campaign)['status'])
        self.assertEqual('PASSED', q.status(campaign)['status'])

    def test_vanished_dispatch_before_running_publication_is_incomplete(self):
        campaign = self.create()
        (campaign / 'launch.claim').write_text('reserved')
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
        q.atomic(campaign / 'launch.json', {'process': dict(win.identity(os.getpid()), created=1)})
        self.assertEqual('INCOMPLETE', q.status(campaign)['status'])
        self.assertFalse((campaign / 'stage0').exists())


def prepare_demo(path, mode='ok', delay=10):
    root = Path(path).absolute()
    root.mkdir(parents=True, exist_ok=False)
    repository(root)
    value = plan(root, (mode,), delay)
    q.atomic(root / 'plan.json', value)
    campaign = q.create(root, value, 'focused')
    print(json.dumps(dict(project=str(root), campaign=str(campaign), purpose='disposable fixture')))


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--fixture': sys.exit(fixture())
    if len(sys.argv) > 1 and sys.argv[1] == '--prepare-demo':
        prepare_demo(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else 'ok',
                     float(sys.argv[4]) if len(sys.argv) > 4 else 10)
        sys.exit(0)
    unittest.main(verbosity=2)
