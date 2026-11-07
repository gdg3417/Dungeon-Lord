# H1-F4 recovery semantic contract — residual publication and interruption

Semantic contract dated 2026-10-26. Final compatibility check updated 2026-10-27 after acceptance in principle of context-v3 and persistent context continuity.

**Status: all historical-activation authority decisions resolved; schema 17 activated in source and under qualification.** The owner selects independently producer-bound evidence for historical A>C. A frozen release mapping is usable only when evidence ties the exact save to its original immutable inputs; schema/profile/defaults alone never suffice. Unproven A>C sources fail closed unchanged. A=C sources may activate after all applicable historical time/content/Research/monster/checkpoint/state validation succeeds. Twelve context-v3 inputs, one persistent owner hash, the 590-byte envelope, no automatic rebinding, W, L1, S/Q and original/residual ordering remain approved. Frozen schema-16 dispatch/resource bytes are retained. Expected refusal of unproven historical fixtures is not a design blocker. Focused gates are not complete runtime qualification.

## 1. Identity, authority and scope

- Repository: gdg3417/Dungeon-Lord.
- Worktree: C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord.
- Branch: codex/h1-f4-recovery.
- Base and unchanged runtime HEAD: 3eb6ccf93558b8145d64e9e63cbefcbd73484602, merged PR #258.
- Recommended implementation configuration: GPT-6.1 Sol High; Complex, because the cross-system design is approved. Actual model: GPT-6.1 Sol; reasoning setting is not independently exposed. Availability fallback: GPT-6 Sol High; no substitution used. Earlier design-model records remain historical.
- Runtime implementation, qualification and benchmarking are authorized after the context dependency audit clears. The current correction/audit has changed documentation and diagnostic evidence only; no Unity, build, benchmark, PR or owner UAT has started.

The [F2 policy table](h1-f2-time-provenance-paired-cuts.md) is the authority for D1–D6, H1-C01-A and H1-C02-A. It supersedes conflicting older proposed wording in the [hybrid proposal](living-dungeon-hybrid-persistence-proposal.md). Existing canonical save, Research, monster, scheduler and historical executable contracts remain binding.

| Approved policy | Consequence here |
| --- | --- |
| D1 fractional precision | Exact integer 100 ns duration accounting before the existing floating-point mana formula; no per-segment truncation. |
| D2 qualified history | Preserve schema-16 and earlier meanings, readers, limits, migrations, checkpoints and source evidence. |
| D3 ordinary successful-save accrual | Eligible foreground elapsed time remains with TimeService for later processing; publication completion does not consume it. |
| D4 original plus one grouped residual | One original transition and at most one successful residual transition per episode; retries are not extra grants. |
| D5 ordered recovery | Drain existing A>C; original mana and applicable recovery; grouped residual and applicable recovery; then new ordinary work. |
| D6 confirmed work and clock rejection | Preserve accepted/closed work and confirmed effects. Reject invalid clocks or uncertain canonical authority without speculative credit. |
| H1-C01-A atomic closure | Residual wallet, monster effects, accounted endpoint S and closed phase publish together. |
| H1-C02-A normal accrual and prospective overload | No arbitrary slow-write timeout or new workload limit. Known failure stops new progression prospectively; accepted work remains owed. Measured overload thresholds remain separate work. |

### Accepted S/Q rule

The grouped residual consumes eligible recovery duration only through S. The successful complete-save publication closes the episode with accounted endpoint S. Q confirms durable authority and releases the coordinator barrier; **Q does not independently advance the accounted boundary**.

This resolves the former S/Q question. It does not mean a failed candidate establishes S, that physical focus transitions can be reconstructed, or that the existing schema-16 runtime already supports the representation below.

## 2. Cuts, ownership and time classes

Use half-open duration intervals [start,end). Existing scheduler events retain their complete-timestamp ordering and C≤A; this interval notation does not change event tie-breaking.

| Symbol | Meaning and owner |
| --- | --- |
| W | The one durable wall-coverage/accounted endpoint: lastSavedUtcUnix plus its versioned fraction in canonical time provenance. |
| B | Immutable W at episode creation. An original-interval source, never a moving cursor. |
| R | One complete precise recovery-entry observation, captured after canonical authority is independently established and before draining. Immutable for this episode. |
| G | Original grant endpoint: R in precise mode; the historical whole-second projection of R for first legacy activation, as specified in section 10. Derived from mode and R. |
| C / A | Closed / accepted logical horizons in the existing Living Dungeon scheduler. The episode neither duplicates nor independently advances them. |
| E | Exact eligible residual duration already classified through W but not yet granted. A duration obligation, not mana, a wall timestamp or an adventurer horizon. |
| S | A particular residual candidate's complete captured endpoint. It becomes authoritative only if that candidate is independently proven authoritative. |
| Q | Observation of successful durable publication; a process event, not a persisted time boundary. |
| F / U | Observed proven failure / uncertain-authority cut, respectively. Known foreground blocking begins prospectively here. |
| N | Entry observation on reopening an already persisted episode. N is not a replacement for R. |

A wall span is folded exactly once into one of these classes:

| Class | Offline mana eligibility | New adventurer time |
| --- | --- | --- |
| Eligible recovery processing before a successful handoff | Included in the grouped residual | None; only pre-existing A>C drains |
| Observed background during an unfinished episode | Included in the grouped residual | None |
| Valid unknown gap after loss of process-local evidence | Included under the approved pre-O1 unknown-crash rule | None |
| Known foreground persistence stall | Excluded | No new accrual after its observed prospective stop cut |
| Eligible ordinary foreground after a proven handoff | Excluded from offline mana | Existing TimeService monotonic observation |
| Invalid/contradictory clock evidence | No credit or boundary advancement from the invalid observation | No inferred advancement |

A synchronous call merely taking time is not, by itself, a detected failure or an approved overload declaration. Before F/U, its recovery duration is handled by the in-flight rule below. Do not retrospectively backdate a stall to the start of a slow call.

A failure latch and lifecycle state are separate. If a blocked foreground process is observed entering background, that background is eligible under the absence policy. On return, the failure latch still blocks foreground progression until authority and the required transaction succeed.

## 3. Minimal durable semantic representation

### Exact prospective owner grammar

Vnext replaces the existing tenth owner in place. Its canonical field order is exactly:

```json
"timeProvenance":{"FormatVersion":2,"Mode":"preciseRecovery","AccountedFraction100ns":2500000,"RecoveryContextSha256":"<64 lowercase hex>","Episode":{"EntryPredecessorSha256":"<64 lowercase hex>","SourceSeconds":80,"SourceFraction100ns":0,"EntrySeconds":100,"EntryFraction100ns":2500000,"EntryContext":"unknownPostCrash","OriginalMode":"precise","Phase":"originalPending","OriginalEligible100ns":202500000,"ResidualEligible100ns":0}}
```

This illustrative open record requires primary.lastSavedUtcUnix=100, hence W=R=100.25. Hash placeholders are explanatory, not valid fixture bytes. A closed record has the identical five outer fields with `Episode:null`, retaining the context identity. There is no omitted/empty/closed episode object. There is no new primary owner or second context hash. The earlier nested-hash proposal is preserved in the source-pinned evidence copy and is superseded in principle by this owner-level shape.

