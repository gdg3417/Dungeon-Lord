# H1-F4 shutdown reconciliation and allocator/replacement qualification

Branch `codex/h1-f4-recovery`, draft PR #259; main baseline `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Recommended GPT-6.1 Sol High, Complex; GPT-6 Sol High fallback. No production source, tuning, save format, frozen reader, migration or supervisor rule changed in this correction.

## Surviving state and completed campaign

The computer restart preserved local HEAD `0636f5563a52adfaa20f5b3dc8c6bf51007a7c28` and both commits after pushed HEAD `a01ebfcd5c9d73bc950bfa2c08e1d5494dde9ed8`: `7dd5a945` and `0636f556`. Git reported no staged, unstaged or untracked project changes. Current campaign/preparation JSON files parse, and no `.tmp` or `.partial` files were found there. A first post-shutdown diagnostic failed only while loading a signature-inspection cmdlet; its script and explicit failure record are retained separately. It performed no preservation recovery.

Campaign **`ff4ed8765392487ba8fa325012e4af17`**, source **`0636f5563a52adfaa20f5b3dc8c6bf51007a7c28`**, recorded **23 passed / 0 failed / 0 skipped**, with all five stages PASSED. Final preservation finished at **2026-10-29 03:08:40 UTC**, before the observed reboot at **05:32:28 UTC**. Exact NUnit leaf names match the sealed 23-case selection, including every one of the seven earlier failed identities. No pass was inferred from process absence, and no test campaign was restarted because of the shutdown.

Independent post-shutdown inspection reverified every recorded terminal stage against its campaign manifest, exit/child identities, evidence hashes and exact NUnit records. All 1,494 current input hashes match their original inventory and original archive members; the three settings and TMP are original. Protected owner/save evidence and **176** prior campaign/preparation trees match. The actual reservation is `campaign=null`, `previous=.../ff4ed8765392487ba8fa325012e4af17`.

Recorded Job accounting completed before each passing stage was published. Later named-Job queries were absent with error 2, separately from that historical proof. Old runner PID 23320 has been reused by Chrome. PID 6904 denies the existing direct identity query; an independent OS CIM row reports `conhost.exe`, creation FILETIME `134377255572008140`, distinct from recorded Python creation `134377169046833474`. Its full image path remains unavailable. This is explicit PID-reuse evidence, not an absence assertion or a supervisor-rule exception. No process was controlled or terminated.

## OS tool change: retain historical result, do not relabel applicability

The system Windows PowerShell binary changed across the restart:

| Identity | SHA-256 |
| --- | --- |
| Campaign-pinned binary | `60c9b29843624dd8af6b6a7b26147753be6aeef65098b9a503cc50850afe410f` |
| Installed binary | `d8f182560210d43baaf767d482ad1dc1207372522e91413b9d5e515a875e552d` |

The installed file reports `10.0.26100.9818`; its Microsoft Windows Authenticode signature is Valid. Windows reports updates installed on October 29. These observations are consistent with an OS update; they do not independently establish which update replaced the file. The other nine pinned tool hashes match. The first signature query through Windows PowerShell failed to autoload `Microsoft.PowerShell.Security`; the retained second diagnostic used the existing bundled PowerShell to inspect the same file and records that diagnostic executable's hash.

The unchanged supervisor correctly reports **recorded_status=PASSED, current status=INCOMPLETE, Tool changed: powershell.EXE**. This report preserves both facts. It does not rewrite the campaign, repin its history, claim applicability to the changed tool environment, or reinterpret any earlier INCOMPLETE / UNCERTAIN campaign. Future fresh campaigns must pin the actual installed tool; their strict tool checks remain unchanged. No reservation recovery is necessary.

## Fixture cause and correction

The first selected run, campaign `e48d180350ad40b6961fb26f1a9d8c64` at `7dd5a945`, remains **FAILED: 16 passed / 7 failed / 0 skipped**. Its complete process/Job exit and unchanged qualified inputs permitted normal guarded finalization before the shutdown; the original FAILED verdict, campaign tree and released reservation were not modified.

`7dd5a945` moved the applicable allocator/history and canonical-replacement cases onto supported foreground profiles, their exact consumed inputs and the existing source-bound time-transition helper. It retained allocator, history-byte equality, invalid-state rejection, checkpoint and revision assertions. This exposed a second fixture defect: `CurrentAdmission` expected a future arrival, but the optional-branch fixture had no main-floor monster. `DemandInterval` correctly returned zero at its first main-floor-content eligibility guard; dereferencing `NextOpportunityTime.Value` caused all seven exceptions.

`0636f556` legally places a Goblin and Hidden Cache on the main floor through existing canonical content mutation before automatic demand is exercised. Configuration is authored before immutable input consumption; these later content placements are ordinary canonical gameplay state, not configuration rebinding. The helper retains the same bound inputs and uses the supported foreground pump. No arrival, horizon, W, clock or producer proof is fabricated. The seven exact identities now pass: the final-record-alias admission case and all six canonical-replacement variants. The six variants retain byte-identical nonempty party owners and unchanged revisions, with validated candidate/readback/reopen assertions.

[Complete sanitized evidence](h1-f4-shutdown-and-allocator-qualification-20261029.json) contains exact names/stacks, stage records, source/tool/input identities, restoration/finalization and post-shutdown audit. The audit receipt is `TestResults/h1-f4-20261027-lc02/independent-shutdown-verification02.json`, SHA-256 `4886a9040ea2018015830daf327b622d1f91db2dba18d77f0fa5458697bda8be`. Current preparations and both campaigns are H1-F4-owned retained review evidence. No cleanup, settings restoration, save modification, lease release or Unity execution occurred during the shutdown audit.

## Remaining qualification

This completes the recorded 23-case source qualification; it is not a current full-suite result or an inferred new 123-case failure count. The legacy B2/B3A reproduction remains independently recorded at 23/100/0. Historical B1 terminal retention must retain frozen-reader/pure-scheduler coverage, while current writable tests must use the supported C2 authority. Neither unsupported profiles nor historical checkpoints may be relabeled to bypass this distinction.

Remaining work includes those fixtures, fresh full EditMode and applicable PlayMode, interruption/repeated-crash/reopen, isolated Windows Player startup/gameplay/shutdown, and matched main/H1-F4 performance and memory. No Windows/runtime benchmark result is claimed here. PR #259 remains draft; no merge or owner UAT.
