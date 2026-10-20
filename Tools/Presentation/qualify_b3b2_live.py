"""Measured PR #245 live-readability campaign; reuse the established preservation adapter."""
from pathlib import Path
import sys

variant = sys.argv.pop(1)
if variant not in ('before', 'after'):
    raise ValueError('Choose before or after; never reopen an older campaign')
if any(arg in ('build-smoke-fix', 'smoke-fix') for arg in sys.argv[1:]):
    raise ValueError('Historical smoke-fix outputs are not part of this campaign')
profile_retry = 'build-profile-fix' in sys.argv or 'profile-retry' in sys.argv
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
source = source.replace("'focused': 'LivingDungeonB3B2'", "'profile': 'ForegroundPerformanceSustainedProduction68Runs', 'focused': 'LivingDungeonB3B2'")
source = source.replace("('admit','settle','finish')", "('admit','settle','finish','profile')")
source = source.replace("if phase!='admit'", "if phase not in ('admit','profile')")
if profile_retry:
    if variant != 'before': raise ValueError('Only the baseline profile fixture has a retry build')
    source = source.replace('Windows-living-dungeon-b3b2-20261019-live-before', 'Windows-living-dungeon-b3b2-20261019-live-before-profile-fix')
    source = source.replace('BuildLivingDungeonB3B2LiveBeforeWindows', 'BuildLivingDungeonB3B2LiveBeforeProfileFixWindows')
    source = source.replace("else 'native-'", "else 'native-profile-fix-'")
exec(compile(source, str(source_path), 'exec'))
