# Phase 5B Production Tuning and Run-Condition Contract

**Status:** Draft prerequisite; owner decisions outstanding; not Locked or Approved

**Packet:** Phase 5B0 — lock route-choice tuning and live party-condition authority

**Prepared against:** merged PR #210 at `75781a4cfb7a7e837c855608f9a0753139a1bf77`

**Current writable save schema:** 10

## 1. Purpose and merge gate

This document is the repository-owned prerequisite for production Phase 5B gameplay implementation. It reconciles the approved formula and behavioral design with the state merged in PR #209, but it does not activate route choice, optional traversal, branch encounters, branch outcomes, or knowledge learning.

Every row marked **OWNER APPROVAL REQUIRED** is deliberately unresolved. This document must not be described as Locked, Approved, or merge-ready until the owner supplies the values or semantic rules and those decisions are incorporated into this same branch and PR. No production numeric Phase 5B value is authorized by an example, an existing unrelated coefficient, or a convenient implementation default.

Governing sources:

- [Phase 5 branching and route-choice design lock](phase-5-branching-and-route-choice-design.md)
- [Post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md)
- [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md)
- [Phase 5A static qualification](../../Docs/testing/evidence/phase5a-durable-optional-branch-state/static-review-evidence.md)
- [AI model-selection policy](../../Docs/process/AI_Model_Selection_Policy.md)

## 2. Verified implementation baseline

PR #209 completed Phase 5A. Schema 10 owns the optional-branch graph, `corridorContent`, and `sharedBranchKnowledge`; `FloorRouteNodeKind.DeadEnd = 6` is implemented; `ac_300` gates branch construction; and branch construction, content custody, structural invalidation, removal, persistence, and required-route regression protection are qualified.

`BranchKnowledgeRecord` already carries stable floor, branch, and edge IDs; the topology fingerprint; explicit topology/incentive/danger/confidence known flags; bounded perceived incentive, danger, and confidence; and optional last-confirmed run identity. `BranchTopologyFingerprint` is the existing structural applicability authority. Phase 5B must consume these owners rather than create parallel branch or knowledge state.

The current run model does not yet satisfy the Phase 5B decision contract:

- `RunSurvivalSummary` carries party size, survivors, deaths, and survivor ratio, but no average party health.
- `AdventurerPartyCompositionSummary` carries only class IDs. It has no member personality/profile identity or capability record.
- The configured party-composition preview currently uses a different size range from the run-survival party roll. Phase 5B therefore lacks one authoritative live party instance shared by composition, condition, and route decision.
- `CanonicalMvpRouteProjection` deliberately ignores optional edges and DeadEnds and exposes only required-route rooms.
- `RunSimulationService` has no fork-time route-choice step, optional traversal, corridor encounter resolution, branch-specific outcome, or run-driven knowledge mutation.
- `RunOutcomeRecord` can already carry overall loot, survival, heat, room resolutions, route outcome, and configured/reached/cleared placement-effect summaries, but it has no structured Phase 5B decision or branch-outcome evidence.

## 3. Proposed single configuration authority

The repository's established production pattern is one `RunSimulationConfig` loaded from `Assets/_Project/Data/Bootstrap/run_simulation_config.json`, validated before `RunSimulationService` creation by `BootstrapConfigValidationService`. Existing run tuning, party-composition inputs, posture tuning, placement-effect signals, outcome tuning, stable rule-source IDs, and validation all use that path.

Accordingly, the proposed Phase 5B authority is one bounded typed Phase 5B configuration object nested in `RunSimulationConfig` and serialized in the existing `run_simulation_config.json`. Its future validator must fail closed for missing, duplicate, non-finite, out-of-range, unordered, or incomplete values. A second JSON file, ScriptableObject, save field, fallback constant set, or duplicated writable table is not approved.

**OWNER APPROVAL REQUIRED:** approve this single-owner architecture and the exact typed object/collection shape. Repository evidence does not support a stronger alternative writable tuning authority. The proposal changes no production JSON in Phase 5B0.

