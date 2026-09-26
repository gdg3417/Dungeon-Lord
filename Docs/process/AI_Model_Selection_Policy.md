# Dungeon Lord AI Model and Reasoning Selection Policy

Status: Active  
Version: 1.0  
Last reviewed: 2026-09-25  
Scope: ChatGPT project work, Codex Desktop, Codex CLI, ChatGPT Work, implementation prompts, correction prompts, repository reviews, debugging, test-failure investigation, and other AI-assisted development work for Dungeon Lord.

## 1. Purpose

This policy establishes stable rules for selecting OpenAI models and reasoning levels for Dungeon Lord development.

Its goals are to:

1. Use the lowest-cost configuration that is appropriate for the task.
2. Preserve higher-cost model usage for work that materially benefits from it.
3. Avoid inconsistent model recommendations between conversations.
4. Prevent model recommendations from changing merely because the user questions them.
5. Make model selection explicit and reviewable in every implementation and correction prompt.
6. Allow the policy to evolve deliberately when model capabilities or official guidance materially change.

This file is the canonical repository source for Dungeon Lord AI model-selection guidance.

## 2. Governing principles

Model choice and reasoning level are separate decisions.

A larger task does not automatically require a more capable model.

A short task may still require a more capable model when it carries substantial architectural, save, migration, determinism, lifecycle, or integration risk.

Do not use higher reasoning effort merely because a task is important. Higher reasoning uses more allowance and does not guarantee a better result.

Before increasing reasoning effort, first verify that:

1. The instructions are clear.
2. The model has the necessary repository files and context.
3. Required connected tools or permissions are available.
4. The task was classified correctly.
5. The failure was actually a reasoning failure rather than an information, access, or instruction problem.

Capability escalation should normally happen before excessive reasoning escalation.

## 3. Task classifications

Every Codex implementation or correction recommendation must classify the task as one of the following.

### Routine

The solution pattern is established, the scope is narrow, integration risk is low, and errors are easy to detect and correct.

Examples:

- Documentation edits.
- Localization-key maintenance.
- Straightforward test corrections.
- Small configuration or data changes.
- Mechanical code changes following an established repository pattern.
- Repetitive cleanup that does not alter architecture or persistent state.

### Standard

Meaningful implementation is required, but architecture, ownership boundaries, specifications, and acceptance criteria are substantially known.

Examples:

- Normal bounded feature implementation.
- Adding validation and tests to an established system.
- Extending an existing gameplay system using established patterns.
- Integrating several known components.
- Most normal bug fixes after the root cause is understood.
- Most follow-up corrections after PR review.

### Complex

The task contains meaningful uncertainty, unfamiliar behavior, cross-system interaction, persistent-state risk, difficult debugging, or significant architectural reasoning.

Examples:

- Save-schema or migration work with compatibility risk.
- Difficult lifecycle or state-corruption bugs.
- Determinism failures without a clear root cause.
- Cross-system regressions whose visible symptom may originate elsewhere.
- Unfamiliar architecture work.
- Repository-wide forensic investigation.
- Changes affecting several authoritative state owners at once.
- High-risk changes where a subtle mistake could affect saves, deterministic simulation, or future extensibility.

### Exceptional

The task remains unusually difficult after appropriate lower settings have been tried, or it has unusually broad consequences that justify the highest available reasoning settings.

Exceptional classification must include a written task-specific justification.

## 4. Model defaults

### GPT-5.6 Terra

Use Terra for Routine work where the implementation approach is already known and consequences of a mistake are limited and readily testable.

Default reasoning level: Medium.

Use Terra Low for extremely mechanical work where little interpretation is required.

Typical Terra work includes:

- Documentation-only changes.
- Straightforward test fixes.
- Localization-key maintenance.
- Simple data or configuration edits.
- Mechanical one-file or few-file changes with a clear existing precedent.
- Repetitive cleanup that does not alter architecture or persistent state.

Do not increase Terra to High simply to force Terra through a task that belongs on Sol.

### GPT-5.6 Sol

Sol is the default model for Standard Dungeon Lord feature development.

Default reasoning level: Medium.

Typical Sol work includes:

- Implementing a normal bounded feature PR.
- Extending established gameplay systems.
- Adding validation and corresponding automated tests.
- Integrating multiple existing components where ownership is already understood.
- Implementing an approved specification using established repository patterns.
- Normal bug fixes requiring moderate investigation.
- Most focused correction prompts after review.

