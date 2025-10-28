using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WpfToAvaloniaAnalyzers.Cli;

namespace WpfToAvaloniaAnalyzers.Cli.Tests;

public class ToolOptionsTests
{
    [Fact]
    public void DocumentScopeWithFixAllModeResolvesRoutedEventDiagnostics()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var workspacePath = Path.Combine(tempRoot, "Test.sln");
            File.WriteAllText(workspacePath, string.Empty);

            var documentPath = Path.Combine(tempRoot, "Test.cs");
            File.WriteAllText(documentPath, "class C {}");

            using var workspace = new AdhocWorkspace();

            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestAssembly",
                LanguageNames.CSharp,
                filePath: Path.Combine(tempRoot, "TestProject.csproj"),
                metadataReferences: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                });

            solution = solution.AddProject(projectInfo);

            var documentInfo = DocumentInfo.Create(
                documentId,
                "Test.cs",
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class C {}"), VersionStamp.Create())),
                filePath: documentPath);

            solution = solution.AddDocument(documentInfo);

            workspace.TryApplyChanges(solution);

            Assert.True(
                ToolOptions.TryCreate(workspacePath, "document", null, documentPath, new[] { "WA020" }, null, "fixall", out var toolOptions, out var createError));
            Assert.Null(createError);

            Assert.True(toolOptions.TryResolve(workspace.CurrentSolution, openedProject: null, out var resolved, out var resolveError));
            Assert.Null(resolveError);

            Assert.Equal(FixScope.Document, resolved.Scope);
            Assert.Equal(documentId, resolved.DocumentId);
            Assert.Contains("WA020", resolved.RequestedDiagnosticIds);
            Assert.Equal(ExecutionMode.FixAll, resolved.Mode);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public void SolutionScopeDefaultsToAllProjectsWithParallelMode()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var workspacePath = Path.Combine(tempRoot, "Test.sln");
            File.WriteAllText(workspacePath, string.Empty);

            using var workspace = new AdhocWorkspace();

            var solution = workspace.CurrentSolution;

            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            solution = solution.AddProject(ProjectInfo.Create(
                projectId1,
                VersionStamp.Create(),
                "ProjectOne",
                "ProjectOne",
                LanguageNames.CSharp,
                filePath: Path.Combine(tempRoot, "ProjectOne.csproj"),
                metadataReferences: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                }));

            solution = solution.AddProject(ProjectInfo.Create(
                projectId2,
                VersionStamp.Create(),
                "ProjectTwo",
                "ProjectTwo",
                LanguageNames.CSharp,
                filePath: Path.Combine(tempRoot, "ProjectTwo.csproj"),
                metadataReferences: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                }));

            workspace.TryApplyChanges(solution);

            Assert.True(
                ToolOptions.TryCreate(workspacePath, "solution", null, documentPath: null, new[] { "WA020" }, null, "parallel", out var toolOptions, out var createError));
            Assert.Null(createError);

            Assert.True(toolOptions.TryResolve(workspace.CurrentSolution, openedProject: null, out var resolved, out var resolveError));
            Assert.Null(resolveError);

            Assert.Equal(FixScope.Solution, resolved.Scope);
            Assert.Equal(2, resolved.ProjectIds.Length);
            Assert.Contains("WA020", resolved.RequestedDiagnosticIds);
            Assert.Equal(ExecutionMode.Parallel, resolved.Mode);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "wpf-to-avalonia-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Allow cleanup failures in tests; the OS will clean temporary files eventually.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
