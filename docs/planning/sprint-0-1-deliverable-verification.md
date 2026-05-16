# Sprint 0 / Sprint 1 Deliverable Verification (Durable Artifact)

Last updated: 2026-05-16

## Purpose

This document preserves Sprint 0 and Sprint 1 deliverable verification in-repo so future reviewers can see what was checked, where each artifact exists, and which gaps remain before Sprint 1 closeout.

## Verification Table

| Deliverable | Expected path/name | Actual path/name | Status | Notes |
|---|---|---|---|---|
| Bootstrap scene | `Assets/_Project/Scenes/Bootstrap.unity` | `Assets/_Project/Scenes/Bootstrap.unity` | Exists and appears correctly wired | Matches expected location. |
| GameRoot | `Assets/_Project/Scripts/Core/GameRoot.cs` | `Assets/_Project/Scripts/Core/GameRoot.cs` | Exists and appears correctly wired | Matches expected location. |
| BootstrapOverlay | `Assets/_Project/Scripts/UI/BootstrapOverlay.cs` | `Assets/_Project/Scripts/UI/BootstrapOverlay.cs` | Exists and appears correctly wired | Matches expected location. |
| ContentServices | `Assets/_Project/Scripts/Services/ContentServices.cs` | `Assets/_Project/Scripts/Services/ContentServices.cs` | Exists and appears correctly wired | Matches expected location. |
| SaveService | `Assets/_Project/Scripts/Services/SaveService.cs` | `Assets/_Project/Scripts/Services/SaveService.cs` | Exists and appears correctly wired | Matches expected location. |
| TimeService | `Assets/_Project/Scripts/Services/TimeService.cs` | `Assets/_Project/Scripts/Services/TimeService.cs` | Exists and appears correctly wired | Matches expected location. |
| TelemetryService | `Assets/_Project/Scripts/Services/TelemetryService.cs` | `Assets/_Project/Scripts/Services/TelemetryService.cs` | Exists and appears correctly wired | Matches expected location. |
| KpiService | `Assets/_Project/Scripts/Services/KpiService.cs` | `Assets/_Project/Scripts/Services/KpiService.cs` | Exists and appears correctly wired | Matches expected location. |
| RestrictedActionGateService | `Assets/_Project/Scripts/Services/RestrictedActionGateService.cs` | `Assets/_Project/Scripts/Services/RestrictedActionGateService.cs` | Exists and appears correctly wired | Matches expected location. |
| SaveMigration | `Assets/_Project/Scripts/Services/SaveMigration.cs` | `Assets/_Project/Scripts/Services/SaveMigration.cs` | Exists and appears correctly wired | Matches expected location. |
| FormulaEngine | `Assets/_Project/Scripts/Economy/FormulaEngine.cs` | `Assets/_Project/Scripts/Economy/FormulaEngine.cs` | Exists and appears correctly wired | Matches expected location. |
| ManaSystem | `Assets/_Project/Scripts/Economy/ManaSystem.cs` | `Assets/_Project/Scripts/Economy/ManaSystem.cs` | Exists and appears correctly wired | Matches expected location. |
| HeatSystem | `Assets/_Project/Scripts/Economy/HeatSystem.cs` | `Assets/_Project/Scripts/Economy/HeatSystem.cs` | Exists and appears correctly wired | Matches expected location. |
| All Bootstrap JSON files | `Assets/_Project/Data/Bootstrap/*.json` | `build_config.json`, `content_bootstrap.json`, `content_manifest.json`, `dev_commands.json`, `heat_runtime.json`, `schema_versions.json`, `string_table_en.json` under `Assets/_Project/Data/Bootstrap/` | Exists and appears correctly wired | Bootstrap JSON set exists and is non-empty. |
| Schema/version data | `Assets/_Project/Data/Schemas/*` and bootstrap versioning data | `Assets/_Project/Data/Schemas/schema_bundle.json`, `mana_modifiers.schema.json`, `heat_modifiers.schema.json`, `research_modifiers.schema.json`; plus `Assets/_Project/Data/Bootstrap/schema_versions.json` | Exists and appears correctly wired | Schema bundle and version map are present. |
| Test assembly setup | Test classes and asmdef should live under a coherent Unity tests path/assembly | Test classes are under `Assets/_Project/Tests/EditMode/`; asmdef is `Assets/_Project/Tests 1/Tests 1.asmdef`; assembly name is `Tests1` | Exists but naming/path/docs mismatch | **Should fix before Sprint 1 closeout** unless documented: path split (`Tests` vs `Tests 1`) and spacing/name differences likely contribute to Test Runner discoverability confusion. |
| All Sprint 1 test classes | Sprint 1 EditMode classes present under `Assets/_Project/Tests/EditMode/` | `FormulaEngineTests.cs`, `HeatSystemTests.cs`, `KpiServiceTests.cs`, `ManaSystemTests.cs`, `MigrationRunnerTests.cs`, `RestrictedActionGateTests.cs`, `SimulationClockTests.cs`, `SimulationDeterminismTests.cs`, `TelemetryServiceTests.cs` | Exists but coverage incomplete | Existing coverage includes migration, restricted action gate, simulation clock, and determinism. Remaining direct unit-test gaps still appear to include `ContentServices` and `SaveService` class-level coverage. |
| Sprint 1 closeout checklist | `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` | `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` | Exists and appears correctly wired | Checklist exists and is linkable as gate artifact. |
| Sprint 1 testing runbook | `docs/planning/sprint-1-testing-runbook.md` | `docs/planning/sprint-1-testing-runbook.md` | Exists and appears correctly wired | Runbook exists and is linkable as gate artifact. |
| Sprint 0 setup/checklist docs | `docs/planning/*sprint-0*` and/or `Docs/*Sprint0*` checklist/setup docs | No dedicated Sprint 0 setup/checklist doc found in current repository search | Missing | No dedicated Sprint 0 checklist was found; not a runtime blocker; should be waived in Sprint 1 closeout or tracked as documentation debt. |
| Duplicate `Tests 1` / `Tests1.asmdef` check | No duplicate or ambiguous asmdef naming | Found `Assets/_Project/Tests 1/Tests 1.asmdef`; no parallel `Tests1.asmdef` found | Exists but naming/path/docs mismatch | **Should fix before Sprint 1 closeout**: normalize path/name or document rationale to reduce tooling/path ambiguity and review confusion. |
| Root `.gitignore` Unity generated-folder rules | `.gitignore` contains Unity generated directory patterns | Root `.gitignore` includes `[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`, `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/` | Exists and appears correctly wired | Unity generated-folder coverage exists at repo root. |

## Open Items Summary

### Must fix before Sprint 1 closeout

1. None currently identified from this document-only verification pass.

### Should fix before Sprint 1 closeout

1. Normalize Unity test assembly naming/path (`Assets/_Project/Tests/EditMode` versus `Assets/_Project/Tests 1/Tests 1.asmdef`) or explicitly document why the current split is intentional.
2. Add direct class-level tests for `ContentServices` and `SaveService`, or document accepted indirect coverage in Sprint 1 closeout evidence.
3. Add/link Sprint 0 setup/checklist artifact if Sprint 0 completion evidence is a governance prerequisite; otherwise record a formal waiver in Sprint 1 closeout.

### Can defer to Sprint 2/backlog

1. If Sprint 0 documentation is not a hard gate for Sprint 1 promotion, backlog the Sprint 0 historical reconstruction as a documentation debt item with owner and target sprint.

## Evidence Collection Method (for repeatability)

The deliverable verification above was assembled from repository path inspection for runtime/services, bootstrap content, schemas, tests, closeout docs, and root ignore rules.