## 4. Decision register

| # | Implementation authority | Current evidence and locked boundary | Owner decision still required |
|---:|---|---|---|
| 1 | Production configuration owner | The established run authority is `RunSimulationConfig` plus `run_simulation_config.json`, with bootstrap validation. | **OWNER APPROVAL REQUIRED:** approve the proposed bounded nested Phase 5B object, its stable rule-source ID, required collections, uniqueness rules, and fail-closed absence/version policy. |
| 2 | Authoritative live party instance | Composition preview produces class IDs; survival independently rolls party size. Their configured size ranges currently differ. | **OWNER APPROVAL REQUIRED:** define one deterministic run-party creation boundary and member-slot identity/order used by composition, behavior, capability, health, survivors, and branch choice. Decide how existing preview and survival generation reconcile without parallel parties. |
| 3 | Member behavioral-profile generation | No persistent or transient member personality profiles exist. One party profile must be derived deterministically from member composition, but personality must not be inferred directly from class. | **OWNER APPROVAL REQUIRED:** define the independent member-profile source, stable profile IDs, deterministic assignment inputs and mapping, member ordinal identity, and whether profiles are transient per run or obtained from an existing future-approved authority. No durable ordinary-member identity is required. |
| 4 | Five behavior profiles | The required profiles are Cautious, Greedy, Curious, Goal-Oriented, and Gambler. Each needs four normalized dimensions. | **OWNER APPROVAL REQUIRED:** supply, for every profile, `Reward Appetite`, `Risk Tolerance`, `Uncertainty Tolerance`, and `Required-Route Commitment`, each in `[0,1]`, plus exact stable IDs. No values are proposed here. |
| 5 | Party-profile aggregation | Behavioral preferences are collective and may use configured weighting; named/hero influence is deferred. | **OWNER APPROVAL REQUIRED:** choose the exact aggregation per behavioral dimension, member weights if any, normalization/clamping, empty/invalid-party behavior, and deterministic tie/order handling. A single universal aggregation rule is not assumed. |
| 6 | Specialist capability mapping | Personality and capability are separate. Current class IDs exist, but no approved class-to-specialty or capability values exist. | **OWNER APPROVAL REQUIRED:** define stable capability IDs, which approved member data grants each capability, numeric interpretation, aggregation rule such as best expert versus weighted combination, and separate pre-run interpretation versus in-run detection/handling effects. Do not assume Rogue or any other class is the trap specialist without this approval. |
| 7 | Intelligence quality | Poor, Standard, and Good are locked bands assigned deterministically when the party forms. Numeric meanings and stable IDs are absent. | **OWNER APPROVAL REQUIRED:** approve stable band IDs, numeric interpretation, deterministic assignment identity/mapping, interaction with shared knowledge, and category-specific specialist modifiers. Specialists may interpret existing knowledge but may not manufacture missing pre-run facts. |
| 8 | Initial and live party health | Phase 5 requires average party health and active/surviving member count. The run model has no health field or transition. Survivor ratio is not average health and must not substitute for it. | **OWNER APPROVAL REQUIRED:** define initial average health, health domain/representation, per-encounter health transition authority, aggregation after deaths, Phase 5 healing or explicit lack of healing, ordering relative to retreat/branch decisions, and terminal behavior when active count is zero. No loss formula is proposed. |
| 9 | Perceived incentive `I` | Corridor loot assignments expose option IDs and tiles. Existing placement effects expose `LootBonus` and `Attraction`; loot-table value is resolved during encounters, after a decision. The decision interface must remain broader than loot. | **OWNER APPROVAL REQUIRED:** select the authoritative pre-decision loot/incentive signals, map actual and known/perceived signals to `I` in `[0,1]`, define unknown and stale inputs, relevance/preference handling, caps/normalization, and applicability of `LootBonus`, `Attraction`, or other existing signals. Do not use post-resolution loot rolls as foreknowledge. |
| 10 | Perceived danger `D` | Corridor trap assignments expose option IDs and tiles. Existing placement effects expose `Danger`, `ManaPressure`, and `HeatPressure`; corridor definitions expose trap capacity. Branch danger is primarily traps. | **OWNER APPROVAL REQUIRED:** select which trap/placement signals form actual and perceived danger, define handling/mitigation semantics, aggregate multiple signals, and normalize to `D` in `[0,1]`. Capacity is not automatically realized danger, and no scale is proposed. |
| 11 | Uncertainty `U` | Schema 10 distinguishes unknown values, confidence, last-confirmed run, and topology applicability. Structural invalidation uses the existing topology fingerprint. Content edits may make knowledge wrong without changing topology. | **OWNER APPROVAL REQUIRED:** define how missing topology/incentive/danger, confidence, topology applicability, and staleness combine into `U` in `[0,1]`; approve knowledge-confidence thresholds, any stale-trust modifiers, run-age interpretation, and behavior for inapplicable records. Stale is not automatically false; unknown is not safety; no passive decay is implied. |
| 12 | Required-route reserve pressure `Q` | The canonical graph and required-route room placement effects can identify the ordered suffix after the fork, but the current compatibility projection does not publish fork-aware suffix data. The design forbids simulating the entire remaining floor for this choice. | **OWNER APPROVAL REQUIRED:** define the bounded coarse source signals, fork-to-terminal suffix derivation, capability/condition interaction if any, and normalization to `Q` in `[0,1]`. Approve a deterministic graph-aware projection that reads only the required-route remainder needed for this summary. |
| 13 | Initial inclination `J` | `J` must reflect party goals and usable pre-run intelligence and remain in `[-1,1]`. Existing run posture and `AdventurerRunIntentSummary` scores were designed for other behavior and are not Phase 5 authority. | **OWNER APPROVAL REQUIRED:** define the goal/intelligence inputs, combination and normalization, stable rule source, and neutral/missing-data behavior. Do not alias an existing posture or intent score without explicit approval and semantic reconciliation. |
| 14 | Coarse expected survivability | Locked inputs are perceived danger, uncertainty, average health, active members, and relevant capability. It must not claim an exact survival probability. | **OWNER APPROVAL REQUIRED:** define the representation/bands, combination rule, capability interpretation, bounds, ordering, and stable reason evidence. No bands, coefficients, or thresholds are proposed. |
| 15 | `PartyMinimumSurvivability` | The hard rule and equality behavior are locked. Profile-specific production values do not exist. | **OWNER APPROVAL REQUIRED:** supply the value for each approved behavior profile, including aggregation/override behavior when a party profile is derived rather than selected directly. |
| 16 | Branch-appeal weights | The formula has exactly five nonnegative configuration-owned weights. | **OWNER APPROVAL REQUIRED:** supply `wReward`, `wDanger`, `wUncertainty`, `wReserve`, and `wIntent`. Validation must require finite nonnegative values and `WeightTotal > 0`. No values are proposed. |
| 17 | Confidence thresholds | Deterministic skip/enter bands are locked; values do not exist. | **OWNER APPROVAL REQUIRED:** supply `SkipThreshold` and `EnterThreshold` with `-1 <= SkipThreshold < EnterThreshold <= 1`. |
| 18 | Marginal deterministic decision | The formula, tuple order, stable hash, unsigned conversion, linear likelihood, and equality behavior are already locked in Decision 30. | **OWNER APPROVAL REQUIRED:** supply only the stable `BranchDecisionRuleSourceId` value stored in the single Phase 5B config owner. No other gameplay choice is open; implementation must incorporate the locked contract by reference exactly. |
| 19 | Knowledge learning and merge semantics | Schema 10 can store bounded branch knowledge. The design locks actual observation, survivor propagation, stale knowledge, and coarse wipe information, but no update values or conflict rules exist. | **OWNER APPROVAL REQUIRED:** define updates after skip, entry with survivors, full wipe, observed incentive, observed danger, new confirmation, and stale-but-applicable information; define overwrite/merge precedence, confidence changes, last-confirmed semantics, and which observations count. Skip must not reveal contents; survivor-specific facts require actual observation. |
| 20 | Coarse full-wipe danger owner | A normal full wipe creates only coarse dungeon/floor danger information and does not reveal exact branch contents. `BranchKnowledgeRecord` is branch-keyed; no separately approved coarse floor-danger owner was found. | **OWNER APPROVAL REQUIRED:** identify the existing compatible owner or approve a schema-10-compatible representation and semantics. Do not silently write precise branch incentive/danger or exact cause from a wipe, and do not pre-authorize schema 11. |
| 21 | Fork execution and traversal ordering | Required-route projection ignores optional edges. Phase 5A owns one optional edge to a DeadEnd and automatic return, but no run execution plan inserts the fork or branch encounters. | **OWNER APPROVAL REQUIRED:** define deterministic fork position resolution, retreat-before-choice integration, the required/optional execution sequence, corridor tile/content encounter order, automatic return point, resolved-content non-retriggering, and behavior when a branch encounter wipes or stops the party. |
| 22 | Branch encounter and outcome semantics | Corridor content has custody and placement, while current encounter resolution is room-oriented. Phase 5B must not infer corridor trap/loot behavior from room behavior without approval. | **OWNER APPROVAL REQUIRED:** define corridor trap resolution, loot observation/resolution/extraction, health/casualty/heat effects, success/failure/stop semantics, and which existing placement-effect or loot authorities are reused. Do not mutate Heat or award loot until this contract is approved and implemented atomically. |
| 23 | Structured reporting | `RunOutcomeRecord` already carries overall run summaries and room resolution. It lacks branch decision inputs/result, fork identity, survivability-gate result, marginal evidence, and branch outcomes. | **OWNER APPROVAL REQUIRED:** approve the minimal structured Phase 5B decision and branch-outcome summary, stable reason codes, persistence versus transient/debug scope, and mapping into existing route/loot/danger/heat reporting. No detailed per-party thought UI is required. Player-facing strings must remain localization-owned. |
| 24 | Workload and canonical ordering | MVP allows at most one decision per continuing party per floor per run. Schema-10 branch knowledge and corridor content already canonicalize by stable IDs. | **OWNER APPROVAL REQUIRED:** approve explicit per-run/per-floor processing bounds and any failure policy at configured limits. All floors, branches, corridor assignments, observations, and evidence must use specified ordinal/canonical order; dictionary or collection enumeration cannot become authority. |

