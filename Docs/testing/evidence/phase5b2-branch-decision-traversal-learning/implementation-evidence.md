# Phase 5B2 implementation and qualification

Baseline: clean local and fetched `main`, merged PR #212 at `ff797d7249d0ab50025a0e506d4c0c0c922f15ab`. No intervening main commits were present at the initial fetch. Working branch: `codex/phase5b2-branch-decision-traversal-learning`.

## Ownership and execution

Phase 5A (PR #209) remains the spatial, corridor custody, fingerprint and shared-knowledge authority. Phase 5B0 (PR #211) remains the tuning/behavior contract. Phase 5B1 (PR #212) remains the only party, formation, active expertise, targeting and HP/death authority. No schema or migration files change; both current schema constants remain 10. No persisted ordinary member identity, HP, detailed decision history, second corridor owner, or second knowledge owner is added.

The pure `BranchDecisionResolver` reads original-member behavioral means, current active-member HP fractions/expertise, applicable stored facts and remaining required-route danger. Unknown facts contribute no known reward/danger and retain full uncertainty. Actual branch contents are used only for traversal and survivor observation. The exact survivability gate precedes appeal; threshold equality is deterministic; only the strict marginal interval computes a roll, with equality between roll and likelihood skipping.

Decision identity is exactly `(BranchDecisionRuleSourceId, RunId, FloorInstanceId, OptionalBranchId)`. `StableStringHash` returns zero for null/empty, otherwise starts at 23 and folds each ordinal UTF-16 character with unchecked `hash * 31 + character`. The tuple starts at 17 and folds each field hash with unchecked `hash * 31 + fieldHash`. The unsigned result divided by `4294967296.0` produces the roll. Tick, room order, clock, collection order, global RNG and runtime hashes are absent. PR #212 party-generation SHA-256 is untouched.

`PhaseFiveBRouteProjection` consumes the validated schema 10 graph and the existing required-route projection. Its transient view associates required rooms with stable floor/node identities and exposes the fork, DeadEnd, physical tiles, assignments, applicable knowledge and required suffix. Only required-room origins are accepted. Physical ordering uses Manhattan distance from `OptionalBranchGeometry.TryResolveSourceTile`, then stored 64-bit Sequence, then ordinal AssignmentId. Straight one-tile-wide geometry remains validated by Phase 5A; no saved order or duplicate topology is introduced.

The origin encounter and its existing threshold/wipe stop precede any decision. Entering resolves reached assignments in physical order using `RunEncounterResolver` for traps and the existing loot resolver for loot. HP persists across the fork and return. A wipe stops later assignments, including loot. Returning is a control-flow transition, with no second decision, event, reward, Heat application or observation. The current retreat authority is the existing room success-threshold stop and roster wipe check; no new HP-based retreat formula is invented.

Branch loot uses the existing resolver with a separate deterministic segment seed: the repository integer fold over `run.loot.branch_segment.v1`, RunId, FloorInstanceId, OptionalBranchId and AssignmentId. It never consumes the choice roll. Required one-room and multi-room seed rules are untouched. Reached effects and actual casualties feed the existing extraction, Heat delta, Heat application and cooling authorities exactly once.

## Atomic publication

`SaveService.CommitPhaseFiveBRun` delegates to `DetachedCanonicalWriteAuthority.CommitPhaseFiveBRun`. The writer validates the owned canonical session, spatial state and current durable bytes. It deep-clones recognized live state, preserves explicit nulls and existing transient historical references, and reattaches only the already-owned canonical state. A new local RunParty and detached StructureRuntimeState calculate the complete run, cooling, history, sequence, objectives and proposed knowledge.

All workload counters are checked before publication. Recognized candidate state is captured, and `PrepareLiveReplacement` receives the owned spatial state, structural investment, unchanged corridor content and explicit proposed `SharedBranchKnowledgeAuthority`. The existing exact-byte atomic persistence/readback implementation remains the only writer. Only a successful result updates SaveService's canonical session and publishes runtime state. Failure leaves live Save, HP evidence, Heat, history, sequence, objectives, topology, corridor content, knowledge and durable active bytes unchanged.

Same-session Party, EncounterEvents and BranchOutcomes survive canonical readback by matching RunId and tick. They remain properties outside public-field serialization. Reopen retains aggregates, Heat and shared knowledge, and leaves historical party/branch detail unavailable rather than regenerating it.

## Knowledge and workload

Learning is finalized after ultimate survival is known. Fork survivors may confirm topology on skip/stop without learning content or increasing content confidence. Completed physical observations report present or absent incentive/danger independently of pre-run knowledge. An unknown or contradictory observed fact resets the single shared confidence to 0.75. A matching applicable report increases it once by 0.125, clamped to 1. Last-confirmed run identity is updated. A full wipe adds no precise branch knowledge. There is no passive decay; content edits retain stale knowledge until survivor observation; fingerprints control topology applicability.

Explicit counters enforce one decision per floor, five per complete run, two processed assignments per branch and five knowledge updates per run. Exact bounds pass; one-over throws stable `branch.run.WorkloadExceeded`. Test-only synthetic floor fixtures exercise complete-run limits without expanding production floors. Atomic corridor-workload failure is also exercised through the real canonical writer after candidate trap damage.

## Production configuration

The existing version-1 `RunSimulationConfig.PhaseFiveB.BranchDecision` object contains the full production authority: rule `run.branch_decision.rule.phase5b.v1`; trap-interpretation bonus `0.15`; incentive, danger and required-route references `6`, `3` and `6`; initial inclination `0`; condition weights `0.65` health and `0.35` active members; threat weights `0.70` danger and `0.30` uncertainty; trap-expertise mitigation `0.35`; survivability penalty `0.60`; profile minimums Cautious `0.70`, Greedy `0.45`, Curious `0.50`, Goal-Oriented `0.60`, Gambler `0.35`; appeal weights reward `1.25`, danger `1.00`, uncertainty `0.75`, reserve `0.50`, intent `0.00`; thresholds `-0.15` and `0.15`; initial observation `0.75`; reconfirmation increase `0.125`; and workload limits `1`, `5`, `2`, `5` in per-floor decision, complete-run decision, per-branch assignment and per-run knowledge-update order.

## Automated qualification

Unity version: `6000.3.2f1`; installed Unity CLI: `1.0.0-beta.10`. The repository's Editor test-discovery bridge registers all new fixtures. No new test is hidden behind a platform skip.

The focused fixtures cover configuration omissions/domains, hash vectors and boundaries, survivability/unknown knowledge, origin timing and retreat precedence, physical order, HP carryover/retargeting, wipe-before-loot, return, repeated loot identity, learning/reconfirmation/contradiction, workload bounds, atomic write failure/stale session, transient readback and reopen, schema 10, and unchanged corridor/topology ownership.

Final automated results against the completed diff:

| Suite | Total | Passed | Failed | Skipped | Inconclusive | Duration |
|---|---:|---:|---:|---:|---:|---:|
| Focused Phase 5B EditMode | 102 | 102 | 0 | 0 | 0 | 2.125 s |
| Complete EditMode | 1,067 | 1,067 | 0 | 0 | 0 | 130.787 s |
| Complete PlayMode | 2,569 | 2,559 | 0 | 10 | 0 | 129.425 s |

The ten PlayMode skips are the same established guards: eight synchronous EditMode-only GameRoot structural fixtures, the non-Windows inverse native-filesystem fixture, and the Windows Player-only standalone qualification fixture. No Phase 5B2 test is skipped. The transient two-test regression check used while correcting canonical GameRoot fixture setup also passed 2/2; the final complete suites supersede it as qualification evidence.

## Manual qualification still required

Use the normal Bootstrap scene and GitHub Desktop workflow; no shell or new QA outcome-forcing controls are required. Qualify no branch, empty branch, trap-only, loot-only and trap-plus-loot on distinct tiles; SKIP and ENTER; pre-fork injury, corridor injury, lead death/retargeting, expertise mitigation, wipe and reached/unreached loot; automatic return, required continuation and absence of duplicate events/rewards; survivor learning, reconfirmation, content contradiction and deterministic repeated setup; save/reopen and historical aggregate fallback. Repeat presentation checks at 1920×1080 and 1280×720. Windows Development Build, standalone execution and standalone close/reopen remain unqualified. Automated tests do not claim these manual gates passed.

## Scope and limitations

Production remains the existing supported Floor 1 layout with at most one straight optional dead end. No branch monsters/bosses, multiple branches, optional rooms, new floors, discretionary reversal, healing, equipment, spells, new RNG, new Heat/loot authority, general save refactor, UI redesign or balance changes outside the approved contract are included. Diagnostics extend the existing localized survival surface. Historical Phase 5A evidence is unchanged; only affected current-status prose in the execution plan, design lock, Phase 5B contract and invariant glossary is reconciled.
