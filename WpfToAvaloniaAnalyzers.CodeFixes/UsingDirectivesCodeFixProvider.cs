using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvaloniaAnalyzers.CodeFixes.Services;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingDirectivesCodeFixProvider)), Shared]
public class UsingDirectivesCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove WPF usings and add Avalonia usings";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA002_RemoveWpfUsings.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit)
            return;

        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => UpdateUsingsAsync(context.Document, compilationUnit, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static Task<Document> UpdateUsingsAsync(
        Document document,
        CompilationUnitSyntax compilationUnit,
        CancellationToken cancellationToken)
    {
        // Remove WPF usings
        var newRoot = UsingDirectivesService.RemoveWpfUsings(compilationUnit);

        // Add Avalonia usings
        newRoot = UsingDirectivesService.AddAvaloniaUsings(newRoot);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
