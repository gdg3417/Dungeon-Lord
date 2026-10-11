"""Retain PR 231 map-reassignment correction evidence without rewriting the original packet."""
from pathlib import Path
from datetime import datetime
import hashlib, json, re, shutil, subprocess, xml.etree.ElementTree as ET, zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'Docs/testing/evidence/phase7-graphical-room-content/map-reassignment'
PROJECT = ROOT/'Temp/ui-composition-floorhud-validation'
PLAYER = PROJECT/'Builds/Development/Windows-map-reassignment'
REVIEWED = 'ed06ad9e6f7fd8693b5ff9867b45d469e4ac395e'
OUT.mkdir(parents=True, exist_ok=True)
def sha(data): return hashlib.sha256(data).hexdigest()
def write(name, value): (OUT/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT).decode().strip()
def time(value): return datetime.fromisoformat(value.replace('Z','+00:00'))
def cases(run):
    result = {}
    for case in run.iter('test-case'): result.setdefault(case.get('fullname'),[]).append(case.get('result'))
    return {name:sorted(values) for name,values in result.items()}

reports = []
with zipfile.ZipFile(OUT/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults').glob('map-reassignment-*.xml')):
        data=path.read_bytes(); run=ET.fromstring(data); archive.writestr(path.name,data)
        reports.append(dict(path=path.name,sha256=sha(data),attributes=run.attrib,
            failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace'))
                for c in run.iter('test-case') if c.get('result')=='Failed'],
            skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message'))
                for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')],
            measured_output=[dict(name=c.get('fullname'),output=c.findtext('output'))
                for c in run.iter('test-case') if 'RoomContentMapReassignment' in c.get('fullname','')]))
write('report-summary.json',reports)
final = [ET.parse(ROOT/('TestResults/map-reassignment-full-'+mode+'.xml')).getroot() for mode in ('editmode','playmode')]
assert all(r.get('failed')=='0' for r in final), 'Final test failures'
comparison=[]
with zipfile.ZipFile(OUT.parent/'compact-placement/qualification-reports.zip') as archive:
    for mode,new in zip(('editmode','playmode'),final):
        old=cases(ET.fromstring(archive.read('placement-compact-full-'+mode+'.xml'))); new=cases(new)
        comparison.append(dict(mode=mode,removed=sorted(old.keys()-new.keys()),added=sorted(new.keys()-old.keys()),
            changed=[dict(name=n,before=old[n],after=new[n]) for n in sorted(old.keys()&new.keys()) if old[n]!=new[n]]))
assert all(not r['removed'] and not r['changed'] for r in comparison), 'Existing outcomes changed'
write('test-outcome-comparison.json',comparison)

