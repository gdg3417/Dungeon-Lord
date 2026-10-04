# Dungeon Lord AI Model and Reasoning Selection Policy

Status: Active  
Version: 2.0  
Last reviewed: 2026-10-03  
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

Model availability differs between regular ChatGPT Chat, ChatGPT Work, Codex, the API, and different plans or workspace configurations.

For Dungeon Lord implementation and correction prompts, recommendations are primarily for Codex unless the prompt explicitly says otherwise.

Current Work and Codex preferred models are:

- GPT-6 Luna.
- GPT-6.1 Sol.
- GPT-6 Astra.

GPT-6 Sol remains an available fallback but is not the preferred Sol model when GPT-6.1 Sol is available.

GPT-5.6 Sol, Terra, and Luna are previous-generation fallbacks for environments where the preferred GPT-6-family model is unavailable.

Do not assume that a model available in regular Chat is selectable in Work or Codex, or vice versa.

Do not recommend GPT-6 Pro as a Codex model unless Codex explicitly exposes that exact model label. GPT-6 Pro is a regular Chat product label powered by GPT-6 Astra.

When the preferred model is unavailable on the user's product, plan, or workspace, use the documented fallback for that task class rather than inventing a new recommendation.

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

- Documentation edits.
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

## 6. GPT-6.1 Sol

GPT-6.1 Sol is the preferred workhorse for Dungeon Lord implementation.

It should replace GPT-5.6 Sol as the normal default when available.

### GPT-6.1 Sol Medium

Default for Standard implementation.

Examples:

- Normal bounded gameplay feature PRs.
- Extending an established system under an approved specification.
- Adding tests and validation to known authorities.
- Integrating several existing components.
- Normal UI implementation with approved behavior.
- Most focused corrections after review.

### GPT-6.1 Sol High

Use for Complex work where the task is difficult but well specified.

Examples:

- Cross-system implementation with known authority boundaries.
- Complex deterministic simulation work with locked semantics.
- Difficult performance or workload changes with a known objective.
- Large editor or UI integration with approved architecture.
- Migration implementation where the source and target contracts are already explicitly locked.

High reasoning on GPT-6.1 Sol is appropriate when more analysis is needed but the problem itself is not fundamentally ambiguous.

### GPT-6.1 Sol Extra High or Max

Do not recommend routinely.

Use only when representative evidence shows GPT-6.1 Sol High is insufficient and the task remains well defined enough that Sol, rather than Astra, is still the right model family.

The recommendation must state why High is insufficient.

## 7. GPT-6 Astra

GPT-6 Astra is the preferred highest-capability model for ambiguous, unfamiliar, or unusually consequential work.

Astra should not be the routine default for normal Dungeon Lord feature implementation because usage conservation is an explicit project requirement.

### Astra Low

Use when the primary need is stronger model capability rather than prolonged reasoning.

Examples:

- Difficult debugging with an unclear root cause.
- Unfamiliar architecture investigation.
- Cross-system regression analysis.
- Repository forensics.
- Review of a risky implementation where subtle integration mistakes are plausible.

Astra Low may be preferable to pushing a weaker model to excessive reasoning.

### Astra Medium

Use when both model capability and deeper reasoning are justified.

Examples:

- High-risk save or migration design with unresolved questions.
- State-corruption or lifecycle problems spanning multiple authorities.
- Architecture decisions involving conflicting constraints.
- Difficult determinism or persistence problems where failure has broad consequences.
- A Complex task where GPT-6.1 Sol High produced materially incomplete or unsafe reasoning.

### Astra High

Exceptional only.

Use when Astra Medium was insufficient or when the task has unusually broad consequences and requires especially deep analysis.

The recommendation must state why Astra Low or Medium is insufficient.

### Astra Extra High or Max

Not a normal Dungeon Lord recommendation.

Use only after concrete evidence demonstrates that lower Astra settings are inadequate.

A written justification is mandatory.

## 8. GPT-6 Sol

GPT-6 Sol is a fallback, not a normal default, when GPT-6.1 Sol is available.

Use GPT-6 Sol only when:

1. GPT-6.1 Sol is unavailable in the user's current Codex or Work environment.
2. Workspace configuration prevents GPT-6.1 Sol use.
3. Compatibility requires GPT-6 Sol.
4. Project-specific comparison demonstrates a concrete reason to prefer GPT-6 Sol.

For equivalent tasks, use the same general reasoning guidance as GPT-6.1 Sol.

Do not recommend GPT-6 Sol merely because it appears adjacent to GPT-6.1 Sol in a model picker.

