"""Measured PR #245 live-readability campaign; reuse the established preservation adapter."""
from pathlib import Path
import sys

variant = sys.argv.pop(1)
if variant not in ('before', 'after'):
    raise ValueError('Choose before or after; never reopen an older campaign')
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
exec(compile(source, str(source_path), 'exec'))
