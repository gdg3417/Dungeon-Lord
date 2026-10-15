"""Preserving validation for approved production C0; old prerequisite artifacts stay intact."""
import sys
import qualify_visual_feedback as audit

audit.NAME = 'ui-composition-combat-c0-integration-validation'
audit.OUT = audit.ROOT / 'TestResults/combat-c0-integration'
audit.inputs.NAMESPACE = 'combat-c0-integration-validation-20261014.json'

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'baseline-prepare':
        audit.NAME = 'ui-composition-combat-c0-b1-baseline'
        audit.OUT = audit.ROOT / 'TestResults/combat-c0-integration/baseline'
        audit.inputs.NAMESPACE = 'combat-c0-b1-baseline-20261014.json'
        audit.prepare()
    elif mode == 'prepare': audit.prepare()
    elif mode == 'refresh':
        audit.write('inputs-refreshed.json', audit.inputs.prepare(audit.ROOT, audit.NAME))
    elif mode == 'check': sys.exit(audit.check())
    elif mode == 'checkpoint': audit.checkpoint(sys.argv[2])
    elif mode == 'restore-fixtures': audit.restore_test_fixtures()
    elif mode == 'audit': sys.exit(audit.audit())
    else: raise ValueError('Unsupported qualification operation')
