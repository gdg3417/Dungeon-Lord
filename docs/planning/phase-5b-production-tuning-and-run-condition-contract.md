# Phase 5B Production Tuning and Run-Condition Contract

**Status:** Owner-approved Phase 5B implementation prerequisite; pending final external review

**Packet:** Phase 5B0 — lock route-choice tuning and live party-condition authority

**Prepared against:** merged PR #210 at `75781a4cfb7a7e837c855608f9a0753139a1bf77`

**Owner approval recorded:** 2026-09-26

**Current writable save schema:** 10

## 1. Purpose and implementation gate

This document closes the production semantics and initial tuning required before Phase 5B gameplay may be implemented. It reconciles the locked Phase 5 branch-decision contract with the Phase 5A state merged in PR #209. It does not activate route choice, optional traversal, corridor encounters, branch outcomes, member health, targeting, or knowledge learning.

All values identified as initial tuning are owned by the single Phase 5B configuration authority. They are not universal game constants and must not be hardcoded in runtime logic. Stable IDs, formula structure, ordering contracts, deterministic hash rules, authority ownership, and semantic invariants are contracts rather than ordinary balance tuning.

Phase 5B implementation remains blocked until this documentation packet receives final external review and is merged. Runtime implementation then requires its own review, tests, qualification, and atomicity evidence.

Governing sources:

- [Phase 5 branching and route-choice design lock](phase-5-branching-and-route-choice-design.md)
- [Post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md)
- [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md)
- [System Spec 6](../../Docs/06%20-%20Adventurer%20Behavior.%20Evaluation.%20and%20Party%20AI.md)
- [System Spec 16](../../Docs/16%20-%20Adventurer_Economy_and_External_World_Simulation.md)
- [Phase 5A static qualification](../../Docs/testing/evidence/phase5a-durable-optional-branch-state/static-review-evidence.md)
- [AI model-selection policy](../../Docs/process/AI_Model_Selection_Policy.md)

## 2. Verified implementation baseline

PR #209 completed Phase 5A. Schema 10 owns optional-branch topology, `corridorContent`, and `sharedBranchKnowledge`; `FloorRouteNodeKind.DeadEnd = 6` is implemented; `ac_300` gates branch construction; and branch construction, content custody, structural invalidation, removal, persistence, and required-route regression protection are qualified.

`BranchKnowledgeRecord` already carries stable floor, branch, and edge IDs; the topology fingerprint; explicit topology, incentive, danger, and confidence known flags; bounded perceived incentive, danger, and confidence; and optional last-confirmed run identity. `BranchTopologyFingerprint` remains structural-applicability authority.

Current runtime remains unchanged by Phase 5B0 and does not yet satisfy this contract:

- `RunSurvivalSummary` carries party size, survivors, deaths, and survivor ratio, but no member roster or average party health.
- `RunSimulationService` independently rolls the survival party in the configured 3–5 range and uses success/failure survivor ratios, composition survivor-ratio adjustments, and casualty-pressure death-count derivation as aggregate casualty authorities.
- `AdventurerPartyCompositionResolver` independently creates a 1–3 member class preview. It has no member identity, personality, capability, or health.
- `CanonicalMvpRouteProjection` deliberately ignores optional edges and DeadEnds and exposes only the required route.
- `RunOutcomeRecord` carries aggregate loot, survival, heat, room resolutions, route outcome, and placement-effect summaries but no detailed Phase 5B branch-decision or branch-outcome evidence.

Phase 5B must replace those conflicting live-party and casualty authorities deliberately. It must not leave them operating in parallel with member-level HP.

## 3. Single configuration authority

The sole future writable Phase 5B tuning authority is one bounded, typed Phase 5B configuration object nested inside the existing `RunSimulationConfig`, serialized through `Assets/_Project/Data/Bootstrap/run_simulation_config.json`, and validated before `RunSimulationService` creation by `BootstrapConfigValidationService`.

The nested configuration has configuration version `1` and stable branch-decision rule source ID `run.branch_decision.rule.phase5b.v1`. Its typed shape must contain logical sections equivalent to:

- identity and version;
- party formation;
- behavior profiles;
- capability definitions;
- intelligence;
- health;
- encounter damage profiles;
- perception and normalization;
- survivability;
- branch-decision weights and thresholds;
- knowledge learning;
- traversal and targeting;
- reporting reason codes; and
- workload limits.

