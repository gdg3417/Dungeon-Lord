"""Explicit H1-F4 campaign preparation; execution/ownership remains the approved supervisor."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import zipfile
import qualification_inputs as inputs
import qualification_windows as win

ROOT = Path(__file__).resolve().parents[2]
OWNER = Path("C:/Dev/Dungeon-Lord")
CLI = Path("C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe")
EDITOR = Path("C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Unity.exe")
SETTINGS = ["ProjectSettings/ProjectSettings.asset", "ProjectSettings/EditorBuildSettings.asset", "ProjectSettings/ShaderGraphSettings.asset"]
FONT = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
FIXTURE_FILTER = ";".join(["DungeonBuilder.M0.EditorTools.Tests.H1F4RecoveryContract",
    "DungeonBuilder.M0.Editor.Tests.PhaseFourSpatial", "DungeonBuilder.M0.Editor.Tests.PhaseFourWrites"])
RESEARCH_FILTER = FIXTURE_FILTER + ";" + ";".join([
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.R0ResearchDurability",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.R0ResearchSaveIntegration"])
LIVING_FILTER = RESEARCH_FILTER + ";" + ";".join([
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B2",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonTwoFloor"])
CONFIGURATION_REPRO_FILTER = ";".join([
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B1.SchemaSeventeenWriterRejectsReloadedConfigurationBeforeCandidatePreparation",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B1.EveryCommittedPrefixRestoresExactFutureReceiptsAndOutcome"])
INSTRUMENTATION_FILTER = ";".join([
    "DungeonBuilder.M0.Editor.Tests.AdmissionStartReconstruction",
    "DungeonBuilder.M0.Editor.Tests.ForegroundPerformanceDiagnostics"])
SCENE_REPRO_FILTER = ";".join([
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.RoomContentMapReassignmentCapacityMoveOwnedAndDiscard",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish"])

SCENE_CLOCK_FILTER = SCENE_REPRO_FILTER + ";" + ";".join([
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.SceneClock",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.RoomContentLootRejectionReasons",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.RoomContentMapReassignmentCategoriesInvalidRetryAndSave"])

SCENE_LIFECYCLE_FILTER = ";".join("DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene." + name for name in (
    "LivingDungeonB3B2", "R0Research", "AnalysisDoesNotInterruptAuthoritativeForegroundAdmissionAndSettlement"))

SCENE_FIXES_FILTER = ";".join("DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene." + name for name in (
    "LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState",
    "LivingDungeonB3B2SuspendedDrainRecognizesItsDurablyPersistedPauseBoundary",
    "LivingDungeonB3B2UncertainRollbackKeepsRealRootRecoveryLatch"))

SCENE_PLAY_FILTER = ";".join("DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests." + name for name in (
    "LivingDungeonB3B2", "SceneClock", "R0Research", "AnalysisForegroundContinuity",
    "RoomContentLootRejectionReasons", "RoomContentMapReassignmentCapacity", "RoomContentMapReassignmentCategories"))

SHARED_REPLAY_FILTER = ";".join((
    "DungeonBuilder.M0.Editor.Tests.PhaseFiveBBranchIntegration",
    "DungeonBuilder.M0.EditorTools.Tests.CombatC1ReplayEditModeFixtures",
    "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.VisualRunReplay"))


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write(path, value):
    with Path(path).open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, indent=2, sort_keys=True)
        stream.write("\n")


def inventory(root):
    return {name: sha(root / name) for name in sorted(inputs.target_inventory(root))}


def tree(root):
    inputs.no_redirect(root)
    if not root.exists():
        return {}
    result = {}
    for path in sorted(root.rglob("*")):
        inputs.no_redirect(path)
        if path.is_file():
            result[str(path.relative_to(root))] = sha(path)
    return result


def identities(attempt):
    if not re.fullmatch(r"[a-z0-9-]{1,40}", attempt):
        raise ValueError("Explicit safe attempt required")
    prep = ROOT / "TestResults" / ("h1-f4-20261027-" + attempt)
    product = "Dungeon Lord B3B2 Smoke H1F4 20261027 " + attempt
    save = Path.home() / "AppData/LocalLow/gdg3417" / product
    return prep, product, save


def protected(save):
    prior = Path.home() / ".codex/worktrees/h1-f3-stable-recovery/Dungeon-Lord/TestResults"
    executable = Path.home() / ".codex/worktrees/h1-f2-time-provenance/Dungeon-Lord/Builds/Development/Windows-h1-f2-headroom/Dungeon Lord.exe"
    return {
        "owner_proposal": sha(OWNER / "Docs/planning/living-dungeon-hybrid-persistence-proposal.md"),
        "owner_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=OWNER, text=True).strip(),
        "owner_default_test_xml": sha(OWNER / "TestResults.xml") if (OWNER / "TestResults.xml").exists() else None,
        "other_saves": {p.name: tree(p) for p in sorted(save.parent.iterdir()) if p.is_dir() and p != save},
        "stopped_h1f3_original": tree(prior / "qualification-supervisor/b343351f516b4a19b8992ad00910ae40"),
        "stopped_h1f3_replacement": tree(prior / "qualification-supervisor/b227e498d4ef4f2a91cd7801fb915881"),
        "historical_corpus": tree(ROOT / "Tools/Presentation/Fixtures"),
        "retained_review_evidence": tree(ROOT / "Docs/testing/evidence"),
        "historical_two_floor_inputs": tree(ROOT / "TestResults/living-dungeon-two-floor-20261020/inputs"),
        "qualified_f2_executable": sha(executable) if executable.exists() else None,
    }


def check_protected(prep, save):
    if protected(save) != json.loads((prep / "protected.json").read_text()):
        raise ValueError("Protected owner/save/historical evidence changed")


def qualification_player_settings(original, product):
    # Measured d16c05a4 input, not a general YAML canonicalizer. Any source
    # change requires qualification of its preparation instead of normalizing it.
    if hashlib.sha256(original).hexdigest() != "34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993":
        raise ValueError("Unqualified original PlayerSettings bytes")
    if not re.fullmatch(r"Dungeon Lord B3B2 Smoke H1F4 20261027 [a-z0-9-]{1,40}", product):
        raise ValueError("Unexpected isolated product identity")
    owner = b"  productName: Dungeon Lord\r\n"
    before = (b"  applicationIdentifier:\r\n"
              b"    Standalone: com.gdg3417.dungeonlord\r\n"
              b"    Android: com.gdg3417.dungeonlord\r\n")
    after = (b"  applicationIdentifier:\n"
             b"    Android: com.gdg3417.dungeonlord\n"
             b"    Standalone: com.gdg3417.dungeonlord\n")
    if original.count(owner) != 1 or original.count(before) != 1:
        raise ValueError("Unexpected measured PlayerSettings structure")
    isolated = ("  productName: " + product + "\n").encode("utf-8")
    prepared = original.replace(b"\r\n", b"\n").replace(owner.replace(b"\r\n", b"\n"), isolated)
    prepared = prepared.replace(before.replace(b"\r\n", b"\n"), after)
    # Independently reverse precisely the declared edits. Nothing else may move.
    restored = prepared.replace(after, before.replace(b"\r\n", b"\n")).replace(isolated, owner.replace(b"\r\n", b"\n"))
    if b"\r" in prepared or restored.replace(b"\n", b"\r\n") != original:
        raise ValueError("Unexpected PlayerSettings preparation difference")
    return prepared


def prepare(attempt):
    prep, product, save = identities(attempt)
    if ROOT == OWNER or "/.codex/worktrees/h1-f4-recovery/" not in ROOT.as_posix():
        raise ValueError("Require the existing managed H1-F4 worktree")
    win.assert_no_unity()
    if prep.exists() or save.exists():
        raise ValueError("Unused preparation/product/save identities required")
    prep.mkdir(parents=True)
    write(prep / "protected.json", protected(save))
    write(prep / "original-inputs.json", inventory(ROOT))
    with zipfile.ZipFile(prep / "original-inputs.zip", "x", zipfile.ZIP_DEFLATED) as archive:
        for name in inventory(ROOT):
            archive.write(ROOT / name, name)
    path = ROOT / SETTINGS[0]
    path.write_bytes(qualification_player_settings(path.read_bytes(), product))
    for name in SETTINGS[1:]:
        path = ROOT / name
        path.write_bytes(path.read_bytes().replace(b"\r\n", b"\n"))
    write(prep / "qualified-inputs.json", inventory(ROOT))
    write(prep / "inventory.json", {"owner": "H1-F4 qualification", "purpose": "isolated focused contract gate",
          "retention": "preserve", "project": str(ROOT), "preparation": str(prep),
          "product": product, "save": str(save), "build": None,
          "generated_project_paths": [str(ROOT / "Library"), str(ROOT / "Temp"), str(ROOT / "Logs")],
          "player_settings_preparation": {
              "measured_campaign": "d16c05a430f7404189ab22034383ec50",
              "original_sha256": json.loads((prep / "original-inputs.json").read_text())[SETTINGS[0]],
              "qualified_sha256": sha(ROOT / SETTINGS[0]),
              "edits": ["isolated productName", "CRLF to LF", "unchanged applicationIdentifier entries reordered Android then Standalone"]},
          "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()})
    print(json.dumps({"preparation": str(prep), "product": product, "save": str(save)}))


def plan(attempt, discovery=False, full_edit=False, fixtures=False, research=False, living=False, common_repro=False, configuration_repro=False, instrumentation=False, b3b1=False, scene_repro=False, scene_clock=False, scene_bootstrap=False, scene_lifecycle=False, scene_fixes=False, scene_play=False, shared_replay=False, monster_recovery=False):
    if sum((discovery, full_edit, fixtures, research, living, common_repro, configuration_repro, instrumentation, b3b1, scene_repro, scene_clock, scene_bootstrap, scene_lifecycle, scene_fixes, scene_play, shared_replay, monster_recovery)) > 1:
        raise ValueError("Choose one explicit workload")
    prep, product, save = identities(attempt)
    check_protected(prep, save)
    if inventory(ROOT) != json.loads((prep / "qualified-inputs.json").read_text()):
        raise ValueError("Inputs changed since preparation")
    helper = Path(__file__).resolve()
    git = Path(shutil.which("git")); powershell = Path(shutil.which("powershell"))
    pinned = [Path(sys.executable), CLI, EDITOR, git, powershell, helper, helper.with_name("qualification_supervisor.py"),
              helper.with_name("qualification_windows.py"), helper.with_name("qualification_inputs.py")]
    tools = {str(path): {"sha256": sha(path), "version": "H1-F4 explicit campaign; CLI beta.14; Unity 6000.3.2f1"} for path in pinned}
    tools[str(Path(sys.executable))]["version"] = sys.version
    tools[str(git)]["version"] = subprocess.check_output([str(git), "--version"], text=True).strip()
    tools[str(powershell)]["version"] = "Windows PowerShell 5.1; process-inspection dependency"
    command = lambda mode: [sys.executable, "-B", str(helper), mode, "--attempt", attempt, "--stage", "{stage}"]
    restored = json.loads((prep / "original-inputs.json").read_text())
    restored_id = hashlib.sha256(json.dumps(restored, sort_keys=True).encode()).hexdigest()
    name = "monster-recovery" if monster_recovery else "shared-replay" if shared_replay else "scene-play" if scene_play else "scene-fixes" if scene_fixes else "scene-lifecycle" if scene_lifecycle else "scene-bootstrap" if scene_bootstrap else "scene-clock" if scene_clock else "scene-repro" if scene_repro else "b3b1-regressions" if b3b1 else "instrumentation-regressions" if instrumentation else "configuration-repro" if configuration_repro else "common-repro" if common_repro else "inventory" if discovery else "full-edit" if full_edit else "living-regressions" if living else "research-regressions" if research else "fixture-regressions" if fixtures else "focused"
    workload = {"id": name, "role": "report" if discovery else "test", "depends": [name + "-preflight"],
                "command": command("run-monster-recovery" if monster_recovery else "run-shared-replay" if shared_replay else "run-scene-play" if scene_play else "run-scene-fixes" if scene_fixes else "run-scene-lifecycle" if scene_lifecycle else "run-scene-bootstrap" if scene_bootstrap else "run-scene-clock" if scene_clock else "run-scene-repro" if scene_repro else "run-b3b1" if b3b1 else "run-instrumentation" if instrumentation else "run-configuration-repro" if configuration_repro else "run-common-repro" if common_repro else "run-inventory" if discovery else "run-full-edit" if full_edit else "run-living" if living else "run-research" if research else "run-fixtures" if fixtures else "run-test"), "timeout_seconds": 3600 if full_edit else 960,
                "artifacts": ["unity.log", "invocation.json", "editor-exit.json" if discovery else "cli-exit.json", "generated-inputs"]}
    if discovery:
        workload["artifacts"].append("inventory.json")
    else:
        # Discovery at c1054632: 2768 EditMode leaves. The only subsequent
        # discovery changes are three root cases, one accepted-work case and
        # two shared fixture cases, one reentrant publication case and two
        # ordinary-save stall/background cases.
        # Explicit affected cohort: 111 recovery + 77 structural + 59 write
        # leaves. Original source-pinned full XML proves the latter two counts;
        # the renamed native-creation case does not change leaf count.
        # Research adds its 18 durable-acknowledgement and three save cases.
        # Living adds its 51 one-floor and 37 two-floor leaves; no Editor
        # sustained production-profile scene or Windows workload is selected.
        workload.update(xml="results.xml", expected_tests=40 if monster_recovery else 84 if shared_replay else 28 if scene_play else 3 if scene_fixes else 20 if scene_lifecycle else 1 if scene_bootstrap else 9 if scene_clock else 2 if scene_repro else 51 if b3b1 else 28 if instrumentation else 5 if configuration_repro else 2 if common_repro else 2783 if full_edit else 358 if living else 268 if research else 247 if fixtures else 111,
            allowed_skips=["DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed"] if full_edit else [])
    if scene_play or scene_fixes or scene_lifecycle or scene_repro or scene_clock or scene_bootstrap or b3b1 or living or full_edit: workload["artifacts"].append("fixture-inputs")
    stages = [
        {"id": name + "-preflight", "role": "preflight", "depends": [], "command": command("preflight")[:-1] + ["{campaign}/" + name + "-preflight"], "timeout_seconds": 120, "artifacts": ["preservation.json"]},
        workload,
        {"id": "restore-preflight", "role": "preflight", "depends": [name], "command": command("preflight")[:-1] + ["{campaign}/restore-preflight"], "timeout_seconds": 120, "artifacts": ["preservation.json"]},
        {"id": "restore", "role": "report", "depends": ["restore-preflight"], "command": command("restore"), "timeout_seconds": 120,
         "artifacts": ["restoration.json"], "after_input_identity": restored_id},
        {"id": "preservation", "role": "final", "depends": [name + "-preflight", name, "restore-preflight", "restore"],
         "command": command("finish"), "timeout_seconds": 120, "artifacts": ["preservation.json"], "input_identity": restored_id},
    ]
    data = {"purpose": "qualification", "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
            "tools": tools, "isolation": {"product": product, "save": str(save / "save_primary.json"), "project": str(ROOT)},
            "approved_substitutions": ["Temporary isolated productName; exact archived restoration. Only the measured original PlayerSettings bytes are prepared as LF with unchanged Android/Standalone identifier entries in measured order before sealing. The two other measured import-only LF settings rewrites are likewise presealed; all three original files are restored afterward.",
                                       "UNITY_NO_AUTH_BROKER=1 in the CLI child only, as documented by installed CLI and approved supervisor runbook. No ownership/drain exemption.",
                                       "Only the previously measured TMP fallback-font rewrite may be retained then restored after a zero-exit CLI; any other change fails closed."],
            "stages": stages, "selections": {name: [s["id"] for s in stages]}}
    if discovery:
        data["approved_substitutions"].append("Discovery only: pinned Editor directly, without -quit, for the asynchronous existing TestRunnerApi.RetrieveTestList callbacks; collector exits explicitly. No tests execute and no passing test gate is inferred.")
    write(prep / "plan.json", data)
    print(json.dumps({"plan": str(prep / "plan.json"), "tests": None if discovery else 40 if monster_recovery else 84 if shared_replay else 28 if scene_play else 3 if scene_fixes else 20 if scene_lifecycle else 1 if scene_bootstrap else 9 if scene_clock else 2 if scene_repro else 51 if b3b1 else 28 if instrumentation else 5 if configuration_repro else 2 if common_repro else 2783 if full_edit else 358 if living else 268 if research else 247 if fixtures else 111,
                      "skips": None if discovery else 1 if full_edit else 0}))


def preflight(attempt, stage, restored=False):
    prep, product, save = identities(attempt)
    check_protected(prep, save)
    expected = json.loads((prep / ("original-inputs.json" if restored else "qualified-inputs.json")).read_text())
    if inventory(ROOT) != expected:
        raise ValueError("Exact input applicability mismatch")
    win.assert_no_unity()
    write(Path(stage) / "preservation.json", {"passed": True, "inputs": len(expected), "product": product,
          "save": str(save), "original_inputs_restored": restored, "protected_evidence_unchanged": True})


def run_test(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", "DungeonBuilder.M0.EditorTools.Tests.H1F4RecoveryContract",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "focused", "cli-exit.json")


def run_fixtures(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", FIXTURE_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "fixture-regressions", "cli-exit.json")


def run_research(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", RESEARCH_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "research-regressions", "cli-exit.json")


def run_common_repro(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter",
               "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B2.SchemaSeventeenDirectPumpRequiresSourceBoundTimeCut",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "common-repro", "cli-exit.json")


def run_configuration_repro(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", CONFIGURATION_REPRO_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "configuration-repro", "cli-exit.json")


def run_instrumentation(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", INSTRUMENTATION_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "instrumentation-regressions", "cli-exit.json")


def run_b3b1(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter",
               "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B1",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "b3b1-regressions", "cli-exit.json")


def run_scene_repro(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", SCENE_REPRO_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-repro", "cli-exit.json")


def run_scene_clock(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", SCENE_CLOCK_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-clock", "cli-exit.json")


def run_scene_bootstrap(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter",
               "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.SceneClockNativeFractionAndOwnerRemainBound",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-bootstrap", "cli-exit.json")


def run_monster_recovery(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter",
               "DungeonBuilder.M0.EditorTools.Tests.CombatC2EditModeFixtures",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "monster-recovery", "cli-exit.json")


def run_shared_replay(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", SHARED_REPLAY_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "shared-replay", "cli-exit.json")


def run_scene_play(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "PlayMode", "--filter", SCENE_PLAY_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-play", "cli-exit.json")


def run_scene_fixes(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", SCENE_FIXES_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-fixes", "cli-exit.json")


def run_scene_lifecycle(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", SCENE_LIFECYCLE_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "scene-lifecycle", "cli-exit.json")


def run_living(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", LIVING_FILTER,
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "living-regressions", "cli-exit.json")


def run_full_edit(attempt, stage):
    stage = Path(stage).resolve()
    if any(os.environ.get(name) for name in ("DUNGEON_FOREGROUND_DIAGNOSTIC_OUTPUT_ROOT", "DUNGEON_FOREGROUND_DIAGNOSTIC_SOURCE",
                                           "DUNGEON_ADMISSION_BENCHMARK_ROOT", "DUNGEON_ADMISSION_BENCHMARK_SOURCE")):
        raise ValueError("Full regression requires optional exporter variables unset; use an explicit isolated benchmark plan")
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--output", str(stage / "results.xml"),
               "--timeout", "3500", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    return run_child(attempt, stage, command, "full-edit", "cli-exit.json")


def run_inventory(attempt, stage):
    stage = Path(stage).resolve()
    command = [str(EDITOR), "-batchmode", "-nographics", "-projectPath", str(ROOT), "-executeMethod",
               "DungeonBuilder.M0.EditorTools.H1F4QualificationInventory.Capture", "-h1F4Inventory", str(stage / "inventory.json"),
               "-logFile", str(stage / "unity.log")]
    code = run_child(attempt, stage, command, "inventory", "editor-exit.json")
    if code == 0:
        _, product, _ = identities(attempt)
        report = json.loads((stage / "inventory.json").read_text(encoding="utf-8"))
        if report["Product"] != product or Path(report["Project"]) != ROOT or report["UnityVersion"] != "6000.3.2f1":
            raise ValueError("Inventory identity mismatch")
        for mode in ("EditMode", "PlayMode"):
            cases = report[mode]
            if not cases or len({case["Id"] for case in cases}) != len(cases):
                raise ValueError("Missing or duplicate discovery identities")
    return code


def run_child(attempt, stage, command, expected_stage, exit_name):
    prep, _, _ = identities(attempt)
    stage = Path(stage).resolve()
    if stage.parent.parent != ROOT / "TestResults/qualification-supervisor" or stage.name != expected_stage:
        raise ValueError("Require the supervisor's exact owned stage")
    before = inventory(ROOT)
    font = (ROOT / FONT).read_bytes()
    environment = os.environ.copy()
    override = {"UNITY_NO_AUTH_BROKER": "1"} if command[0] == str(CLI) else {}
    if expected_stage in ("scene-play", "scene-fixes", "scene-lifecycle", "scene-repro", "scene-clock", "scene-bootstrap", "b3b1-regressions", "living-regressions", "full-edit"):
        # Fresh sealed artifact directory; old fixed-path fixture exports must
        # never be reused or overwritten. Override is local to this child only.
        export = stage / "fixture-inputs"; export.mkdir(exist_ok=False)
        write(export / "ownership.json", {"owner": "H1-F4 qualification", "retention": "preserve", "stage": str(stage),
              "source_sha": json.loads((prep / "inventory.json").read_text())["source_sha"],
              "purpose": "Campaign-owned exports and failed-scene snapshots; no extra files means none were produced"})
        override["DUNGEON_H1F4_TEST_EXPORT_ROOT"] = str(export)
    environment.update(override)
    write(stage / "invocation.json", {"command": command, "environment_override": override, "ownership": "Enclosing unchanged supervisor job"})
    result = subprocess.run(command, cwd=ROOT, env=environment, stdin=subprocess.DEVNULL)
    generated = stage / "generated-inputs"
    generated.mkdir()
    after = inventory(ROOT)
    changed = sorted(name for name in set(before) | set(after) if before.get(name) != after.get(name))
    restored = False
    if changed:
        if result.returncode != 0 or changed != [FONT] or after[FONT] != "f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52":
            write(generated / "manifest.json", {"changed": changed, "restored": False})
            raise ValueError("Unapproved input change; preserved without restoration")
        with (generated / "fallback-font.asset").open("xb") as handle:
            handle.write((ROOT / FONT).read_bytes())
        (ROOT / FONT).write_bytes(font)
        restored = True
        if inventory(ROOT) != before:
            raise ValueError("Exact font restoration failed")
    write(generated / "manifest.json", {"changed": changed, "restored": restored})
    write(stage / exit_name, {"exit_code": result.returncode})
    return result.returncode


def restore(attempt, stage):
    prep, _, save = identities(attempt)
    check_protected(prep, save)
    if inventory(ROOT) != json.loads((prep / "qualified-inputs.json").read_text()):
        raise ValueError("Changed inputs before restoration")
    with zipfile.ZipFile(prep / "original-inputs.zip") as archive:
        for name in SETTINGS:
            (ROOT / name).write_bytes(archive.read(name))
    if inventory(ROOT) != json.loads((prep / "original-inputs.json").read_text()):
        raise ValueError("Original inputs not restored")
    write(Path(stage) / "restoration.json", {"passed": True, "restored": SETTINGS})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["prepare", "plan", "plan-inventory", "plan-full-edit", "plan-fixtures", "plan-research", "plan-living", "plan-common-repro", "plan-configuration-repro", "plan-instrumentation", "plan-b3b1", "plan-scene-repro", "plan-scene-clock", "plan-scene-bootstrap", "plan-scene-lifecycle", "plan-scene-fixes", "plan-scene-play", "plan-shared-replay", "plan-monster-recovery", "preflight", "run-test", "run-inventory", "run-full-edit", "run-fixtures", "run-research", "run-living", "run-common-repro", "run-configuration-repro", "run-instrumentation", "run-b3b1", "run-scene-repro", "run-scene-clock", "run-scene-bootstrap", "run-scene-lifecycle", "run-scene-fixes", "run-scene-play", "run-shared-replay", "run-monster-recovery", "restore", "finish"])
    parser.add_argument("--attempt", required=True)
    parser.add_argument("--stage")
    args = parser.parse_args()
    if args.mode == "prepare": prepare(args.attempt)
    elif args.mode == "plan": plan(args.attempt)
    elif args.mode == "plan-inventory": plan(args.attempt, discovery=True)
    elif args.mode == "plan-full-edit": plan(args.attempt, full_edit=True)
    elif args.mode == "plan-fixtures": plan(args.attempt, fixtures=True)
    elif args.mode == "plan-research": plan(args.attempt, research=True)
    elif args.mode == "plan-living": plan(args.attempt, living=True)
    elif args.mode == "plan-common-repro": plan(args.attempt, common_repro=True)
    elif args.mode == "plan-configuration-repro": plan(args.attempt, configuration_repro=True)
    elif args.mode == "plan-instrumentation": plan(args.attempt, instrumentation=True)
    elif args.mode == "plan-b3b1": plan(args.attempt, b3b1=True)
    elif args.mode == "plan-scene-repro": plan(args.attempt, scene_repro=True)
    elif args.mode == "plan-monster-recovery": plan(args.attempt, monster_recovery=True)
    elif args.mode == "plan-shared-replay": plan(args.attempt, shared_replay=True)
    elif args.mode == "plan-scene-play": plan(args.attempt, scene_play=True)
    elif args.mode == "plan-scene-fixes": plan(args.attempt, scene_fixes=True)
    elif args.mode == "plan-scene-lifecycle": plan(args.attempt, scene_lifecycle=True)
    elif args.mode == "plan-scene-clock": plan(args.attempt, scene_clock=True)
    elif args.mode == "plan-scene-bootstrap": plan(args.attempt, scene_bootstrap=True)
    elif args.mode == "run-test": raise SystemExit(run_test(args.attempt, args.stage))
    elif args.mode == "run-inventory": raise SystemExit(run_inventory(args.attempt, args.stage))
    elif args.mode == "run-full-edit": raise SystemExit(run_full_edit(args.attempt, args.stage))
    elif args.mode == "run-fixtures": raise SystemExit(run_fixtures(args.attempt, args.stage))
    elif args.mode == "run-research": raise SystemExit(run_research(args.attempt, args.stage))
    elif args.mode == "run-living": raise SystemExit(run_living(args.attempt, args.stage))
    elif args.mode == "run-common-repro": raise SystemExit(run_common_repro(args.attempt, args.stage))
    elif args.mode == "run-configuration-repro": raise SystemExit(run_configuration_repro(args.attempt, args.stage))
    elif args.mode == "run-instrumentation": raise SystemExit(run_instrumentation(args.attempt, args.stage))
    elif args.mode == "run-b3b1": raise SystemExit(run_b3b1(args.attempt, args.stage))
    elif args.mode == "run-scene-repro": raise SystemExit(run_scene_repro(args.attempt, args.stage))
    elif args.mode == "run-monster-recovery": raise SystemExit(run_monster_recovery(args.attempt, args.stage))
    elif args.mode == "run-shared-replay": raise SystemExit(run_shared_replay(args.attempt, args.stage))
    elif args.mode == "run-scene-play": raise SystemExit(run_scene_play(args.attempt, args.stage))
    elif args.mode == "run-scene-fixes": raise SystemExit(run_scene_fixes(args.attempt, args.stage))
    elif args.mode == "run-scene-lifecycle": raise SystemExit(run_scene_lifecycle(args.attempt, args.stage))
    elif args.mode == "run-scene-clock": raise SystemExit(run_scene_clock(args.attempt, args.stage))
    elif args.mode == "run-scene-bootstrap": raise SystemExit(run_scene_bootstrap(args.attempt, args.stage))
    elif args.mode == "restore": restore(args.attempt, args.stage)
    else: preflight(args.attempt, args.stage, restored=args.mode == "finish")
