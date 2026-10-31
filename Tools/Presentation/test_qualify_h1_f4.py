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
    @staticmethod
    def measured_settings():
        # Read the verified source; every operation below uses bytes/disposable
        # files. Never write the project/owner settings from these tests.
        return (Path(__file__).resolve().parents[2] / q.SETTINGS[0]).read_bytes()

    def test_measured_settings_preparation_reproduces_exact_playmode_bytes(self):
        original = self.measured_settings()
        prepared = q.qualification_player_settings(original,
            "Dungeon Lord B3B2 Smoke H1F4 20261027 complete-scene-play01")
        self.assertEqual("9748e7d76082d943f194386d65ed978175f30fe013f96f360815eb82df4ea56d",
                         q.hashlib.sha256(prepared).hexdigest())
        self.assertNotIn(b"\r", prepared)
        self.assertEqual(original, self.measured_settings())

    def test_measured_settings_reject_changed_order_values_structure_and_other_bytes(self):
        original = self.measured_settings()
        first = b"    Standalone: com.gdg3417.dungeonlord\r\n    Android: com.gdg3417.dungeonlord\r\n"
        reverse = b"    Android: com.gdg3417.dungeonlord\r\n    Standalone: com.gdg3417.dungeonlord\r\n"
        values = {"order": original.replace(first, reverse),
                  "identifier": original.replace(b"com.gdg3417.dungeonlord", b"com.unproven.other", 1),
                  "structure": original.replace(first, first + first),
                  "additional_difference": original + b"# unexpected source change\r\n",
                  "line_endings": original.replace(b"\r\n", b"\n")}
        for name, value in values.items():
            with self.subTest(name=name):
                self.assertNotEqual(original, value)
                with self.assertRaisesRegex(ValueError, "Unqualified original"):
                    q.qualification_player_settings(value, "Dungeon Lord B3B2 Smoke H1F4 20261027 disposable")

    def test_measured_settings_reject_nonisolated_or_injected_product(self):
        for product in ("Dungeon Lord", "Dungeon Lord B3B2 Smoke H1F4 20261027 invalid\nfield: value", ""):
            with self.subTest(product=product), self.assertRaisesRegex(ValueError, "isolated product"):
                q.qualification_player_settings(self.measured_settings(), product)

    def test_measured_preparation_archive_and_restoration_are_exact(self):
        original = self.measured_settings()
        with patch.object(q, "ROOT", self.root):
            prep, product, _ = q.identities("measured-restore"); prep.mkdir(parents=True)
            originals = {name: original if name == q.SETTINGS[0] else ("disposable " + name + "\r\n").encode()
                         for name in q.SETTINGS}
            for name, data in originals.items():
                path = self.root / name; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(data)
            before = q.inventory(self.root); q.write(prep / "original-inputs.json", before)
            with zipfile.ZipFile(prep / "original-inputs.zip", "x") as archive:
                for name in q.SETTINGS: archive.write(self.root / name, name)
            archive_hash = q.sha(prep / "original-inputs.zip")
            (self.root / q.SETTINGS[0]).write_bytes(q.qualification_player_settings(original, product))
            for name in q.SETTINGS[1:]:
                (self.root / name).write_bytes(originals[name].replace(b"\r\n", b"\n"))
            q.write(prep / "qualified-inputs.json", q.inventory(self.root))
            with patch.object(q, "check_protected"): q.restore("measured-restore", self.stage)
            self.assertEqual(before, q.inventory(self.root))
            self.assertEqual(archive_hash, q.sha(prep / "original-inputs.zip"))
            for name in q.SETTINGS: self.assertEqual(originals[name], (self.root / name).read_bytes())

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

    def test_unexpected_postseal_settings_change_is_still_preserved_and_rejected(self):
        prepared = q.qualification_player_settings(self.measured_settings(),
            "Dungeon Lord B3B2 Smoke H1F4 20261027 disposable")
        path = self.root / q.SETTINGS[0]; path.parent.mkdir(parents=True); path.write_bytes(prepared)
        changed = prepared.replace(b"com.gdg3417.dungeonlord", b"com.unproven.other", 1)
        def child(*args, **kwargs):
            path.write_bytes(changed)
            return SimpleNamespace(returncode=0)
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", side_effect=child):
            with self.assertRaisesRegex(ValueError, "Unapproved input change"):
                q.run_test("test", self.stage)
        self.assertEqual(changed, path.read_bytes())
        self.assertEqual({"changed": [q.SETTINGS[0]], "restored": False},
                         json.loads((self.stage / "generated-inputs/manifest.json").read_text()))

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
            self.assertEqual(111, plan["stages"][1]["expected_tests"])
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

    def test_inventory_plan_is_report_only_with_full_preservation(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("inventorytest"); prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {}); q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("inventorytest", discovery=True)
            plan = json.loads((prep / "plan.json").read_text()); supervisor.validate_plan(plan)
            self.assertEqual("report", plan["stages"][1]["role"])
            self.assertNotIn("expected_tests", plan["stages"][1]); self.assertNotIn("xml", plan["stages"][1])
            self.assertEqual(set(plan["selections"]["inventory"][:-1]), set(plan["stages"][-1]["depends"]))

    def test_inventory_uses_pinned_editor_without_quit_and_never_runs_tests(self):
        stage = self.stage.parent / "inventory"; stage.mkdir()
        with patch.object(q, "ROOT", self.root):
            _, product, _ = q.identities("inventorytest")
            def child(command, **kwargs):
                report = {"Product": product, "Project": str(self.root), "UnityVersion": "6000.3.2f1",
                    "EditMode": [{"Id": "edit-1", "Name": "\u500f"}], "PlayMode": [{"Id": "play-1"}]}
                (stage / "inventory.json").write_bytes(json.dumps(report, ensure_ascii=False).encode("utf-8"))
                return SimpleNamespace(returncode=0)
            with patch.object(q.subprocess, "run", side_effect=child) as run:
                self.assertEqual(0, q.run_inventory("inventorytest", stage))
            command = run.call_args.args[0]
            self.assertEqual(str(q.EDITOR), command[0]); self.assertIn("-executeMethod", command)
            self.assertNotIn("-quit", command); self.assertNotIn("-runTests", command)
            self.assertEqual({}, json.loads((stage / "invocation.json").read_text())["environment_override"])
            self.assertTrue((stage / "editor-exit.json").exists())

    def test_full_edit_selects_every_case_without_a_filter(self):
        stage = self.stage.parent / "full-edit"; stage.mkdir()
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as run:
            prep, _, _ = q.identities("fulltest"); prep.mkdir(parents=True)
            q.write(prep / "inventory.json", {"source_sha": "b" * 40})
            self.assertEqual(0, q.run_full_edit("fulltest", stage))
        self.assertNotIn("--filter", run.call_args.args[0]); self.assertIn("3500", run.call_args.args[0])

    def test_fixture_cohort_selects_only_the_three_reviewed_fixtures(self):
        stage = self.stage.parent / "fixture-regressions"; stage.mkdir()
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as run:
            self.assertEqual(0, q.run_fixtures("fixtures", stage))
        command = run.call_args.args[0]
        self.assertEqual(";".join(["DungeonBuilder.M0.EditorTools.Tests.H1F4RecoveryContract",
            "DungeonBuilder.M0.Editor.Tests.PhaseFourSpatial", "DungeonBuilder.M0.Editor.Tests.PhaseFourWrites"]),
            command[command.index("--filter") + 1])

    def test_fixture_plan_keeps_exact_count_and_all_preservation_stages(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("fixturetest"); prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {}); q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("fixturetest", fixtures=True)
            plan = json.loads((prep / "plan.json").read_text()); supervisor.validate_plan(plan)
            self.assertEqual(247, plan["stages"][1]["expected_tests"])
            self.assertEqual([], plan["stages"][1]["allowed_skips"])
            self.assertEqual(set(plan["selections"]["fixture-regressions"][:-1]), set(plan["stages"][-1]["depends"]))

    def test_research_cohort_adds_only_the_two_durable_research_fixtures(self):
        stage = self.stage.parent / "research-regressions"; stage.mkdir()
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as run:
            self.assertEqual(0, q.run_research("research", stage))
        command = run.call_args.args[0]
        self.assertEqual(q.FIXTURE_FILTER + ";DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.R0ResearchDurability;" +
            "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.R0ResearchSaveIntegration", command[command.index("--filter") + 1])

    def test_research_plan_keeps_exact_count_and_preservation(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("researchtest"); prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {}); q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("researchtest", research=True)
            plan = json.loads((prep / "plan.json").read_text()); supervisor.validate_plan(plan)
            self.assertEqual(268, plan["stages"][1]["expected_tests"])
            self.assertEqual([], plan["stages"][1]["allowed_skips"])
            self.assertEqual(set(plan["selections"]["research-regressions"][:-1]), set(plan["stages"][-1]["depends"]))

    def test_full_edit_plan_declares_exact_discovery_and_one_named_inverse_skip(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("fulltest"); prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {}); q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("fulltest", full_edit=True)
            plan = json.loads((prep / "plan.json").read_text()); supervisor.validate_plan(plan)
            self.assertEqual(2783, plan["stages"][1]["expected_tests"])
            self.assertEqual(["DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed"],
                             plan["stages"][1]["allowed_skips"])

    def test_living_cohort_exports_only_into_fresh_child_owned_artifacts(self):
        stage = self.stage.parent / "living-regressions"; stage.mkdir()
        before = os.environ.get("DUNGEON_H1F4_TEST_EXPORT_ROOT")
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as run:
            prep, _, _ = q.identities("living"); prep.mkdir(parents=True)
            q.write(prep / "inventory.json", {"source_sha": "b" * 40})
            self.assertEqual(0, q.run_living("living", stage))
        command = run.call_args.args[0]
        self.assertEqual(q.RESEARCH_FILTER + ";DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonB3B2;" +
            "DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.LivingDungeonTwoFloor", command[command.index("--filter") + 1])
        self.assertEqual(str(stage / "fixture-inputs"), run.call_args.kwargs["env"]["DUNGEON_H1F4_TEST_EXPORT_ROOT"])
        self.assertTrue((stage / "fixture-inputs").is_dir())
        self.assertEqual("b" * 40, json.loads((stage / "fixture-inputs/ownership.json").read_text())["source_sha"])
        self.assertEqual(before, os.environ.get("DUNGEON_H1F4_TEST_EXPORT_ROOT"))
        self.assertFalse((self.root / "TestResults/living-dungeon-two-floor-20261020").exists())

    def test_existing_fixture_output_prevents_launch_without_overwrite(self):
        stage = self.stage.parent / "living-regressions"; stage.mkdir()
        output = stage / "fixture-inputs"; output.mkdir()
        (output / "evidence.txt").write_bytes(b"retained")
        with patch.object(q, "ROOT", self.root), patch.object(q.subprocess, "run") as run:
            with self.assertRaises(FileExistsError): q.run_living("living", stage)
            run.assert_not_called()
        self.assertEqual(b"retained", (output / "evidence.txt").read_bytes())

    def test_living_plan_declares_count_and_seals_exported_inputs(self):
        with patch.object(q, "ROOT", self.root):
            prep, _, _ = q.identities("livingtest"); prep.mkdir(parents=True)
            q.write(prep / "qualified-inputs.json", {}); q.write(prep / "original-inputs.json", {})
            with patch.object(q, "check_protected"), patch.object(q, "inventory", return_value={}), \
                    patch.object(q, "sha", return_value="a" * 64), patch.object(q.subprocess, "check_output", return_value="b" * 40):
                q.plan("livingtest", living=True)
            plan = json.loads((prep / "plan.json").read_text()); supervisor.validate_plan(plan)
            self.assertEqual(358, plan["stages"][1]["expected_tests"])
            self.assertEqual([], plan["stages"][1]["allowed_skips"])
            self.assertIn("fixture-inputs", plan["stages"][1]["artifacts"])
            self.assertEqual(set(plan["selections"]["living-regressions"][:-1]), set(plan["stages"][-1]["depends"]))


if __name__ == "__main__":
    unittest.main()
