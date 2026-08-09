# GD66 save-spatial migration workload sizing evidence

**Status (2026-08-09): PROPOSED AND NOT YET APPROVED.** This is an owner-review sizing packet, not production configuration. It does not activate schema 7 and does not authorize these numbers. The values below were not copied from or derived from `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`.

## Method and evidence boundary

The accounting implementations were inspected directly in `RawSavePayloadClassifier.Scanner`, `ContractJsonWorkloadBudget`, `CanonicalSpatialSaveContracts.TryCanonicalizeCore`, and `DetachedWholeSaveCandidateSerializer`. Repository fixtures inspected include wrapped schemas 1–6 and unwrapped v1, empty and populated migration preparation, R1/R2 semantic fixtures, content-only implicit-container fixtures, whole-save unknown-root/primary preservation, descriptor/journal/receipt contracts, complete-save validation, full save lifecycle, research/objective/run-history tests, and the production compatibility profile and catalog.

The current production contract proves: schemas 1–6 migrate to 7; the active geometry is R1/R2; R2 has two rooms, two fixed endpoints, four route nodes, and three direct edges; Basic rooms each permit two monster, two trap, and two loot assignments; and production run history retains 10 outcomes. Thus the modeled maximum current spatial state has 12 content assignments and **26 canonical records** (`1 floor + 2 rooms + 4 nodes + 3 edges + 2 fixed + 12 assignments + 2 semantics`). Direct-doorway R1/R2 edges carry no corridor footprint tiles, so the current migration model requires zero saved edge-footprint tiles; the proposed tile limit deliberately allows future valid serialized footprints without adopting Phase 3.

Source-reconstructable exact raw fixtures measured without Unity were: empty wrapped v6 **53 bytes, depth 2, 3 object members**; unwrapped v1 **31 bytes, depth 1, 2 members**; and the whole-save unknown-root/primary preservation fixture **161 bytes, depth 4, 5 members in one object, 2 array elements, 9 raw bytes in its longest string, 13 parsed values, 12 object/array records, and 91 decoded UTF-16 name/value units under the report’s independent JSON walk (not the strict contract record counter)**. The production profile is **7,168 bytes**, the production catalog **8,947 bytes**, canonical legacy gameplay configuration **11,080 source JSON bytes**, and the largest repository JSON asset is Bootstrap English at **89,802 bytes**; these data assets are comparison evidence only, not save-limit inputs.

The repository does **not** check in a maximum populated player save or an emitted maximum schema-7 candidate, and Unity is unavailable in this environment. Therefore byte, node, string, and scan-work maxima below are conservative modeled recommendations rather than repository-proven maxima. Owner approval should be conditional on running the measurement/boundary suite described below against emitted schema 1–6, maximum-history, maximum-content R2, unknown-evidence, candidate, descriptor, journal, and recovery artifacts.

## Seventeen-field proposal

