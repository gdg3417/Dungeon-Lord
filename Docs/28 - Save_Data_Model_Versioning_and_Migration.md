**System Spec 28: Save Data Model, Versioning, and Migration**

*Dungeon Builder, locked design specification*

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

Tile placement or movement does not trigger an immediate save. It is covered by interval saves and other key actions.

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
