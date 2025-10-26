using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EffectiveValuesInitialSizeCodeFixProvider)), Shared]
public sealed class EffectiveValuesInitialSizeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove EffectiveValuesInitialSize override";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA014_RemoveEffectiveValuesInitialSize.Id);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var propertyDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (propertyDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => RemovePropertyAsync(context.Document, propertyDeclaration, ct),
                Title),
            diagnostic);
    }

    private static async Task<Document> RemovePropertyAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var updatedRoot = root.RemoveNode(propertyDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        if (updatedRoot == null)
            return document;

        return document.WithSyntaxRoot(updatedRoot);
    }
}
