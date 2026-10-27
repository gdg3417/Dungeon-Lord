"""Source/asset dependency diagnostic. Does not execute Unity or canonical recovery."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import subprocess


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    args = p.parse_args()
    root = args.repo.resolve(strict=True)
    output = args.output.resolve()
    expected_parent = root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit"
    if output.parent != expected_parent or output.exists():
        raise SystemExit("Output must be new and in this probe's exact evidence directory")
    assets = [
        "Assets/_Project/Data/Bootstrap/run_simulation_config.json",
        "Assets/_Project/Resources/passive_online_mana.json",
        "Assets/_Project/Resources/structural_economy.json",
        "Assets/_Project/Resources/monster_recovery.json",
        "Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json",
        "Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json",
        "Assets/_Project/Resources/living_dungeon_foreground.json",
        "Assets/_Project/Resources/living_dungeon_foreground_v2.json",
        "Assets/_Project/Data/Production/Save/room_content_spatial_occupancy.json",
        "Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json",
    ]
    extra_assets = ["Assets/_Project/Data/Bootstrap/content_bootstrap.json",
                    "Assets/_Project/Data/Bootstrap/loot_config.json"]
    sources = [
        "Assets/_Project/Scripts/Core/GameRoot.cs",
        "Assets/_Project/Scripts/Core/GameRoot.Foreground.cs",
        "Assets/_Project/Scripts/Services/PlayerResearchActionHandler.cs",
        "Assets/_Project/Scripts/Services/ResearchProgressApplyResolver.cs",
        "Assets/_Project/Scripts/Services/ResearchProgressResolver.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs",
        "Assets/_Project/Scripts/Services/LootRollResolver.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/ProductionSpatialGeneratedSet.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs",
        "Assets/_Project/Scripts/Util/Models.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialMigrationPreparation.cs",
    ]
    blobs = {path: (root / path).read_bytes() for path in assets + extra_assets + sources}
    text = lambda name: blobs[next(path for path in sources if path.endswith(name))].decode("utf-8")
    checks = {
        "tick_duration_from_other_bootstrap": "SaveSpatialMigrationLimits.Canonical.Spatial, bootstrap.tickSeconds," in text("GameRoot.cs"),
        "accepted_drain_tick_uses_external_duration": "long tickUnits = checked((long)tickSeconds * policy.UnitsPerSecond);" in text("LivingDungeonForeground.cs"),
        "research_config_from_other_bootstrap": "Content.Bootstrap.researchProgressScaffold" in text("GameRoot.cs"),
        "research_tick_applies_rate": "elapsedSecondsUsed * config.progressPerActiveSecond" in text("ResearchProgressApplyResolver.cs"),
        "accepted_opportunity_captures_live_loot": "runLoot, null, runtime, RunPostureResolver.BalancedId" in text("LivingDungeonForeground.cs"),
        "new_start_serializes_live_loot": "loot = lootConfig == null ? null : JsonUtility.ToJson(lootConfig)" in text("ActiveFloorRunSnapshot.cs"),
        "legacy_omits_combat": "[NonSerialized] internal Gameplay.RunSimulation.CombatC0Config CombatC0;" in text("Models.cs"),
        "legacy_serializer_unchanged_shape": 'JsonUtility.ToJson(value, true) + "\\n"' in text("DetachedSpatialMigrationPreparation.cs"),
    }
    assert all(checks.values()), checks
    components = [blobs[path] for path in assets]
    domain = b"DL-H1F4-context-v2\n"
    assert len(domain) == 19 and len(components) == 10

    def digest(values):
        assert len(values) == 10 and all(values)
        return sha(domain + b"".join(hashlib.sha256(v).digest() for v in values))

    baseline = digest(components)
    assert baseline == digest([bytes(bytearray(v)) for v in components])
    raw_config = components[0]
    changed_config = raw_config.replace(b'"MaxHealth": 8,', b'"MaxHealth": 10,', 1)
    before = json.loads(raw_config)
    after = json.loads(changed_config)
    expected = copy.deepcopy(before)
    goblin = next(m for m in expected["CombatC0"]["Monsters"] if m["Id"] == "placement.option.monster.goblin")
    assert goblin["MaxHealth"] == 8
    goblin["MaxHealth"] = 10
    assert after == expected
    variant = [changed_config] + components[1:]
    assert digest(variant) != baseline
    policy = json.loads(components[3])
    readiness = lambda hp: (hp * policy["ReadinessNumerator"] + policy["ReadinessDenominator"] - 1) // policy["ReadinessDenominator"]
    assert [readiness(8), readiness(10)] == [6, 7]

    bootstrap = json.loads(blobs[extra_assets[0]])
    units = json.loads(components[7])["UnitsPerSecond"]
    tick = bootstrap["tickSeconds"]
    assert tick == 10 and units == 1000
    accepted = tick * units
    rate = bootstrap["researchProgressScaffold"]["progressPerActiveSecond"]
    assert tick * rate != tick * (rate * 2)
    loot = json.loads(blobs[extra_assets[1]])
    table = next(t for t in loot["tables"] if t["id"] == before["LootTableId"])
    assert table["minRollCount"] == table["maxRollCount"] == 2
    assert accepted // (tick * units) == 1 and accepted // (tick * 2 * units) == 0

    result = {
        "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
        "probe_sha256": sha(Path(__file__).read_bytes()),
        "files_sha256": {path: sha(data) for path, data in blobs.items()},
        "source_checks": checks,
        "digest_fixture": {
            "domain_hex": domain.hex(), "ten_asset_byte_digest": baseline,
            "goblin_maximum_only_variant_digest": digest(variant),
            "unchanged_input_digest_identical": True, "readiness_before_after": [6, 7],
            "limitation": "Direct asset-byte composition: manifest/catalog assets stand for their canonical loaded bytes. No Unity validation/JsonUtility serialization was run; this is not a qualified runtime digest or historical-serializer regression verdict."
        },
        "further_omissions": [
            {"asset": extra_assets[0], "change": "tickSeconds 10 -> 20",
             "closed_units": 0, "accepted_units": accepted, "units_per_second": units,
             "ticks_before_after": [1, 0], "all_ten_context_components_unchanged": True},
            {"asset": extra_assets[0], "change": "researchProgressScaffold.progressPerActiveSecond doubled",
             "active_seconds": tick, "progress_delta_before_after": [tick * rate, tick * rate * 2],
             "all_ten_context_components_unchanged": True},
            {"asset": extra_assets[1], "change": "Increase every item.worldValue by 1; preserve IDs/pools/weights",
             "fixed_roll_count": table["minRollCount"], "generated_world_value_delta": 2,
             "reason": "Same selection and roll count; each selected item contributes one additional world value",
             "all_ten_context_components_unchanged": True},
        ],
        "unity_or_episode_protocol_tests_executed": False,
        "qualification_counts": None,
    }
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"output": str(output), "sha256": sha(output.read_bytes()),
                      "source_checks": len(checks), "further_omission_examples": 3,
                      "goblin_digest_changes": True, "unchanged_digest_repeats": True}))


if __name__ == "__main__":
    main()
