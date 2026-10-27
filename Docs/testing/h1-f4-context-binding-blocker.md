# H1-F4 implementation dependency audit — context binding blocker

2026-10-26 diagnosis; 2026-10-27 resolution. **CONTEXT AND HISTORICAL-ACTIVATION POLICIES RESOLVED.** The owner approves full CombatC0-inclusive bootstrap bytes, twelve context-v3 inputs, persistent owner-level identity and producer-bound evidence for historical A>C. Unproved historical sources fail closed; A=C proceeds only after applicable historical validations. Automatic schema-17 context rebinding remains unsupported. The diagnosis below is retained as historical evidence, not an outstanding implementation blocker. See the [current dependency audit](h1-f4-context-v2-dependency-audit.md).

Branch: `codex/h1-f4-recovery`. Base and unchanged runtime HEAD:
`3eb6ccf93558b8145d64e9e63cbefcbd73484602` (#258). Local `origin/main`
also resolves to that commit. No successor schema or runtime change has started.

The owner approved the complete recovery design and authorized implementation.
This finding comes from required safeguard 8: checking the ten hash inputs against
the actual recovery/executable/load dependencies. It does not reopen L1, S/Q,
the episode grammar, duration accounting, ordering or closure policy.

Recommended configuration: **GPT-6.1 Sol High; Complex**, because the approved
cross-system contract is specified. Actual model: GPT-6.1 Sol; the executing
reasoning setting is not independently exposed. Availability fallback:
GPT-6 Sol High; no substitution was selected.

## Newly established mismatch

The [pre-correction approved contract](evidence/h1-f4/context-binding-blocker/approved-design-before-context-v2.md) defined digest
input 1 as `LegacyGameplayConfigurationContract.SerializeCanonical(parsedRunConfig)`.
That is intentionally a historical projection which excludes live Combat C0 data.

| Source at the pinned runtime commit | Relevant behavior |
| --- | --- |
| [Models.cs](../../Assets/_Project/Scripts/Util/Models.cs#L277) | `RunSimulationConfig.CombatC0` is explicitly `[NonSerialized]`. |
| [Legacy serializer](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialMigrationPreparation.cs#L51) | Uses `JsonUtility.ToJson(value, true)` plus LF; the excluded field cannot affect those bytes. |
| [Combat config](../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Config.cs#L19) | Explicitly documents exclusion as necessary to preserve qualified B1 serialization/start/checkpoint bytes. The full bootstrap parser attaches Combat C0 separately. |
| [Recovery rules](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs#L52) | Maximum HP comes from the attached combat monster profiles; readiness derives from that maximum and the separate recovery resource. |
| [SaveService configuration](../../Assets/_Project/Scripts/Services/SaveService.cs#L244) | Constructs recovery rules from the full live config, while the canonical validation context also retains the legacy serialized config. These are distinct projections. |
| [Monster validity](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs#L96) | Checks saved HP against the current maximum/readiness. It does not bind the historical maximum used by a pending episode. |
| [Combat profile validation](../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0QualificationConfig.cs#L72) | Requires positive monster maximum HP, not equality to an immutable authored value. |

Change only Goblin maximum HP in the full bootstrap configuration from 8 to 10.
Both values satisfy the inspected maximum-HP constraint. Legacy serialization is
unchanged, and digest inputs 2–10 are unchanged. The approved context digest
therefore cannot distinguish the two recovery contexts. A SHA-256 collision is not
involved: its input is identical because the relevant dependency was omitted.

Active checkpoint validation does not provide a universal substitute: recovery
also applies with no active checkpoints. A pending episode can have C=A and a
vacant active floor. The existing saved individual has no prior maximum-HP field.
An injured living record remains within both positive maxima.

The production recovery resource is readiness 66/100 and one mana per HP. With
sufficient funds and one injured Goblin on a vacant active floor:

| Pending phase / saved HP | Max HP 8 | Max HP 10 |
| --- | --- | --- |
| OriginalPending / HP 1 / wallet 100 | Readiness 6; heal 5; wallet 95 | Readiness 7; heal 6; wallet 94 |
| ResidualPending / HP 6 / wallet 100 | Readiness 6; heal 0; wallet 100 | Readiness 7; heal 1; wallet 99 |

The residual example also shows a state which already completed its original
recovery under the earlier rules. Reopening under the changed bootstrap could
perform a different residual recovery despite matching the approved digest.
Determinism and the requested fail-closed compatibility guarantee would be
unproven if implementation simply followed that digest.

## Reproduction and limits of evidence

[probe.py](evidence/h1-f4/context-binding-blocker/probe.py) is a read-only source and
fixture probe, with only an explicit new evidence output. It checks the four
relevant source facts, creates the hypothetical config variant in memory,
checks equality of the non-Combat projection and computes both integer recovery
examples. It never changes the production config or a save.

Executed successfully using the bundled Python executable:

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Docs/testing/evidence/h1-f4/context-binding-blocker/probe.py --repo . --output Docs/testing/evidence/h1-f4/context-binding-blocker/result.json
```

[result.json](evidence/h1-f4/context-binding-blocker/result.json) pins runtime HEAD,
the probe, eleven inspected source/input files and the unchanged approved design.
Result SHA-256:
`9f93d388b24be6fee92eddf75f0d6310376460c0ffd891d2fc757d17e5481eae`.

The projection hashes in this probe are diagnostic Python JSON hashes, explicitly
**not production JsonUtility hashes**. Equality of the production input follows
from the explicit NonSerialized declaration and the inspected serializer. No
Unity serialization run or real canonical-save reopen has been executed. The
numeric examples reproduce the inspected integer rules; they are not counted as
Unit/SIT or successful canonical publication tests.

## Approved focused correction

Replace context component 1 with SHA-256 of the **exact full
`runSimulationConfigJson.bytes` used to construct and validate the live config**,
including its separately attached CombatC0 section. Hash those immutable loaded
bytes after successful complete bootstrap parsing/validation. Do not reload a
different asset or reconstruct bytes from the legacy projection.

Use domain prefix `DL-H1F4-context-v2` followed by LF to distinguish this corrected
binding. Keep the other nine components and their order. This is still ten raw
32-byte component digests after a 19-byte domain prefix, with one 64-character
saved digest. The approved format-2 owner, 590-byte grammar envelope and proposed
schema-owned limits need no size change for this correction.

Preserve `LegacyGameplayConfigurationContract`, its outputs, schema-16 history and
all historical checkpoint contracts exactly. Do not remove NonSerialized or add
Combat C0 to that historical serializer. The full-asset binding deliberately
rejects raw config changes, including formatting-only changes, consistently with
the other byte-bound components. No tuning or copied configuration is introduced.

The owner and independent review explicitly approved this exact input correction.
The [current design gate](../planning/h1-f4-recovery-design-gate.md) now records it.
The original approved document was copied byte-for-byte to the evidence directory
before editing; its SHA-256 is still EB11094727CFDE1450A56C60F47949A5DF179F8DAD7280B670EE90BF65FA29EF.
No legacy serializer or runtime contract has changed. The remaining dependency
audit found additional inputs which this focused correction does not cover.

Required executable regression: two valid full configs differing
only in CombatC0 must retain identical legacy serialization but produce different
recovery context digests; reopening a pending episode with the changed config
must reject before any mana/HP effects. Cover both phases, no-active-checkpoint
states and unchanged-config reopen, then run the full approved qualification.
Include explicit readiness 6→7, byte-identical unchanged-input digests and frozen
historical serialization. These production/episode regressions are still pending;
the source/fixture probes do not count as their execution or as episode rejection
tests. The new dependency blocker must be resolved before live recovery continues.

## Qualification, benchmark and preservation status

| Gate | Actual status |
| --- | --- |
| Source/fixture dependency probe | Exit 0; four structural assertions and two arithmetic examples |
| Unit/EditMode, SIT, fault injection, historical compatibility | Applicable; NOT RUN for H1-F4 |
| PlayMode / isolated Windows Development Player | Applicable; NOT RUN for H1-F4 |
| Fresh pre-H1-F4 and final H1-F4 benchmarks | NOT RUN; pending correction and implementation |
| Historical performance evidence | Existing evidence preserved; no new performance verdict |
| Owner UAT / draft PR / merge | NOT STARTED |

No runtime pass/fail/skip counts, speedups or regression verdict are claimed.
The known intermittent native Windows shutdown problem remains unresolved and
has not been relabeled as a passing exit. The diagnostic Python child exited zero;
no Unity, player or supervisor process was launched by this investigation.

| Exact artifact path | Owner / purpose | Retention |
| --- | --- | --- |
| `C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord` | Current task; approved design and dependency audit | Preserve |
| Worktree `Docs/testing/evidence/h1-f4/context-binding-blocker/approved-design-before-context-v2.md` | Exact pre-correction approved proposal; SHA-256 EB11094727CFDE1450A56C60F47949A5DF179F8DAD7280B670EE90BF65FA29EF | Preserved byte-for-byte; current planning document updated as authorized |
| Worktree `Docs/testing/h1-f4-context-binding-blocker.md` | Current task; implementation blocker and proposed correction | Preserve for review |
| Worktree `Docs/testing/evidence/h1-f4/context-binding-blocker/probe.py` | Current task; repeatable source/fixture diagnostic | Preserve |
| Worktree `Docs/testing/evidence/h1-f4/context-binding-blocker/result.json` | Current task; source-pinned diagnostic evidence | Preserve |
| Owner checkout, saves, historical builds/evidence and stopped campaigns | Owner / prior campaigns | No writes, cleanup or launches performed |

No tracked runtime, resource, migration, test, tool or project-setting file was
changed. No schema successor was introduced before discovering this dependency
mismatch. No commit or PR was created.

**COMBATC0 CORRECTION APPROVED — SEE THE NEW CONTEXT-V2 DEPENDENCY AUDIT**
