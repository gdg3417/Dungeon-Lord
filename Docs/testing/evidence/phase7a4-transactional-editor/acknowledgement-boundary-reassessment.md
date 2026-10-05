# Phase 7A4 acknowledgement-boundary reassessment

Status: blocked. The original INV-12 regression still fails. No corrected production protocol has been implemented or qualified, and no PR has been created.

Historical status above describes the second stop. The owner subsequently approved distinguishing live acknowledgement from recoverable transactional commit evidence. See [transactional resolution and validation](transactional-resolution-and-validation.md) for that decision and the corrected protocol; the probes and earlier reasoning remain preserved below.

This companion preserves the original [failure evidence](blocked-persistence-evidence.md). Passing diagnostic probes below are evidence of an unresolved storage-contract limitation, not passing acceptance tests for draft recovery.

## Scope and baseline

- Working branch: `codex/phase-7a4-transactional-editor-production-dungeon`.
- HEAD: `64d31257f06d1dafbf49593ade88f32b808bd556`.
- Intentional uncommitted Phase 7A4 work was preserved. No branch switch, reset, clean, staging, commit, push, or wholesale replacement occurred.
- Recommended investigation configuration: GPT-6 Astra, Medium; classification: Ambiguous or High-Risk. Reason: unresolved durable acknowledgement and restart evidence under compound storage failures. User-directed availability fallback: GPT-6 Sol, High. The repository policy's generic Astra fallback is GPT-6.1 Sol, High; the explicit task-specific fallback takes precedence. No fallback or executing-model change is claimed.
- Canonical writable schema remains 13. No migration, package, tuning, localization, or runtime authority change was made during this reassessment.

## Authorities inspected

AGENTS.md; Docs/process/AI_Model_Selection_Policy.md; INV-12 in the cross-spec glossary; Spec 28 save cadence and atomic publication; Spec 38 save/draft boundary; A0 decisions 16-20 and acknowledgement/recovery clauses; the current draft domain/store/commit and Phase 7A4 tests; ExactCompleteSaveAtomicPersistence; DetachedSpatialMigrationTransaction and its deterministic failure injector; WindowsSpatialMigrationFileSystem; and the GD66 design's transaction and recovery rules.

GD66 is not a drop-in answer to this draft contract. Its design explicitly permits self-valid current canonical bytes to load without a live journal, and recovery can finish an interrupted migration. Its optional receipt is audit evidence. Those semantics cannot be silently imported into the user's stricter rule forbidding promotion of a command whose required acknowledgement failed.

## Root cause and current trust transition

The intended transition is:

`presented -> queued/Pending -> candidate -> successful required barrier -> acknowledged -> recoverable`

The implementation instead does this:

1. `Move` changes only the detached presented draft and queues command N.
2. `FlushNext` calls `Serialize(AcknowledgedSequence + 1)`. Those candidate bytes already contain `AcknowledgedSequence = N` before any storage acknowledgement.
3. `FileDungeonDraftStore.Write` invokes the canonical exact replacement primitive. It writes predecessor rollback bytes and candidate bytes, replaces the active draft, checks the directory boundary, and reads back.
4. If the boundary fails, it attempts rollback. A failed rollback can leave candidate N active.
5. `Write` returns false and the live object remains Failed with acknowledged sequence N-1.
6. `FileDungeonDraftStore.Read` reads only the active payload. `Recover` validates its syntax, version, baseline, sequence and commands, then adopts its embedded acknowledged sequence.

Step 6 collapses candidate validity into proof of historical acknowledgement. It does not consult transaction outcome evidence. The schema and command checks cannot prove that the original durability barrier succeeded.

## Why a receipt alone has not established a correction

`ISpatialMigrationFileSystem` does not promise that a throwing operation had no effect, nor does it expose a restart-queryable record of whether an earlier barrier returned successfully. This is not merely a hypothetical mock behavior:

- Windows `WriteAllBytesDurable` writes bytes before calling the fallible `FlushFileBuffers`.
- Windows `Move` calls `SetFileInformationByHandle` before a fallible flush and destination-identity checks.
- Windows `FlushDirectory` is a supported-volume/path revalidation boundary, not a POSIX directory fsync. A failed check does not erase preceding bytes.
- The existing GD66 test filesystem explicitly supports failures after mutation for writes, replacements, moves and deletes.

The diagnostic probes construct two histories with identical file names and bytes: one final evidence operation returns successfully; the other throws after mutation, or the subsequent flush/check throws. Re-reading the same files does not distinguish the histories. The fake represents an allowed surviving-byte outcome, not an actual hardware power-loss experiment or proof that all such failures physically persist.

