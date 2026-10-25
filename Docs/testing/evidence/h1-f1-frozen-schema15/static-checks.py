from pathlib import Path
import hashlib, json, subprocess

root = Path.cwd()
fixtures = root / 'Tools/Presentation/Fixtures/schema15-09a46810'
rows = [line.split('\t') for line in (fixtures / 'SHA256SUMS.tsv').read_text().splitlines()]
files = [(name, digest) for name, digest in rows if name.endswith('.json')]
for name, digest in files:
    assert hashlib.sha256((fixtures / name).read_bytes()).hexdigest() == digest, name
resource = root / 'Assets/_Project/Resources/save_spatial_migration_limits_schema15.json'
assert resource.read_bytes() == (fixtures / 'limits.json').read_bytes()
source = root / 'Assets/_Project/Scripts/Gameplay/DungeonSpatial'
unchanged = []
for path in sorted(source.glob('Schema*Upgrade.cs')):
    if path.name == 'SchemaFourteenToFifteenUpgrade.cs': continue
    relative = path.relative_to(root).as_posix()
    baseline = subprocess.check_output(['git', 'show', '09a46810:' + relative])
    assert path.read_bytes().replace(b'\r\n', b'\n') == baseline.replace(b'\r\n', b'\n'), relative
    unchanged.append(relative)
target = (source / 'CanonicalSaveSchemaVersions.cs').read_text()
assert 'CurrentWritableTarget = MonsterContinuityIntroduction;' in target
assert 'MonsterContinuityIntroduction = 15;' in target
subprocess.run(['git', 'diff', '--check'], check=True)
report = dict(baseline='09a46810aeab7441a2ac09ac6220b0f4cc13d6dd',
    fixture_files_verified=len(files), verdict_pairs=len((fixtures/'verdicts.tsv').read_text().splitlines()),
    schema15_limit_sha256=hashlib.sha256(resource.read_bytes()).hexdigest(),
    earlier_transformations_unchanged=unchanged, current_writable_target=15, diff_check='passed')
out = root / 'Docs/testing/evidence/h1-f1-frozen-schema15/static-checks.json'
out.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(json.dumps(report))
