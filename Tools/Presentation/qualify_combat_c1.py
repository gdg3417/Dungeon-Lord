"""C1 qualification using existing projects; no owner-save writes or deletion."""
from pathlib import Path
import json, os, subprocess, sys, zipfile
import qualify_visual_feedback as audit

audit.NAME = 'ui-composition-combat-c0-access-validation'
audit.OUT = audit.ROOT / 'TestResults/combat-c1'
audit.inputs.NAMESPACE = 'combat-c0-access-validation-20261015.json'
OWNER = 'ui-composition-combat-c0-diagnostics-validation-warm'

def inventory():
    evidence = '\n'.join(p.read_text(encoding='utf-8-sig', errors='replace')
                         for p in (audit.ROOT/'Docs/testing/evidence/combat-c0').rglob('*.md'))
    rows = []
    for p in sorted((audit.ROOT/'Temp').iterdir()):
        if not p.is_dir(): continue
        if not (p/'ProjectSettings/ProjectVersion.txt').exists(): continue
        linked = p.is_symlink() or bool(getattr(p.lstat(), 'st_file_attributes', 0) & 1024)
        nested = ['.git'] if (p/'.git').exists() else []
        configs = p/audit.inputs.CONFIG
        namespace = json.loads(configs.read_text(encoding='utf-8-sig'))['save']['fileName'] if configs.exists() else None
        builds = [str(x.relative_to(p)) for x in (p/'Builds').rglob('*.exe')] if (p/'Builds').exists() else []
        status = subprocess.check_output(['git','-C',str(p),'status','--short'],text=True) if nested else None
        rows.append(dict(project=p.name, linked=linked, nested_git=nested, checkout_status=status,
            independent_checkout=bool(nested), namespace=namespace, builds=builds,
            c0_evidence_reference=p.name in evidence,
            disposition='retain protected owner environment' if p.name==OWNER else
                'reuse for C1 automated qualification' if p.name==audit.NAME else 'obsolete candidate; owner approval required',
            dependency='Keep all saves/recovery, prior builds and evidence. Input differences are not proof of uncommitted work.'))
    audit.write('temporary-project-inventory.json', rows)
    for r in rows: print(r['project'], '|', r['disposition'], '| linked:',r['linked'], '| git:',r['nested_git'], '| namespace:',r['namespace'], '| players:',len(r['builds']))
    # Record all protected bytes before any automated process or owner refresh.
    if not (audit.OUT/'preservation-before.json').exists():
        audit.write('preservation-before.json', [dict(path=str(p),sha256=audit.sha(p),fixture=False) for p in audit.protected()])
    audit.inputs.NAMESPACE = 'combat-c0-diagnostics-validation-20261015.json'
    result = audit.inputs.compare(audit.ROOT,OWNER)
    audit.write('owner-inputs-before.json',result)
    print('Owner comparison:', result['expected_count'],'inputs;',len(result['unexpected']),'unexpected;',len(result['failures']),'differences')

def owner_prepare():
    audit.NAME=OWNER
    audit.inputs.NAMESPACE='combat-c0-diagnostics-validation-20261015.json'
    result=audit.inputs.compare(audit.ROOT,audit.NAME)
    if result['unexpected']: raise ValueError('Unexpected owner inputs; preserve and inspect')
    archive=audit.OUT/'owner-inputs-before-refresh.zip'
    with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as saved:
        for r in result['files']:
            path=audit.ROOT/'Temp'/OWNER/r['path']
            if not r['equal'] and path.exists(): saved.write(path,r['path'])
    audit.write('owner-refresh-preflight.json',result)
    audit.write('owner-inputs-refreshed.json',audit.inputs.prepare(audit.ROOT,audit.NAME))
    print('Owner source refreshed with unchanged disposable namespace; no save opened')

if __name__ == '__main__':
    mode=sys.argv[1]
    if mode=='inventory': inventory()
    elif mode=='prepare':
        # Established disposable fixtures may be changed by broad suites; back up only these.
        if (audit.OUT/'preservation-before.json').exists():
            rows=json.loads((audit.OUT/'preservation-before.json').read_text())
            import shutil
            for r in rows:
                p=Path(r['path'])
                if p.parent==audit.SAVE and (p.name.startswith(('bootstrap_overlay_','run_sim_test_')) or p.name=='mvp_canonical_journey_test.json'):
                    r['fixture']=True
                    dest=audit.OUT/'preexisting-test-fixtures'/p.name
                    dest.parent.mkdir(parents=True,exist_ok=True)
                    if not dest.exists(): shutil.copy2(p,dest)
            audit.write('preservation-before.json',rows)
        audit.prepare()
    elif mode=='refresh': audit.write('inputs-refreshed.json',audit.inputs.prepare(audit.ROOT,audit.NAME))
    elif mode=='check': sys.exit(audit.check())
    elif mode=='checkpoint': audit.checkpoint(sys.argv[2])
    elif mode=='restore-fixtures': audit.restore_test_fixtures()
    elif mode=='audit': sys.exit(audit.audit())
    elif mode=='owner-prepare': owner_prepare()
    elif mode=='owner-checkpoint':
        audit.NAME=OWNER
        audit.inputs.NAMESPACE='combat-c0-diagnostics-validation-20261015.json'
        audit.checkpoint('owner-'+sys.argv[2])
    else: raise ValueError('Unsupported operation')
