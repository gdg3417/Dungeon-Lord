"""Archive C1 qualification, keeping original attempts and owner saves intact."""
from pathlib import Path
import hashlib, json, re, subprocess, zipfile
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/testing/evidence/combat-c1'
OUT.mkdir(parents=True,exist_ok=True)
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(name,value): (OUT/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
def redact(log):
    log=re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',log)
    log=re.sub(r'(?i)(Bearer\s+)[A-Za-z0-9._~-]+',r'\1[REDACTED]',log)
    return re.sub(r'(?i)((?:access token|Machine Id|Id):?\s+)[^\r\n]+',r'\1[REDACTED]',log)
reports=[]
with zipfile.ZipFile(OUT/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for p in sorted((ROOT/'TestResults').glob('combat-c1-*.xml')):
        run=ET.parse(p).getroot(); archive.write(p,p.name)
        reports.append(dict(file=p.name,sha256=sha(p),attributes=run.attrib,
            failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message')) for c in run.iter('test-case') if c.get('result')=='Failed'],
            skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')]))
write('report-summary.json',reports)
with zipfile.ZipFile(OUT/'qualification-support.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for p in sorted((ROOT/'TestResults/combat-c1').glob('*')):
        if p.is_file(): archive.write(p,p.name)
    for p in sorted((ROOT/'Temp').glob('combat-c1-*.log')):
        archive.writestr('logs/'+p.name,redact(p.read_text(encoding='utf-8-sig',errors='replace')))
    helper=ROOT/'TestResults/combat-c1-input-helper-tests.txt'
    if helper.exists(): archive.write(helper,helper.name)
    for p in ('temporary-project-inventory.json','preservation-after.json','owner-inputs-refreshed.json','inputs-final-exact.json'):
        source=ROOT/'TestResults/combat-c1'/p
        if source.exists(): (OUT/p).write_bytes(source.read_bytes())
screens=sorted((ROOT/'Temp/ui-composition-combat-c0-access-validation/TestResults/phase7a4-screenshots').glob('composition-combat-c1*.png'))
with zipfile.ZipFile(OUT/'screenshots.zip','w',zipfile.ZIP_STORED) as archive:
    for p in screens: archive.write(p,p.name)
write('screenshots.json',[dict(file=p.name,sha256=sha(p),bytes=p.stat().st_size) for p in screens])
build=ROOT/'Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c1'
if (build/'build-report.json').exists():
    report=json.loads((build/'build-report.json').read_text())
    if report['buildResult']!='Succeeded' or report['errorCount']!=0 or not report['developmentBuild']: raise ValueError('Unqualified build')
    files=sorted(p for p in build.rglob('*') if p.is_file()); namespace='combat-c0-diagnostics-validation-20261015.json'
    packed=namespace.encode() in (build/'Dungeon Lord_Data/sharedassets0.assets').read_bytes()
    if not packed: raise ValueError('Disposable namespace not packed')
    with zipfile.ZipFile(OUT/'build-reports-original.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for name in ('build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt'):
            p=build/name; archive.write(p,name)
    write('windows-artifact-manifest.json',dict(report=report,executable=str(build/'Dungeon Lord.exe'),namespace=namespace,
        namespace_packed=packed,player_launched=False,files=len(files),bytes=sum(p.stat().st_size for p in files),
        inventory=[dict(path=p.relative_to(build).as_posix(),bytes=p.stat().st_size,sha256=sha(p)) for p in files]))
baseline='22e7ce1b08ae17391bfd3542a45821a46f02bbe1'
tracked=subprocess.check_output(['git','diff','--name-only',baseline],cwd=ROOT,text=True).splitlines()
untracked=subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=ROOT,text=True).splitlines()
write('changed-files.json',sorted(set(tracked+untracked)))
print(len(reports),'reports;',len(screens),'screenshots archived')