## 5. Locked branch decision contract

The owner-approved formula in Decision 30 remains unchanged. After the hard survivability gate passes:

```text
RewardTerm      = I * RA
DangerTerm      = D * (1 - RT)
UncertaintyTerm = U * (1 - UT)
ReserveTerm     = Q * RC
IntentTerm      = J

WeightTotal = wReward + wDanger + wUncertainty + wReserve + wIntent

BranchAppeal =
    clamp(
        (
            + wReward      * RewardTerm
            - wDanger      * DangerTerm
            - wUncertainty * UncertaintyTerm
            - wReserve     * ReserveTerm
            + wIntent      * IntentTerm
        ) / WeightTotal,
        -1,
        +1
    )
```

All five weights are finite and nonnegative, and `WeightTotal > 0`. Current health and active-member count participate in survivability only and are not duplicated as appeal terms.

The hard gate remains exact:

```text
if ExpectedSurvivability < PartyMinimumSurvivability:
    SKIP
```

Equality continues to normal appeal evaluation. Confidence-band validation remains `-1 <= SkipThreshold < EnterThreshold <= 1`; equality at `BranchAppeal <= SkipThreshold` skips deterministically, and equality at `BranchAppeal >= EnterThreshold` enters deterministically.

The marginal decision is not redefined here. The implementation must reproduce Decision 30 exactly:

