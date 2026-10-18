"""Read verified attempts and create new evidence; never rewrite prior reports."""
from pathlib import Path
import json, subprocess, sys, zipfile, xml.etree.ElementTree as ET
import qualify_pr242_usability as campaign
q = campaign.gate
ROOT, OUT = q.ROOT, q.OUT
DEST = ROOT / 'Docs/testing/evidence/pr242-usability'
START = '46068bdcaf1d55e8fb8ff2a5d4c044f7f34266b9'
QUALIFIED = 'b5502291e9da3d229bbcc3d152f8bd56954d84eb'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()

def collect(required):
    if len(required) != 4: raise ValueError('Specify final focused, EditMode, PlayMode and build attempt')
    for name in ['qualification.json', 'reports.zip', 'screenshots.zip', 'changed-files.md']:
        q.unused(DEST/name)
    baseline = q.read(OUT/'baseline.json')
    q.verify(baseline['protected']); q.verify_authored_scope()
    attempts = []
    for folder in sorted(OUT.iterdir()):
        if not folder.is_dir() or not (folder/'prepared.json').is_file(): continue
        if not (folder/'restored.json').is_file(): raise ValueError('Unrestored attempt: '+folder.name)
        q.verify(q.read(folder/'artifact-index.json'))
        plan = q.read(folder/'prepared.json')
        item = dict(attempt=folder.name, stage=plan['stage'], sourceCommit=plan['commit'],
                    command=q.read(folder/'command.json'), restored=q.read(folder/'restored.json'),
                    logSha256=q.sha(folder/'editor.log'))
        if (folder/'results.xml').exists():
            run = ET.parse(folder/'results.xml').getroot()
            item['tests'] = dict(run.attrib)
            item['failures'] = [dict(name=c.get('fullname'), message=c.findtext('failure/message'), stack=c.findtext('failure/stack-trace'))
                                for c in run.iter('test-case') if c.find('failure') is not None]
            item['diagnostics'] = [dict(name=c.get('name'), output=c.findtext('output')) for c in run.iter('test-case')
                                   if c.get('name') in ['OwnerUsabilityConstructionAndContentReachability',
                                      'TransactionalRoomConstructionLayoutsLocalizationAndResume',
                                      'RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated']]
        attempts.append(item)
    selected = [next(a for a in attempts if a['attempt'] == name) for name in required]
    for a in selected:
        if a['command']['exitCode'] != 0: raise ValueError('Required run failed: '+a['attempt'])
        if 'tests' in a and (a['tests']['failed'] != '0' or int(a['tests']['passed']) == 0):
            raise ValueError('Missing passing verdict: '+a['attempt'])
        if git('diff', '--name-only', selected[0]['sourceCommit'], a['sourceCommit'], '--', *q.inputs.INPUT_ROOTS):
            raise ValueError('Final runs did not qualify identical committed Unity inputs')
    if git('diff', '--name-only', selected[0]['sourceCommit'], 'HEAD', '--', *q.inputs.INPUT_ROOTS):
        raise ValueError('Current Unity inputs differ from final tested inputs')
    build = q.destinations(required[3])['build']
    report = q.read(build/'build-report.json')
    if report['buildResult'] != 'Succeeded' or report['errorCount'] != 0 or not report['developmentBuild']:
        raise ValueError('Development build failed')
    if report['includedScenes'] != ['Assets/_Project/Scenes/Bootstrap.unity']:
        raise ValueError('Unexpected player scene list')
    art = (build/'packed-dungeon-art.txt').read_text(encoding='utf-8-sig').splitlines()
    if len(art) != 52: raise ValueError('Required production artwork missing')
    data = build/'Dungeon Lord_Data'
    assembly = (data/'Managed/Assembly-CSharp.dll').read_bytes()
    for symbol in [b'FocusConstructionArea', b'interactionFocus']:
        if symbol not in assembly: raise ValueError('Corrected UI missing from player')
    for symbol in [b'SyntheticForegroundQualification', b'SettleSyntheticTerminal', b'LivingDungeonTerminalSettlement']:
        if symbol in assembly: raise ValueError('Qualification capability present in player')
    identities = q.destinations(required[3])
    if identities['namespace'].encode() not in (data/'sharedassets0.assets').read_bytes(): raise ValueError('Save namespace not packed')
    if identities['product'].encode() not in (data/'globalgamemanagers').read_bytes(): raise ValueError('Product identity not packed')
    old = q.read(ROOT/'TestResults/living-dungeon-b3a-20261017/baseline.json'); q.verify(old['protected'])
    historical = q.read(ROOT/'TestResults/living-dungeon-b3a-20261017/historical-evidence-before.json')
    missing = [r for r in historical if not Path(r['path']).is_file() or q.sha(Path(r['path'])) != r['sha256']]
    accepted = q.read(ROOT/'TestResults/living-dungeon-b3a-20261017/missing-original-screenshots.json')
    if {(r['path'],r['sha256']) for r in missing} != {(r['path'],r['sha256']) for r in accepted}:
        raise ValueError('Historical mismatches exceed accepted incident')
    protected = ['Assets/_Project/Scripts/Gameplay', 'Assets/_Project/Scripts/Services', 'Assets/_Project/Scripts/Core',
                 'Assets/_Project/Tests/EditMode/LivingDungeonPhaseB3ATests.cs',
                 'Assets/_Project/Tests/PlayMode/LivingDungeonPhaseB3APlayModeTests.cs',
                 'Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonPhaseB3AEditModeFixtures.cs']
    if git('diff','--name-only',QUALIFIED,'HEAD','--',*protected): raise ValueError('B3A authority changed')
    with zipfile.ZipFile(DEST/'screenshots.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
        for name in ['before-01', required[0]]:
            for path in sorted((OUT/name/'screens').rglob('*.png')):
                archive.write(path, name+'/'+path.relative_to(OUT/name/'screens').as_posix())
    with zipfile.ZipFile(DEST/'reports.zip', 'x', zipfile.ZIP_DEFLATED) as archive:
        archive.write(OUT/'baseline.json','baseline.json')
        archive.write(ROOT/'Docs/testing/evidence/pr242-graphics/manual-uat.md','manual-uat-before-owner-failure.md')
        archive.write(Path(__file__), 'collect_pr242_usability.py')
        for a in attempts:
            folder = OUT/a['attempt']
            for name in ['results.xml','prepared.json','command.json','restored.json','artifact-index.json']:
                path=folder/name
                if path.is_file(): archive.write(path,folder.name+'/'+name)
        archive.writestr('log-hashes.json',json.dumps([{k:a[k] for k in ['attempt','logSha256']} for a in attempts],indent=2))
    result = dict(automationPassed=True, ownerUsabilityApproved=False, independentReviewComplete=False, draft=True,
                  startCommit=START, qualifiedUISourceCommit=selected[0]['sourceCommit'], qualifiedB3ACommit=QUALIFIED,
                  priorManualUatSha256=q.sha(ROOT/'Docs/testing/evidence/pr242-graphics/manual-uat.md'),
                  attempts=attempts, finalAttempts=required,
                  build=dict(report=report, artwork=len(art), isolatedProduct=identities['product'], isolatedSave=identities['namespace'],
                             path=str(build), files=sum(p.is_file() for p in build.rglob('*')),
                             bytes=sum(p.stat().st_size for p in build.rglob('*') if p.is_file()),
                             playerSha256=q.sha(build/'Dungeon Lord.exe'), assemblySha256=q.sha(data/'Managed/Assembly-CSharp.dll')),
                  preservation=dict(freshProtected=len(baseline['protected']), freshUnchanged=len(baseline['protected']),
                                    originalSavePlayerFiles=len(old['protected']), originalSavePlayerUnchanged=True,
                                    historicalTotal=len(historical), historicalUnchanged=len(historical)-len(missing),
                                    acceptedMissingOriginals=missing, B3AProtectedSourceDiffEmpty=True))
    q.write(DEST/'qualification.json', result)
    changed = sorted(set(git('diff','--name-only',START,'HEAD').splitlines()+[
        'Docs/testing/evidence/pr242-usability/'+n for n in ['qualification.json','qualification.md','reports.zip','screenshots.zip','changed-files.md']]+[
        'Docs/testing/evidence/pr242-graphics/manual-uat.md','Tools/Presentation/collect_pr242_usability.py',
        'Tools/Presentation/prepare_pr242_usability_owner.py']))
    with (DEST/'changed-files.md').open('x',encoding='utf-8') as stream:
        stream.write('# Exact owner-usability correction files\n\n'+'\n'.join('- `'+n+'`' for n in changed)+'\n')
    print(json.dumps(dict(automationPassed=True, finalTests=[a.get('tests') for a in selected], build=report, preservation=result['preservation'])))

if __name__ == '__main__': collect(sys.argv[1:])