| Exact field | Grammar and meaning |
| --- | --- |
| FormatVersion / Mode | Exactly integer 2 / string preciseRecovery; Vnext only. |
| AccountedFraction100ns | Integer 0..9999999. Existing primary.lastSavedUtcUnix supplies W's only seconds component. |
| EntryPredecessorSha256 | Exactly 64 lowercase hexadecimal ASCII characters; SHA-256 of complete independently proven entry predecessor bytes, before migration if activating. Immutable. |
| RecoveryContextSha256 | Exactly 64 lowercase hexadecimal ASCII characters on the provenance owner, not in Episode; remains present when Episode=null. Checked against the owned validated inputs before every transition. No automatic different-context rebinding. |
| SourceSeconds / SourceFraction100ns | B, immutable; positive seconds <=253402300799, fraction 0..9999999. |
| EntrySeconds / EntryFraction100ns | R, immutable; same bounds. This plus EntryContext is the durable projection of the existing RecoveryEntryObservation. |
| EntryContext | Exactly knownBackground or unknownPostCrash. Neither known foreground nor persistence-stalled entry can authorize absence. |
| OriginalMode | Exactly precise or legacyWholeSeconds. Legacy is permitted only for the initial schema-16 activation episode and remains until that episode closes. |
| Phase | Exactly originalPending or residualPending. |
| OriginalEligible100ns | D0; immutable nonnegative signed Int64 duration. |
| ResidualEligible100ns | E; nonnegative signed Int64 duration, incremented only with newly classified eligible coverage. |

Canonical integers use ASCII decimal without signs, leading zeros (except zero), fractions or exponents. Exact ordered shapes reject aliases, duplicates, extra/missing fields, escapes substituting for canonical enum/hash spellings, unsupported values and owner collisions. Existing canonical serializer/strict-reader rules apply. No arrays or recursive extension are permitted inside this owner.

Validation requires B<=G<=R<=W, D0<=duration(B,G), E<=duration(G,W), and checked arithmetic. Precise mode derives G=R. Legacy mode requires B fraction zero, G=(R.Seconds,0), unknownPostCrash context, and D0=duration(B,G), an exact multiple of 10000000. ResidualPending requires C=A; OriginalPending permits C<=A. The existing scheduler alone owns C/A. For an open episode, A cannot increase. Transitions preserve immutable fields and reject E decreases except by clearing the entire record in a successful residual candidate. D0 remains present after original settlement as immutable provenance, but Phase prevents repayment.

The largest representable elapsed duration in the permitted positive time domain is 2534023007989999999 units; this fits signed Int64. Both counters also obey their tighter interval bounds. Overflow, negative differences, contradictory classifications and invalid enum/phase transitions fail closed, never saturate. At closure W=S, Episode=null, and the grant/recovery effects publish atomically.

Episode identity is the tuple (FormatVersion, EntryPredecessorSha256, B, R, EntryContext, OriginalMode). No ID allocator, extra timestamp, persisted monotonic epoch, Q, retry counter or interval list exists. The hash binds entry provenance; reopening validates the authoritative complete record, not an unavailable historical predecessor file. It is not an authenticity proof or permission to reconstruct history.

### Exact context binding

RecoveryContextSha256 is SHA-256 over ASCII `DL-H1F4-context-v3\n` followed by twelve raw 32-byte SHA-256 digests in this fixed order. The corrected first component and unchanged components 2–10 are retained; 11/12 complete the accepted-in-principle configuration coverage:

1. The exact validated full runSimulationConfigJson bootstrap asset bytes, including CombatC0. Capture/own these same bytes when constructing the validated live configuration; do not substitute LegacyGameplayConfigurationContract.SerializeCanonical, an incomplete reserialization, or a later resource reload.
2. The bytes loaded for PassiveOnlineManaConfigurationSnapshot.ProductionResourcePath.
3. Resources structural_economy bytes.
4. Resources monster_recovery bytes.
5. ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Manifest).
6. ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Catalog).
7. Resources living_dungeon_foreground bytes.
8. Resources living_dungeon_foreground_v2 bytes.
9. The loaded roomContentSpatialOccupancyJson bytes.
10. The loaded spatialLayoutCompatibilityProfilesJson bytes.
11. The exact validated contentBootstrapJson bytes used for tick cadence and all Research scaffold interpretation.
12. The exact validated lootConfigJson bytes used by the running services and new starts at accepted opportunities.

The prefix contains one actual LF byte, not a literal backslash/n pair. Each component must first pass its existing applicable validation; hash the very immutable inputs used to construct the services, not a later resource reload. Digest composition is fixed at 19+384 bytes (the ASCII prefix is 19 bytes including LF). No arbitrary variable-length manifest or copied numeric configuration is saved. Both supported foreground profiles are bound, while each active checkpoint still requires its existing supported executable/profile and contextual validation. Full canonical validation, configured capacity, Research and monster invariants remain mandatory; this digest does not replace them. Localized presentation is not a recovery calculation input.

The approved CombatC0 correction preserves all frozen legacy serializers and schemas 7–16, migration outputs, checkpoint formats and the 590-byte owner grammar. Required executable regressions cover Goblin maximum HP 8→10 changing this digest and readiness 6→7, identical inputs reproducing it, both unfinished phases rejecting mismatches before any healing/mana/settlement, and unchanged historical serialization. These runtime regressions remain required and unexecuted while the additional dependency blocker is resolved; source probes are not substitutes.

**Audit finding addressed in principle:** the earlier ten components omitted content_bootstrap.json (simulation tick cadence and Research tick rules) and loot_config.json (new starts inside already accepted A>C). Components 11/12 above address this input-coverage gap. Historical context selection still requires independent proof; adding inputs does not infer which rules produced an old save.

**Completed audit conclusion:** mutable Heat/wallet/HP/Research progress and existing party start/checkpoint data already have canonical/captured owners and must not be added to a configuration digest. Existing admitted parties retain captured loot/run rules; unexecuted accepted opportunities do not. ResidualPending has C=A, so those unpaid foreground effects cannot be inserted into its retry. The accepted-in-principle amendment keeps the same single RecoveryContextSha256 field on timeProvenance between AccountedFraction100ns and Episode, checks it on every publication/reopen and retains it at closure. It adds no field copy or writable state owner. Open maximum bytes remain 590, nodes/record reserve 16 and string characters 428; closed maximum becomes 201 bytes. Executable consumed-input, every-writer and real serialization qualification remain pending, not passed by source inspection.

Historical sources lack this global identity. Activation must use an explicitly qualified source-schema/profile/input mapping, not stamp current input hashes and claim they prove source provenance. Frozen readers/serializers/migration outputs stay unchanged. Unknown mappings fail closed with accepted obligations/evidence retained; required active v1/v2 compatibility is not discarded. This mapping and relocated identity are the specific review gate. The source/byte-envelope probes are not executed migration or ContractJson qualification.

Missing/mismatched inputs block continuation before effects or publication, preserving the save/evidence. This deliberately uses byte equality for the raw inputs: even a whitespace-only change requires the pinned input set or a separately reviewed compatibility migration. Closed saves retain the same persistent context binding. Episode=null and C=A do not authorize adopting a different hash; H1-F4 implements no automatic rebinding, even with an apparently zero gap. Existing snapshot, monster and Research obligations remain canonical. This is one configuration identity in the existing save authority, not a new content version or gameplay coefficient.

### Exact bounded grammar reserve

