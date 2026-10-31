"""Read-only source/fixture probe; not a Unity or canonical-save qualification test."""
import argparse
import copy
import hashlib
import json
import pathlib
import subprocess


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=pathlib.Path, required=True)
    parser.add_argument("--output", type=pathlib.Path, required=True)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    output = args.output.resolve()
    evidence_dir = root / "Docs/testing/evidence/h1-f4/context-binding-blocker"
    if output.parent != evidence_dir or output.exists():
        raise SystemExit("Output must be a new file in this probe's exact evidence directory")

    paths = [
        "Assets/_Project/Scripts/Util/Models.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialMigrationPreparation.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Config.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0QualificationConfig.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs",
        "Assets/_Project/Scripts/Services/BootstrapConfigValidationService.cs",
        "Assets/_Project/Scripts/Services/SaveService.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs",
        "Assets/_Project/Data/Bootstrap/run_simulation_config.json",
        "Assets/_Project/Resources/monster_recovery.json",
        "Docs/planning/h1-f4-recovery-design-gate.md",
    ]
    blobs = {path: (root / path).read_bytes() for path in paths}
    models = blobs[paths[0]].decode("utf-8")
    serializer = blobs[paths[1]].decode("utf-8")
    rules = blobs[paths[4]].decode("utf-8")
    assertions = {
        "combat_explicitly_nonserialized": "[NonSerialized] internal Gameplay.RunSimulation.CombatC0Config CombatC0;" in models,
        "legacy_serializer_uses_jsonutility": 'JsonUtility.ToJson(value, true) + "\\n"' in serializer,
        "recovery_health_from_combat": "health = combat.Monsters.ToDictionary(m => m.Id, m => m.MonsterHealth, StringComparer.Ordinal);" in rules,
        "record_accepts_health_below_current_max": "r.Health >= 0 && r.Health <= rules.MaximumHealth(option)" in rules,
    }
    assert all(assertions.values()), assertions

    original = json.loads(blobs[paths[8]])
    changed = copy.deepcopy(original)
    goblin_id = "placement.option.monster.goblin"
    before_monster = next(m for m in original["CombatC0"]["Monsters"] if m["Id"] == goblin_id)
    after_monster = next(m for m in changed["CombatC0"]["Monsters"] if m["Id"] == goblin_id)
    assert before_monster["MaxHealth"] == 8
    after_monster["MaxHealth"] = 10  # Hypothetical in-memory variant; no content file write.
    before_projection = {k: v for k, v in original.items() if k != "CombatC0"}
    after_projection = {k: v for k, v in changed.items() if k != "CombatC0"}
    assert before_projection == after_projection
    normalized = lambda v: json.dumps(v, sort_keys=True, separators=(",", ":")).encode("utf-8")

    recovery = json.loads(blobs[paths[9]])
    numerator = recovery["ReadinessNumerator"]
    denominator = recovery["ReadinessDenominator"]
    cost = recovery["ManaPerHealth"]
    readiness = lambda maximum: (maximum * numerator + denominator - 1) // denominator
    thresholds = [readiness(before_monster["MaxHealth"]), readiness(after_monster["MaxHealth"])]
    examples = []
    for phase, health in [("originalPending", 1), ("residualPending", thresholds[0])]:
        outcomes = []
        for maximum, threshold in zip([8, 10], thresholds):
            wallet = 100  # Illustrative sufficient funding, below production capacity 1000.
            restored = min(max(0, threshold - health), wallet // cost)
            outcomes.append({"maximum_health": maximum, "readiness": threshold,
                             "valid_living_health_record": 0 < health <= maximum,
                             "health_after_recovery": health + restored,
                             "mana_spent": restored * cost, "wallet_after_recovery": wallet - restored * cost})
        assert outcomes[0]["mana_spent"] != outcomes[1]["mana_spent"]
        examples.append({"phase": phase, "initial_health": health, "initial_wallet": 100,
                         "vacant_active_floor": True, "revival_required": False,
                         "outcomes": outcomes})

    result = {
        "purpose": "H1-F4 approved context binding: excluded recovery dependency counterexample",
        "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
        "probe_sha256": sha(pathlib.Path(__file__).read_bytes()),
        "source_files_sha256": {path: sha(blob) for path, blob in blobs.items()},
        "source_assertions": assertions,
        "fixture_change": "Only CombatC0.Monsters[goblin].MaxHealth 8 -> 10, in memory",
        "outside_combat_projection_equal": before_projection == after_projection,
        "projection_sha256_before": sha(normalized(before_projection)),
        "projection_sha256_after": sha(normalized(after_projection)),
        "projection_hash_is_production_jsonutility_hash": False,
        "context_digest_analysis": "Input 1 excludes the only changed field by NonSerialized; inputs 2-10 unchanged. Approved production digest must remain equal. Not computed with Unity here.",
        "counterexamples": examples,
        "unity_or_canonical_save_validation_executed": False,
        "qualification_counts": None,
        "limitations": "Structural source assertions and integer arithmetic only; no real save load/reopen, Unity test, migration, executable gate or protocol fault qualification is claimed.",
    }
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(json.dumps({"output": str(output), "sha256": sha(output.read_bytes()),
                      "source_assertions": len(assertions), "counterexamples": len(examples),
                      "outside_combat_projection_equal": True}))


if __name__ == "__main__":
    main()
