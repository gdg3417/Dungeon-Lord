"""Disposable preparation/runner tests. No Unity, real owner save or campaign runs."""
import json
import os
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile
import qualify_h1_f4 as q
import qualification_supervisor as supervisor


class H1F4CampaignTests(unittest.TestCase):
    def setUp(self):
        parent = q.ROOT / "TestResults/h1-f4-tool-fixtures"
        parent.mkdir(parents=True, exist_ok=True)
        temporary = tempfile.TemporaryDirectory(prefix="owned-", dir=parent)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.stage = self.root / "TestResults/qualification-supervisor/disposable/focused"
        self.stage.mkdir(parents=True)
        font = self.root / q.FONT
        font.parent.mkdir(parents=True)
        font.write_bytes(b"test-only original font")

    def test_child_keyring_override_is_local_and_command_is_exact(self):
        before = os.environ.get("UNITY_NO_AUTH_BROKER")
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as run:
            self.assertEqual(0, q.run_test("test", self.stage))
        self.assertEqual(before, os.environ.get("UNITY_NO_AUTH_BROKER"))
        self.assertEqual("1", run.call_args.kwargs["env"]["UNITY_NO_AUTH_BROKER"])
        self.assertEqual([str(q.CLI), "test", str(self.root), "--mode", "EditMode", "--filter",
                          "DungeonBuilder.M0.EditorTools.Tests.H1F4RecoveryContract", "--output", str(self.stage / "results.xml"),
                          "--timeout", "900", "--no-color", "--", "-logFile", str(self.stage / "unity.log")], run.call_args.args[0])
        self.assertNotIn("shell", run.call_args.kwargs)

    def test_cli_failure_propagates_with_unchanged_inputs(self):
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=8)):
            self.assertEqual(8, q.run_test("test", self.stage))
        self.assertEqual(8, json.loads((self.stage / "cli-exit.json").read_text())["exit_code"])

    def test_invalid_stage_cannot_launch(self):
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run") as run:
            with self.assertRaises(ValueError):
                q.run_test("test", self.root)
            run.assert_not_called()

    def test_unknown_input_change_is_preserved_without_restoration(self):
        path = self.root / "Assets/unexpected.txt"
        path.write_bytes(b"original test bytes")
        def child(*args, **kwargs):
            path.write_bytes(b"changed test bytes")
            return SimpleNamespace(returncode=0)
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", side_effect=child):
            with self.assertRaisesRegex(ValueError, "Unapproved input change"):
                q.run_test("test", self.stage)
        self.assertEqual(b"changed test bytes", path.read_bytes())
        self.assertFalse(json.loads((self.stage / "generated-inputs/manifest.json").read_text())["restored"])

    def test_plan_has_unique_commands_and_complete_preservation_dependencies(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("test")
            prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {})
            q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), \
                    patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("test")
            plan = json.loads((prep / "plan.json").read_text())
            supervisor.validate_plan(plan)
            self.assertEqual(30, plan["stages"][1]["expected_tests"])
            self.assertEqual([], plan["stages"][1]["allowed_skips"])
            self.assertEqual(set(plan["selections"]["focused"][:-1]), set(plan["stages"][-1]["depends"]))

    def test_settings_restore_is_exact_and_preserves_other_input(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("restoretest")
            prep.mkdir(parents=True)
            originals = {name: ("original test " + name + "\r\n").encode() for name in q.SETTINGS}
            for name, data in originals.items():
                path = self.root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
            q.write(prep / "original-inputs.json", q.inventory(self.root))
            with zipfile.ZipFile(prep / "original-inputs.zip", "x") as archive:
                for name in q.SETTINGS:
                    archive.write(self.root / name, name)
            for name in q.SETTINGS:
                (self.root / name).write_bytes(b"temporary test settings\n")
            q.write(prep / "qualified-inputs.json", q.inventory(self.root))
            with patch.object(q, "check_protected"):
                q.restore("restoretest", self.stage)
            for name, data in originals.items():
                self.assertEqual(data, (self.root / name).read_bytes())
            self.assertEqual(b"test-only original font", (self.root / q.FONT).read_bytes())


if __name__ == "__main__":
    unittest.main()
