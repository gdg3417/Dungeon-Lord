"""C2 qualification in the retained r2 project; preserve every earlier save/player."""
from pathlib import Path
import json, sys, zipfile
import qualify_visual_feedback as audit

audit.NAME = 'ui-composition-combat-c0-diagnostics-validation-r2'
audit.OUT = audit.ROOT/'TestResults/combat-c2'
audit.inputs.NAMESPACE = 'combat-c2-automated-20261016.json'

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'prepare':
        result = audit.inputs.compare(audit.ROOT, audit.NAME)
        if result['unexpected']: raise ValueError(str(result['unexpected']))
        audit.OUT.mkdir(parents=True, exist_ok=True)
        archive = audit.OUT/'r2-inputs-before-c2.zip'
        if not archive.exists():
            with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED) as z:
                for row in result['files']:
                    p = audit.ROOT/'Temp'/audit.NAME/row['path']
                    if p.exists(): z.write(p, row['path'])
        audit.prepare()
    elif mode == 'refresh': audit.write('inputs-refreshed.json', audit.inputs.prepare(audit.ROOT, audit.NAME))
    elif mode == 'check': sys.exit(audit.check())
    elif mode == 'audit': sys.exit(audit.audit())
    elif mode == 'owner-baseline':
        owner = audit.ROOT/'Temp/ui-composition-combat-c0-diagnostics-validation-warm'
        paths = {owner/p for p in audit.inputs.target_inventory(owner)}
        for name in ('Logs','UserSettings'):
            paths.update(p for p in (owner/name).rglob('*') if p.is_file())
        target = audit.OUT/'owner-environment-before.json'
        if target.exists(): raise ValueError('Owner baseline already recorded')
        audit.write(target.name,[dict(path=str(p),sha256=audit.sha(p)) for p in sorted(paths)])
        print(len(paths),'owner input/log/settings files recorded without opening the project')
    elif mode == 'owner-audit':
        rows=json.loads((audit.OUT/'owner-environment-before.json').read_text())
        for row in rows:
            p=Path(row['path']); row['current_sha256']=audit.sha(p) if p.exists() else None
            row['equal']=row['sha256']==row['current_sha256']
        audit.write('owner-environment-after.json',rows)
        print(len(rows),'owner environment files;',sum(not r['equal'] for r in rows),'changed')
        sys.exit(any(not r['equal'] for r in rows))
    elif mode == 'restore-fixtures': audit.restore_test_fixtures()
    elif mode == 'checkpoint': audit.checkpoint(sys.argv[2])
    else: raise ValueError(mode)
