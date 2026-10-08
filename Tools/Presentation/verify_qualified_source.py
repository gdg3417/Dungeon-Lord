"""Compare every Unity project input against the disposable qualification copy.

The only permitted transformations are the explicitly isolated save filename and
text newline normalization. Writes an auditable manifest and fails on any drift.
"""
from pathlib import Path
import hashlib, json, subprocess, sys

root = Path(__file__).resolve().parents[2]
target = root / 'Temp' / sys.argv[1]
output = root / sys.argv[2]
assert target.resolve().is_relative_to(root / 'Temp')
files = subprocess.check_output(['git', 'ls-files', '-z', 'Assets', 'Packages', 'ProjectSettings', 'ContentAuthoring'], cwd=root).decode().split('\0')
files += subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z', 'Assets'], cwd=root).decode().split('\0')
rows, failures = [], []
for name in sorted(set(files)-{''}):
    source = root/name
    if not source.is_file(): continue
    dest = target/name
    if not dest.is_file(): failures.append(name+': missing'); continue
    left, right = source.read_bytes(), dest.read_bytes()
    isolated = name in ('Assets/_Project/Scripts/Services/SaveService.cs', 'Assets/_Project/Data/Bootstrap/build_config.json')
    if name.endswith('SaveService.cs'):
        right = right.replace(b'phase7-ui-composition-validation-01a11d08.json', b'save_primary.json')
    elif name.endswith('build_config.json'):
        original, tested = json.loads(left.decode('utf-8-sig')), json.loads(right.decode('utf-8-sig'))
        assert tested['save']['fileName'] == 'phase7-ui-composition-validation-01a11d08.json'
        tested['save']['fileName'] = original['save']['fileName']
        left = json.dumps(original, sort_keys=True).encode()
        right = json.dumps(tested, sort_keys=True).encode()
    if b'\x00' not in left and b'\x00' not in right:
        left, right = left.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n',b'\n'), right.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n',b'\n')
    match = left == right
    if not match: failures.append(name+': mismatch')
    rows.append(dict(path=name, sha256=hashlib.sha256(left).hexdigest(), qualified_sha256=hashlib.sha256(right).hexdigest(),
                     equal=match, isolated_save_namespace=isolated))
result = dict(branch=subprocess.check_output(['git','branch','--show-current'],cwd=root).decode().strip(),
              source_head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip(),
              validation_directory=str(target), files=rows, failures=failures,
              permitted_transformations=['validation save filename only','text CRLF/LF and UTF-8 BOM normalization'])
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(len(rows),'project inputs compared;',len(failures),'mismatches')
for failure in failures: print(failure)
sys.exit(bool(failures))
