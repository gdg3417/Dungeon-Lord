Constructed Floor 2 could be inspected for eligibility but could not be safely activated and consumed by a real run. A4 adds zero-cost atomic activation/deactivation, minimal localized Bootstrap controls, and deterministic two-floor execution with one party and one complete-run settlement.

Baseline: merged PR #217, `756607cca58aac584f69cdc3b4c27d30b58eefad`. **This is not Phase 6 closeout.**

## Runtime and activation safety

- `CanonicalRunnableFloorProjection` provides the shared per-floor runtime interpretation. A3 eligibility and A4 runtime share `RequiredFloorTraversal`: traversal stops at the first reached Completion and never executes rooms beyond it.
- `ActiveFloorRunSnapshot` materializes the complete canonical Active prefix before party formation. Floors remain separate and capture route/content, branch plans/knowledge, configuration, loot, initial runtime, ticks, and posture. Real runs and activation preflight use this exact snapshot factory.
- Each floor has its own Phase 5B plan. RequiredSuffix and RemainingRequiredDanger never include another floor's rooms. Existing branch identities, fingerprints, learning, and deterministic ordering are retained.
- The Phase 6 coordinator reuses one RunParty through both floors. RunId, ordinals, class, level, exact current/max HP, behavior, capabilities, trap expertise, intelligence, and surviving formation continue unchanged. Dead members stay dead; no healing/refill/replacement or transition encounter is added.
- EXIT at Floor 1 is deliberate terminal evidence with localized presentation, separate from retreat, wipe, technical rejection, and final completion. Success retains its full-clear contract.

## Decisions and settlement

Transition identity is `(run.floor_transition_decision.phase6.v1, RunId, CurrentFloorInstanceId, NextFloorInstanceId)`; objective identity is `(run.depth_objective.phase6.v1, RunId)`. Config owns both sources. Length-prefixed UTF-8 fields, SHA-256, and the first unsigned big-endian 32-bit word produce independent deterministic rolls. Equality exits for marginal decisions. A wipe or retreat before Completion makes no transition decision; no next Active floor exits without a roll.

Typed production configuration owns all approved tuning, references, minima, thresholds, uncertainty, objective weights, and workload limits. Objective weights normalize in canonical mode order. Unknown Floor 2 perception uses configured uncertainty without inspecting hidden reward/danger content. Shared branch knowledge is not repurposed.

Loot stays carried across descent. Extraction, Heat, cooling, attraction, forecast, demand, branch learning, history, sequence, objective evaluation, and durable publication occur once at complete termination. There is no between-floor checkpoint.

## Lifecycle, compatibility, and UI

Commit validates the current session and exact durable bytes, re-resolves A3 eligibility, mutates a detached candidate, validates canonical/production state, and materializes the same complete snapshot consumed by gameplay. Exact complete-save atomic replacement, durable readback, and reopen validation occur before live publication; rollback remains available during reopen checks.

Floor 1 stays Active. Deactivation cascades through deeper floors; manual activation changes only its target. Activate All Eligible supports the currently configured Floor 2 progression and stops at its first blocker. Contents, custody, IDs, investment, and mana are preserved. Online/offline passive mana continue to use CanonicalActiveFloorResolver, with one/two/one Active-floor behavior. Loot initialization now precedes startup load/offline publication.

Schema stays **11**, with **no migration**. No party, ordinary HP, snapshot, transient objective, between-floor state, eligibility cache, duplicate Active-floor list, or floor knowledge is persisted. Bootstrap adds localized lifecycle controls, stable blocker reasons, and minimal transition evidence; English localization adds 29 keys.

## Validation

- Targeted startup regression: 1/1 passed, zero skips.
- Focused A4: 49/49 passed, zero skips.
- Relevant regression cases in final EditMode: 556/556 passed, zero skips. The obsolete A2 second-Active-floor rejection assertion was updated.
- Full EditMode: 1,252/1,252 passed, zero skips. Four older canonical-run fixtures now supply loot through the existing Phase 5B helper.
- Full PlayMode: 2,751 total, 2,741 passed, zero failed, 10 expected skips. One legacy config fixture now supplies the required typed Phase Six test config; its 8 affected cases and 126-case containing regression class pass.
- Production/configuration/localization/floor-layout/build-gate validation: 79/79 passed in the final EditMode run. English localization has 968 entries, 29 additions, and zero duplicate keys.
- Windows x86_64 Development Build: passed with wrapper exit 0, Bootstrap-only scene, 170,667,976 bytes, 0 errors, and 1 warning for unavailable Unity Cloud native-symbol upload credentials.
- Final `git diff --check`: passed with no whitespace errors.
- No owner manual qualification has been performed. External review and correction of blockers must precede asking the owner to qualify the stable Editor and Windows build/standalone behavior.

## Accepted limits and A5 boundary

Production support is Floors 1–2. Durable shared floor knowledge and richer aggregate multi-floor reporting remain deferred, as do A5 final qualification and Phase 6 closeout. No Floors 3–5 production content, cross-floor graph edges/pathfinding, backtracking, transition damage/loot, healing, fatigue, run-event mana, Phase 7 editor, or unrelated refactor is included. Synthetic deeper-floor tests exercise cascade semantics only.