checkpoints=[]
with zipfile.ZipFile(OUT/'source-inventories-and-diffs.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'TestResults').glob('map-reassignment-*.json')):
        value=json.loads(path.read_text(encoding='utf-8-sig'))
        if not isinstance(value,dict) or 'input_roots' not in value: continue
        data=path.read_bytes(); archive.writestr(path.name,data)
        checkpoints.append(dict(path=path.name,sha256=sha(data),head=value.get('source_head'),
            expected=value['expected_count'],actual=value['actual_count'],failures=value['failures'],unexpected=value['unexpected']))
    for path in sorted((ROOT/'TestResults').glob('map-reassignment-*.diff')): archive.write(path,path.name)
assert all(not c['unexpected'] for c in checkpoints), 'Unexpected source inputs'
assert not next(c for c in checkpoints if c['path']=='map-reassignment-final-source.json')['failures']
write('source-checkpoint-index.json',checkpoints)
logs=[]
with zipfile.ZipFile(OUT/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted((ROOT/'Temp').glob('map-reassignment-*.log')):
        clean,count=re.subn(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',
            r'\1[REDACTED]',path.read_text(encoding='utf-8-sig',errors='replace'))
        clean,license_tokens=re.subn(r'(?i)(access token )\S+',r'\1[REDACTED]',clean)
        clean,bearer=re.subn(r'(?i)(Bearer\s+)\S+',r'\1[REDACTED]',clean)
        archive.writestr(path.name,clean)
        logs.append(dict(path=path.name,sha256=sha(clean.encode()),redactions=count+bearer+license_tokens,
            compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*',clean))),
            compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*',clean)))))
write('log-index.json',logs)

before=json.loads((ROOT/'TestResults/map-reassignment-owner-before.json').read_text(encoding='utf-8-sig'))
preserved=[dict(path=r['Path'],before=r['Hash'].lower(),after=sha(Path(r['Path']).read_bytes())) for r in before]
assert all(r['before']==r['after'] for r in preserved), 'Audited owner/validation state changed'
save_dir=Path(before[0]['Path']).parent
old_names=sorted(Path(r['Path']).name for r in before if Path(r['Path']).parent==save_dir)
new_names=sorted(p.name for p in save_dir.iterdir() if p.is_file() and
    (p.name.startswith('save_primary.json') or p.name.startswith('phase7-ui-composition-validation-01a11d08.json')))
assert old_names==new_names, 'Audited save/sidecar inventory changed'
write('owner-preservation.json',dict(files=preserved,prefix_before=old_names,prefix_after=new_names,unchanged=True))
old_player=json.loads((ROOT/'TestResults/map-reassignment-builds-before.json').read_text(encoding='utf-8-sig'))
assert all(r['Hash'].lower()==sha(Path(r['Path']).read_bytes()) for r in old_player), 'Earlier owner build changed'
assert len(old_player)==sum(p.is_file() for folder in ('Windows','Windows-room-content-uat','Windows-placement-compact') for p in (PROJECT/'Builds/Development'/folder).rglob('*'))
write('earlier-build-preservation.json',dict(paths=['Temp/ui-composition-floorhud-validation/Builds/Development/Windows','Temp/ui-composition-floorhud-validation/Builds/Development/Windows-room-content-uat','Temp/ui-composition-floorhud-validation/Builds/Development/Windows-placement-compact'],
    files=len(old_player),before_inventory_sha256=sha(json.dumps(old_player,sort_keys=True).encode()),unchanged=True))
shutil.copy2(ROOT/'TestResults/map-reassignment-project-before.json',OUT/'project-inventory-before.json')

build=json.loads((PLAYER/'build-report.json').read_text(encoding='utf-8-sig'))
assert build['buildResult']=='Succeeded' and build['errorCount']==0 and build['developmentBuild']
assert build['targetPlatform']=='StandaloneWindows64' and all(time(build['utcTimestamp'])>=time(r.get('end-time')) for r in final)
with zipfile.ZipFile(OUT/'build-reports-original.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for name in ('build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt'):
        archive.write(PLAYER/name,name)
        (OUT/name).write_text('\n'.join(line.rstrip() for line in (PLAYER/name).read_text(encoding='utf-8-sig').splitlines())+'\n',encoding='utf-8')
art=(PLAYER/'packed-dungeon-art.txt').read_text(encoding='utf-8-sig').splitlines()
assert set(art).issubset(set((PLAYER/'packed-source-assets.txt').read_text(encoding='utf-8-sig').splitlines()))
files=[dict(path=p.relative_to(PLAYER).as_posix(),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(PLAYER.rglob('*')) if p.is_file()]
write('windows-artifact-manifest.json',dict(path=str(PLAYER),build=build,packed_art_count=len(art),files=files))
screens=[]; (OUT/'screenshots').mkdir(exist_ok=True)
for path in sorted((PROJECT/'TestResults/phase7a4-screenshots').glob('map-reassignment-*.png')):
    assert path.stat().st_mtime>=time(final[1].get('start-time')).timestamp(), 'Stale UAT capture'
    shutil.copy2(path,OUT/'screenshots'/path.name)
    screens.append(dict(path='screenshots/'+path.name,sha256=sha(path.read_bytes()),bytes=path.stat().st_size))
assert len(screens)==1
write('screenshot-index.json',screens)
write('qualified-input-trees.json',dict(reviewed=REVIEWED,qualified_implementation=git('rev-parse','HEAD'),
    branch=git('branch','--show-current'),trees={name:git('rev-parse','HEAD:'+name) for name in ('Assets','Packages','ProjectSettings','ContentAuthoring')},
    changed_files=git('diff','--name-only',REVIEWED,'HEAD').splitlines(),save_schema=13,draft_format=5,draft_record_version=4))
print(len(reports),'XML reports;',len(checkpoints),'source inventories;',len(screens),'screenshots;',len(files),'player files; audited saves and earlier build unchanged')
