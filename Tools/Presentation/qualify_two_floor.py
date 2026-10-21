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
source = source.replace("'focused': 'LivingDungeonTwoFloor'", "'focused': 'DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonTwoFloor', 'profile-edit': 'LivingDungeonTwoFloorProductionProfileSceneTests'")
source = source.replace("'profile-edit': 'LivingDungeonTwoFloorProductionProfileSceneTests'", "'profile-edit': 'LivingDungeonTwoFloorProductionProfileSceneTests', 'profile-two': 'LivingDungeonTwoFloorEditorProductionProfile'")
source = source.replace("'profile-two': 'LivingDungeonTwoFloorEditorProductionProfile'", "'profile-two': 'LivingDungeonTwoFloorEditorProductionProfile', 'closed-terminal': 'TerminalPrefixOnlyFitsAnUnclosedV2Boundary'")
source = source.replace("'closed-terminal': 'TerminalPrefixOnlyFitsAnUnclosedV2Boundary'", "'closed-terminal': 'TerminalPrefixOnlyFitsAnUnclosedV2Boundary', 'layout-two': 'LivingDungeonTwoFloorWaitingSelectedFloorLayoutsPreserveCanonicalState'")
source = source.replace("'layout-two': 'LivingDungeonTwoFloorWaitingSelectedFloorLayoutsPreserveCanonicalState'", "'layout-two': 'LivingDungeonTwoFloorWaitingSelectedFloorLayoutsPreserveCanonicalState', 'production-exit': 'AutomaticProductionPartyCanChooseASurvivingFloorOneExitWithTwoCapturedFloors'")
source = source.replace("'LivingDungeonB3B2;Graphical", "'DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonTwoFloor;LivingDungeonB3B2;Graphical")
source = source.replace("p=Path(row['path']);z.write(p,p.relative_to(ROOT/'TestResults').as_posix())",
    "p=Path(row['path']);\n            if p.suffix.lower() != '.zip': z.write(p,p.relative_to(ROOT/'TestResults').as_posix())")
source = source.replace("elif mode=='smoke':", "elif mode=='two-floor-smoke':\n    from qualify_two_floor_native import launch\n    launch(m,sys.argv[2],sys.argv[3],sys.argv[4])\nelif mode=='smoke':")
source = source.replace("elif mode=='smoke':", "elif mode=='golden-capture':\n    from qualify_two_floor_compat import capture\n    capture(m,sys.argv[2])\nelif mode=='smoke':")
source = source.replace("exec(compile(source,m.__file__,'exec'),m.__dict__)",
    "source=source.replace(\"'1800'\", \"'7200'\")\nsource=source.replace(\"'regression': 'LivingDungeonPhaseA;\", \"'regression': 'LivingDungeonB3B1;CombatC1;Localization;LivingDungeonPhaseA;\")\nexec(compile(source,m.__file__,'exec'),m.__dict__)")
source = source.replace("elif mode=='all-required':", "elif mode=='remaining-required':\n    for stage in ('boundary-edit','profile-edit','full-play','build'):\n        if stage=='build': build_namespace()\n        result=m.run(stage,sys.argv[2])\n        if result: sys.exit(result)\n        checkpoint(sys.argv[2]+'-'+stage+'-passed')\n    m.q.NAMESPACE='living-dungeon-b3b2-qualification-two-floor-20261020.json'\n    m.q.inputs.NAMESPACE=m.q.NAMESPACE\n    data=subprocess.check_output(['git','show','HEAD:'+m.q.inputs.CONFIG],cwd=ROOT)\n    (ROOT/m.q.inputs.CONFIG).write_bytes(m.q.inputs.transformed(m.q.inputs.CONFIG,data))\n    current_service(True)\nelif mode=='all-required':")
source = source.replace("('boundary-edit','profile-edit','full-play','build')", "('boundary-edit','regression','profile-two','full-play','build')")
exec(compile(source, str(source_path), 'exec'))
