Constructed Floor 2 could be inspected for eligibility but could not be safely activated and consumed by a real run. A4 adds zero-cost atomic activation/deactivation, minimal localized Bootstrap controls, and deterministic two-floor execution with one party and one complete-run settlement.

Baseline: merged PR #217, `756607cca58aac584f69cdc3b4c27d30b58eefad`. **This is not Phase 6 closeout.**

Owner manual Editor qualification initially stopped at head `269d2d3760f592aabbc08c023afa49485777a421` when eligible constructed-Inactive Floor 2 did not activate or show a result. The underlying candidate failed strict spatial round-trip with `NonCanonicalBytes` (`gd66.transaction.candidate_invalid`): Unity's detached JSON clone changed NativeCanonical null migration metadata to empty strings, and `SerializeMembers` omitted the normalization already used by full spatial serialization. Member serialization now calls that same normalization before emission. Validators, atomic persistence, snapshot preflight, schema 11, and ownership remain unchanged. Migrated-only lifecycle fixtures missed this native-save shape. Read-only owner-save preparation/preview passed; the owner save was never written and temporary diagnostics were removed.

Lifecycle feedback has independent persistent state immediately below its buttons. Native/migrated GameRoot integration covers activation/deactivation/reactivation, readback/reopen, no unrelated state changes, feedback, and Activate All no-op. Invalid-layout failure stays readable and blocked. Eight missing exact technical-save localization keys were added and all lifecycle reasons are covered. The existing `floor.activation.layout_invalid` authority/table mapping was already correct and is preserved.

## Runtime and activation safety

- `CanonicalRunnableFloorProjection` provides the shared per-floor runtime interpretation. A3 eligibility and A4 runtime share `RequiredFloorTraversal`: traversal stops at the first reached Completion and never executes rooms beyond it.
- `ActiveFloorRunSnapshot` materializes the complete canonical Active prefix before party formation. Floors remain separate and capture route/content, branch plans/knowledge, configuration, loot, initial runtime, ticks, and posture. Real runs and activation preflight use this exact snapshot factory.
- Each floor has its own Phase 5B plan. RequiredSuffix and RemainingRequiredDanger never include another floor's rooms. Existing branch identities, fingerprints, learning, and deterministic ordering are retained.
- The Phase 6 coordinator reuses one RunParty through both floors. RunId, ordinals, class, level, exact current/max HP, behavior, capabilities, trap expertise, intelligence, and surviving formation continue unchanged. Dead members stay dead; no healing/refill/replacement or transition encounter is added.
- EXIT at Floor 1 is deliberate terminal evidence with localized presentation, separate from retreat, wipe, technical rejection, and final completion. Success retains its full-clear contract.

## Decisions and settlement

Transition identity is `(run.floor_transition_decision.phase6.v1, RunId, CurrentFloorInstanceId, NextFloorInstanceId)`; objective identity is `(run.depth_objective.phase6.v1, RunId)`. Config owns both sources. Length-prefixed UTF-8 fields, SHA-256, and the first unsigned big-endian 32-bit word produce independent deterministic rolls. Equality exits for marginal decisions. A wipe or retreat before Completion makes no transition decision; no next Active floor exits without a roll.

Typed production configuration owns all approved tuning, references, minima, thresholds, uncertainty, objective definitions, and workload limits. Objective selection weights normalize in canonical mode order. Shallow uses Weight 0.20 and PullStrength 0.00; target_depth uses Weight 0.50, PullStrength 0.75, and TargetFloorIndex 1 (Floor 2); deepest_reasonable uses Weight 0.30 and PullStrength 0.50. Selection Weight controls assignment frequency, PullStrength controls the selected objective's normalized deeper pressure, and global ObjectiveWeight controls its relative importance in descent appeal. Runtime owns neither a hidden Floor 2 condition nor objective pull tuning. Target-depth pull requires the configured target to be reachable within the immutable Active-prefix snapshot; an absent target provides zero pull even when an earlier next floor exists. Validation enforces the exact objective/transition rule sources and the approved five-Active-floor/four-transition scope. Unknown Floor 2 perception uses configured uncertainty without inspecting hidden reward/danger content. Shared branch knowledge is not repurposed.

Loot stays carried across descent. Extraction, Heat, cooling, attraction, forecast, demand, branch learning, history, sequence, objective evaluation, and durable publication occur once at complete termination. There is no between-floor checkpoint.

Later same-session canonical publications preserve Party, EncounterEvents, BranchOutcomes, FloorTransitions, and DepthObjective through the single `RunTransientEvidence.Retain` helper using RunId/TickStarted identity. These fields remain transient: durable serialization omits them, and reopen does not reconstruct them from current tuning.

