# H1-F4 lb01 owner-authorized preservation recovery

Campaign `8fdb906232d24b64a8a655c5c340cca3`, source `b31f418332bb636500ca7cb71929a4606a270033`, was exceptionally recovered once after explicit owner authorization. Its historical outcome remains permanently **INCOMPLETE / UNCERTAIN**. Its four selected NUnit cases never executed because of the test compiler error; no pass/fail/skip counts are inferred. Later absence does not prove historical descendant completion.

The exact reviewed script SHA-256 was `980ea6f5ec9d70dc3e858e31a5dac1ce0222a4e777c73403b179f25892689e6c`; diagnostic `7904639fbcae8da560da631ef86f6f130bbfd26d221792271979e92de03b22b4`. The separate read-only owner preflight passed every original guard before execution. The reviewed command ran once with receipt `owner-approved-recovery01`, without script modification, process termination, force unlock, evidence deletion, policy changes or automatic retry.

Only the three archived ProjectSettings files were restored. Their current versions were preserved with hashes before the first restoration. TMP was already original and was not restored. All 1,494 original input hashes passed before the existing exclusive Lease released this campaign's reservation. The script checked original lock bytes before acquisition and used its held handle plus file metadata under Lease.

| Restored file | Original SHA-256 |
| --- | --- |
| ProjectSettings/ProjectSettings.asset | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` |
| ProjectSettings/EditorBuildSettings.asset | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` |
| ProjectSettings/ShaderGraphSettings.asset | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` |

Independent verification reread the complete receipt and actual filesystem: exact restored input inventory, unchanged original TMP/archive, all three preserved current copies, nine pinned tools, both local commits, clean worktree, protected owner/save/historical evidence, 166 prior campaign/preparation directories, unchanged original campaign tree, absent recorded process identities and named Jobs, and actual reservation `campaign=null, previous=<this exact campaign>`. This is recovery verification, not qualification of the interrupted test stage.

Recovery receipt SHA-256: `e86e19ba450c01343771f5f92839a43eed68f802f0a15827a086d3c31df07a21`. Independent receipt SHA-256: `5be7753f73f189cada67c2b6a45cd1ef279a86df3aa0f9d237da2c2862ca343b`.

[Complete sanitized receipts](h1-f4-lb01-preservation-recovery-20261028.json). Exact owned artifacts remain under `TestResults/h1-f4-20261027-lb01/owner-approved-recovery01` and `preservation-review01`; owner H1-F4 qualification/recovery, purpose independent review, retention preserve. The [published proposal and complete manifests](https://github.com/gdg3417/Dungeon-Lord/pull/259#issuecomment-6485483907) retain their original REVIEW ONLY status; this later receipt records the separately authorized execution.

The subsequent compiler correction invokes LINQ Count() at the two new test-only uses, retaining System.Linq and the original no-write assertion. Compilation/discovery, fresh four-case qualification and the remaining B2/B3A/full-suite/Windows/performance gates are separate work; no passing result is claimed here. PR #259 remains draft, with no merge or owner UAT.
