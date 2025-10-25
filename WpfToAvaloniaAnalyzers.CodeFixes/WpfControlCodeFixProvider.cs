using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WpfControlCodeFixProvider)), Shared]
public class WpfControlCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WPF001_UseAvaloniaControl.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Code fix implementation will be added here
        await Task.CompletedTask;
    }
}
