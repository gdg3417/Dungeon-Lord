"""Retain C0 integrated production reports and redacted diagnostics without changing Unity inputs."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/testing/evidence/combat-c0/integration'
OUT.mkdir(parents=True, exist_ok=True)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2), encoding='utf-8')


def cases(report):
    result = {}
    for case in report.iter('test-case'):
        result.setdefault(case.get('fullname'), []).append((case.get('result'), case.findtext('reason/message')))
    return {key: sorted(values) for key, values in result.items()}


reports = []
with zipfile.ZipFile(OUT / 'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT / 'TestResults').glob('combat-c0-integration-*.xml')):
        report = ET.parse(path).getroot()
        archive.write(path, path.name)
        reports.append(dict(file=path.name, sha256=sha(path), attributes=report.attrib,
            failures=[dict(name=c.get('fullname'), message=c.findtext('failure/message'))
                      for c in report.iter('test-case') if c.get('result') == 'Failed'],
            skips=[dict(name=c.get('fullname'), reason=c.findtext('reason/message'))
                   for c in report.iter('test-case') if c.get('result') in ('Skipped', 'Inconclusive')],
            measurements=[dict(name=c.get('fullname'), output=c.findtext('output'))
                          for c in report.iter('test-case') if 'Production envelope upper' in (c.findtext('output') or '')]))
write('report-summary.json', reports)

with zipfile.ZipFile(ROOT / 'Docs/testing/evidence/combat-c0/qualification-reports.zip') as baseline:
    for mode in ('editmode', 'playmode'):
        path = ROOT / ('TestResults/combat-c0-integration-full-' + mode + '.xml')
        if not path.exists():
            continue
        current = ET.parse(path).getroot()
        if int(current.get('total', '0')) == 0 or current.get('failed') != '0':
            raise ValueError('Full suite is not passing: ' + path.name)
        old = ET.fromstring(baseline.read('combat-c0-full-' + mode + '.xml'))
        before, after = cases(old), cases(current)
        comparison = dict(removed=sorted(before.keys() - after.keys()), added=sorted(after.keys() - before.keys()),
            changed=[dict(name=k, before=before[k], after=after[k]) for k in sorted(before.keys() & after.keys())
                     if before[k] != after[k]])
        write('baseline-' + mode + '.json', comparison)
        if comparison['removed'] or comparison['changed']:
            raise ValueError('Historical test outcomes changed')


def redact(log):
    log = re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))', r'\1[REDACTED]', log)
    log = re.sub(r'(?i)(Bearer\s+)[A-Za-z0-9._~-]+', r'\1[REDACTED]', log)
    log = re.sub(r'(?i)((?:access token|Machine Id|Id):?\s+)[^\r\n]+', r'\1[REDACTED]', log)
    return log


with zipfile.ZipFile(OUT / 'qualification-support.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT / 'TestResults/combat-c0-integration').glob('*')):
        if path.is_file():
            archive.write(path, path.name)
    for path in sorted((ROOT / 'Temp').glob('combat-c0-integration-*.log')):
        archive.writestr('logs/' + path.name, redact(path.read_text(encoding='utf-8-sig', errors='replace')))
    baseline_log = ROOT / 'Temp/combat-c0-b1-baseline.log'
    if baseline_log.exists():
        archive.writestr('logs/' + baseline_log.name, redact(baseline_log.read_text(encoding='utf-8-sig', errors='replace')))
    for path in sorted((ROOT / 'TestResults/combat-c0-integration/baseline').glob('*.json')):
        archive.write(path, 'preintegration-capture/' + path.name)

build = ROOT / 'Temp/ui-composition-combat-c0-integration-validation/Builds/Development/Windows-combat-c0-integration'
if (build / 'build-report.json').exists():
    report = json.loads((build / 'build-report.json').read_text())
    with zipfile.ZipFile(OUT / 'build-reports-original.zip', 'w', zipfile.ZIP_DEFLATED) as originals:
        for name in ('build-report.json', 'build-messages.txt', 'packed-dungeon-art.txt', 'packed-source-assets.txt'):
            source = build / name
            originals.write(source, name)
            if name.endswith('.txt'):
                # Raw build bytes stay in the archive/player output; readable Git copies
                # normalize only generated trailing whitespace and line endings.
                text = '\n'.join(line.rstrip() for line in source.read_text(encoding='utf-8-sig').splitlines()) + '\n'
                (OUT / name).write_text(text, encoding='utf-8', newline='\n')
            else:
                (OUT / name).write_bytes(source.read_bytes())
    files = sorted(p for p in build.rglob('*') if p.is_file())
    namespace = 'combat-c0-integration-validation-20261014.json'
    write('windows-artifact-manifest.json', dict(report=report, files=len(files), bytes=sum(p.stat().st_size for p in files),
        executable=str(build / 'Dungeon Lord.exe'), namespace=namespace, player_launched=False,
        namespace_packed=namespace.encode() in (build / 'Dungeon Lord_Data/sharedassets0.assets').read_bytes(),
        inventory=[dict(path=p.relative_to(build).as_posix(), bytes=p.stat().st_size, sha256=sha(p)) for p in files]))

tracked = subprocess.check_output(['git', 'diff', '--name-only', 'eb80df1e3fd09c18aedb062afc108eb941deb8a3'], cwd=ROOT).decode().splitlines()
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard'], cwd=ROOT).decode().splitlines()
write('changed-files.json', sorted(set(tracked + untracked)))
print('Archived', len(reports), 'reports; historical result comparisons completed where present')
