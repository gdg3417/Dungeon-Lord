**System Spec 28: Save Data Model, Versioning, and Migration**

**H1-F2 candidate amendment (2026-10-25; independent review required):** Proposed schema 16 appends `primary.timeProvenance` after nine owners: format 1, `legacyWholeSeconds`, integer `AccountedFraction100ns:0`. `lastSavedUtcUnix` is the sole seconds component. Contextual frozen-15→16 migration preserves values/extensions, refuses collisions and missing/invalid boundaries, and uses exact rollback/readback authority. Historical 7→15 transforms and pinned 14→15 bytes remain frozen. The [contract](planning/h1-f2-time-provenance-paired-cuts.md) records the eight approved directions, writer synchronization, activation fence and remaining dependencies. Schema/field choices require this PR's review. Fractional settlement and full recovery are deferred.

*Dungeon Builder, locked design specification*

**Phase 6 final implementation status (2026-10-01):** PR #220 is merged at `e9f93b8d742ccaba38c7de32b776970006791d93`; Phase 6 is complete and schema 12 is the writable baseline. `sharedFloorKnowledge` is durable, lifecycle activation alone does not invalidate it, and material gameplay-affecting floor changes make it inapplicable until fresh qualifying evidence replaces or reconfirms it. Coarse reporting is durable; detailed transition causes remain same-session/transient.

**A5A schema contract:** Schema 11 remains a frozen readable source. The 11-to-12 upgrade accepts only strictly valid schema 11 bytes, copies every existing root and primary member in order, changes the version token, and appends exactly one empty `sharedFloorKnowledge` tail owner after `sharedBranchKnowledge`. Schemas 7 through 10 continue through their existing steps and then through 11-to-12. Native schema 12 creation also writes one empty floor-knowledge owner. Floor records are sorted by stable floor ID and carry an applicability fingerprint, explicit reward/danger known flags and observed scores, shared confidence, and a last-confirmed run ID. Invalid or duplicate records fail the current-target reader; stale but structurally valid fingerprints remain stored and inapplicable until new survivor evidence replaces them. Run settlement writes this owner with branch knowledge, Heat, history, and sequence in one exact complete-save transaction.

**Historical Phase 6A3 implementation status (2026-09-29):** Schema 11 remains the writable target and no migration or serialized eligibility cache is added. Floor 2 activation eligibility is derived read-only from the existing canonical state, production/configuration, duplicate-safe `ac_100` completion, production semantics, and `ActivationValid` layout validation. Resolution exposes stable reasons and target identity but writes no activation, save, runtime, or persistent state. Activation mutation and multi-floor runs remain deferred, and the current run projection still rejects a second Active floor.

**Phase 6A2 merged status (2026-09-29):** PR #216 merged at `45ac7da34bb4527b9b45fcaeb9ec5ec01d816e7f`; external review, automated qualification, Editor qualification, and Windows standalone qualification passed. Floor 2 construction and Inactive editing remain as documented in the [Phase 6A2 evidence](testing/evidence/phase6a2-floor2-construction-inactive-editing/implementation-evidence.md).

**Historical Phase 6A1 implementation status (2026-09-28):** Schema 11 is the current writable target. Each persisted floor owns one explicit `ActivationState` (Active or Inactive), the sole writable activation authority. Frozen schemas 7–10 retain their historical shapes; the sequential migration chain maps every schema 10 floor to Active, preserves existing state and extension members, and creates no floors. Validation requires Floor 1 active and deeper active floors to form a contiguous prefix. Online and offline passive mana count only validated Active floors. Production still contains only Floor 1; Floor 2 construction, lifecycle actions, and multi-floor runs remain deferred.

Canonical migration and rewrites use detached validation, atomic persistence, exact durable readback, reopen validation, then live publication. Failure publishes no runtime state; stale-session and qualified Windows persistence boundaries remain in force.

| Status | Locked |
|----|----|
| Scope | Primary dungeon, season dungeon, sub dungeons, account layer |
| Primary goal | Prevent data loss and integrity issues while supporting offline play |
| Non goals | Implement full backend architecture details beyond interfaces |

# 1. Purpose

Define the save contract, authoritative sources of truth, versioning rules, migration behavior, and anti abuse safeguards. This spec ensures that balance updates apply to existing saves, offline play remains viable, and competitive features remain trustworthy.

# 2. Design goals

- Players can play as a guest, but progress can be lost until an account is linked.

- Separate saves exist for primary dungeon, season dungeon, and each sub dungeon.

- Saves store stable IDs and player progress, not tuned numeric balance values.

