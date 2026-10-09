"""Isolated, reproducible helper regressions. Never uses the owner checkout/save."""
from pathlib import Path
import json, os, subprocess, tempfile, unittest
from qualification_inputs import CONFIG, SERVICE, NAMESPACE, compare, prepare, target_path

class QualificationInputsTests(unittest.TestCase):
    def setUp(self):
        self.fixture = tempfile.TemporaryDirectory(prefix='dungeon-input-test-')
        self.addCleanup(self.fixture.cleanup)
        self.root = Path(self.fixture.name)
        subprocess.run(['git','init','-q',str(self.root)],check=True)
        subprocess.run(['git','config','core.autocrlf','false'],cwd=self.root,check=True)
        self.name = 'ui-composition-fixture'
        self.target = self.root/'Temp'/self.name
        for name, data in {
            CONFIG: json.dumps({'save':{'fileName':'save_primary.json'},'other':17}),
            SERVICE: 'const string Fallback = "save_primary.json";\n',
            'Assets/stable.cs': 'class Stable {}',
            'Assets/art.png': b'\x89PNG\x00original',
            'Assets/art.png.meta': 'guid: fixture',
            'Packages/manifest.json': '{}',
            'ProjectSettings/ProjectSettings.asset': 'companyName: gdg3417\nproductName: Dungeon Lord\n',
            'ContentAuthoring/source.json': '{}',
        }.items():
            path = self.root/name
            path.parent.mkdir(parents=True,exist_ok=True)
            path.write_bytes(data.encode() if isinstance(data,str) else data)
        subprocess.run(['git','add','Assets','Packages','ProjectSettings','ContentAuthoring'],cwd=self.root,check=True)
        # Intended nonignored untracked asset AND metadata are also qualified.
        (self.root/'Assets/untracked.png').write_bytes(b'\x00untracked')
        (self.root/'Assets/untracked.png.meta').write_text('guid: untracked')

    def test_clean_and_untracked_inputs_accepted(self):
        result = prepare(self.root,self.name)
        self.assertEqual([],result['failures'])
        self.assertEqual(10,result['actual_count'])

    def test_repeat_is_idempotent_and_leaves_generated_library_alone(self):
        first = prepare(self.root,self.name)
        library = self.target/'Library/sentinel'
        library.parent.mkdir(); library.write_bytes(b'unchanged')
        self.assertEqual(first,prepare(self.root,self.name))
        self.assertEqual(b'unchanged',library.read_bytes())

    def test_stale_removed_script_rejected_before_copy(self):
        prepare(self.root,self.name)
        subprocess.run(['git','rm','-q','-f','Assets/stable.cs'],cwd=self.root,check=True)
        self.assertIn('Assets/stable.cs',compare(self.root,self.name)['unexpected'])
        with self.assertRaisesRegex(ValueError,'Stale target'): prepare(self.root,self.name)

    def test_stale_removed_art_and_metadata_rejected(self):
        prepare(self.root,self.name)
        subprocess.run(['git','rm','-q','-f','Assets/art.png','Assets/art.png.meta'],cwd=self.root,check=True)
        self.assertEqual(['Assets/art.png','Assets/art.png.meta'],compare(self.root,self.name)['unexpected'])
        with self.assertRaises(ValueError): prepare(self.root,self.name)

    def test_missing_required_target_fails(self):
        prepare(self.root,self.name)
        (self.target/'Assets/stable.cs').unlink()
        self.assertIn('Assets/stable.cs: missing',compare(self.root,self.name)['failures'])

    def test_changed_required_target_fails(self):
        prepare(self.root,self.name)
        (self.target/'Assets/stable.cs').write_text('changed')
        self.assertIn('Assets/stable.cs: mismatch',compare(self.root,self.name)['failures'])

    def test_target_only_in_each_input_root_fails_even_generated_meta(self):
        prepare(self.root,self.name)
        for root in ('Assets','Packages','ProjectSettings','ContentAuthoring'):
            (self.target/root/'unexpected.meta').write_text('generated or stale')
        self.assertEqual(4,len(compare(self.root,self.name)['unexpected']))
        with self.assertRaises(ValueError): prepare(self.root,self.name)

    def test_allowed_transforms_and_wrong_namespace(self):
        prepare(self.root,self.name)
        service = self.target/SERVICE
        self.assertIn(NAMESPACE,service.read_text())
        service.write_bytes(b'\xef\xbb\xbf'+service.read_bytes().replace(b'\n',b'\r\n'))
        self.assertFalse(compare(self.root,self.name)['failures'])
        service.write_text(service.read_text(encoding='utf-8-sig').replace(NAMESPACE,'save_primary.json'))
        self.assertIn(SERVICE+': mismatch',compare(self.root,self.name)['failures'])

    def test_unsafe_paths_rejected_and_unrelated_unchanged(self):
        outside = self.root/'outside'; outside.write_bytes(b'owner sentinel')
        for name in ('../outside','ui-composition-x/../../outside','C:/owner','ui-composition-x\\outside','..','other-validation'):
            with self.assertRaises(ValueError): prepare(self.root,name)
        prepare(self.root,self.name)
        self.assertEqual(b'owner sentinel',outside.read_bytes())

    def test_missing_tracked_source_fails_closed(self):
        (self.root/'Assets/stable.cs').unlink()
        with self.assertRaisesRegex(ValueError,'Missing required source'): prepare(self.root,self.name)

    def test_redirected_target_rejected(self):
        self.target.parent.mkdir()
        outside = self.root/'unrelated'; outside.mkdir()
        if os.name == 'nt':
            # Directory junctions need no symlink privilege on Windows.
            subprocess.run(['cmd','/c','mklink','/J',str(self.target),str(outside)],check=True,capture_output=True)
        else: self.target.symlink_to(outside,target_is_directory=True)
        with self.assertRaises(ValueError): prepare(self.root,self.name)
        self.assertEqual([],list(outside.iterdir()))

    def test_redirected_input_descendant_rejected(self):
        prepare(self.root,self.name)
        outside = self.root/'unrelated'; outside.mkdir()
        redirect = self.target/'Assets/redirect'
        if os.name == 'nt':
            subprocess.run(['cmd','/c','mklink','/J',str(redirect),str(outside)],check=True,capture_output=True)
        else: redirect.symlink_to(outside,target_is_directory=True)
        with self.assertRaises(ValueError): compare(self.root,self.name)
        with self.assertRaises(ValueError): prepare(self.root,self.name)

if __name__ == '__main__': unittest.main(verbosity=2)
