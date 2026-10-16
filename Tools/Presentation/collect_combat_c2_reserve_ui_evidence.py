"""Collect correction evidence separately from the original C2 qualification packet."""
from pathlib import Path
import hashlib, json, shutil, subprocess, xml.etree.ElementTree as ET, zipfile
import qualify_combat_c2_reserve_ui as qualification

ROOT = qualification.audit.ROOT
OUT = qualification.audit.OUT
DEST = ROOT/'Docs/testing/evidence/combat-c2/reserve-affordability'
PROJECT = ROOT/'Temp'/qualification.audit.NAME
BUILD = PROJECT/'Builds/Development/Windows-combat-c2-reserve-ui-20261016'
BASE = 'd3f81ab866ff9c35b72ceb9fd86ef8015609b4f0'
sha = qualification.audit.sha

def main():
    DEST.mkdir(parents=True, exist_ok=True)
    manifest = dict(reviewed_head=BASE, qualification_project=str(PROJECT), unity='6000.3.2f1', reports=[])
    commands=[]
    for filename in ('commands-02-tests.json','commands-03-play.json','commands-03-build.json'):
        records=json.loads((OUT/filename).read_text(encoding='utf-8-sig'))
        if not isinstance(records,list): records=[records]
        commands.extend(r for r in records if filename!='commands-02-tests.json' or r['name']=='EditMode')
    manifest['final_commands']=commands
    lines=['# Final correction qualification commands','',
        'Executed in `C:/Dev/Dungeon-Lord`; exact arguments and exit codes from the staged runner. Original C2 full-suite results remain separate.','']
    for r in commands:
        quote=lambda s: "'"+s.replace("'","''")+"'"
        lines.extend(['## '+r['name'],'', '```powershell', '& '+quote(r['executable'])+' '+ ' '.join(quote(a) for a in r['arguments']),
            '```','', 'CLI exit: '+str(r['exitCode'])+'.',''])
    (DEST/'commands.md').write_text('\n'.join(lines),encoding='utf-8')
    with zipfile.ZipFile(DEST/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for p in sorted((ROOT/'TestResults').glob('combat-c2-reserve-ui-*.xml')):
            run=ET.parse(p).getroot(); cases=list(run.iter('test-case'))
            row=dict(path=str(p),sha256=sha(p),attributes=run.attrib,
                passed=sum(c.get('result')=='Passed' for c in cases),
                failed=sum(c.get('result')=='Failed' for c in cases),
                ignored=sum(c.get('result')=='Skipped' and c.get('label')=='Ignored' for c in cases),
                other_skipped=sum(c.get('result')=='Skipped' and c.get('label')!='Ignored' for c in cases),
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message')) for c in cases if c.get('result')=='Failed'],
                production_gates=sum(c.get('result')=='Passed' and 'ProductionSpatialContentBuildGateTests' in c.get('fullname','') for c in cases))
            manifest['reports'].append(row); archive.write(p,p.name)
        for p in sorted(OUT.glob('*.json')): archive.write(p,'audits/'+p.name)
        for name in ('build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt'):
            p=BUILD/name; archive.write(p,'build/'+name)
        manifest['build']=json.loads((BUILD/'build-report.json').read_text(encoding='utf-8-sig'))
        if manifest['build']['buildResult']!='Succeeded' or manifest['build']['errorCount']!=0 or not manifest['build']['developmentBuild']:
            raise ValueError('No successful Development Build verdict')
        packed=(BUILD/'packed-source-assets.txt').read_text(encoding='utf-8-sig').splitlines()
        required=['Assets/_Project/Resources/monster_recovery.json','Assets/_Project/Resources/save_spatial_migration_limits_schema14.json',
            'Assets/_Project/Resources/save_spatial_migration_limits_schema13.json','Assets/_Project/Data/Bootstrap/build_config.json',
            'Assets/_Project/Data/Bootstrap/string_table_en.json']
        if any(p not in packed for p in required): raise ValueError('Missing C2 packed dependency')
        namespace_assets=[str(p.relative_to(BUILD)) for p in BUILD.rglob('*.assets') if qualification.audit.inputs.NAMESPACE.encode() in p.read_bytes()]
        if not namespace_assets: raise ValueError('Isolated save namespace absent from player')
        manifest['namespace_assets']=namespace_assets
        manifest['packed_required']=required
        manifest['build_files']=[dict(path=str(p.relative_to(BUILD)),sha256=sha(p),bytes=p.stat().st_size) for p in sorted(BUILD.rglob('*')) if p.is_file()]
        # Core/config inputs must remain byte-identical; SaveService's presentation-only diff is reviewed separately.
        names=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE],cwd=ROOT,text=True).splitlines()
        changed=subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets/_Project/Scripts/Gameplay',
            'Assets/_Project/Resources','Assets/_Project/Data'],cwd=ROOT,text=True).splitlines()
        if changed: raise ValueError('Core/config changes: '+str(changed))
        unchanged=[]
        for name in names:
            if not (name.startswith('Assets/_Project/Scripts/Gameplay/') or name.startswith('Assets/_Project/Resources/') or
                    name.startswith('Assets/_Project/Data/')): continue
            path=ROOT/name
            unchanged.append(dict(path=name,sha256=sha(path)))
        manifest['unchanged_core_and_configuration']=unchanged
        manifest['original_c2_evidence']=[]
        for name in ('implementation-and-qualification.md','qualification-manifest.json','qualification-reports.zip','changed-files.md'):
            relative='Docs/testing/evidence/combat-c2/'+name
            original=subprocess.check_output(['git','show',BASE+':'+relative],cwd=ROOT)
            path=ROOT/relative
            if original.replace(b'\r\n',b'\n')!=path.read_bytes().replace(b'\r\n',b'\n'): raise ValueError('Original evidence changed: '+relative)
            manifest['original_c2_evidence'].append(dict(path=relative,sha256=sha(path)))
        for key, filename in [('preservation','preservation-after.json'),('owner_environment','owner-environment-after.json')]:
            rows=json.loads((OUT/filename).read_text()); failures=[r['path'] for r in rows if not r['equal']]
            if failures: raise ValueError(str(failures))
            manifest[key]=dict(checked=len(rows),changed=len(failures))
        owner=Path.home()/'AppData/LocalLow/gdg3417/Dungeon Lord/combat-c0-diagnostics-validation-20261015.json'
        manifest['protected_owner_save']=dict(path=str(owner),sha256=sha(owner))
        manifest['screenshots']=[]
        for p in sorted((PROJECT/'TestResults/phase7a4-screenshots').glob('combat-c2-reserve-ui*.png')):
            target=DEST/'screenshots'/p.name; target.parent.mkdir(exist_ok=True); shutil.copy2(p,target)
            manifest['screenshots'].append(dict(path=str(target.relative_to(ROOT)),sha256=sha(target)))
    (DEST/'qualification-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps(dict(reports=[{k:r[k] for k in ('path','passed','failed','ignored','other_skipped','production_gates')} for r in manifest['reports']],
        preservation=manifest['preservation'],owner_environment=manifest['owner_environment'],unchanged_core_inputs=len(unchanged)),indent=2))

if __name__=='__main__': main()
