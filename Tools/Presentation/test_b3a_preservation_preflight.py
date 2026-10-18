"""Disposable text fixtures only: no Unity project copy, Editor or owner paths."""
from pathlib import Path
import io
import json
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import b3a_preservation_preflight as gate
import collect_b3a_evidence as collector


class PreservationPreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='b3a-gate-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.out = self.root/'TestResults/living-dungeon-b3a-20261017'
        self.save = self.root/'owner-save-fixture'
        self.out.mkdir(parents=True)
        self.save.mkdir()
        self.originals = {}
        q = gate.q
        for name in [q.inputs.CONFIG, q.inputs.SERVICE]+q.OUTPUT_TESTS+q.FIXTURE_TESTS+q.MUTABLE[2:]:
            if name == q.inputs.CONFIG:
                data = b'{"save":{"fileName":"save_primary.json"}}'
            elif name == q.inputs.SERVICE:
                data = b'// frozen fake fallback: save_primary.json\r\n'
            elif name in q.OUTPUT_TESTS:
                data = b'// fake screenshot output: TestResults/historical\r\n'
            elif name in q.FIXTURE_TESTS:
                data = ('// '+ ' '.join(old for old, new in gate.FIXTURE_REPLACEMENTS)+'\r\n').encode()
            else:
                data = b'reviewed local override fixture'
            self.put(name, data)
            self.originals[name] = data
            if name.endswith('.cs'):
                self.put(name+'.meta', b'reviewed fake metadata')
                self.originals[name+'.meta'] = b'reviewed fake metadata'
        owner = self.save/'original-owner.json'
        owner.write_bytes(b'owner fixture bytes')
        old = self.root/'TestResults/old.png'
        old.write_bytes(b'historical evidence fixture bytes')
        baseline = dict(inputs=[dict(path=n, sha256=gate.digest(d)) for n, d in self.originals.items()],
                        protected=[dict(path=str(owner), sha256=gate.q.sha(owner), fixture=False)])
        self.baselines = {'baseline.json': json.dumps(baseline).encode(),
                          'historical-evidence-before.json': json.dumps([dict(path=str(old), sha256=gate.q.sha(old))]).encode()}
        for name, data in self.baselines.items():
            (self.out/name).write_bytes(data)
        with zipfile.ZipFile(self.out/'root-inputs-before.zip', 'x') as archive:
            for name, data in self.originals.items():
                archive.writestr(name, data)
        with zipfile.ZipFile(self.out/'test-fixtures-before.zip', 'x'):
            pass
        (self.out/'fixture-substitutions.json').write_text(json.dumps([dict(path=n) for n in q.FIXTURE_TESTS]))
        for name, data in self.originals.items():
            if name == q.inputs.CONFIG:
                value = json.loads(data)
                value['save']['fileName'] = q.NAMESPACE
                data = json.dumps(value, indent=2).encode()
            elif name == q.inputs.SERVICE:
                data = data.replace(b'save_primary.json', q.NAMESPACE.encode())
            elif name in q.OUTPUT_TESTS:
                data = data.replace(b'TestResults/', b'TestResults/living-dungeon-b3a-20261017/outputs/')
            elif name in q.FIXTURE_TESTS:
                for old, new in gate.FIXTURE_REPLACEMENTS:
                    data = data.replace(old.encode(), new.encode())
            self.put(name, data)
        self.anchor = patch.object(gate, 'anchors', return_value=(
            {n: gate.digest(d) for n, d in self.originals.items()}, self.baselines))
        self.anchor.start()
        self.addCleanup(self.anchor.stop)

    def put(self, name, data):
        path = self.root/name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def snapshot(self):
        return {p.relative_to(self.root).as_posix(): gate.q.sha(p) if p.is_file() else 'directory'
                for p in self.root.rglob('*')}

    def check(self, reason=None, stage='focused'):
        before = self.snapshot()
        if reason:
            with self.assertRaisesRegex(ValueError, reason):
                gate.validate(self.root, self.out, self.save, stage, 'unused')
        else:
            self.assertTrue(gate.validate(self.root, self.out, self.save, stage, 'unused')['approved'])
        self.assertEqual(before, self.snapshot(), 'preflight must be read-only on success AND rejection')

    def test_complete_isolation_passes_without_writes(self):
        for stage in sorted(gate.STAGES):
            with self.subTest(stage=stage): self.check(stage=stage)
    def test_unused_build_destination_passes_without_writes(self): self.check(stage='build')

    def test_restored_isolation_rejected(self):
        (self.out/'substitutions-restored.json').write_text('{}')
        self.check('restored/inactive')

    def test_missing_preparation_rejected(self):
        (self.out/'baseline.json').unlink()
        self.check('preparation incomplete')

    def test_tampered_baseline_rejected(self):
        (self.out/'baseline.json').write_text('{}')
        self.check('baseline differs')

    def test_partial_save_namespace_rejected(self):
        self.put(gate.q.inputs.SERVICE, self.originals[gate.q.inputs.SERVICE])
        self.check('Prepared-source identity mismatch')

    def test_unredirected_shared_screenshot_helper_rejected(self):
        name = gate.q.OUTPUT_TESTS[-1]
        self.put(name, self.originals[name])
        self.check('ProductionDungeonScreenshots')

    def test_unredirected_scene_screenshot_helper_rejected(self):
        name = gate.q.OUTPUT_TESTS[1]
        self.put(name, self.originals[name])
        self.check('ProductionDungeonFloorSceneTests')

    def test_unredirected_static_fixture_rejected(self):
        name = gate.q.FIXTURE_TESTS[0]
        self.put(name, self.originals[name])
        self.check('BootstrapOverlayPagingTests')

    def test_new_source_rejected(self):
        self.put('Assets/unreviewed.cs', b'extra input')
        self.check('Unexpected/missing Unity inputs')

    def test_changed_local_override_rejected(self):
        self.put(gate.q.MUTABLE[2], b'unreviewed local changes')
        self.check('Prepared-source identity mismatch')

    def test_missing_metadata_rejected_without_creating_metadata(self):
        name = gate.q.inputs.SERVICE+'.meta'
        (self.root/name).unlink()
        self.check('Unexpected/missing Unity inputs')
        before = self.snapshot()
        with self.assertRaisesRegex(ValueError, 'will not create'):
            with patch.object(gate.q, 'ROOT', self.root), patch.object(gate.q, 'OUT', self.root/'new-preparation'):
                gate.q.prepare()
        self.assertEqual(before, self.snapshot())

    def test_bad_original_source_archive_rejected(self):
        with zipfile.ZipFile(self.out/'root-inputs-before.zip', 'w') as archive:
            for name, data in self.originals.items():
                archive.writestr(name, b'bad' if name == gate.q.inputs.CONFIG else data)
        self.check('Unreviewed source/override backup')

    def test_protected_owner_mismatch_rejected(self):
        (self.save/'original-owner.json').write_bytes(b'changed')
        self.check('Preservation mismatch')

    def test_historical_evidence_mismatch_rejected(self):
        (self.root/'TestResults/old.png').write_bytes(b'replacement is not original')
        self.check('Preservation mismatch')

    def test_used_report_destinations_rejected(self):
        for suffix in ['.xml', '.log', '-command.json']:
            with self.subTest(suffix=suffix):
                path = self.out/('unused-focused'+suffix)
                path.write_bytes(b'retained')
                self.check('Destination already used')
                path.unlink()

    def test_used_screenshot_and_windows_destinations_rejected(self):
        for name in ['outputs', 'windows']:
            with self.subTest(name=name):
                path = self.out/name
                path.mkdir()
                self.check('Destination already used')
                path.rmdir()

    def test_partial_build_directory_rejected(self):
        (self.root/'Builds/Development/Windows-living-dungeon-b3a-20261017').mkdir(parents=True)
        self.check('Destination already used', stage='build')

    def test_used_save_and_static_fixture_namespaces_rejected(self):
        for name in [gate.q.NAMESPACE+'.bak', 'bootstrap_overlay_b3a_20261017_test.json',
                     'run_sim_test_b3a_20261017_test.json', 'mvp_canonical_journey_b3a_20261017.json']:
            with self.subTest(name=name):
                path = self.save/name
                path.write_bytes(b'retained isolated fixture')
                self.check('namespace already used')
                path.unlink()

    def test_closed_baseline_stage_rejected(self): self.check('Unsupported/closed stage', stage='baseline-ui')

    def test_partial_fixture_record_rejected(self):
        (self.out/'fixture-substitutions.json').write_text('[]')
        self.check('fixture preparation record is incomplete')

    def test_changed_qualified_commit_inputs_rejected(self):
        self.anchor.stop()
        snapshot = json.dumps(dict(finalCommitAtCollection=gate.QUALIFIED, unityInputs=[])).encode()
        with patch.object(gate, 'git', side_effect=[snapshot, b'Assets/changed.cs']):
            self.check('Unity inputs changed')

    def test_reviewed_anchor_baselines_read_from_immutable_archive(self):
        self.anchor.stop()
        buffer = io.BytesIO()
        with zipfile.ZipFile(buffer, 'w') as archive:
            for name, data in self.baselines.items(): archive.writestr(name, data)
        snapshot = json.dumps(dict(finalCommitAtCollection=gate.QUALIFIED,
                             unityInputs=[dict(path=n, sha256=gate.digest(d)) for n, d in self.originals.items()])).encode()
        with patch.object(gate, 'git', side_effect=[snapshot, b'', buffer.getvalue()]):
            self.check()

    def test_collector_rejects_partial_existing_evidence(self):
        destination = self.root/'collected-evidence'
        destination.mkdir()
        (destination/'commands.json').write_bytes(b'previous partial evidence')
        before = self.snapshot()
        with patch.object(collector, 'DEST', destination):
            with self.assertRaisesRegex(ValueError, 'existing/partial'):
                collector.collect()
        self.assertEqual(before, self.snapshot())


if __name__ == '__main__':
    unittest.main(verbosity=2)
