# Schema-15 source-pinned fixtures

Source: `09a46810aeab7441a2ac09ac6220b0f4cc13d6dd` (main, PR #254).

The capture ran before runtime edits. `baseline-capture.cs.txt` and `baseline-wrapper.cs.txt` preserve the exact capture harness, outside Unity's input roots so regression tests cannot regenerate expectations. To reproduce, use a fresh checkout of that source commit, copy the harness to its original test paths, and run the `H1F1BaselineCapture` Editor fixture. It refuses to overwrite files. Do not run it against the implementation being tested.

`SHA256SUMS.tsv` pins original complete saves, their canonical configurations, schema-14 sources, baseline 14→15 candidates, malformed/unsupported variants and limits. Non-file rows record baseline upgrade acceptance. `verdicts.tsv` records independent baseline structural/contextual acceptance for 35 source-pinned cases; structural verdicts invoke the original private core with schema 15 and the registered profiles. Byte fixtures use `-text` Git attributes to preserve original bytes and hashes.

The dormant extension fixture preserves compatible unknown root/primary values. The populated dormant fixture contains two monster assignments. Active v1 (checkpoint format 2) and v2 (format 3) each retain one party, next ordinal 0, run allocator 2 and an accepted horizon of 100 with closed horizon -1. The pinned configs include the original isolated test survival settings; they are not production tuning changes.

Baseline upgrades accept the two dormant sources and produce byte-identical originals. They reject active v1/v2 schema-14 sources because the original output check receives no profiles. The frozen implementation must preserve both outcomes. Neither candidate files nor verdicts are derived from the new implementation.

The baseline capture report and pre-edit runtime hashes are retained in `Docs/testing/evidence/h1-f1-frozen-schema15/`.
