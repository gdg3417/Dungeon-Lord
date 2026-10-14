# Implementation review

Review covers the manual drain/publication integration, transient retention, projection/cursor, production controller/world partials, UI/localization/presentation policy, regression tests and qualification tooling.

- Manual runs still resolve through the existing simulator and complete-save transaction. Presentation data publishes only after persistence succeeds. Replay receives no simulator, wallet, Heat, history-writer or save-service reference.
- Assembly-internal observer properties have explicitly nonserialized backing fields. The original public structured gameplay contract, frozen Phase A projector/fixture, schema 14 and frozen migrations remain unchanged.
- Exact ordinals provide chronology, including connections and branch return paths. Spatial interaction records are indexed by stable identity; aggregate traversal storage order is not treated as time. Room positions use the canonical transform. Geometry compatibility is checked before constructing a display sequence.
- Only adjacent same-floor recorded movement interpolates. No inferred stair/corridor connection, unrecorded loot reach, monster death or party member position is introduced. Encounter casualties and final summaries remain authoritative.
- The display cursor, frame array and two retained sprite views are bounded. No per-frame path search, collection construction or new per-floor animation instance exists. UI strings update at event/control boundaries. Projection construction is linear in recorded steps plus indexed interaction records, with bounded canonical floor/room lookups.
- Edit Mode suspends/hides presentation. Floor/camera selection has no authority. A canonical spatial mismatch closes incompatible replay and presents the localized explanation. Ordinary editing and existing run-history inspection remain intact.
- Exact original failed reports are retained. The two-floor helper correction injects an existing required dependency; it changes no activation rule. The consumer allowlist includes the same approved controller's new read-only partial and retains its publication restrictions.
- Qualification uses exact disposable input inventories and distinct save namespaces. Owner data is copied to a new UAT filename without opening it. Existing owner saves, backups, previous players and prior evidence are not reset or migrated.

Known limits are documented in [implementation.md](implementation.md) and [owner-uat.md](owner-uat.md): latest manual outcome only, session-local detail, conservative whole-dungeon compatibility, an aggregate party symbol, lightweight encounters, existing manual camera controls, and no mobile hardware/performance or owner gameplay acceptance claim.

Independent code review and owner Editor/Windows UAT remain separate required judgments. This review does not authorize a merge or subsequent Phase 7/B3A work.
