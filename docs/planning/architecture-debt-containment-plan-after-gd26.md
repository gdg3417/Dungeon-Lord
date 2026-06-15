# AR1 Architecture Debt Containment Plan After GD26

## Status and scope

Main is merged through PR 120 / GD26. This AR1 document is a documentation-only architecture stabilization plan for containing integration debt before GD27 gameplay work resumes.

The gameplay prototype now includes placement options, placement effects, run simulation, loot, heat, casualties, First Dungeon Contract, Adventurer Run Intent, Arrival Pressure, Traffic Pressure, and manual smoke runs using resolved adventurer intent. The project is therefore no longer only backend scaffolding: it now has end-to-end prototype gameplay paths, player-facing MVP presentation, diagnostics, smoke flows, and save/migration support that interact in visible ways.

The current UI remains Bootstrap/immediate-mode prototype UI. That is acceptable for the present phase, but the growing responsibility surface in `BootstrapOverlay` and `GameRoot` needs explicit boundaries before additional gameplay scope is layered on top.

## Architecture health assessment

### `GameRoot`

Current health: functional but too broad. `GameRoot` owns Unity bootstrapping, content loading, service construction, config summaries and validation, save state, migration handling, tick/update flow, diagnostics strings, run orchestration, MVP summary resolution, and overlay binding. This makes it a practical integration root, but also a high-conflict file for future gameplay and UI work.

Target health: remain the composition root and lifecycle coordinator only. It should construct or receive services, expose stable read-only state needed by prototype UI, and delegate feature orchestration to smaller services.

Debt containment need: AR4 should extract or centralize config validation and summary orchestration first, because those responsibilities are integration-heavy but lower-risk than changing gameplay simulation.

### `BootstrapOverlay`

Current health: useful for fast prototype smoke validation, but too large for continued feature growth. It owns immediate-mode controls, input shortcuts, diagnostics pages, compact smoke view, copied smoke text composition, MVP placement selection, run posture selection, action button handling, action feedback, and viewport state.

Target health: remain a thin Bootstrap prototype shell while smoke composition and action handling move behind focused collaborators. It can keep immediate-mode layout until a future UI replacement exists, but it should not own domain-facing orchestration or large text-composition routines.

Debt containment need: AR2 and AR3 should target this file in sequence, preserving visual output and manual smoke controls.

### `RunSimulationService`

Current health: comparatively well-contained. It is a domain service for deterministic run simulation and related resolution, with injected or loaded config. It should remain outside UI composition and should not absorb Bootstrap control flow.

Target health: continue as simulation orchestration for a single run, with config-owned tuning and deterministic behavior protected by EditMode tests.

Debt containment need: avoid modifying run math during AR cleanup. Only touch this area if tests expose a seam needed to preserve behavior during extraction.

### `Models.cs`

Current health: large shared model/config surface. This is tolerable while the prototype is consolidating, but it creates discoverability and merge-risk problems as more systems accumulate.

Target health: split only when a post-AR2-through-AR4 change demonstrates a clear boundary and low-risk migration path. Premature model movement can create broad namespace churn without reducing near-term risk.

Debt containment need: AR5 is explicitly conditional. Do not split `Models.cs` merely because it is large.

### Resolver and presenter pattern

Current health: strong direction. Resolvers and presenters already provide seams between deterministic domain logic, player-facing summaries, diagnostics, and Bootstrap UI. This pattern is the safest extraction target because existing tests can lock behavior while callers move.

Target health: keep resolvers deterministic and config-driven; keep presenters focused on transforming resolved state and localization references into display-ready summaries; keep Bootstrap-specific text assembly outside simulation services.

Debt containment need: new extraction classes should follow this pattern instead of becoming replacement manager classes.

### Config and localization ownership

Current health: the repository has the correct guardrails: gameplay tuning belongs in content/config/typed assets, and player-facing English belongs in localization/string table references. The integration roots currently carry too much summary and validation glue, but the ownership principle is sound.

Target health: runtime systems consume injected, loaded, or test config. Player-facing output remains localization-key based. Config validation and summary orchestration should be centralized enough to avoid duplicating validation rules across UI, boot, and diagnostics.

Debt containment need: AR cleanup must not move tuning into runtime code, rename stable IDs, or introduce hardcoded player-facing English.

