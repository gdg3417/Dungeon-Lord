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
def cases(run):
    result={}
    renames={
        'ReplayRetainsHistoricalSupportedEventsAndRejectsC0WithLocalizedExplanation':'ReplayRetainsHistoricalSupportedEventsAndRejectsMismatchedC0Layout',
        'ReplayDistinguishesC0FromMissingHistoricalEvidenceWithoutEnablingIt':'ReplayRequiresTransientDetailsForBothC0AndHistoricalRuns'}
    for c in run.iter('test-case'):
        name=c.get('fullname')
        for old,new in renames.items(): name=name.replace(old,new)
        result.setdefault(name,[]).append((c.get('result'),c.findtext('reason/message')))
    return {k:sorted(v) for k,v in result.items()}
with zipfile.ZipFile(ROOT/'Docs/testing/evidence/combat-c0/diagnostics-20261015/qualification-reports.zip') as baseline:
    for mode in ('editmode','playmode'):
        p=ROOT/('TestResults/combat-c1-full-'+mode+'.xml')
        if not p.exists() and mode=='editmode':
            parts=[ROOT/'TestResults'/('combat-c1-'+name+'.xml') for name in ('build-gates','art-import','editmode-remainder')]
            if not all(p.exists() for p in parts): continue
            current=ET.Element('test-run'); seen=set()
            for part in parts:
                run=ET.parse(part).getroot()
                if run.get('failed')!='0': raise ValueError('EditMode partition failed')
                for c in run.iter('test-case'):
                    if c.get('fullname') in seen: raise ValueError('EditMode partitions overlap')
                    seen.add(c.get('fullname')); current.append(c)
            current.set('total',str(len(seen))); current.set('failed','0')
            current.set('passed',str(sum(c.get('result')=='Passed' for c in current)))
            current.set('skipped',str(sum(c.get('result') in ('Skipped','Inconclusive') for c in current)))
            current.set('origin','Three disjoint EditMode partitions; interrupted combined attempts produced no verdict')
            write('editmode-coverage.json',dict(current.attrib,parts=[p.name for p in parts],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in current if c.get('result') in ('Skipped','Inconclusive')]))
        elif not p.exists(): continue
        else: current=ET.parse(p).getroot()
        if current.get('failed')!='0' or int(current.get('total','0'))==0: raise ValueError('Full suite did not pass')
        old=ET.fromstring(baseline.read('combat-c0-diagnostics-full-'+mode+'-static.xml'))
        before,after=cases(old),cases(current)
        comparison=dict(baseline=old.attrib,current=current.attrib,removed=sorted(before.keys()-after.keys()),added=sorted(after.keys()-before.keys()),
            changed=[dict(name=k,before=before[k],after=after[k]) for k in sorted(before.keys()&after.keys()) if before[k]!=after[k]],
            intentional_renames='Two C0 replay-guard tests now assert C1 layout/detail validation; all assertion coverage retained.')
        # The last C0 full report predates its owner-approved overlay reversion.
        # Precisely identify that one historical replaced method, never ignore other removals.
        comparison['pre_c1_removed']=[k for k in comparison['removed'] if k.endswith(('.C0FocusedDiagnosticsUseExistingPagingAndConstrainTextToSafeArea','.FocusedC0OverlayPagesWithoutOverflow'))]
        comparison['pre_c1_change_evidence']='git diff 27dc09b991e6354ce64735ea3a47e11279c3cce5 22e7ce1b08ae17391bfd3542a45821a46f02bbe1 -- Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs Assets/_Project/Editor/DungeonSpatial/Tests/CombatC0DiagnosticsEditModeFixtures.cs; runtime method and Editor adapter replaced by two overflow/stale-paging cases before C1.'
        write('baseline-'+mode+'-comparison.json',comparison)
        if set(comparison['removed'])-set(comparison['pre_c1_removed']) or comparison['changed']: raise ValueError('Existing test outcome or skip changed')