Exact C# type names may be selected during implementation. A second Phase 5B JSON file, ScriptableObject tuning authority, save-owned tuning, duplicated runtime constants, fallback tuning table, or hardcoded gameplay default is not approved.

Validation must fail closed for missing required configuration, unsupported configuration version, duplicate stable IDs, missing required profiles, bands, classes, or content profiles, non-finite values, out-of-domain values, invalid min/max ordering, invalid threshold ordering, nonpositive total decision weight, incomplete collections, and unordered or duplicate authoritative entries where canonical ordering is required.

The following are initial production tuning and remain configurable through this authority: party-size range and initial member level; class health; damage ranges; profile dimensions and selection weights; aggregation parameters; capability values and modifiers; intelligence values and distribution; survivability coefficients and bands; incentive, danger, uncertainty, and reserve normalization; branch weights and thresholds; knowledge-confidence changes; workload bounds; and every other gameplay coefficient in this document.

## 4. Closed decision register

All 24 Phase 5B0 owner-decision rows are closed. Implementation-selected details explicitly delegated below—such as zero- versus one-based member ordinals, exact C# type names, stable profile/intelligence assignment-rule source identifiers, and deterministic integer-damage rounding—are engineering contracts to document and test, not unresolved gameplay authority.

| # | Implementation authority | Approved authority |
|---:|---|---|
| 1 | Production configuration owner | One version-1 Phase 5B object nested in `RunSimulationConfig` and serialized in the existing `run_simulation_config.json`; no fallback or second authority; fail-closed validation; branch rule ID `run.branch_decision.rule.phase5b.v1`. |
| 2 | Authoritative live party instance | One transient 3–5 member roster per run. `RunId` plus canonical `MemberOrdinal` identifies each member. Composition, behavior, capability, health, casualties, route choice, and reporting consume this roster. The 1–3 preview becomes a view of it. |
| 3 | Member behavioral-profile generation | Each member receives one deterministic profile independent of class from the configured profile collection using run identity, member ordinal, and an explicit stable configured assignment rule source. No global RNG, runtime hash, clock, call order, or collection order. Profiles are transient. |
| 4 | Five behavior profiles | Stable IDs, initial dimensions, and explicit initial selection weight `0.20` per profile are approved in section 6. All dimensions and weights are tunable configuration in `[0,1]`; weights form a valid normalized distribution after runtime normalization. |
| 5 | Party-profile aggregation | Arithmetic mean across original party members, equal weighting, no class/survivor/named/hero weighting, no recomputation after casualties, fail closed for empty or invalid party. |
| 6 | Specialist capability mapping | `adventurer.capability.trap_expertise`; configured class values in section 7; party value is the maximum among active members. Personality remains separate. |
| 7 | Intelligence quality and report-confidence interpretation | Party-scoped transient Poor, Standard, and Good bands with stable IDs, interpretation factors, and formation weights in section 7; deterministic assignment from stable run identity and configured rule source. Schema 10 retains one shared branch-report confidence, with transient effective incentive/danger confidence derived in section 7. |
| 8 | Initial and live party health | Real integer member HP; Level 1 initial class MaxHealth in section 8; initial `CurrentHealth = MaxHealth`; deaths only at `CurrentHealth <= 0`; roster-derived average health and casualties; no Phase 5 healing. |
| 9 | Perceived incentive `I` | Initial actual incentive is normalized `LootBonus` using configured reference 6. Attraction is not counted again and post-resolution loot rolls are not foreknowledge. The interface remains broader than loot. |
| 10 | Perceived danger `D` | Initial actual danger is normalized existing `Danger` using configured reference 3. Capacity is not realized danger; ManaPressure and HeatPressure are not injected into `D`. Decisions consume perceived knowledge rather than secret actual values. |
| 11 | Uncertainty `U` | Mean of incentive and danger uncertainty derived from known flags and effective confidence; unknown is 1; inapplicable topology produces `U = 1` and excludes content knowledge; no passive age decay. |
| 12 | Required-route reserve pressure `Q` | Graph-aware required-route suffix after the fork, normalized summed `Danger` using configured reference 6. Do not simulate the whole remaining floor or apply health/capability twice. |
| 13 | Initial inclination `J` | `J = 0` for initial Phase 5. Existing posture or intent scores are not aliases. `wIntent = 0`; the interface remains for future approved goals/intelligence. |
| 14 | Coarse expected survivability | The normalized condition, threat, mitigation, and survivability calculation in section 10, with configured coefficients and optional diagnostic bands. It is not an exact survival probability. |
| 15 | `PartyMinimumSurvivability` | Per-profile values in section 10, averaged over original party members and not recomputed after casualties. Strictly less skips; equality continues. |
| 16 | Branch-appeal weights | `wReward = 1.25`, `wDanger = 1.00`, `wUncertainty = 0.75`, `wReserve = 0.50`, `wIntent = 0.00`; finite, nonnegative, configurable, and total greater than zero. |
| 17 | Confidence thresholds | `SkipThreshold = -0.15`; `EnterThreshold = 0.15`; configured and validated under the locked inequality. |
| 18 | Marginal deterministic decision | Rule ID above; Decision 30 identity order, stable-string hash, tuple fold, unsigned conversion, linear likelihood, and strict comparison remain exact and unchanged. |
| 19 | Knowledge learning | Skip, survivor observation, reconfirmation, contradiction, staleness, topology invalidation, and wipe semantics are approved in section 12. Initial observation confidence is 0.75 and reconfirmation increase is 0.125. |
| 20 | Coarse full-wipe danger owner | Existing persisted run-history death/wipe evidence remains coarse dungeon-level danger evidence. Do not write precise branch knowledge from a wipe and do not add a branch/floor save owner. |
| 21 | Fork execution and traversal ordering | The deterministic 13-step sequence in section 13 governs branch origin, retreat precedence, enter/skip, physical order, tie-breaks, stop/wipe, automatic return, and later retreat. |
| 22 | Branch and current-room encounter/outcome semantics | Current MVP Goblin, Skeleton, Snare, Spike Trap, and Chilling Sigil assignments use `LeadActive`; each damaging assignment resolves one sequential member-level event using room/branch severity, configured profile, formation, targeting, and approved mitigation. Loot uses existing deterministic loot and extraction authorities only when reached; Heat uses existing authority; no optional-branch monsters. |
| 23 | Structured reporting | Detailed Phase 5B decision evidence is transient for MVP with stable reason codes in section 14. Existing aggregate run outcomes remain; durable branch learning stays in `sharedBranchKnowledge`; player text is localization-owned. |
| 24 | Workload and canonical ordering | Configured limits are 1 decision per floor/run, 5 per complete run, 2 corridor assignments per branch, and 5 knowledge updates per run. Breach fails closed with stable `WorkloadExceeded` evidence and no partial mutation. |

