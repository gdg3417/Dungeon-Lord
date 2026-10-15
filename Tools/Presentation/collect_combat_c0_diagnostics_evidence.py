"""Collect correction evidence separately; never overwrite earlier C0 qualification."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/testing/evidence/combat-c0/diagnostics-20261015'
OUT.mkdir(parents=True, exist_ok=True)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(name, value): (OUT/name).write_text(json.dumps(value, indent=2), encoding='utf-8')
def redact(log):
    log = re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))', r'\1[REDACTED]', log)
    log = re.sub(r'(?i)(Bearer\s+)[A-Za-z0-9._~-]+', r'\1[REDACTED]', log)
    return re.sub(r'(?i)((?:access token|Machine Id|Id):?\s+)[^\r\n]+', r'\1[REDACTED]', log)

reports = []
with zipfile.ZipFile(OUT/'qualification-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults').glob('combat-c0-diagnostics-*.xml')):
        run = ET.parse(path).getroot(); archive.write(path, path.name)
        reports.append(dict(file=path.name, sha256=sha(path), attributes=run.attrib,
            failures=[dict(name=c.get('fullname'), message=c.findtext('failure/message')) for c in run.iter('test-case') if c.get('result') == 'Failed'],
            skips=[dict(name=c.get('fullname'), reason=c.findtext('reason/message')) for c in run.iter('test-case') if c.get('result') in ('Skipped', 'Inconclusive')]))
write('report-summary.json', reports)

with zipfile.ZipFile(ROOT/'Docs/testing/evidence/combat-c0/integration/qualification-reports.zip') as baseline:
    for mode in ('editmode', 'playmode'):
        path = ROOT/('TestResults/combat-c0-diagnostics-full-'+mode+'.xml')
        for suffix in ('static', 'warm', 'serial'):
            candidate = ROOT/('TestResults/combat-c0-diagnostics-full-'+mode+'-'+suffix+'.xml')
            if candidate.exists(): path = candidate; break
        if mode == 'editmode' and not path.exists():
            current = ET.Element('test-run')
            seen = set()
            for part in ('build-gates', 'editmode-complement'):
                run = ET.parse(ROOT/('TestResults/combat-c0-diagnostics-'+part+'.xml')).getroot()
                if int(run.get('total', '0')) == 0 or run.get('failed') != '0': raise ValueError('Partition did not pass')
                for case in run.iter('test-case'):
                    if case.get('fullname') in seen: raise ValueError('Overlapping EditMode partitions')
                    seen.add(case.get('fullname')); current.append(case)
            current.set('total', str(len(seen))); current.set('failed', '0')
            write('editmode-partition-summary.json', dict(total=len(seen),
                passed=sum(c.get('result') == 'Passed' for c in current),
                skipped=sum(c.get('result') in ('Skipped', 'Inconclusive') for c in current),
                parts=['combat-c0-diagnostics-build-gates.xml', 'combat-c0-diagnostics-editmode-complement.xml'],
                unpartitioned_run='Timed out; not passing evidence'))
        else:
            current = ET.parse(path).getroot()
        if int(current.get('total', '0')) == 0 or current.get('failed') != '0': raise ValueError('Full suite did not pass')
        old = ET.fromstring(baseline.read('combat-c0-integration-full-'+mode+'.xml'))
        def cases(run): return {c.get('fullname'): (c.get('result'), c.findtext('reason/message')) for c in run.iter('test-case')}
        before, after = cases(old), cases(current)
        comparison = dict(removed=sorted(before.keys()-after.keys()), added=sorted(after.keys()-before.keys()),
            changed=[name for name in before.keys() & after.keys() if before[name] != after[name]])
        write('historical-'+mode+'-comparison.json', comparison)
        if comparison['removed'] or comparison['changed']: raise ValueError('Historical test result changed')
        if mode == 'editmode':
            focused = ET.parse(ROOT/'TestResults/combat-c0-diagnostics-focused-final03.xml').getroot()
            if after.keys() != before.keys() | cases(focused).keys(): raise ValueError('EditMode inventory coverage differs')

with zipfile.ZipFile(OUT/'qualification-support.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults/combat-c0-diagnostics').glob('*')):
        if path.is_file(): archive.write(path, path.name)
    for path in sorted((ROOT/'Temp').glob('combat-c0-diagnostics-*.log')):
        archive.writestr('logs/'+path.name, redact(path.read_text(encoding='utf-8-sig', errors='replace')))
    for directory in ('native-import-failure', 'native-import-retry', 'native-import-r2-stall',
                      'complement-reimport-stall', 'serial-import-stall', 'warm-dynamic-font-stall'):
        for path in sorted((ROOT/'TestResults/combat-c0-diagnostics'/directory).glob('*')):
            if path.is_file():
                archive.writestr(directory+'/'+path.name, redact(path.read_text(encoding='utf-8-sig', errors='replace')))

build = ROOT/'Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c0-integration'
report = json.loads((build/'build-report.json').read_text())
files = sorted(p for p in build.rglob('*') if p.is_file())
namespace = 'combat-c0-diagnostics-validation-20261015.json'
write('windows-artifact-manifest.json', dict(report=report, files=len(files), bytes=sum(p.stat().st_size for p in files),
    executable=str(build/'Dungeon Lord.exe'), namespace=namespace, player_launched=False,
    namespace_packed=namespace.encode() in (build/'Dungeon Lord_Data/sharedassets0.assets').read_bytes(),
    inventory=[dict(path=p.relative_to(build).as_posix(), bytes=p.stat().st_size, sha256=sha(p)) for p in files]))
with zipfile.ZipFile(OUT/'build-reports.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for name in ('build-report.json', 'build-messages.txt', 'packed-dungeon-art.txt', 'packed-source-assets.txt'): archive.write(build/name, name)
write('changed-files.json', subprocess.check_output(['git','diff','--name-only','164fc6cb97bf48a9731a4e797858ff0a1e418c9b'],cwd=ROOT,text=True).splitlines())
print('Correction reports, redacted logs, historical comparisons and Windows inventory collected.')
