# PR #235 shared-history alias correction

Reviewed implementation: `c887cf7bd12a700ff4230b6a90a6f6460092ddf4` on `codex/living-dungeon-phase-b2`. Main baseline remains `af464e05d63acfa73a57786888266d5901feac2f`. Recommended configuration: GPT-6.1 Sol, Medium; Standard correction, known cause and established activated-history contract. Policy fallback: GPT-6 Sol Medium, then GPT-5.6 Sol Medium.

## Finding and correction

`AllocatorValid` checked the latest/final alias only when the latest sequence already occurred in the recent-ID set. A valid latest ID absent from a populated recent array bypassed the comparison. A null latest record also bypassed all alias checks. `RunHistoryState.AppendOutcome` always makes the latest outcome the final retained record.

The populated-history guard now requires a non-null latest outcome, and the existing final RunId/full serialized-record comparison runs whenever recent history is nonempty. Equality still uses the established serialized public-field representation; no new record format is introduced. Existing sequence parsing, reservation, allocation exhaustion, checkpoint identities, schema and event ordering are unchanged.

This requirement remains gated on activation/current active-save validation. Frozen schemas and 13→14 migration are unchanged. Null latest and the exact historical default record with empty recent history retain their prior behavior; neither is rewritten by activation.

## Added regression coverage

Seven invalid forms are exercised in each of three paths: direct allocator validation, preserved dormant schema-13 migration followed by activation rejection, and already-activated complete-save validation/reopen rejection. They cover an absent ID, an earlier alias, equal ID with conflicting score, equal ID with conflicting tag data, explicit null, populated default record and omitted latest field.

Three positive cases cover a separate but fully equal final-record alias and both null/default latest representations with empty recent history. The existing successful manual-run/automatic-admission coexistence test additionally asserts full final-record alias equality. This adds 24 cases without weakening existing assertions.

Invalid requests publish no session/runtime projection. Tests compare source, durable and committed-session bytes, dormant revision and history bytes. Historical histories remain loadable/migratable while ineligible activation fails closed.

## Qualification environment

The correction uses a separate `Temp/ui-composition-b2-alias-validation` Unity project with only the standard Bootstrap/SaveService filename substitution to `living-dungeon-phase-b2-alias-validation.json`. The prepared owner-UAT project and previous players are not refreshed or overwritten. Existing owner data is hashed before qualification, including the owner's current B2 UAT progress. Only explicitly disposable fixture files may be restored from verified current backups after archiving their test-modified versions.

Original B2 evidence and reports remain intact. Correction reports, redacted logs, input checkpoints and preservation evidence are retained separately. Owner gameplay is not performed.

## Final results

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused allocator, canonical validation, migration and manual-run regressions | 40 | 0 | 0 |
| Full EditMode | 1,873 | 0 | 1 existing |
| Full PlayMode | 3,300 | 0 | 10 existing |
| Production build gates, included in full EditMode | 65 | 0 | 0 |

Every reviewed-commit leaf occurrence, result and skip reason is preserved in both full suites; each adds 24 cases. Full EditMode includes 81 B2 cases, the unchanged Phase A group (42 cases including frozen 38-scenario equivalence), and all 37 B1 cases including deadlines. Existing schema-13/schema-14 persistence, manual-run, stale-session, atomic failure/recovery and real Windows filesystem cases pass. The native Windows migration group retains 31 passes and its established non-Windows inverse skip. No assertions are weakened and no cases are excluded.

Windows Development Build succeeded with Unity 6000.3.2f1, Bootstrap only, 181,485,192 bytes, 0 build errors and 1 native-symbol upload warning. The ignored licensing-client validation message and empty test-assembly warning are retained in the logs; no C# compiler warnings/errors were found. All 52 required art assets, the Bootstrap config and frozen schema-13 resource are packed. `sharedassets0.assets` contains `living-dungeon-phase-b2-alias-validation.json`. The player was not launched.

The strict checker verified 1,254 exact transformed inputs before each run/build and after archiving/restoring Unity-generated input differences. Hash comparison preserved all 10,794 preexisting save/player files, including primary, permanent manual UAT, the owner's current B2 UAT progress, backups and previous builds. Exactly 22 preexisting disposable fixture files were restored from current verified backups after archiving their changed bytes. Primary and both UAT files were never restored, reset, parsed for migration or migrated.

The prepared owner-UAT project/player remains at the earlier reviewed commit; this correction's qualification uses the separate project. Refreshing the owner environment to the correction is a separate setup step after re-review. No production arrivals, settlement, time conversion or B3 work is introduced.

Results, commands, measurements and baseline comparisons: [alias-correction-summary.json](alias-correction-summary.json). Exact NUnit reports: [alias-correction-reports.zip](alias-correction-reports.zip). Redacted logs, input checkpoints, generated-input archives, build inventory and preservation hashes: [alias-correction-evidence.zip](alias-correction-evidence.zip). Original performance measurements remain evidence from their original environment; this correction does not establish mobile performance or large-scale gameplay readiness.

The focused correction is ready for independent re-review. No merge is authorized.
