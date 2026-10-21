"""Two-floor campaign: existing preservation adapter, fresh products and outputs."""
from pathlib import Path
source_path = Path(__file__).with_name('qualify_b3b2.py')
source = source_path.read_text(encoding='utf-8')
for old, new in (
    ('living-dungeon-b3b2-20261018', 'living-dungeon-two-floor-20261020'),
    ('living-dungeon-b3b2-qualification-20261018', 'living-dungeon-b3b2-qualification-two-floor-20261020'),
    ('b3b2_20261018', 'two_floor_20261020'),
    ('Dungeon Lord B3B2 Smoke 20261018', 'Dungeon Lord B3B2 Smoke Two Floor 20261020'),
    ('living-dungeon-b3b2-owner-uat-starter-', 'living-dungeon-two-floor-owner-uat-starter-'),
    ("'focused': 'LivingDungeonB3B2'", "'focused': 'LivingDungeonTwoFloor'"),
    ('BuildLivingDungeonB3B2Windows', 'BuildLivingDungeonTwoFloorWindows'),
):
    source = source.replace(old, new)
source = source.replace("'LivingDungeonB3B2PlayModeTests;", "'LivingDungeonTwoFloorPlayModeTests;LivingDungeonB3B2PlayModeTests;")
source = source.replace("'LivingDungeonB3B2;Graphical", "'LivingDungeonTwoFloor;LivingDungeonB3B2;Graphical")
source = source.replace("p=Path(row['path']);z.write(p,p.relative_to(ROOT/'TestResults').as_posix())",
    "p=Path(row['path']);\n            if p.suffix.lower() != '.zip': z.write(p,p.relative_to(ROOT/'TestResults').as_posix())")
exec(compile(source, str(source_path), 'exec'))