## 5. Authoritative transient run party

One transient roster is created at run formation. The authoritative size remains the existing config-owned run range of 3 through 5. Ordinary members gain no durable save identity solely for Phase 5.

Every member has stable run-local identity composed of `RunId` and `MemberOrdinal`. Implementation may choose zero- or one-based ordinals, but it must state the choice explicitly and use it consistently. All member processing and evidence use ordinal order.

Each member logically carries at least `MemberOrdinal`, `ClassId`, `BehaviorProfileId`, `Level`, `MaxHealth`, `CurrentHealth`, active/dead state, derived capabilities, and deterministic formation position/order.

The old composition preview must become a projection of this roster rather than independently creating another party. Current separate size authorities must not survive as competing writable/live authorities.

## 6. Behavioral profiles and aggregation

Profile assignment is independent of class. Class must not imply personality. Each member receives one deterministic profile from the canonical configured collection using the authoritative run identity, member ordinal, and an explicit stable configured profile-assignment rule source. Initial profile-selection weighting is equal across all five profiles.

| Stable profile ID | Initial selection weight | Reward Appetite | Risk Tolerance | Uncertainty Tolerance | Required-Route Commitment |
|---|---:|---:|---:|---:|---:|
| `adventurer.behavior_profile.cautious` | 0.20 | 0.40 | 0.20 | 0.20 | 0.80 |
| `adventurer.behavior_profile.greedy` | 0.20 | 0.90 | 0.60 | 0.45 | 0.35 |
| `adventurer.behavior_profile.curious` | 0.20 | 0.55 | 0.50 | 0.90 | 0.35 |
| `adventurer.behavior_profile.goal_oriented` | 0.20 | 0.35 | 0.45 | 0.40 | 0.95 |
| `adventurer.behavior_profile.gambler` | 0.20 | 0.75 | 0.90 | 0.85 | 0.20 |

