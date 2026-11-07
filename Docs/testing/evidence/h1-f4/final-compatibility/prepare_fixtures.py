"""Prepare review fixtures and inspect retained evidence; no runtime validation."""
import argparse
import base64
import copy
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import zipfile


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    output = args.output.resolve()
    if output.parent != root / "Docs/testing/evidence/h1-f4/final-compatibility" or output.exists():
        raise SystemExit("Expected unused output in this preparation's exact evidence directory")
    previous = json.loads((root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit/result.json").read_bytes())
    asset_names = [name for name in previous["files_sha256"] if name.endswith(".json")]
    assert len(asset_names) == 12
    inputs = [(root / name).read_bytes() for name in asset_names]
    domain = b"DL-H1F4-context-v3\n"
    digest = lambda values: sha(domain + b"".join(hashlib.sha256(v).digest() for v in values))
    current_hash = digest(inputs)
    assert len(domain) == 19 and current_hash == digest([bytes(bytearray(v)) for v in inputs])
    changes = []
    for name, component in [("tickSeconds", 10), ("researchProgression", 10), ("lootWorldValues", 11), ("combatMaximumHp", 0)]:
        modified = json.loads(inputs[component])
        if name == "tickSeconds":
            assert modified["tickSeconds"] == 10
            modified["tickSeconds"] = 20
        elif name == "researchProgression":
            assert modified["researchProgressScaffold"]["progressPerActiveSecond"] == 0.01
            modified["researchProgressScaffold"]["progressPerActiveSecond"] = 0.02
        elif name == "lootWorldValues":
            for item in modified["items"]:
                item["worldValue"] += 1
        else:
            goblin = next(m for m in modified["CombatC0"]["Monsters"] if m["Id"] == "placement.option.monster.goblin")
            assert goblin["MaxHealth"] == 8
            goblin["MaxHealth"] = 10
        variant = list(inputs)
        variant[component] = (json.dumps(modified, indent=2) + "\n").encode("utf-8")
        assert digest(variant) != current_hash
        changes.append({"change": name, "component": component + 1, "modified_input_sha256": sha(variant[component]),
                        "context_sha256": digest(variant), "fixture_scope": "In-memory asset variant; no production asset edited"})

    states = [
        {"id": "originalPending", "phase": "originalPending", "C": 0, "A": 10000},
        {"id": "residualPending", "phase": "residualPending", "C": 10000, "A": 10000},
        {"id": "closedAcceptedDebt", "phase": None, "C": 0, "A": 10000},
        {"id": "closedOfflineGap", "phase": None, "C": 10000, "A": 10000, "W": 100, "R": 120},
        {"id": "settledNoGap", "phase": None, "C": 10000, "A": 10000, "W": 100, "R": 100},
        {"id": "closedActiveSnapshot", "phase": None, "C": 10000, "A": 10000, "retained_parties": 1},
        {"id": "closedResearchInProgress", "phase": None, "C": 10000, "A": 10000, "research_progress": 0.4},
        {"id": "closedMonsterDeficit", "phase": None, "C": 10000, "A": 10000, "goblin_health": 1},
    ]
    cases = []
    for state in states:
        cases.append({"id": state["id"] + ".sameContext", "state": state,
                      "saved_context": current_hash, "loaded_context": current_hash,
                      "expected": "identity_noop_only_then_existing_recovery_barriers",
                      "automatic_rebind": False, "note": "Matching context does not bypass other state/authority/clock validation"})
        for change in changes:
            cases.append({"id": state["id"] + ".changed." + change["change"], "state": state,
                          "saved_context": current_hash, "loaded_context": change["context_sha256"],
                          "expected": "incompatible_no_automatic_rebind",
                          "repeated_reopen_attempts": 3,
                          "expected_each_attempt": "same_predecessor_hash_W_phase_and_effects; no grant, healing, drain or context write"})

    historical = []
    corpus = root / "Tools/Presentation/Fixtures/schema15-09a46810"
    manifest = {line.split("\t")[0]: line.split("\t")[1] for line in (corpus / "SHA256SUMS.tsv").read_text().splitlines()}
    checked = 0
    for name, expected in manifest.items():
        if name.endswith(".json"):
            assert sha((corpus / name).read_bytes()) == expected
            checked += 1
    for name in ["active-v1", "active-v2"]:
        raw = (corpus / (name + ".json")).read_bytes()
        data = json.loads(raw)
        primary = data["primary"]
        scheduler = primary["livingDungeon"]["Scheduler"]
        party = primary["livingDungeon"]["Parties"][0]
        checkpoint = base64.b64decode("".join(party["CheckpointChunks"]))
        assert primary["lastSavedUtcUnix"] == 0 and scheduler["AcceptedThrough"] > scheduler["ClosedThrough"]
        assert raw.count(b'"lastSavedUtcUnix":0') == 1 and raw.endswith(b"}}")
        derived15 = raw.replace(b'"lastSavedUtcUnix":0', b'"lastSavedUtcUnix":1', 1)
        derived16 = derived15.replace(b'"schemaVersion":15', b'"schemaVersion":16', 1)
        derived16 = derived16[:-2] + b',"timeProvenance":{"FormatVersion":1,"Mode":"legacyWholeSeconds","AccountedFraction100ns":0}' + derived16[-2:]
        for schema, content in [(15, derived15), (16, derived16)]:
            fixture_path = output.parent / (name + ".test-only-schema" + str(schema) + ".json")
            with fixture_path.open("xb") as handle:
                handle.write(content)
            assert json.loads(content)["primary"]["livingDungeon"] == primary["livingDungeon"]
            historical.append({"id": name + ".derivedSchema" + str(schema), "fixture": str(fixture_path.relative_to(root)),
                               "sha256": sha(content), "historical_producer_capture": False,
                               "expected_activation": "reject_unproven_original_global_context",
                               "reason": "Explicit fake timestamp/new provenance do not attest old tick, Research or future-admission loot inputs"})
        historical.append({"id": name + ".original", "source": str((corpus / (name + ".json")).relative_to(root)),
                           "sha256": sha(raw), "checkpoint_format": struct.unpack("<i", checkpoint[:4])[0],
                           "C": scheduler["ClosedThrough"], "A": scheduler["AcceptedThrough"],
                           "retained_historical_reader_verdict": "positive_with_registered_profile",
                           "expected_activation": "reject_missing_boundary_and_unproven_global_context"})

    archive_path = root / "Docs/testing/evidence/h1-f2/qualification-reports.zip"
    archive_rows = []
    with zipfile.ZipFile(archive_path) as archive:
        source_map = {row["path"].replace("\\", "/"): row["sha256"].lower()
                      for row in json.loads(archive.read("windows-source.json"))}
        matching = sum(source_map.get(name) == sha(data) for name, data in zip(asset_names, inputs))
        assert matching == 12
        for name in archive.namelist():
            if not name.startswith("windows-") or not name.endswith("-save.json"):
                continue
            raw = archive.read(name)
            save = json.loads(raw)
            scheduler = save["primary"]["livingDungeon"]["Scheduler"]
            archive_rows.append({"member": name, "sha256": sha(raw), "schema": save["schemaVersion"],
                                 "C": scheduler["ClosedThrough"], "A": scheduler["AcceptedThrough"],
                                 "last_saved": save["primary"]["lastSavedUtcUnix"]})
        producer = json.loads(archive.read("windows-build.provenance.json"))["source"]

    result = {
        "purpose": "Compatibility review fixture matrix; expected decisions are not executed runtime results",
        "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
        "generator_sha256": sha(Path(__file__).read_bytes()), "input_order": asset_names,
        "exact_checkout_input_hashes": {name: sha(data) for name, data in zip(asset_names, inputs)},
        "context_v3_asset_byte_fixture_hash": current_hash,
        "asset_hash_limit": "No Unity import/validation run; manifest/catalog raw assets stand for their canonical loaded bytes in this fixture",
        "changed_config_variants": changes, "modern_semantic_cases": cases,
        "historical_cases": historical, "historical_corpus_files_hash_verified": checked,
        "historical_archive": {"path": str(archive_path.relative_to(root)), "sha256": sha(archive_path.read_bytes()),
                               "build_source_record": producer, "matching_project_input_hashes": matching,
                               "save_snapshots": archive_rows,
                               "positive_evidence": "Recorded producer source, project inputs and closed save artifacts; not a generic runtime historical-context authority",
                               "has_archived_A_gt_C_save": any(row["A"] > row["C"] for row in archive_rows)},
        "runtime_tests_executed": False, "unity_tests_executed": False,
        "no_production_legacy_proof_provider_invented": True,
    }
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"output": str(output), "sha256": sha(output.read_bytes()),
                      "semantic_cases": len(cases), "historical_cases": len(historical),
                      "corpus_hashes_verified": checked, "archive_has_A_gt_C": result["historical_archive"]["has_archived_A_gt_C_save"]}))


if __name__ == "__main__":
    main()