- Server authority is used for time sensitive and competitive systems.

- Rollback and clock manipulation attempts degrade privileges rather than corrupt data.

- Migration is safe, deterministic, and does not require manual player repair for common cases.

# 3. Save partitions

Each account has separate save partitions:

- Primary dungeon save

- Season dungeon save (only one active season dungeon at a time)

- Sub dungeon saves (one per sub dungeon instance)

- Account layer (entitlements, premium currency, settings)

# 4. Sources of truth

Authority model by category:

- Primary dungeon state: server authoritative whenever online, client cached for offline play. Reconnect uses server as the merge authority.

- Season dungeon state: server authoritative always.

- Premium currency: server authoritative only.

- Research timers: server stores start time, duration, and completion status. Client may display progress offline, completion finalizes only after server confirmation.

- Leaderboards placement: server authoritative only.

# 5. Save cadence

Save writes occur at fixed intervals plus key actions.

## 5.1 Interval saves

- Interval: every 30 seconds during active play.

- Also save on app backgrounding and app termination callbacks when available.

## 5.2 Key actions that always save

- Monster placement or monster level up

- Loot table change

- Starting research

- Claiming research completion

- Floor unlock

- Any premium currency spend

During the Phase 7 production editor, each accepted discrete draft mutation queues ordered persistence to separate non-authoritative draft storage, as required by INV-12. Pointer/drag frames are not individual writes. Presentation may display a pending candidate; the live acknowledged prefix advances only on reported persistence success. An uncertain result preserves evidence, does not advance live acknowledgement, and blocks Save Changes without canonical or resource mutation. Restart recovery independently selects a predecessor-complete recoverably committed prefix using explicit commit evidence bound to the candidate, baseline and predecessor chain. It may resolve a previously unknown result as committed; candidate bytes alone or historical callback inference are insufficient. Missing commit evidence selects the proven predecessor; contradictory or insufficient evidence fails closed. An unacknowledged suffix may be lost, but is not required to be destroyed if its commit is proven. Save Changes requires the included final draft to be live-acknowledged or recovered as committed before final revalidation and one separate atomic canonical commit; interval saves and other key actions remain additional canonical safety boundaries.

Before authoritative room-local positions become writable, schema-12 non-positional room assignments require versioned/frozen authored migration-placement profiles for the specific source-schema boundary. The profiles provide deterministic valid slots by applicable production room definition/orientation and canonical assignment ordering, preserve existing assignment/room identity, category, option, Sequence, custody, investment, and layout, and consume no runtime randomness or unrelated inputs. Profile coordinates/occupancy are migration compatibility content/config data, not runtime constants. Missing, malformed, insufficient, overlapping, or incompatible data fails closed before publication, leaves original bytes untouched, and permits no deletion, custody return, partial result, or fallback position. The future packet must review/freeze the profiles and qualify migration, reopen, idempotence, canonical ordering, and every maximum-valid current production assignment envelope; no next schema number is declared here.

## 5.3 Player feedback

Autosave status is invisible in MVP. Errors use clear messaging only when required.

# 6. Cloud saves and conflicts

- One save per account in early releases.

- Most recent timestamp wins when selecting between competing versions.

- If two saves are within 5 minutes, show a conflict prompt that explains the resolution logic.

- Multi device protection: if a device reconnects after another device has played online, the reconnecting device must download the server version. There is no user choice.

# 7. Guest to account linking

- Guest saves remain local only until the player links an account.

- Linking flow warns about the risk of progress loss if the device is lost or reinstalled.

- Once linked, cloud saves and telemetry identity become active.

# 8. Versioning

- Each save partition stores: save_version, content_version, and last_write_timestamp.

- Content IDs are never renamed. Deprecated IDs remain resolvable via a mapping table.

- If an ID is removed, the system either replaces with a fallback equivalent or marks the entity as disabled requiring player action, depending on the content class.

# 9. Migration and missing content behavior

## 9.1 Deprecated ID mapping

- Deprecated IDs map to a replacement ID of the same class.

- Mapping is data driven and validated in the build linter.

- Mapping applies on load, before simulation begins.

## Owner-approved living-dungeon persistence addendum (2026-10-12)

See the [Living Dungeon Owner Design Lock](../docs/planning/decisions/living-dungeon-owner-design-lock.md). Future active parties retain immutable run-start inputs and compatible historical executable rule behavior; a version string/hash alone is insufficient to settle an old run.