Every value and selection weight is configuration-owned and tunable in `[0,1]`. The initial weights total `1.0`; validation requires every configured weight to be finite and nonnegative and the total to be finite and greater than zero. Runtime normalizes the configured weights and must not hardcode a 20-percent selection probability or require future weights to sum exactly to `1.0`.

Each party behavioral dimension is the arithmetic mean of the original run-party members' configured values. Phase 5 MVP uses equal member weighting, no class weighting, no survivor weighting, no named-character or hero weighting, and no post-casualty personality recomputation. Casualties change health, active count, and available capabilities, not the party's underlying personality. Empty or invalid party state fails closed.

Future named-character influence may add configured weighting only under a later approved contract.

## 7. Capability and intelligence

The initial capability is `adventurer.capability.trap_expertise`.

| Class | Initial trap expertise |
|---|---:|
| Warrior | 0.00 |
| Rogue | 1.00 |
| Mage | 0.00 |
| Cleric | 0.00 |
| Ranger | 0.50 |

These values are tunable configuration. Party trap expertise is the maximum value among currently active members. If the best expert dies or becomes inactive before the fork, the available party value changes. Personality and capability remain separate. Expertise may improve interpretation of known trap information and mitigate actual trap harm; it cannot manufacture knowledge missing from shared knowledge. No additional Phase 5 specialist capability is approved.

Intelligence belongs to the transient party, not individual members. It is assigned deterministically at formation from stable run identity, the canonical configured bands, configured formation weights, and an explicit stable configured rule source. It is not durable adventurer identity.

| Stable intelligence ID | Interpretation factor | Formation weight |
|---|---:|---:|
| `adventurer.intelligence.poor` | 0.60 | 0.25 |
| `adventurer.intelligence.standard` | 0.80 | 0.50 |
| `adventurer.intelligence.good` | 1.00 | 0.25 |

Schema 10 has one persisted `ConfidenceKnown`/`Confidence` pair per `BranchKnowledgeRecord`, not separate incentive- and danger-confidence fields. Persisted `Confidence` is the shared confidence in that record's applicable branch report; it is not separate per-fact confidence. No `IncentiveConfidence`, `DangerConfidence`, additional knowledge owner, or schema 11 is approved for Phase 5B MVP.

For an applicable record, transient decision interpretation derives confidence exactly as follows:

```text
EffectiveIncentiveConfidence =
    IncentiveKnown
        ? clamp(Confidence × IntelligenceInterpretationFactor, 0, 1)
        : 0

EffectiveDangerConfidence =
    DangerKnown
        ? clamp(
            Confidence × IntelligenceInterpretationFactor
            + TrapInterpretationConfidenceBonus × ActiveTrapExpertise,
            0,
            1)
        : 0
```

`TrapInterpretationConfidenceBonus = 0.15` is initial tunable configuration. Active expertise therefore improves interpretation of known danger information only; it cannot manufacture missing shared knowledge. Effective values are transient decision inputs and are never persisted separately.

## 8. Member health and route-condition views

Actual health is integer hit points, not normalized `0..1` health. Every transient member has `Level`, `MaxHealth`, and `CurrentHealth`.

- `MaxHealth > 0`.
- Initial `CurrentHealth = MaxHealth`.
- `CurrentHealth` cannot exceed `MaxHealth`.
- Damage subtracts absolute HP.
- A member dies only when `CurrentHealth <= 0`.
- No aggregate resolver may independently declare arbitrary deaths.
- Survivor and death summaries derive from the roster.

Phase 5 MVP uses Level 1 members with initial configurable MaxHealth:

| Class | Level 1 MaxHealth |
|---|---:|
| Warrior | 10 |
| Cleric | 8 |
| Rogue | 7 |
| Ranger | 7 |
| Mage | 6 |

These are initial tunable production values, not hardcoded class constants. Long-term MaxHealth may incorporate class, level, equipment, buffs, and other approved stat authorities. Phase 5B0 does not approve a Level 2–50 growth curve.

The branch decision derives bounded views from member HP:

```text
MemberHealthFraction = CurrentHealth / MaxHealth

AveragePartyHealth =
    average MemberHealthFraction
    across currently active members

ActiveMemberFraction =
    ActiveMemberCount / InitialPartySize
```

