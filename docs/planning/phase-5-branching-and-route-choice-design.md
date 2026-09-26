# Phase 5 Branching and Route Choice Design Lock

**Project:** Dungeon Lord

**Status:** Owner-approved design decisions, reconciled for repository documentation merge

**Approved:** 2026-09-17

**Repository reconciliation refreshed:** 2026-09-19

**Verified repository baseline:** merged PR #208 at `ad026a29b1f8020a7ab8c682ac3c98da1ebf341c`

**Intended repository path:** `docs/planning/phase-5-branching-and-route-choice-design.md`

## 1. Purpose

This document records the owner-approved design decisions for Phase 5 optional branching and adventurer route choice.

It closes the owner-decision portion of the Phase 5 gate identified by:

- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`
- `docs/planning/post-gd60-mvp-execution-plan.md`
- `docs/planning/gd63-spatial-and-progression-design-decisions.md`

Phase 5 still requires implementation, configuration, deterministic tests, integration review, and gameplay validation. This document does not authorize hardcoded tuning values, speculative systems, save-schema changes, advanced AI, or post-MVP feature expansion.

All numeric weights, thresholds, confidence-band limits, normalization curves, influence multipliers, and other tuning values remain configuration-owned and must not be invented in runtime code.

### 1.1 Current repository baseline and sequencing

This decision lock was prepared and reconciled against merged PR #207, `Phase 4: Add canonical offline passive mana grants`, at `cf9ff2a261f6776bbd9f2d3939ca9db346488ca4`.

PR #208 merged this design lock at `ad026a29b1f8020a7ab8c682ac3c98da1ebf341c`. Phase 5A is the current implementation packet: it owns schema 10, durable optional-branch/corridor-content/shared-knowledge state, research-gated construction/removal, and required-route regression protection. Phase 5B remains the distinct route-choice and run-integration packet. Phase 5A does not implement `BranchAppeal`, production decision weights/thresholds, traversal, branch encounter resolution, or run-driven knowledge learning.

At the verified preparation baseline:

- `main` is `cf9ff2a261f6776bbd9f2d3939ca9db346488ca4`.
- Phase 3 is closed through PR #200.
- Phase 4 structural economy, returned-content redeployment, content acquisition, direct unassignment, canonical passive online mana, test portability, and canonical offline passive mana are merged through PRs #201 through #207.
- Save schema remains 9.
- PR #207 added no migration or new persisted gameplay authority.
- Run-event mana, durable Core Level progression, research mana effects, active soft-cap tuning, and authoritative clock-cheat enforcement remain deferred.
- Phase 5A implementation is in progress; Phase 5B runtime route choice remains unimplemented.

Repository documentation that describes PR #207 as unmerged, offline passive mana as future work, or Phase 4 as unimplemented is stale and conflicts with this recorded baseline.

This baseline statement is status context only. It does not change the behavioral decisions below.

## 2. Locked scope

### 2.1 MVP branch structure

For MVP:

- Each floor has one required route.
- At most one optional branch is allowed per floor after the approved branching unlock.
- The optional branch is a dead-end detour from the required route.
- MVP optional branches are corridor routes containing traps and potentially loot.
- MVP optional branches do not contain branch-room monster encounters.
- A continuing party makes at most one optional-branch decision per floor per run.
- Completing the optional dead end automatically returns the party to the required route.
- Automatic return is not another branch decision.
- Already resolved traps, loot, or other encounters do not retrigger on return.
- No discretionary mid-branch reversal is added by Phase 5.
- Existing retreat behavior can still end a run when applicable.

### 2.2 Full-game extensibility

The full game may later expand optional branches into multi-room routes containing:

- monsters
- traps
- loot
- side bosses
- objectives
- multiple encounter types
- additional reward and danger sources

The Phase 5 decision architecture must permit that expansion without replacing the route-choice model.

### 2.3 Explicit Phase 5 non-goals

Phase 5 does not require:

- map purchasing
- information brokers
- rumor-market simulation
- explicit adventurer-to-adventurer information transactions
- detailed individual adventurer memory
- persistent ordinary-adventurer identity
- named-character or hero implementation
- consumable-aware branch choice
- carried-loot risk in branch choice
- fatigue or generic travel-cost simulation
- multi-room branch planning
- monsters inside MVP optional branches
- deliberate misinformation systems
- per-party thought bubbles or detailed player-facing reasoning
- a full player analytics dashboard
- advanced discretionary backtracking
- alternate entrances
- alternate completion or descent terminals

## 3. Core model

The Phase 5 branch decision is based on a dungeon proposition interpreted by a party.

At a high level:

`Perceived incentive - perceived risk, interpreted through party behavior and current condition -> branch choice`

The system must distinguish:

1. actual dungeon state
2. shared world knowledge about the dungeon
3. the current party's usable interpretation of that knowledge
4. the party's current condition and capabilities
5. the final deterministic route decision

The route selector consumes normalized, bounded decision dimensions. It does not directly simulate raw loot tables, individual future traps, or future combat outcomes.

## 4. Owner-approved decisions

### Decision 1: Base branch-choice model

Use a hybrid reward-risk model.

The dungeon creates the base proposition through perceived reward and perceived danger. Party characteristics modify how that proposition is interpreted.

Conceptually:

`Branch appeal = perceived reward - perceived danger + party modifiers`

Party differences should influence the same branch without replacing the underlying dungeon-driven proposition.

### Decision 2: Pre-run knowledge versus in-run perception

Use a combination of imperfect prior information and party-specific in-run perception.

Before entering:

- Adventurers may possess incomplete or stale information based on previous surviving runs and external knowledge.
- They do not receive perfect dungeon information.

During the run:

- Party composition and specialist capabilities affect what they can actually detect, verify, interpret, or handle.
- Being told a hidden trap exists is different from actually locating or disabling it.
- A relevant specialist, such as a rogue-like trap specialist, may be much better at acting on known or suspected trap information.

### Decision 3: Shared world knowledge plus party-specific information quality

Use shared dungeon knowledge as the world-level information authority, then filter it through party-specific preparation and interpretation quality.

Do not implement fully separate world knowledge for every ordinary adventurer.

The model must allow later systems such as:

- guild intelligence
- purchased maps
- information brokers
- unreliable sources
- scouting
- reputation
- deliberate deception

without requiring those systems in Phase 5.

### Decision 4: Knowledge propagation from survivors and wipes

Normal knowledge propagation requires at least one survivor.

Only information actually observed by that party can improve specific shared knowledge.

Examples:

- Skipping the branch does not reveal branch contents.
- Seeing obvious loot can improve loot knowledge.
- Failing to detect a hidden trap does not reveal that trap.
- Detecting a hidden trap and surviving can improve trap knowledge.

A full party wipe still creates a coarse world signal that the dungeon or floor is dangerous.

A wipe does not reveal:

- the exact room of death
- the exact trap
- the exact monster
- the exact branch contents
- the exact cause of death

unless a later system supplies that information.

### Decision 5: Knowledge changes after dungeon edits

Use a hybrid stale-information model.

Major structural changes:

- invalidate affected topology knowledge

Content-level changes:

- do not automatically erase or rewrite shared knowledge
- can leave existing reports in circulation
- may therefore make previously correct information stale and wrong

No passive knowledge decay is required for Phase 5.

A fact becoming old does not make it false.

### Decision 6: Party intelligence quality and specialist interpretation

Use two layers:

1. general party preparation or intelligence quality
2. category-specific interpretation from party composition and specialists

General preparation controls how much shared knowledge reaches the party and how current or reliable it is.

Specialists can improve interpretation of relevant known information.

Specialists cannot reveal information that shared knowledge does not contain merely because they are specialists.

In-run perception remains separate from pre-run intelligence interpretation.

### Decision 7: Staleness and correctness are separate

Shared knowledge preserves the last observed fact until it is contradicted, invalidated, or superseded.

A fact can be:

- old and still correct
- old and now wrong
- recently confirmed and correct
- contradicted and replaced by newer evidence

Age may affect how much a sophisticated party trusts a report, but age does not automatically erase or invert the known fact.

Example:

`Goblin reported on Branch 2`

If the goblin remains there, the old report remains correct.

If the player replaces it without outside discovery, the world may continue believing the goblin report until new evidence emerges.

### Decision 8: Player-facing reasoning and scale

Normal gameplay does not provide a detailed explanation for every individual party decision.

The simulation must remain internally explainable and testable, but player-facing understanding should scale to many simultaneous parties.

Phase 5 should:

- preserve structured internal decision evidence for tests and debugging
- report the route actually taken
- report branch-specific loot, danger, and heat outcomes as required by the roadmap

Phase 5 should not build:

- detailed per-party reasoning UI
- numeric thought breakdowns
- a major analytics dashboard

Long-term player-facing understanding should primarily come from aggregate behavior, trends, outcomes, and selective inspection where useful.

### Decision 9: Ordinary adventurers versus named characters and heroes

Use a hybrid identity-persistence model.

Most ordinary adventurers and parties do not require durable individual identity in the Phase 5 route-choice simulation. They may be instantiated as lightweight run/party entities while the external world preserves ordinary-adventurer lifecycle effects at the pooled or cohort level.

A limited class of notable adventurers may later become persistent named characters or heroes with:

- durable individual identity
- individual history
- accomplishments
- relationships
- rivalries
- grudges
- unusual influence

Phase 5 does not implement the named-character or hero persistence system.

The branch-decision architecture must not block it.

#### Spec 16 reconciliation

This decision clarifies, rather than silently ignores, the existing locked language in `Docs/16 - Adventurer_Economy_and_External_World_Simulation.md`.

Spec 16 already says that MVP adventurers are simulated at a high level as pooled counts per region and level band, while also saying that adventurers persist across runs, gain levels and gear, and may retire.

For ordinary adventurers, Phase 5 interprets that lifecycle persistence as population/cohort persistence unless a later approved system explicitly promotes an adventurer to durable individual identity. Ordinary lifecycle effects such as progression, gear distribution, retirement, death, circulation, or regional counts may persist in aggregate without requiring every ordinary adventurer to own a permanent save identity.

Named characters or heroes are the intended future path for durable individual persistence.

Spec 16 and its duplicated locked-summary wording in `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md` must remain reconciled with this clarification so that the two authorities do not contradict each other. This clarification is documentation-only and does not implement the future named-character system.

### Decision 10: Current party condition affects branch danger

The same branch should be evaluated differently depending on the party's current condition at the fork.

Current condition changes how threatening perceived branch danger is.

Earlier encounters therefore matter strategically.

A player may weaken a party before it reaches an optional branch, making that same branch less attractive.

### Decision 11: Phase 5 dynamic condition inputs

Phase 5 initially uses:

- average party health
- surviving or active party-member count

Do not require consumable or carried-loot inputs for the initial implementation.

The decision interface must allow later addition of:

- depleted healing or other consumables
- limited-use abilities or resources
- already-carried loot and extraction risk

without replacing the branch-choice model.

### Decision 12: Perceived reward

Phase 5 uses perceived loot as the first concrete branch incentive.

Perceived loot value is modified by party relevance or preference.

Conceptually:

`Reward appeal = perceived loot value interpreted through party preference`

The underlying system should use a broader concept such as incentive or reward appeal rather than hardcoding the route selector specifically to loot.

Later incentive categories may include:

- exploration
- objectives
- glory
- information gathering
- rescue
- rare monsters
- quests
- other motivations

### Decision 13: Uncertainty is itself a form of risk

Unknown information must not be treated as known safety.

Perceived danger includes:

- known or believed danger
- uncertainty caused by missing or weak information

Party personality influences how strongly uncertainty is treated as risk.

A cautious party can strongly dislike uncertainty.

A curious or gambler-oriented party can tolerate uncertainty more readily.

### Decision 14: Confidence bands and deterministic variation

Use a hybrid confidence-band model.

Clearly unfavorable decisions:

- deterministically skip

Clearly favorable decisions:

- deterministically enter

Only genuinely marginal decisions:

- use deterministic seeded variation

The seeded result must remain reproducible for the same decision identity and inputs.

Do not use Unity global random state or call-order-dependent randomness.

### Decision 15: One party behavioral profile

Ordinary parties derive one deterministic behavioral profile from their member composition.

The route selector operates on the party profile rather than making every member cast an independent vote at the fork.

Future named characters or heroes may receive configured influence multipliers that allow them to pull the derived party profile more strongly toward their own preferences.

### Decision 16: Hybrid aggregation by attribute type

Different party-profile attributes may aggregate differently depending on what they represent.

Behavioral preferences:

- are collectively derived from party members
- may use configured weighted aggregation

Specialist capabilities:

- may be dominated by the best relevant expert or another configured specialist rule

Current condition:

- comes from live party state rather than personality aggregation

Named or hero influence:

- may later use configured additional weighting

Do not hardcode one universal aggregation formula for all attribute types.

### Decision 17: Initial inclination and binding fork decision

A party may enter the dungeon with an initial inclination toward or against an optional branch based on its goals and pre-run intelligence.

The binding route choice occurs when the party physically reaches the branch split.

At the fork, the party uses:

- current condition
- usable intelligence
- information learned during the current run
- perceived branch incentive
- perceived branch danger
- uncertainty
- behavioral profile
- remaining required-route considerations

Phase 5 does not add discretionary mid-branch reversal.

### Decision 18: Preserve capability for the required route

The party should consider known mandatory danger that remains after the optional branch.

The party does not optimize or simulate the entire remaining floor.

Instead, it uses a coarse remaining-required-route danger or reserve-pressure input.

The same optional branch may therefore be more attractive near the end of the required route than before a known dangerous mandatory encounter.

### Decision 19: Compact perceived-danger summary

The branch selector consumes a compact perceived-danger summary.

For MVP this summary is primarily derived from:

- perceived trap danger
- estimated or known trap burden
- uncertainty

The route selector does not individually forecast every future trap outcome.

Lower-level knowledge and trap systems are responsible for producing the bounded danger summary.

The same interface must later support branch danger from:

- monsters
- traps
- bosses
- environmental hazards
- other encounter types

without replacing route selection.

### Decision 20: Branch length

Phase 5 does not apply an arbitrary generic penalty per corridor tile.

Known branch length remains an available future input.

Length may later matter when gameplay systems give it actual meaning through:

- additional rooms
- more encounters
- fatigue
- consumable depletion
- time pressure
- carried-loot risk
- other traversal costs

Longer geometry is not inherently penalized merely for being longer.

### Decision 21: Coarse expected survivability

The party derives a coarse expected-survivability assessment.

Do not calculate or expose a false exact survival percentage.

The survivability assessment uses:

- perceived branch danger
- uncertainty
- current average health
- surviving party members
- relevant capability interpretation

The exact bands and thresholds remain configuration-owned.

### Decision 22: Party-specific hard survivability refusal boundary

Each party has a configured minimum acceptable survivability threshold.

If expected survivability is below that threshold:

- the party skips the optional branch regardless of reward

If survivability is at or above the threshold:

- the branch proceeds through the normal reward-versus-risk calculation

A cautious party may have a higher minimum acceptable survivability.

A reckless or gambler-oriented party may tolerate worse survivability.

Even extreme reward should not make ordinary parties knowingly ignore every survival concern.

### Decision 23: Self-assessment

Ordinary Phase 5 parties accurately understand their own current capabilities.

Imperfect information applies primarily to the dungeon and outside-world knowledge.

Future explicit authored traits may deliberately distort self-assessment, such as:

- overconfidence
- timidity
- veteran judgment
- unusual hero confidence

Generic personality must not accidentally double-count this effect.

### Decision 24: Retreat has precedence over branching

Existing retreat or continue logic resolves before optional branch selection.

If the party retreats:

- no branch decision occurs

If the party continues:

- the branch selector decides whether to enter the optional route or stay on the required route

Branch selection cannot override a retreat decision.

### Decision 25: Normalize raw gameplay values before route selection

Underlying gameplay systems may use continuous raw values.

The branch selector consumes normalized, bounded decision dimensions instead of raw gameplay values.

Examples include:

- perceived incentive appeal
- perceived branch danger
- expected survivability
- uncertainty pressure
- required-route reserve pressure

Do not directly compare unrelated raw values such as loot currency against trap damage inside the route selector.

### Decision 26: Personality traits modify normalized behavioral dimensions

Named traits are authored content that modify a small set of normalized behavioral dimensions.

The route selector does not directly special-case every named trait.

Class capabilities and specialist capabilities remain a separate layer from personality.

This allows combinations such as:

- cautious and greedy
- curious and risk-averse
- goal-oriented with strong trap expertise

without route-code special cases.

### Decision 27: Initial Phase 5 behavioral dimensions and traits

Use four normalized behavioral dimensions for Phase 5:

1. Reward Appetite
2. Risk Tolerance
3. Uncertainty Tolerance
4. Required-Route Commitment

Initial Phase 5 behavior profiles should be able to express at least:

- Cautious
- Greedy
- Curious
- Goal-Oriented
- Gambler

These profiles demonstrate meaningful variation without requiring the full documented personality catalog.

Class and specialist capability remain separate.

### Decision 28: Initial Phase 5 intelligence-quality bands

Use three configurable preparation or intelligence-quality bands:

- Poor
- Standard
- Good

The underlying numeric values remain configuration-owned.

The band is assigned deterministically when the party is formed.

Relevant specialists may improve interpretation of information their specialty covers.

Specialists cannot manufacture knowledge the shared world does not contain.

Later systems such as purchased maps or guild intelligence may influence preparation quality without replacing the model.

### Decision 29: Shared-knowledge persistence model

Persist aggregate branch intelligence rather than detailed individual-trap memories for every adventurer.

For each known optional branch, the model should be capable of representing:

- topology or branch-existence knowledge
- perceived incentive or loot knowledge
- perceived trap-danger knowledge
- confidence
- last-confirmed run or revision information

Uncertainty is derived from missing, weak, or stale information.

Survivor observations can update or supersede known facts.

A wipe updates only coarse dungeon or floor danger reputation unless another later system provides specific evidence.

Content changes do not automatically rewrite external knowledge.

Structural changes explicitly invalidate affected topology knowledge.

The exact save representation and migration requirements must be reconciled with the current repository before implementation and must not be guessed.

### Decision 30: Phase 5 decision pipeline

The approved resolver order is:

1. **Resolve retreat first.**  
   If the party retreats, stop. There is no branch decision.

2. **Build the party behavioral profile.**  
   Aggregate member preferences using the approved hybrid rules. Keep specialist capabilities separate. Future named characters may apply configured influence multipliers.

3. **Build usable intelligence.**  
   Start with shared world knowledge. Filter it through party preparation quality. Apply relevant specialist interpretation. Include applicable information learned during the current run before the fork.

4. **Create bounded perceived summaries.**  
   Produce normalized perceived incentive, branch danger, uncertainty, remaining required-route danger, and current party condition. Phase 5 reward is loot. Phase 5 branch danger is primarily traps. Interfaces remain extensible.

5. **Estimate coarse survivability.**  
   Combine perceived danger and uncertainty with current health, surviving members, and relevant capability interpretation. Do not produce a fake exact probability.

6. **Apply the hard survivability gate.**  
   Apply this rule before the normal branch-appeal formula:

   ```text
   if ExpectedSurvivability < PartyMinimumSurvivability:
       SKIP
   ```

   Equality is not refusal. If `ExpectedSurvivability == PartyMinimumSurvivability`, continue to normal branch evaluation. Reward cannot override a below-threshold refusal.

7. **Calculate branch appeal with the locked normalized formula.**
   The normal decision consumes these bounded inputs:

   - `I` = perceived incentive, `[0, 1]`
   - `D` = perceived branch danger, `[0, 1]`
   - `U` = uncertainty, `[0, 1]`
   - `RA` = Reward Appetite, `[0, 1]`
   - `RT` = Risk Tolerance, `[0, 1]`
   - `UT` = Uncertainty Tolerance, `[0, 1]`
   - `RC` = Required-Route Commitment, `[0, 1]`
   - `Q` = remaining required-route reserve pressure, `[0, 1]`
   - `J` = initial branch inclination, `[-1, 1]`, where negative predisposes the party to skip, zero is neutral, and positive predisposes it to enter

   Current health and surviving-party count participate in the coarse survivability calculation and hard gate. They do not receive another direct branch-appeal term because that would double-count current condition.

   The formula uses exactly five nonnegative configuration-owned weights: `wReward`, `wDanger`, `wUncertainty`, `wReserve`, and `wIntent`. No production numeric values are approved here. Configuration validation requires:

   ```text
   WeightTotal =
       wReward +
       wDanger +
       wUncertainty +
       wReserve +
       wIntent
   ```

   `WeightTotal <= 0` is invalid configuration and must fail closed.

   The exact derived terms are:

   ```text
   RewardTerm      = I * RA
   DangerTerm      = D * (1 - RT)
   UncertaintyTerm = U * (1 - UT)
   ReserveTerm     = Q * RC
   IntentTerm      = J
   ```

   After the survivability hard gate passes, calculate:

   ```text
   BranchAppeal =
       clamp(
           (
               + wReward      * RewardTerm
               - wDanger      * DangerTerm
               - wUncertainty * UncertaintyTerm
               - wReserve     * ReserveTerm
               + wIntent      * IntentTerm
           )
           / WeightTotal,
           -1,
           +1
       )
   ```

   `BranchAppeal` therefore has the exact bounded range `[-1, 1]`. The formula contains no additional hidden term. Raw loot value, raw trap damage, branch length, consumables, carried loot, monster threat, and other future dimensions are not inserted directly into the Phase 5 formula. A later approved system may affect an existing normalized input only where specifically authorized.

8. **Apply the locked confidence bands.**
   `SkipThreshold` and `EnterThreshold` are configuration-owned numeric values. Configuration validation requires:

   ```text
   -1 <= SkipThreshold < EnterThreshold <= +1
   ```

   Resolution is exactly:

   ```text
   if BranchAppeal <= SkipThreshold:
       SKIP deterministically

   else if BranchAppeal >= EnterThreshold:
       ENTER deterministically

   else:
       resolve as a marginal decision
   ```

   Boundary equality is deterministic, not marginal.

9. **Resolve only the marginal band with the locked deterministic tie-break.**
   Only when `SkipThreshold < BranchAppeal < EnterThreshold`, calculate the fixed linear mapping:

   ```text
   EntryLikelihood =
       (BranchAppeal - SkipThreshold)
       / (EnterThreshold - SkipThreshold)
   ```

   This mapping increases linearly and monotonically from the skip boundary toward the enter boundary. Its curve is not configuration-selectable in Phase 5.

   The deterministic marginal decision identity is exactly this ordered tuple:

   1. `BranchDecisionRuleSourceId`
   2. `RunId`
   3. `FloorInstanceId`
   4. `OptionalBranchId`

   No new durable `PartyId` is required solely for Phase 5 branch selection because the MVP party is associated with its run identity. Implementation must reconcile with the repository's existing stable `RunId`, canonical `FloorInstanceId`, and persisted `OptionalBranchId` contracts rather than create parallel identity authorities.

   Derive the stable signed 32-bit hash using the repository's explicit convention:

   ```text
   hash = 17

   for each decision-identity field in the exact order above:
       hash = unchecked(hash * 31 + StableStringHash(field))
   ```

   `StableStringHash` is exactly:

   ```text
   if value is null or empty:
       return 0

   hash = 23

   for each character in ordinal string order:
       hash = unchecked(hash * 31 + character)

   return hash
   ```

   Convert the final signed 32-bit hash to an unsigned 32-bit value and derive:

   ```text
   DecisionRoll =
       ((uint)hash) / 4294967296.0
   ```

   The roll domain is `0 <= DecisionRoll < 1`. Resolve the marginal choice exactly as:

   ```text
   if DecisionRoll < EntryLikelihood:
       ENTER
   else:
       SKIP
   ```

   Exact equality between `DecisionRoll` and `EntryLikelihood` resolves to `SKIP`. The same decision identity and decision inputs must always reproduce the same result. The seed and roll must not consume Unity global RNG state, runtime `GetHashCode`, wall-clock time, mutable shared PRNG state, call order, iteration position, unrelated party or floor state, dictionary enumeration order, or room enumeration order.

10. **Commit one route choice.**  
    Enter or skip. Phase 5 adds no discretionary mid-branch reversal. Completing the optional dead end automatically returns the party to the required route without a second branch decision.

Numeric weight values, threshold values, input normalization curves, survivability bands, and modifiers are configuration-owned. The formula structure, term signs, normalized input domains, confidence-boundary semantics, linear marginal mapping, decision-identity fields and order, stable hash algorithm, roll conversion, and equality behavior are locked design.

### Decision 31: Player-facing reporting

Phase 5 does not build a detailed per-party reasoning interface.

Phase 5 does not build a major player analytics dashboard.

Phase 5 must preserve structured internal decision evidence sufficient for:

- deterministic automated tests
- debugging
- balancing
- route-decision reproduction

Player-facing or existing reporting should provide the actual route taken and branch-specific outcomes required by the active roadmap, including applicable:

- loot
- danger
- heat contributions

Long-term player-facing understanding should use scalable aggregate behavior and outcomes rather than requiring explanation of every individual party decision.

## 5. Required behavioral distinctions

Implementation must preserve these conceptual separations.

### 5.1 Reality versus knowledge

The game knows the actual dungeon state.

Shared world knowledge may be incomplete, stale, correct, or wrong.

A party's usable intelligence is filtered from shared knowledge.

### 5.2 Stale versus incorrect

Stale means not recently confirmed.

Stale does not mean false.

A stale report may still be correct.

A content change can make an old report incorrect without automatically informing the world.

### 5.3 Knowledge versus perception

Knowing that a trap was reported does not guarantee that the party can detect, disarm, or avoid it.

Pre-run knowledge and in-run specialist perception are separate systems.

### 5.4 Personality versus capability

Personality controls what the party wants and tolerates.

Capability controls what the party can know, detect, or handle.

A rogue-like specialist can improve trap handling without automatically being greedy, reckless, or curious.

### 5.5 Condition versus personality

Current health and surviving members are live run-state inputs.

Risk tolerance and reward appetite are behavioral-profile inputs.

Do not collapse these into one opaque score before their distinct effects are applied.

### 5.6 Retreat versus route choice

Retreat answers whether the party continues the run.

Branch selection answers which permitted route the continuing party takes.

Branch selection does not own retreat.

### 5.7 Ordinary lifecycle persistence versus individual identity

Ordinary adventurer lifecycle state may persist at the pooled/cohort level without requiring a durable identity for every ordinary adventurer.

Durable individual identity is reserved for later approved named-character or hero systems unless another explicit approved specification requires otherwise.

Phase 5 route choice must not create a per-adventurer persistence dependency solely to satisfy branch behavior.

## 6. Determinism and authority requirements

Phase 5 implementation must preserve:

- deterministic simulation
- stable IDs
- canonical ordering
- decision-specific deterministic seeded variation only where approved
- the exact ordered decision identity `BranchDecisionRuleSourceId`, `RunId`, `FloorInstanceId`, `OptionalBranchId`
- the explicit stable-string hash and ordered tuple-fold algorithm in Decision 30
- the unsigned 32-bit `[0, 1)` roll conversion and strict-less-than marginal comparison in Decision 30
- no use of runtime `GetHashCode` as deterministic seed authority
- no dependence on global RNG state
- no dependence on unrelated iteration order
- no dependence on unrelated party scheduling
- configuration-owned tuning
- no hardcoded gameplay coefficients or thresholds
- side-effect-free decision evaluation before route commit
- atomic state changes
- versioned save compatibility
- explicit migration when a save change actually requires it
- stable reason codes where reason-code contracts are introduced
- localization-backed player-facing text
- mobile-bounded workloads
- no duplicated writable authorities

## 7. Save and migration boundary

This document approves behavior and information concepts.

It does not pre-approve a new save schema.

The current verified preparation baseline is schema 9 after merged PR #207. PR #207 itself added no migration or new persisted gameplay authority.

Before implementation:

- inspect the current save schema and migration state
- inspect existing route/run-state persistence
- inspect current stable party, run, floor, branch, and graph identities
- determine whether shared branch intelligence requires durable save state
- use the smallest compatible representation
- add a schema migration only if the current save contract truly requires one
- never overload unrelated existing fields
- preserve canonical ordering
- preserve older saves through explicit migration when needed

Any save change must be reviewed separately against current repository state.

## 8. Configuration boundary

The following remain configuration-owned and are not fixed by this design lock:

- normalization curves
- trait modifiers
- profile aggregation weights
- specialist interpretation modifiers
- named-character influence multipliers
- Poor, Standard, and Good intelligence numeric values
- survivability bands
- party-specific minimum survivability thresholds
- the five nonnegative branch-appeal weight values
- confidence-band boundaries
- perceived-incentive and perceived-danger input normalization/scaling
- remaining-required-route reserve-pressure input derivation
- knowledge confidence thresholds
- stale-information trust modifiers

Runtime code must consume approved configuration.

The normalized input domains, exact formula structure and signs, hard-gate ordering, threshold equality behavior, fixed linear marginal mapping, decision-identity tuple and ordering, stable hash algorithm, roll conversion, and strict comparison are not configuration choices.

## 9. Phase 5 implementation acceptance criteria

A Phase 5 implementation should not be considered complete until automated and manual evidence demonstrates at least:

1. Branch allowance is locked until the approved research or allowance authority permits it.
2. No floor can exceed the approved MVP optional-branch allowance.
3. Required-route reachability and completion remain valid.
4. A party that retreats never makes a branch decision.
5. A continuing party makes at most one optional-branch decision per floor/run.
6. The binding decision occurs at the fork, not at dungeon entry.
7. Current health and surviving members can change the decision.
8. Perceived loot can change branch appeal.
9. Party reward preference can change the response to the same perceived loot.
10. Known trap danger can change branch appeal.
11. Uncertainty is treated as risk rather than known safety.
12. Different uncertainty tolerance can change behavior with the same intelligence.
13. Remaining required-route danger can influence optional-branch willingness.
14. Survivability below a party's minimum threshold always skips the branch.
15. Reward cannot override the hard survivability refusal boundary.
16. Clearly favorable and clearly unfavorable decisions do not use random variation.
17. Only marginal decisions use deterministic seeded variation.
18. Repeating the same full inputs and stable decision identity produces the same route decision.
19. Adding or reordering unrelated parties does not change an existing party's branch decision.
20. Global RNG state does not control route selection.
21. Specialist knowledge behavior preserves the pre-run/in-run boundary:
    - During pre-run/shared-intelligence interpretation, a specialist may improve interpretation of information the shared world actually contains but may not manufacture specific missing facts.
    - During in-run perception, an applicable specialist may detect, verify, or otherwise observe information that was not previously present in shared knowledge under the applicable gameplay mechanics.
    - Newly observed specific information propagates into shared knowledge only under the approved survivor and actual-observation rules.
22. Survivor knowledge updates require actual observation.
23. A full wipe increases only coarse danger knowledge in the normal Phase 5 path.
24. Content changes can leave stale information without automatically rewriting world knowledge.
25. Structural changes invalidate affected topology knowledge.
26. Old but still-correct information remains usable.
27. MVP optional branches contain traps and potentially loot, not branch-room monster encounters.
28. Branch danger is consumed as a bounded summary rather than individual future-encounter simulation.
29. Branch length has no arbitrary per-tile reluctance penalty.
30. Completing the optional dead end returns the party to the required route without another branch decision.
31. Resolved branch encounters do not retrigger on automatic return.
32. Structured diagnostic evidence exists for deterministic test/debug reproduction.
33. Player-facing behavior does not depend on detailed per-party explanation UI.
34. Route/path reporting and branch-specific loot, danger, and heat outcomes remain available as required by the active roadmap.
35. All gameplay tuning remains configuration-owned.
36. Save behavior remains compatible and explicitly migrated only when required.
37. Localization ownership is preserved for player-facing text.
38. Work remains bounded for future many-party, many-floor simulation.
39. Ordinary adventurer route choice does not require durable per-adventurer save identity; existing ordinary lifecycle persistence remains representable through the external-world pooled/cohort model, while named/hero individual persistence remains deferred.

### 9.1 Formula and deterministic tie-break test obligations

Future implementation evidence must additionally demonstrate that:

1. Inputs outside their approved normalized ranges fail validation or are handled by the approved bounded-input authority rather than silently changing formula semantics.
2. `WeightTotal <= 0` fails configuration validation.
3. `SkipThreshold >= EnterThreshold` fails configuration validation; threshold validation also enforces the locked `[-1, 1]` bounds.
4. `BranchAppeal <= SkipThreshold` always skips without marginal variation.
5. `BranchAppeal >= EnterThreshold` always enters without marginal variation.
6. Only values strictly between the thresholds invoke marginal resolution.
7. Marginal `EntryLikelihood` uses the exact linear formula in Decision 30.
8. Increasing `BranchAppeal` within an otherwise identical marginal decision never lowers `EntryLikelihood`.
9. The same rule source ID, run ID, floor instance ID, optional branch ID, and gameplay inputs reproduce the same result.
10. Changing unrelated party ordering does not alter an existing decision.
11. Changing Unity global RNG state does not alter the decision.
12. Runtime `GetHashCode` is not an authority for the deterministic seed.
13. Decision-identity fields are hashed in the exact approved order.
14. Exact `DecisionRoll == EntryLikelihood` resolves to `SKIP`.
15. Current health and surviving members influence the survivability path and are not also directly added as a second branch-appeal term.
16. Reward cannot override the hard survivability refusal gate.

## 10. Manual gameplay questions for Phase 5 qualification

Automated correctness is not sufficient.

Manual qualification should ask:

- Does changing branch loot meaningfully change how often appropriate parties take the branch?
- Does adding trap danger make parties respond in a way that feels believable?
- Do cautious, greedy, curious, goal-oriented, and gambler-oriented profiles create recognizable behavioral differences?
- Does weakening a party before the fork change its willingness to take the branch?
- Does placing a branch before versus after known mandatory danger produce understandable differences?
- Does imperfect or stale information create believable surprises rather than apparently random behavior?
- Does branch placement create an actual dungeon-building tradeoff?
- Can the player infer useful aggregate behavior without reading per-party decision explanations?
- Does the system remain understandable enough to support the build, run, inspect, revise loop?
- Does the system feel extensible toward larger multi-room branches without making the beginner dungeon unnecessarily complex?

## 11. Deferred full-game extensions

The following are explicitly supported as future extensions but are not Phase 5 requirements:

- multi-room optional routes
- branch monsters
- branch bosses
- multi-branch floors
- cumulative branch danger across many encounters
- consumable-aware choices
- carried-loot risk
- fatigue
- time pressure
- information purchasing
- maps
- guild intelligence
- unreliable information sellers
- explicit rumor propagation
- deliberate misinformation
- rescue objectives
- exploration objectives
- glory and challenge incentives
- persistent named heroes
- hero influence multipliers
- overconfidence and other self-assessment biases
- detailed individual adventurer histories
- expanded aggregate analytics
- discretionary backtracking
- advanced path planning

## 12. Repository consistency requirements

Repository planning/spec documentation must remain reconciled with this design lock so that:

- `docs/planning/post-gd60-mvp-execution-plan.md` reflects merged PR #207, does not call offline passive mana unmerged, and does not describe the Phase 5 branch formula and tie-break as unresolved owner design.
- The Phase 5 roadmap section links to this design lock.
- Section 7 of `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md` points to this document as the approved Phase 5 route-selection policy and uses the same per-floor/per-run branch-decision boundary.
- `docs/planning/gd63-spatial-and-progression-design-decisions.md` does not preserve obsolete current-status language or describe the Phase 5 formula/tie-break as unresolved.
- `Docs/planning/phase-4a-structural-economy.md` does not describe canonical offline mana as future or unmerged.
- `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md` reflects the current Phase 4 status. Existing invariants change only when a concrete cross-spec clarification requires it; a new feature alone does not require a new invariant.
- `Docs/16 - Adventurer_Economy_and_External_World_Simulation.md` and the corresponding Spec 16 text in `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md` reflect Decision 9: ordinary adventurer lifecycle persistence may be represented at pooled/cohort level without requiring durable individual identity for every ordinary adventurer, while named characters/heroes remain the future individually persistent path.
- The current-status addendum in `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md` does not present the old Phase 2/schema-6 state as current.
- `README.md` does not present the dated Phase 2/schema-6 and inactive-spatial state as current.
- Clearly historical evidence/status text remains historical traceability rather than current authority.
- This design lock changes no runtime code, save schema, migration, tuning value, or gameplay behavior.
