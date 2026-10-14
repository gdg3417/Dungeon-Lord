# PR #236 interaction-feedback correction

Reviewed parent: `ff9f0a7ed9b0cc3efea793bab53e2615df075f40`. Original evidence remains in `../phase7-visual-run-replay/` unchanged. This correction adds no phase, simulator, save owner or schema change.

## Historical identity

`RunTransientEvidence.TryCorridorFingerprint` uses the existing canonical `PhaseFiveSaveContracts.Write` and `SpatialContractSha256` implementation. The identity covers all corridor assignments on the completed run's **traversed branches**: floor, branch, edge, assignment ID, category, option, sequence and tile. Collection insertion order is canonicalized. Wallet, Heat, learned knowledge and gameplay tuning are excluded; unvisited branches are outside this observer's scope.

The manual transaction captures the identity from its validated detached `CorridorContentAuthority`. An assembly-internal, explicitly nonserialized `PresentationCorridorFingerprint` accompanies the existing step/layout observer fields and is retained only through successful same-session publication. Replay creation checks both layout and corridor identities. Publications also validate an already open replay against its own branch scope, independently of a newer latest run. Changed/removed/replaced relevant assignments fail with the historical-layout explanation. No fingerprint is written to schema 14 or a journal.

## Recorded presentation

- `RunSpatialEvent.Path`, `ConfiguredStart` and engagement position provide monster approach geometry. Every point passes through `RoomLocalCoordinateTransform`; endpoints, floor identity and adjacent recorded edges are checked. No path search or unrecorded movement is generated. A one-point path has no invented approach.
- `RunStep` ordinal order still controls sequential encounters. Approach takes presentation-configured seconds per recorded edge, followed by a configured feedback interval. Damage/survivor display waits for approach completion. Pause freezes this elapsed presentation time.
- The same canonical monster renderer moves along the recorded approach. Afterward it stays at its recorded endpoint, tinted blue with a historical-state badge. It does not disappear and respawn at its configured start. Close restores original position, color and sorting order. New runs/restarts create a fresh presentation sequence.
- Untouched room content keeps its ordinary art. Triggered traps retain orange historical feedback; reached loot retains green feedback. Unreachable interactions retain a red invalid marker at their recorded configured position. Traversed-branch snapshots also supply bounded static corridor content that was not reached; those nodes stay untouched and receive no damage/reach event.
- A single reusable UI Toolkit label beside the party shows exact recorded member ordinal, damage and `HealthAfter`. Zero damage is distinct, and is called mitigated only when positive unmitigated damage is recorded. Untriggered/unreachable nodes have no damage label. The active trap is highlighted at its actual recorded tile.
- Positive `HealthBefore` followed by zero `HealthAfter` enables a small red diamond at the recorded aggregate encounter position. It persists until replay completion or Close. It represents a proven adventurer casualty, without inventing individual travel or a monster corpse. The damage label identifies the member.
- Localized descriptions name the content, explain the recorded room-route/loot ordering, and expose the existing recorded optional-branch decision reason. They do not infer new personality, aggro or tactical decisions.

## Lifecycle and bounds

Only the selected floor owns visible historical content. Offscreen floors have no graphical animation objects; the global presentation cursor can continue. Camera/floor changes cannot modify run authority. Edit Mode restores ordinary visuals and suspends playback; returning preserves the paused cursor. A committed incompatible edit closes replay.

The policy owns approach/damage durations, sizes, text bounds, colors and marker limits. The sequence is limited to 8,192 steps, a total of 8,192 recorded monster-path points, and 256 interaction/content markers by the default typed policy. Sprite/content/state/casualty pools grow only within those bounds and are reused across floors/replays. Binding/projection allocates at lifecycle boundaries; frame updates do not construct paths or collections. One damage label prevents overlapping damage bursts. Descriptions scroll within a bounded area so controls remain reachable, including Large text and portrait tests.

The gameplay integration is limited to capturing and retaining the observer fingerprint. No encounter resolver, targeting, damage, pathfinding, combat order, reward, mana, Heat, run-history settlement or migration calculation changes. Save schema stays 14; original frozen fixtures/projectors stay unchanged.

## Limits and deferrals

The simulator supplies no monster HP, monster damage received or monster death. Therefore monster deaths/corpses, damage-to-monster indicators and tactical battle outcomes remain unsupported and are not displayed. There is no simultaneous attack presentation or individual adventurer motion. Several resolved encounters/casualties can share an aggregate tile; historical symbols can overlap there. Text and the sequential current highlight identify the active event.

Detailed replay remains latest-manual-run, session-local evidence. Restart loses detail and shows the unavailable explanation. Whole-dungeon spatial compatibility remains deliberately conservative. No old run is resimulated. Desktop screenshots and bounded-work checks do not establish phone-hardware performance or allocation-free whole frames. Casualty/mitigation screenshots use explicitly labeled engine fixtures with valid test config; they are not owner gameplay acceptance or changed production outcomes.