If no active member remains, the run is terminal and no branch decision occurs. These fractions are decision inputs, not actual HP authority. Survivor ratio must not substitute for average party health.

No active Phase 5 healing is approved. HP loss persists for the run; there is no automatic between-encounter healing, consumable healing, or Cleric healing. Spec 6's longer-term Support/healing direction remains valid but requires a later contract.

## 9. Formation, targeting, damage, and casualty reconciliation

Initial configurable front-to-rear formation priority is Warrior, Rogue, Ranger, Cleric, then Mage. Same-class ties use canonical `MemberOrdinal`. Active survivors close formation deterministically under the same ordering. Formation priority is config-owned.

Formation is not universal targeting. The encounter architecture must support content-owned policies such as `LeadActive`, `FrontlinePreferred`, `RearlinePreferred`, `SpecificRole`, `AllActive`, `MultiTarget`, and `PositionArea`, without requiring Phase 5B to implement every future policy. Monsters must not be globally assumed to damage the frontline member. Future flanking, rearline attacks, area attacks, cleaves, role targeting, and spell targeting remain possible.

Initial optional-corridor traps use `LeadActive`. Only the current lead active member receives trap HP damage unless future content explicitly owns another policy. Trap expertise remains separate from formation.

For the current MVP content set, Goblin, Skeleton, Snare, Spike Trap, and Chilling Sigil assignments all use `LeadActive`. This is initial configurable content behavior, not a universal targeting rule. The architecture continues to support future content-owned `FrontlinePreferred`, `RearlinePreferred`, `SpecificRole`, `AllActive`, `MultiTarget`, `PositionArea`, and later approved policies, but Phase 5B does not implement flanking, rearline attacks, cleaves, area attacks, or multi-target monster attacks merely because the architecture permits them.

Damage is absolute HP and must not scale as a percentage of target MaxHealth. Initial configurable MVP profiles are:

| Content | Minimum damage | Maximum damage |
|---|---:|---:|
| Goblin | 1 | 2 |
| Skeleton | 1 | 3 |
| Snare | 1 | 2 |
| Spike Trap | 2 | 4 |
| Chilling Sigil | 0 | 0 |

These are initial Level 1/MVP tuning, not universal constants. Content IDs must not be hardcoded to literal damage switch cases. Future content, levels, attacks, gear, defenses, resistances, buffs, spells, and multi-target behavior must be able to supply damage profiles without replacing the health authority.

Existing casualty pressure remains encounter-severity input:

```text
EncounterSeverity = clamp(CasualtyPressure, 0, 1)

Damage =
    deterministic bounded interpolation
    from DamageMin through DamageMax
    using EncounterSeverity
```

The integer-rounding rule is implementation-selected but must be explicit, deterministic, configuration-independent, and tested. Global randomness is prohibited.

For each reached required-route room, resolve the existing approved room-level casualty pressure once and derive:

```text
RoomEncounterSeverity = clamp(RoomCasualtyPressure, 0, 1)
```

Supply that same `RoomEncounterSeverity` to every damaging content assignment resolved in that room. Do not independently reroll or recompute unrelated casualty pressure per target. Each current damaging assignment resolves exactly one member-level damage event in canonical room-content order using its configured damage profile, the room severity, its configured targeting policy, current formation, and approved capability mitigation:

- Goblin: one `LeadActive` event.
- Skeleton: one `LeadActive` event.
- Snare: one `LeadActive` event.
- Spike Trap: one `LeadActive` event.
- Chilling Sigil: `LeadActive` targeting contract with configured `0..0` HP damage; it causes no HP loss while retaining its existing non-HP effects.

The canonical room-content order is Monster, then Trap, then Loot. Within each category, preserve the existing canonical persisted ordering: sequence, then stable `AssignmentId` ordinal order. This order has gameplay consequences: every event applies immediately to the live roster, and if an event reduces the current lead to zero HP, that member becomes inactive, formation closes deterministically, and the next canonical event targets the new `LeadActive` member. Do not batch room damage and assign deaths afterward. Formation determines position, targeting policy determines affected member(s), damage profile determines absolute HP damage, and member HP determines casualties.

Loot is non-damaging and resolves under its existing authority after Monster and Trap processing only when the room was reached and the run is not terminal. If member-level damage causes a full wipe, stop subsequent room-content processing and route progression, derive the wipe from the roster, and do not process later Trap or Loot assignments. Other approved run-stop behavior retains its existing traversal precedence.

