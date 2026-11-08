"""Record exact workload input identities and all seed differences, without normalization."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT.parent.parent / 'h2-a-performance-main/Dungeon-Lord'
PR = ROOT.parent.parent / 'h2-a-performance-pr/Dungeon-Lord'
MAIN_CAMPAIGN = BASE / 'TestResults/qualification-supervisor/ca7fc8906b1c449492923e69a909b406'
PR_CAMPAIGN = PR / 'TestResults/qualification-supervisor/4862ecc09db74edf85c97020a217c147'

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def differences(a, b, path='$'):
    if type(a) is not type(b): return [{'path': path, 'main': a, 'pr': b}]
    if isinstance(a, dict):
        rows = []
        for key in sorted(a.keys() | b.keys()):
            if key not in a or key not in b: rows.append({'path':path+'.'+key, 'main':a.get(key), 'pr':b.get(key)})
            else: rows.extend(differences(a[key], b[key], path+'.'+key))
        return rows
    if isinstance(a, list):
        if len(a) != len(b): return [{'path':path, 'main_length':len(a), 'pr_length':len(b)}]
        return [r for i,(x,y) in enumerate(zip(a,b)) for r in differences(x,y,path+'['+str(i)+']')]
    return [] if a == b else [{'path':path, 'main':a, 'pr':b}]

result = {'method': 'Exact hashes and unmodified JSON comparisons. No timestamp, ordering, revision or saved-state normalization.', 'workloads': {}}
for variant in ('owner-inactive', 'two-floor'):
    left = next(MAIN_CAMPAIGN.rglob('editor-'+variant+'.profile.json'))
    right = next(PR_CAMPAIGN.rglob('editor-'+variant+'.profile.json'))
    a = next(left.parent.glob(variant+'-*.manifest.json'))
    b = next(right.parent.glob(variant+'-*.manifest.json'))
    ma, mb = read(a), read(b)
    sa, sb = Path(ma['SeedPath']), Path(mb['SeedPath'])
    assert sha(sa) == ma['SeedSha256'] and sha(sb) == mb['SeedSha256']
    result['workloads'][variant] = {'main_manifest': str(a), 'pr_manifest': str(b),
        'main_seed_sha256': sha(sa), 'pr_seed_sha256': sha(sb),
        'seed_bytes_identical': sa.read_bytes() == sb.read_bytes(),
        'all_seed_differences': differences(read(sa), read(sb)),
        'descent_samples': [ma['DescendingSamples'], mb['DescendingSamples']],
        'longest_floor_two': [ma['LongestFloorTwoDuration'], mb['LongestFloorTwoDuration']]}
a = read(BASE / 'TestResults/h2-a-checkpoint2-performance-main-20261107-01/original-inputs.json')
b = read(PR / 'TestResults/h2-a-checkpoint2-performance-pr-20261108-01/original-inputs.json')
controls = [p for p in a if p.startswith(('Assets/_Project/Resources/','Assets/_Project/Data/','Packages/','ProjectSettings/')) or p in (
    'Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs',
    'Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonTwoFloorProductionProfileSceneTests.cs',
    'Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs',
    'Assets/_Project/Scenes/Bootstrap.unity')]
result['control_input_hashes'] = {p: {'main': a[p], 'pr': b.get(p), 'identical': a[p] == b.get(p)} for p in controls}
result['all_control_inputs_identical'] = all(x['identical'] for x in result['control_input_hashes'].values())
out = Path(__file__).parent / 'workload-comparability.json'
assert not out.exists()
out.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'output':str(out),'all_controls_identical':result['all_control_inputs_identical'],
    'workloads': {k:v['all_seed_differences'] for k,v in result['workloads'].items()}}, indent=2))
