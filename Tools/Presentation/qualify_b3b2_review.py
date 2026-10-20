"""PR #245 correction: reuse the B3B2 campaign with unused identities/outputs."""
from pathlib import Path
import sys

if any(arg in ('build-smoke-fix', 'smoke-fix') for arg in sys.argv[1:]):
    raise ValueError('This correction campaign supports only its separate plain build/smoke paths')

source_path = Path(__file__).with_name('qualify_b3b2.py')
source = source_path.read_text(encoding='utf-8')
for old, new in (
    ('living-dungeon-b3b2-20261018', 'living-dungeon-b3b2-20261019-review'),
    ('living-dungeon-b3b2-qualification-20261018', 'living-dungeon-b3b2-qualification-20261019-review'),
    ('b3b2_20261018', 'b3b2_20261019_review'),
    ('Dungeon Lord B3B2 Smoke 20261018', 'Dungeon Lord B3B2 Smoke 20261019 Review'),
    ('living-dungeon-b3b2-owner-uat-starter-', 'living-dungeon-b3b2-review-owner-uat-starter-'),
    ('BuildLivingDungeonB3B2Windows', 'BuildLivingDungeonB3B2ReviewWindows'),
):
    source = source.replace(old, new)
# Preserve/hash the existing archive containers in place. Back up raw evidence
# without recursively copying prior campaigns' large verified backup archives.
source = source.replace("p=Path(row['path']);z.write(p,p.relative_to(ROOT/'TestResults').as_posix())",
    "p=Path(row['path']);\n            if p.suffix.lower() != '.zip': z.write(p,p.relative_to(ROOT/'TestResults').as_posix())")
exec(compile(source, str(source_path), 'exec'))
