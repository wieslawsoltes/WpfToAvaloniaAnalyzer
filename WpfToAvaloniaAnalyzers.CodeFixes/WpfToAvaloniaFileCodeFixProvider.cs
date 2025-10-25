using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using WpfToAvaloniaAnalyzers.CodeFixes.Services;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WpfToAvaloniaFileCodeFixProvider)), Shared]
public sealed class WpfToAvaloniaFileCodeFixProvider : CodeFixProvider
{
    private const string Title = "Apply all WPF to Avalonia conversions";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA007_ApplyAllAnalyzers.Id);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic == null)
            return Task.CompletedTask;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => ApplyAllFixesAsync(context.Document, cancellationToken),
                Title),
            diagnostic);

        return Task.CompletedTask;
    }

    private static Task<Document> ApplyAllFixesAsync(Document document, CancellationToken cancellationToken) =>
        WpfToAvaloniaBatchService.ApplyAllFixesAsync(document, cancellationToken);
}
