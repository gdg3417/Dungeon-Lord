# H2-A bounded retained foreground execution

Initial review checkpoint; implementation and focused qualification in progress.
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

`PresentationSteps` grows only with run ordinals. `Combat.Events`, kernel participant/encounter collections, traversal histories and iterator lookahead survive with the execution. Combat configuration additionally bounds events, rounds, encounters and participants; admission assurance remains mandatory. These are finite execution-envelope bounds, not a measured heap-byte budget. No new numeric limit is activated. Representative retained-memory measurements and boundary tests remain required before final qualification.

## Scope and gates

Schema 17, historical formats, tuning, localization and flags remain unchanged. Unit/SIT apply to reuse, byte equality, invalidation, settlement and failures. Focused compilation/tests precede draft PR creation. Integrated review, full EditMode/PlayMode, Windows lifecycle, matched performance and any risk-based owner smoke remain later gates. No UAT may begin before independent authorization.

H2-B cadence, coalescing, provisional rollback, UI and offline adventurer processing are deferred. Existing complete-save stages, write frequency, exact readback, time classification and accepted-work recovery remain intact.

## Artifact ownership

| Exact path | Owner / purpose | Retention |
| --- | --- | --- |
| `C:\Users\gdg34\.codex\worktrees\h2-a-retained-foreground\Dungeon-Lord` | H2-A / managed implementation checkout | Preserve through review |
| `C:\Dev\Dungeon-Lord` | Owner / normal checkout and historical evidence | Preserve unchanged |

No qualification artifacts, saves or processes have yet been created by H2-A. Prior campaigns and owner saves are outside cleanup scope.