Unfinished parties, automatic arrival/scheduling state and catch-up progress eventually use one versioned canonical save authority, preserving the existing atomic publication and proven-prefix recovery principles. Schema 14 is provisional only. No save fields, schema number, migration mapping, parallel journal or writable authority is selected by this addendum. A concrete schema and migration design must be reviewed before implementation. Failure recovers the last proven committed prefix without duplicate settlement or fabricated events. Draft commits must not retroactively alter earlier run snapshots.

## 9.2 Removed content handling

- Rooms or tiles: replace with a fallback tile or empty tile, then mark for player review if the replacement changes function.

- Monsters: replace with fallback within the same family tier when possible, otherwise disable the slot.

- Loot items: replace with nearest tier fallback, or mark the loot entry disabled and require player removal from the loot table.

- Research nodes: preserve progress if the node still exists, otherwise mark the branch as deprecated and map to a replacement node when available.

# 10. Integrity safeguards

## 10.1 Rollback detection

- Detect save rollback if save_version decreases, timestamps move backward, or the server reports a newer authoritative state.

- On detection: force cloud pull, disable offline grants, and lock research until online verification completes.

## 10.2 Admin and testing slots

- Multiple save slots are available for testing accounts.

- Admin role can create and switch slots via backend tooling.

- Production players use a single slot.

# 11. Telemetry hooks

- save_write (partition, reason, duration_ms, success)

- save_load (partition, save_version, content_version, success)

- save_conflict_detected (delta_seconds, resolution)

- rollback_detected (reason, action_taken)

- migration_applied (mapping_count, disabled_count)

# 12. MVP constraints

- No visible autosave indicator.

- Minimal conflict prompts, only when within the 5 minute window.

- Guest mode supported, but not guaranteed across reinstall.

- Admin save slot support limited to internal testers.

# 13. Open questions

None.

## Schema-15 frozen historical source contract (H1-F1, 2026-10-25)

Schema 15 remains the writable target. Its historical reader is now explicitly pinned to the schema-15 contract at `09a46810aeab7441a2ac09ac6220b0f4cc13d6dd` (PR #254), independently of `CurrentWritableTarget`. This prerequisite does not select or activate a later schema or implement hybrid time provenance.

`ParseValidateFrozenSchemaFifteenAndRoundTrip` preserves the exact canonical root/primary ordering, lifecycle and ownership, structural investment, corridor/branch/floor knowledge, strict `livingDungeon` owner, and `monsterContinuity` tail owner. Dormant owners and registered historical v1/v2 executable profiles remain supported, including accepted-but-unclosed horizons. Complete bytes, allocator, party sequence, start/checkpoint identity, ordinals and checkpoints remain unchanged. Compatible unknown root/primary extensions retain their original canonical bytes; reserved-member case collisions, malformed/reordered owners, unsupported formats/profiles/executables and unsupported schema versions fail closed. No time-provenance field is required for schema 15.

The structural overload proves the byte/structural contract only. The contextual overload additionally retains production configuration, occupancy, compatibility selection, monster-recovery consistency and historical executable/profile requirements. It never grants `CurrentTargetValidated`; writable-target validation remains a separate call. Recovery can recognize a contextually valid frozen-15 predecessor after a later target change. The load coordinator refuses to reinterpret that source through legacy projection; a reviewed later migration is still required.

`Resources/save_spatial_migration_limits_schema15.json` is an exact copy of the baseline production limits, chained above the existing frozen-14 and frozen-13 profiles. Both raw classification (including the schema-15 monster-record exception) and canonical/contextual validation select that historical envelope through schema 15 inclusively. Changes to target budgets cannot reinterpret it. Existing injected test limits remain supported. No new budget values are introduced.

The 14→15 transformation retains its bytes and rejection behavior, with only its output limits/validation bound to frozen 15. In particular, the baseline output check has no executable profiles and rejects active schema-14 owners even when source preparation receives profiles; H1-F1 preserves this behavior. Dormant empty and populated candidates are pinned byte-for-byte. Exact predecessor/candidate comparison, atomic replacement, readback and uncertainty handling retain their existing authorities.

See [qualification evidence](testing/evidence/h1-f1-frozen-schema15/implementation-evidence.md). Remaining H1 work includes the separately reviewed provenance representation/target, legacy activation mapping, ordinary-save/passive-time integration, and recovery/transition qualification. This prerequisite does not implement the H1 owner decisions, H2 retention/batching, or O1 simulation. No player-visible gameplay behavior or Windows filesystem primitive changes, so this change alone requires no manual gameplay UAT or additional native Windows durability qualification; automated migration qualification remains required before merge.
