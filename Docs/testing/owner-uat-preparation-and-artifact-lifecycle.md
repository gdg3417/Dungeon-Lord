# Owner UAT preparation and disposable artifact lifecycle

This runbook defines the player-facing PR handoff and safe handling of disposable UAT/qualification artifacts. It complements the [qualification supervisor](qualification-supervisor.md), the [PR #249 prepared Moonlight workflow](evidence/phase7-production-analysis/owner-moonlight-uat.md), and the [PR #249 functional qualification and coverage limits](evidence/phase7-production-analysis/functional-qualification.md).

## Prepare the owner handoff

For a player-facing PR, after implementation is stable and automated qualification has passed, Codex prepares a ready-to-use UAT handoff as part of the normal PR review packet when owner UAT applies. Include:

- Isolated, named scenarios using existing approved fixtures or preparation tools.
- The qualified build identity and launch location, or the appropriate Editor setup.
- Exact launch and scenario-selection steps, without requiring routine owner setup.
- Expected results and the behaviors each scenario covers.
- Save/reopen steps when persistence or lifecycle behavior is relevant.
- Known limits, evidence to capture, and which results remain automated-only.

Prepare the environment for independent review; do not launch owner UAT automatically. Independent review determines whether the owner may begin. Owner observations remain the owner's acceptance, not a launcher or automated-qualification verdict.

Prefer existing qualification tools, approved fixtures, and established launch flows such as PR #249's scenario launcher. Do not introduce a new framework or a bespoke launcher for each PR. Do not ask the owner to edit JSON, move or modify save files, change configuration, or construct routine test conditions. Use isolated scenario/product/save identities and have Codex prepare them through existing approved mechanisms. If existing tools cannot safely prepare the required case, document the gap for review rather than delegating manual artifact work to the owner.

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

Preserve owner saves and recovery data, active or uncertain processes, qualified builds, historical evidence, and any unresolved failure-reproduction state. Keep raw logs and private save evidence in their approved local locations; publish only sanitized evidence when required. Do not treat a successful save marker or a closed window as proof that its process exited. Do not launch a replacement session while a prior owned process or reservation remains unresolved.

Use the qualification supervisor's exact process ownership, source/input identity, status, and preservation rules. Its `status` operation is read-only; incomplete or uncertain campaigns remain preserved for review. Do not force-unlock, kill an orphan, relabel a failure, overwrite an existing build, or bypass a STOP condition. Any exceptional recovery remains subject to the supervisor's approval requirements. Existing launchers may perform only their documented, exact-identity process handling; that is not general permission to terminate processes.

## Cleanup gate

Cleanup is allowed only after the applicable qualification succeeded, required evidence was retained, and the artifact inventory confirms the exact target is disposable, owned by this campaign, and no longer needed. Before cleanup, verify path containment and exact artifact identity against its recorded inventory; ensure no active process, save lease, unresolved failure, or evidence dependency remains. Retain sanitized reports and required historical evidence.

Clean only those verified campaign-owned disposable artifacts. Never clean owner saves, active or uncertain processes, qualified builds, historical evidence, or unresolved failure states under this procedure. If ownership, identity, path, process state, retention need, or cleanup impact is unknown or uncertain, stop and obtain owner approval before cleanup. Record the cleaned path and outcome in the inventory.

This workflow prepares materials and protects artifacts; it does not start UAT, decide whether UAT may begin, record owner acceptance, or authorize indiscriminate cleanup.
