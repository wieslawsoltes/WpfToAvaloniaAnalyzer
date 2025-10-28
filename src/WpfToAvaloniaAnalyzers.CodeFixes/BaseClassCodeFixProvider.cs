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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BaseClassCodeFixProvider)), Shared]
public class BaseClassCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert WPF Control base class to Avalonia Control";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA003_ConvertWpfBaseClass.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => UpdateBaseClassAsync(context.Document, root, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static Task<Document> UpdateBaseClassAsync(
        Document document,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var newRoot = BaseClassService.UpdateWpfBaseClassToAvalonia(root);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