The approved future flow is:

```text
placement/content effects -> CasualtyPressure -> EncounterSeverity
encounter source -> configured absolute damage profile
targeting policy + formation -> affected member/member set
EncounterSeverity + damage profile -> deterministic absolute HP damage
individual CurrentHealth -> actual deaths
actual roster -> SurvivorCount / DeathCount / SurvivorRatio
```

Current `SuccessSurvivorRatio`, `FailureSurvivorRatio`, composition survivor-ratio adjustments, and casualty-pressure death-count derivation cannot remain parallel casualty authorities after member HP activates. Actual deaths come only from member HP. `SurvivorCount`, `DeathCount`, and `SurvivorRatio` derive from the roster; loot extraction and Heat continue consuming those summaries. Each legacy field must be explicitly classified during implementation as retired active authority, compatibility-only, derived diagnostic evidence, or safely removable under a separately reviewed change. Legacy fields must not independently kill, resurrect, or overwrite members.

Active trap expertise mitigates actual trap damage:

```text
TrapDamageMultiplier =
    1 - (0.50 × ActiveTrapExpertise)
```

Clamp the multiplier to its valid range. The `0.50` coefficient is configurable. This does not imply automatic detection, avoidance, or disarming.

## 10. Decision inputs and survivability

### 10.1 Perceived incentive `I`

```text
ActualIncentive =
    clamp(LootBonus / IncentiveReferenceLootBonus, 0, 1)
```

Initial configurable `IncentiveReferenceLootBonus = 6`. Do not count `Attraction` again as branch incentive; it remains owned by the broader attraction system. Do not use post-resolution loot rolls as pre-decision knowledge. The interface remains extensible beyond loot.

### 10.2 Perceived danger `D`

```text
ActualDanger =
    clamp(Danger / DangerReferenceValue, 0, 1)
```

Initial configurable `DangerReferenceValue = 3`. Trap capacity is not realized danger. ManaPressure and HeatPressure are not injected into `D`. Parties decide from perceived/shared knowledge rather than secret actual values.

### 10.3 Uncertainty `U`

For applicable topology:

```text
IncentiveUncertainty =
    IncentiveKnown ? 1 - EffectiveIncentiveConfidence : 1

DangerUncertainty =
    DangerKnown ? 1 - EffectiveDangerConfidence : 1

U =
    (IncentiveUncertainty + DangerUncertainty) / 2
```

All confidence and uncertainty values are clamped to `[0,1]`. The effective confidence terms are the transient single-record derivations in section 7; schema 10 persists only the shared branch-report `Confidence` value. If topology knowledge is inapplicable under the existing fingerprint, `U = 1` and content-specific knowledge is not used. There is no passive age decay. Stale is not automatically false; old applicable knowledge retains confidence until contradicted, superseded, or structurally invalidated.

### 10.4 Required-route reserve pressure `Q`

```text
Q =
    clamp(
        SumRemainingRequiredRouteDanger
        / RequiredRouteDangerReference,
        0,
        1
    )
```

Initial configurable `RequiredRouteDangerReference = 6`. The graph-aware projection reads the required-route suffix after the fork only and does not simulate the entire remaining floor. Current health and capability are not applied again to `Q`.

### 10.5 Initial inclination `J`

Initial Phase 5 uses `J = 0`. Existing Cautious/Balanced/Greedy posture and adventurer-intent scores are not aliases because that would risk double-counting personality. The interface remains for later approved goals/intelligence systems.

### 10.6 Expected survivability

```text
Condition =
    0.65 × AveragePartyHealth
    + 0.35 × ActiveMemberFraction

Threat =
    0.70 × D
    + 0.30 × U

MitigatedThreat =
    Threat × (1 - 0.35 × TrapExpertise)

ExpectedSurvivability =
    clamp(
        Condition - 0.60 × MitigatedThreat,
        0,
        1
    )
```

Every coefficient is configurable. This is a coarse normalized assessment, not an exact survival probability and not a player-facing probability.

Optional configurable diagnostic bands are Critical below 0.25, Fragile from 0.25 through below 0.50, Viable from 0.50 through below 0.75, and Strong at or above 0.75.

