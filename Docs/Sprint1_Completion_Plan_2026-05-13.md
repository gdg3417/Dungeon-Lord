# Sprint 1 Completion Plan (Actionable, repository-specific)

Date: 2026-05-13  
Scope: Close Sprint 1 (“Simulation Spine”) based on current repo state.

## 1) Sprint 1 exit criteria (target)
- Deterministic tick replay passes.
- Save load/migrate roundtrip works.
- Formula stacking/bucket order tests are green.

## 2) Current status snapshot
### Already done
- Formula engine exists with bucket sequencing + tests.
- Mana system exists with deterministic tick-style input + tests.
- Simulation clock/time service exists + tests.
- Save root + migration runner skeleton exists + tests.
- Content loader + manifest/schema gate checks exist.
- Minimal debug overlay/dashboard exists.

### Missing to truly close Sprint 1
1. HeatSystem v1 implementation is not yet present (`ApplyEvent`, `Decay`, baseline bounds/rules).
2. Deterministic replay harness that validates multi-tick reproducibility end-to-end is not yet present.
3. Save migration roundtrip coverage needs stronger fixtures beyond skeleton behavior.
4. Formula contract coverage should include explicit clamp/soft-cap ordering edge cases under mixed bucket stacks.

---

## 3) Actionable work plan (ordered)

## Task S1-01 — Implement `HeatSystem` runtime core
**Goal:** Land MVP Heat v1 core behavior used by simulation loop.

### Scope
- Create `IHeatSystem` + `HeatSystem` in `Assets/_Project/Scripts/Economy/`.
- Add:
  - `ApplyEvent(HeatEventInput)` for explicit event deltas.
  - `Decay(HeatDecayInput)` for passive decay across elapsed ticks/time.
  - Guardrails for min bound (never below 0).
- Keep tier transitions simple and data-driven-ready (do not add post-MVP behavior).

### Deliverables
- New system file(s) + model types.
- No UI dependency required in first pass.

### Acceptance
- Deterministic same-input -> same-output behavior.
- Lower-bound invariant enforced.

---

## Task S1-02 — Add HeatSystem unit tests (EditMode)
**Goal:** Lock Heat invariants before integrating with loop.

### Test cases
- Event delta adds/subtracts correctly.
- Decay applies deterministically over N ticks.
- Heat never goes negative.
- Repeated identical input sequences are replay-stable.

### Deliverables
- `Assets/_Project/Tests/EditMode/HeatSystemTests.cs`.

### Acceptance
- All new heat tests pass consistently.

---

## Task S1-03 — Add deterministic tick replay harness test
**Goal:** Prove Sprint 1 simulation determinism for core systems.

### Scope
- Add a test fixture that runs a fixed scripted timeline (e.g., 300 ticks) twice using identical inputs/seeds and compares outputs at each checkpoint.
- Include mana outputs, heat outputs, and tick index snapshots.

### Deliverables
- `Assets/_Project/Tests/EditMode/SimulationDeterminismTests.cs`.

### Acceptance
- Replay run A == run B for all asserted checkpoints.

---

## Task S1-04 — Strengthen formula contract tests
**Goal:** Ensure global modifier behavior is locked for future systems.

### Scope
- Extend existing formula tests with combined stacks:
  - additive + multiplicative + clamp + soft-cap + rounding.
  - clamp-before/after cases based on bucket enum order.
- Add explicit edge assertions for tiny/large values and sign behavior.

### Deliverables
- Update `FormulaEngineTests.cs`.

### Acceptance
- Contract tests encode expected bucket ordering unambiguously.

---

## Task S1-05 — Expand save migration fixture coverage
**Goal:** Satisfy “save load/migrate roundtrip works” with robust fixtures.

### Scope
- Add fixture JSONs representing at least:
  - current schema save,
  - one older schema save,
  - malformed/partial save fallback path.
- Verify `Load -> Migrate -> Save -> Reload` preserves required fields and schema version.

### Deliverables
- Additional tests in `MigrationRunnerTests.cs` and/or `SaveService` tests.

### Acceptance
- Roundtrip assertions pass for old/current fixtures.

---

## Task S1-06 — Wire Heat tick call into runtime loop (minimal)
**Goal:** Integrate HeatSystem without expanding scope into Sprint 2 domains.

### Scope
- Instantiate HeatSystem in `GameRoot` composition.
- On tick, invoke decay path and expose current heat value to debug overlay line.
- Keep event inputs stubbed/minimal (no adventurer loop yet).

### Deliverables
- Small integration edits in `GameRoot` and `BootstrapOverlay`.

### Acceptance
- Play mode shows heat changing over time according to decay rules.

---

## Task S1-07 — Minimal content contract wiring for Heat modifiers
**Goal:** Keep Heat behavior table-driven-ready per Sprint 1 principles.

### Scope
- Ensure heat config/modifier payload can be loaded from existing schema-gated content assets (or defaults when missing).
- Add validation fallback logging for missing keys (non-fatal).

### Deliverables
- Small updates in content model/service parsing.

### Acceptance
- Startup succeeds with clear warning if optional heat config not found.

---

## Task S1-08 — Sprint 1 close-out checklist execution
**Goal:** Final go/no-go decision for Sprint 1 completion.

### Checklist
1. Run full EditMode test suite.
2. Run determinism fixture multiple times.
3. Validate pause/resume + offline elapsed path does not break determinism guarantees.
4. Validate save migration roundtrip on fixture matrix.
5. Confirm debug panel displays mana/heat/tick clearly.

### Acceptance
- All checklist items pass with no invariant violations.

---

## 4) Suggested implementation sequence (for immediate execution)
1. S1-01 HeatSystem core.
2. S1-02 Heat tests.
3. S1-06 minimal runtime wiring.
4. S1-03 determinism harness.
5. S1-04 formula contract expansions.
6. S1-05 migration fixture expansion.
7. S1-07 content wiring.
8. S1-08 close-out checklist + signoff note.

## 5) Definition of Done for Sprint 1
Sprint 1 is complete when:
- HeatSystem v1 exists with passing invariants.
- Deterministic replay test is stable.
- Save migrate roundtrip fixture matrix is green.
- Formula contract tests cover mixed bucket edge cases.
- Debug view shows mana, heat, and tick-time state for verification.
