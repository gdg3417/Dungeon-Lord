"""Phase A qualification checkpoints. Reuses the strict existing input checker; no cache/save cleanup."""
from pathlib import Path
import argparse, hashlib, json, sys, subprocess, zipfile, xml.etree.ElementTree as ET, re

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/Presentation'))
import qualification_inputs as q
q.NAMESPACE = 'living-dungeon-phase-a-validation.json'
NAME = 'ui-composition-floorhud-validation'
PROJECT = ROOT/'Temp'/NAME
OUT = ROOT/'TestResults'
EVIDENCE = ROOT/'Docs/testing/evidence/living-dungeon-phase-a'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')
def audit():
    savedir=Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord')
    files=[p for p in savedir.iterdir() if p.is_file() and any(p.name.startswith(prefix) for prefix in
        ('save_primary.json','phase7-ui-composition-validation-01a11d08.json'))]
    files += [ROOT/'ProjectSettings/ProjectSettings.asset', ROOT/'ProjectSettings/ProjectVersion.txt']
    builds=PROJECT/'Builds/Development'
    files += [p for d in builds.iterdir() if d.is_dir() and d.name!='Windows-living-dungeon-phase-a'
        for p in d.rglob('*') if p.is_file()]
    return {str(p):dict(bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(files)}
def checkpoint(name, prepare=False):
    result=q.compare(ROOT,NAME)
    write(OUT/('phase-a-'+name+'-before-inputs.json'),result)
    if result['failures']:
        # Archive all disposable differences before refreshing only the four input trees.
        with zipfile.ZipFile(OUT/('phase-a-'+name+'-disposable-differences.zip'),'w',zipfile.ZIP_DEFLATED) as z:
            for row in result['files']:
                path=PROJECT/row['path']
                if not row['equal'] and path.is_file(): z.write(path,row['path'])
    if prepare: result=q.prepare(ROOT,NAME)
    write(OUT/('phase-a-'+name+'-inputs.json'),result)
    if result['failures']: raise ValueError(result['failures'])
    print(result['expected_count'],'exact inputs; no mismatches')
def collect():
    EVIDENCE.mkdir(parents=True,exist_ok=True)
    summaries=[]
    with zipfile.ZipFile(EVIDENCE/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(OUT.glob('phase-a-*.xml')):
            run=ET.parse(p).getroot(); z.write(p,p.name)
            summaries.append(dict(file=p.name,sha256=sha(p),attributes=run.attrib,
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace'))
                    for c in run.iter('test-case') if c.get('result')=='Failed'],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message'))
                    for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')],
                measurements=[dict(name=c.get('fullname'),output=c.findtext('output')) for c in run.iter('test-case')
                    if c.find('output') is not None and 'SmallWorkBudget' in c.get('fullname','')]))
    write(EVIDENCE/'report-summary.json',summaries)
    def cases(run):
        result={}
        for c in run.iter('test-case'): result.setdefault(c.get('fullname'),[]).append(c.get('result'))
        return {name:sorted(values) for name,values in result.items()}
    comparison=[]
    previous=ROOT/'Docs/testing/evidence/phase7-graphical-room-content/content-multiplicity/qualification-reports.zip'
    with zipfile.ZipFile(previous) as archive:
        for mode in ('editmode','playmode'):
            old=cases(ET.fromstring(archive.read('content-multiplicity-full-'+mode+'.xml')))
            current=ET.parse(OUT/('phase-a-final-full-'+mode+'.xml')).getroot()
            assert current.get('failed')=='0' and int(current.get('passed'))>0,'Incomplete/failing final suite'
            new=cases(current)
            row=dict(mode=mode,removed=sorted(old.keys()-new.keys()),added=sorted(new.keys()-old.keys()),
                changed=[dict(name=n,before=old[n],after=new[n]) for n in sorted(old.keys()&new.keys()) if old[n]!=new[n]])
            assert not row['removed'] and not row['changed'],'Unrelated regression test outcomes changed'
            assert all('LivingDungeonPhaseA' in n for n in row['added']),'Unexpected added test'
            comparison.append(row)
    write(EVIDENCE/'test-outcome-comparison.json',comparison)
    gates={}
    run=ET.parse(OUT/'phase-a-final-full-editmode.xml').getroot()
    for token in ('ProductionSpatialContentBuildGate','BuildReadiness'):
        subset=[c for c in run.iter('test-case') if token in c.get('fullname','')]
        gates[token]=dict(total=len(subset),passed=sum(c.get('result')=='Passed' for c in subset),
            failed=sum(c.get('result')=='Failed' for c in subset),skipped=sum(c.get('result')=='Skipped' for c in subset))
    write(EVIDENCE/'production-gates.json',gates)
    write(EVIDENCE/'static-checks.json',json.loads((OUT/'phase-a-static.json').read_text()))
    before=json.loads((OUT/'phase-a-owner-before.json').read_text())
    after=audit(); assert before==after,'Owner settings/saves or historical builds changed'
    write(EVIDENCE/'owner-preservation.json',dict(unchanged=True,files=before))
    logs=[]
    with zipfile.ZipFile(EVIDENCE/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted((ROOT/'Temp').glob('phase-a-*.log')):
            clean=re.sub(r'(?i)(access token |Bearer\s+)\S+',r'\1[REDACTED]',p.read_text(encoding='utf-8-sig',errors='replace'))
            clean=re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',clean)
            z.writestr(p.name,clean)
            logs.append(dict(file=p.name,compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*',clean))),
                compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*',clean)))))
    write(EVIDENCE/'log-index.json',logs)
    with zipfile.ZipFile(EVIDENCE/'input-checkpoints.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(OUT.glob('phase-a-*-inputs.json')): z.write(p,p.name)
        for p in sorted(OUT.glob('phase-a-*-disposable-differences.zip')): z.write(p,p.name)
    player=PROJECT/'Builds/Development/Windows-living-dungeon-phase-a'
    if (player/'build-report.json').exists():
        build=json.loads((player/'build-report.json').read_text(encoding='utf-8-sig'))
        write(EVIDENCE/'windows-build.json',dict(report=build,files=[dict(path=p.relative_to(player).as_posix(),bytes=p.stat().st_size,sha256=sha(p))
            for p in sorted(player.rglob('*')) if p.is_file()]))
    write(EVIDENCE/'baseline.json',dict(commit='a9a9c8e98a0360c1b01224044eb78015200e4a40',cases=38,
        fixture_sha256=sha(ROOT/'Assets/_Project/Tests/EditMode/phase-a-baseline.txt')))
    names=set(subprocess.check_output(['git','diff','--name-only'],cwd=ROOT).decode().splitlines())
    names.update(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=ROOT).decode().splitlines())
    names.add('Docs/testing/evidence/living-dungeon-phase-a/changed-files.json')
    rows=[]
    for name in sorted(names):
        if name.endswith('.meta'): reason='Stable Unity import identity for the paired new asset'
        elif name=='.gitattributes': reason='Preserve frozen structured fixture LF text on Windows checkouts'
        elif 'RunConnectionSnapshot' in name: reason='Capture validated physical connections and branch socket approach paths'
        elif 'RunExecution' in name or 'RunSimulationService.Execution' in name: reason='Opaque authoritative in-memory continuation, timing, bounded advancement and inspection'
        elif 'RunSimulationService' in name: reason='Suspend existing gameplay loops and route completed callers through explicit draining'
        elif 'ActiveFloorRunSnapshot' in name: reason='Retain immutable connection geometry and derived work ceiling'
        elif 'RunHeatBoundary' in name: reason='Pure continuous Heat boundary/stage application with representable ceilings'
        elif 'CanonicalOfflinePassiveMana' in name: reason='Reuse the canonical calculation for immutable historical queries'
        elif 'DungeonVisualBuildQualification' in name: reason='New Windows build output slot preserves historical players'
        elif 'Tests/' in name and 'Editor/' not in name: reason='Frozen differential fixtures and incremental, boundary, frame and integration qualification'
        elif 'EditModeFixtures' in name: reason='Register new runtime fixtures in the repository Editor runner'
        elif 'Tools/' in name: reason='Strict source comparison and preserved qualification evidence collection'
        elif 'implementation-contract' in name: reason='Pre-refactor contract, implemented API and deferred ownership requirements'
        elif 'execution-plan' in name: reason='Record only implemented/qualified Phase A scope and pending review'
        else: reason='Phase A implementation, validation and preservation evidence'
        rows.append(dict(path=name,reason=reason))
    write(EVIDENCE/'changed-files.json',rows)
    print('Collected',len(summaries),'reports; owner/historical artifact audit unchanged')

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('action',choices=['prepare','compare','audit','collect']); parser.add_argument('--name',default='checkpoint')
    args=parser.parse_args(); OUT.mkdir(exist_ok=True)
    if args.action=='audit': write(OUT/'phase-a-owner-before.json',audit()); print('Owner and historical artifact audit captured')
    elif args.action=='collect': collect()
    else: checkpoint(args.name,args.action=='prepare')
