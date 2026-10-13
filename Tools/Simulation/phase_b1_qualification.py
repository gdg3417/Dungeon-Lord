"""B1 isolated input/owner preservation and retained qualification evidence; no cleanup."""
import argparse, json, re, subprocess, sys, zipfile, xml.etree.ElementTree as ET
sys.dont_write_bytecode = True
import phase_a_qualification as a

a.q.NAMESPACE = 'living-dungeon-phase-b1-validation.json'
PREFIX = 'phase-b1-'
EVIDENCE = a.ROOT/'Docs/testing/evidence/living-dungeon-phase-b1'
FIXTURE_SHA = '53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198'

def audit():
    files = a.audit()
    savedir = a.Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord')
    for p in savedir.iterdir():
        if p.is_file() and p.name not in ('Player.log', 'Player-prev.log', 'TestResults.xml') and not p.name.startswith(a.q.NAMESPACE):
            files[str(p)] = dict(bytes=p.stat().st_size, sha256=a.sha(p))
    builds = a.PROJECT/'Builds/Development'
    for directory in builds.iterdir():
        if directory.name == 'Windows-living-dungeon-phase-b1': continue
        for p in directory.rglob('*'):
            if p.is_file(): files[str(p)] = dict(bytes=p.stat().st_size, sha256=a.sha(p))
    return {p:v for p,v in files.items() if '/Windows-living-dungeon-phase-b1/' not in p.replace('\\','/')}

def checkpoint(name, prepare):
    result = a.q.compare(a.ROOT, a.NAME)
    a.write(a.OUT/(PREFIX+name+'-before-inputs.json'), result)
    if result['failures']:
        with zipfile.ZipFile(a.OUT/(PREFIX+name+'-disposable-differences.zip'), 'w', zipfile.ZIP_DEFLATED) as z:
            for row in result['files']:
                p = a.PROJECT/row['path']
                if not row['equal'] and p.is_file(): z.write(p, row['path'])
    if prepare: result = a.q.prepare(a.ROOT, a.NAME)
    a.write(a.OUT/(PREFIX+name+'-inputs.json'), result)
    if result['failures']: raise ValueError(result['failures'])
    print(name, result['expected_count'], 'exact isolated inputs')

def restore_test_saves():
    # Existing PlayMode fixtures use their own disposable filenames. Preserve prior fixture bytes.
    savedir = a.Path('C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord')
    backup = a.ROOT/'Temp/phase-b1-preserved-preexisting-saves'
    a.q.no_redirect(savedir); a.q.no_redirect(backup)
    before = json.loads((a.OUT/(PREFIX+'expanded-owner-before.json')).read_text()); now = audit()
    changed = [a.Path(k) for k,v in before.items() if now.get(k) != v]
    for p in changed:
        allowed = ((p.name.startswith('bootstrap_overlay_') and p.name.endswith('_test.json')) or
            (p.name.startswith('run_sim_test_') and p.name.endswith('.json')) or p.name=='mvp_canonical_journey_test.json')
        if p.parent.resolve() != savedir.resolve() or not allowed:
            raise ValueError('Protected owner/player change; refusing automatic restore: '+str(p))
        a.q.no_redirect(p)
        original=backup/p.name
        if a.sha(original) != before[str(p)]['sha256']: raise ValueError('Backup mismatch: '+str(original))
    with zipfile.ZipFile(a.OUT/(PREFIX+'changed-disposable-test-saves.zip'),'w',zipfile.ZIP_DEFLATED) as z:
        for p in changed:
            if p.is_file(): z.write(p,p.name)
    for p in changed: p.write_bytes((backup/p.name).read_bytes())
    after=audit(); assert all(after.get(k)==v for k,v in before.items())
    a.write(a.OUT/(PREFIX+'restored-test-saves.json'),dict(restored=[p.name for p in changed],
        original_owner_files_unchanged=True,primary_save_restored=False,exact_byte_preservation=True))
    print('Restored',len(changed),'preexisting disposable fixture files; no owner primary restore')

