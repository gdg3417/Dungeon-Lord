"""Audit source ownership and proposed JSON byte envelope; never qualifies a save."""
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
    if output.parent != root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit" or output.exists():
        raise SystemExit("Expected new output in the exact audit evidence directory")
    source_paths = [
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonContracts.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs",
        "Assets/_Project/Scripts/Gameplay/RunSimulation/RunCheckpoint.cs",
        "Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonOutcomeConsequences.cs",
        "Assets/_Project/Scripts/Util/Models.cs",
        "Assets/_Project/Data/Bootstrap/content_manifest.json",
        "Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json",
    ]
    blobs = {name: (root / name).read_bytes() for name in source_paths}
    source = lambda index: blobs[source_paths[index]].decode("utf-8")
    assertions = {
        "scheduler_has_horizons_not_global_config": "MinimumOpportunitySpacingUnits, AcceptedThrough = -1, ClosedThrough = -1;" in source(0),
        "unpaid_tick_uses_live_tick_seconds": "long tickUnits = checked((long)tickSeconds * policy.UnitsPerSecond);" in source(1),
        "admitted_loot_owned_in_start": "loot = lootConfig == null ? null : JsonUtility.ToJson(lootConfig)" in source(2),
        "captured_loot_not_live_getter": "LootConfiguration => loot == null ? null : JsonUtility.FromJson<LootConfig>(loot)" in source(2),
        "captured_run_rules_not_live_getter": "Configuration => JsonUtility.FromJson<RunSimulationConfig>(configuration)" in source(2),
        "supported_reader_retains_format_one": "if (format == RunCheckpointCodec.Format) return RunCheckpointCodec.Restore" in source(3),
        "party_heat_uses_snapshot_rules": "var config = snapshot.Configuration;" in source(5),
        "legacy_combat_exclusion_still_present": "[NonSerialized] internal Gameplay.RunSimulation.CombatC0Config CombatC0;" in source(6),
    }
    assert all(assertions.values()), assertions
    original = {
        "FormatVersion": 2, "Mode": "preciseRecovery", "AccountedFraction100ns": 9999999,
        "Episode": {
            "EntryPredecessorSha256": "f" * 64, "RecoveryContextSha256": "f" * 64,
            "SourceSeconds": 253402300799, "SourceFraction100ns": 9999999,
            "EntrySeconds": 253402300799, "EntryFraction100ns": 9999999,
            "EntryContext": "unknownPostCrash", "OriginalMode": "legacyWholeSeconds",
            "Phase": "originalPending", "OriginalEligible100ns": 2534023007989999999,
            "ResidualEligible100ns": 2534023007989999999,
        },
    }
    proposed_episode = copy.deepcopy(original["Episode"])
    context = proposed_episode.pop("RecoveryContextSha256")
    proposed = {name: original[name] for name in list(original)[:3]}
    proposed["RecoveryContextSha256"] = context
    proposed["Episode"] = proposed_episode
    encode = lambda owner: (',"timeProvenance":' + json.dumps(owner, separators=(",", ":"), ensure_ascii=True)).encode("ascii")
    original_bytes = len(encode(original))
    proposed_bytes = len(encode(proposed))
    assert original_bytes == proposed_bytes == 590
    original_closed = copy.deepcopy(original)
    proposed_closed = copy.deepcopy(proposed)
    original_closed["Episode"] = proposed_closed["Episode"] = None
    assert "RecoveryContextSha256" not in original_closed
    assert proposed_closed["RecoveryContextSha256"] == context
    def nodes(value):
        return 1 + (sum(nodes(v) for v in value.values()) if isinstance(value, dict) else 0)
    assert nodes(original) == nodes(proposed) == 16
    assert sum(len(v) for v in (original["Episode"], original)) + 1 == 16
    assert sum(len(v) for v in (proposed["Episode"], proposed)) + 1 == 16

    previous = json.loads((root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit/result.json").read_bytes())
    asset_names = [name for name in previous["files_sha256"] if name.endswith(".json")]
    historical_sha = "c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d"
    history = []
    for name in asset_names:
        old_bytes = subprocess.check_output(["git", "show", historical_sha + ":" + name], cwd=root)
        current_bytes = (root / name).read_bytes()
        history.append({"path": name, "source_sha": historical_sha, "sha256": sha(old_bytes),
                        "matches_current": old_bytes == current_bytes})
    assert len(history) == 12
    result = {
        "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
        "probe_sha256": sha(Path(__file__).read_bytes()),
        "source_files_sha256": {name: sha(data) for name, data in blobs.items()},
        "source_assertions": assertions,
        "encoding_envelope": {"approved_open_bytes": original_bytes,
                              "proposed_open_bytes": proposed_bytes,
                              "approved_closed_bytes": len(encode(original_closed)),
                              "proposed_closed_bytes": len(encode(proposed_closed)),
                              "value_nodes_each": nodes(proposed), "member_record_reserve_each": 16,
                              "limitation": "Maximum syntactic envelope includes semantically invalid combinations; no ContractJson execution, migration or valid-save boundary test."},
        "closed_context_continuity": {"approved_closed_owner_has_identity": False,
                                      "proposed_closed_owner_has_identity": True,
                                      "scope": "Proposed representation check only, not a validated canonical save"},
        "schema_fifteen_source_asset_comparison": history,
        "all_twelve_assets_equal_at_selected_source": all(item["matches_current"] for item in history),
        "legacy_mapping_limitation": "Two source trees with matching inputs do not prove every historical save's provenance or authorize a migration mapping.",
        "runtime_or_unity_tests_executed": False,
    }
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"output": str(output), "sha256": sha(output.read_bytes()),
                      "source_assertions": len(assertions), "open_owner_bytes_each": 590,
                      "all_twelve_assets_equal_at_selected_source": result["all_twelve_assets_equal_at_selected_source"]}))


if __name__ == "__main__":
    main()
