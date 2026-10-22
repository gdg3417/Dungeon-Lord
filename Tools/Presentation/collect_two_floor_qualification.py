"""Publish compact report/provenance metadata, never raw Unity authentication logs."""
from pathlib import Path
import datetime, hashlib, json, subprocess, sys, xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
out = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root/'TestResults/living-dungeon-two-floor-20261020'
destination = root / 'Docs/testing/evidence/living-dungeon-two-floor/qualification-ledger.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
commands = []
for p in sorted(out.glob('*-command.json')):
    value = json.loads(p.read_text(encoding='utf-8-sig'))
    if not isinstance(value, dict): continue
    row = dict(record=p.relative_to(root).as_posix(), sha256=sha(p))
    for key in ('commit','executable','arguments','startedUtc','finishedUtc','exitCode',
                'gameplayAssemblySha256','packedManagersSha256','buildArtifactManifestSha256',
                'postExitSaveSha256','postExitCompleteSaveBytes','bootstrapReadyElapsedBoundsMs'):
        if key in value: row[key] = value[key]
    if value.get('startedUtc') and value.get('finishedUtc'):
        row['commandWallSeconds'] = (datetime.datetime.fromisoformat(value['finishedUtc']) - datetime.datetime.fromisoformat(value['startedUtc'])).total_seconds()
    xml = p.with_name(p.name.removesuffix('-command.json') + '.xml')
    if xml.exists():
        tree = ET.parse(xml).getroot()
        row['nunit'] = {k: tree.attrib.get(k) for k in ('result','total','passed','failed','skipped','inconclusive','duration')}
        row['nunit']['sha256'] = sha(xml)
        row['nunit']['nonPassingCases'] = [dict(name=c.get('fullname'),result=c.get('result'),
            reason=c.findtext('reason/message') or c.findtext('failure/message'))
            for c in tree.iter('test-case') if c.get('result') != 'Passed']
    log = p.with_name(p.name.removesuffix('-command.json') + '.log')
    if log.exists(): row['logSha256'] = sha(log)
    report = p.with_name(p.name.removesuffix('-command.json') + '.json')
    if report.exists():
        data = json.loads(report.read_text(encoding='utf-8-sig'))
        row['nativeReport'] = {k:data[k] for k in ('Passed','Failure','Phase','Accepted','Closed','NextRunSequence','HistoryCount','Mana','Heat','MarkerVisible','PartySequences','PartyCheckpoints',
            'BlockerSequence','WaitingSequence','BlockedAt','ReleasedAt','BlockerTerminalAt','BlockerTerminalOrdinal',
            'AStartIdentity','BStartIdentity','WaitingObserved','WaitingResumed','ActiveEditCommitted','CompletedRuns') if k in data}
        row['nativeReport']['sha256'] = sha(report)
    commands.append(row)
result = dict(baseline='76f78c5c2d17d3aab916f297aecb5c25cf146d76',
    evidenceCollectedAtSource=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),
    records=commands, limitations=['Each result applies to its recorded commit, not automatically to the final documentation commit.',
    'Filters overlap; counts are not a unique test total. Skips and interrupted/failed attempts remain separate.',
    'NUnit root duration is retained verbatim; domain reload can make it cover only the final report segment. Use commandWallSeconds for the complete CLI invocation.',
    'Raw Unity logs remain local because they can contain ephemeral CLI authentication arguments.'])
result['interruptions'] = [json.loads((out/'38-interruption-disposition.json').read_text())]
result['orchestration'] = [dict(path=p.relative_to(root).as_posix(),sha256=sha(p)) for p in
    (out/'qualification-32.ps1',out/'watch_final_native.py',out/'final-native.ps1',out/'prepare_final_native.py',Path(__file__))]
with destination.open('x', encoding='utf-8') as stream: json.dump(result,stream,indent=2)
print('Recorded',len(commands),'command records at',destination)
