# Co21 proposal 03 — independent review only

Campaign `25da41d400e343f3b53c560cbe1cfd52`, branch `codex/h1-f4-recovery`, source `ffc46ff1c9105cea71fe18599caeae22e26d0b10`. Historical status INCOMPLETE / UNCERTAIN. Reservation marker matched the existing Co21 hash and owner. The lock path stat matched the pinned identity without opening or acquiring the live lock. The attempt03 journal is unused. Co21 recovery remains unexecuted.

## Scope and hash chain

Proposal 02 script, launcher and review manifest matched their published hashes. They were preserved. Proposal 03 retains all 02 authority constants, process and ownership checks, the nine targets, replacement and journal semantics, and the reservation release predicate. Only lock acquisition and the new attempt identity, output names and related hash pins changed.

| Artifact | SHA-256 |
|---|---|
| [Procedure](h1-f4-co21-recovery-proposal03.py) | `369d3dc3dc4f19402135c8314e8c4426f92ff57dc16df696010aa528edaccc33` |
| [Launcher](h1-f4-co21-recovery-launcher03.ps1) | `80ccfdb70938de4667322afd894c292aa31e0f1d117a79cd3df35bfda8ea3ee7` |
| [Exact command — NOT EXECUTED](h1-f4-co21-exact-command03.ps1) | `490305704562b3d0b13f0374e9aa1ba1e2306666ce6a48af2e8b1a031335cd19` |

Independently parsed the command and launcher, compared the literal pins to the actual file digests, and checked the approval token across all three files. The exact command authenticates the launcher bytes before evaluating them. The launcher authenticates and retains the procedure bytes against replacement. The command is proposed for the managed worktree with the permissions described in review02, only after separate explicit authorization. No live entry point was invoked.

## Lock acquisition

`CreateFileW` requests read access with `OPEN_EXISTING`, `FILE_SHARE_READ | FILE_SHARE_WRITE`, and no delete sharing. It opens the final component itself, without following a final-component reparse point. The guard checks the approved identity on the opened handle and against the path. The existing supervisor Lease then takes its existing byte-range lock, and its own handle must match the same approved identity before the procedure enters restoration scope. The protective handle stays open until after Lease exit, including during reservation release. If the path vanished before the open, Windows refuses the open without creating a file. Once the verified handle is open, ordinary Windows delete and replace operations on the lock are denied. Exceptions close both handles, including a failure after the supervisor stream is acquired.

The guard uses existing Windows sharing and supervisor byte-range exclusion. It does not add a lock protocol. This packet does not establish live lock availability, execute recovery, or independently qualify a multi-file transaction. Review02's existing OS/runtime trust assumptions and partial-failure limits still apply.

## Disposable validation

| Evidence | Outcome | SHA-256 |
|---|---|---|
| [Acquisition tests](h1-f4-co21-lock-fixture03-final.log) | 10 passed, 0 failed | `675a7fd9e3bba3e5104b43d2dd915ac8914c1d0924f48549549ec88be3352648` |
| [Existing recovery-boundary tests against proposal 03](h1-f4-co21-recovery-fixture03.log) | 9 passed, 0 failed | `f1e5769416bc2dbe782ac0958dd0b31d7be7073af579a498bd3449ec03670950` |
| [Benign launcher checks](h1-f4-co21-launcher-fixture03.log) | 4 passed, 0 failed | `f12cfa847836daacdf4a3b1117daf5c99ebe1cee47670da63a6f350500f5561d` |

Acquisition tests cover existing identity and repeat entry, missing lock, removal after a preliminary check, replacement identity, handle identity mismatch, actual cross-process supervisor contention, denied deletion and replacement before and during Lease, exceptions on entry and inside the scope, and protection during reservation release. Failure cases preserve disposable marker and sentinel input bytes, and check that rejected acquisition does not create a lock. Reacquisition and an existing-file DELETE-access probe detect leaked byte locks and protective handles. Only the release test releases a separate disposable marker.

The [first acquisition log](h1-f4-co21-lock-fixture03.log) is retained. Its three fixture errors came from renaming the disposable lock as a leak probe, which changed the Windows handle-reported timestamp used by the next invocation. The final fixture requests DELETE access without renaming. Proposal and launcher bytes did not change. Disposable test paths are retained in the logs. No Co21 campaign files, reservation, lock, inputs, historical evidence or Co20 results were modified.
