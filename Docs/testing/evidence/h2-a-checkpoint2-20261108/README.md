# Checkpoint 2 evidence

Read [the integration/performance report](../../h2-a-checkpoint2-integration-report.md) before interpreting the measurements. In particular, the complete two-floor traces are not exactly matched. No frame, timestamp, revision or saved-state difference was normalized away.

`report.json` retains immutable campaign source identities, sanitized stage receipts, archive/member hashes, exact artifact paths, the original failed verdicts and final restoration/reservation checks. `qualification-evidence.zip` contains case names/results and authored measurement lines, receipts, explicit plans and campaign adapters. Raw Unity/CLI logs, private protection maps and saves remain local. The original source-pinned supervisor records remain authoritative; documentation commits do not relabel older campaigns as runs at a newer SHA.

`raw-profiles.zip` contains the four unmodified production profile reports and isolated seed manifests. The two comparison JSON files describe all recorded categories and inclusive scopes. `two-floor-divergence.json` separately describes the exactly matching 6,721-frame prefix, not a repaired full trace. `workload-comparability.json` records control hashes and every seed difference.

The unchanged production instrumentation and fixtures performed the measurements. The retained Python files only reduce existing artifacts offline. After extracting the raw archive, `compare_profiles.py MAIN_JSON PR_JSON UNUSED_OUTPUT_JSON` reproduces the full-profile reduction. `check_workload_comparability.py` is a provenance copy of the script at the exact local analysis path in the ledger; its original path and local fixture artifacts are required. The campaign adapters/plans in the qualification archive likewise retain their original absolute identities; do not rerun them over existing output or save namespaces.

All raw evidence, preparation archives, isolated namespaces, caches and worktrees remain preserved through independent review. Reservations are released by retaining `active.json` with `campaign: null`; the marker files are not deleted. No owner UAT, complete final suite or Windows player qualification is included.
