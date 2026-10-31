# H1-F4 qualification clock ownership — 2026-10-28

PR259, `codex/h1-f4-recovery`; main base
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Recommended GPT-6.1 Sol High,
Complex; availability fallback GPT-6 Sol High. No owner UAT or merge.

## Confirmed source defect and correction

The development profiling, foreground smoke and two-floor qualification drivers
created a replacement TimeService after SaveService had bound recovery to its
original owner. Their BeginForeground call also reset the sampling owner.
Those drivers are unsuitable for schema17 qualification in that form.

Commit `1c559072c09b187081b3d67a32157f1214c564f6` retains one prebound provider
and one TimeService. The existing product/save/command-line opt-in constructs
the provider before recovery binding. Admission initially uses a real monotonic
source, preserving its actual 1.2-second Windows check. Reopen/profile providers
start stationary so startup cannot advance their loaded horizon. Controlled
acceleration freezes a real source at its current value and adds a suffix; it
never resets elapsed time, W, R, A or C. Negative/nonfinite advances reject
without changing the provider.

The three drivers now advance that original provider. A private instance-local
identity guard rejects an unbound/foreign observation owner. Editor profiling
may use only the already injected scene provider's own advance delegate. The
normal recovery coordinator must resolve boot/deferred recovery before the
driver progresses; failure/retry/uncertain authority blocks it. No ordinary
production/release clock path, serialized override, global clock or writable save
authority is introduced. The provider and drivers remain Editor/Development-only.

## Qualification at that source

| Campaign | Pass/fail/skip | Outcome |
| --- | --- | --- |
| 4d56b7a177d64b689cd9acd0f7e51e1f | 1/0/0 | Focused deterministic provider/horizon gate; all five stages and independent verification PASSED |
| 84da4d3bfa8c4afeaaf8d93ca6d47497 | 357/1/0 | FAILED preserved: export's isolated sidecar path243 exceeded existing240 limit; no limit or assertion changed |
| 22c1fdded2c64dca82d2c5e87e205c2d | 358/0/0 | Fresh shorter `cl01` namespace, prechecked233-character sidecar; all five stages and independent verification PASSED |

The focused case is included in the358. The latter also includes the111 H1-F4,
structural/canonical-write, Research and one/two-floor cohorts. Counts overlap
earlier gates and are not summed. It is not a full EditMode or performance pass.
Independent verification checks source/tools, complete native/helper/Job exit,
exact original inputs/font/settings, released reservation and protected/prior
evidence. The failed campaign had unchanged qualified inputs, complete process
evidence and an already released reservation; guarded normal restoration retained
its original bytes/verdict and140 prior evidence directories. No exceptional
recovery, process termination or ownership-rule change was needed.

The JSON companion includes exact input/archive/tool identities, original
failure/stack, full receipts and stage records. The existing240-character path
contract remains unchanged; future producer/full-suite attempts must choose a
short namespace before preparation.

## Remaining work

The Editor profiling caller still needs conversion to the approved scene
prebinding seam and fresh campaign-owned exports/captures. Its fixed historical
output paths must not execute against retained evidence. Actual scene owner
identity and corresponding PlayMode qualification are next; the one-case gate
above does not prove native Player startup, gameplay or shutdown.

Remaining historical/current fixtures, full EditMode/PlayMode, interruption and
reopen qualification, Windows execution/complete exit and matched main-versus-
H1-F4 performance remain pending. The known native shutdown issue is open.
No benchmark, responsiveness or memory conclusion is claimed.
