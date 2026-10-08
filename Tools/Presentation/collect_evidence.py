"""Retain observed NUnit results, redacted Unity logs and read-only owner hashes."""
from pathlib import Path
import hashlib, json, re, zipfile
import xml.etree.ElementTree as ET
from PIL import Image

root = Path(__file__).resolve().parents[2]
evidence = root / 'Docs/testing/evidence/phase7-production-dungeon-ui-composition'
evidence.mkdir(parents=True, exist_ok=True)
reports = []
with zipfile.ZipFile(evidence/'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root/'TestResults').glob('ui-composition-*.xml')):
        data = path.read_bytes()
        try: run = ET.fromstring(data)
        except ET.ParseError: continue  # Never claim an incomplete running report.
        cases = list(run.iter('test-case'))
        row = dict(path=path.relative_to(root).as_posix(), sha256=hashlib.sha256(data).hexdigest(),
                   run_attributes=run.attrib, observed_cases=len(cases),
                   failures=[dict(name=c.get('fullname'), message=c.findtext('failure/message'),
                                  stack=c.findtext('failure/stack-trace')) for c in cases if c.get('result')=='Failed'],
                   skips=[dict(name=c.get('fullname'), reason=c.findtext('reason/message'))
                          for c in cases if c.get('result') in ('Skipped','Inconclusive')])
        reports.append(row)
        archive.writestr(path.name, data)
(evidence/'report-summary.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')

logs = []
with zipfile.ZipFile(evidence/'unity-logs-redacted.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((root/'Temp').glob('ui-composition-*.log')):
        data = path.read_text(encoding='utf-8-sig', errors='replace')
        # Unity CLI can put licensing/authentication arguments in the command-line header.
        clean, count = re.subn(r'(?i)([-/](?:access[-_]?token|auth[-_]?token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',
                              r'\1[REDACTED]', data)
        clean, bearer_count = re.subn(r'(?i)(Bearer\s+)\S+', r'\1[REDACTED]', clean)
        archive.writestr(path.name, clean)
        logs.append(dict(path=path.relative_to(root).as_posix(), redactions=count+bearer_count,
                         shared_sha256=hashlib.sha256(clean.encode()).hexdigest(),
                         compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*', clean))),
                         compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*', clean)))))
(evidence/'log-index.json').write_text(json.dumps(logs, indent=2), encoding='utf-8')

preservation = []
for filename in ('ui-composition-owner-save-before.json', 'ui-composition-owner-settings-before.json'):
    rows = json.loads((root/'TestResults'/filename).read_text(encoding='utf-8-sig'))
    if isinstance(rows, dict): rows = [rows]
    for item in rows:
        path = Path(item['Path'].replace('\\\\', '\\'))
        if not path.is_absolute(): path = root/path
        current = hashlib.sha256(path.read_bytes()).hexdigest().upper()
        preservation.append(dict(path=str(path), before_sha256=item['Hash'], after_sha256=current,
                                 unchanged=current==item['Hash'].upper()))
(evidence/'owner-preservation.json').write_text(json.dumps(preservation, indent=2), encoding='utf-8')
assert all(item['unchanged'] for item in preservation), 'Owner file changed; investigate before delivery'
art = []
for path in sorted((root/'Assets/_Project/UI/ProductionDungeon/Art').glob('*.png')):
    with Image.open(path) as source:
        dimensions = source.size
    metadata = path.with_suffix(path.suffix+'.meta').read_text(encoding='utf-8-sig')
    art.append(dict(path=path.relative_to(root).as_posix(), dimensions=dimensions,
                    source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                    meta_guid=re.search(r'^guid: (\w+)', metadata, re.MULTILINE).group(1)))
(evidence/'asset-inventory.json').write_text(json.dumps(art, indent=2), encoding='utf-8')
print(len(reports), 'complete XML reports;', len(logs), 'redacted logs;', len(preservation), 'owner hashes unchanged')
