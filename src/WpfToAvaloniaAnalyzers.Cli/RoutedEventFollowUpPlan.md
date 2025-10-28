# Routed Event / Dependency Property Follow-Up Plan

This plan tracks the remaining gaps discovered while running the analyzers/code-fixes against `WPF-Samples/Properties/CustomClassesWithDP` and adds a resilient, configurable fix pipeline roadmap.

---

## Phase 1 · Harden the Code-Fix Pipeline

1. [ ] **Capture Failure Telemetry**
   - Instrument each Roslyn code action to log document path, diagnostic ID, action title, exception trace, and timing (JSON output per run).
2. [ ] **Document-Level Guard Rails**
   - Apply fixes in isolated workspaces, verify `SyntaxFactory.AreEquivalent` deltas, and rerun analyzers to ensure diagnostics clear; roll back otherwise.
3. [ ] **Retry & Timeout Strategy**
   - Add single retry with cloned solution; introduce per-action cancellation (default 5s) and mark persistent failures as manual follow-ups instead of aborting the batch.
4. [ ] **Overlap & Loop Heuristics**
   - Order diagnostics outer-to-inner, cache post-fix hashes to prevent ping-pong edits, and requeue affected diagnostics only when the tree really changes.
5. [ ] **Configurable Execution Profiles**
   - Expose pipeline heuristics via a composable options object (e.g., `FixPipelineProfile`) allowing CLI flags/env overrides (`--pipeline safe|fast|diagnostic`).
6. [ ] **Pipeline Summary & CLI Surfacing**
   - Emit a final report summarizing applied/skipped fixes with reasons; ensure CLI prints actionable guidance for failures.

---

## Phase 2 · Dependency Property Conversion Parity

1. [ ] Investigate metadata-only registrations that currently skip (`ShirtColorProperty`) and unblock the code fix.
2. [ ] Support `FrameworkPropertyMetadata` default values when no callbacks are present.
3. [ ] Add regression tests covering these scenarios to lock behavior in place.

---

## Phase 3 · Metadata Callbacks & Validation

1. [ ] Translate property-changed callbacks into Avalonia class handlers and update signatures to `AvaloniaPropertyChangedEventArgs<T>`.
2. [ ] Support `CoerceValueCallback` by generating typed coercion delegates passed via `AvaloniaProperty.Register`.
3. [ ] Convert `ValidateValueCallback` lambdas to strongly typed delegates and route through the `validate:` parameter.
4. [ ] Expand tests (unit + sample) to combine defaults, coerce, validate, and class handlers.

---

## Phase 4 · Routed Event Accessors & Handlers

1. [ ] Update routed-event accessors to swap sender/event arg types to Avalonia equivalents (`Interactive`, pointer args, etc.).
2. [ ] Normalize `RaiseEvent` usages to Avalonia helpers (`Interactive.RaiseEvent`, `RoutedEventArgs` without WPF constructors).
3. [ ] Revisit handler mapping to ensure specialized WPF delegates map to the closest Avalonia event args.
4. [ ] Strengthen regression tests for accessor conversions and handler rewrites.

---

## Phase 5 · CLI Defaults & Ergonomics

1. [ ] Implement diagnostic presets (e.g., `--preset all`) to expand to `WA001`–`WA020`.
2. [ ] Improve CLI messaging for unavailable fixes, pointing users to targeted reruns or manual follow-up.
3. [ ] Allow composition of presets with the new pipeline profiles (e.g., `--preset routed-events --pipeline safe`).

---

## Phase 6 · Regression Coverage & Samples

1. [ ] Create an expected Avalonia version of `Shirt.cs` showcasing completed DP and routed-event conversions.
2. [ ] Add combined regression tests mirroring `Shirt` (metadata, validation, coercion, routed events).
3. [ ] Extend CLI integration tests to run presets with the hardened pipeline and compare against expected outputs.

---

## Phase 7 · Stretch Goals / Backlog

1. [ ] Document advanced edge cases (custom routed event strategies, bespoke event args mapping) in the README once core work lands.
2. [ ] Revisit `WpfToAvaloniaBatchService` to ensure DP and routed-event conversions compose regardless of execution order.
