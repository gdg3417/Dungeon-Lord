"""Fresh isolated correction campaign using the reviewed two-floor adapters."""
from pathlib import Path
import sys, types, hashlib

original = Path(__file__).with_name('qualify_two_floor.py')
source = original.read_text(encoding='utf-8')
source = source.replace('living-dungeon-two-floor-20261020', 'living-dungeon-two-floor-perf-20261021')
source = source.replace('living-dungeon-b3b2-qualification-two-floor-20261020', 'living-dungeon-b3b2-qualification-two-floor-perf-20261021')
source = source.replace('Dungeon Lord B3B2 Smoke Two Floor 20261020', 'Dungeon Lord B3B2 Smoke Two Floor Perf')
source = source.replace('Windows-living-dungeon-two-floor-20261021-final', 'Windows-living-dungeon-two-floor-perf-20261021')
source = source.replace('BuildLivingDungeonTwoFloorFinalWindows', 'BuildLivingDungeonTwoFloorPerformanceWindows')
# Keep every newly generated test input/report inside this campaign's archived
# output directory. The existing preflight checks these exact substitutions.
source = source.replace("exec(compile(source, str(source_path), 'exec'))",
    "source=source.replace(\"mode=sys.argv[1]\", \"m.q.OUTPUT_TESTS += ['Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs', 'Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonTwoFloorProductionProfileSceneTests.cs']\\nmode=sys.argv[1]\")\nexec(compile(source, str(source_path), 'exec'))")
native_path = original.with_name('qualify_two_floor_native.py')
native_source = native_path.read_text(encoding='utf-8')
old = 'Windows-living-dungeon-two-floor-20261021-final'
if native_source.count(old) != 1:
    raise ValueError('Reviewed native build path changed')
native_source = native_source.replace(old, 'Windows-living-dungeon-two-floor-perf-20261021')
native = types.ModuleType('qualify_two_floor_native')
native.__file__ = str(Path(__file__).resolve())
exec(compile(native_source, native.__file__, 'exec'), native.__dict__)
sys.modules['qualify_two_floor_native'] = native
exec(compile(source, str(original), 'exec'))
if sys.argv[1] == 'prepare':
    m.q.write('performance-adapter.json', dict(
        wrapperSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        twoFloorAdapterSha256=hashlib.sha256(original.read_bytes()).hexdigest(),
        nativeAdapterSha256=hashlib.sha256(native_path.read_bytes()).hexdigest(),
        nativeTransformation='Fresh Windows performance output path only',
        additionalEvidenceSubstitutions=m.q.OUTPUT_TESTS[-2:]))
