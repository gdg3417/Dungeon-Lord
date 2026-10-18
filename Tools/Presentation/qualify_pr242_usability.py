"""Separate owner-usability correction campaign; reuse the reviewed fail-closed gate."""
from pathlib import Path
import sys
import qualify_pr242_graphics as gate

gate.NAME = 'pr242-usability-20261018'
gate.OUT = gate.ROOT / 'TestResults' / gate.NAME
gate.__file__ = __file__
APPROVED = set(gate.SOURCE_OVERRIDES + [
    'Assets/_Project/UI/ProductionDungeon/Dungeon.uss',
    'Assets/_Project/UI/ProductionDungeon/Dungeon.uxml',
    'Assets/_Project/Scripts/UI/ProductionDungeonController.cs',
    'Assets/_Project/Scripts/UI/ProductionDungeonConstruction.cs',
    'Assets/_Project/Scripts/UI/ProductionDungeonRoomContent.cs',
    'Assets/_Project/Scripts/UI/ProductionDungeonComposition.cs',
    'Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs',
    'Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonRoomContentSceneTests.cs',
    'Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs'])

def scope():
    for name, digest in [('qualify_pr242_graphics.py', 'f9f38078309d0a01b0a1c09cbf1a62837c03b245a77393d88b82e743b6e3ef3e'),
                         ('qualification_inputs.py', 'd684f97a4f2e6287f36620c308262fd1fcee39e23fa58295493bc52b803e167c')]:
        if gate.sha(Path(__file__).parent / name) != digest:
            raise ValueError('Reviewed preservation dependency changed: ' + name)
    changed = gate.git('diff', '--name-only', 'HEAD', '--', *gate.inputs.INPUT_ROOTS).splitlines()
    if set(changed) - set(gate.SOURCE_OVERRIDES):
        raise ValueError('Commit reviewed corrections before preparation: ' + str(changed))
    qualified = gate.read(gate.ROOT / 'Docs/testing/evidence/living-dungeon-b3a/source-applicability.json')
    gate.exact_inventory([r['path'] for r in qualified['unityInputs']], gate.inventory())
    for row in qualified['unityInputs']:
        if row['path'] not in APPROVED and gate.sha(gate.ROOT / row['path']) != row['sha256']:
            raise ValueError('Protected qualified source changed: ' + row['path'])

gate.verify_authored_scope = scope
gate.FILTERS['focused'] = ('EditMode', 'OwnerUsabilityConstructionAndContentReachability;RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated;TransactionalRoomConstructionLayoutsLocalizationAndResume')

if __name__ == '__main__':
    try:
        mode = sys.argv[1]
        if mode == 'init': gate.init()
        elif mode == 'prepare': gate.prepare(sys.argv[2], sys.argv[3])
        elif mode == 'preflight': gate.preflight(sys.argv[2])
        elif mode == 'run': sys.exit(gate.run(sys.argv[2]))
        elif mode == 'finish': gate.finish(sys.argv[2])
        elif mode == 'selftest': gate.selftest()
        else: raise ValueError('Unsupported operation')
    except (ValueError, OSError, KeyError, gate.zipfile.BadZipFile) as error:
        print('PR242 usability qualification rejected:', error, file=sys.stderr)
        sys.exit(2)