## 9. GPT-5.6 fallback models

GPT-5.6 Sol, Terra, and Luna are previous-generation fallbacks.

Do not select them by default when the corresponding preferred GPT-6-family model is available.

Fallback guidance:

- GPT-6 Luna unavailable: use GPT-5.6 Luna for highly mechanical work or GPT-5.6 Terra Medium for Routine implementation.
- GPT-6.1 Sol unavailable: use GPT-6 Sol when available, otherwise GPT-5.6 Sol.
- GPT-6 Astra unavailable: use GPT-6.1 Sol High for the best available complex-work fallback, while explicitly noting the availability constraint.

Do not silently substitute a previous-generation model. State when the recommendation is a fallback caused by availability.

## 10. Default escalation ladder

Use this escalation pattern unless repository evidence provides a concrete reason to deviate.

Extremely mechanical:

GPT-6 Luna, Low.

Routine:

GPT-6 Luna, Medium.

Standard implementation:

GPT-6.1 Sol, Medium.

Complex but well defined:

GPT-6.1 Sol, High.

Ambiguous, unfamiliar, or forensic:

GPT-6 Astra, Low.

High-risk and ambiguity remains material:

GPT-6 Astra, Medium.

Exceptional unresolved work:

GPT-6 Astra, High.

Extra High or Max:

Only with written justification and concrete evidence that the lower appropriate setting was inadequate.

Do not mechanically escalate through every reasoning level.

A stronger model at lower reasoning may be better than excessive reasoning on a weaker model.

## 11. PR-specific defaults

### Documentation-only PR

Default: GPT-6 Luna, Medium.

Use Luna Low only when the text change is purely mechanical and interpretation is negligible.

### Small mechanical correction PR

Default: GPT-6 Luna, Medium.

Use Luna Low for an exact one-line or similarly trivial correction.

### Normal implementation PR

Default: GPT-6.1 Sol, Medium.

This is the standard starting point for most Dungeon Lord feature PRs.

### Complex but locked implementation PR

Default: GPT-6.1 Sol, High.

Use when specifications and authority boundaries are clear but integration reasoning is substantial.

### Difficult investigation or unknown systemic defect

Default: GPT-6 Astra, Low.

### High-risk save, migration, persistence, or architecture design with unresolved questions

Default: GPT-6 Astra, Medium.

### Focused follow-up correction after review

If the root cause and required fix are already understood:

GPT-6 Luna, Medium, for narrow corrections.

GPT-6.1 Sol, Medium, for meaningful implementation corrections.

If the review exposed a new systemic or ambiguous problem:

GPT-6 Astra, Low.

## 12. Phase and feature risk does not automatically determine model

Do not select Astra merely because a PR belongs to a late development phase, touches many files, or is important.

For example:

- A Phase 7 UI implementation with locked behavior and established architecture may be GPT-6.1 Sol Medium or High.
- A three-line save fix with unclear corruption risk may require Astra.
- A large repetitive localization update may remain Luna.
- A migration implementation with a fully locked source-to-target mapping may be GPT-6.1 Sol High, while designing that mapping may require Astra Medium.

Classify the reasoning problem, not the phase number or diff size.

## 13. Investigation and implementation may use different models

The model used to diagnose a problem and the model used to implement the resulting fix do not have to match.

Examples:

- Astra Low may identify the root cause of a cross-system regression.
- Once the root cause and exact fix are known, GPT-6.1 Sol Medium may implement it.
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

Do not spend Astra usage on work that Luna or GPT-6.1 Sol is expected to perform reliably.

Do not repeatedly retry an underpowered configuration when evidence shows the task belongs on a stronger model.

A single appropriately selected stronger-model run may be more efficient than repeated failed lower-tier attempts.

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
| Standard implementation PR | GPT-6.1 Sol | Medium | GPT-6 Sol Medium, then GPT-5.6 Sol Medium |
| Complex but well-defined implementation | GPT-6.1 Sol | High | GPT-6 Sol High, then GPT-6 Astra Low |
| Ambiguous or forensic investigation | GPT-6 Astra | Low | GPT-6.1 Sol High |
| High-risk unresolved save, migration, persistence, or architecture work | GPT-6 Astra | Medium | GPT-6.1 Sol High |
| Exceptional unresolved work | GPT-6 Astra | High | No automatic fallback |
| Extra High or Max | Exception only | Written justification required | None |

## 22. Guidance basis

This version was reviewed against official OpenAI guidance available on 2026-10-03.

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

This guidance basis explains the policy but does not automatically modify it. Future official guidance changes must be incorporated through the revision process above.