### Test coverage and smoke evidence

Current health: EditMode coverage exists across simulation, resolver, presenter, validation, save/migration, and MVP presentation seams. Manual Bootstrap smoke evidence is also part of the workflow.

Target health: every AR extraction preserves full EditMode test behavior and Bootstrap smoke behavior. When output text composition is moved, copied smoke evidence should remain identical unless a test documents an explicitly scoped change.

Debt containment need: Unity test runs are expected for extraction PRs that touch C# code. AR1 itself is documentation-only, so Unity tests are not required.

## Responsibility map

| Area | Current responsibilities | Target responsibilities |
| --- | --- | --- |
| `GameRoot` | Unity bootstrapping; TextAsset ownership; service construction; content/config load; config validation summaries; save/migration; run state; tick state; diagnostics lines; MVP summary access; overlay binding. | Composition root, lifecycle coordinator, and stable state facade. Delegate validation summaries, run orchestration, and diagnostics composition to focused collaborators. |
| `BootstrapOverlay` | Immediate-mode UI shell; keyboard/dev controls; diagnostics navigation; player-facing section navigation; MVP placement/run controls; compact smoke and copied smoke composition; action feedback. | Thin Bootstrap prototype UI shell. Keep layout and manual controls, but delegate smoke text composition and MVP action handling. |
| Player-facing screen presentation | Mixed between presenters and `BootstrapOverlay` assembly. | Presenter/resolver-owned summaries with localization references; overlay only renders selected sections. |
| Compact smoke and copied smoke composition | Built inside `BootstrapOverlay`, coupled to viewport state and run/action selections. | Dedicated smoke composition service/presenter that receives current summaries and returns deterministic text blocks for display/copy. |
| MVP action handling | Selection and button behavior live in `BootstrapOverlay`, calling into `GameRoot` and presenters directly. | Dedicated action handler/application service for placement and manual run actions; overlay gathers user intent and renders results. |
| Run simulation orchestration | Split between `BootstrapOverlay` action triggers, `GameRoot` run methods/state, and `RunSimulationService`. | `RunSimulationService` keeps run math; a small orchestration layer coordinates selected intent, save updates, feedback, and diagnostics. |
| Config validation | Loaded and summarized through `GameRoot` and validator services. | Central validation/summary orchestration service invoked by `GameRoot`; validators remain focused and testable. |
| Diagnostics pages | Page constants, navigation, and body text are in `BootstrapOverlay`; many diagnostic lines are exposed by `GameRoot`. | Diagnostics page shell can remain in Bootstrap, but page body composition should move behind diagnostics presenters as pages grow. |
| Save and migration handling | `GameRoot` owns save service lifecycle and migration integration. Dedicated migration services exist. | `GameRoot` invokes save/migration lifecycle; migration logic stays isolated from gameplay rules and UI. |

## Extraction sequence

### AR2: extract smoke text composition from `BootstrapOverlay`

- Purpose: move compact smoke and copied smoke text composition into a focused presenter/composer while preserving exact Bootstrap smoke output.
- Files likely touched:
  - `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - New focused smoke composer/presenter under `Assets/_Project/Scripts/Services/`
  - New or updated EditMode tests under `Assets/_Project/Tests/EditMode/`
- Files not allowed to change unless necessary:
  - `RunSimulationService.cs`
  - `GameRoot.cs`
  - JSON tuning, localization tables, scenes, prefabs, assets, and `.meta` files
- Tests expected:
  - Existing EditMode suite remains green.
  - New or updated smoke composition tests prove copied smoke text is preserved.
  - Manual Bootstrap smoke evidence is preserved.
- Explicit non-goals:
  - No gameplay math changes.
  - No UI redesign.
  - No removal of manual smoke controls.
  - No localization key changes unless required to preserve existing behavior.
- Merge boundary: merge after the smoke composer is isolated, tested, and called by `BootstrapOverlay` without visible smoke behavior changes.

### AR3: extract MVP placement and run action handling from `BootstrapOverlay`

- Purpose: move placement and manual run action decisions out of immediate-mode UI code while preserving existing controls and feedback.
- Files likely touched:
  - `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - New action handler/application service under `Assets/_Project/Scripts/Services/`
  - Relevant MVP presenter/resolver tests under `Assets/_Project/Tests/EditMode/`