- identity fields in this exact order: `BranchDecisionRuleSourceId`, `RunId`, `FloorInstanceId`, `OptionalBranchId`;
- the exact `StableStringHash` character fold and ordered tuple fold defined there;
- signed 32-bit hash converted to `uint`, divided by `4294967296.0`, producing `[0,1)`;
- the exact linear `EntryLikelihood = (BranchAppeal - SkipThreshold) / (EnterThreshold - SkipThreshold)`;
- `ENTER` only when `DecisionRoll < EntryLikelihood`; equality is `SKIP`.

Runtime `GetHashCode`, Unity/global or shared RNG, wall-clock time, call order, iteration position, dictionary enumeration, room enumeration, and unrelated state remain prohibited decision authorities.

## 6. Knowledge and schema boundary

Schema 10 appears sufficient for Phase 5B. It already owns branch identity, topology applicability, bounded perceived incentive/danger/confidence, and confirmation-run identity. Phase 5B0 changes no save field, serializer, schema owner, migration, fixture, or canonical spatial serialization and does not authorize schema 11.

That conclusion depends on owner closure of the coarse full-wipe danger owner and the persistence scope of structured decision evidence. If later implementation evidence proves that an approved behavior cannot be represented without new persisted state, the evidence must identify the exact missing state and trigger a separate save/migration review. Convenience, preallocation, or speculative future use is not evidence.