The maximum syntactic owner member, including its leading comma and key, occupies **590 ASCII/UTF-8 bytes**, **16 parsed value nodes**, **16 writer collection records**, and **428 string characters**. The record reserve counts the containing primary member and all 15 nested members; the parser charges no more than this writer reserve. Maximum numeric widths and longest enums produce this conservative grammar envelope even when their combination fails semantic interval checks. It is a representation bound, not an allowed duration beyond the clock domain.

Relative to schema 16's 92/4/4/71 owner, Vnext needs +498 bytes, +12 nodes, +12 records and +357 string characters. Proposed schema-owned ceilings, retaining existing ratios and all unrelated limits:

| Limit | Frozen 16 | Proposed Vnext |
| --- | ---: | ---: |
| Serialized input / whole candidate bytes | 1376348 | 1376846 |
| Parsed nodes | 10244 | 10256 |
| Collection records | 5380 | 5392 |
| String characters | 1376327 | 1376684 |
| Raw save bytes | 2752696 | 2753692 |
| Raw scan work | 5505392 | 5507384 |

The owner adds one nesting level; the owner object has 5 members and its episode has 10, below the existing depth 32 and member 64 bounds. Arrays, strings, copied values, unknown extensions, diagnostics and simulation budgets remain at their existing configured limits. Preserve schema-13 base-charge exclusions for the dedicated owner. Freeze schema-16 limit dispatch and resources before modifying current-target references. These exact reserves need boundary fixtures and real reader/writer qualification; arithmetic here is not executed parser evidence.

### What W accounts for while an episode is open

In the activated interpretation, "accounted" means that every interval through W is either excluded by a supported classification, already settled, or represented by a durable unpaid obligation. It does **not** mean every interval through W has already paid mana.

This is necessary to retain classified exclusions across a second crash without a second advancing wall cursor:

- OriginalPending owes the immutable original interval plus E.
- ResidualPending proves the original transition already succeeded and owes only E.
- No episode means neither obligation remains through W.

Advancing W while an episode is open must atomically retain the corresponding phase and unpaid duration. No ordinary save may replace W with current UTC, discard E, erase the phase or call the old W→now resolver. This is a new activated contract requiring strict reader/writer review; **it must never reinterpret schema 16**.

For the precise original interval, D0 is the sum of eligible portions of [B,R). Any already observed foreground or stalled portion is excluded, not silently included because B<R. For a normal boot with no surviving classification evidence, the valid [B,R) gap is unknown absence. Same-process resume must use its retained classifications.

For residual accounting, E is the sum of eligible portions of [G,W), including the explicitly defined legacy carry if present. Check B≤G≤R≤W, 0≤D0≤G−B and 0≤E≤W−G in checked integer arithmetic. These range checks do not prove provenance by themselves: only supported predecessor-bound transitions can update these quantities, and loading still requires the existing complete contextual validation.

### Entry and repeated draining

1. Independently recover/open canonical authority. First resolve any still-known pre-entry foreground observation with the existing action/acceptance barrier. Do not relabel that observation as absence merely to enter recovery.
2. Capture one R. Prepare an episode-entry candidate before any recovery-dependent drain/grant.
3. Persist the entry record using the existing complete-save protocol. Initially W=R, D0 describes the original obligation and E is zero in precise mode. A retry may fold additional classified time into E and a later W, retaining the same B and R.
4. Do not start accepted-work draining until the entry record is durably established. If the process dies before that first publication, no durable episode exists; the next process may capture a new R. No episode-dependent gameplay has been published.
5. Every subsequent drain publication preserves B/R/identity/phase, C≤A, all existing checkpoints and the accumulated E/W pair. It may advance C only through complete existing timestamps, never beyond the old accepted A. It may also fold classified wall time into E and W without executing parties for that wall time.
6. A second crash after any drain resumes the saved episode with the same R and remaining C→A work. Capture N, add only the valid unrepresented [W,N) gap to the appropriate pending duration, and keep the original interval fixed.

Phase OriginalPending also covers C=A before the original transition; no separate durable "drain complete" Boolean is needed. ResidualPending requires C=A. New ordinary actions, admissions beyond accepted work, Research requests and unrelated rate-changing mutations remain behind the recovery barrier.

## 4. Original and grouped residual transitions

When C=A, calculate the original using the canonical state after draining and immutable original eligibility D0/G. Publish its capacity-clamped award, applicable monster recovery/debit and phase ResidualPending in one complete candidate. E remains unpaid. Capture/fold any additional recovery time through that candidate's W.

If the original transaction rolls back, phase remains OriginalPending and it may be retried. If its candidate is authoritative after a crash, phase ResidualPending prevents a second original award or recovery debit. Zero-duration or zero-award cases still need a durable phase transition; they must not return early and leave the episode ambiguous.

While ResidualPending, rate-affecting gameplay is blocked. The residual uses the existing canonical rate/efficiency/capacity from that phase's boundary state. Metadata publication and lifecycle observation cannot change those inputs. Reopening must validate the required supported content/configuration context. A changed context must be proved calculation-equivalent or fail closed; silently applying a new rate to previously accumulated duration is outside this contract.

Duration accumulated during accepted-work draining is not a series of priced awards. Under this proposed grouped boundary-state calculation, it is evaluated with the post-original residual boundary state, along with the other residual duration. Any Heat/Research changes produced by accepted draining occur before that state is selected. No intermediate rate, wallet or monster recovery is applied to the duration accumulator. This preserves the required drain-before-state-dependent-grant order; it does not claim equivalence to a time-varying offline simulation.

For eligible segments with exact integer durations d1…dn:

    E = d1 + … + dn
    seconds = E / UnitsPerSecond + (E % UnitsPerSecond) / (double)UnitsPerSecond
    preCapacityAward = existingEffectiveOfflineRate * seconds / SecondsPerHour
    walletAfterGrant = min(existingCapacity, canonicalWallet + preCapacityAward)

Apply the existing formula in its established operation order, once. Then apply applicable canonical monster recovery once using that resulting wallet, current identities and occupied-floor restrictions. Do not round/cap/grant/heal each segment separately. Original and residual remain two distinct ordered transitions, as D4/D5 require.

This is exact **grouped duration accounting**, not a claim that floating-point mana is arbitrary-precision or that many independently capped/healing grants equal one grant. Summation is safe here because rate inputs remain fixed throughout the residual phase and no intermediate residual award is spendable. A future design allowing rate-changing actions between segments would invalidate this proof and need a different reviewed contract.

The H1-F3 helper accepts real endpoints and cannot directly express a noncontiguous eligible union. A later implementation needs a narrowly reviewed eligible-duration input to the same calculation formula. Do not synthesize a fictitious endpoint R+E, interpolate a ForegroundObservation or subtract capped awards to approximate the result.

A residual candidate atomically:

- folds all eligible duration through its captured S;
- applies the one residual award and applicable monster recovery;
- publishes W=S;
- removes the pending episode and its unpaid E.

Before success none of this is spendable or confirmed. Successful zero-award/capacity-full closure is still a real phase/coverage publication.

## 5. Publication, failure and retry semantics

### First attempt without a prior failure latch

Capture S with the actual wall/monotonic observation and existing predecessor binding. The candidate contains recovery obligations only through S. Until its result is known, the one in-flight suffix is **conditional observation evidence**, not durably accepted foreground and not a granted residual.

