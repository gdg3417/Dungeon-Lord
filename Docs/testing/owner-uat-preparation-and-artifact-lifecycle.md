# Owner UAT preparation and disposable artifact lifecycle

This runbook defines the player-facing PR handoff and safe handling of disposable UAT/qualification artifacts. It complements the [qualification supervisor](qualification-supervisor.md), the [PR #249 prepared Moonlight workflow](evidence/phase7-production-analysis/owner-moonlight-uat.md), and the [PR #249 functional qualification and coverage limits](evidence/phase7-production-analysis/functional-qualification.md).

## Prepare the owner handoff

For applicable player-facing work, Codex uses the supervisor's [owner UAT readiness stage](qualification-supervisor.md#readiness-stages). Prepare the handoff once implementation is stable and the applicable focused checks pass. List any mandatory final gates deferred until owner acceptance and identify earlier gates required by the change's risk. Supply one concise launch guide with the following information.

- Isolated, named scenarios using existing approved fixtures or preparation tools.
- The qualified build identity and launch location, or the appropriate Editor setup.
- Exact launch and scenario-selection steps, without requiring routine owner setup.
- Expected results and the behaviors each scenario covers.
- Save/reopen steps when persistence or lifecycle behavior is relevant.
- Known limits, evidence to capture, and which results remain automated-only.

Stage the environment for independent review. Only after that review approves readiness may the owner launch UAT. The owner judges discoverability, clarity, feedback, responsiveness, strategic freedom, meaningful decisions and gameplay understanding. Include functional verification in the owner's guide only where human observation is needed. A launcher or automated run cannot record owner acceptance.

Prefer existing qualification tools, approved fixtures and established launch flows such as PR #249's scenario launcher. Use representative starting states. Where the approved preparation authority can safely lease or otherwise isolate multiple scenarios on one product, reuse the sealed exact-source build or its hash-matched copy. Verify the artifact file-set, product identity and each scenario's starting save identity and reopen path. Use a distinct product or Editor setup if the required isolation cannot otherwise be established. Keep runtime overrides within approved contracts.

Codex handles technical preparation, diagnosis and routine process management. The owner receives ready-to-launch scenarios and does not need to edit JSON, move saves, change configuration, run terminal commands or create routine test conditions. Report a specific preparation gap for review when the existing authorities cannot safely establish the case. Use existing launchers within their documented scopes.

When UAT finds a defect and the intended behavior is established, Codex corrects and requalifies it through the [autonomous workflow](qualification-supervisor.md#autonomous-execution-and-review-checkpoints). Ask the owner for a decision when the behavior is materially ambiguous. Retain prior verdicts and send the corrected scenario through the applicable review before renewed acceptance. Record why an unrelated technical correction leaves existing acceptance applicable.

### Historical PR #275 example

The manipulation checkpoint at `a03f5c35567899af56447627bed1860b060ecac3`
used focused production PlayMode, geometry and fixture checks, a Windows
Development build, isolated Floor 1 and Floor 2 reopen cycles and preservation.
The reported 6m 31s covered that selected checkpoint. The final review at
`f68593c9c9001e1bd0535334fe12856898cdf63d` records owner acceptance and later
final gates separately. It reports full EditMode at 66m 36.49s, full PlayMode at
22m 43.74s, and a distinct Windows standalone native filesystem case at 55.66s.
The single-floor (306.71s), two-floor (1,389.54s) and distinct Foreground
(198.38s) 68-run case timings were embedded in EditMode. The complete EditMode
results reconciled all 263 historical regression identities. Earlier failed owner
UAT verdicts and incomplete campaigns retain their original dispositions.
[PR #275 review and qualification record](https://github.com/gdg3417/Dungeon-Lord/pull/275)

UAT preparation does not relax configuration-owned gameplay tuning or localization rules. Do not introduce gameplay-tuning overrides in runtime or ask the owner to change configuration. Player-facing text remains on stable localization keys, follows [Spec 27 fallback behavior](../27%20-%20Localization_and_Text_System.md), and must support future language packs without code changes; validate those behaviors through existing approved fixtures or automated evidence where applicable.

## Track and preserve artifacts

Maintain a per-PR artifact inventory in the evidence packet or preparation manifest. For each build, project copy, scenario, save namespace, process, and evidence set, record:

| Field | Record |
| --- | --- |
| Path or identity | Exact absolute path or product/save/process identity |
| Owner | Codex, the qualification supervisor, an existing approved launcher, or owner-held |
| Purpose | Scenario, qualified build, active session, raw evidence, sanitized evidence, or other use |
| Source | Qualified source/build identity and fixture origin where applicable |
| Retention status | Active, preserve, unresolved, retained evidence, eligible for cleanup, or cleaned |

Preserve owner saves and recovery data, active or uncertain processes, qualified builds, historical evidence and any unresolved failure-reproduction state. Keep raw logs and private save evidence in their approved local locations. Publish only sanitized evidence when required. A successful quit/save marker or a closed window does not establish process exit. Verify the actual process and relevant children, including crash handlers, through the existing authority before reusing a save lease or launching a replacement.

Use the supervisor's exact process ownership, source/input identity, read-only status and [bounded recovery rules](qualification-supervisor.md#bounded-operational-recovery). Keep qualified builds sealed, failed verdicts intact and uncertain work preserved. Existing launchers may perform only their documented exact-identity process handling. For example, the PR #249 launcher has a specific saved-process exit procedure and retains any failed graceful exit as failed. A launcher STOP blocks the affected operation while Codex investigates. Use exceptional authorization when ownership or recovery remains uncertain.

## Cleanup gate

Cleanup is allowed only after the applicable qualification succeeded, required evidence was retained, and the artifact inventory confirms the exact target is disposable, owned by this campaign, and no longer needed. Before cleanup, verify path containment and exact artifact identity against its recorded inventory; ensure no active process, save lease, unresolved failure, or evidence dependency remains. Retain sanitized reports and required historical evidence.

Clean only those verified campaign-owned disposable artifacts. Never clean owner saves, active or uncertain processes, qualified builds, historical evidence, or unresolved failure states under this procedure. If ownership, identity, path, process state, retention need, or cleanup impact is unknown or uncertain, stop and obtain owner approval before cleanup. Record the cleaned path and outcome in the inventory.
