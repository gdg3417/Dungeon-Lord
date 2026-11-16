"""Adversarial marker checks. The Windows campaign supplies real Job Object evidence."""
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

import qualification_planned_stop as stop
import qualification_supervisor as q


class PlannedStopTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='dlq-h2-')
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        self.directory = root / 'interrupt'
        self.directory.mkdir()
        player = root / 'build/player' / stop.PLAYER
        player.parent.mkdir(parents=True)
        player.write_bytes(b'fixture image')
        self.nonce = 'a' * 32
        self.stage = {'planned_stop': {'cut': stop.CUT, 'nonce': self.nonce}}
        self.state = {'source_sha': 'b' * 40, 'plan': {'isolation': {'product': 'DL B3B2 Smoke Two Floor Q12345678'}}}
        home = patch.object(stop.Path, 'home', return_value=root)
        home.start()
        self.addCleanup(home.stop)
        self.save = stop.slot_path(self.state, self.nonce)
        self.save.parent.mkdir(parents=True)
        self.save.write_bytes(b'candidate')
        scratch = root / 'prior'
        scratch.write_bytes(b'predecessor')
        original, candidate = q.digest(scratch), q.digest(self.save)
        self.rollback = self.save.with_name(self.save.name + f'.canonical-write-{original[:16]}-{candidate[:16]}.rollback')
        self.rollback.write_bytes(scratch.read_bytes())
        self.identity = {'pid': 234, 'created': 456, 'executable': os.path.normcase(str(player))}
        q.atomic(root / 'build/build-seal.json', {'source': self.state['source_sha'], 'files': {stop.PLAYER: q.digest(player)}})
        q.atomic(self.directory / 'player.json', {'process': self.identity, 'nonce': self.nonce, 'save': str(self.save),
                                                 'image_sha256': q.digest(player), 'source': self.state['source_sha']})
        self.marker = dict(Cut=stop.CUT, Nonce=self.nonce, Ordinal=1, Pid=234, Created=456, SavePath=str(self.save),
                           Predecessor=original, Candidate=candidate)

    def check(self):
        return stop.verify_marker(self.directory, self.state, self.stage, q.digest, q.read)

    def test_exact_marker_resolves_one_image_and_slot(self):
        q.atomic(self.directory / stop.MARKER, self.marker)
        self.assertEqual(self.check(), (self.marker, self.identity, self.save, self.rollback))

    def test_wrong_marker_never_requests_owned_job_stop(self):
        for fields in ({'Nonce': 'f' * 32}, {'SavePath': str(self.save) + '.other'}, {'Pid': 455},
                       {'Created': 999}, {'Cut': 'pre-replace'}, {'Ordinal': 2}, {'Candidate': '0' * 64}):
            with self.subTest(fields=fields):
                q.atomic(self.directory / stop.MARKER, self.marker | fields)
                job = Mock()
                with patch.object(stop.win, 'wait', side_effect=['timeout', 'cancel']):
                    outcome, evidence, rejection = stop.wait_for_cut(
                        None, None, 5, job, self.directory, self.state, self.stage, q.digest, q.read)
                self.assertEqual(outcome, 'cancel')
                self.assertIsNone(evidence)
                self.assertTrue(rejection)
                job.hold_member.assert_not_called()
                job.stop.assert_not_called()

    def test_valid_marker_without_exact_job_membership_is_rejected(self):
        q.atomic(self.directory / stop.MARKER, self.marker)
        job = Mock()
        job.hold_member.side_effect = ValueError('wrong job')
        with patch.object(stop.win, 'wait', side_effect=['timeout', 'exit']):
            outcome, evidence, rejection = stop.wait_for_cut(
                None, None, 5, job, self.directory, self.state, self.stage, q.digest, q.read)
        self.assertEqual((outcome, evidence, rejection), ('exit', None, 'wrong job'))

    def test_capture_preserves_active_and_rollback_and_checks_os_exit(self):
        observed = (*self.marker_observation(), 77)
        with patch.object(stop.win, 'process_exit_code', return_value=0):
            with self.assertRaisesRegex(ValueError, 'Unexpected OS exit'):
                stop.capture(self.directory, observed, 125, q.digest)
        with patch.object(stop.win, 'process_exit_code', return_value=125):
            receipt = stop.capture(self.directory, observed, 125, q.digest)
        self.assertEqual(receipt['files'][self.save.name]['sha256'], self.marker['Candidate'])
        self.assertEqual(receipt['files'][self.rollback.name]['sha256'], self.marker['Predecessor'])
        self.assertEqual((self.directory / 'raw-save' / self.save.name).read_bytes(), self.save.read_bytes())

    def marker_observation(self):
        q.atomic(self.directory / stop.MARKER, self.marker)
        return self.check()

    def test_malformed_or_missing_marker_preserves_ordinary_wait_outcomes(self):
        with self.assertRaises(FileNotFoundError):
            self.check()
        (self.directory / stop.MARKER).write_text('{')
        with self.assertRaises(ValueError):
            self.check()
        with patch.object(stop.win, 'wait', return_value='timeout'):
            self.assertEqual(stop.wait_for_cut(
                None, None, 0, Mock(), self.directory, self.state, self.stage, q.digest, q.read),
                ('timeout', None, None))
        self.assertFalse(stop.completed(self.stage, {'exit_code': 0}))
        self.assertFalse(stop.completed({}, {'exit_code': 125, 'completion': 'PLANNED_STOP'}))
