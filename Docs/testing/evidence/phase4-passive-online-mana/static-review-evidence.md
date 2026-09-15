# Phase 4 canonical passive online mana: implementation and qualification

Branch: `codex/phase-4-canonical-passive-online-mana`.
Starting HEAD/main: `aac3af2a5836d22d62f9a4be5c2c27e9cf26c3ea` (merged PR #204).
GitHub main was fetched and matched this exact baseline before implementation. The only pre-existing worktree change was the intentionally preserved local-only `ProjectSettings/UnityConnectSettings.asset` change (`m_Enabled: 0` to `m_Enabled: 1`). No Unity or Unity Hub process was running.

## Qualification status

- Static C# compilation: **PASS** for core/tests, Player, and Editor. Core/tests retained five existing CS0649 warnings for JSON-deserialized `BuildReadinessTests.QualificationAssemblyDefinition` fields; Player and Editor compiled with zero errors/warnings.
- `git diff --check`: **PASS** before Unity qualification.
- Focused passive-mana fixture: **42/42 passed** in both final EditMode and PlayMode discovery.
- Full EditMode: **886 passed, 0 failed, 0 skipped/ignored, 0 inconclusive**; wrapper exit **0**; duration **118.2716389 seconds**. XML: `C:\Users\gdg34\AppData\Local\Temp\phase4_passive_online_mana_editmode.xml` (SHA-256 `E03CD08991A48FDEADD2401BE4C95F4EEB2327138222D69FBA2F56388CBD3524`). Preserved Editor log: `C:\Users\gdg34\AppData\Local\Temp\phase4_passive_online_mana_editmode_editor.log` (SHA-256 `AD5C4D60AF6C0827382931BC644AF7EFD1973FF19A4FF60422B5A5D53C2D16A7`).
- Full PlayMode: **2,389 passed, 0 failed, 10 expected skipped/ignored, 0 inconclusive**; wrapper exit **0**; duration **113.5977454 seconds**. The expected skips are eight synchronous EditMode-only fixtures, one inverse non-Windows fixture, and one Windows Player-only fixture. XML: `C:\Users\gdg34\AppData\Local\Temp\phase4_passive_online_mana_playmode.xml` (SHA-256 `2C70C8CDAD0D97950AC9E530A1BE4D578514A7666A39EC505460396E06BEBA0F`). Preserved Editor log: `C:\Users\gdg34\AppData\Local\Temp\phase4_passive_online_mana_playmode_editor.log` (SHA-256 `1D3AC049FDC97400F8C894813980C176BE8E36D54C9BEFE0D39DF1BC6993C0EE`).
- Windows x86_64 Development Build: **PASS**, wrapper exit **0**. Unity **6000.3.2f1**, target `StandaloneWindows64`, Development Build true, Bootstrap-only scene, result Succeeded, 0 errors, 1 warning. Output: `Builds/Development/Windows/Dungeon Lord.exe`; report: `Builds/Development/Windows/build-report.json` (SHA-256 `4CB756F6438DD70AED181FDEBC24A3AB732EF4786CF403BF204C0E03AFFEC2FB`); provenance: `Builds/Development/Windows/build-provenance.json` (SHA-256 `69129F0C5844F1EA16EC077A2A8FF85438B993CCED723ED3AD23E8BC0F2972C2`); log: `C:\Users\gdg34\AppData\Local\Temp\phase4_passive_online_mana_windows_development_build.log` (SHA-256 `EFDB261E7ADA16C5F904779FD4BDEBACEFD220E1C9DF02A7E86B57E1702C3496`). The sole warning is the established missing Unity Cloud credentials for native-symbol upload; the local build succeeded.
- Manual Editor/standalone UAT: not performed. No manual-pass claim is made.

## Implemented authority

- `passive_online_mana.json` is the sole passive-production tuning source. Its strict immutable snapshot owns the temporary `MvpBaselineCoreLevel` of 1, mana per Core Level per minute, mana per active floor per minute, the complete heat-efficiency map, and soft-cap activation/parameters. Missing, malformed, duplicate, out-of-order, nonfinite, negative, unsupported-version, and inconsistent soft-cap inputs fail closed without hidden gameplay defaults.
- `MvpBaselineCoreLevel` is an explicitly temporary Phase 4 configuration input. No persisted Core Level, progression authority, schema field, or migration is added.
- `CanonicalActiveFloorResolver` validates canonical spatial state and derives the floor count from that state. It does not read a legacy floor counter or assume one floor.
- Heat comes from the existing `StructureRuntimeState.Heat` authority and resolves through the existing current-tier resolver. Efficiencies come from passive mana configuration.
- Formula order is Base → Heat → Research → Event/Season → Clamp/SoftCap → Rounding. Research and Event/Season inputs are neutral. Soft-cap support is architecturally present and configuration-disabled; disabled evaluation bypasses the transform and requires no sentinel tuning.
- The authoritative result is mana/hour. With the approved current one-floor MVP baseline it resolves Peace 180, Notice 171, and Concern 153 mana/hour. Only after hourly rounding is it divided into the configured active simulation tick award.
- The 10-second tick remains owned by `content_bootstrap.json`. A Peace tick therefore awards 0.5 mana; the tick award is not rounded to an integer. Repeated deterministic timelines produce the same fractional balance.
- `StructureRuntimeState.ManaReserve` remains the only wallet. Each eligible tick prepares one result and publishes one atomic assignment, clamped to `structural_economy.json` ManaCapacity. No derived rate is persisted.
- TimeService suppresses ticks while paused. Resume does not add subscriptions. A canonical adventure action neither synthesizes a passive tick nor invokes legacy mana production.
- StructureSimulation keeps legacy/prototype data and heat behavior, but canonical mode suppresses positive `ManaDeltaPerTick`. This is the narrow compatibility boundary preventing a legacy Mana Generator double-award.
- `content_bootstrap.json` owns `activeSaveIntervalSeconds`. GameRoot accumulates eligible active ticks and uses the existing SaveService atomic state-change write when that interval elapses; it no longer saves research progress on every active tick. Pause/background/quit/key-action save paths remain in place.
- Bootstrap diagnostics display localized current/capacity mana, authoritative mana/hour, Core/floor contributions, heat efficiency, and full-storage state without exposing raw heat identifiers.
- Schema remains 9. OfflineSummaryResolver is unchanged and grants no mana.

## Coverage added and reconciled

`PassiveOnlineManaTests` covers production-config loading and values; fail-closed validation; authored enabled/disabled soft caps; Peace/Notice/Concern rates and stage order; neutral future modifier stages; multiple canonical floors; invalid canonical state; fractional and repeated-timeline determinism; capacity boundaries and ownership; active/pause/resume lifecycle; configured periodic save cadence; canonical legacy-generator suppression; adventure/offline non-award; schema-9 fractional save/reopen without Core/rate persistence; and localized presentation.

Existing formula, clock, structure-simulation, and GameRoot boot integration tests now assert exposed formula stages, paused tick suppression, canonical legacy separation, and periodic fractional persistence. `PhaseFourPassiveOnlineMana` registers the focused suite for Unity discovery.

Regression qualification retains the existing structural editing, construction/refund, acquisition pricing/StartingMana, custody/redeployment, and direct-unassignment suites. Production structural and acquisition tuning files are unchanged.

## Static validation executed

The standalone Roslyn response files are derived from the prior qualified Phase 4 compile inputs and add the new runtime/test sources. They do not edit Unity-generated project files.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-core.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-player.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-editor.rsp"
```

## Automated and build commands executed

Each Unity launch was preceded by exact repository, branch, HEAD, worktree, project-settings content, process, project-path, and command checks. The final passing commands were:

```powershell
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode EditMode --output "$env:TEMP\phase4_passive_online_mana_editmode.xml"
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode PlayMode --output "$env:TEMP\phase4_passive_online_mana_playmode.xml"
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" build "C:\Dev\Dungeon-Lord" --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --log-file "$env:TEMP\phase4_passive_online_mana_windows_development_build.log" --provenance-path "C:\Dev\Dungeon-Lord\Builds\Development\Windows\build-provenance.json" --allow-dirty-build --no-tail
```

The first EditMode iteration exposed zero-floor test-fixture assumptions, one test reflection setup omission, and existing failure-banner precedence; the second reduced failures to two harness cases; the third reduced to one expected-log harness issue. All were corrected before the final clean EditMode and PlayMode runs. The first PlayMode attempt exposed two EditMode helper cases that only execute in PlayMode discovery; those fixtures were corrected and both complete suites were rerun clean. No failed run is represented as qualification.

Unity test/build imports reordered identical application identifiers, and the build additionally materialized URP prefilter/runtime settings and Standalone batching defaults. Each generated diff was inspected and reversed after its run. Final content hashes for `ProjectSettings/ProjectSettings.asset`, `Assets/Settings/UniversalRP.asset`, and `Assets/UniversalRenderPipelineGlobalSettings.asset` match `HEAD`. The pre-existing local-only UnityConnect change remains untouched.

## Exact manual validation checklist

Manual UAT remains pending. Perform these steps on a clean/test save; do not convert this checklist into a pass claim without recorded execution evidence.

1. Start from an appropriate clean/test save and record its source/schema.
2. Confirm fresh-native starting mana remains the currently approved value.
3. Observe canonical mana increasing during active play and record before/after timestamps and balances.
4. Confirm the displayed Peace/Notice/Concern mana/hour matches the canonical resolver for the current state.
5. In Peace with one active floor, confirm two configured 10-second ticks add exactly 1 mana total, demonstrating fractional 0.5-per-tick accumulation rather than per-tick inflation.
6. Pause and confirm elapsed paused time adds zero mana.
7. Resume and confirm exactly one production stream continues with no catch-up or duplicate subscription.
8. Trigger an adventure and confirm the button/action itself adds no passive or run-event mana.
9. Build or acquire content, confirm the expected existing cost, and verify earning continues from the resulting canonical balance.
10. Put the wallet just below capacity, cross it with passive production, and confirm the canonical balance clamps exactly to capacity and reports full storage without overflow.
11. Accumulate fractional/cumulative mana, allow the configured periodic save or use an existing key-action save, close, reopen, and confirm the canonical schema-9 balance restores exactly.
12. Exercise legacy structure simulation compatibility and confirm a configured Mana Generator does not add a second award in canonical play.
13. Confirm all new Bootstrap text is localized and no raw heat/config identifiers appear as player labels.
14. Recheck room/corridor construction, movement, replacement, deletion/refund, paid content acquisition, StartingMana, returned-custody redeployment, and direct unassignment for unchanged costs and identity behavior.

Repeat the relevant active/pause/resume/adventure/capacity/persistence checks in the qualified Windows Development Build and review `Player.log` for errors, exceptions, assertions, save failures, canonical-validation failures, or localization failures.

## Explicitly deferred

- Run-event mana, including death, elite, skill-spill, damage, and adventurer-level mana.
- Offline mana earning and lifecycle grants.
- Durable Core Level progression or persistence.
- Research mana effects, including Conversion Tuning.
- Event and seasonal mana effects.
- Active mana soft-cap tuning.
- Mana Farm and broader economy/content expansion.
