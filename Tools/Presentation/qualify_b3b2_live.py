"""Measured PR #245 live-readability campaign; reuse the established preservation adapter."""
from pathlib import Path
import sys

variant = sys.argv.pop(1)
if variant not in ('before', 'after', 'final', 'marker'):
    raise ValueError('Choose an unused before, after or final campaign; never reopen an older campaign')
if any(arg in ('build-smoke-fix', 'smoke-fix') for arg in sys.argv[1:]):
    raise ValueError('Historical smoke-fix outputs are not part of this campaign')
profile_retry = 'build-profile-fix' in sys.argv or 'profile-retry' in sys.argv
rendered = 'build-rendered' in sys.argv or 'rendered' in sys.argv
if rendered and variant != 'marker': raise ValueError('Rendered visual qualification belongs to the marker campaign')
sys.argv = [arg.replace('build-rendered', 'build') for arg in sys.argv if arg != 'rendered']
sys.argv = [arg.replace('build-profile-fix', 'build').replace('profile-retry', 'profile') for arg in sys.argv]
source_path = Path(__file__).with_name('qualify_b3b2.py')
source = source_path.read_text(encoding='utf-8')
for old, new in (
    ('living-dungeon-b3b2-20261018', 'living-dungeon-b3b2-20261019-live-' + variant),
    ('living-dungeon-b3b2-qualification-20261018', 'living-dungeon-b3b2-qualification-20261019-live-' + variant),
    ('b3b2_20261018', 'b3b2_20261019_live_' + variant),
    ('Dungeon Lord B3B2 Smoke 20261018', 'Dungeon Lord B3B2 Smoke 20261019 Live ' + variant.title()),
    ('living-dungeon-b3b2-owner-uat-starter-', 'living-dungeon-b3b2-live-' + variant + '-owner-uat-starter-'),
    ('BuildLivingDungeonB3B2Windows', 'BuildLivingDungeonB3B2Live' + variant.title() + 'Windows'),
): source = source.replace(old, new)
source = source.replace("p=Path(row['path']);z.write(p,p.relative_to(ROOT/'TestResults').as_posix())",
    "p=Path(row['path']);\n            if p.suffix.lower() != '.zip': z.write(p,p.relative_to(ROOT/'TestResults').as_posix())")
source = source.replace("IDENTITY_TEST=", "m.q.OUTPUT_TESTS.append('Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs')\nIDENTITY_TEST=")
source = source.replace("'focused': 'LivingDungeonB3B2'", "'probe': 'TerminalSettlementPreservesTheCurrentSavedBoundary;LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState', 'profile': 'ForegroundPerformanceSustainedProduction68Runs', 'readability': 'VisualRun;CombatC1;LivingDungeonB3B1;TimeService;SimulationClock', 'focused': 'LivingDungeonB3B2'")
source = source.replace("('admit','settle','finish')", "('admit','settle','finish','profile')")
source = source.replace("'probe':", "'recovery-probe': 'RecoveryConditionChangesAreStagedBeforeAdmissionWhenDoubleWalletDebitRoundsAway', 'probe':")
source = source.replace("'profile': 'ForegroundPerformanceSustainedProduction68Runs',", "'profile': 'ForegroundPerformanceSustainedProduction68Runs', 'marker-ui': 'LivingDungeonB3B2LiveLabels;LivingDungeonB3B2Marker;LivingDungeonB3B2RecordedAdjacent;LivingDungeonB3B2PresentationSnaps;LivingDungeonB3B2ActiveIdle;VisualRun;CombatC1;Localization',")
source = source.replace("if phase!='admit'", "if phase not in ('admit','profile')")
if variant == 'marker':
    source = source.replace('    import datetime', "    args += ['-b3b2MarkerVisuals']\n    import datetime")
    # Resume only the already inventoried, interrupted preparation. Every
    # original archive/manifest stays intact; normal preflight rechecks hashes.
    source = source.replace("elif mode=='run':", """elif mode=='complete-preparation':
    m.q.applicability('preparation-resumed')
    with zipfile.ZipFile(m.q.OUT/'historical-evidence-preserved.zip','x',zipfile.ZIP_DEFLATED) as z:
        for row in json.loads((m.q.OUT/'historical-evidence-before.json').read_text()):
            p=Path(row['path'])
            if p.suffix.lower() != '.zip': z.write(p,p.relative_to(ROOT/'TestResults').as_posix())
    m.isolate_product()
elif mode=='run':""")
# This file owns a documented capture substitution, but its committed tests can
# evolve during the campaign. Cleanup restores HEAD after archiving qualified
# inputs instead of reverting those corrections to the initial archive version.
source = source.replace('        restore_identity_test()',
    '        restore_identity_test()\n        name="Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs"\n        (ROOT/name).write_bytes(subprocess.check_output(["git","show","HEAD:"+name],cwd=ROOT))')
if profile_retry:
    if variant != 'before': raise ValueError('Only the baseline profile fixture has a retry build')
    source = source.replace('Windows-living-dungeon-b3b2-20261019-live-before', 'Windows-living-dungeon-b3b2-20261019-live-before-profile-fix')
    source = source.replace('BuildLivingDungeonB3B2LiveBeforeWindows', 'BuildLivingDungeonB3B2LiveBeforeProfileFixWindows')
    source = source.replace("else 'native-'", "else 'native-profile-fix-'")
    loader = "exec(compile(source,m.__file__,'exec'),m.__dict__)"
    if source.count(loader) != 1: raise ValueError('Nested adapter loader changed')
    source = source.replace(loader,
        "source=source.replace('Windows-living-dungeon-b3b2-20261019-live-before', 'Windows-living-dungeon-b3b2-20261019-live-before-profile-fix')\n" + loader)
    source = source.replace('IDENTITY_TEST=',
        "assert 'Builds/Development/Windows-living-dungeon-b3b2-20261019-live-before-profile-fix' in m.run.__code__.co_consts, 'Retry target was not isolated'\nIDENTITY_TEST=")
if rendered:
    # Batch mode has no usable screenshot backbuffer and ignores resizing.
    # A requested visual smoke needs a rendered, non-activating player window.
    source = source.replace('Windows-living-dungeon-b3b2-20261019-live-marker', 'Windows-living-dungeon-b3b2-20261019-live-marker-rendered')
    source = source.replace('BuildLivingDungeonB3B2LiveMarkerWindows', 'BuildLivingDungeonB3B2LiveMarkerRenderedWindows')
    source = source.replace("native='living-dungeon-b3b2-qualification-20261019-live-marker-native'", "native='living-dungeon-b3b2-qualification-20261019-live-marker-native-rendered'")
    source = source.replace("'native-namespace'", "'native-rendered-namespace'")
    source = source.replace("else 'native-'", "else 'native-rendered-'")
    source = source.replace("'-batchmode',", '')
    source = source.replace('startup.wShowWindow=subprocess.SW_HIDE', 'startup.wShowWindow=4')
    loader = "exec(compile(source,m.__file__,'exec'),m.__dict__)"
    if source.count(loader) != 1: raise ValueError('Nested adapter loader changed')
    source = source.replace(loader,
        "source=source.replace('Windows-living-dungeon-b3b2-20261019-live-marker', 'Windows-living-dungeon-b3b2-20261019-live-marker-rendered').replace('BuildLivingDungeonB3B2LiveMarkerWindows', 'BuildLivingDungeonB3B2LiveMarkerRenderedWindows')\n" + loader)
exec(compile(source, str(source_path), 'exec'))