Future mutation must be an atomic complete-state operation across the run outcome and every approved affected owner. Validation and decision preview must remain side-effect-free. Frozen historical schemas remain frozen.

## 7. Additional implementation authority discovered by reconciliation

The required list exposed four cross-cutting decisions that need explicit owner closure rather than being hidden inside implementation:

1. **Party identity reconciliation:** the current composition preview and survival roll can describe different party sizes. Phase 5B needs one authoritative transient run-party roster and canonical member order.
2. **Fork-aware run projection:** current canonical projection intentionally removes optional edges. Phase 5B needs a deterministic graph-aware execution projection without creating a second graph authority.
3. **Retreat/stop integration:** the design requires retreat to resolve before branch choice, while current route stops are room-result consequences. The exact pre-fork continue/retreat authority and ordering must be named.
4. **Coarse wipe knowledge ownership:** schema-10 branch records are branch-keyed, while the locked wipe signal is coarse dungeon/floor danger. The owner must choose compatible semantics before knowledge mutation exists.

These are part of the same Phase 5B0 owner review. They do not justify a second Phase 5B0 PR.

## 8. Required implementation constraints after approval

The later Phase 5B implementation must preserve deterministic simulation; stable IDs; explicit ordinal/canonical ordering; the existing research gate; existing topology fingerprint applicability; existing required-route behavior; single-source configuration-owned tuning; stable reason codes; localization-owned player text; bounded mobile-safe work; side-effect-free validation/preview; and atomic complete-state mutation.

It must not introduce a second writable tuning or knowledge authority, infer personality from class, infer average health from survivor ratio, treat unknown as safe, treat stale as false, simulate the entire remaining floor for `Q`, expose a false exact survival probability, or insert hidden terms into `BranchAppeal`.

## 9. Phase 5B0 completion checklist

Phase 5B0 becomes merge-ready only after:

- every **OWNER APPROVAL REQUIRED** row has exact approved values or semantic rules;
- the single configuration shape and all stable IDs are approved;
- profile, intelligence, survivability, normalization, confidence, knowledge-update, health, traversal, outcome, and workload authorities are complete;
- the formula and deterministic marginal contract remain byte-for-byte equivalent in meaning to Decision 30;
- schema 10 sufficiency is reconfirmed against the approved reporting and wipe-knowledge choices;
- no runtime, save, migration, scene, prefab, ProjectSettings, production gameplay data, research data, localization table, fixture, or `.meta` change is included in this documentation packet.

Until then, this packet is ready for owner decision review only, not production Phase 5B implementation.