| Field | Exact consumer/accounting | Largest proven or modeled current-MVP requirement | Proposed value | Absolute headroom / multiplier | Why and boundary behavior |
|---|---|---:|---:|---:|---|
| `MaximumRawSaveBytes` | `RawSavePayloadClassifier`; exact active payload byte length before cloning/parsing | No archived maximum save; modeled full state is required to remain below 128 KiB | **262144 bytes** | at least 131072 / 2× modeled ceiling | Caps clone and raw tree input; a bloated or appended save stops before deserialization. Test exact 262144 and 262145 bytes with otherwise valid padded unknown evidence. |
| `MaximumRawNestingDepth` | scanner stack; simultaneously open object/array containers | depth 4 exact preservation fixture; modeled full save contracts remain below 16 | **32** | 16 / 2× modeled | Supports current nested outcomes/summaries while stopping stack/deep-container attacks. Test 32 accepted and 33 rejected with valid nested unknown arrays. |
| `MaximumRawObjectMembers` | scanner `Frame.Names.Count`; independently per object | 21 recognized legacy primary names; schema-7 adds 2 owners; modeled primary ≤23 | **64** | 41 / 2.78× | Allows unknown extension members without permitting huge hash sets. Test a valid primary with 64 unique members and 65 rejection. |
| `MaximumRawArrayElements` | scanner elements; independently per array | 12 canonical assignments; 10 run outcomes; modeled legacy floor slots are smaller | **128** | 116 / 10.67× assignments | Allows extension arrays and current history, blocks extremely broad arrays. Exact/plus-one array fixture required. |
| `MaximumRawStringBytes` | scanner bytes between quotes, including escape bytes, before UTF-8 decoding | longest exact fixture token 9 bytes; stable hashes 64 bytes; current identifiers are far smaller than 1 KiB | **4096 bytes** | 4032 / 64× hash | Accommodates extension strings/local notes without allowing one token to dominate memory. Test 4096 raw ASCII/escapes and 4097; separately test multibyte UTF-8. |
| `MaximumRawScanWork` | every consumed lexical byte plus delimiter checks and failed alternatives | exact value must be emitted by scanner instrumentation; valid JSON work is greater than bytes | **1048576 charges** | policy ceiling; measured multiplier pending | Independent provisional cap, not derived from raw bytes. The Unity harness binary-searches the real scanner success threshold for every retained raw fixture; approval requires the measured worst case and exact/+1 synthetic boundary. |
| `MaximumSerializedInputBytes` | strict parser/writer for canonical members, complete save, descriptor, journal, receipts/intents | no archived maximum emitted candidate; comparison inputs ≤11,080 bytes; modeled complete save <128 KiB | **262144 bytes** | ≥131072 / 2× modeled | One strict contract cannot allocate/emit beyond 256 KiB. Test each largest contract at its measured minimum and a padded valid complete save at limit/+1. |
| `MaximumSerializedParsedNodes` | `ContractJsonWorkloadBudget.TryNode`; every JSON value node | exact preservation fixture 13; modeled maximum-history/R2 save expected well below 2048 | **8192 nodes** | ≥6144 / ≥4× modeled | Separately bounds parse object graph even for tiny tokens. Binary-search boundary tests for complete save, canonical members, descriptor and journal plus synthetic 8192/8193-node arrays. |
| `MaximumSerializedCollectionRecords` | explicit `Record()` calls for contract-owned array/object records, not all nodes | canonical modeled records 26; descriptor/journal record counts are smaller | **2048 records** | 2022 / 78.77× canonical | Allows preserved independent state and future compatible records without equating records to nodes. Test contract-specific exact minima and 2048/2049 record fixtures. |
| `MaximumSerializedStringCharacters` | cumulative decoded property names and string values in UTF-16 units | exact preservation fixture 91; hashes/IDs dominate strict sidecars; no archived maximum full save | **131072 UTF-16 units** | ≥65536 / ≥2× modeled 64K | Independent from raw UTF-8 bytes, including surrogate-pair behavior. Test 131072 and 131073 units and non-BMP strings. |
| `MaximumSerializedDiagnostics` | `SpatialIssueCollector`; retained issues before final slot becomes `WorkloadExceeded` | valid inputs produce 0; contracts can report multiple structural issues | **64 issues** | 64 over valid / bounded ~256-byte enum payload plus list overhead | Gives actionable deterministic diagnostics without unbounded error accumulation. Test 64 distinct issues and 65th replacement/exhaustion semantics. |
| `MaximumCanonicalSpatialRecords` | canonicalizer sum of floors, rooms, nodes, edges, fixed structures, assignments, semantics | **26** modeled maximum supported R2 content combination | **64 records** | 38 / 2.46× | Compatibility/lossless headroom for the approved MVP only; it does not authorize more rooms or Phase-3 topology. Test canonical 64 and 65 record structurally valid synthetic states, plus production R1/R2. |
| `MaximumCanonicalMaterializedTiles` | sum of saved `edge.Footprint.OccupiedTiles` only | **0** for current direct-doorway R1/R2; production layout occupied totals 26/42 are different accounting | **64 tiles** | 64 over current | Lossless compatibility allowance for pre-existing serialized footprints only; never derived from content validation tiles and not authority for corridor construction. Test 64/65 unique footprint entries plus R1/R2 zero-footprint regression. |
| `MaximumWholeSaveCandidateBytes` | `BoundedOutput`; entire emitted schema-7 root | no archived maximum candidate; must include copied primary, unknowns, canonical owners and envelope | **262144 bytes** | policy ceiling; measured expansion pending | Must not exceed the strict complete-save input ceiling because `BuildPrepared` immediately parses the whole candidate. Exact/+1 construction must keep every other applicable budget permissive. |
| `MaximumCopiedSourceValueBytes` | cumulative raw bytes of recognized legacy primary values copied losslessly | no archived maximum; at most source bytes minus names/envelope | **262144 bytes** | ≥131072 / ≥2× modeled | Independent from total output; stops recognized state values consuming the whole candidate budget. Exact/+1 test across multiple recognized values. |
| `MaximumUnknownMembers` | combined unknown root plus primary member count | exact preservation fixture has 3 (`rootBefore`, `unknown`, `rootAfter`) | **64 members** | 61 / 21.33× | Allows forward-compatible evidence but bounds lists/names; independent from bytes. Test split root/primary totals of 64 and 65. |
| `MaximumUnknownMemberBytes` | cumulative raw JSON value bytes for unknown root plus primary members | exact preservation fixture values total 22 bytes (`[1,{"x":true}]`, `1.00`, `false`) | **131072 bytes** | 131050 / 5957× exact fixture | Allows meaningful future extension evidence while bounding retained raw slices. Test one and many members totaling exactly 131072 and 131073 bytes. |