- **Success:** S is the logical handoff. Eligible monotonic S→Q time becomes the existing TimeService's provisional ordinary foreground observation. Process/accept it only after the residual candidate is proven authoritative. Never derive it from Q−S UTC.
- **Proven rollback at F:** discard the attempted S as an authoritative cut. Retain the same episode and all unpaid eligible recovery duration through F, including the successful-recovery-path work attempted during [S,F). It was never accepted as ordinary foreground. Start the known foreground stall prospectively at F.
- **Uncertain result at U:** retain both exact predecessor/candidate identities and the bounded conditional suffix. Stop new progression at U. Issue no grant, ordinary action or retry until independent recovery selects authority.

Changing an unresolved suffix from conditional to one of these outcomes does not reclassify accepted foreground: no such acceptance was permitted while the candidate was unresolved. Existing scheduler A and its entire pending prefix remain unchanged by that choice.

S is immutable inside one candidate. A subsequent candidate may use a fresh S only after the prior attempt is proven rolled back. Never mutate bytes after staging or silently replace an in-flight candidate.

### Explicit retry after proven failure

Keep the failure latch active through the retry's preparation, write, validation and readback. Foreground retry/wait time remains excluded; a retry click does not retrospectively turn the stall into eligible recovery or ordinary foreground. Observed genuine background during the wait remains eligible and joins the unpaid duration.

Each retry takes a new complete cut S2 and prepares from the same proven predecessor plus retained classification totals. It grants the entire still-unpaid residual once, not the difference between a failed proposed award and a new award. Any prior successful metadata/drain publication is already in W/E and is not counted again.

On repeated failure, stay blocked and preserve totals; do not create one durable interval per attempt or retry automatically every frame. On success at Q2, the stall latch clears prospectively at Q2. In this case [S2,Q2) was known stalled foreground and supplies **zero** new foreground credit. Begin ordinary sampling at the observed release cut. Q2 still does not update W by itself; the retained excluded suffix is accounted by the next supported complete cut.

If the successful retry was an entry/drain/original transaction, continue its still-required recovery phase after Q2. If it was residual closure, release ordinary progression subject to lifecycle and pending-work barriers.

### Resolving uncertain authority in the same process

Do not discard still-retained observations by pretending this is a fresh unknown crash:

- Proven candidate: apply its phase exactly once. For residual closure, retain eligible first-attempt monotonic time from S to U as provisional foreground; time after U follows the blocking/background classifications actually observed.
- Proven predecessor: discard attempted S; fold pre-U recovery-eligible duration and subsequently observed background into the unpaid episode; exclude the known foreground stall after U.
- Neither uniquely proven: remain blocked, preserve evidence and use the existing recovery-required result. Never choose the larger wallet or newer-looking timestamp.

At most one conditional suffix exists because no second writer/attempt may run while authority is uncertain.

Reconciliation is relative to the selected version's W/E: if the candidate wins, its prefix through S is already included and only the retained suffix is folded; if the predecessor wins, retain the unpublished prefix plus suffix. For an entry/drain/original candidate, eligible post-cut time remains recovery duration because no ordinary handoff occurred. This prevents double addition of a candidate's already-persisted duration during same-process recovery.

### Handing ordinary foreground time back to the existing owner

A successful first-attempt tail must enter TimeService's existing sampled horizon once, with its real monotonic cut. Do not create a second elapsed counter in the episode or accept a UTC-derived horizon. A fresh full ForegroundObservation binds the proven successor session, unchanged pre-existing A/C and the complete observed logical horizon; accepted publication then uses the existing scheduler path.

During known continuous foreground, block the offline resolver even though durable W is still S. A later supported foreground publication accounts for the paired wall cut and accepted logical horizon together. Ordinary Save/Research cannot independently sample UTC past that unprocessed cut.

If a pause is observed before that tail is accepted, retain the complete pause observation and close/accept its foreground prefix before settling the subsequent background interval. If persistence fails, keep that cut and the failure latch; observed background may be aggregated while ordinary foreground remains blocked. Do not resume new foreground segments ahead of these obligations. This preserves chronology with one pending foreground prefix, rather than needing a queue of alternating foreground and background histories.

Background observed after a successfully closed S belongs to a later absence/recovery obligation; it does not reopen the old episode or create a second residual grant for it. Required foreground-prefix and subsequent absence barriers still precede ordinary actions.

## 6. Pause/focus loss and the observation limit — L1

The current TimeService samples the old eligibility state before SetForegroundFocus/OnPause changes it. ForegroundObservation captures a real full observation; it does not supply physical lifecycle timestamps or an interpolation API. Synchronous publication can delay Unity callbacks.

**Owner-approved L1: observed lifecycle changes define the operational eligibility cut.** Preserve that existing sample-then-transition convention:

1. Before publication, capture current observed eligibility and a complete paired cut.
2. During a synchronous call, do not invent observations or assume the coordinator received callbacks it could not execute.
3. On return, reconcile available current lifecycle state and delivered notifications before permitting ordinary actions. Apply each effective state change at its actual observation/delivery cut, never at an estimated earlier physical timestamp. Redundant observations do not add elapsed time twice.
4. A delivered loss notification at D stops eligibility at observed D. A current-state check detecting a missed notification uses the check's own cut D. It cannot establish the earlier physical loss time.
5. A background-and-return cycle entirely hidden inside a synchronous call is not reconstructible from equal endpoint states. Under L1, the prior observed state governs that unsampled span. This is an explicit observation-based eligibility rule, **not evidence that the app physically stayed foregrounded**.
6. Apply the orthogonal failure latch: if the held class was known blocked foreground, its unsampled span remains excluded under L1, even if the retry eventually succeeds. Any detected background transition applies prospectively at its observation cut.
7. Invalid clocks, contradictory canonical evidence or an observation that cannot be safely ordered are D6 failures; do not infer fractional credit or move W. Do not alter accepted A to repair them.

For a success first observed at Q followed by a delayed pause callback at D, L1 permits the observed eligible monotonic prefix through D, then begins background at D. The candidate remains closed at S. Callback delivery does not rewrite that candidate or retroactively shorten already accepted foreground work.

**Approval recorded:** the owner explicitly accepts observation-time transitions, the unreconstructible hidden background/return cycle, provisional at-most-once S→Q foreground admission, preservation of known stalls and fail-closed invalid/contradictory evidence. No exact physical lifecycle tracking is claimed. This resolves L1; it does not authorize runtime or the proposed encoding.

Thus the storage/retry state machine is specified, but this document does not falsely declare physical lifecycle attribution solved.

## 7. Crash, reopen and durable knowledge

A live error does not erase knowledge. A real crash may erase process-local observations. Distinguish these cases even if both later execute the same canonical loader.

- Persisted classifications through W survive because the unpaid E/phase and W are atomic. An excluded stall already covered by W cannot be credited again.
- On a real restart, the last persisted "stalled" class does not prove the process stayed alive or stalled after W. Do not extrapolate an open stall to N; the death time is unknown.
- Without surviving authoritative evidence after W, classify the valid [W,N) suffix as unknown crash absence under the approved policy. This may include time that the old process once knew was stalled but never persisted. Exact retention of that lost knowledge is impossible without an additional durable write. Do not claim otherwise.
- If observations still exist in the same process, retain them and use section 5; do not invoke the unknown-gap rule merely to escape a failed write.
- Capture N once for a reopening attempt, validate it against durable W, and keep it stable while establishing the new classified cut. For an existing episode, N extends residual accounting; it never changes B, R or original completion.
- Every successful classification/drain publication advances the sole W together with E and phase. Another crash starts only from that newer W; it cannot recreate prior intervals.
- Invalid/backward N, corrupt or unsupported episode/context, inconsistent C/A or ambiguous file authority blocks new credit and actions.