Under the locked requirement that a failed required acknowledgement leaves the live prefix at N-1, placing the acknowledgement in a new receipt moves this question to that receipt's own final publication/barrier. A later marker has the same issue. If the failed history's remaining evidence is also the successful history's evidence, a deterministic reader cannot both recover N for the successful history and reject N for the failed history using only that evidence. Always failing closed on it also fails the normal successful reopen requirement.

Advancing the live acknowledged sequence before the final recovery-proof transition is not an established solution either: interruption between the early acknowledgement and that transition can leave no evidence that distinguishes the newly acknowledged command from a failed candidate. No change was made to redefine acknowledgement that way.

This is the unresolved contract boundary, not a claim that transactional draft storage is generally impossible. A protocol based on a durable commit decision with an explicitly unknown caller outcome can be designed, but allowing recovery to resolve that outcome may recover N when the caller did not observe success. That behavior is presently forbidden. Alternatively, a stronger independently qualified storage primitive would need to expose the missing conclusive outcome guarantee. Neither a semantic change nor such a primitive was assumed or introduced.

## Deterministic evidence and exact results

Validation used the existing isolated checkout at `C:/Users/gdg34/.codex/worktrees/phase7a4-validation/Dungeon-Lord`, at the required HEAD. Its draft runtime source matched the working tree by SHA-256 before running. Only the changed Phase 7A4 test source was copied for these tests; owner save files were not used.

| Test / boundary | Result | What it establishes |
| --- | --- | --- |
| `FailedDraftDurabilityAndFailedRollbackNeverRecoverAnUnacknowledgedCommand` | 1 total, 0 passed, 1 failed, 0 skipped | Original blocker persists: failed flush plus failed rollback leaves live acknowledgement 0 but recovery accepts 1. Added assertions pass for canonical bytes, unchanged mana, disabled CanSave and rejected canonical commit before the final recovery assertion fails. |
| `DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Write)` | Passed | A complete receipt write can throw while leaving the success history's file set and bytes. |
| Same probe, `Replace` | Passed | Staged receipt replacement can throw after publication with the same remaining files as success. |
| Same probe, `Move` | Passed | Initial receipt publication can throw after mutation with the same remaining files as success. |
| Same probe, `Delete` | Passed | Removing a pending marker can throw after removal with the same remaining files as success. |
| Same probe, `Flush` | Passed | A barrier/check can throw without changing the receipt bytes visible in the successful history. |

Probe totals: **5 total, 5 passed, 0 failed, 0 skipped**. Each probe compares every file path and payload, then repeats the reads to demonstrate that another read adds no historical outcome evidence. These tests are explicitly named and commented as investigation probes; they are not a repaired fault matrix and do not suppress or reclassify the failing regression.

Reports retained in the established ignored location:

- `TestResults/phase7a4-reassessment-original-blocker.xml`
- `TestResults/phase7a4-storage-evidence-probes.xml`

Commands used the established Unity CLI `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`, `test`, `--mode EditMode`, exact filters above, and `--timeout 600`.

## Required matrix and gates still unqualified

No corrected implementation exists, so normal corrected-protocol recovery, candidate partial-write failure, successful rollback recovery, every proposed trust transition, predecessor completeness, ambiguous-state Save Changes gates, maximum workload, and recovery idempotence remain unqualified. The original blocker and the probes do not replace that required matrix.

The earlier 31-pass Phase 7A4 run remains historical evidence only. It was not rerun as a full selection during this stopped investigation. Relevant canonical integration selections, full EditMode, full PlayMode, production build/content gates, Windows Development Build, screenshot capture and baseline skipped-test comparison were not run. Owner visual/usability UAT and real-device touch qualification remain outstanding.

## Changes made in this reassessment

1. Strengthened the existing failing regression with canonical-byte, mana and Save Changes assertions.
2. Added the five parameterized storage-evidence diagnostic probes in the same test source.
3. Removed only the two identified trailing spaces in Bootstrap.unity's newly added `m_Name:` fields. No scene reserialization or hierarchy change was performed.
4. Added this companion and linked it from the original evidence without rewriting the original failure history.

`git diff --check` passes after the whitespace repair. Git still emits the configured LF-to-CRLF advisory for existing modified files; no broad line-ending normalization was performed. The working tree remains intentionally dirty with the prior Phase 7A4 implementation and these investigation changes. No PR or merge exists.

## Stop and required next decision

Work stops under the user's conditions covering unresolved acknowledgement promotion and a storage platform that cannot provide the claimed outcome property. The current runtime implementation is still unsafe and unqualified; passing diagnostic probes do not make it suitable for UAT.

Further implementation needs either an approved definition of durable commit versus unknown caller acknowledgement, or a concrete storage primitive with a qualified guarantee sufficient for the current strict definition. The existing API and tested failure model do not provide that guarantee. Until that boundary is settled, no receipt/journal implementation is presented as a completed correction and no broader Phase 7A4 implementation or PR creation proceeds.
