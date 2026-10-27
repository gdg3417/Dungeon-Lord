# H1-F4 context-v2 dependency audit

Initial audit 2026-10-26; policy resolved 2026-10-27. **PRODUCER-BOUND HISTORICAL ACTIVATION SELECTED; IMPLEMENTATION IN PROGRESS.** The owner authorizes historical A>C only with independently validated evidence tying the exact save to original immutable configuration/execution inputs. Schema/profile/defaults or a release mapping without that tie are insufficient. Unproven A>C fails closed unchanged. A=C may activate after all applicable historical validations. Context-v3, one persistent owner hash, the 590-byte envelope and no automatic rebinding are approved. The earlier decision analysis below remains historical; its alternatives are resolved by the selected policy, not a continuing blocker.

Branch `codex/h1-f4-recovery`; runtime HEAD/base remains
`3eb6ccf93558b8145d64e9e63cbefcbd73484602` (#258). The owner approved
implementation and the CombatC0 correction. This report follows the explicit
instruction to audit the other inputs and report further material omissions
before proceeding. It does not reopen approved recovery/lifecycle policy.

Recommended model: GPT-6.1 Sol High, Complex; actual GPT-6.1 Sol, reasoning setting
not independently exposed; fallback GPT-6 Sol High, not used.

## Approved correction applied to the contract

The [design gate](../planning/h1-f4-recovery-design-gate.md) now uses component 1
from the exact validated full `runSimulationConfigJson.bytes`, including CombatC0,
and domain `DL-H1F4-context-v2\n` with actual LF. Components 2–10 and their order,
the format-2 episode grammar and the 590-byte reserve are unchanged. Bind the
immutable bytes that produced the validated service inputs; no resource reload
or legacy reserialization is permitted as a substitute.

The [earlier blocker](h1-f4-context-binding-blocker.md) records this correction as
approved. Its original probe/results remain byte-identical. The pre-correction
approved design is retained as an [exact copy](evidence/h1-f4/context-binding-blocker/approved-design-before-context-v2.md),
SHA-256 `EB11094727CFDE1450A56C60F47949A5DF179F8DAD7280B670EE90BF65FA29EF`.
Runtime implementation has not started because the required audit established
the substantive omissions below.

## Audit of all ten components and their callers

| Component | Dependencies covered by the specified input | Boundary/remaining requirement |
| --- | --- | --- |
| 1: full run_simulation_config.json | Combat stats/HP, heat tiers, placement/traffic/outcome rules, party and Research project IDs, captured run configuration | Does not contain simulation tick cadence, Research scaffold rules or loot item/table data. These come from two different assets below. |
| 2: passive_online_mana | Rate coefficients, heat efficiencies, offline efficiency, optional soft cap | CanonicalPassiveManaService also receives tickSeconds from content_bootstrap; its online tick award and tick count are not bound by this component. |
| 3: structural_economy | Canonical mana capacity and structural economics | Existing spatial/wallet validation still applies. Ordinary edits are behind the recovery barrier. |
| 4: monster_recovery | Initial level, readiness numerator/denominator, mana per HP | Max HP is now covered by corrected component 1. Bound the same resource bytes used by rules capture; bypass Editor reload/fallback paths when provenance is unproven. |
| 5: canonical production manifest | Spatial content identity/version contracts | Use the existing complete validated snapshot's canonical representation. No new hidden formula input was identified in this projection. |
| 6: canonical production catalog | Authored floor/room/corridor/fixed structure/assignment definitions | Occupancy and layout compatibility are additionally covered below; localization tables are presentation-only for these calculations. |
| 7: foreground v1 resource | Versioned profile, units, arrival intervals, execution timing and budgets | Does not contain passive simulation tickSeconds. Frozen schema-13 serialized limits used to construct the profile must remain frozen and selected exactly; arbitrary replacement limits are not authorized. |
| 8: foreground v2 resource | Two-floor timing, transfer and foreground policy/budgets | Same external tick dependency. Supported checkpoint readers retain their separate validation and captured inputs. |
| 9: room-content occupancy | Loaded assignment occupancy rules | Other inputs used to construct this snapshot are the covered catalog/run config. Capture and validate the same immutable inputs. |
| 10: spatial compatibility profiles | Schema/layout selection and supported compatibility mapping | Does not authenticate accepted-tick Research rules, tick cadence or unexecuted admission loot. |

Other inspected execution inputs: already started parties retain run/loot/config
data in their supported immutable start/checkpoint capture. That protects those
captures, but not future opportunities already inside A. Research claim rewards
and ordinary floor/content mutations remain behind the recovery barrier and
cannot be inserted as new actions. Current accepted-drain consequence composition
uses captured run rules plus covered current run config/catalog/monster rules.
The inspected HeatSystem.ApplyEvent is an arithmetic clamp; it does not load
heat_runtime or structure_simulation_config during this drain path. Render/UI,
language, diagnostics and save-frequency settings are not substituted for the
identified progression inputs.

This is a source dependency audit of the pinned current recovery/drain callers;
it is not a claim that arbitrary future callbacks or external service changes are
compatible. Implementation must consume privately owned inputs corresponding to
the hashed bytes and reject unproven substitutions. No additional material
semantic omission was identified in these inspected paths beyond the two assets
and three effects below. Executable integration qualification remains mandatory.

## New omission 1: content_bootstrap.json

This is **different from run_simulation_config.json**. The corrected component 1
does not include it.

### Accepted tick cadence and passive mana

[GameRoot](../../Assets/_Project/Scripts/Core/GameRoot.cs#L714) supplies
`Content.Bootstrap.tickSeconds` to canonical passive mana and retains it as
`_configuredSimulationTickSeconds`.
[LivingDungeonForeground](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs#L87)
computes `tickUnits = tickSeconds * policy.UnitsPerSecond`, then derives each next
tick from the saved closed horizon. At each tick it increments totalTicks and
invokes the detached passive/Research callback before continuing closure.

Counterexample: C=0, A=10000 logical units; profile UnitsPerSecond=1000; no party
event/opportunity due before A. With tickSeconds=10 the accepted interval contains
one tick; with tickSeconds=20 it contains none. All ten approved components,
scheduler C/A and episode fields can be identical. With valid passive production
and spare capacity, the closed wallet/tick result differs. A tick interval is not
saved in the episode or recovered from A, and the profile's movement units do not
establish it. Silently choosing the current bootstrap changes accepted work.

### Research progression and completion-pending

[ApplyForegroundPassiveTick](../../Assets/_Project/Scripts/Core/GameRoot.Foreground.cs#L147)
constructs the Research handler with scaffold configurations from Content.Bootstrap.
The [getters](../../Assets/_Project/Scripts/Core/GameRoot.cs#L2481) select pending,
progress, eligibility, claim and verification configurations from that asset.
[ApplyActiveTick](../../Assets/_Project/Scripts/Services/PlayerResearchActionHandler.cs#L179)
uses these to resolve in-progress state, apply progress and set CompletionPending.
The [progress resolver](../../Assets/_Project/Scripts/Services/ResearchProgressApplyResolver.cs#L71)
uses `elapsedSecondsUsed * config.progressPerActiveSecond`.

For an in-progress valid project, ten accepted active seconds generate 0.1 units
at the production rate 0.01. Changing only the rate to 0.02 generates 0.2 units.
Both rates pass the inspected nonnegative/finite validation; the existing
ruleSourceId, slot/project identity, run config and all ten digest inputs remain
unchanged. Eligibility/enablement changes can likewise affect progress or
completion-pending. A saved ruleSourceId alone does not authenticate its numeric
configuration. This affects the required Research durability during A>C draining.

## New omission 2: loot_config.json

[GameRoot](../../Assets/_Project/Scripts/Core/GameRoot.cs#L533) loads a separate
loot asset into SaveService. At an opportunity inside accepted time,
[LivingDungeonForeground](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs#L255)
can create a run using live `runLoot`.
[ActiveFloorRunSnapshot](../../Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs#L166)
captures it at that admission. Before admission, it is not part of a party's saved
start/checkpoint.

This is accepted foreground work. A recovery drain must not shrink A or consume
a scheduled opportunity differently to avoid binding its dependency. The current
producer advances through the existing A while processing such opportunities;
new observations beyond A remain forbidden by the recovery contract.

Counterexample: change each loot item's worldValue by +1, preserving IDs, table,
weights and counts. The production table always rolls two items. The same
selection therefore has generated world value +2 for every seed, affecting later
run rewards/consequences. All ten digest components remain unchanged. Existing
checkpoints continue to protect their captured loot; they cannot protect a start
which has not yet occurred inside the accepted horizon.

## Initial input-coverage proposal — insufficient by itself

Keep all ten approved components, including corrected component 1, **exactly in
their approved order**. Append two fixed components:

11. SHA-256 of the exact validated immutable contentBootstrapJson bytes used for
    tick cadence and all Research scaffold inputs in the drain callback.
12. SHA-256 of the exact validated immutable lootConfigJson bytes used by the
    accepted-opportunity producer.

Use domain `DL-H1F4-context-v3\n` with actual LF for this twelve-component binding.
The preimage becomes 19+384 bytes rather than 19+320. The saved digest is still
64 lowercase hex characters: no episode field, owner grammar, schema reserve,
wallet or time boundary changes. Freeze historical serializer/schema/checkpoint
behavior and consume the same owned validated bytes without later reloads.

This is presented because the owner explicitly retained ten components and the
other nine definitions. Appending dependencies changes that approved technical
contract; it cannot be silently achieved by repurposing another component or by
pretending a complete bootstrap asset is part of run_simulation_config.json.
No new gameplay coefficient, interval history or persistence framework is
needed. This proposal addresses both newly identified assets together, but it
does not by itself solve the closed-save continuity problem established below.

Required boundary/continuation regressions must reject mismatched pending-episode
context **before draining C→A**, as well as before original/residual healing or
mana. Cover both phases, changed tick cadence, each Research rule group and loot
used at accepted opportunities; unchanged inputs must reproduce the digest and
all outcomes. Previously started parties must preserve their captured historical
inputs and supported v1/v2 checkpoints. These augment the approved CombatC0 tests.

## Completed ownership and phase audit

The three findings are not interchangeable. Classify an input by its current
owner before deciding whether it belongs in a configuration digest:

| Input / state | Actual owner and preservation | Finding |
| --- | --- | --- |
| TickSeconds and future passive-tick positions | Live content bootstrap; scheduler stores A/C, not tick cadence | Confirmed gap for OriginalPending A>C; neither a party checkpoint nor saved totalTicks supplies the interval for the next tick. |
| Research progress rate, cap, eligibility/enablement rules | Live content bootstrap; Research state stores accumulated units, completion-pending and IDs | Confirmed gap for unpaid accepted ticks. Matching a rule ID does not bind its numeric rules. |
| Loot for a party already admitted | Immutable ActiveFloorRunSnapshot private loot JSON, contained in its supported run-start/checkpoint | Dismissed as a live-loot dependency for that party's replay. Its existing capture must be used without substituting current loot. |
| Loot for an unexecuted opportunity at time <=A | Live runLoot passed to ActiveFloorRunSnapshot.Create at admission | Confirmed gap: there is no admitted-run snapshot yet. A is an accepted time horizon, not a list of already captured parties. |
| Current Heat, wallet, HP/revival, floor activation, knowledge, scheduler/allocator and Research progress/completed records | Existing mutable canonical owners, composed by predecessor-bound transactions | Not configuration inputs. Preserve their state/ordering; do not duplicate them in the digest or create new owners. |
| Run heat/cooling rules for an admitted party | Captured snapshot.Configuration and immutable outcome evidence | Already captured; LivingDungeonOutcomeConsequences applies those rules to the current canonical Heat. Current run config is separately used for supported current-context checks/history/objective composition. |
| Passive rate and efficiency coefficients/capacity | Components 1–3, plus current canonical active floors/Heat | Existing approved formula coverage. Original uses post-drain canonical state; residual uses the fixed post-original state, as approved. Canonical state is not a second config digest. Tick cadence remains an additional dependency for foreground draining. |
| Monster max HP/readiness/cost | Corrected component 1 and component 4; current HP/occupancy are canonical | The approved CombatC0 fix covers the static rule gap. Captured combat-start rules protect an admitted execution; post-combat recovery separately consumes the bound current recovery rules. |
| Supported checkpoint executable/profile/timing | Existing format dispatch, captured start/checkpoint identities, supported profile and limits | Preserve supported formats 1/2 and current transfer dispatch. A valid executable/profile ID does not authenticate an external Research rate, loot asset or global tick cadence. |

Phase boundaries narrow the problem but do not remove it. OriginalPending may
contain A>C and therefore needs the unpaid-tick/opportunity inputs. ResidualPending
requires C=A and cannot create additional foreground ticks or admissions; the
three external changes do not retroactively change completed Research/party
state in that phase. Only unpaid residual mana/monster recovery remains. Those
effects still require the bound static rate/recovery inputs. Replaying already
closed Research ticks or replacing an existing party's captured loot would be an
implementation defect, not a reason to hash mutable gameplay state.

At each successful partial drain, updated C/checkpoints/Research units and wallet
are published together; they preserve the completed prefix. After a second crash,
only C→A remains. The missing configuration affects that unpaid suffix, not a
license to recompute the closed prefix. During residual retries there is no new
simulation beyond A; changing canonical Heat/wallet to make a digest match is
forbidden. S/Q and L1 classification remain exactly as approved.

## Context lifetime: confirmed missing identity after closure

The approved grammar places RecoveryContextSha256 inside Episode. Residual
success writes Episode=null. A later ordinary foreground pump may publish A>C
while the episode remains null; bounded pumps are expressly allowed to leave
accepted work outstanding. The saved scheduler contains ProfileId, opportunity
state and A/C, but no global tick/Research/uncaptured-loot context identity.
ContentVersion is a label, not a saved cryptographic identity for those assets.
Neither bootstrap manifest binds their exact bytes: the inspected manifests
declare versions/required schemas, not those input hashes.

Concrete sequence:

1. Residual succeeds at S, clears Episode and resumes ordinary play under context X.
2. A foreground candidate durably accepts A=10000 with C=0, using 1000 units/sec,
   and no party/opportunity due before A. That candidate has Episode=null.
3. The process crashes. The next build loads tickSeconds=20 rather than X's 10,
   or a changed Research progress rule; all previously covered inputs may match.
4. An episode captured now would stamp the new context Y. It cannot establish
   which context accepted the old A. Even a twelve-component episode hash first
   computed on this reopening is too late. The missing tick result is the same
   one-tick-versus-zero counterexample above.

This is a structural design counterexample, not an executed valid-save fixture.
It proves why adding hash inputs solely inside pending episodes is insufficient.
The exact predecessor hash identifies the save bytes; it does not recover absent
configuration. Keeping only a process-local value cannot survive this crash.
Ordinary saves also must not drop a retained identity after an episode closes.

## Context-continuity amendment accepted in principle

Retain the twelve fixed components and domain context-v3 above. Move
the **existing single** RecoveryContextSha256 field from the episode into the
timeProvenance owner. Do not duplicate it. Proposed canonical outer field order:

```json
"timeProvenance":{"FormatVersion":2,"Mode":"preciseRecovery","AccountedFraction100ns":2500000,"RecoveryContextSha256":"<64 lowercase hex>","Episode":null}
```

When open, Episode retains the ten other approved fields in their existing order:
EntryPredecessorSha256, SourceSeconds, SourceFraction100ns, EntrySeconds,
EntryFraction100ns, EntryContext, OriginalMode, Phase, OriginalEligible100ns,
ResidualEligible100ns. Its mode/phase grammar, R, B, durations and transitions are
unchanged. On residual success, W=S and Episode=null publish with the same context
identity and effects. No second W, wallet, elapsed ledger or writable state owner
is introduced. Schema 16 remains untouched. The owner accepts this relocation in
the unimplemented format-2 schema-17 grammar in principle; it is not reopened by
the remaining historical activation check.

The maximum open owner remains **590 bytes**: the exact same key/string/value is
relocated, with the same total members, strings and value nodes. Outer members
change from 4 to 5; episode members from 11 to 10. Value nodes and conservative
writer-record reserve remain 16, and the string-character envelope remains 428.
The closed owner grows from 110 to 201 bytes at maximum fraction width, within the
same 590-byte reserve. The prior source-schema ceilings and the proposed target
ceilings need no size increase. These are JSON envelope calculations; real
ContractJson boundary qualification is still required after review.

Apply the identity check before any candidate transition or effect:

| Publication / interruption | Required context rule |
| --- | --- |
| Native schema-17 creation | Bind the validated immutable context inputs in the first complete save, before foreground acceptance. |
| Every ordinary save/acceptance/drain/action | Persist the same owner hash; compare to the owned runtime inputs before accepting new work or changing state. Never silently stamp a new hash on mismatch. |
| Episode entry | Inherit and validate the predecessor's hash before capturing R/draining; do not replace it with a fresh current-input identity. |
| Original/residual publication and readback | Same hash, checked before calculations/recovery and preserved atomically with W/phase/effects. |
| Residual committed but Q lost | Independently prove the complete candidate, including hash and Episode=null. Closure is not replayed. |
| Closed-save A>C crash/reopen | The surviving owner hash checks the unpaid global work's context before any drain. Party checkpoints independently retain their captured inputs. |
| Same-process rollback/uncertain authority/retry | Keep the proven complete version's hash and captured input bytes; no reconciliation may combine context from one version with gameplay/phase from another. |
| Changed context | Fail closed. A compatible context change requires a separately qualified explicit migration; H1-F4 does not infer numeric/config equivalence or permit resuming unpaid work with new rules. |

### Historical activation needs explicit source mapping

Schemas 7–16 do not contain this global identity. A source reader proving valid
spatial state/checkpoints does not prove an absent tick/Research/uncaptured-loot
configuration identity. An activation must not simply hash the current config and
claim that it was the source's context.

The narrow proposed activation rule is: qualify a source-schema/supported-profile
mapping to its exact historical rule/input set, then explicitly validate the
supported transition to the target input set. Capture the target owner identity
as part of the already approved atomic schema/entry publication. Preserve A/C,
run-start/checkpoint bytes, rewards, mutable state and R. Keep the historical
reader/serializer/migration outputs unchanged; the added mapping is a new-target
activation precondition, not a rewrite of old validity. Unknown or unproven
mappings fail closed with obligations/evidence retained. Do not blanket-refuse
all active v1/v2 saves or claim their required migration qualification has passed.

Source comparison found **11 of the twelve input Git blobs identical** between
the selected schema-15 source c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d and the
baseline. The compatibility-profile asset differs because schema 16 was added.
This supports investigating a narrow qualified mapping; it is not proof for every
historical save, executable, content deployment or imported runtime asset set.
The activation mapping and its evidence remain required review/qualification
work, with no new default tuning or invented source timestamps.

Raw checkout bytes also differ from several Git blobs by CRLF. That is diagnosed
explicitly in the source comparison; production hashes still use exact captured
validated asset bytes without normalization. The initial continuity probe's
raw historical-vs-checkout comparisons are not classified as gameplay changes.
Historical imported/runtime asset bytes must not be fabricated from Git EOL
assumptions. This matters to source-pinned migration and Windows qualification.

### Review decision and executable tests

The owner accepts twelve-input coverage, relocation/persistence of the one identity
and the unchanged 590-byte envelope in principle. These are no longer open design
choices. Historical activation still requires a source-proof authority/mapping;
the final compatibility section below states what the retained evidence proves
and the precise missing decision. L1, S/Q, original/residual arithmetic, single W
and atomic closure policy need no change.

After clearance, test all three unpaid-work counterexamples, an admitted party
retaining old captured loot, both episode phases, close→ordinary A>C→crash→reopen,
two crashes during draining, changed and identical config, original/residual
effect fences, and every writer retaining the hash. Test existing formats 1/2,
frozen schemas/migration outputs, explicit source mappings, unknown mapping
rejection, maximum owner bounds and nonzero closed fractions. Do not replace any
of these with arithmetic/source probes. Full qualification and three-way
benchmarking remain authorized after this correction clears.

## Diagnostic evidence and pending qualification

[probe.py](evidence/h1-f4/context-binding-v2-audit/probe.py) ran successfully with
the bundled Python executable. It pins source HEAD, its own hash, all ten input
assets, the two omitted assets and twelve relevant source files.
[result.json](evidence/h1-f4/context-binding-v2-audit/result.json) SHA-256:
`456a4057b5d8d551f77af0420167eb3cabc3561c5c23a3350347360d7cacda66`.

It checks eight source facts, composes the context-v2 digest using the asset-byte
fixture, verifies changed Goblin HP changes that digest/readiness, verifies
byte-identical copied inputs reproduce it, and computes the three additional
counterexamples. Manifest/catalog asset bytes stand for their canonical loaded
bytes in this diagnostic; the existing canonical loader establishes that identity
on successful validation, but no Unity validation/serialization was run here.
These are diagnostic assertions, **not production regression or qualification
pass counts**. No real pending episode was loaded or rejected in this probe.

The requested executable regressions remain pending: changed/unchanged context,
readiness, both episode phases rejecting before effects, and unchanged actual
historical serialization. Their implementation and full Unit/SIT/fault,
PlayMode/isolated Windows qualification follow correction of the additional
dependencies. No partial helper is exposed as a completed recovery transaction.

Fresh pre-H1-F4 and final H1-F4 performance benchmarks remain mandatory and unrun.
Retained pre-optimization evidence is untouched; no speedup/regression conclusion
is claimed. No owner UAT, draft PR or merge has started. The known native Windows
shutdown issue is unchanged and has no new passing-exit classification.

The resumed [continuity probe](evidence/h1-f4/context-binding-v2-audit/continuity_probe.py)
completed with eight additional source assertions and the proposed JSON envelope
checks. [Continuity result](evidence/h1-f4/context-binding-v2-audit/continuity-result.json)
SHA-256: `6d0759fa346b77a038be5f5c5bc7650c321e32dbfb31fe5083c7b1fa05cd6f40`.
It explicitly distinguishes a representation check from a real canonical save.

The [separate source-blob comparison](evidence/h1-f4/context-binding-v2-audit/source_blob_comparison.py)
completed and differentiates Git blobs, parsed JSON and checkout/EOL bytes.
[Source comparison result](evidence/h1-f4/context-binding-v2-audit/source-blob-result.json)
SHA-256: `85efd84de136d48e11e01c4f1a59fb74ec611bd751d77928d0f38edbd71f285f`.
All earlier diagnostic scripts/results remain unchanged. No Unity, actual
episode-rejection, historical serializer, full regression or performance pass
count is claimed for these diagnostics.

## Final compatibility check A: exact historical evidence and its limits

Historical validity and new-target activation are separate verdicts. The source
reader continues accepting supported historical bytes under its frozen contracts.
It cannot manufacture an absent wall boundary or global configuration identity.

| Retained evidence | What it establishes | What it does not establish |
| --- | --- | --- |
| schema15-09a46810 SHA256SUMS/verdicts and baseline capture harness, source 09a46810aeab7441a2ac09ac6220b0f4cc13d6dd | Exact original active v1/v2 save/config bytes; supported checkpoint formats 2/3; A=100,C=-1; frozen reader acceptance and immutable start/checkpoint identities | Original lastSavedUtcUnix is zero. These originals cannot establish the required recovery boundary. Saved legacy config excludes CombatC0 and does not contain global tick/Research/uncaptured-loot context. |
| TimeProvenanceTests.Source and ExactMigrationPreservesEveryHistoricalByteExceptVersionAndAppendedOwner | Explicitly identified timestamp-bearing test variants and byte-preserving 15→16 expectations | The fake test timestamp 1 is not a historical observation or historical producer proof. The original corpus is not edited. |
| MigratedAcceptedWorkDrainsThroughTheExistingForegroundAuthority | Drains the identified variant to its already accepted horizon using registered executable/profile | Its call supplies literal tickSeconds=10 and a no-op passive callback; A=100 is below the production first tick. It does not qualify unpaid Research/passive ticks or a future admission under an authenticated old global context. |
| H1-F2 qualification-reports.zip Windows source/build/process records and snapshots | One recorded producer/build campaign, source 591b2c3cb27b6d71057cb648ae692609ab4346ab, sealed project inputs and four valid schema-16 save snapshots with positive boundaries. All twelve project-asset hashes match this baseline checkout's bytes. | All four have C=A. They do not supply the requested historical A>C tick/Research/admission case or a general source identity embedded in arbitrary saves. Project-input records are not a newly implemented runtime consumed-byte attestation. The fifth archive snapshot is an intentionally unsupported future-schema projection, not a format-2 save. |

Do not conflate a pre-Q receipt hash with a later AppQuit snapshot: the old
qualification helper emits a receipt and then calls Application.Quit; subsequent
canonical publication can yield a different complete save. Use the exact artifact
cut and its producer/copy records. This is not a reclassification of old passing
tests or the known shutdown problem.

The exact sufficient proof would have to bind: (1) the independently proven
incoming complete-save bytes/hash and schema; (2) its supported producer/executable
and callback/tick contract; (3) the exact original configuration/input set and
validated snapshot interpretations; (4) source A/C, immutable starts/checkpoints
and unchanged closed state; and (5) a qualified source→target mapping preserving
all unpaid work, original grant/recovery rules and historical serialization.
The proof must come from an authorized source contract or independently tied
producer evidence, not be assembled by hashing whichever assets are loaded now.
Code-version labels, schema number, contentVersion or a supported party profile
alone are not such a binding in the inspected current source.

If proof is missing, ambiguous, stale or contradictory, refuse activation before
R capture, drain, mana/healing or source replacement; preserve the source and
pending obligations. A retry/reopen does not upgrade missing proof into evidence.
Old schema-14 active-source rejection and missing-boundary rejection remain
unchanged. Unsupported histories are not repaired by new coefficients, timestamp
fabrication, dropped A or an empty-party assumption.

### Historical authority question — resolved by the owner

The repository has no existing rule which gives an application a unique original
global tick/Research/uncaptured-loot context for every supported legacy A>C save.
Positive frozen-reader results are insufficient to invent that authority. A
production activation path cannot be reduced to whitelisting disposable fixture
hashes, nor can it automatically stamp the current context on old bytes.

The alternatives originally presented for review were:

- An explicit named qualified-release/source-schema/profile→frozen-input mapping,
  with the declared legacy contract accepted as the selection authority and
  qualification covering every supported family. Its exact source/input sets
  must be approved and pinned; they are not derived from the current config.
- Or producer-bound handoff evidence which was tied to the exact save and original
  input set. Unknown/unattested saves remain incompatible. Launching a fresh old
  build against an unknown save does not retroactively prove its earlier rules.

The owner selected the second boundary: independently producer-bound proof.
The first mapping is acceptable only with independently validated exact-save/input
evidence, never by label alone. Implementation supplies this as a read-only trusted
evidence dependency, with no fabricated default or writable journal. Positive
qualified producer cases and unproved cases remain separate. Expected rejection
of the latter does not prevent implementation. Historical A=C uses all applicable
time/content/Research/monster/checkpoint/state validations; no timestamp or
missing global context is silently asserted as historical proof.

## Final compatibility check B: persistent context rebinding

**H1-F4 performs no automatic rebinding to a different digest.** This follows the
approved fail-closed/no-unreviewed-migration rule and the explicit instruction to
document incompatibility if safe rebinding cannot be established. It does not
assert every possible future config change is unsafe; no such migration is part
of this approved contract.

| State | Same exact context | Different context |
| --- | --- | --- |
| OriginalPending, including A>C | Continue only the proven phase/prefix under its old rules | Refuse before drain, grant/healing or write |
| ResidualPending, C=A | Settle only unpaid E; retain R and identity | Refuse; do not reprice E, heal under new rules or reset the phase |
| Episode=null, C<A | Drain only accepted old work after the identity/authority checks | Refuse; closure of the old episode does not erase accepted obligations |
| Episode=null, C=A, W<R | Recover unconsumed absence under the saved context before ordinary work | Refuse; do not move W forward to discard old absence or grant it under new rules |
| Episode=null, C=A, active-party snapshots | Preserve captured rules/loot/HP starts and future reconciliation obligations | Refuse; A=C does not mean all parties or consequences are finished |
| Episode=null, C=A, injured/revival monsters or Research state | Preserve existing canonical HP/wallet/progress/completion and configured interpretation | Refuse; no implicit HP rescale, recovery-price change, progress reset or threshold migration |
| Fully settled, no gap/parties/pending state | Same-hash verification is an identity no-op, not a rebind | Still unsupported without an explicit reviewed transform; do not infer compatibility from apparent quiescence |
| No predecessor: genuine new save | Bind the validated native configuration in its first canonical save | This is initialization, not legacy rebinding; never use it to replace an incompatible predecessor |

Changes in whitespace or other bytes included by the exact digest also require a
reviewed compatibility transform; no semantic-normalization shortcut is added.
Source→schema-17 activation, including its required compatibility-profile append,
is an explicit qualified transition and cannot be advertised as generic ordinary
save rebinding. Its unresolved historical proof gate remains above.

## Exact consumed bytes and every-writer enforcement status

All twelve inputs have named existing loading/validation sites. The current
schema-16 runtime has no context-v3 owner or enforcement; source inspection is
not a claim that every current writer already retains it. Required integration:

| Input(s) | Existing consumption / required ownership |
| --- | --- |
| 1 full run bootstrap | GameRoot's bootstrap parser attaches CombatC0. Capture/own the exact bytes at that call; construct all bound config snapshots from them, not from the historical serializer or another reload. |
| 2 passive config; 3 structural economy | GameRoot loads/parses these resource bytes into their immutable snapshots. Retain the identical consumed bytes alongside those instances. |
| 4 monster recovery | MonsterRecoveryRules.Capture currently reloads Resources internally. Bind the validated policy bytes and full combat config when constructing rules; an unrelated later load/Editor fallback is not proof. Preserve the frozen helper behavior for historical callers. |
| 5/6 manifest/catalog | ProductionSpatialContentSnapshot privately stores canonical validated bytes. Use those exact immutable representations, not an unvalidated substitute graph or later asset reload. |
| 7/8 foreground policies | Parse and own both exact resource inputs once. The policy instances/profile construction used by SaveService must correspond to them, retaining frozen executable/limit dispatch. |
| 9 occupancy; 10 compatibility | Bind the same loaded asset bytes and validated snapshots already consumed by canonical contextual validation. |
| 11 content bootstrap | Content.LoadAll supplies the parsed bootstrap used for tick cadence and all Research scaffold getters. Keep the exact source bytes and privately owned validated interpretation; getter-by-getter fresh config substitutions are forbidden. |
| 12 loot | The exact lootConfigJson used by SaveService/RunSimulationService and new accepted-opportunity snapshot creation. Validate and own its immutable data; keep existing admitted snapshots independent. |

Neither mutable canonical gameplay state nor process-local clock samples are
added to this digest. Byte ownership must agree with service-object ownership;
holding a hash while mutating a parsed config graph would not establish a valid
binding. Actual consumed-byte assertions remain executable qualification work.

Every schema-17 canonical producer must use the common session transition guard:
exact predecessor hash, saved context equal to the owned validated context,
approved W/episode/horizon transition, then complete candidate validation and
atomic persistence/readback. Native creation and explicit activation establish
the hash; ordinary saves, Boot/Quit/pause/focus, foreground acceptance/drain,
Research Start/Claim, manual settlements, mutations/drafts/undo/QA mana retain it.
Uncertainty/readback/acknowledgement cannot rewrite it. Current PrepareReplacement
and NativeCanonicalSaveCreator emit WriteLegacy; those frozen schema-16 routes
must remain intact while the future schema-17 route is added. No current-runtime
retention pass or executable phase/context rejection is claimed.

## Explicit compatibility fixtures and verification

[prepare_fixtures.py](evidence/h1-f4/final-compatibility/prepare_fixtures.py) completed
successfully and produced [fixtures.json](evidence/h1-f4/final-compatibility/fixtures.json),
SHA-256 `82f802f2253e85509896900422834df1c0224d541cebfebdc55d7ef491ef1fbd`.

The packet contains 40 explicit modern semantic expectations: eight same-context
positive identity cases and 32 changed tick/Research/loot/CombatC0 cases across
open phases, closed A>C, offline gap, settled/no-gap, retained party, Research and
monster-deficit states. Negative cases specify three identical reopen refusals
with W/phase/effects unchanged. Positive identity does not bypass other recovery
barriers or prove that configuration rebinding occurred.

Six historical cases reference the two immutable original v1/v2 corpus payloads
and four separately named timestamp-bearing schema-15/16 test derivatives. The
derivatives are written only in this new evidence directory, not the frozen
corpus. They preserve livingDungeon/checkpoint content and are **not historical
producer captures**. Reader-positive originals and missing-boundary/context
activation negatives are distinguished. The packet also records the archived
native schema-16 source/input evidence and C=A saves as positive historical
provenance examples, without pretending they supply a general A>C proof provider.

All 46 original corpus JSON hashes were verified against their unchanged manifest.
The archived project input records match all twelve selected baseline asset-file
hashes. These are actual diagnostic/hash checks. The 40 expectations and six
historical cases are fixture data, **not executed Unit/SIT or canonical activation
tests**. The requested positive production A>C activation/repeated-reopen test
still requires the missing historical proof authority. No fabricated proof token
or future timestamp fixture is counted as that evidence.

## Artifact preservation

All paths below are relative to
`C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord` unless absolute.

| Artifact | Owner / purpose | Retention |
| --- | --- | --- |
| Docs/planning/h1-f4-recovery-design-gate.md | Current task; approved v2 correction and audit status | Updated as authorized; preserve |
| Docs/testing/h1-f4-context-binding-blocker.md | Current task; original defect, now approved correction | Updated status; historical diagnosis retained |
| Docs/testing/evidence/h1-f4/context-binding-blocker/probe.py and result.json | Previous investigation; original source-pinned diagnosis | Byte-identical; preserve |
| Docs/testing/evidence/h1-f4/context-binding-blocker/approved-design-before-context-v2.md | Current task; exact original approved design | Preserve hash-identical copy |
| Docs/testing/h1-f4-context-v2-dependency-audit.md | Current task; consolidated dependency audit/blocker | Preserve |
| Docs/testing/evidence/h1-f4/context-binding-v2-audit/probe.py and result.json | Current task; diagnostic source/input evidence | Preserve |
| Docs/testing/evidence/h1-f4/context-binding-v2-audit/continuity_probe.py and continuity-result.json | Current task; context lifetime/source and JSON envelope diagnostic | Preserve |
| Docs/testing/evidence/h1-f4/context-binding-v2-audit/source_blob_comparison.py and source-blob-result.json | Current task; historical Git/checkout byte distinction | Preserve |
| Docs/testing/evidence/h1-f4/final-compatibility/prepare_fixtures.py and fixtures.json | Current task; explicit compatibility matrix, original hashes and archive evidence | Preserve |
| Docs/testing/evidence/h1-f4/final-compatibility/active-v1.test-only-schema15.json and active-v1.test-only-schema16.json | Current task; separately identified fake-time test variants | Preserve; not historical producer evidence |
| Docs/testing/evidence/h1-f4/final-compatibility/active-v2.test-only-schema15.json and active-v2.test-only-schema16.json | Current task; separately identified fake-time test variants | Preserve; not historical producer evidence |
| Owner checkout/saves, historical builds/evidence and stopped campaigns | Owner / prior campaigns | No writes, cleanup or launches performed |

Only documentation and diagnostic artifacts changed. No runtime/resource/test/
project-setting files, schema constants or frozen migration contracts changed.
No Unity/player/supervisor was launched; the diagnostic Python child exited zero.
No commit or PR was created.

**POLICY RESOLVED — IMPLEMENTATION/QUALIFICATION AND THREE-WAY BENCHMARKS IN PROGRESS**

## Implementation checkpoint — 2026-10-27

The historical authority decision is resolved as stated above. Producer-bound A>C activation and no automatic context rebinding are implementation requirements, not pending policy choices. The exact consumed-byte bundle and checked canonical time transitions are being implemented. Schema 16 remains the active writer until schema-17 integration is complete. Earlier statements about documentation-only work describe their dated review snapshots.

Foundation qualification at `831b1acb159c11e885fe5039b5b26a6ea04c3b1d`: 30 passed, 0 failed, 0 skipped; supervised restoration and protected-evidence preservation passed. The first attempt had 29 passes and one test-assertion failure, retained and corrected without changing a production limit. See [source-pinned foundation receipt](evidence/h1-f4/foundation-20261027/qualification.json). This qualifies the foundation only. Boot/lifecycle integration, the full interruption matrix, PlayMode, Windows and all fresh performance measurements remain pending. No speedup, completed H1-F4 runtime or PR readiness is claimed.

The next 48-case attempt did not reach NUnit: a namespace import was missing and
is now corrected. The independent Job Object completion guard also could not
verify all descendants exited in its existing window. The campaign and reservation
remain INCOMPLETE, preserved under the runbook. See the [specific operational
blocker and reviewed recovery proposal](h1-f4-qualification-process-blocker.md).
This does not reopen the resolved dependency or historical-activation policies.

The owner approved the specific exceptional recovery. Its independent verification
passed: only the three archived settings were restored, protected inputs/evidence
and every original campaign file remained unchanged, and Lease released only that
reservation. The original INCOMPLETE / UNCERTAIN verdict is permanent. A fresh
48-case campaign at `ad161252b316ee7788dd022c9c6283e3e4145d09` completed with
39 passes, 9 failures and no skips. Eight failures use an enlarged generic test
budget where a frozen production profile budget is required; one supplies a
synthetic empty Research class instead of explicit absent state. The fixture
correction keeps all 48 cases and changes no production rules or frozen history.
See the [source-pinned recovery/fresh-attempt receipt](h1-f4-recovery-execution-20261027.json).

## Schema-17 executable checkpoint

The authority decision above is implemented without a new digest component or
durable field. Schema 17 is active in source; schema-16 resource/reader/migration
dispatch remains frozen. The 82-case transaction gate passed at
`38fd7320f4d31a0db5ff9f1f37035f4e13a8caa5`; the 98-case native/continuity gate
passed at `982ff1dc7e2d36a5d2fadf22510e9093100e3da3`, both with zero failures
or skips and verified process/restoration/preservation records in the receipt.

Additional executable dependency checks reject actual configuration/loot,
policy/tick cadence, passive-rate or capacity services outside the owned consumed
bundle. Profile/context validation rejects unsupported executable profiles.
Typed-object equivalence or a supplied digest alone does not create a writable
current-schema session. Native creation and uncertain resolution require fresh
durability/semantic proof before acknowledgement; matching candidate bytes after a
failed flush alone no longer report native success. The legacy native branch is
retained unchanged. Explicit synthetic current fixtures are separate from frozen
source bytes and never count as historical producer evidence.

The lifecycle conservation test exposed and corrected omitted non-stalled return
processing after background interrupted a conditional foreground prefix. That
processing now joins the existing grouped eligible duration; known foreground
stalls stay excluded. This is an implementation correction under the approved
L1/S/Q classifications, with no encoding or policy change. Live root, accepted
A>C repeated-crash, broader regression, PlayMode/Windows and performance evidence
remain required. These focused passes do not claim those missing gates passed.
