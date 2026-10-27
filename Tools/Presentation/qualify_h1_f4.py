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
        "qualified_f2_executable": sha(executable) if executable.exists() else None,
    }


def check_protected(prep, save):
    if protected(save) != json.loads((prep / "protected.json").read_text()):
        raise ValueError("Protected owner/save/historical evidence changed")


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
    text = path.read_bytes().decode("utf-8-sig")
    if text.count("  productName: Dungeon Lord\n") + text.count("  productName: Dungeon Lord\r\n") != 1:
        raise ValueError("Unexpected owner product identity")
    path.write_bytes(text.replace("  productName: Dungeon Lord", "  productName: " + product).encode("utf-8"))
    for name in SETTINGS[1:]:
        path = ROOT / name
        path.write_bytes(path.read_bytes().replace(b"\r\n", b"\n"))
    write(prep / "qualified-inputs.json", inventory(ROOT))
    write(prep / "inventory.json", {"owner": "H1-F4 qualification", "purpose": "isolated focused contract gate",
          "retention": "preserve", "project": str(ROOT), "preparation": str(prep),
          "product": product, "save": str(save), "build": None,
          "generated_project_paths": [str(ROOT / "Library"), str(ROOT / "Temp"), str(ROOT / "Logs")],
          "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()})
    print(json.dumps({"preparation": str(prep), "product": product, "save": str(save)}))


def plan(attempt):
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
    stages = [
        {"id": "focused-preflight", "role": "preflight", "depends": [], "command": command("preflight")[:-1] + ["{campaign}/focused-preflight"], "timeout_seconds": 120, "artifacts": ["preservation.json"]},
        {"id": "focused", "role": "test", "depends": ["focused-preflight"], "command": command("run-test"), "timeout_seconds": 960,
         "xml": "results.xml", "expected_tests": 64, "allowed_skips": [], "artifacts": ["unity.log", "invocation.json", "cli-exit.json", "generated-inputs"]},
        {"id": "restore-preflight", "role": "preflight", "depends": ["focused"], "command": command("preflight")[:-1] + ["{campaign}/restore-preflight"], "timeout_seconds": 120, "artifacts": ["preservation.json"]},
        {"id": "restore", "role": "report", "depends": ["restore-preflight"], "command": command("restore"), "timeout_seconds": 120,
         "artifacts": ["restoration.json"], "after_input_identity": restored_id},
        {"id": "preservation", "role": "final", "depends": ["focused-preflight", "focused", "restore-preflight", "restore"],
         "command": command("finish"), "timeout_seconds": 120, "artifacts": ["preservation.json"], "input_identity": restored_id},
    ]
    data = {"purpose": "qualification", "source_sha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
            "tools": tools, "isolation": {"product": product, "save": str(save / "save_primary.json"), "project": str(ROOT)},
            "approved_substitutions": ["Temporary isolated productName; exact archived restoration. The two measured import-only LF settings rewrites are applied before sealing and restored afterward.",
                                       "UNITY_NO_AUTH_BROKER=1 in the CLI child only, as documented by installed CLI and approved supervisor runbook. No ownership/drain exemption.",
                                       "Only the previously measured TMP fallback-font rewrite may be retained then restored after a zero-exit CLI; any other change fails closed."],
            "stages": stages, "selections": {"focused": [s["id"] for s in stages]}}
    write(prep / "plan.json", data)
    print(json.dumps({"plan": str(prep / "plan.json"), "tests": 64, "skips": 0}))


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
    prep, _, _ = identities(attempt)
    stage = Path(stage).resolve()
    if stage.parent.parent != ROOT / "TestResults/qualification-supervisor" or stage.name != "focused":
        raise ValueError("Require the supervisor's exact owned stage")
    before = inventory(ROOT)
    font = (ROOT / FONT).read_bytes()
    command = [str(CLI), "test", str(ROOT), "--mode", "EditMode", "--filter", "DungeonBuilder.M0.EditorTools.Tests.H1F4RecoveryContract",
               "--output", str(stage / "results.xml"), "--timeout", "900", "--no-color", "--", "-logFile", str(stage / "unity.log")]
    environment = os.environ.copy()
    environment["UNITY_NO_AUTH_BROKER"] = "1"
    write(stage / "invocation.json", {"command": command, "environment_override": {"UNITY_NO_AUTH_BROKER": "1"}, "ownership": "Enclosing unchanged supervisor job"})
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
    write(stage / "cli-exit.json", {"exit_code": result.returncode})
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
    parser.add_argument("mode", choices=["prepare", "plan", "preflight", "run-test", "restore", "finish"])
    parser.add_argument("--attempt", required=True)
    parser.add_argument("--stage")
    args = parser.parse_args()
    if args.mode == "prepare": prepare(args.attempt)
    elif args.mode == "plan": plan(args.attempt)
    elif args.mode == "run-test": raise SystemExit(run_test(args.attempt, args.stage))
    elif args.mode == "restore": restore(args.attempt, args.stage)
    else: preflight(args.attempt, args.stage, restored=args.mode == "finish")