Sol Low may be used for work that is somewhat more involved than Routine but does not need normal Standard-depth reasoning.

Sol High is not the normal escalation path for fundamentally difficult or unfamiliar work. If Sol High is being considered because the task itself is substantially more difficult, first evaluate whether Astra Low is the better fit.

### GPT-6 Astra

Use Astra for Complex work where the problem itself is difficult, unfamiliar, ambiguous, unusually consequential, or requires stronger cross-system reasoning.

Default reasoning level when Astra is warranted: Low.

Typical Astra work includes:

- Save migration work with meaningful compatibility risk.
- Difficult state corruption or lifecycle bugs.
- Cross-system failures with an unclear source.
- Hard determinism defects.
- Unfamiliar architectural problems.
- Complex regressions.
- Repository-wide forensic investigation.
- High-risk architecture changes affecting persistent state or several authoritative systems.

Astra Low is the first Astra setting.

Use Astra Medium only when at least one of the following applies:

1. Astra Low materially missed required behavior or integration risk.
2. The task remains exceptionally complex even for Astra Low.
3. Several unresolved architectural questions must be reasoned about together.
4. Repository evidence or failed implementation demonstrates that additional reasoning depth is justified.

Astra High, XHigh, Max, or equivalent highest-effort settings are exceptional. They must not be recommended routinely.

Before recommending Astra High or above, state specifically why Astra Low or Medium is insufficient.

Preserving the user's limited model allowance is an explicit project consideration.

### GPT-5.6 Luna

Luna is not a default implementation model for this repository.

It may be used for focused, repetitive, or low-risk support work such as:

- Information extraction.
- Categorization.
- Short text edits.
- Bulk formatting.
- Other mechanical tasks that do not require repository-wide judgment.

Do not recommend Luna for gameplay-state changes, saves, migrations, architecture, deterministic behavior, economic rules, or specification interpretation.

## 5. Default escalation ladder

Use this escalation pattern unless repository evidence provides a concrete reason to deviate.

Routine known work:

GPT-5.6 Terra, Medium.

Standard implementation:

GPT-5.6 Sol, Medium.

Complex, unfamiliar, or high-risk work:

GPT-6 Astra, Low.

Astra Low proves insufficient for a genuine reasoning reason:

GPT-6 Astra, Medium.

Exceptional unresolved work after appropriate lower settings:

GPT-6 Astra, High or above, with written justification.

Do not normally escalate through:

Terra Medium -> Terra High -> Sol High -> Astra Medium.

When the problem has crossed into a higher capability class, move to the appropriate model before repeatedly raising reasoning effort on the lower-capability model.

## 6. PR-specific defaults

### Documentation-only PR

Default: GPT-5.6 Terra, Low or Medium.

Use Medium when the documentation governs important process, architecture, specifications, or future implementation behavior.

### Small mechanical correction PR

Default: GPT-5.6 Terra, Medium.

Examples include a known test portability correction or a narrow change following an exact established pattern.

### Normal implementation PR

Default: GPT-5.6 Sol, Medium.

This is the normal starting point for most Dungeon Lord feature PRs.

### Difficult architecture or persistent-state PR

Default: GPT-6 Astra, Low.

Examples include difficult migrations, unclear authority interactions, complex lifecycle work, or unfamiliar cross-system architecture.

### Follow-up correction after PR review

If the root cause and required fix are already understood:

GPT-5.6 Terra, Medium, or GPT-5.6 Sol, Medium, depending on scope.

If the review exposed an unknown systemic problem:

GPT-6 Astra, Low.

## 7. Review and debugging rules

The model used for implementation and the model used for investigation do not have to match.

A feature may be implemented with Sol Medium and later require Astra Low to investigate an unexpected cross-system regression.

A failed implementation does not automatically justify raising reasoning effort.

Before escalating, determine whether the failure resulted from:

1. Incomplete instructions.
2. Missing repository context.
3. Missing files or permissions.
4. Incorrect assumptions.
5. Scope ambiguity.
6. A genuine reasoning failure.

Higher reasoning cannot compensate for missing evidence or access.

## 8. Usage-conservation rules

Use the lowest-cost configuration that is appropriate for the task, not simply the lowest-cost configuration available.

Do not spend Astra usage on work that Terra or Sol is expected to perform reliably.