### Candidate committed, acknowledgement lost

If independent recovery proves the residual candidate authoritative, its W=S and absent episode prove closure. Original and residual are not run again. Lost Q and lost monotonic samples cannot be reconstructed from UTC.

A new process may handle the valid unknown [S,N) gap as a new episode's original absence. This is not replay of the old residual: that old residual ended at S. Unaccepted ordinary work after S may be lost under the approved crash policy. If a later foreground acceptance was durably published, its newer W and A are authoritative instead; drain that A>C first and begin unknown absence only after that W.

Likewise, if an original-grant candidate was committed, ResidualPending prevents original replay; if a drain candidate was committed, its newer C/checkpoints and E/W survive. Temporary files, a replacement call having returned, or an earlier in-memory success message alone do not select authority.

## 8. Transition and interruption tables

### Coordinator transitions

| Trigger / known state | Authoritative result and time treatment | Required next action |
| --- | --- | --- |
| Entry publication succeeds | Persist B/R/identity/OriginalPending/D0/E/W before draining. | Drain old A>C only. |
| Entry publication rolls back | No durable episode yet; retain same local R and totals; F starts stall. | Explicit retry; no drain/grant first. |
| Drain succeeds, C<A | Publish exact next closed prefix with same R and accumulated E/W. | Continue bounded existing drain. |
| Drain reaches C=A | OriginalPending remains; no new ordinary acceptance. | Original atomic transition. |
| Original succeeds | Wallet/recovery and ResidualPending together; E remains unpaid. | Prepare residual after observing/folding intervening time. |
| Residual first attempt succeeds | Closed at S; eligible conditional monotonic tail joins TimeService once after proof. | Existing foreground/absence barriers, then ordinary actions. |
| Proven rollback at F | Old phase and wallet; attempted S discarded; eligible pre-F time retained; foreground stall begins at F. | Explicit retry from proven predecessor. |
| Repeated retry fails | Same unpaid obligation, accumulated observed background, excluded foreground waits/retries. | Retain bounded totals; no automatic loop or grant. |
| Retry succeeds | Candidate determines phase; failure latch clears at Q; stalled S→Q gets no ordinary or offline credit. | Resume remaining recovery or reanchor ordinary clock. |
| Authority uncertain at U | Keep one candidate/predecessor pair and conditional suffix; prospective stop at U. | Independent recovery only; no second attempt. |
| Pause/focus loss observed | Fold old observed class to D, then background; keep failure latch separately. | L1 rules and pending-prefix barrier. |
| Resume while episode open/failed | Fold observed background; preserve R and phase. | Recovery/retry before ordinary progression. |
| Real restart with open episode | Recover exact phase/C/A/E/W; new N extends eligible unknown suffix only. | Preserve R, finish remaining stages. |
| Real restart with closed candidate | Old episode stays closed; unknown suffix begins at proven S or newer W. | New episode if needed; no old reward/grant replay. |

### Every existing atomic protocol boundary

For each row, "predecessor" and "candidate" mean complete, independently validated saves. A crash is not modeled as a caught exception: it may prevent the existing rollback handler from running.

| Crash/interruption point | Permitted recovery disposition | Episode/effect consequence |
| --- | --- | --- |
| Before candidate preparation / before entry persistence | Existing active predecessor, if independently proven. | No entry record means no durable R; no episode-dependent drain was permitted. |
| After rollback bytes are written/read back, before candidate staging | Existing authority proof; rollback filename alone is insufficient. | Predecessor phase; no proposed effect. |
| During/after durable candidate staging, before replacement | An uninstalled candidate is not a committed grant. Preserve evidence and prove predecessor. | Predecessor phase/E/W; staged S has no authority. |
| During atomic replacement / after replacement, before directory flush | Do not infer old/new authority from the interrupted call. Use existing independent recovery. | Apply only the uniquely proven complete version; otherwise block. |
| After directory flush, before exact active readback | Independently reopen/validate; missing acknowledgement is not proof of rollback. | Proven candidate phase controls, including original/residual completion. |
| After exact readback, during fresh semantic reopen | Byte equality alone is insufficient. | Prove supported complete candidate or predecessor; ambiguity blocks. |
| Semantic validation fails and restoration succeeds | Exact predecessor restoration must be proven. | No candidate grant/closure; live F starts prospective stall. |
| Restoration fails or cannot be proven | Recovery-required, not retryable success/failure guess. | No publication; retain evidence and block. |
| After successful readback/semantic reopening, before rollback cleanup | Proven candidate may already be authoritative despite leftover rollback evidence. | Do not replay its successful phase. |
| During cleanup / after cleanup, before caller acknowledgement Q | Independent candidate proof resolves lost acknowledgement. | Candidate effects/closure occur once; Q is not needed in the save. |
| After Q, before next foreground acceptance | Closed candidate at S survives; provisional monotonic tail may not. | Restart uses unknown [S,N), not fabricated adventurer time. |
| After a later foreground acceptance, before closing all its work | Newer W/A/C and checkpoints control. | Drain only C→A; unknown absence starts at newer W. |

These rules apply separately to entry, each drain, original, residual and supported metadata publications. Each candidate is complete; no recovery procedure may combine a wallet from one version with a phase/cut from another.

## 9. Boundedness and absence of competing authority

The required storage is constant in the number of failures, lifecycle transitions and crashes:

- One canonical episode record, with fixed-size identity/context references, immutable original cuts, one phase and a small fixed set of integer durations.
- One durable advancing wall endpoint W.
- Existing scheduler C/A, checkpoint owners and wallet.
- One process-local current observation/class/failure latch and accumulated uncommitted eligible/excluded totals.
- While a write is unresolved, one additional conditional suffix summary and the existing exact predecessor/candidate evidence.
- After successful handoff, at most one pending foreground prefix before later background recovery; barriers prevent repeated foreground/background alternation from building a queue.

Conceptually fold each observed segment once into E or exclusion, advance the transient last-observation cut, and forget its individual endpoints. On publication, fold the aggregate into the canonical E/W pair and discard the committed transient portion. Retain the single attempt-cut snapshot needed to distinguish candidate from predecessor on uncertain recovery. Repeated retry overwrites a resolved attempt snapshot; it does not append history.

Constant memory is not a guarantee of bounded elapsed time, backlog drain duration or OS latency. Checked representability failures block without truncation. Existing execution budgets remain; no new arbitrary limit, overload threshold or cancellation guarantee is introduced.

Aggregation remains valid only because no new ordinary simulation runs while the episode is pending, residual inputs stay fixed, and conditional foreground forms at most one prefix before blocking/background. If an implementation lets new foreground play resume between unresolved retries or lets rate-changing actions interleave, the fixed-summary proof fails; reject that implementation rather than add an interval journal.

E/D0 are not another wallet and are not directly spendable. They cannot schedule parties. B/R/G are immutable descriptors; process-local observation endpoints have no durable authority. The single writer changes W, phase, durations and gameplay owners atomically. No second mutable timestamp cursor, independent grant service, event journal or persistence framework is required.