## Required cross-field configuration invariants

1. `MaximumWholeSaveCandidateBytes <= MaximumSerializedInputBytes` is mandatory. `BuildPrepared` immediately passes the complete candidate to `DetachedCompleteSaveContract.ParseValidateAndRoundTrip`, so any larger advertised candidate-success boundary is unreachable.
2. A production-valid candidate must independently satisfy serialized input bytes, parsed nodes, collection records, decoded string characters, diagnostics, canonical records, and canonical footprint tiles. Configuration validation must reject a profile whose advertised whole-candidate boundary cannot be exercised under the other strict ceilings.
3. Candidate construction needs expansion allowance for the schema-7 envelope, member names, `canonicalSpatialAuthority`, and `spatialFloors`. This is not expressible as a fixed equality: copied recognized values and unknown values are both subsets of the raw payload, while canonical bytes vary by R1/R2/content. Approval must use the harness’s measured `candidateBytes - rawBytes`, `candidateBytes - copiedBytes - unknownBytes`, and maximum-content candidate results.
4. `MaximumCopiedSourceValueBytes` and `MaximumUnknownMemberBytes` must each be no greater than the raw payload ceiling for reachable inputs, but their sum need not be accepted for every raw payload because envelope/name bytes also consume that ceiling. A raw-valid payload that exceeds a candidate/copy/unknown workload budget must fail with the stable `gd66.payload.workload_exceeded`, never incidental `gd66.transaction.candidate_invalid`.
5. Cross-field checks validate coherence only. They do not derive a missing field, provide fallback values, or erase the fields’ distinct accounting semantics.

## Independence and memory implications

These values intentionally do not derive from one another. Raw scan work counts parser operations, not bytes. Raw string bytes include JSON escapes and UTF-8 bytes, while serialized string accounting uses decoded UTF-16 units. Parsed nodes count all values; collection records count only explicit contract records. Canonical records use the spatial domain sum above; canonical tiles count saved edge-footprint entries only. Candidate bytes include envelope, member names, canonical owners, and unknowns; copied-source bytes count recognized raw values only. Unknown count and unknown bytes constrain different attacks.

