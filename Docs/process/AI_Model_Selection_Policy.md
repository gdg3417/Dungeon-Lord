# Dungeon Lord AI Model and Reasoning Selection Policy

Status: Active  
Version: 2.1  
Last reviewed: 2026-11-21  
Scope: ChatGPT project work, ChatGPT Work, Codex Desktop, Codex CLI, Codex Cloud, implementation prompts, correction prompts, repository reviews, debugging, test-failure investigation, and other AI-assisted development work for Dungeon Lord.

## 1. Purpose

This policy establishes stable rules for selecting OpenAI models and reasoning levels for Dungeon Lord development.

Its goals are to:

1. Use the lowest-cost configuration that is appropriate for the task.
2. Preserve high-cost model usage for work that materially benefits from it.
3. Avoid inconsistent model recommendations between conversations.
4. Prevent recommendations from changing merely because the user questions them.
5. Make model selection explicit and reviewable in every implementation and correction prompt.
6. Distinguish task difficulty from task risk, ambiguity, and product-surface availability.
7. Allow the policy to evolve deliberately when model capabilities, product availability, or official guidance materially change.

This file is the canonical repository source for Dungeon Lord AI model-selection guidance.

## 2. Product-surface rule

Model availability differs among regular Chat, ChatGPT Work, Codex and the API. For Dungeon Lord implementation and correction prompts, recommend a Codex configuration unless the request specifies another surface.

The preferred models are GPT-6 Luna for routine bounded work, GPT-6.2 Sol for well-defined implementation, and GPT-6.2 Astra for ambiguity, difficult independent review or high-consequence investigation. Select the model and reasoning level actually exposed in the Work picker or Codex selector. Access depends on plan, seat, workspace permissions and client compatibility. Regular Chat has its own model choices. Use the task-class fallback in §21 if the preferred configuration is unavailable and disclose the substitution.