- Files not allowed to change unless necessary:
  - `RunSimulationService.cs`
  - Config JSON and tuning assets
  - Localization data
  - Scenes, prefabs, assets, and `.meta` files
- Tests expected:
  - EditMode tests for placement action handling, invalid selections, run posture handoff, run feedback preservation, and deterministic action results.
  - Existing Bootstrap smoke behavior remains available.
- Explicit non-goals:
  - No scheduler, auto-runs, concurrent party simulation, stats, combat, inventory, economy expansion, raids, or monetization.
  - No gameplay tuning changes.
  - No player-facing behavior changes unless exact preservation or intentional scope is documented in tests.
- Merge boundary: merge when `BootstrapOverlay` only captures input and renders handler results for MVP actions.

### AR4: extract or centralize config validation and summary orchestration from `GameRoot`

- Purpose: reduce `GameRoot` as a validation and summary hub while preserving boot behavior and diagnostics availability.
- Files likely touched:
  - `Assets/_Project/Scripts/Core/GameRoot.cs`
  - Existing validator services such as loot/config validators only if needed
  - New config validation/summary orchestration service under `Assets/_Project/Scripts/Services/`
  - Validator and diagnostics EditMode tests
- Files not allowed to change unless necessary:
  - `BootstrapOverlay.cs`
  - Simulation services
  - Save migration logic
  - JSON tuning, localization, scenes, prefabs, assets, and `.meta` files
- Tests expected:
  - Existing EditMode validation tests remain green.
  - New tests cover centralized validation summaries and failure/safe-fallback paths.
  - Diagnostics pages remain available.
- Explicit non-goals:
  - No tuning changes unless the PR is specifically scoped as config validation.
  - No scene or prefab changes.
  - No save schema changes.
- Merge boundary: merge when `GameRoot` delegates validation/summary orchestration through a narrow tested seam.

### AR5: split large shared model/config classes only if needed after AR2 through AR4

- Purpose: reduce model/config discoverability and merge conflicts only after extraction work reveals stable boundaries.
- Files likely touched:
  - `Assets/_Project/Scripts/Util/Models.cs`
  - New model/config files under appropriate existing script folders
  - Tests only as needed for namespace/reference preservation
- Files not allowed to change unless necessary:
  - Runtime logic files unrelated to type moves
  - JSON tuning, localization, scenes, prefabs, assets, and `.meta` files
- Tests expected:
  - Full EditMode suite remains green.
  - Compile-only behavior is not sufficient if any constructor/default behavior changes.
- Explicit non-goals:
  - No broad namespace reshuffle.
  - No type renames unless required and migration-safe.
  - No behavior changes.
- Merge boundary: merge only if the split is mechanical, narrow, and demonstrably lowers future AR/GD conflict risk.

## Guardrails

- Do not change gameplay math during architecture cleanup.
- Do not change player-facing behavior unless a test documents the exact preservation or explicitly scoped intentional change.
- Do not rename stable IDs.
- Do not move tuning into runtime code.
- Do not hardcode player-facing English.
- Do not remove manual smoke controls.
- Do not add scheduler, auto-runs, concurrent party simulation, stats, combat, inventory, economy expansion, raids, or monetization.
- Do not perform broad file moves or namespace reshuffles in the same PR as behavior changes.
- Every extraction PR must preserve full EditMode test behavior and Bootstrap smoke behavior.

## Acceptance criteria for future architecture cleanup PRs

Each future AR PR must:

- Have a narrow changed file set.
- Preserve existing visible output unless explicitly scoped.
- Include or update EditMode tests.
- Preserve copied smoke evidence.
- Keep diagnostics available.
- Avoid changing JSON tuning unless the PR is specifically about config validation.
- Avoid scene or prefab changes unless explicitly required.
- Confirm whether Unity Unit/SIT/UAT evidence is applicable; documentation-only changes may mark them not applicable.

## AR1 validation notes

AR1 is documentation-only. Unity tests are not required because runtime code, tests, JSON tuning, localization, scenes, prefabs, assets, and `.meta` files are unchanged. Validation should use available text checks and Git changed-file inspection to confirm the patch remains documentation-only.