At the proposed ceilings, the largest mandatory byte buffers are approximately 256 KiB raw input, its 256 KiB owned clone, and a 256 KiB candidate output (about 768 KiB of byte payload at peak before parser objects and temporary strings). `BoundedOutput` currently uses `List<byte>`, so transient backing-array growth may approach another candidate-sized allocation. An 8,192-node object graph and 2,048 records can add several MiB depending on Mono/IL2CPP object overhead; this must be profiled in Windows Editor/Standalone and the lowest supported mobile-memory test device before approval. The profile is “mobile-conscious,” not mobile-qualified; GD66 durability support remains Windows-only.

## Measured requirements versus policy decisions

Repository-proven/model-derived requirements currently exist for raw structural depth/member shapes, the 10-entry history policy, the R1/R2 topology, the 26-record maximum-content R2 model, and zero saved direct-edge footprint tiles. The committed Unity harness will replace source-only measurements with the exact consumer thresholds for every retained fixture.

Until that harness executes, the proposed byte ceilings, raw scan-work ceiling, strict node/record/string ceilings, unknown-evidence allowances, candidate/copy ceilings, diagnostics allowance, and all excess above the 26-record/zero-tile MVP model are **policy/headroom decisions**, not measured production requirements. The 64-tile allowance is only lossless-compatibility policy; it is not current-MVP geometry demand and does not authorize Phase 3. Large unknown-member headroom is forward-compatible evidence policy, not current gameplay demand.

## Paste-ready proposal

```json
{
  "Schema": "save_spatial_migration_limits",
  "SchemaVersion": 1,
  "MaximumRawSaveBytes": 262144,
  "MaximumRawNestingDepth": 32,
  "MaximumRawObjectMembers": 64,
  "MaximumRawArrayElements": 128,
  "MaximumRawStringBytes": 4096,
  "MaximumRawScanWork": 1048576,
  "MaximumSerializedInputBytes": 262144,
  "MaximumSerializedParsedNodes": 8192,
  "MaximumSerializedCollectionRecords": 2048,
  "MaximumSerializedStringCharacters": 131072,
  "MaximumSerializedDiagnostics": 64,
  "MaximumCanonicalSpatialRecords": 64,
  "MaximumCanonicalMaterializedTiles": 64,
  "MaximumWholeSaveCandidateBytes": 262144,
  "MaximumCopiedSourceValueBytes": 262144,
  "MaximumUnknownMembers": 64,
  "MaximumUnknownMemberBytes": 131072
}
```

**PROPOSED AND NOT YET APPROVED. Do not add this JSON to production data or compose it into live save services until owner approval.**

## Approval and boundary-test gate

The committed `Gd66SaveWorkloadMeasurementTests` harness emits exact counters (by binary-searching the production consumers’ acceptance boundaries) for: wrapped schemas 1–6; unwrapped v1; empty canonical; R1; maximum-content R2; content-only implicit container; 10-result run history; populated `dungeonLayout` and `structureRuntime`; pending/in-progress/completed research; objectives and lifecycle timestamps; unknown root/primary evidence; migrated schema-7 candidate; native candidate; descriptor; every journal stage; receipt; restoration intent; and complete-save round trip. Persist the measured table as assertions or reviewed evidence, not runtime defaults.

Every approved field needs an exact-limit success and limit-plus-one fail-closed test where a structurally valid fixture can reach the boundary. Diagnostic exhaustion instead asserts the stable `WorkloadExceeded` replacement rule. Raw scan work must expose a test-only counter because input bytes cannot predict delimiter charges. Canonical tile tests must synthesize valid saved edge footprints without changing production R1/R2 or enabling corridor gameplay. Whole-candidate/copied/unknown tests must vary each budget independently and prove prior bytes remain unchanged on failure.

No byte/node/string/scan numeric recommendation is a repository-proven maximum until that committed harness runs under Unity. The proposed values are suitable for owner discussion, but final approval should be withheld if profiling shows the node graph or `List<byte>` growth exceeds the mobile memory budget, or if emitted maximum valid fixtures approach 50% of their proposed ceilings.
