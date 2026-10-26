# H1-F2 qualification

Subsequent owner gameplay passes and post-gameplay persistence observations are recorded in [the owner UAT closeout](pr256-owner-uat-closeout.md). That closeout found a second native process-shutdown failure after a successful AppQuit save; final closeout is blocked pending focused investigation and second-reopen qualification. The original results below remain unchanged historical evidence.

Baseline: main `c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d`, merged #255. Branch: `codex/h1-f2-time-provenance-paired-cuts`. Isolated managed worktree: `C:/Users/gdg34/.codex/worktrees/h1-f2-time-provenance/Dungeon-Lord`.

Final runtime/data implementation: `591b2c3cb27b6d71057cb648ae692609ab4346ab`. Earlier implementation `6e4fa27f162f0a3e19ad3bb37c99599377204c4e` and CLI-launcher correction `4cad8a87a0b241f0d1253befbb89efc4b4c2b8f5` are retained. The final implementation additionally reserves exact schema grammar overhead at historical limits. Source manifests distinguish dirty precommit launches from committed-head runs; they are not relabeled as later launches.

Model recommendation: GPT-6.1 Sol High; Complex, specified cross-system deterministic persistence foundation. GPT-6 Sol High is the availability fallback; Astra Medium is reserved for a concrete unresolved authority/persistence contradiction. No model escalation is claimed.

## Environment and isolation

Unity 6000.3.2f1; Unity CLI 1.0.0-beta.14 (official update check confirmed current). All Unity suites use graphics. The runner temporarily substitutes `Dungeon Lord B3B2 Smoke H1F2 20261025` for the product name and restores the original settings bytes. Windows qualification has its own product namespace, `Dungeon Lord H1F2 Windows 20261025`, and temporary qualification sources copied from the committed Tools templates. The build includes Bootstrap only. The Windows runner and templates are from implementation commit 591b2c3c. Its manifest captures C#, JSON, scenes and assembly definitions; generated font, URP, graphics and connection settings were separately sealed after the build and restored during closeout. Those generated changes are excluded from the PR. No broader asset-input seal is claimed.

The owner's original checkout retains only its pre-existing proposal edit. Its SHA-256 remains `B04136E1B8375BEF6DC2105BACD3E19062B4365BE10E043B91C2D87670BD267D`, identical to the pre-work value. No owner save or prior qualification build is used or overwritten.

## Executed gates

| Gate / report | Passed | Failed | Skipped | Source |
| --- | ---: | ---: | ---: | --- |
| Committed-head focused, `focused-10` | 154 | 0 | 0 | 591b2c3c |
| Migration, failure matrix, readback and Windows filesystem, `migration-04` | 223 | 0 | 1 | Recorded parent 4cad8a87 plus exact input manifest; runtime/data match final implementation |
| Research Start/progress/Claim/reopen, `research-03` | 98 | 0 | 0 | 591b2c3c |
| Final gameplay integration, `integration-04` | 436 | 0 | 0 | 591b2c3c |
| Final PlayMode, `play-03` | 129 | 0 | 0 | 591b2c3c |
| Current/historical schema registry, `schema-03` | 91 | 0 | 0 | Recorded parent 4cad8a87 plus exact input manifest; runtime/data match final implementation |

The single skip is the expected inverse-platform case on Windows. Gates overlap; counts describe executed test invocations, not distinct tests. The full unfiltered project suite, new scene profiling and visual owner UAT are not claimed.

Exact commands, timestamps, XML, per-case results, source manifests and logs are retained under `TestResults/h1-f2`. Publication copies and seals are recorded in [the evidence manifest](evidence/h1-f2/evidence-manifest.json) and [reports archive](evidence/h1-f2/qualification-reports.zip). Authentication arguments in Unity logs are redacted in publication copies; original logs remain local and their original hashes are retained.

## Coverage

| Requested acceptance area | Evidence |
| --- | --- |
| Fractional representation, canonical round trips, carry/borrow, range/overflow, malformed/duplicate values | H1F2TimeProvenance integer/JSON cases |
| Same-second foreground/background, independent clocks, eligibility and paired consistency | H1F2PairedObservation deterministic sources and PlayMode wrapper |
| Invented/interpolated partial cuts and stale predecessor | Opaque live capture, exact-horizon authorization, actual writer endpoint and stale-cut rejection |
| Exact 15→16, historical activation, dormant and active v1/v2 A>C | Full-byte migration comparison, actual coordinator/reopen, existing foreground-authority drain cases |
| Existing whole-second mana, capacity, Research, manual/draft action compatibility | Migration, Research, passive-mana and foreground integration gates |
| Unknown extensions, owner collisions, missing executable/config, unsupported future schemas | Frozen corpus, strict owner cases and coordinator no-write refusals |
| Interrupted migration, rollback, uncertain authority, independent recovery and current readback | New coordinator fault tests plus established migration/failure matrices |
| Original 14→15 bytes and stable schema-15 verdicts | Unchanged 46 pinned JSON hashes and 35 recorded structural/contextual verdict pairs from #255 |
| Exact old byte ceiling and owner/base accounting | Measured 92-byte/four-node/four-record/71-character reserve and a size-boundary migration followed by another canonical save |

