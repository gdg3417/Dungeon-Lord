"""Runner-only tests. No Unity, owner saves or real campaign is launched."""
import os
import hashlib
import json
from pathlib import Path
from types import SimpleNamespace
import tempfile
import unittest
import zipfile
from unittest.mock import patch

import qualify_h1_f3 as q


class H1F3RunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='h1f3-runner-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.stage = self.root / 'TestResults/qualification-supervisor/disposable/focused'
        self.stage.mkdir(parents=True)

    def test_direct_keyring_is_child_local_and_synchronous(self):
        for old_value in [None, '0', 'inherited']:
            environment = {} if old_value is None else {'UNITY_NO_AUTH_BROKER': old_value}
            with patch.object(q, 'ROOT', self.root), patch.dict(os.environ, environment, clear=True), \
                 patch.object(q, 'write') as write, patch.object(q.subprocess, 'run') as run:
                run.return_value = SimpleNamespace(returncode=0)
                self.assertEqual(0, q.run_test('focused', self.stage))
                self.assertEqual(old_value, os.environ.get('UNITY_NO_AUTH_BROKER'))
                self.assertEqual('1', run.call_args.kwargs['env']['UNITY_NO_AUTH_BROKER'])
                self.assertEqual(self.root, run.call_args.kwargs['cwd'])
                self.assertNotIn('shell', run.call_args.kwargs)
                self.assertEqual({'returncode': 0}, write.call_args.args[1])

    def test_cli_failures_are_not_converted_to_success(self):
        for code in [8, 14, 125]:
            with patch.object(q, 'ROOT', self.root), patch.object(q, 'write'), \
                 patch.object(q.subprocess, 'run', return_value=SimpleNamespace(returncode=code)):
                self.assertEqual(code, q.run_test('focused', self.stage))

    def test_command_keeps_existing_filter_timeout_mode_and_reports(self):
        with patch.object(q, 'ROOT', self.root), patch.object(q, 'write'), \
             patch.object(q.subprocess, 'run', return_value=SimpleNamespace(returncode=0)) as run:
            q.run_test('focused', self.stage)
            command = run.call_args.args[0]
            self.assertEqual([str(q.CLI), 'test', str(self.root), '--mode', 'EditMode',
                '--filter', 'H1F3;H1F2;FrozenSchemaFifteen', '--output', str(self.stage/'results.xml'),
                '--timeout', '900', '--no-color', '--', '-logFile', str(self.stage/'unity.log')], command)

    def test_invalid_gate_or_path_never_launches(self):
        with patch.object(q, 'ROOT', self.root), patch.object(q.subprocess, 'run') as run:
            with self.assertRaises(ValueError): q.run_test('other', self.stage)
            with self.assertRaises(ValueError): q.run_test('focused', self.root)
            with self.assertRaises(ValueError): q.run_test('migration', self.stage)
            run.assert_not_called()

    def test_measured_integration_font_rewrite_is_retained_and_exactly_restored(self):
        font = self.root / q.FALLBACK_FONT
        font.parent.mkdir(parents=True)
        font.write_bytes(b'original test font')
        stage = self.stage.with_name('integration')
        stage.mkdir()
        def cli(*args, **kwargs):
            font.write_bytes(b'Editor rewritten test font')
            return SimpleNamespace(returncode=0)
        with patch.object(q, 'ROOT', self.root), patch.object(q.subprocess, 'run', side_effect=cli):
            self.assertEqual(0, q.run_test('integration', stage))
        self.assertEqual(b'original test font', font.read_bytes())
        self.assertEqual(b'Editor rewritten test font', (stage/'generated-inputs/fallback-font.asset').read_bytes())
        self.assertTrue(json.loads((stage/'generated-inputs/manifest.json').read_text())['restored'])

    def test_unknown_input_change_is_never_automatically_restored(self):
        path = self.root / 'Assets/unexpected.txt'
        path.parent.mkdir()
        path.write_bytes(b'original')
        def cli(*args, **kwargs):
            path.write_bytes(b'changed')
            return SimpleNamespace(returncode=0)
        with patch.object(q, 'ROOT', self.root), patch.object(q.subprocess, 'run', side_effect=cli):
            with self.assertRaisesRegex(ValueError, 'Unapproved Unity input'): q.run_test('focused', self.stage)
        self.assertEqual(b'changed', path.read_bytes())

    def test_failed_cli_preserves_generated_font_for_review(self):
        font = self.root / q.FALLBACK_FONT
        font.parent.mkdir(parents=True)
        font.write_bytes(b'original')
        stage = self.stage.with_name('integration')
        stage.mkdir()
        def cli(*args, **kwargs):
            font.write_bytes(b'failed run evidence')
            return SimpleNamespace(returncode=8)
        with patch.object(q, 'ROOT', self.root), patch.object(q.subprocess, 'run', side_effect=cli):
            self.assertEqual(8, q.run_test('integration', stage))
        self.assertEqual(b'failed run evidence', font.read_bytes())
        self.assertFalse(json.loads((stage/'generated-inputs/manifest.json').read_text())['restored'])

    def restoration_fixture(self, corrupt=False):
        prep = self.root / 'preparation'
        prep.mkdir()
        originals = {name: ('  productName: Dungeon Lord\r\n' if index == 0 else 'original\r\n').encode()
                     for index, name in enumerate(q.SETTINGS)}
        for name in originals:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b'qualified isolation\n')
        qualified = q.inventory(self.root)
        before = {name: hashlib.sha256(data).hexdigest() for name, data in originals.items()}
        (prep / 'inputs-before.json').write_text(json.dumps(before))
        (prep / 'qualified-inputs.json').write_text(json.dumps(qualified))
        with zipfile.ZipFile(prep / 'original-settings.zip', 'w') as archive:
            for name, data in originals.items(): archive.writestr(name, b'wrong backup' if corrupt else data)
        return prep, originals

    def test_restore_and_final_check_require_exact_original_bytes(self):
        prep, originals = self.restoration_fixture()
        with patch.object(q, 'ROOT', self.root), patch.object(q, 'PREP', prep), patch.object(q, 'verify_protected'):
            q.restore(self.stage)
            self.assertEqual(originals, {name: (self.root/name).read_bytes() for name in originals})
            q.check(self.stage, restored=True)
            self.assertTrue(json.loads((self.stage/'preservation.json').read_text())['settings_restored'])

    def test_corrupt_restoration_backup_changes_no_input(self):
        prep, _ = self.restoration_fixture(corrupt=True)
        with patch.object(q, 'ROOT', self.root), patch.object(q, 'PREP', prep), patch.object(q, 'verify_protected'):
            before = q.inventory(self.root)
            with self.assertRaisesRegex(ValueError, 'backup mismatch'): q.restore(self.stage)
            self.assertEqual(before, q.inventory(self.root))

    def test_unapproved_input_change_blocks_restoration(self):
        prep, _ = self.restoration_fixture()
        (self.root/q.SETTINGS[0]).write_bytes(b'not the qualified inputs')
        with patch.object(q, 'ROOT', self.root), patch.object(q, 'PREP', prep), patch.object(q, 'verify_protected'):
            before = q.inventory(self.root)
            with self.assertRaisesRegex(ValueError, 'Unexpected inputs'): q.restore(self.stage)
            self.assertEqual(before, q.inventory(self.root))


if __name__ == '__main__': unittest.main(verbosity=2)