## 10. Frozen schema 16, activation and every publication

### Frozen source and explicit activation

Vnext denotes the authorized schema 17 while the verified main remains schema 16. Schema 16 remains format 1, legacyWholeSeconds, fraction zero. No format-2 payload is legal in schema 16; schema number and owner format must agree. No writable downgrade or generic future-version fallthrough is supported. The successor has not been implemented during the dependency audit.

Before runtime implementation, the frozen-16 source must include its reader, contextual validator, limits, serializer expectations, executable/checkpoint dispatch and timestamp rules pinned at 3eb6ccf93558b8145d64e9e63cbefcbd73484602. Freeze its calls that currently depend on CurrentWritableTarget so they continue to select 16 explicitly. Preserve existing 7→15 and 15→16 transformation bytes, historical fixtures, compatible extensions and active v1/v2 acceptance/rejection. Route their validated output into a separate 16→Vnext activation; never modify an old transform to emit Vnext.

| Input | Exact activation/open mapping |
| --- | --- |
| Proven schema 16 on boot | Validate frozen 16 and all applicable historical state/time/content/Research/monster/checkpoint requirements. If A>C, require independent producer-bound proof of original execution inputs before R/draining; absent proof refuses activation unchanged. A=C requires the applicable validations, not an invented source identity. On successful activation capture one R, B=(lastSavedUtcUnix,0), G=(R.Seconds,0), prepare OriginalPending/unknownPostCrash/legacyWholeSeconds, D0=(G.Seconds-B.Seconds)*10000000 and E=R.Fraction100ns. Bind exact source bytes/current qualified target context and persist before drain/grant, retaining R through retries. |
| Older supported schema | Apply the unchanged existing supported chain through frozen 16, including historical rejection paths; retain exact actual predecessor bytes. The same historical source-context proof gate applies. No fake fixture timestamp, current-config stamp or detached intermediate supplies authority. |
| Vnext with open episode | Validate context/phase/W/interval bounds and canonical owners. Capture N; retain B/R/G/mode/context/D0. Add only eligible unrecorded suffix after W to E. Persist new coverage before discarding its local evidence; drain remaining C→A. |
| Vnext closed, actual restart | Capture R after proof; create precise OriginalPending with B=old W, D0=duration(B,R), E=0, W=R and unknownPostCrash at capture; fold later preparation time into E/W. |
| Vnext closed, observed background/resume | First resolve any retained foreground prefix through the existing acceptance/drain barrier. Capture R with knownBackground; D0 is the eligible wall duration through R from the single remaining classified suffix, excluding known foreground stalls. At capture initialize OriginalPending with E=0,W=R; fold later preparation time into E/W. No accepted foreground span is reclassified. |
| Fresh native save | Bind the exact validated native context and one precise creation observation establishing W, Episode=null, in the first complete save. There is no predecessor, invented prior absence or legacy carry. Do not invoke this path to replace an incompatible legacy save. |

First activation is a load-time transition from proven schema-16 bytes, not a live mid-session mode switch. This restriction avoids reconstructing a fractional cut inside discarded legacy lifecycle segments. No same-process rounding/interpolation mapper is authorized. A source without valid time, compatible checkpoint/content or unique authority fails closed.

On an entry-publication failure, keep the same R and add eligible time after it to E on retry; W may then exceed R. A crash before any entry commit may capture a new R because no episode-dependent drain/grant was allowed. Once entry commits, R survives every later crash. Atomic schema activation and entry persistence are one candidate, not a schema-only publication followed by a vulnerable entry write. For older inputs, the entry hash is the actual complete source predecessor, not a detached intermediate schema-16 serialization.

The legacy original invokes the existing whole-second calculation/operation order against the post-drain canonical state using D0/10000000. Its unchanged original award is followed by the newly activated precise residual. [B,G) and [G,R) are disjoint; the latter's observed fraction is in E exactly once. Later episodes use precise mode. No historical fraction of B is recovered. Existing legacy resolvers cannot run on Vnext, including during migration/boot.

### Shared single-boundary publication rule

All producers must enter the existing canonical session/writer and exact complete-save persistence. The proposed boundary transition is an internal checked input to PrepareReplacement, bound to exact predecessor bytes and a full captured observation; it is not a new persistence layer or a public timestamp setter.

Each candidate either (a) preserves W and the complete episode byte-for-byte or (b) advances W once with an authorized complete classification/foreground cut, the corresponding horizon acceptance or unpaid duration, and required effects. No recognized snapshot, SaveReason, wall sample at acknowledgement, monster pass, Research result, manual run or QA helper can independently change lastSavedUtcUnix/fraction. Supplied contradictory recognized timestamps are rejected. Runtime time provenance remains derived read-only state.

| Publication family / trigger | Required treatment |
| --- | --- |
| Ordinary Save, autosave, Boot, Quit, pause and focus loss | Resolve the coordinator barrier. Preserve W if merely saving metadata, or publish a full validated paired cut with accepted horizon/classification. Never call fresh UTC to consume elapsed time. Background begins at the validated lifecycle observation; saving background metadata does not settle it by timestamp alone. |
| Foreground acceptance | Publish full captured A and paired W together; no interpolation at partial C. Time after the captured cut stays in TimeService. Existing C≤A and deterministic order remain. |
| Existing accepted-prefix closure, settlement/release, manual-run/confirmed rewards | Preserve accepted A and exact already-closed prefix. Publish only authorized next state. With open episode, a separate full recovery observation may atomically add E/W; it does not derive W from the logical C. |
| Recovery entry / drain / classification-only save | Atomically retain R/phase and fold new eligible wall duration into E while advancing W; excluded time advances coverage without credit. A pure classification write has no mana/HP side effects. An ordinary metadata request while recovery is pending cannot bypass this barrier. |
| Original settlement | Require OriginalPending and C=A. One original award/cap and applicable monster recovery; atomically change phase to ResidualPending, retain E and immutable fields. Fold intervening eligible time to E at the candidate's complete cut. |
| Residual settlement | Require ResidualPending/C=A. Add eligible duration through fresh S to E, evaluate one grouped award/cap and applicable recovery; publish W=S and Episode=null in the same candidate. |
| Research Start/Claim, spatial/floor/content mutations, undo, draft actions, QA mana | Existing foreground action barrier resolves obligations first. Then retain W or use its already authorized complete foreground cut. Research acknowledgement/reward never precedes durable success. No ordinary rate-changing action is allowed inside an episode. |
| Migration/native creation | Only the exact mappings above may establish the target representation. Migration does not opportunistically update unrelated saved timestamps. |
| Semantic readback, runtime publication, acknowledgement Q | Validate/publish the candidate as written; never modify its time boundary or compensate with another grant. |

For ordinary successful saves, storage elapsed time remains eligible foreground monotonic time for the next full observation under D3. It cannot be swallowed by a newly sampled save timestamp. For residual success it is conditional: only independently proven closure permits the eligible S→Q prefix to enter TimeService, once, bound to the successful successor. If observation callbacks occur during publication, fold them in observation order; once background interrupts the conditional foreground prefix, block new foreground until the prefix is accepted/drained and subsequent absence resolved. A retry's failure latch excludes foreground storage time even when that retry succeeds.