## Lifecycle, compatibility, and UI

Commit validates the current session and exact durable bytes, re-resolves A3 eligibility, mutates a detached candidate, validates canonical/production state, and materializes the same complete snapshot consumed by gameplay. Exact complete-save atomic replacement, durable readback, and reopen validation occur before live publication; rollback remains available during reopen checks.

Floor 1 stays Active. Deactivation cascades through deeper floors; manual activation changes only its target. Activate All Eligible supports the currently configured Floor 2 progression and stops at its first blocker. An already-Active Floor 2 is a validated no-op for Activate All after exact session-byte verification: no durable rewrite, session replacement, or runtime republication occurs. Direct Activate retains the existing already-active blocker. Contents, custody, IDs, investment, and mana are preserved. Online/offline passive mana continue to use CanonicalActiveFloorResolver, with one/two/one Active-floor behavior. Loot initialization now precedes startup load/offline publication.

Schema stays **11**, with **no migration**. No party, ordinary HP, snapshot, transient objective, between-floor state, eligibility cache, duplicate Active-floor list, or floor knowledge is persisted. Bootstrap adds localized lifecycle controls, stable blocker reasons, and minimal transition evidence; English localization adds 29 keys.

## Validation

Current correction: focused A4/save tests 172/172; relevant regressions and full EditMode 1,271/1,271; full PlayMode 2,770 total with 2,760 passed, zero failed, and the same ten expected skips; explicit production/configuration/localization/build-gate selection 173/173 and layout/migration-localization selection 82/82. English Bootstrap table has 976 unique entries. Windows x86_64 Development Build passed (Unity 6000.3.2f1, Bootstrap-only Development Build, zero errors, one established Unity Cloud symbol-upload warning); scoped git diff --check passed.

Owner Editor qualification passed direct activation, Activate All already-active no-op, deactivation/reactivation, Floor 1 deactivation rejection, visible localized lifecycle feedback, passive mana one/two/one behavior (171/228/171 mana per hour at Notice 95%), unchanged lifecycle mana, preserved Floor 2 layout/content/investment, Active and constructed-Inactive close/reopen persistence, and no corruption/recovery warning. One-floor baseline, deliberate Floor 1 EXIT presentation, DESCEND, exact party/HP continuity with the same RunId, no roster healing/refill/replacement/resurrection, one settlement, and same-session transient-evidence retention all passed.

Owner Windows Development Build qualification passed lifecycle controls, Floor 2 Active/Inactive behavior, two-floor runs, DESCEND evidence, exact HP continuity, one final settlement, and save/reopen lifecycle persistence, with no crash, assertion, save-recovery warning, or new runtime error. Diagnostics can be difficult to view at some Game-view resolutions and may require a larger Game view or display size; this is an accepted readability limitation. Historical results on prior reviewed heads follow.

- Review 2 targeted objective/lifecycle tests: 4/4 passed; SaveService no-op publication test: 1/1 passed; zero skips.
- Focused A4: 64/64 passed, zero skips.
- Relevant Phase 5B / Phase 6 / lifecycle / route / save regressions: 620/620 passed, zero skips.
- Full EditMode: 1,267/1,267 passed, zero skips.
- Full PlayMode: 2,766 total, 2,756 passed, zero failed, 10 expected skips. The skip set exactly matches the previous run: eight synchronous EditMode-only GameRoot fixtures, the non-Windows inverse filesystem check, and the Windows Player-only standalone qualification test.
- Production/configuration/localization/floor-layout/build-gate validation: 62/62 passed. English localization has 968 entries, 29 A4 additions, and zero duplicate keys.
- Windows x86_64 Development Build: passed with wrapper exit 0 under Unity 6000.3.2f1, Bootstrap-only scene, a 162.8 MB Unity report / 170,912,558-byte output tree, zero build errors, and the established unavailable Unity Cloud native-symbol credentials warning.
- Final `git diff --check`: passed with no whitespace errors.
- At that earlier automated checkpoint owner manual qualification had not yet begun; it subsequently stopped on the activation failure described above. Corrected Editor and Windows standalone manual qualification remain pending after external re-review.

## Accepted limits and A5 boundary

Production support is Floors 1–2. Durable shared floor knowledge and richer aggregate multi-floor reporting remain deferred, as do A5 final qualification and Phase 6 closeout. No Floors 3–5 production content, cross-floor graph edges/pathfinding, backtracking, transition damage/loot, healing, fatigue, run-event mana, Phase 7 editor, or unrelated refactor is included. Synthetic deeper-floor tests exercise cascade semantics only.