def collect():
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    summaries=[]
    with zipfile.ZipFile(EVIDENCE/'qualification-reports.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(a.OUT.glob(PREFIX+'*.xml')):
            run=ET.parse(p).getroot(); z.write(p,p.name)
            summaries.append(dict(file=p.name, sha256=a.sha(p), attributes=run.attrib,
                failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace')) for c in run.iter('test-case') if c.get('result')=='Failed'],
                skips=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in run.iter('test-case') if c.get('result') in ('Skipped','Inconclusive')],
                measurements=[dict(name=c.get('fullname'),output=c.findtext('output')) for c in run.iter('test-case') if c.find('output') is not None and 'LivingDungeonPhaseB1' in c.get('fullname','')]))
    a.write(EVIDENCE/'report-summary.json', summaries)
    comparisons=[]
    with zipfile.ZipFile(a.EVIDENCE/'review-correction/qualification-reports.zip') as old:
        def cases(run):
            rows={}
            for c in run.iter('test-case'): rows.setdefault(c.get('fullname'),[]).append(c.get('result'))
            return {k:sorted(v) for k,v in rows.items()}
        for mode in ('editmode','playmode'):
            before=cases(ET.fromstring(old.read('phase-a-review-full-'+mode+'.xml')))
            run=ET.parse(a.OUT/(PREFIX+'full-'+mode+'.xml')).getroot()
            assert run.get('failed')=='0' and int(run.get('passed'))>0
            after=cases(run)
            row=dict(mode=mode, removed=sorted(before.keys()-after.keys()), added=sorted(after.keys()-before.keys()),
                changed=[k for k in sorted(before.keys()&after.keys()) if before[k]!=after[k]])
            assert not row['removed'] and not row['changed'],row
            assert all('LivingDungeonPhaseB1' in name for name in row['added']),row
            comparisons.append(row)
    a.write(EVIDENCE/'test-outcome-comparison.json', comparisons)
    before=json.loads((a.OUT/(PREFIX+'owner-before.json')).read_text()); current=audit()
    assert all(current.get(k)==v for k,v in before.items()), 'Owner/prior player changed'
    expanded=json.loads((a.OUT/(PREFIX+'expanded-owner-before.json')).read_text())
    assert all(current.get(k)==v for k,v in expanded.items()), 'Expanded save preservation changed'
    a.write(EVIDENCE/'owner-preservation.json',dict(unchanged=True,files=before))
    a.write(EVIDENCE/'expanded-save-preservation.json',dict(unchanged=True,total_verified_files=len(expanded),
        original_audit='owner-preservation.json',additional_files={k:v for k,v in expanded.items() if k not in before},
        test_fixture_restoration=json.loads((a.OUT/(PREFIX+'restored-test-saves.json')).read_text())))
    assert a.sha(a.ROOT/'Assets/_Project/Tests/EditMode/phase-a-baseline.txt')==FIXTURE_SHA
    a.write(EVIDENCE/'baseline.json',dict(cases=38,fixture_sha256=FIXTURE_SHA,regenerated=False))
    with zipfile.ZipFile(EVIDENCE/'input-checkpoints.zip','w',zipfile.ZIP_DEFLATED) as z:
        for pattern in (PREFIX+'*-inputs.json',PREFIX+'*-disposable-differences.zip'):
            for p in sorted(a.OUT.glob(pattern)): z.write(p,p.name)
    logs=[]
    with zipfile.ZipFile(EVIDENCE/'unity-logs-redacted.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in sorted((a.ROOT/'Temp').glob(PREFIX+'*.log')):
            clean=re.sub(r'(?i)(access token |Bearer\s+)\S+',r'\1[REDACTED]',p.read_text(encoding='utf-8-sig',errors='replace'))
            clean=re.sub(r'(?i)([-/][A-Za-z_-]*(?:token|password|serial)(?:\s+|=))(?:(?:"[^"]*")|(?:\S+))',r'\1[REDACTED]',clean)
            z.writestr(p.name,clean)
            logs.append(dict(file=p.name,compiler_errors=sorted(set(re.findall(r'.*error CS[^\r\n]*',clean))),compiler_warnings=sorted(set(re.findall(r'.*warning CS[^\r\n]*',clean)))))
    a.write(EVIDENCE/'log-index.json',logs)
    player=a.PROJECT/'Builds/Development/Windows-living-dungeon-phase-b1'
    a.write(EVIDENCE/'windows-build.json',dict(report=json.loads((player/'build-report.json').read_text(encoding='utf-8-sig')),
        files=[dict(path=p.relative_to(player).as_posix(),bytes=p.stat().st_size,sha256=a.sha(p)) for p in sorted(player.rglob('*')) if p.is_file()]))
    run=ET.parse(a.OUT/(PREFIX+'full-editmode.xml')).getroot()
    gates=[c for c in run.iter('test-case') if 'ProductionSpatialContentBuildGate' in c.get('fullname','')]
    a.write(EVIDENCE/'production-gates.json',dict(total=len(gates),passed=sum(c.get('result')=='Passed' for c in gates)))
    names=set(subprocess.check_output(['git','diff','--name-only',
        'eeaf9d1516c2ffa5d6600238a3e46fcb3787ecc8'],cwd=a.ROOT).decode().splitlines())
    names.update(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=a.ROOT).decode().splitlines())
    names.add('Docs/testing/evidence/living-dungeon-phase-b1/changed-files.json')
    def purpose(name):
        if name.endswith('.meta'): return 'Stable Unity import identity for the paired new script'
        if name.endswith('RunCheckpoint.cs'): return 'Immutable format-1 encoding, independent pins, bounded replay/evidence validation and failure results'
        if name.endswith('RunStartCapture.cs'): return 'Complete retained typed inputs and fresh reconstruction with canonical null-aware recapture'
        if name.endswith('ActiveFloorRunSnapshot.cs'): return 'Explicit retained snapshot wire values and pure connection announcements'
        if name.endswith('RunExecution.cs'): return 'Pre-effect due-time checks, bounded progression, checkpoint export and accepted-time/terminal lifecycle'
        if name.endswith('RunSimulationService.Execution.cs'): return 'Start input capture and checkpoint restoration through the same engine'
        if '/RunSimulationService' in name: return 'Announce existing room/interaction/branch/floor/terminal boundaries before their resolver effects'
        if 'DungeonVisualBuildQualification' in name: return 'Preserving Windows B1 build output slot'
        if 'EditModeFixtures' in name: return 'Register B1 tests in the repository Editor runner'
        if '/Tests/' in name: return 'Fresh-instance restoration, deadline/equivalence/failure, frame and workload qualification'
        if name.startswith('Tools/'): return 'Strict isolated-source and owner/fixture preservation; retained result/build evidence'
        if 'execution-plan' in name: return 'Actual merged Phase A/B1 status and deferred B2/B3/C dependency sequencing'
        if 'implementation-contract' in name: return 'Pre-implementation choice, exact format/semantics/bounds and future canonical ownership'
        return 'Actual B1 qualification, source/preservation, performance, review and publication evidence'
    a.write(EVIDENCE/'changed-files.json',[dict(path=name,purpose=purpose(name)) for name in sorted(names)])
    print('Collected',len(summaries),'reports;',len(before),'protected files unchanged')

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('action',choices=['audit','prepare','compare','restore-test-saves','collect']); p.add_argument('--name',default='checkpoint')
    args=p.parse_args(); a.OUT.mkdir(exist_ok=True)
    if args.action=='audit': a.write(a.OUT/(PREFIX+'owner-before.json'),audit()); print('Captured preservation audit')
    elif args.action=='collect': collect()
    elif args.action=='restore-test-saves': restore_test_saves()
    else: checkpoint(args.name,args.action=='prepare')
