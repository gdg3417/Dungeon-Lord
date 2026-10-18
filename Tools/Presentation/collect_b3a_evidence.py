"""Collect actual B3A verdicts, preservation and package evidence; never infer success."""
from pathlib import Path
import hashlib, json, re, subprocess, zipfile, xml.etree.ElementTree as ET
import qualify_b3a as q

ROOT, OUT = q.ROOT, q.OUT
DEST = ROOT/'Docs/testing/evidence/living-dungeon-b3a'
BUILD = ROOT/'Builds/Development/Windows-living-dungeon-b3a-20261017'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(name, value): (DEST/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
def collect():
    if (DEST/'qualification-reports.zip').exists(): raise ValueError('Preserve existing qualification evidence; choose a new evidence version')
    DEST.mkdir(parents=True,exist_ok=True)
    reports=[]
    for path in sorted(OUT.glob('*.xml')):
        run=ET.parse(path).getroot()
        cases=list(run.iter('test-case'))
        groups={}
        for label,needle in [('B3A','LivingDungeonPhaseB3A'),('B1','LivingDungeonPhaseB1'),('B2','LivingDungeonPhaseB2'),
                             ('PhaseA','LivingDungeonPhaseA'),('C2','CombatC2'),('Heat','Heat'),('Knowledge','Knowledge'),
                             ('Manual','PhaseFiveBBranchIntegration'),('Canonical','DetachedCanonical'),
                             ('Migration','Migration'),('ProductionGates','ProductionSpatialContentBuildGate')]:
            subset=[c for c in cases if needle in c.get('fullname','')]
            if subset: groups[label]={state:sum(c.get('result')==state for c in subset) for state in ['Passed','Failed','Skipped','Inconclusive']}
        reports.append(dict(file=path.name,sha256=sha(path),verdict=run.attrib,groups=groups,
            failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message'),stack=c.findtext('failure/stack-trace')) for c in cases if c.get('result')=='Failed'],
            skipped=[dict(name=c.get('fullname'),reason=c.findtext('reason/message')) for c in cases if c.get('result') in ['Skipped','Inconclusive']],
            measurements=[dict(name=c.get('fullname'),output=c.findtext('output')) for c in cases if 'B3A ' in (c.findtext('output') or '')]))
    commands=[json.loads(p.read_text(encoding='utf-8-sig')) for p in sorted(OUT.glob('*-command.json'))]
    report=json.loads((BUILD/'build-report.json').read_text(encoding='utf-8-sig'))
    packed=(BUILD/'packed-dungeon-art.txt').read_text(encoding='utf-8-sig').splitlines()
    if report['buildResult']!='Succeeded' or report['errorCount']!=0 or not report['developmentBuild']: raise ValueError('Build not qualified')
    if len(packed)!=52: raise ValueError('Required production artwork missing')
    files=sorted(p for p in BUILD.rglob('*') if p.is_file())
    assemblies=list((BUILD/'Dungeon Lord_Data/Managed').glob('Assembly-CSharp.dll'))
    if len(assemblies)!=1: raise ValueError('Player assembly inspection unavailable')
    player_bytes=assemblies[0].read_bytes()
    excluded=[b'SyntheticForegroundQualification',b'SettleSyntheticTerminal',b'LivingDungeonTerminalSettlement']
    if any(term in player_bytes for term in excluded): raise ValueError('Qualification capability leaked into player')
    namespace_packed=q.NAMESPACE.encode() in (BUILD/'Dungeon Lord_Data/sharedassets0.assets').read_bytes()
    if not namespace_packed: raise ValueError('Isolated validation save namespace was not packaged')
    required={}
    for stage in ['focused','full-edit','integration-play']:
        candidates=[r for r in reports if r['file'].endswith('-'+stage+'.xml')]
        if not candidates: raise ValueError('Required suite missing: '+stage)
        item=max(candidates,key=lambda r:r['file'])
        required[stage]=dict(file=item['file'],passed=int(item['verdict']['passed']),failed=int(item['verdict']['failed']),
                             success=item['verdict'].get('failed')=='0' and int(item['verdict']['passed'])>0)
    if not any(c['arguments'][0]=='build' and c['exitCode']==0 for c in commands): raise ValueError('No successful build command')
    preservation={}
    for name in ['preservation-after.json','historical-evidence-after.json']:
        rows=json.loads((OUT/name).read_text())
        if not rows: raise ValueError('Preservation audit unavailable: '+name)
        preservation[name]=dict(total=len(rows),unchanged=sum(r['equal'] for r in rows),
                                failures=[r for r in rows if not r['equal']])
    complete=all(r['success'] for r in required.values()) and all(not r['failures'] for r in preservation.values())
    write('qualification-status.json',dict(complete=complete,requiredSuites=required,preservation=preservation,
          note='Failing tests and preservation mismatches are retained as unresolved; this report does not waive them.'))
    write('report-summary.json',reports); write('commands.json',commands)
    write('windows-package.json',dict(report=report,files=len(files),bytes=sum(p.stat().st_size for p in files),
        requiredArtwork=len(packed),qualificationCapabilityAbsent=True,namespace=q.NAMESPACE,namespacePacked=namespace_packed,
        inventory=[dict(path=p.relative_to(BUILD).as_posix(),bytes=p.stat().st_size,sha256=sha(p)) for p in files]))
    write('source-applicability.json',dict(finalCommitAtCollection=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
        unityInputs=[dict(path=n,sha256=sha(ROOT/n)) for n in q.inputs.source_inventory(ROOT)],
        qualificationSubstitutions=dict(save=q.NAMESPACE,graphicalEvidence='TestResults/living-dungeon-b3a-20261017/outputs/',staticFixtures='b3a_20261017'),
        changesFromBase=subprocess.check_output(['git','diff','--name-only','e5abe082a8a51a00845dfff5c735fbb3eb9edb8d','HEAD'],cwd=ROOT,text=True).splitlines()))
    with zipfile.ZipFile(DEST/'qualification-reports.zip','x',zipfile.ZIP_DEFLATED) as z:
        for p in sorted(OUT.glob('*')):
            if p.is_file() and p.suffix in ['.xml','.json']: z.write(p,p.name)
        logs=[]
        for p in sorted(OUT.glob('*.log')):
            data=p.read_text(encoding='utf-8-sig',errors='replace')
            diagnostics=[line for line in data.splitlines() if re.search(r'\b(?:error|warning) CS\d+',line)]
            logs.append(dict(file=p.name,sha256=sha(p),bytes=p.stat().st_size,compilerDiagnostics=diagnostics,
                             rawLogRetainedLocally=True))
        z.writestr('log-index-and-compiler-diagnostics.json',json.dumps(logs,indent=2))
    for name in ['build-report.json','build-messages.txt','packed-dungeon-art.txt','packed-source-assets.txt']:
        (DEST/name).write_bytes((BUILD/name).read_bytes())
    print(json.dumps(dict(complete=complete,reports=len(reports),build=report['buildResult'],files=len(files),artwork=len(packed))))
if __name__=='__main__': collect()
