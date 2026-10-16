"""Collect actual C2 verdicts and hashes. Raw Unity logs remain local and untouched."""
from pathlib import Path
import hashlib, json, subprocess, zipfile, xml.etree.ElementTree as ET
import qualify_combat_c2 as c2

audit=c2.audit
target=audit.ROOT/'docs/testing/evidence/combat-c2'
target.mkdir(parents=True,exist_ok=True)
def digest(data): return hashlib.sha256(data).hexdigest()
reports=[]
for path in sorted((audit.ROOT/'TestResults').glob('combat-c2*.xml')):
    root=ET.parse(path).getroot()
    cases=list(root.iter('test-case'))
    reports.append(dict(path=path.relative_to(audit.ROOT).as_posix(),sha256=audit.sha(path),
        result=root.get('result'),total=int(root.get('total','0')),passed=int(root.get('passed','0')),
        failed=int(root.get('failed','0')),skipped=int(root.get('skipped','0')),
        ignored=sum(t.get('result')=='Skipped' and t.get('label')=='Ignored' for t in root.iter('test-case')),
        skipped_not_ignored=sum(t.get('result')=='Skipped' and t.get('label')!='Ignored' for t in root.iter('test-case')),
        nonpassing=[dict(name=t.get('fullname'),result=t.get('result'),label=t.get('label'),reason=t.findtext('reason/message'))
            for t in root.iter('test-case') if t.get('result')!='Passed'],
        suites={key:{verdict:sum(key in (t.get('classname','')+' '+t.get('fullname','')) and t.get('result')==verdict for t in cases)
            for verdict in ('Passed','Failed','Skipped')} for key in ('CombatC2','CombatC0','CombatC1','LivingDungeonPhaseB1',
                'LivingDungeonPhaseB2','ProductionSpatialContentBuildGateTests','Gd66Windows')},
        duration=root.get('duration'),failures=[dict(name=t.get('fullname'),message=t.findtext('failure/message'))
            for t in root.iter('test-case') if t.get('result')=='Failed']))
commands=[]
for path in sorted(audit.OUT.glob('commands-*.json')):
    rows=json.loads(path.read_text(encoding='utf-8-sig'))
    commands.extend(rows if isinstance(rows,list) else [rows])
frozen_paths=['Assets/_Project/Resources/save_spatial_migration_limits_schema13.json',
              'Assets/_Project/Tests/EditMode/combat-c0-b1-golden.zip']
frozen=[]
for name in frozen_paths:
    original=subprocess.check_output(['git','show','0624bc0bbbd34a8ceff26d764d7f35c3b464d8bc:'+name],cwd=audit.ROOT)
    current=(audit.ROOT/name).read_bytes()
    frozen.append(dict(path=name,baseline_sha256=digest(original),current_sha256=digest(current),equal=original==current))
name='Assets/_Project/Data/Production/Save/save_spatial_migration_limits.json'
original=subprocess.check_output(['git','show','0624bc0bbbd34a8ceff26d764d7f35c3b464d8bc:'+name],cwd=audit.ROOT)
copy=audit.ROOT/'Assets/_Project/Resources/save_spatial_migration_limits_schema14.json'
frozen.append(dict(path=copy.relative_to(audit.ROOT).as_posix(),baseline_source=name,
    baseline_sha256=digest(original),current_sha256=audit.sha(copy),equal=original==copy.read_bytes()))
if not all(row['equal'] for row in frozen): raise ValueError('Frozen input differs')
summary=dict(baseline='0624bc0bbbd34a8ceff26d764d7f35c3b464d8bc',branch='combat-c2-monster-continuity',
    project=str(audit.ROOT/'Temp'/audit.NAME),namespace=audit.inputs.NAMESPACE,reports=reports,commands=commands,
    frozen_inputs=frozen,raw_logs='Retained under repository Temp; not exported with licensing command lines',
    review='Independent ChatGPT review and owner Editor/Windows UAT required before merge approval')
inputs=audit.inputs.compare(audit.ROOT,audit.NAME)
if inputs['failures']: raise ValueError('Final qualification inputs differ from source')
summary['final_source_inputs']=dict(count=inputs['expected_count'],failures=len(inputs['failures']),
    files=inputs['files'],permitted_transformations=inputs['permitted_transformations'])
summary['full_stage_source_differences']={}
for path in sorted([*audit.OUT.glob('inputs-final-04-*-exact.json'),*audit.OUT.glob('inputs-final-06-*-exact.json')]):
    previous={r['path']:r['source_sha256'] for r in json.loads(path.read_text())['files']}
    differences=[r['path'] for r in inputs['files'] if previous.get(r['path'])!=r['source_sha256']]
    if any('/Tests/' not in name for name in differences): raise ValueError('Gameplay source changed after full qualification: '+str(differences))
    summary['full_stage_source_differences'][path.name]=differences
summary['protected_owner_save']=dict(path=str(audit.SAVE/'combat-c0-diagnostics-validation-20261015.json'),
    sha256=audit.sha(audit.SAVE/'combat-c0-diagnostics-validation-20261015.json'))
build=audit.ROOT/'Temp'/audit.NAME/'Builds/Development/Windows-combat-c2-20261016'
if not (build/'Dungeon Lord.exe').is_file(): raise ValueError('Missing final Windows player')
packed=(build/'packed-source-assets.txt').read_text(encoding='utf-8-sig').splitlines()
required=['Assets/_Project/Resources/monster_recovery.json','Assets/_Project/Resources/save_spatial_migration_limits_schema14.json',
    'Assets/_Project/Resources/save_spatial_migration_limits_schema13.json','Assets/_Project/Data/Bootstrap/build_config.json',
    'Assets/_Project/Data/Bootstrap/string_table_en.json']
missing=[name for name in required if name not in packed]
if missing: raise ValueError('Missing packed C2 dependencies: '+str(missing))
namespace=[p.relative_to(build).as_posix() for p in build.rglob('*.assets') if audit.inputs.NAMESPACE.encode() in p.read_bytes()]
if not namespace: raise ValueError('Isolated namespace absent from packed player assets')
build_report=json.loads((build/'build-report.json').read_text(encoding='utf-8-sig'))
if build_report['buildResult']!='Succeeded' or not build_report['developmentBuild'] or build_report['errorCount']!=0:
    raise ValueError('No successful Development Build verdict')
summary['windows_build']=dict(directory=str(build),report=build_report,packed_required=required,namespace_assets=namespace,
    messages=(build/'build-messages.txt').read_text(encoding='utf-8-sig').splitlines(),
    files=[dict(path=p.relative_to(build).as_posix(),sha256=audit.sha(p),bytes=p.stat().st_size) for p in sorted(build.rglob('*')) if p.is_file()])
for filename in ('preservation-after.json','owner-environment-after.json'):
    path=audit.OUT/filename
    if path.exists():
        rows=json.loads(path.read_text(encoding='utf-8-sig'))
        summary[filename]=dict(files=len(rows),unchanged=sum(r['equal'] for r in rows),changed=sum(not r['equal'] for r in rows))
(target/'qualification-manifest.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
archive=target/'qualification-reports.zip'
with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as z:
    for report in reports: z.write(audit.ROOT/report['path'],Path(report['path']).name)
    for path in sorted(audit.OUT.glob('*.json')): z.write(path,'audit/'+path.name)
    for path in audit.OUT.glob('inputs-final-04-failed-play-before-refresh.zip'): z.write(path,'audit/'+path.name)
print(len(reports),'actual XML reports;',len(commands),'recorded staged commands; frozen inputs match')