Frozen fixtures and 7→15 transforms are unchanged. Disposable minimal/zero-timestamp scaffolds now supply explicit test time where they exercise the new writable target; production migration does not manufacture missing time. Current-target assertions now follow schema 16. A pre-existing PlayMode reflection wrapper passed one argument to a two-argument admission test; the wrapper now exercises its declared three variants.

## Earlier attempts and corrections

All earlier reports are retained as actual outcomes. Focused attempts 01/02 were 18 passed / 116 failed because of strict compatibility-table encoding/canonical layout; 03 was 120/14 after fixing that table; 04 was 139/3 while correcting new fake-profile/zero-time scaffolds. Focused 05–08 passed (142, 150, 152 and 152 respectively). Focused 09 passed 154 after adding exact grammar reserves and size-boundary tests.

The first broad integration selection included workload measurements and was intentionally interrupted; exit 6, no NUnit verdict. It is not counted as passed. The narrowed integration-02 completed 328/10; failures were current-schema expectations and an upgrade chain ending at 15. Integration-03 passed 436. Migration-01 completed 200/23/1; migration-02 222/1/1; migration-03 passed 223 with one expected skip. Research-01 completed 96/2, corrected by explicit test boundaries; research-02 passed 98. Play-01 completed 127/2 (stale Research assertion and the pre-existing reflection wrapper); play-02 passed 129. Schema-01 completed 90/1; its formerly unsupported selector 16 became supported and was replaced by an intentionally unregistered selector; schema-02 passed 91.

The first Windows setup attempt rejected an unsupported CLI output flag before compilation. The supported execute-method invocation then built successfully with zero errors and one warning, but the initial directory guard stopped before player launch because Unity had created the empty test-product directory. These setup/build-only records and the first build are preserved separately. The final runner allows an empty directory while refusing any existing product data and writes the corrected build to a separate output directory.

Final review found that a dedicated owner must not consume historical base-save reserve or strand an old save at its byte ceiling. The final correction adds exactly 92 member bytes, four parsed nodes, four records and 71 string characters to current limits, retaining existing raw/scan ratios. Frozen-15 limits remain byte-identical. The two new headroom tests prove this derivation and an exact-limit migration followed by another canonical save. Focused-09 passed 154; migration-04 passed 223 with one expected skip; schema-03 passed 91. Committed-head focused-10 passed 154, research-03 passed 98 and integration-04 passed 436. Earlier attempts retain their original source identities and results.

## Windows qualification

Passed on Windows using runtime/data commit `591b2c3cb27b6d71057cb648ae692609ab4346ab`. The actual Bootstrap player build succeeded with zero errors and one warning: Unity services could not supply a native-symbol upload URI. This did not prevent build or player execution.

Five separate player processes exited zero and emitted passing receipts: native create/close, reopen/close, schema-15 migration/close, schema-16 reopen/close, and unsupported schema-17 refusal. The first four durable results have schema 16, legacyWholeSeconds mode and zero fraction. The future-source hash remains unchanged. Each phase and durable save snapshot is retained. The schema-15 input is an explicitly recorded projection of the real native save; it is not described as an independently historical production save.

Player: `Builds/Development/Windows-h1-f2-headroom/Dungeon Lord.exe`; SHA-256 `ABC8179E345B70C9D7CA423C54E02739ED9B9ADCA668C2C745FFB81AADC05CC6`. Execution: 2026-10-26 01:17:36–01:18:38 UTC. Commands, source manifest, build report, warning, receipts and save snapshots are archived. This is automated real-player persistence qualification; visual owner UAT is not claimed.

## Compatibility and review boundary

Schema 16 and its field arrangement remain independently reviewable implementation choices. `lastSavedUtcUnix` plus `timeProvenance` form one compound boundary; only whole-second legacy mode and a zero fraction are writable. No independently writable W/passive cursor, fractional grant, original/residual recovery episode, overload system, H2 retention/batching, O1 party catch-up or Phase 8 feature is activated. Existing 14→15 active-source rejection remains historical behavior. Missing/zero historical boundaries and unsupported content/config/executables fail closed without source fabrication.

Ready for independent review, with no known unresolved persistence/authority contradiction. Before merge: independent review of the schema/owner mapping, measured grammar reserve, migration and activation fence, paired-cut authority and the actual qualification evidence. This packet does not complete H1, H2, O1 or Phase 7. Remaining H1 work is listed in the [implementation contract](../planning/h1-f2-time-provenance-paired-cuts.md).
