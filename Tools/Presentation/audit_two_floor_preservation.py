"""Run only after Editor and every standalone have exited and isolation is restored."""
from pathlib import Path
import hashlib, json, sys, zipfile, xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
out = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root/'TestResults/living-dungeon-two-floor-20261020'
destination = root/'Docs/testing/evidence/living-dungeon-two-floor/preservation-summary.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def normalized(data): return data.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n',b'\n')
if not (out/'substitutions-restored.json').exists(): raise ValueError('Restore qualification substitutions first')
# The original launcher was interrupted during profile 38. Verify the actual
# exact-source gates, including its isolated replacement, instead of treating
# that stale orchestration record as a successful campaign.
qualified_source = '3040ddc4b87b0f70eac4046c3a7a4c48c5ec12da'
required = ('32-full-edit','32-full-play','32-build','32-wait','33-release',
            '34-finish','35-v1','36-base','37-one-profile','39-two-profile')
for label in required:
    command = json.loads((out/(label+'-command.json')).read_text())
    if command.get('commit') != qualified_source or command.get('exitCode') != 0:
        raise ValueError('Required qualification did not complete: '+label)
    report = out/(label+'.json')
    if label not in ('32-full-edit','32-full-play','32-build') and (not report.exists() or not json.loads(report.read_text()).get('Passed')):
        raise ValueError('Required player qualification failed: '+label)
for label, counts in (('32-full-edit',(2328,0,1)),('32-full-play',(3725,0,10))):
    run = ET.parse(out/(label+'.xml')).getroot()
    if tuple(int(run.get(k)) for k in ('passed','failed','skipped')) != counts:
        raise ValueError('Unexpected full-suite verdict: '+label)
original_rows, eol = [], []
with zipfile.ZipFile(out/'root-inputs-before.zip') as original:
    for name in original.namelist():
        if not (name.startswith(('ProjectSettings/','Packages/')) or name.endswith('.meta')): continue
        path = (root/name).resolve()
        if not path.is_relative_to(root) or not path.is_file(): raise ValueError('Missing/unsafe original input '+name)
        before, current = original.read(name), path.read_bytes()
        if before != current:
            if normalized(before) != normalized(current): raise ValueError('Unexpected semantic input change '+name)
            eol.append((name,current,before))
        original_rows.append(dict(path=name,sha256=hashlib.sha256(before).hexdigest()))
if eol:
    with zipfile.ZipFile(out/'final-eol-preservation.zip','x',zipfile.ZIP_DEFLATED) as retained:
        for name,current,before in eol:
            retained.writestr(name,current); (root/name).write_bytes(before)
for row in original_rows:
    if sha(root/row['path']) != row['sha256']: raise ValueError('Original input was not restored '+row['path'])
seals = []
for name in ('qualified-native-build-files.json','qualified-native-final-build-files.json'):
    source = out/name; data = json.loads(source.read_text()); directory = Path(data['directory'])
    expected = {row['path'] for row in data['files']}
    actual = {p.relative_to(directory).as_posix() for p in directory.rglob('*') if p.is_file()}
    if expected != actual: raise ValueError('Build file set changed '+str(directory))
    if any(sha(directory/row['path']) != row['sha256'] for row in data['files']):
        raise ValueError('Build artifact changed '+str(directory))
    seals.append(dict(record=name,sha256=sha(source),commit=data['commit'],files=len(expected),allUnchanged=True))
protected = json.loads((out/'preservation-after.json').read_text())
history = json.loads((out/'historical-evidence-after.json').read_text())
if any(not r.get('equal') for r in protected+history): raise ValueError('Protected/historical preservation failed')
result = dict(qualifiedSource=qualified_source,requiredGates=required,
    interruptedCampaignRecordPreserved=True,
    originalInputArchiveSha256=sha(out/'root-inputs-before.zip'),
    originalSettingsPackagesAndMetadata=len(original_rows),allOriginalInputsByteEqual=True,
    archivedAndRestoredEolOnly=[name for name,_,_ in eol],
    protectedFiles=len(protected),allProtectedByteEqual=True,historicalEvidenceFiles=len(history),allHistoricalByteEqual=True,
    protectedAuditSha256=sha(out/'preservation-after.json'),historicalAuditSha256=sha(out/'historical-evidence-after.json'),
    buildSeals=seals, limitations=['Original owner/save contents and raw Unity authentication logs are not published.',
    'All canonical post-exit collections occur only after player termination; process guards precede preservation reads.'])
with destination.open('x',encoding='utf-8') as stream: json.dump(result,stream,indent=2)
print(json.dumps(result,indent=2))