Initial configurable minimum survivability values are Cautious 0.70, Greedy 0.45, Curious 0.50, Goal-Oriented 0.60, and Gambler 0.35. The run threshold is the arithmetic mean of the original members' configured profile minimums and is not recomputed after casualties.

The hard gate remains exact:

```text
if ExpectedSurvivability < PartyMinimumSurvivability:
    SKIP
```

Equality continues to normal branch evaluation.

## 11. Locked branch-appeal and marginal contract

The Decision 30 formula structure is unchanged:

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

Initial configurable weights are `wReward = 1.25`, `wDanger = 1.00`, `wUncertainty = 0.75`, `wReserve = 0.50`, and `wIntent = 0.00`. All must be finite and nonnegative and `WeightTotal > 0`. Current health and active-member count participate in survivability only.

Initial configurable thresholds are `SkipThreshold = -0.15` and `EnterThreshold = 0.15`. Validation remains `-1 <= SkipThreshold < EnterThreshold <= 1`. `BranchAppeal <= SkipThreshold` skips deterministically; `BranchAppeal >= EnterThreshold` enters deterministically. Boundary equality is not marginal.

Only the strict marginal band uses Decision 30's deterministic resolution. The ordered identity remains exactly:

1. `BranchDecisionRuleSourceId`
2. `RunId`
3. `FloorInstanceId`
4. `OptionalBranchId`

The implementation must use the exact locked `StableStringHash` character fold and ordered tuple fold, convert the signed 32-bit result to `uint`, divide by `4294967296.0` for `[0,1)`, and use:

```text
EntryLikelihood =
    (BranchAppeal - SkipThreshold)
    / (EnterThreshold - SkipThreshold)

ENTER only when DecisionRoll < EntryLikelihood
```

Exact equality is `SKIP`. Runtime `GetHashCode`, global/shared RNG, wall clock, call order, iteration position, dictionary order, room order, and unrelated state are prohibited authorities.

## 12. Knowledge learning

Skipping does not reveal loot, traps, precise danger, or hidden content. A party that physically reached the fork and later retains at least one survivor may confirm applicable topology or branch existence.

Directly observed incentive or danger with survivors begins at configurable `InitialObservationConfidence = 0.75`. Reconfirming the same applicable observation increases configurable `ReconfirmationConfidenceIncrease = 0.125`, clamped to `1.0`. These updates apply to the one persisted shared branch-report `Confidence` value. Both currently known incentive and danger subsequently consume that same persisted confidence, while `IncentiveKnown` and `DangerKnown` independently control whether each fact exists; confidence never makes an unknown fact known. Contradictory direct survivor evidence replaces the relevant perceived fact, resets the shared confidence to the initial observation value, and updates last-confirmed run identity. Independent incentive/danger confidence histories would be a post-MVP persistence redesign requiring separate save/schema review.

There is no passive confidence decay. Existing topology-fingerprint applicability remains authoritative. Old applicable knowledge remains usable until contradicted, superseded, or structurally invalidated.

A full wipe creates no precise branch-specific content knowledge. It does not write exact traps, loot, cause, or branch danger. Existing persisted run-history death/wipe evidence remains the coarse dungeon-level danger signal where existing systems consume recent failures. `BranchKnowledgeRecord` is not overloaded, no new branch/floor save owner is added, and schema 11 is not approved.

## 13. Fork execution, traversal, and outcomes

Execution order is deterministic:

1. Traverse the required route to the branch origin.
2. Resolve the required-route encounter at the origin under existing behavior.
3. Resolve existing retreat/continue authority.
4. Retreat ends branch processing; no branch decision or traversal occurs.
5. Continuing evaluates the branch decision.
6. `SKIP` continues immediately on the required route.
7. `ENTER` traverses the optional corridor toward the DeadEnd.
8. Resolve corridor content in physical path order.
9. Equivalent position/order ties use stored sequence and then `AssignmentId` ordinal order.
10. A full wipe ends the run.
11. An existing run-stop condition ends the run.
12. Survivors completing the branch return automatically to the fork without retriggering content or making another branch decision.
13. Existing retreat authority may run again after return using updated condition before required-route progress resumes.

No discretionary mid-branch reversal is added.

Corridor traps use their configured absolute-damage profile, target `LeadActive`, use casualty pressure as encounter severity, apply active trap-expertise mitigation, damage only targeted members, update individual HP, and derive casualties from HP.

