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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DependencyPropertyCodeFixProvider)), Shared]
public class DependencyPropertyCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert to Avalonia StyledProperty";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the field declaration
        var fieldVariable = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .First();

        if (fieldVariable == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ConvertToAvaloniaPropertyAsync(context.Document, fieldVariable, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ConvertToAvaloniaPropertyAsync(
        Document document,
        VariableDeclaratorSyntax fieldVariable,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var newRoot = DependencyPropertyService.ConvertDependencyPropertyToStyledProperty(root, fieldVariable);
        return document.WithSyntaxRoot(newRoot);
    }
}