Codex CLI requires version `0.166.0` or later for GPT-6.2 Astra and GPT-6.2 Sol. Work exposes reasoning through its Default slider or the settings available for a selected model. Codex CLI uses its own settings. Confirm reasoning support on the selected surface. In a managed workspace, check the intended account and workspace permissions. When troubleshooting desktop availability, check for an app update. API model availability and rates apply to API use. [OpenAI Work and Codex help](https://help.openai.com/en/articles/20001275-chatgpt-work-and-codex).

## 3. Governing principles

Model choice and reasoning level are separate decisions.

Task size alone does not determine model choice.

A large but highly mechanical task may remain appropriate for Luna.

A short task may require Sol or Astra when it carries substantial architectural, save, migration, determinism, lifecycle, authority, or integration risk.

Distinguish three questions:

1. How well-defined is the task?
2. How much cross-system reasoning does it require?
3. How costly would a subtle mistake be?

Use a stronger model when ambiguity, unfamiliarity, or consequence requires stronger judgment.

Use higher reasoning when the model is appropriate but the task requires deeper analysis of a clearly defined problem.

Do not increase reasoning effort merely because a task is important. Higher effort can consume more allowance and does not guarantee a better result.

Before increasing reasoning effort, first verify that:

1. The instructions are complete and internally consistent.
2. The model has the necessary repository files and context.
3. Required connected tools, permissions, and local files are available.
4. The task was classified correctly.
5. The failure was actually a reasoning failure rather than an information, access, instruction, environment, or testing problem.

## 4. Task classifications

Every Codex implementation or correction recommendation must classify the task as one of the following.

### Routine

The solution pattern is established, scope is narrow, integration risk is low, and errors are easy to detect and correct.

Examples:

- Bounded documentation edits with a clear source and low interpretation risk.
- Small localization maintenance.
- Straightforward test corrections.
- Small configuration or data edits with an approved value.
- Mechanical changes following a clear repository precedent.
- Repetitive cleanup that does not alter architecture or persistent state.

### Standard

Meaningful implementation is required, but architecture, ownership boundaries, specifications, dependencies, and acceptance criteria are substantially known.

Examples:

- Normal bounded feature implementation.
- Extending an established gameplay system.
- Adding validation and automated tests to an established authority.
- Integrating several known components.
- Most normal bug fixes after the root cause is understood.
- Most follow-up corrections after PR review.

### Complex

The task requires substantial cross-system reasoning or is difficult, but the objective and governing constraints are still reasonably well defined.

Examples:

- A broad but specified feature spanning several established systems.
- Performance work with a known bottleneck and clear constraints.
- Complicated deterministic simulation changes with approved semantics.
- A difficult migration whose schema contract and intended mapping are already explicit.
- Integration work across several authorities where ownership is known.
- Complex UI architecture or editor integration with a locked design.
- Documentation reconciliation across approved design, runtime authorities and dated amendments.

### Ambiguous or High-Risk

The task contains meaningful uncertainty, unfamiliar architecture, unclear root cause, conflicting evidence, or unusually consequential persistent-state risk.

Examples:

- Save corruption with an unknown source.
- Migration design where the correct mapping or authority is not yet clear.
- Hard determinism defects without a root cause.
- Lifecycle failures that may originate across multiple authorities.
- Repository-wide forensic investigation.
- Architecture decisions with competing valid approaches.
- Changes where a subtle error could corrupt saves or establish a competing authority.

### Exceptional

The task remains unusually difficult after an appropriate model and reasoning level have been tried, or it has unusually broad consequences requiring the highest available reasoning settings.

Exceptional classification must include a written task-specific justification.

## 5. GPT-6 Luna

GPT-6 Luna is the preferred efficiency model for scoped, routine, and frequently repeated work.

### Luna Low

Use for highly mechanical or narrowly scoped work.

Examples:

- Small text edits.
- Simple extraction or categorization.
- One-file mechanical changes with exact instructions.
- Trivial test fixture edits with a known cause and exact expected fix.
- Formatting or repetitive cleanup.

### Luna Medium

Use for Routine work that still requires interpretation.

Examples:

- Documentation governance edits whose content is already decided.
- Small test fixes where the failure and desired behavior are known.
- Small localization or configuration changes.
- Straightforward implementation following an exact established pattern.

Luna Medium is the default for Routine tasks.

Do not push Luna to very high reasoning merely to compensate for a task that belongs on Sol or Astra.

## 6. GPT-6.2 Sol

GPT-6.2 Sol is the preferred workhorse for Dungeon Lord implementation.

### GPT-6.2 Sol Medium

Default for Standard implementation.

Examples:

- Normal bounded gameplay feature PRs.
- Extending an established system under an approved specification.
- Adding tests and validation to known authorities.
- Integrating several existing components.
- Normal UI implementation with approved behavior.
- Most focused corrections after review.

### GPT-6.2 Sol High

Use for Complex work where the task is difficult but well specified.

Examples:

- Cross-system implementation with known authority boundaries.
- Complex deterministic simulation work with locked semantics.
- Difficult performance or workload changes with a known objective.
- Large editor or UI integration with approved architecture.
- Migration implementation where the source and target contracts are already explicitly locked.

High reasoning on GPT-6.2 Sol is appropriate when more analysis is needed but the problem itself is not fundamentally ambiguous.

### GPT-6.2 Sol Extra High and higher

Do not recommend routinely.

Use only when representative evidence shows GPT-6.2 Sol High is insufficient and the task remains well defined enough that Sol, rather than Astra, is still the right model family.

The recommendation must state why High is insufficient.

## 7. GPT-6.2 Astra

GPT-6.2 Astra is the preferred highest-capability model for ambiguous, unfamiliar, or unusually consequential work.

Astra should not be the routine default for normal Dungeon Lord feature implementation because usage conservation is an explicit project requirement.

### Astra Low

Use when the primary need is stronger model capability rather than prolonged reasoning.

Examples:

- Difficult debugging with an unclear root cause.
- Unfamiliar architecture investigation.
- Cross-system regression analysis.
- Repository forensics.
- Difficult independent review where subtle cross-system integration mistakes are plausible.

Astra Low may be preferable to pushing a weaker model to excessive reasoning.

### Astra Medium

Use when both model capability and deeper reasoning are justified.

Examples:

- High-risk save or migration design with unresolved questions.
- State-corruption or lifecycle problems spanning multiple authorities.
- Architecture decisions involving conflicting constraints.
- Difficult determinism or persistence problems where failure has broad consequences.
- A Complex task where GPT-6.2 Sol High produced materially incomplete or unsafe reasoning.

### Astra High

Exceptional only.

Use when Astra Medium was insufficient or when the task has unusually broad consequences and requires especially deep analysis.

The recommendation must state why Astra Low or Medium is insufficient.

### Astra Extra High and higher

Not a normal Dungeon Lord recommendation.

Use only after concrete evidence demonstrates that lower Astra settings are inadequate.

A written justification is mandatory.

## 8. GPT-6 family fallbacks

- For GPT-6.2 Sol, use GPT-6.1 Sol at the same recommended effort when available. GPT-6 Sol remains a legacy compatibility option at the corresponding effort.
- For GPT-6.2 Astra Low or Medium, prefer GPT-6 Astra at the corresponding supported effort. If Astra is unavailable, GPT-6.2 Sol High, then GPT-6.1 Sol High, can progress bounded investigation or evidence gathering. If uncertainty still makes a high-risk design or final review unsafe, retain the independent review gate and narrow the work.
- Exceptional settings have no automatic fallback. Reassess the reason for the exception and disclose what the available configuration can cover.

Check account and client availability before assuming any fallback is selectable.

## 9. GPT-5.6 fallback models

If GPT-6 Luna is unavailable, use GPT-5.6 Luna Low for highly mechanical work or GPT-5.6 Terra Medium for routine work. Use GPT-5.6 Sol at the corresponding supported effort only when the applicable GPT-6 Sol variants are unavailable and the task remains well defined.

Disclose availability substitutions and verify that the fallback meets the task's needs.

## 10. Default escalation ladder

Use this escalation pattern unless repository evidence provides a concrete reason to deviate.

Extremely mechanical:

GPT-6 Luna, Low.

Routine:

GPT-6 Luna, Medium.

Standard implementation:

GPT-6.2 Sol, Medium.

Complex but well defined:

GPT-6.2 Sol, High.

Ambiguous, unfamiliar, or forensic:

GPT-6.2 Astra, Low.

High-risk and ambiguity remains material:

GPT-6.2 Astra, Medium.

Exceptional unresolved work:

GPT-6.2 Astra, High.

Extra High or higher, where supported:

Only with written justification and concrete evidence that the lower appropriate setting was inadequate.

Do not mechanically escalate through every reasoning level.

A stronger model at lower reasoning may be better than excessive reasoning on a weaker model.

## 11. PR-specific defaults

### Documentation-only PR

Default for routine edits: GPT-6 Luna, Medium. Use Luna Low for purely mechanical work. Use GPT-6.2 Sol High for complex reconciliation across specifications and implementation.

### Small mechanical correction PR

Default: GPT-6 Luna, Medium.

Use Luna Low for an exact one-line or similarly trivial correction.

### Normal implementation PR

Default: GPT-6.2 Sol, Medium.

This is the standard starting point for most Dungeon Lord feature PRs.

### Complex but locked implementation PR

Default: GPT-6.2 Sol, High.

Use when specifications and authority boundaries are clear but integration reasoning is substantial.

### Ambiguous architecture, unknown systemic defect or difficult independent review

Default: GPT-6.2 Astra, Low.

### High-risk save, migration, persistence, or architecture design with unresolved questions

Default: GPT-6.2 Astra, Medium.

### Focused follow-up correction after review

If the root cause and required fix are already understood:

GPT-6 Luna, Medium, for narrow corrections.

GPT-6.2 Sol, Medium, for meaningful implementation corrections.

If the review exposed a new systemic or ambiguous problem:

GPT-6.2 Astra, Low.

## 12. Phase and feature risk does not automatically determine model

Do not select Astra merely because a PR belongs to a late development phase, touches many files, or is important.

For example:

- A Phase 7 UI implementation with locked behavior and established architecture may be GPT-6.2 Sol Medium or High.
- A three-line save fix with unclear corruption risk may require GPT-6.2 Astra Low to investigate.
- A large repetitive localization update may remain Luna.
- A migration implementation with a fully locked source-to-target mapping may be GPT-6.2 Sol High, while designing that mapping may require GPT-6.2 Astra Medium.

Classify the reasoning problem, not the phase number or diff size.

## 13. Investigation and implementation may use different models

The model used to diagnose a problem and the model used to implement the resulting fix do not have to match.

Examples:

- GPT-6.2 Astra Low may identify the root cause of a cross-system regression.
- Once the root cause and exact fix are known, GPT-6.2 Sol Medium may implement it.
- A narrow mechanical follow-up may be delegated to Luna Medium.

Do not continue spending Astra usage on an implementation that has become routine after the hard reasoning is complete.

## 14. Failed implementation does not automatically justify escalation

Before escalating model or reasoning effort, determine whether the failure resulted from:

1. Incomplete instructions.
2. Missing repository context.
3. Missing files or permissions.
4. Incorrect assumptions.
5. Scope ambiguity.
6. Environment or tool failure.
7. Incorrect test setup.
8. A genuine reasoning failure.

Higher reasoning cannot compensate for missing evidence or access.

## 15. Usage-conservation rules

Usage conservation is a first-class project concern.

Use the lowest-cost model and reasoning level that is appropriate for the task.

Do not spend Astra usage on work that Luna or GPT-6.2 Sol is expected to perform reliably.

Do not repeatedly retry an underpowered configuration when evidence shows the task belongs on a stronger model.

A single appropriately selected stronger-model run may be more efficient than repeated failed lower-tier attempts.

Compare representative tasks when selecting among plausible configurations. Account for usable output, review and rework, elapsed time and actual usage on the chosen surface. Published benchmarks and rate cards do not predict any one task's result.

Before a large Work or Codex task, check current usage allowance when practical.

Do not downgrade below the task's required capability solely because allowance is low. Instead:

1. Reduce scope.
2. Split investigation from implementation.
3. Use a cheaper model for mechanical sub-work.
4. Reserve the stronger model for the portion that actually requires it.

## 16. No reactive recommendation changes

Select the recommended configuration before presenting an implementation or correction prompt.

If the user questions the recommendation, explain why it follows this policy.

Do not change the recommendation merely because the user pushes back.

A recommendation may change only when one or more of the following occurs:

1. New facts about the task are discovered.
2. The user changes the optimization goal.
3. Evidence shows the original task classification was wrong.
4. Official OpenAI model guidance materially changes.
5. Product or plan availability changes.
6. Repository evidence materially changes the assessed risk.
7. A representative attempt demonstrates that the selected model or reasoning level is inadequate.

When a recommendation changes, explicitly identify which condition caused the change.

Do not present a revised recommendation as though it had always been the obvious choice.

## 17. Required header for Codex prompts

Every Dungeon Lord implementation or correction prompt provided to the user must begin with:

Recommended Codex configuration: [model], [reasoning level]

Task classification: [Routine, Standard, Complex, Ambiguous or High-Risk, or Exceptional]

Reason: [one sentence explaining why the classification and configuration apply]

Fallback if unavailable: [fallback model and reasoning level, or "none required"]

The configuration is advice to the user. It is not an instruction to Codex to change its own model.

If the preferred model is still rolling out or availability is uncertain, say so rather than assuming the user can select it.

## 18. Recommendations for repository review and planning

ChatGPT review and planning are not automatically governed by the same model picker as Codex.

When recommending a ChatGPT configuration, distinguish:

- regular Chat;
- ChatGPT Work;
- Codex.

Do not tell the user to select a Work-only or Codex-only model in regular Chat.

For repository implementation work, Codex remains the preferred execution environment.

For long multi-step research or artifact creation, Work may be appropriate.

For conversational planning and review, regular Chat may remain appropriate unless the user explicitly wants Work or another available mode.

## 19. Relationship to repository authority

This policy governs AI model and reasoning recommendations only.

It does not override:

- AGENTS.md repository implementation guardrails.
- Locked game-design specifications.
- Cross-spec invariants.
- Save and migration rules.
- Approved planning documents.
- Merged implementation behavior.
- Test evidence.
- User instructions for a specific task.

When model-selection guidance conflicts with implementation authority, the implementation authority governs what must be built.

This policy governs which AI configuration should be recommended to perform the work.

## 20. Policy revision

This policy is intentionally stable across conversations.

Do not reinterpret it independently in each new chat.

Review the policy when:

1. A new major OpenAI model becomes available.
2. A preferred model receives a material successor, such as a new Sol revision.
3. A model named here is materially changed, renamed, restricted, or retired.
4. OpenAI publishes materially different model-selection or reasoning guidance.
5. Available reasoning controls materially change.
6. Work or Codex availability materially changes.
7. Dungeon Lord development evidence demonstrates that a default is consistently inappropriate.

When a change is warranted:

1. Verify current official OpenAI guidance.
2. Compare the change against actual Dungeon Lord development experience.
3. Propose the policy change explicitly.
4. Update this file through a focused documentation PR.
5. Update AGENTS.md only when enforcement language or the canonical path must change.
6. Update ChatGPT Project Instructions when their short enforcement copy or defaults must change.
7. Do not silently change project defaults before the repository policy is updated unless the user explicitly directs otherwise.

## 21. Current approved defaults

| Task type | Preferred model | Reasoning | Fallback |
| --- | --- | --- | --- |
| Extremely mechanical | GPT-6 Luna | Low | GPT-5.6 Luna Low |
| Routine work | GPT-6 Luna | Medium | GPT-5.6 Terra Medium |
| Standard implementation PR | GPT-6.2 Sol | Medium | GPT-6.1 Sol Medium, then GPT-6 Sol Medium or GPT-5.6 Sol Medium |
| Complex but well-defined implementation or reconciliation | GPT-6.2 Sol | High | GPT-6.1 Sol High, then GPT-6 Sol High or GPT-5.6 Sol High for a well-defined task |
| Ambiguous architecture, systemic investigation or difficult independent review | GPT-6.2 Astra | Low | GPT-6 Astra Low, then GPT-6.2 Sol High or GPT-6.1 Sol High for bounded work |
| High-risk unresolved save, migration, persistence or architecture work | GPT-6.2 Astra | Medium | GPT-6 Astra Medium, then GPT-6.2 Sol High or GPT-6.1 Sol High for bounded work under §8 |
| Exceptional unresolved work | GPT-6.2 Astra | High | No automatic fallback |
| Extra High or higher, where available | Exception only | Written justification and evidence | No automatic fallback |

## 22. Guidance basis

**Current review · 2026-11-21.** OpenAI released GPT-6.2 Astra and Sol on November 17, 2026. Its guidance positions Sol for everyday software and document work, Astra for demanding projects and Luna for frequent bounded tasks. Dungeon Lord assigns the specific reasoning defaults above using task ambiguity, cross-system depth, consequences and observed project evidence. Release benchmark and API pricing comparisons do not establish savings, shorter completion time or successful outcomes for a Dungeon Lord task. Compare representative work, including review and retries, before changing project defaults. [OpenAI model selection](https://developers.openai.com/api/docs/guides/model-selection) · [GPT-6.2 release announcement](https://openai.com/index/introducing-gpt-6-2-astra-sol/) · [Work and Codex availability](https://help.openai.com/en/articles/20001275-chatgpt-work-and-codex).

## 23. Historical guidance basis · v2.0

Version 2.0 was reviewed against official OpenAI guidance available on 2026-10-03. Its project defaults were GPT-6 Luna Medium for Routine, GPT-6.1 Sol Medium for Standard and High for Complex, and GPT-6 Astra Low for ambiguous or forensic work and Medium for unresolved high-risk work.

At that time:

- GPT-6 Astra was positioned as the state-of-the-art, highest-capability model for ambiguous problems, deep analysis, and ambitious work.
- GPT-6.1 Sol was positioned for complex work where capability, time, and cost all matter, with near-Astra performance for complex coding and professional work.
- GPT-6 Luna was positioned as the efficient model for scoped work, triage, frequent tasks, and fine-grained edits.
- GPT-6 Sol remained available, while GPT-6.1 Sol was identified as the newer preferred Sol model.
- GPT-6.1 Sol, GPT-6 Sol, and GPT-6 Luna were available in ChatGPT Work and Codex, subject to plan, workspace, and rollout availability.
- Astra could consume Work and Codex allowance faster than lower-cost models.
- OpenAI explicitly noted that Astra at Low effort can outperform Sol at High effort.
- OpenAI advised checking instructions, files, connected apps, permissions, and usage before simply increasing reasoning effort.
- OpenAI described Luna Low for fine-grained edits, GPT-6.1 Sol Medium for complex technical work, and higher Astra settings for demanding analysis.
- GPT-6.1 Sol supported Low, Medium, High, Extra High/XHigh, and Max reasoning in supported surfaces.

This section preserves the October 2026 guidance as historical context. See §§2 and 21 for current defaults and fallbacks.