Branch loot resolves only when its physical location is reached. It uses the existing deterministic loot authority, existing loot tuning, and existing extraction/loss semantics; it joins carried/generated run loot and provides no route-choice foreknowledge.

Trap/branch Heat feeds the existing run Heat authority. No branch-only Heat authority is permitted. MVP optional branches contain no monsters, while the member targeting architecture remains compatible with later monsters and richer targeting.

## 14. Reporting and reason codes

Detailed Phase 5B decision evidence is transient for MVP. It must support deterministic tests, debugging, reproduction, balancing, and immediate player-facing reporting where appropriate, without creating durable detailed decision history.

Approved decision reason codes are:

- `branch.decision.retreat_precedence`
- `branch.decision.survivability_refusal`
- `branch.decision.appeal_skip`
- `branch.decision.appeal_enter`
- `branch.decision.marginal_skip`
- `branch.decision.marginal_enter`
- `branch.decision.invalid_configuration`

Approved outcome reason codes are:

- `branch.outcome.skipped`
- `branch.outcome.completed`
- `branch.outcome.wiped`
- `branch.outcome.stopped`

Player-facing text remains localization-owned. Durable learned knowledge remains in `sharedBranchKnowledge`. Existing `RunOutcomeRecord` continues to carry normal aggregate outcomes unless implementation evidence later proves a save change unavoidable. Durable historical branch-decision detail is a plausible post-MVP extension, but Phase 5B0 allocates no save fields and authorizes no schema 11.

## 15. Workload and canonical ordering

Initial configurable workload limits are:

| Bound | Initial value |
|---|---:|
| Branch decisions per floor per run | 1 |
| Branch decisions per complete run | 5 |
| Corridor content assignments processed per branch | 2 |
| Branch knowledge updates per run | 5 |

Five corresponds to the locked MVP maximum floor count. Two corresponds to the current one-trap plus one-loot optional-branch model. These are tunable MVP workload limits, not permanent architectural ceilings.

A workload breach fails closed, emits stable `WorkloadExceeded` evidence, and performs no partial branch decision, loot, Heat, route, health, death, or knowledge mutation. Floors, branches, members, corridor assignments, observations, and evidence use explicit ordinal/canonical order. Dictionary or incidental collection enumeration is never simulation authority.

## 16. Schema, atomicity, and determinism boundary

Schema 10 is sufficient for this approved design because ordinary run members and individual HP are transient, detailed decision evidence is transient, durable learning already uses `sharedBranchKnowledge`, coarse wipe danger uses existing run-history evidence, and no persistent ordinary-adventurer identity is introduced. Schema 11 is not authorized.

If later implementation uncovers an unavoidable persisted-state requirement, work must stop and document the exact requirement for separate save/migration review. Convenience or speculative future use is not evidence.

Future Phase 5B implementation must preserve one roster; deterministic formation, targeting, and damage; actual HP-owned death state; stable IDs; canonical ordering; side-effect-free decision evaluation; atomic complete-state mutation; save compatibility; configuration-owned tuning; localization ownership; bounded workloads; and no duplicate health or casualty authority.

A failed branch/run mutation must not partially apply health, death, loot, Heat, route state, or shared knowledge.

## 17. Explicitly deferred systems

This contract preserves architectural room for later adventurer and monster levels, stat growth, equipment, armor, resistances, buffs and debuffs, spells, different attacks, multi-target attacks, flanking, rearline targeting, healing, named adventurers, and heroes. It does not define or implement them.

Level 2–50 health progression, full monster/adventurer scaling, detection and disarming, Cleric healing AI, consumable healing, durable ordinary-member identity, detailed durable branch-decision history, branch monsters, discretionary backtracking, and post-MVP branch topology remain outside Phase 5B0.

## 18. Phase 5B0 completion state

All 24 former owner-decision rows now have approved authority. The configuration architecture, initial tuning, transient party, member health, deterministic damage, normalization, survivability, appeal, knowledge, traversal, reporting, workload, and schema boundaries are closed for implementation planning.

The packet remains documentation-only and pending final external review. Phase 5B gameplay implementation remains unstarted. Final review must confirm that every numeric gameplay value is configuration-owned, Decision 30 semantics are unchanged, schema remains 10, and no runtime, production data, save, migration, Unity asset, scene, prefab, ProjectSettings, localization, fixture, or `.meta` change entered this packet.
