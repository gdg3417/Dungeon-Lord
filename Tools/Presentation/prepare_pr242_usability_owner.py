"""Owner handoff preservation gate. No automated test/build execution or save authoring."""
from pathlib import Path
import sys
import qualify_pr242_usability as campaign
q = campaign.gate
q.NAME = 'pr242-usability-owner-20261018'
q.OUT = q.ROOT/'TestResults'/q.NAME
q.__file__ = __file__

def dependencies():
    if q.sha(Path(campaign.__file__)) != '35bd40fa74da4c2ef5d817a9220c749680264163ef82f7ab97e96301313393be':
        raise ValueError('Reviewed usability gate changed')

def ready(attempt):
    # preflight runs before the explicit CreateNew copy of an authority-generated
    # starter artifact. This read-only second gate accepts exactly that one file.
    dest=q.destinations(attempt); folder=dest['folder']
    if (folder/'restored.json').exists(): raise ValueError('Inactive owner attempt')
    plan=q.read(folder/'prepared.json'); baseline=q.read(q.OUT/'baseline.json')
    fixture=q.read(folder/'owner-fixture.json')
    if plan['commit']!=q.git('rev-parse','HEAD') or plan['scriptSha256']!=q.sha(Path(__file__)):
        raise ValueError('Owner prepared source identity changed')
    if plan['baselineSha256']!=q.sha(q.OUT/'baseline.json'): raise ValueError('Owner baseline changed')
    q.exact_inventory([str(q.ROOT/n) for n in q.inventory()],[r['path'] for r in plan['prepared']])
    q.verify(plan['prepared']); q.verify(baseline['protected'])
    if fixture['originalInputsSha256']!=q.sha(folder/'original-inputs.zip'): raise ValueError('Owner backup changed')
    target=dest['save']/dest['namespace']; source=Path(fixture['source'])
    q.inputs.no_redirect(source); q.inputs.no_redirect(target)
    qualified_attempt=fixture['qualifiedAttempt']; q.attempt_path(qualified_attempt)
    qualified=q.ROOT/'TestResults/pr242-usability-20261018'/qualified_attempt
    qualified_plan=q.read(qualified/'prepared.json'); index=q.read(qualified/'artifact-index.json')
    if not (qualified/'restored.json').is_file(): raise ValueError('Starter qualification is not closed')
    q.verify(index)
    expected_source=q.OWNER/('Dungeon Lord PR242 Graphics '+qualified_attempt)/'owner-uat-starter.json'
    if source!=expected_source or not any(r['path']==str(source) and r['sha256']==fixture['sha256'] for r in index):
        raise ValueError('Starter is not the indexed isolated qualification artifact')
    if q.git('diff','--name-only',qualified_plan['commit'],'HEAD','--',*q.inputs.INPUT_ROOTS):
        raise ValueError('Owner Unity sources differ from starter qualification')
    if fixture['destination']!=str(target) or fixture['sha256']!=q.sha(source) or q.sha(target)!=fixture['sha256']:
        raise ValueError('Owner starter copy is not exact')
    if {p for p in dest['save'].rglob('*') if p.is_file()}!={target}:
        raise ValueError('Unexpected owner save files before launch')
    for path in [folder/'editor.log',folder/'screens',folder/'results.xml',folder/'command.json',dest['build']]:
        q.unused(path)
    print('OWNER READY PASS:',attempt,'exact authority-generated starter; source/protection/unused outputs verified')

if __name__=='__main__':
    try:
        dependencies(); mode=sys.argv[1]
        if mode=='init': q.init()
        elif mode=='prepare': q.prepare(sys.argv[2],'focused')
        elif mode=='preflight': q.preflight(sys.argv[2])
        elif mode=='ready': ready(sys.argv[2])
        elif mode=='finish': q.finish(sys.argv[2])
        else: raise ValueError('Unsupported owner operation')
    except (ValueError,OSError,KeyError,q.zipfile.BadZipFile) as error:
        print('Owner handoff rejected:',error,file=sys.stderr); sys.exit(2)