Do not repeatedly spend allowance retrying an underpowered configuration when evidence shows the task belongs on a higher-capability model.

One appropriately selected Astra Low run may be preferable to repeated high-effort attempts on a less capable model.

When a task is large but conceptually routine, size alone is not a reason to select Astra.

When a task is small but involves dangerous persistent-state or migration behavior, small size is not a reason to avoid Astra.

Before a large Work or Codex task, check current usage allowance when practical so model and reasoning choices reflect the user's current limits.

## 9. No reactive recommendation changes

Select the recommended configuration before presenting the implementation or correction prompt.

If the user questions the recommendation, explain why the recommendation follows this policy.

Do not change the recommendation merely because the user pushes back.

A recommendation may change only when one or more of the following occurs:

1. New facts about the task are discovered.
2. The user changes the optimization goal, such as explicitly prioritizing usage conservation or maximum capability.
3. Evidence shows the original task classification was wrong.
4. Official OpenAI model guidance materially changes.
5. Repository evidence materially changes the assessed risk.

When a recommendation changes, explicitly identify which condition caused the change.

Do not present a revised recommendation as though it had always been the obvious choice.

## 10. Required header for Codex prompts

Every Dungeon Lord implementation or correction prompt provided to the user must begin with:

Recommended Codex configuration: [model], [reasoning level]

Task classification: [Routine, Standard, Complex, or Exceptional]

Reason: [one sentence explaining why the classification applies]

The configuration line is advice to the user. It is not an instruction to Codex to change its own model.

## 11. Relationship to repository authority

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

When model-selection guidance conflicts with implementation authority, the implementation authority governs what must be built. This policy governs which AI configuration should be recommended to perform the work.

## 12. Policy revision

This policy is intentionally stable across conversations.

Do not reinterpret it independently in each new chat.

Review the policy when:

1. A new major OpenAI model becomes available.
2. A model named here is materially changed, renamed, or retired.
3. OpenAI publishes materially different model-selection or reasoning guidance.
4. Available reasoning controls materially change.
5. Dungeon Lord development experience demonstrates that a default is consistently inappropriate.

When a change is warranted:

1. Verify current official OpenAI guidance.
2. Propose the policy change explicitly.
3. Update this file through a focused documentation PR.
4. Update AGENTS.md only if its enforcement language must change.
5. Update ChatGPT Project Instructions if their short enforcement copy must change.
6. Do not silently change project defaults before the repository policy is updated unless the user explicitly directs otherwise.

## 13. Current approved defaults

| Task type | Default model | Default reasoning |
| --- | --- | --- |
| Routine documentation or code work | GPT-5.6 Terra | Medium |
| Extremely mechanical routine work | GPT-5.6 Terra | Low |
| Standard Dungeon Lord implementation PR | GPT-5.6 Sol | Medium |
| Complex or high-risk implementation/investigation | GPT-6 Astra | Low |
| Complex work where Astra Low is demonstrably insufficient | GPT-6 Astra | Medium |
| Exceptional unresolved work | GPT-6 Astra | High or above, with written justification |

## 14. Guidance basis

This version was reviewed against current official OpenAI guidance on 2026-09-25.

At that time, OpenAI described:

- GPT-6 Astra as its most capable model for coding, research, analysis, and complex problem-solving, including difficult bugs and unfamiliar problems.
- GPT-5.6 Sol as a strong balance of capability and efficiency for coding, research, and professional work, including normal feature implementation.
- GPT-5.6 Terra as a balance of speed, capability, and cost for everyday work, including routine code changes.
- GPT-5.6 Luna as a fast, economical option for focused or repetitive work.
- Lower reasoning effort as a useful starting point when speed or allowance conservation matters.
- Medium reasoning as a balance of response time and deeper reasoning.
- Higher reasoning as useful for difficult problems, while also noting that higher effort uses more allowance and does not always produce a better result.
- Astra Low as capable of outperforming Sol High, making Astra Low or Medium an appropriate starting point when a task has exceeded Sol's normal use case.
- Clear instructions, required files, connected apps, and permissions as prerequisites that cannot be replaced by additional reasoning effort.
- Model and reasoning selection as choices that should be made before starting a large task and reviewed before increasing effort.

This guidance basis explains the policy but does not automatically modify it. Future official guidance changes should be incorporated through the revision process above.
