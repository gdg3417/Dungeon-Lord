"""Collect PR230 correction results separately; preserve all historical archives."""
from pathlib import Path
import hashlib, json, re, shutil, xml.etree.ElementTree as ET, zipfile

root = Path(__file__).resolve().parents[2]
out = root/'Docs/testing/evidence/phase7-production-dungeon-ui-composition/review-corrections'
out.mkdir(parents=True,exist_ok=True)
reports = []
with zipfile.ZipFile(out/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root/'TestResults').glob('ui-review-*.xml')):
        data = path.read_bytes()
        run = ET.fromstring(data)  # Incomplete reports must fail, not disappear.
        cases = list(run.iter('test-case'))
        reports.append(dict(path=path.relative_to(root).as_posix(),sha256=hashlib.sha256(data).hexdigest(),
            run_attributes=run.attrib,observed_cases=len(cases),
            failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace'))
                      for c in cases if c.get('result')=='Failed'],
            skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message'))
                   for c in cases if c.get('result') in ('Skipped','Inconclusive')],
            observations=[c.findtext('output') for c in cases if c.findtext('output') and 'Rail ' in c.findtext('output')]))
        archive.writestr(path.name,data)
(out/'report-summary.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
logs = []
with zipfile.ZipFile(out/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root/'Temp').glob('ui-review-*.log')):
        data = path.read_text(encoding='utf-8-sig',errors='replace')
        clean,count = re.subn(r'(?i)([-/](?:access[-_]?token|auth[-_]?token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',data)
        clean,bearer = re.subn(r'(?i)(Bearer\s+)\S+',r'\1[REDACTED]',clean)
        archive.writestr(path.name,clean)
        logs.append(dict(path=path.relative_to(root).as_posix(),redactions=count+bearer,
            shared_sha256=hashlib.sha256(clean.encode()).hexdigest(),
            compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*',clean))),
            compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*',clean)))))
(out/'log-index.json').write_text(json.dumps(logs,indent=2),encoding='utf-8')
shutil.copy2(root/'TestResults/ui-review-helper-tests.txt',out/'helper-tests.txt')
preserved = []
for row in json.loads((out/'owner-before.json').read_text()):
    after=hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()
    preserved.append(dict(path=row['path'],before_sha256=row['sha256'],after_sha256=after,unchanged=after==row['sha256']))
(out/'owner-preservation.json').write_text(json.dumps(preserved,indent=2),encoding='utf-8')
if not all(row['unchanged'] for row in preserved): raise RuntimeError('Owner preservation mismatch')
screenshots=root/'Temp/ui-composition-review-validation/TestResults/phase7a4-screenshots'
for path in screenshots.glob('composition-review-rail*.png'):
    (out/'screenshots').mkdir(exist_ok=True)
    shutil.copy2(path,out/'screenshots'/path.name)
print(len(reports),'reports;',len(logs),'logs;',len(preserved),'owner files unchanged')
