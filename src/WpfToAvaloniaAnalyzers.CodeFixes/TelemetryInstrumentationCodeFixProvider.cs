using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using WpfToAvaloniaAnalyzers.CodeFixes.Services;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TelemetryInstrumentationCodeFixProvider)), Shared]
public sealed class TelemetryInstrumentationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove telemetry instrumentation";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => RemoveTelemetryAsync(context.Document, node, cancellationToken),
                Title),
            diagnostic);
    }

    private static async Task<Document> RemoveTelemetryAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        switch (node)
        {
            case UsingDirectiveSyntax usingDirective:
                editor.RemoveNode(usingDirective);
                break;
            default:
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (invocation == null)
                    return document;

                var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
                if (statement == null)
                    return document;

                var constructor = statement.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                if (constructor?.Body != null && constructor.Body.Statements.Count == 1)
                {
                    editor.RemoveNode(constructor);
                }
                else
                {
                    editor.RemoveNode(statement);
                }

                break;
        }

        var updatedRoot = TelemetryInstrumentationService.DropUnusedTelemetryUsings(editor.GetChangedRoot());
        return document.WithSyntaxRoot(updatedRoot);
    }
}
