# Phase 5A static qualification evidence

## Scope and baseline

- Packet: Phase 5A durable optional-branch state and research-gated construction.
- Baseline: merged PR #208, `ad026a29b1f8020a7ab8c682ac3c98da1ebf341c`.
- Working branch: `codex/phase5a-durable-optional-branch-state`.
- Phase 4 is complete through PR #207; the Phase 5 design lock is merged in PR #208.
- Manual UAT is outstanding and is not claimed by this evidence.
- Phase 5B route choice, traversal, encounter resolution, knowledge learning, production weights, and thresholds are not implemented.

## Static contract review

### Save and migration

- `SaveMigration.LatestSchemaVersion` and `CanonicalSaveSchemaVersions.CurrentWritableTarget` are 10.
- Schema 10 adds explicit top-level complete-save owners `corridorContent` and `sharedBranchKnowledge`; it does not append fields to the frozen canonical spatial serializer.
- The preserved chain is schemas 1–6 → 7 → 8 → 9 → 10. Schema 9 is parsed through its frozen exact owner shape and upgrades directly to 10.
- Frozen schemas 7, 8, and 9 use an explicit pre-Phase-5 node-kind domain and reject `DeadEnd = 6`; only schema 10 accepts the appended kind. A malformed frozen payload therefore cannot introduce Phase 5 topology through migration.
- The 9 → 10 preparer preserves the original primary/recognized/unknown state and canonical spatial owner, then appends empty Phase 5A owners. Current schema-10 input is rejected as a migration source.
- Native schema-10 creation initializes both new authorities empty.
- Both owners participate in recognized-member classification, complete-save parsing/writing, session replacement, current-target validation, canonical ordering, and save workload accounting.

### Spatial topology and identity

- `FloorRouteNodeKind.DeadEnd = 6`; existing enum values are unchanged.
- A branch is one optional physical `spatial.corridor.straight_stone` edge from a required-route node to one degree-one DeadEnd.
- The validator rejects optional direct doorways, room-backed DeadEnds, outgoing/multiple DeadEnd edges, required-route DeadEnds, unreachable sources, cross-floor references, missing/illegal branch IDs, allowance overflow, bad geometry, overlap, and out-of-bounds/capacity failures.
- Identities use the existing monotonic native edge allocator. For minted edge ID `E`, the exact branch identity is `E + ".branch"` and the DeadEnd node identity is `E + ".dead-end"`. Deleted identities are not returned to the allocator.
- Existing required-route identities and Completion are not replaced by branch construction/removal.

### Research and structural economy

- Basic Branching resolves only from completed research ID `ac_300` and the existing authored architecture effect `max_optional_branch_rooms_per_floor_set` with integer unit.
- Effective allowance is `min(authored effect contribution, FloorSpatialConfiguration.OptionalBranchAllowance)` after research completion; before completion it is zero.
- Missing, duplicate, malformed, non-integral, or ambiguous research/effect content fails closed. No writable duplicate unlock flag exists.
- The authoritative research export bundle is a single production asset tree under `Assets/_Project/Data/Production/Research/`; `GameRoot` receives the Architecture node/effect JSON through explicit serialized `TextAsset` references in the canonical Bootstrap scene. Runtime research resolution performs no `StreamingAssets` filesystem access, discovery, or duplicated-value fallback.
- Construction charges only the existing configured physical-corridor per-tile price and records edge investment. No branch surcharge exists.
- Removal uses the existing investment basis, refund percentage, floor rounding, and wallet capacity policy.
- Construction and removal previews expose the existing structural-economy authority's current balance, base/final cost, affordability, historical refund basis, nominal/credited refund, and resulting balance while retaining geometry and capacity consequences. Commit still recalculates from current authoritative state.

### Corridor content and custody

- Active corridor assignments preserve AssignmentId, CategoryId, OptionId, Sequence, FloorInstanceId, OptionalBranchId, EdgeId, and Tile.
- Fresh acquisition uses the existing acquisition price and sole mana wallet. Redeployment and direct unassignment cost zero mana.
- The existing returned-content owner is the only unassigned inventory authority.
- Sequence is minted from the owning floor's existing `RoomContents.NextSequence`. Assignment ID is `<edgeId>.content.<trap|loot>.<sequence D4>`.
- Validation enforces one identity across room assignments, corridor assignments, and returned custody; one assignment per corridor tile; authored trap/loot capacities; and terminal-tile loot placement.
- Branch removal fails while any corridor assignment remains.

### Shared branch knowledge

- Records are keyed and canonically ordered by stable floor/branch/edge identity.
- Explicit known-state booleans distinguish unknown from real zero perceptions.
- Confidence/incentive/danger values must be finite and within the normalized range when known; NaN and infinity are rejected.
- Applicability is resolved without mutation from a deterministic SHA-256 fingerprint of current stable branch topology and geometry. Content/topology changes can leave evidence stale and inapplicable.
- Explicit branch removal atomically deletes the matching live-branch knowledge record together with the branch topology; it does not retain a record for a branch that no longer exists.
- Phase 5A initializes/migrates the authority empty and does not learn from runs.

### Run behavior and development validation

- Required-route projection ignores optional edges and DeadEnds. Corridor trap/loot state is not supplied to the existing run simulation.
- The Bootstrap development surface exposes localized controls for research qualification, branch origin/socket/length preview and commit, corridor tile/content actions, and branch removal. These controls call the same atomic save/mutation authorities as runtime operations.
- All new visible strings are owned by the English string table; internal stable reason codes are localized before presentation.

## Known limitations

- Manual UAT remains outstanding.
- Phase 5B is intentionally absent.
- The Bootstrap controls are temporary development/QA controls, not the Phase 7 graphical editor.
