"""Diagnostics correction qualification; preserve the owner's C0 project/save/build."""
import sys
import qualify_visual_feedback as audit

audit.NAME = 'ui-composition-combat-c0-diagnostics-validation-r2'
audit.OUT = audit.ROOT / 'TestResults/combat-c0-diagnostics'
audit.inputs.NAMESPACE = 'combat-c0-diagnostics-validation-20261015.json'

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'prepare': audit.prepare()
    elif mode == 'refresh': audit.write('inputs-refreshed.json', audit.inputs.prepare(audit.ROOT, audit.NAME))
    elif mode == 'check': sys.exit(audit.check())
    elif mode == 'checkpoint': audit.checkpoint(sys.argv[2])
    elif mode == 'restore-fixtures': audit.restore_test_fixtures()
    elif mode == 'audit': sys.exit(audit.audit())
    else: raise ValueError('Unsupported qualification operation')
