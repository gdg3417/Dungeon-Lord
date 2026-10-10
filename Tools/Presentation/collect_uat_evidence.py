"""Collect the PR230 owner correction run without altering historical evidence."""
from pathlib import Path
import hashlib, json, re, shutil, xml.etree.ElementTree as ET, zipfile

root = Path(__file__).resolve().parents[2]
out = root / 'Docs/testing/evidence/phase7-production-dungeon-ui-composition/owner-uat-corrections'
out.mkdir(parents=True, exist_ok=True)
digest = lambda data: hashlib.sha256(data).hexdigest()
reports = []
with zipfile.ZipFile(out / 'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root / 'TestResults').glob('ui-uat-*.xml')):
        data = path.read_bytes()
        run = ET.fromstring(data)
        cases = list(run.iter('test-case'))
        reports.append(dict(path=path.relative_to(root).as_posix(), sha256=digest(data), run_attributes=run.attrib,
            observed_cases=len(cases),
            failures=[dict(name=c.get('fullname'), message=c.findtext('failure/message'), stack=c.findtext('failure/stack-trace'))
                      for c in cases if c.get('result') == 'Failed'],
            skips=[dict(name=c.get('fullname'), reason=c.findtext('reason/message'))
                   for c in cases if c.get('result') in ('Skipped', 'Inconclusive')],
            observations=[c.findtext('output') for c in cases if c.findtext('output') and
                          re.search(r'(Layout |Card |Capture |Wheel unit|Input wheel|Boundary |Rail |cycles:|reconstructions)', c.findtext('output'))]))
        archive.writestr(path.name, data)
(out / 'report-summary.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
comparisons = []
with zipfile.ZipFile(out.parent / 'review-corrections/qualification-reports.zip') as previous:
    for mode in ('editmode', 'playmode'):
        old = ET.fromstring(previous.read('ui-review-full-'+mode+'.xml'))
        new = ET.parse(root / ('TestResults/ui-uat-full-'+mode+'.xml')).getroot()
        def outcomes(tree):
            groups = {}
            for case in tree.iter('test-case'):
                groups.setdefault(case.get('fullname'), []).append(case.get('result'))
            return {name: sorted(values) for name, values in groups.items()}
        a, b = outcomes(old), outcomes(new)
        comparisons.append(dict(mode=mode, previous_count=sum(map(len,a.values())), current_count=sum(map(len,b.values())),
            duplicate_names=[dict(name=k,count=len(b[k])) for k in b if len(b[k])>1],
            removed=sorted(a.keys()-b.keys()), added=[dict(name=k,result=b[k]) for k in sorted(b.keys()-a.keys())],
            changed=[dict(name=k,before=a[k],after=b[k]) for k in sorted(a.keys()&b.keys()) if a[k]!=b[k]]))
(out / 'test-outcome-comparison.json').write_text(json.dumps(comparisons, indent=2), encoding='utf-8')
logs = []
with zipfile.ZipFile(out / 'unity-logs-redacted.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root / 'Temp').glob('ui-uat-*.log')):
        data = path.read_text(encoding='utf-8-sig', errors='replace')
        clean, count = re.subn(r'(?i)([-/](?:access[-_]?token|auth[-_]?token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))', r'\1[REDACTED]', data)
        clean, bearer = re.subn(r'(?i)(Bearer\s+)\S+', r'\1[REDACTED]', clean)
        archive.writestr(path.name, clean)
        logs.append(dict(path=path.relative_to(root).as_posix(), redactions=count+bearer, shared_sha256=digest(clean.encode()),
                         compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*', clean))),
                         compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*', clean)))))
(out / 'log-index.json').write_text(json.dumps(logs, indent=2), encoding='utf-8')
manifests = []
with zipfile.ZipFile(out / 'source-inventories-and-output-diffs.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root / 'TestResults').glob('ui-uat-*.json')):
        data = path.read_bytes(); manifest = json.loads(data)
        if 'input_roots' not in manifest:
            continue
        archive.writestr(path.name, data)
        manifests.append(dict(path=path.relative_to(root).as_posix(), sha256=digest(data),
            source_head=manifest['source_head'], expected_count=manifest['expected_count'], actual_count=manifest['actual_count'],
            unexpected=manifest['unexpected'], failures=manifest['failures'], permitted_transformations=manifest['permitted_transformations']))
    for path in sorted((root / 'TestResults').glob('ui-uat-*.diff')):
        archive.write(path, path.name)
(out / 'source-checkpoint-index.json').write_text(json.dumps(manifests, indent=2), encoding='utf-8')
before = json.loads((out / 'owner-before.json').read_text(encoding='utf-8-sig'))
preserved = [dict(path=row['Path'], before_sha256=row['Hash'].lower(), after_sha256=digest(Path(row['Path']).read_bytes())) for row in before]
for row in preserved: row['unchanged'] = row['before_sha256'] == row['after_sha256']
save_dir = Path(before[0]['Path']).parent
expected_save_files = sorted(Path(row['Path']).name for row in before if Path(row['Path']).parent == save_dir)
actual_save_files = sorted(p.name for p in save_dir.glob('save_primary.json*') if p.is_file())
if expected_save_files != actual_save_files or not all(row['unchanged'] for row in preserved):
    raise RuntimeError('Owner save/settings or draft file inventory changed')
(out / 'owner-preservation.json').write_text(json.dumps(dict(files=preserved, primary_prefix_before=expected_save_files,
    primary_prefix_after=actual_save_files, unchanged=True), indent=2), encoding='utf-8')
screens = root / 'Temp/ui-composition-uat-validation/TestResults/phase7a4-screenshots'
inventory = []
for path in sorted(screens.glob('composition-*.png')):
    if path.name.startswith(('composition-uat-boundary-', 'composition-review-')):
        continue  # Detached test probes and rail history are not normal player composition captures.
    (out / 'after').mkdir(exist_ok=True)
    shutil.copy2(path, out / 'after' / path.name)
    inventory.append(dict(path='after/'+path.name, sha256=digest(path.read_bytes())))
(out / 'screenshot-index.json').write_text(json.dumps(inventory, indent=2), encoding='utf-8')
print(len(reports), 'XML reports;', len(logs), 'logs;', len(manifests), 'input inventories;', len(inventory), 'screenshots; owner files unchanged')
