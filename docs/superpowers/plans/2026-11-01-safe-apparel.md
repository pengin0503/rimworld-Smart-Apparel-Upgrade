# Safe apparel diagnosis implementation plan

**Goal:** Explain individual comparisons and prevent unsafe automatic replacement.
**Architecture:** A pure monotonic comparison model checks quality, condition, five protection/insulation stats and vanilla score gain. A game adapter checks current policy, wearable body/DLC rules, all displaced garments, forced/locked apparel, reservations/reachability and activity immediately before a vanilla Wear job. Persist cooldown/reversal history; apply a load/settings grace period. Cross-type/material comparisons remain diagnostic-only because arbitrary equipped effects are not proven safe.
**Spec:** User request 2026-11-01 and Issue #1. Auto mode defaults OFF; no patches or forced outfit changes.

- [ ] RED tests: better quality with worse armor, wear regression, invalid stats, insufficient score, manager-name safeguards, manual/queued/busy/unsafe states, cooldown and rollback.
- [ ] GREEN pure comparison and auto-safety gate.
- [ ] Integrate candidate-specific reasons, full body/layer and requirement checks, transactional bounded scans and persisted cooldowns.
- [ ] Inspect wardrobe manager metadata/source, fail closed when manager names or Harmony patches to apparel management are detected; never invent PackageIds.
- [ ] Add bilingual UI/README, exact-DLL builds, model benchmark and package checks; self-review; PR/CI/merge development checkpoint.

Review focus: multiple displaced garments, save-loaded cooldowns, changes after scan, manual wait jobs, policy/requirement changes, unknown patch inspection failures.
Ruling: inline execution and self-review under the user's supplied scope, no approval round or subagents.

Checkpoint: pure safety model, live candidate diagnostics, persisted cooldowns, compatibility fallback, bilingual UI and packaging implemented. Model RED observed; 24 assertions and exact-DLL Release build pass. Self-review used. Actual Wear completion, Scribe history persistence and large-mod gameplay/performance remain unverified.