with zipfile.ZipFile(OUT/'qualification-support.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for p in sorted((ROOT/'TestResults/combat-c1').glob('*')):
        if p.is_file(): archive.write(p,p.name)
    for p in sorted((ROOT/'Temp').glob('combat-c1-*.log')):
        archive.writestr('logs/'+p.name,redact(p.read_text(encoding='utf-8-sig',errors='replace')))
    for p in sorted((ROOT/'TestResults/combat-c1/retired-projects').glob('*-manifest.json')):
        archive.write(p,'retired-projects/'+p.name)
    helper=ROOT/'TestResults/combat-c1-input-helper-tests.txt'
    if helper.exists(): archive.write(helper,helper.name)
    for p in ('temporary-project-inventory.json','qualification-applicability.json'):
        source=ROOT/'TestResults/combat-c1'/p
        if source.exists(): (OUT/p).write_bytes(source.read_bytes())
preservation=ROOT/'TestResults/combat-c1/preservation-after.json'
if preservation.exists():
    rows=json.loads(preservation.read_text())
    write('preservation-summary.json',dict(protected_files=len(rows),changed=[r for r in rows if not r['equal']],
        archived_originals=sum('preserved_in_archive' in r for r in rows),
        files_at_original_paths=sum(r.get('original_path_exists',True) for r in rows),
        owner_and_primary=[r for r in rows if Path(r['path']).name in ('save_primary.json','combat-c0-diagnostics-validation-20261015.json')],
        complete_inventory='qualification-support.zip: preservation-before.json and preservation-after.json'))
initial_inventory=ROOT/'TestResults/combat-c1/temporary-project-inventory.json'
if initial_inventory.exists():
    rows=json.loads(initial_inventory.read_text())
    for r in rows:
        if r['project']=='ui-composition-combat-c0-diagnostics-validation-r2':
            r['disposition']='retain final C1 automated qualification environment'
            r['namespace']='combat-c0-access-validation-20261015.json'
            r['cache_preservation']='Original Library retained as Library-before-combat-c1-20261015; warm cache copied read-only'
        elif r['project']=='ui-composition-combat-c0-access-validation':
            r['disposition']='obsolete initial focused environment; inspect housekeeping result below'
        manifest=ROOT/'TestResults/combat-c1/retired-projects'/(r['project']+'-manifest.json')
        if manifest.exists():
            retired=json.loads(manifest.read_text(encoding='utf-8-sig'))
            r['disposition']='removed after complete non-cache archival' if retired['removed'] else 'archived; removal pending'
            r['archive']=retired['archive']; r['archive_sha256']=retired['archive_sha256']
        elif r['project']=='ui-composition-combat-c0-b1-baseline':
            r['disposition']='retain pending provenance of extra golden-capture scripts'
        elif r['project']=='ui-composition-combat-c0-diagnostics-validation':
            r['disposition']='retain pending provenance of four source differences from documented qualification commit'
        elif r['project'] not in ('ui-composition-combat-c0-diagnostics-validation-r2','ui-composition-combat-c0-diagnostics-validation-warm'):
            r['disposition']='retain pending ownership and unique historical build/evidence audit; outside verified C0 copies'
        r['exists_after_housekeeping']=(ROOT/'Temp'/r['project']).exists()
    write('temporary-project-inventory-final.json',rows)
inputs=[]
for p in sorted((ROOT/'TestResults/combat-c1').glob('inputs-*-exact.json')):
    data=json.loads(p.read_text()); inputs.append(dict(file=p.name,sha256=sha(p),expected=data['expected_count'],actual=data['actual_count'],failures=data['failures']))
write('input-checkpoints.json',inputs)
write('source-identity.json',dict(branch=subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip(),
    implementation_commit=subprocess.check_output(['git','log','-1','--format=%H','--','Assets','Packages','ProjectSettings','ContentAuthoring'],cwd=ROOT,text=True).strip(),baseline='22e7ce1b08ae17391bfd3542a45821a46f02bbe1',
    unity_input_trees={p:subprocess.check_output(['git','rev-parse','HEAD:'+p],cwd=ROOT,text=True).strip() for p in ('Assets','Packages','ProjectSettings','ContentAuthoring')},
    b1_fixture_sha256=sha(ROOT/'Assets/_Project/Tests/EditMode/combat-c0-b1-golden.zip')))
screen_root=ROOT/'Temp/ui-composition-combat-c0-diagnostics-validation-r2/TestResults/phase7a4-screenshots'
screens=sorted(screen_root.glob('composition-combat-c1*.png'))
if not screens: screens=sorted((ROOT/'Temp/ui-composition-combat-c0-access-validation/TestResults/phase7a4-screenshots').glob('composition-combat-c1*.png'))
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
