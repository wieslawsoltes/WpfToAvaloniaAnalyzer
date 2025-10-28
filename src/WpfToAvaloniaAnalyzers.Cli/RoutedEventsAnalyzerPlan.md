# Routed Events Analyzer Plan

1. [ ] Research WPF and Avalonia routed event APIs, documenting differences in registration patterns, routing strategies, handler signatures, and helper services.
1.1 [ ] Review existing analyzer/code-fix infrastructure (helpers, diagnostics, CLI integration) to reuse patterns and confirm naming and ID conventions.
1.2 [ ] Produce a reference matrix mapping WPF handler types, event args, and routing strategy enums to Avalonia equivalents, noting unsupported or lossy cases.
1.3 [ ] Define the explicit conversion scenarios (static event fields, CLR wrappers, class handlers, ad-hoc handler attachment, `RaiseEvent` usage, custom args) that the new analyzers must cover.

2. [ ] Extend diagnostics infrastructure for routed events.
2.1 [x] Reserve diagnostic IDs and severities for routed-event analyzers in `DiagnosticDescriptors`.
2.2 [x] Add localized titles, messages, and categories describing each routed-event diagnostic and update shared resources.
2.3 [x] Update public documentation (README/analyzer catalog) to announce the new diagnostics and describe expected fixes.

3. [x] Implement analyzer and code fix for routed event registration fields.
3.1 [x] Detect `EventManager.RegisterRoutedEvent` invocations and extract name, routing strategy, handler type, and owner type.
3.2 [x] Generate Avalonia `RoutedEvent.Register<TSender, TArgs>` initializer, choosing the correct generic arguments from the mapping table.
3.3 [x] Rewrite field declarations to `RoutedEvent<TArgs>` including namespace adjustments and ensure static readonly semantics remain.
3.4 [x] Add analyzer and code-fix unit tests covering success, failure, mixed strategies, and custom event-args scenarios.

4. [x] Implement analyzer and code fix for routed event CLR accessors.
4.1 [x] Detect `public event RoutedEventHandler Foo` (and other handler types) patterns with `AddHandler`/`RemoveHandler` bodies.
4.2 [x] Rewrite event delegate types to Avalonia-friendly signatures (for example, `EventHandler<TArgs>` or strongly typed delegates) and update accessor bodies.
4.3 [x] Ensure supporting methods (for example, `OnFoo` handlers) get signature updates to match new delegate types.
4.4 [x] Cover accessor rewrites with analyzer/code-fix tests, including negative tests for non-routed events.

5. [x] Implement analyzer and code fix for `EventManager.RegisterClassHandler`.
5.1 [x] Detect class handler registrations and convert to `AddClassHandler` or `ClassHandlerFactory.Create` equivalents.
5.2 [x] Adjust handler delegates or lambdas to Avalonia signatures, updating parameter types and casts.
5.3 [x] Add targeted tests verifying both conversion accuracy and preservation of additional flags (such as `handledEventsToo`).

6. [x] Implement analyzer and code fix for instance-level `AddHandler`/`RemoveHandler`.
6.1 [x] Identify `AddHandler`/`RemoveHandler` calls using WPF routed-event arguments and translate to Avalonia overloads.
6.2 [x] Update delegate creation (lambda/body) to use strongly typed event args when available.
6.3 [x] Test conversions for delegates, lambdas, method groups, and corner cases (such as `handledEventsToo` overloads).

7. [x] Implement analyzer and code fix for `RaiseEvent` and routed-event args creation.
7.1 [x] Detect `RaiseEvent` usage with WPF `RoutedEventArgs` constructors and swap to Avalonia-friendly calls.
7.2 [x] Update `new RoutedEventArgs(SomeEvent)` expressions to Avalonia `RoutedEventArgs` or derived args, ensuring namespace imports.
7.3 [x] Add analyzer/code-fix tests covering both direct raises and factory methods.

8. [x] Implement analyzer and code fix for `EventManager.RegisterRoutedEvent` owner additions (`AddOwner`) and metadata interactions.
8.1 [x] Detect `AddOwner` patterns and convert to Avalonia `AddOwner` or re-registration logic as applicable.
8.2 [x] Ensure metadata or routed-event key usages migrate correctly, flagging unsupported constructs with diagnostics.
8.3 [x] Test variations with multiple owners and custom metadata.

9. [x] Update shared helpers and utilities.
9.1 [x] Introduce reusable symbol analysis helpers for routed events (owner resolution, handler-to-args inference, routing strategy mapping).
9.2 [x] Add code-generation helpers for building Avalonia syntax nodes (generic types, lambda rewrites, namespace management).
9.3 [x] Cover helper logic with focused unit tests to guard complex inference rules.

10. [ ] Integrate routed-event diagnostics with existing analyzers.
10.1 [x] Extend `WpfToAvaloniaFileAnalyzer` to surface routed-event issues as a trigger for file-level code fixes.
10.2 [x] Ensure new code fixes appear in batch scopes (document/project/solution) and respect execution-mode options in the CLI.
10.3 [x] Update `AnalyzerLoader`/`CodeFixProviderSet` expectations if new providers require ordering or capability flags.

11. [ ] Expand CLI and batch-fix coverage.
11.1 [x] Verify routed-event diagnostics light up through the CLI `--diagnostic` filter and `FixAll` mode.
11.2 [x] Add CLI integration tests (or acceptance scenarios) covering solution-level batch application with routed-event diagnostics only.
11.3 [x] Update CLI README/examples to mention routed-event support and any new command guidance.

12. [ ] Refresh samples and regression coverage.
12.1 [x] Add WPF sample snippets featuring routed-event usage to `WpfToAvaloniaAnalyzers.Sample.Wpf` and expected Avalonia outputs.
12.2 [x] Add regression tests ensuring combined dependency-property and routed-event fixes operate together without conflicts.
12.3 [x] Document known limitations or manual follow-ups required for advanced routed-event scenarios.

13. [ ] Finalize QA and release readiness.
13.1 [x] Run full analyzer/code-fix test suites plus CLI smoke tests to confirm no regressions.
13.2 [x] Update changelog or release notes with routed-event analyzer details, including diagnostic IDs.
13.3 [ ] Prepare follow-up backlog items for unsupported edge cases discovered during testing.
