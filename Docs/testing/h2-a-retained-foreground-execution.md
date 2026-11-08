# H2-A bounded retained foreground execution

Initial implementation checkpoint; focused qualification blocked before any selected test executed.
Base: `01ff418143c0400d8e6fafba7a260f399bcdb2aa` (merged PR #259).

## Inspection and implementation contract

- `SaveService.AdvanceForeground` owns the serialized foreground lane. The writer currently restores one private execution per sequence per pump and abandons all of them on return. Validation, occupancy, lineage and presentation also restore independent executions; those obligations remain.
- A pump clones runtime, validates the exact session and disk predecessor, stages complete timestamp operations, and calls `PrepareAndPersist`. Its fresh durable semantic readback returns a new session, validation and projection. Only then does SaveService publish runtime.
- Stages replace the private owner with parsed candidate state. Iterator locals remain independent of those parsed graphs. Pinning exports the execution into the party. `NextDueTime` prepares an announcement without committing an event; checkpoint export includes committed prefix and timing/control metadata.
- Terminal pinning precedes outcome consumption, reconciliation, authorized history composition and retirement. Vacancy/release permits belong to the exact transaction-local staged lineage and must never cross pumps. Waiting preserves source occupancy; Released and completed transfer share a timestamp.
- H1-F4 binds actual consumed configuration, combat, loot, occupancy and bootstrap values, not just references or digest labels. Retention may only run under that existing validated bundle and compatible production profile.
- Owner: a private SaveService working set, lent exclusively to one synchronous pump. Binding requires the exact immutable canonical session object, validation context, policy, profile, configuration, production content, loot and serialization limits. Checkpoints must byte-match independently validated parties before reuse and after confirmed durable publication. No validation is removed.
- Any failure abandons all speculative executions. An external canonical transaction, session replacement, load/recovery, deletion, disable or shutdown clears the binding. No stale binding authorizes a write. No new recovery protocol or retry is introduced.

## Existing resource envelope

Production v1/v2 allow one/two active parties, respectively, with four canonical retained records, 65,536 checkpoint bytes per party, 2,048 replay events per party, 8,192 total replay events, 1,000,000 combat work, 357,996 owner bytes and existing content/geometry bounds. Retention keeps only active, nonterminal executions; consumed outcomes are abandoned immediately after lineage verification. It does not retain an archive or per-pump log.

`PresentationSteps` grows only with run ordinals. `Combat.Events`, kernel participant/encounter collections, traversal histories and iterator lookahead survive with the execution. Production combat configuration additionally bounds 32,768 events, 32 rounds, 30 encounters and 64 participants; admission assurance remains mandatory. These are finite execution-envelope bounds, not a measured heap-byte budget. No new numeric limit is activated. Representative retained-memory measurements and boundary tests remain required before final qualification.

## Scope and gates

Schema 17, historical formats, tuning, localization and flags remain unchanged. Unit/SIT apply to reuse, byte equality, invalidation, settlement and failures. Focused compilation/tests precede draft PR creation. Integrated review, full EditMode/PlayMode, Windows lifecycle, matched performance and any risk-based owner smoke remain later gates. No UAT may begin before independent authorization.

H2-B cadence, coalescing, provisional rollback, UI and offline adventurer processing are deferred. Existing complete-save stages, write frequency, exact readback, time classification and accepted-work recovery remain intact.

## Artifact ownership

| Exact path | Owner / purpose | Retention |
| --- | --- | --- |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord` | H2-A / managed implementation checkout | Preserve through review |
| `C:\Dev\Dungeon-Lord` | Owner / normal checkout and historical evidence | Preserve unchanged |

Prior campaigns and owner saves are outside cleanup scope. Additional exact artifacts are inventoried below and in the preparation manifest.

## First attempted focused gate (2026-11-07)

Implementation branch: `codex/h2-a-retained-foreground-execution`; source `67f1d5b0df4e3d5cea1fcd56c3d9f60129bb31c3`. No draft PR exists: the requested prerequisite of basic focused validation has not passed.

Unity CLI was checked and updated from 1.0.0-beta.14 to beta.17 under the Unity CLI skill. Editor: 6000.3.2f1. The campaign-specific local adapter reuses the unchanged H1-F4 input inventory, measured settings preparation, protected-evidence checks, child runner, restoration and final preservation authorities, with the existing supervisor. It does not modify a frozen helper or introduce a qualification framework. Its sealed plan explicitly selected seven H2-A cases and no skips.

Campaign `ff0c1234cd734ea39b501f7c7332bee7` is **INCOMPLETE / UNCERTAIN**. Its preflight passed. Unity ignored the new Editor fixture because its authored `.meta` GUID has 33 characters. The resulting NUnit report executed **zero tests**. This is an implementation preparation error, not passing test evidence. The wrapper/CLI exited zero, but the supervisor separately reported active descendants after its five-second accounting drain. Restoration and final preservation stages did not run. Neither the result nor reservation has been overwritten or released.

Read-only investigation verified that recorded supervisor/host/Editor PIDs 52820, 22916, 55248 and 40828 are absent. A Unity SDK `adb.exe` process remains at PID 40928, creation FILETIME `134385682998835767`. Its ownership cannot be proven: the original named Job Object no longer opens. No termination is proposed or performed. No process-loss or successful process-completion qualification is claimed.

The exact recovery inspection independently checked **1,500 prepared input files**, **20 original campaign files**, preparation hashes and the existing protected owner/save/evidence inventory. Inputs still match the sealed prepared map. The reviewable recovery script proposes restoring only the three archived ProjectSettings files and releasing only this campaign's reservation through the existing Lease authority, while preserving the adb process and every original artifact. This exceptional release requires campaign-specific owner approval. Independent postflight verification is prepared; it has not executed.

The owner approved the exact recovery in chat. Execution and separate postflight checks passed: the three archived settings files were restored, the reservation was released through the existing Lease authority, all 20 campaign files and preparation/protected evidence remained unchanged, and adb was preserved. `recovery-execution.json` and `recovery-postflight.json` retain the receipts. The original verdict remains INCOMPLETE / UNCERTAIN.

The malformed fixture GUID is corrected. Reentrant invalidation now replaces an opaque token: a writer can rebind its lent executions only if that token still matches after durable publication. A new regression invalidates during candidate preparation and requires a successful commit with no retained execution, followed by fresh reconstruction. Eight focused cases await a new source-pinned campaign. The ordinary path, failure matrix and memory envelope remain subject to independent review; no functioning cross-pump capability has yet been demonstrated by an executed test.

Unit/SIT: seven focused cases authored, zero executed. No PlayMode, Windows standalone, owner UAT, matched before/after performance, retained-heap measurement, or FPS improvement is claimed. Reconstruction/replay reductions are unmeasured. The first draft checkpoint remains pending. Broader session/start/profile/ordinal negatives, canonical edit and service lifecycle coverage, full failures, partition equivalence, full suites and matched workload evidence remain later gates; this packet does not fulfill H2-A acceptance.

| Exact path / identity | Owner / purpose | Retention |
| --- | --- | --- |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\TestResults\h2-a-checkpoint1-20261107` | H2-A / adapter, exact originals and maps, recovery proposal and inspection | Preserve; recovery not executed |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\TestResults\qualification-supervisor\ff0c1234cd734ea39b501f7c7332bee7` | Supervisor / original incomplete campaign and raw evidence | Preserve unchanged permanently |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\TestResults\qualification-supervisor-reservation\active.json` | Supervisor / unresolved campaign reservation | Preserve pending specific approval |
| `C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord H2A Checkpoint1 20261107` | H2-A / isolated product and runner report; not the owner save | Preserve |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\Library` | H2-A / fresh Editor import cache | Preserve through recovery and retest |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\Temp` | H2-A / generated Editor transient state | Preserve; no cleanup authorized |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord\Logs` | H2-A / generated Editor logs, if present | Preserve |
| PID 40928; executable `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` | Ownership unproven / surviving Android debug bridge | Preserve; no termination |

The session member changed from a private field to a private invalidating property. Three existing reflection setup calls were adjusted in `ProductionDungeonForegroundSceneTests.cs` and `ReturnedContentRedeploymentTests.cs`; this is fixture adaptation to the same production setter, not a gameplay change. Save schema 17, codecs, persistence protocol, configuration values, localization and flags are unchanged.
