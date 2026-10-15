"""C0 prerequisite qualification using the established preserving input/audit workflow.

No owner saves are opened or reset. A new project/output/namespace preserves previous
qualification projects, player builds, disposable UAT evidence and backups.
"""
import sys
import qualify_visual_feedback as audit

audit.NAME = 'ui-composition-combat-c0-validation'
audit.OUT = audit.ROOT / 'TestResults/combat-c0'
audit.inputs.NAMESPACE = 'combat-c0-prerequisite-validation-20261014.json'

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'prepare':
        audit.prepare()
    elif mode == 'check':
        sys.exit(audit.check())
    elif mode == 'checkpoint':
        audit.checkpoint(sys.argv[2])
    elif mode == 'restore-fixtures':
        audit.restore_test_fixtures()
    elif mode == 'audit':
        sys.exit(audit.audit())
    else:
        raise ValueError('Unsupported qualification operation')
