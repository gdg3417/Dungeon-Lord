# Sprint 0 / Sprint 1 Deliverable Verification (Durable Artifact)

Last updated: 2026-05-16

## Purpose

This document preserves Sprint 0 and Sprint 1 deliverable verification in-repo so future reviewers can see what was checked, where each artifact exists, and which gaps remain before Sprint 1 closeout.

## Verification Table

| Deliverable | Expected path/name | Actual path/name | Status | Notes |
|---|---|---|---|---|
| Bootstrap scene | `Assets/_Project/Scenes/Bootstrap.unity` | `Assets/_Project/Scenes/Bootstrap.unity` | ✅ Present | Matches expected location. |
| GameRoot | `Assets/_Project/Scripts/Core/GameRoot.cs` | `Assets/_Project/Scripts/Core/GameRoot.cs` | ✅ Present | Matches expected location. |
| BootstrapOverlay | `Assets/_Project/Scripts/UI/BootstrapOverlay.cs` | `Assets/_Project/Scripts/UI/BootstrapOverlay.cs` | ✅ Present | Matches expected location. |
| ContentServices | `Assets/_Project/Scripts/Services/ContentServices.cs` | `Assets/_Project/Scripts/Services/ContentServices.cs` | ✅ Present | Matches expected location. |
| SaveService | `Assets/_Project/Scripts/Services/SaveService.cs` | `Assets/_Project/Scripts/Services/SaveService.cs` | ✅ Present | Matches expected location. |
| TimeService | `Assets/_Project/Scripts/Services/TimeService.cs` | `Assets/_Project/Scripts/Services/TimeService.cs` | ✅ Present | Matches expected location. |
| TelemetryService | `Assets/_Project/Scripts/Services/TelemetryService.cs` | `Assets/_Project/Scripts/Services/TelemetryService.cs` | ✅ Present | Matches expected location. |
| KpiService | `Assets/_Project/Scripts/Services/KpiService.cs` | `Assets/_Project/Scripts/Services/KpiService.cs` | ✅ Present | Matches expected location. |
| RestrictedActionGateService | `Assets/_Project/Scripts/Services/RestrictedActionGateService.cs` | `Assets/_Project/Scripts/Services/RestrictedActionGateService.cs` | ✅ Present | Matches expected location. |
| SaveMigration | `Assets/_Project/Scripts/Services/SaveMigration.cs` | `Assets/_Project/Scripts/Services/SaveMigration.cs` | ✅ Present | Matches expected location. |
| FormulaEngine | `Assets/_Project/Scripts/Economy/FormulaEngine.cs` | `Assets/_Project/Scripts/Economy/FormulaEngine.cs` | ✅ Present | Matches expected location. |
| ManaSystem | `Assets/_Project/Scripts/Economy/ManaSystem.cs` | `Assets/_Project/Scripts/Economy/ManaSystem.cs` | ✅ Present | Matches expected location. |
| HeatSystem | `Assets/_Project/Scripts/Economy/HeatSystem.cs` | `Assets/_Project/Scripts/Economy/HeatSystem.cs` | ✅ Present | Matches expected location. |
| All Bootstrap JSON files | `Assets/_Project/Data/Bootstrap/*.json` | `build_config.json`, `content_bootstrap.json`, `content_manifest.json`, `dev_commands.json`, `heat_runtime.json`, `schema_versions.json`, `string_table_en.json` under `Assets/_Project/Data/Bootstrap/` | ✅ Present | Bootstrap JSON set exists and is non-empty. |
| Schema/version data | `Assets/_Project/Data/Schemas/*` and bootstrap versioning data | `Assets/_Project/Data/Schemas/schema_bundle.json`, `mana_modifiers.schema.json`, `heat_modifiers.schema.json`, `research_modifiers.schema.json`; plus `Assets/_Project/Data/Bootstrap/schema_versions.json` | ✅ Present | Schema bundle and version map are present. |
| Test assembly setup | One Unity test asmdef under tests path | `Assets/_Project/Tests 1/Tests 1.asmdef` | ⚠️ Naming mismatch | **Should fix before Sprint 1 closeout**: folder and assembly are named `Tests 1` (space + suffix) rather than canonical `Tests` / `Tests1`. Keep as-is only if intentionally documented. |
| All Sprint 1 test classes | Sprint 1 coverage classes present under test assembly | `FormulaEngineTests.cs`, `HeatSystemTests.cs`, `KpiServiceTests.cs`, `ManaSystemTests.cs`, `TelemetryServiceTests.cs` under `Assets/_Project/Tests/EditMode/` | ⚠️ Partial vs full Sprint 1 scope | **Must fix before Sprint 1 closeout** if this list is treated as complete Sprint 1 verification. Service classes like `SaveService`, `SaveMigration`, `RestrictedActionGateService`, and `ContentServices` do not show corresponding Sprint 1 unit test classes in current tree. |
| Sprint 1 closeout checklist | `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` | `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` | ✅ Present | Checklist exists and is linkable as gate artifact. |
| Sprint 1 testing runbook | `docs/planning/sprint-1-testing-runbook.md` | `docs/planning/sprint-1-testing-runbook.md` | ✅ Present | Runbook exists and is linkable as gate artifact. |
| Sprint 0 setup/checklist docs | `docs/planning/*sprint-0*` and/or `Docs/*Sprint0*` checklist/setup docs | No Sprint 0 setup/checklist doc found in current repository search | ⚠️ Missing | **Should fix before Sprint 1 closeout** if Sprint 0 evidence is a formal prerequisite; otherwise **Can defer to Sprint 2/backlog** with explicit waiver note in closeout checklist. |
| Duplicate `Tests 1` / `Tests1.asmdef` check | No duplicate or ambiguous asmdef naming | Found `Assets/_Project/Tests 1/Tests 1.asmdef`; no parallel `Tests1.asmdef` found | ⚠️ Inconsistent naming risk | **Should fix before Sprint 1 closeout**: rename to stable canonical pattern to prevent tooling/path ambiguity and review confusion. |
| Root `.gitignore` Unity generated-folder rules | `.gitignore` contains Unity generated directory patterns | Root `.gitignore` includes `[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`, `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/` | ✅ Present | Unity generated-folder coverage exists at repo root. |

## Open Items Summary

### Must fix before Sprint 1 closeout

1. Confirm and complete Sprint 1 test-class coverage expectations, or explicitly scope the Sprint 1 test list to the currently implemented classes in closeout evidence.

### Should fix before Sprint 1 closeout

1. Normalize Unity test assembly naming/path (`Tests 1` -> canonical name) or document intentional exception.
2. Add/link Sprint 0 setup/checklist artifact if Sprint 0 completion evidence is required by governance docs.

### Can defer to Sprint 2/backlog

1. If Sprint 0 documentation is not a hard gate for Sprint 1 promotion, backlog the Sprint 0 historical reconstruction as a documentation debt item with owner and target sprint.

## Evidence Collection Method (for repeatability)

The deliverable verification above was assembled from repository path inspection for runtime/services, bootstrap content, schemas, tests, closeout docs, and root ignore rules.
