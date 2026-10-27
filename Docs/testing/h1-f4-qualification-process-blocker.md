# H1-F4 qualification process-completion blocker — 2026-10-27

Branch `codex/h1-f4-recovery`, base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`.
No new recovery policy decision is requested. Historical producer-bound activation,
no automatic rebinding, L1, S/Q, format 2, twelve context inputs and the single W
remain approved. This is an exceptional qualification-reservation recovery request.

## Finding and correction

Campaign `9ed872130a134ce7a5627f6c001b8e2f` tested source
`f5ef3e7310c81fe2d22e7979dd72796f4731cfe2`. Unity reported CS0103 at
`DetachedCanonicalWriteAuthority.Recovery.cs:22`: the new partial file omitted
`using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;`. That import is now added.
This is an implementation error, not a design incompatibility. It has not been
recompiled, and no passing tests are claimed for the corrected source.

The runner recorded CLI exit 6; Unity exited before producing NUnit XML.
The selected suite expected 48 cases; discovered/pass/fail/skip counts are unknown.
Separately, the supervisor's Job Object completion guard did not verify empty
ownership within its existing five-second window. Its exact terminal result is
`INCOMPLETE / UNCERTAIN: Owned descendants still active; completion uncertain`.
The persistent project reservation remains assigned to this campaign. Restoration
and final preservation stages did not run.

Later read-only checks found no live process matching the recorded supervisor
(PID 33824 / creation FILETIME 134375784580112532), host (43560 /
134375784707227234), or runner (25108 / 134375784708308671). No Unity Editor/player
was found by the approved guard. Read-only opening of the recorded named job
failed with Windows error 2. These later observations do not prove the missing
completion-window evidence and do not change the campaign verdict. The guard did
not retain a descendant identity that establishes the cause of the missed drain.

No Windows Development Player ran in this attempt. The known intermittent native
Windows shutdown issue remains unresolved; this Editor compiler failure and
unverified Job Object completion are reported separately. No shutdown success,
native crash diagnosis or proven root cause is inferred from missing processes.

## Preserved evidence and scope

The [sanitized diagnostic and exact recovery proposal](h1-f4-qualification-process-blocker.json)
record all campaign-file hashes, exact process identities, the original-input archive
hash and the one-line correction's before/after hashes. Raw Unity logs remain local.

Local owned artifacts, all retained:

- `TestResults/qualification-supervisor/9ed872130a134ce7a5627f6c001b8e2f`: complete stopped campaign.
- `TestResults/h1-f4-20261027-transitions01`: sealed original/qualified input maps, original-input archive and protected-evidence map.
- Its `incomplete-investigation/diagnostic.json` and `recovery-proposal.json`: source-pinned investigation and the proposed exceptional action.
- `TestResults/qualification-supervisor-reservation/active.json`: unchanged unresolved reservation.
- The three temporary isolated settings remain in the managed worktree: ProjectSettings, EditorBuildSettings and ShaderGraphSettings. Their original bytes are archived; restoration awaits approval.

Before the import correction, the complete qualified Unity input map matched its
sealed preparation and all protected owner/save/historical evidence matched.
No process was terminated, reservation released, evidence removed, settings restored,
new campaign launched or supervisor rule changed after the incomplete result.

Completed qualification remains scoped to the [foundation receipt](evidence/h1-f4/foundation-20261027/qualification.json):
30 passed, 0 failed, 0 skipped at `831b1acb159c11e885fe5039b5b26a6ea04c3b1d`,
with supervised restoration and preservation passed. The earlier 29-pass/one-failure
test-assertion attempt is retained. Six disposable preparation-tool tests passed;
they do not qualify gameplay recovery.

The implementation has staged immutable consumed-byte validation, checked
predecessor-bound time transitions, original/residual publication and detached
historical activation. Schema 16 remains the active writer. Full schema-17/live
integration, protocol-cut tests, PlayMode/Windows, UAT preparation and the three-way
performance comparison remain pending. No PR, merge or owner UAT was initiated.

## Exact approval required

The [qualification runbook](qualification-supervisor.md) requires:

> Codex must investigate and obtain owner approval for any recovery that would release an uncertain campaign or terminate an orphan.

Approve only the exceptional recovery of campaign
`9ed872130a134ce7a5627f6c001b8e2f`: acquire the existing execution lock, recheck
the exact reservation/process/evidence identities, verify every Unity input against
the sealed map except the reviewed import correction, restore only the three
archived settings, verify exact preservation, and release this reservation using
the existing Lease authority. Retain the entire campaign as INCOMPLETE. No process
termination is proposed. Any unexpected evidence/input/process mismatch refuses.

After approval, create a fresh isolated 48-case attempt at the corrected source.
Do not resume or relabel this campaign, extend the Job Object drain window, weaken
exit verification or add an exemption. Continue the approved implementation and
qualification only under those existing authorities.

**BLOCKED — NEW SPECIFIC ISSUE**