An at-most-once local handoff token is the already retained exact candidate identity and attempt disposition: unresolved → admitted or discarded, never both and never admitted twice. It does not survive process death or become another writable clock. After success, a metadata/action save cannot cross the unaccepted prefix by resampling wall time; it must retain W or complete the acceptance barrier. A later foreground acceptance gives that logical time its existing durable A/checkpoint authority. A crash before acceptance falls under unknown-crash policy, not UTC-driven adventurer replay.

## 11. Deterministic example timelines

All numbers below are illustrative clock values/durations, not production tuning. Assume valid forward clocks, compatible content and a precise episode unless stated otherwise. Mana outputs are derived by the existing formula from the stated eligible durations.

### A. Successful first residual publication

B=80, R=100. Drain and original finish before S=110. The original uses 20 eligible seconds; residual E=10. Candidate closes at W=110. Q is observed at wall 115, while the monotonic source advanced exactly 4.5 eligible seconds from S.

Result: one original, one residual; W remains 110 at Q. TimeService receives 4.5 seconds in existing logical units, not the wall difference of 5 seconds. No offline resolver grants [110,115) in this surviving process. The next full foreground cut publishes its accepted horizon/paired coverage.

### B. Rollback, genuine background and repeated retry

R=100; original already succeeded. First residual candidate S1=110 rolls back at F=112. E through F is 12 seconds; S1 is discarded as authority.

Observed classes then are:

| Span | Classification | Added residual duration |
| --- | --- | ---: |
| [112,120) | Known foreground stall | 0 |
| [120,130) | Observed background | 10 |
| [130,140) | Foreground still failure-latched | 0 |

An explicit retry captures S2=140 with E=22. It fails at 142: retry time is still stalled, E stays 22. Another retry captures S3=150 with E=22 and succeeds at Q3=153.

Result: exactly one successful residual for 22 seconds, closed at W=150. [150,153) remains excluded foreground stall; ordinary sampling starts at the observed release at 153. No grant is issued for either failed candidate. A later supported full cut accounts for the retained excluded suffix without credit.

### C. Delayed or missing pause notification

S=200; publication returns successfully at Q=210. Physical focus may have been lost at 204, but the only delivered observation is unfocused at D=210.

Under approved L1, eligibility changes at observed 210; the eligible monotonic prefix is handled once as foreground, and background starts at 210. The contract does not claim knowledge of 204. A pause/return entirely hidden between 200 and 210 yields the same observable inputs as continuous foreground; under L1 it therefore yields the same result.

If exact physical attribution is required, this example is not solvable with the current interface. The owner accepts this observation limit; code must not invent the missing timestamp.

### D. Second crash during accepted-work draining

Entry publishes B=80, R=100, W=100, OriginalPending, E=0 with scheduler C=20,A=50.

A drain candidate closes through C=30, retains A=50/R=100, and publishes W=106,E=6. The process crashes. Reopen at N=120 proves that candidate, adds unknown [106,120): E becomes 20, original R stays 100.

A second successful drain publishes C=40,W=123,E=23, then crashes again. Reopen at N2=130 adds only [123,130): E becomes 30. The original interval remains [80,100); accepted work still ends at A=50. No previous closed prefix or original grant is repeated. A known excluded span already folded into either saved W would reduce E accordingly and stay excluded after both crashes.

### E. Residual committed, acknowledgement lost

Original is complete; residual candidate closes at S=110. Replacement/flush/readback complete, then the process dies before Q. Reopen at N=140 independently proves the closed candidate.

Result: original and residual remain complete. No episode exists for them. The valid unknown [110,140) gap can enter a new episode under the crash policy; no adventurer elapsed time is reconstructed. If a later foreground acceptance at W=125 had instead survived, unknown absence would begin at 125 and its A>C would drain first.

### F. Known stall evidence versus unknown suffix

A live process is stalled from observed F=112. It later successfully publishes a metadata/classification cut W=120 with no added eligible duration, retaining its unfinished phase and previous E. It then crashes; reopen N=130.

Result: [112,120) stays excluded. [120,130) is unknown because the physical crash time is not known. The saved stall classification cannot prove continued life through 130. If the same process had retained observations through 130 instead of crashing, that still-known stall would remain excluded.

### G. Mixed residual segments and a capacity boundary

Residual segments are eligible recovery [100,104.25), excluded stall [104.25,109), background [109,110.5), and eligible recovery [110.5,112).

E=4.25+1.5+1.5=7.25 seconds. Use one formula evaluation for 7.25 seconds, one capacity clamp, then one applicable recovery pass. Do not evaluate a 12-second gross interval or let the excluded 4.75 seconds generate mana. Do not grant each segment separately: intermediate healing could free capacity and produce a different result.

### H. Legacy fractional activation seam

Legacy B=10 seconds, R=12.25, G=12, S=13.75 with all time eligible.

Original duration remains the historical 2 whole seconds. Residual receives the observed 0.25-second carry plus 1.5 seconds after R, totaling 1.75. Total eligible duration is 3.75 seconds exactly once. Original whole-second results remain unchanged; the residual is the new activated behavior. No historical B fraction is invented.

## 12. Final review gate

L1 is approved. No further product-policy or gameplay-tuning decision is identified. The complete semantic state machine is implementable in constant space under that policy; this is a design conclusion, not executable qualification evidence.

The owner selects producer-bound evidence tied to the exact historical save and original immutable execution inputs. A frozen qualified-release mapping alone is insufficient. Positive tests must inject independently established producer evidence and exercise the real activation/drain path; unproven fixtures must fail closed unchanged. A=C historical activation remains allowed after all applicable validations. No automatic different-context rebinding is authorized for schema 17. No further policy decision is pending; implementation/qualification must prove the approved behavior rather than reopen it.

After the additional context correction clears, continue authorized implementation: freeze/qualify schema 16 and historical migrations, introduce the narrowly gated duration-based calculation path, prove all writers use the shared rule, execute the deterministic interruption/boundary matrix and the required historical/current/H1-F4 benchmarks. No schema resource, migration contract or runtime file has yet changed.

## 13. Verification, qualification and performance status

This revision receives documentation checks only: branch/base identity, changed-file scope, relative links, required case coverage and manual transition/conservation review. Example timelines are specified expected cases, not executed runtime tests.

Future deterministic Unit/SIT and fault qualification must cover all table rows, repeated crashes during draining, mixed durations/capacity/healing, zero grants, legacy carry, unchanged legacy arithmetic, Research acknowledgement, retained full foreground cuts, stale authority, changed context, malformed/unsupported phases, backward clocks and exact closed-prefix preservation. Lifecycle fixtures must explicitly distinguish supplied observation traces from unknowable physical events. PlayMode and isolated Windows ordinary/abrupt close/reopen remain required for later runtime changes under the approved supervisor.

No Unity, PlayMode, Windows build/scenario, benchmark or owner UAT ran for this correction. Runtime test counts are not claimed. The known intermittent Windows native shutdown failure remains unresolved and must not be counted as a passing exit.

Earlier investigation verified all eight retained performance-summary hashes in comparison.json. That historical finding remains source-specific. Fresh 3eb6ccf and final H1-F4 benchmarks are pending; no H1-F4 speedup/regression can be assessed. Repeated validation, reconstruction/prefix replay, admission assurance and semantic readback remain the measured historical costs; H2 retention/batching is the later intended optimization, with no promised gain.

## 14. Source anchors and preservation

The source facts used for this revision are:

- [F2 policy/provenance](h1-f2-time-provenance-paired-cuts.md) and [F3 inactive foundation](h1-f3-stable-recovery-precision.md).
- [TimeService foreground](../../Assets/_Project/Scripts/Services/TimeService.Foreground.cs), [lifecycle methods](../../Assets/_Project/Scripts/Services/TimeService.cs), [SimulationClock](../../Assets/_Project/Scripts/Services/SimulationClock.cs), [ForegroundObservation](../../Assets/_Project/Scripts/Services/ForegroundObservation.cs) and [RecoveryEntryObservation](../../Assets/_Project/Scripts/Services/RecoveryEntryObservation.cs).
- [GameRoot foreground](../../Assets/_Project/Scripts/Core/GameRoot.Foreground.cs), [GameRoot lifecycle](../../Assets/_Project/Scripts/Core/GameRoot.cs) and [SaveService](../../Assets/_Project/Scripts/Services/SaveService.cs).
- [TimeProvenance](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/TimeProvenance.cs), [canonical session](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs), [writer](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs) and [atomic persistence](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs).
- [Offline formula](../../Assets/_Project/Scripts/Economy/CanonicalOfflinePassiveManaService.cs), [monster recovery](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs) and [scheduler](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonScheduling.cs).

H1-F3 R remains process-local in current code; its durable retention above is proposed new behavior. Current schema-16 writers always emit legacy provenance. Current helper arithmetic does not authorize the noncontiguous grant. Current production boot/ordinary-save/lifecycle behavior is not silently declared compliant with this future contract.

| Artifact | Ownership/purpose | Retention |
| --- | --- | --- |
| Managed worktree and codex/h1-f4-recovery | This task; pinned investigation and revised proposal | Preserve |
| This Markdown file | This task; independent semantic review | Revised in place; preserve |
| Owner normal checkout, saves, builds and planning changes | Owner | No edit, launch or cleanup |
| Historical evidence and unresolved earlier campaigns | Earlier qualification | Preserve |
| New runtime/build/save/supervisor artifacts | None | None created |

The current approved correction/audit changes this Markdown file, the original blocker report, the new dependency-audit report and their diagnostic/preservation evidence. The audit report inventories every new artifact. No commit, PR, merge, runtime implementation or H1-F5 is initiated. Sequence remains H1 → H2 → O1 → comprehensive project reassessment.

**IMPLEMENTATION IN PROGRESS — AUTOMATED QUALIFICATION AND BENCHMARKS PENDING**

## Implementation checkpoint — 2026-10-27

The historical authority decision is resolved as stated above. Producer-bound A>C activation and no automatic context rebinding are implementation requirements, not pending policy choices. The exact consumed-byte bundle and checked canonical time transitions are being implemented. Schema 16 remains the active writer until schema-17 integration is complete. Earlier statements about documentation-only work describe their dated review snapshots.

Foundation qualification at `831b1acb159c11e885fe5039b5b26a6ea04c3b1d`: 30 passed, 0 failed, 0 skipped; supervised restoration and protected-evidence preservation passed. The first attempt had 29 passes and one test-assertion failure, retained and corrected without changing a production limit. See [source-pinned foundation receipt](../testing/evidence/h1-f4/foundation-20261027/qualification.json). This qualifies the foundation only. Boot/lifecycle integration, the full interruption matrix, PlayMode, Windows and all fresh performance measurements remain pending. No speedup, completed H1-F4 runtime or PR readiness is claimed.

The next 48-case attempt stopped at a corrected compiler import error and,
separately, unverified owned-process completion. Its campaign remains INCOMPLETE;
the [specific operational blocker and exact recovery proposal](../testing/h1-f4-qualification-process-blocker.md)
require the runbook's exceptional owner approval. No approved recovery semantic
policy is reopened. No test count or benchmark is claimed for that attempt.

The owner subsequently approved the exact exceptional recovery. At source
`ad161252b316ee7788dd022c9c6283e3e4145d09`, all guards passed, only the three
archived settings were restored, and the existing Lease authority released that
reservation. Separate verification proved the exact restored input hashes and
unchanged original campaign/protected evidence. The original verdict remains
INCOMPLETE / UNCERTAIN permanently. The fresh campaign
`479fe5daf3974a68838f0a834f1e152d` reached all 48 cases: 39 passed, 9 failed,
0 skipped, with verified owned-process completion. Those fixture failures are
being corrected without changing production limits or historical validation.
See the [recovery receipt and fresh result](../testing/h1-f4-recovery-execution-20261027.json).

The fixture correction passed all 48 cases at `e75bd6807a583e501561fbb8a237388da453d417`.
Subsequent source-pinned focused gates passed 56/56 at
`8d122017b22b5a50a45470c2e5e3fe62d2e16412` and 64/64 at
`9e1e36376e95c867c6ea118a255970c16752065f`, with zero failures/skips,
verified owned-process completion, exact input restoration and protected-evidence
preservation. The latter includes staged writer/coordinator integration and
ordinary rollback/deferred lifecycle clock tests. These are focused gates only;
schema 17 is still staged and full runtime, interruption, PlayMode, Windows and
performance qualification remain pending. The original INCOMPLETE evidence is
unchanged and is not relabeled by any later successful campaign.

Schema 17 was activated at `4b6a2ee0863b651e15aac81544c395a64e3da034`.
The corrected transaction gate passed 82/82 at
`38fd7320f4d31a0db5ff9f1f37035f4e13a8caa5`. The subsequent native/continuity
gate passed 98/98 at `982ff1dc7e2d36a5d2fadf22510e9093100e3da3`, zero
failures/skips, with verified process exit, restoration and preservation.
Failed predecessor attempts (55/27 and 97/1) are retained separately in the
source-pinned receipt; no failed result is relabeled. The original INCOMPLETE
campaign's 19 file hashes were independently rechecked unchanged afterward.

The implementation now checks consumed runtime configuration/loot/economy,
foreground policy/tick cadence and mana-service bindings, rather than treating a
digest string as sufficient authority. Native successor publication requires
fresh semantic readback; failed flush/acknowledgement retains uncertainty until
independent proof, including a fresh directory flush. Same-process activation
resolution retains the original R and supported classifications. After a
background interruption of S→Q, return-time recovery processing joins the same
bounded eligible sum; the conservation test retains all ten seconds as a four
second foreground prefix plus six eligible recovery seconds. Stalled foreground
remains excluded. No owner grammar, tuning, time policy or historical serializer
changed for these implementation corrections. Live root/accepted-work coverage,
full regressions, PlayMode, Windows and three-way benchmarks remain pending.

### 2026-10-28 implementation enforcement: retained DTO values

The persistent v3 digest still derives exclusively from the twelve exact
validated input byte arrays. Reference equality of a mutable parsed DTO is not
proof that it continues to represent those bytes. A two-case actual-writer
reproduction at `1a2fa9a8` demonstrated publication after same-reference config
or loot mutation under the unchanged digest. Current-schema validation and
recovery entry therefore check derived process-local value representations of
the consumed mutable DTOs, including CombatC0, bootstrap/Research, loot and
occupancy, before granting authority. No reload, repair, context adoption or
durable representation change is authorized. Unchanged values retain the same
authority; modified values fail closed. This implements the existing immutable
context requirement and introduces no new time or migration policy. See the
dependency audit for exact reproduction, coverage and pending cost assessment.
