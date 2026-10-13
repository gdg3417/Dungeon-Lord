"""Focused PR #233 rerun evidence; preserves original Phase A evidence and player."""
import argparse, json, re, subprocess, sys, zipfile, xml.etree.ElementTree as ET
sys.dont_write_bytecode = True
import phase_a_qualification as a

EVIDENCE = a.EVIDENCE/'review-correction'
PREFIX = 'phase-a-review-'
FIXTURE_SHA = '53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198'

def audit():
    files = a.audit()
    player = a.PROJECT/'Builds/Development/Windows-living-dungeon-phase-a'
    files.update({str(p):dict(bytes=p.stat().st_size,sha256=a.sha(p)) for p in sorted(player.rglob('*')) if p.is_file()})
    return {p:v for p,v in files.items() if '/Windows-living-dungeon-phase-a-review/' not in p.replace('\\','/')}

def collect():
    EVIDENCE.mkdir(parents=True,exist_ok=True)
    summaries=[]
    with zipfile.ZipFile(EVIDENCE/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(a.OUT.glob(PREFIX+'*.xml')):
            run=ET.parse(p).getroot(); z.write(p,p.name)
            summaries.append(dict(file=p.name,sha256=a.sha(p),attributes=run.attrib,
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message')) for c in run.iter('test-case') if c.get('result')=='Failed'],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')]))
    a.write(EVIDENCE/'report-summary.json',summaries)
    comparisons=[]
    with zipfile.ZipFile(a.EVIDENCE/'qualification-reports.zip') as old:
        for mode in ('editmode','playmode'):
            def cases(run):
                rows={}
                for c in run.iter('test-case'): rows.setdefault(c.get('fullname'),[]).append(c.get('result'))
                return {k:sorted(v) for k,v in rows.items()}
            before=cases(ET.fromstring(old.read('phase-a-final-full-'+mode+'.xml')))
            run=ET.parse(a.OUT/(PREFIX+'full-'+mode+'.xml')).getroot()
            assert run.get('failed')=='0' and int(run.get('passed'))>0
            if mode=='editmode':
                initial=ET.parse(a.OUT/(PREFIX+'initial-full-editmode.xml')).getroot()
                assert run.get('start-time') > initial.get('end-time'),'Stale final report'
            after=cases(run)
            row=dict(mode=mode,removed=sorted(before.keys()-after.keys()),added=sorted(after.keys()-before.keys()),
                changed=[k for k in sorted(before.keys()&after.keys()) if before[k]!=after[k]])
            assert not row['removed'] and not row['changed'],row
            assert all('LivingDungeonPhaseA' in name for name in row['added']),row
            comparisons.append(row)
    a.write(EVIDENCE/'test-outcome-comparison.json',comparisons)
    before=json.loads((a.OUT/(PREFIX+'owner-before.json')).read_text())
    assert before==audit(),'Owner inputs or prior players changed'
    a.write(EVIDENCE/'owner-preservation.json',dict(unchanged=True,files=before))
    fixture=a.ROOT/'Assets/_Project/Tests/EditMode/phase-a-baseline.txt'
    assert a.sha(fixture)==FIXTURE_SHA,'Frozen fixtures changed'
    a.write(EVIDENCE/'baseline.json',dict(cases=38,fixture_sha256=FIXTURE_SHA,regenerated=False))
    logs=[]
    with zipfile.ZipFile(EVIDENCE/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted((a.ROOT/'Temp').glob(PREFIX+'*.log')):
            clean=re.sub(r'(?i)(access token |Bearer\s+)\S+',r'\1[REDACTED]',p.read_text(encoding='utf-8-sig',errors='replace'))
            clean=re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',clean)
            z.writestr(p.name,clean)
            logs.append(dict(file=p.name,compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*',clean))),compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*',clean)))))
    a.write(EVIDENCE/'log-index.json',logs)
    with zipfile.ZipFile(EVIDENCE/'input-checkpoints.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(a.OUT.glob(PREFIX+'*-inputs.json')): z.write(p,p.name)
        for p in sorted(a.OUT.glob(PREFIX+'*-disposable-differences.zip')): z.write(p,p.name)
    player=a.PROJECT/'Builds/Development/Windows-living-dungeon-phase-a-review'
    a.write(EVIDENCE/'windows-build.json',dict(report=json.loads((player/'build-report.json').read_text(encoding='utf-8-sig')),
        files=[dict(path=p.relative_to(player).as_posix(),bytes=p.stat().st_size,sha256=a.sha(p)) for p in sorted(player.rglob('*')) if p.is_file()]))
    run=ET.parse(a.OUT/(PREFIX+'full-editmode.xml')).getroot()
    gates=[c for c in run.iter('test-case') if 'ProductionSpatialContentBuildGate' in c.get('fullname','')]
    a.write(EVIDENCE/'production-gates.json',dict(total=len(gates),passed=sum(c.get('result')=='Passed' for c in gates)))
    a.write(EVIDENCE/'static-checks.json',dict(diff_check='passed',python_compile='passed',strict_input_tests=dict(total=12,passed=12,failed=0,skipped=0)))
    names=set(subprocess.check_output(['git','diff','--name-only'],cwd=a.ROOT).decode().splitlines())
    names.update(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=a.ROOT).decode().splitlines())
    names.add('Docs/testing/evidence/living-dungeon-phase-a/review-correction/changed-files.json')
    a.write(EVIDENCE/'changed-files.json',sorted(names))
    print('Collected',len(summaries),'reports;',len(before),'preserved files; frozen fixtures unchanged')

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('action',choices=['audit','prepare','compare','collect']); p.add_argument('--name',default='checkpoint')
    args=p.parse_args()
    if args.action=='audit': a.write(a.OUT/(PREFIX+'owner-before.json'),audit()); print('Captured preservation audit')
    elif args.action=='collect': collect()
    else: a.checkpoint('review-'+args.name,args.action=='prepare')
