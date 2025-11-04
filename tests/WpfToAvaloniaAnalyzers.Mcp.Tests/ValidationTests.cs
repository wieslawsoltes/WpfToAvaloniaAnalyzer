using FluentAssertions;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;
using WpfToAvaloniaAnalyzers.Mcp.Services;
using WpfToAvaloniaAnalyzers.Mcp.Tools;

namespace WpfToAvaloniaAnalyzers.Mcp.Tests;

/// <summary>
/// Tests for input validation and error handling.
/// </summary>
public class ValidationTests
{
    [Fact]
    public async Task ValidateProjectWithNullPathReturnsError()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var result = await UtilityTools.ValidateProject(workspaceManager, null!);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public async Task ValidateProjectWithEmptyPathReturnsError()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var result = await UtilityTools.ValidateProject(workspaceManager, "");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public async Task ValidateProjectWithNonExistentPathReturnsError()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var result = await UtilityTools.ValidateProject(workspaceManager, "/nonexistent/path/project.csproj");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void ConfigurationHasReasonableDefaults()
    {
        // Arrange & Act
        var config = new McpServerConfiguration();

        // Assert - Workspace Cache
        config.WorkspaceCache.Enabled.Should().BeTrue();
        config.WorkspaceCache.MaxCachedWorkspaces.Should().BeInRange(1, 100);
        config.WorkspaceCache.IdleTimeoutMinutes.Should().BeGreaterThan(0);

        // Assert - Timeouts
        config.Timeouts.WorkspaceLoadTimeoutSeconds.Should().BeInRange(10, 600);
        config.Timeouts.AnalysisTimeoutSeconds.Should().BeInRange(10, 600);
        config.Timeouts.CodeFixTimeoutSeconds.Should().BeInRange(10, 1200);
        config.Timeouts.RequestTimeoutSeconds.Should().BeInRange(10, 1200);

        // Assert - Parallelism
        config.Parallelism.MaxDegreeOfParallelism.Should().BeGreaterThan(-2); // Can be -1 for auto
        config.Parallelism.MaxConcurrentWorkspaceOperations.Should().BeGreaterThan(0);

        // Assert - Logging
        config.Logging.MinimumLevel.Should().NotBeNullOrEmpty();

        // Assert - Security
        config.Security.MaxProjectSizeMB.Should().BeGreaterThan(0);
        config.Security.MaxFileCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void WorkspaceStateEnumHasAllExpectedValues()
    {
        // Arrange & Act
        var states = Enum.GetValues<WorkspaceState>();

        // Assert
        states.Should().Contain(WorkspaceState.Loading);
        states.Should().Contain(WorkspaceState.Ready);
        states.Should().Contain(WorkspaceState.Analyzing);
        states.Should().Contain(WorkspaceState.Modifying);
        states.Should().Contain(WorkspaceState.Error);
        states.Should().Contain(WorkspaceState.Disposing);
        states.Should().Contain(WorkspaceState.Disposed);
    }

    [Fact]
    public void DiagnosticInfoHasRequiredProperties()
    {
        // Arrange
        var descriptor = new Microsoft.CodeAnalysis.DiagnosticDescriptor(
            "TEST001",
            "Test Title",
            "Test Message",
            "Test",
            Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        var diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, Microsoft.CodeAnalysis.Location.None);

        // Act
        var diagnosticInfo = DiagnosticInfo.FromDiagnostic(diagnostic, "TestProject");

        // Assert
        diagnosticInfo.Should().NotBeNull();
        diagnosticInfo.Id.Should().Be("TEST001");
        diagnosticInfo.Severity.Should().Be("Warning");
        diagnosticInfo.Message.Should().Be("Test Message");
        diagnosticInfo.ProjectName.Should().Be("TestProject");
    }

    [Fact]
    public void AnalysisProgressHasRequiredProperties()
    {
        // Arrange & Act
        var progress = new AnalysisProgress
        {
            ProjectsAnalyzed = 2,
            TotalProjects = 5,
            CurrentProject = "TestProject",
            DiagnosticsFound = 10
        };

        // Assert
        progress.ProjectsAnalyzed.Should().Be(2);
        progress.TotalProjects.Should().Be(5);
        progress.CurrentProject.Should().Be("TestProject");
        progress.DiagnosticsFound.Should().Be(10);
    }

    [Fact]
    public void AnalysisResultHasRequiredProperties()
    {
        // Arrange & Act
        var result = new AnalysisResult
        {
            Success = true,
            Diagnostics = new List<DiagnosticInfo>(),
            Summary = new AnalysisSummary(),
            ProjectInfo = new ProjectInfo()
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.ProjectInfo.Should().NotBeNull();
    }

    [Fact]
    public void CodeFixResultHasRequiredProperties()
    {
        // Arrange & Act
        var result = new CodeFixResult
        {
            Success = true,
            AppliedFixTitle = "Test Fix",
            ModifiedFiles = new List<string>(),
            AvailableFixes = new List<AvailableFixInfo>(),
            Diffs = new List<FileDiff>()
        };

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedFixTitle.Should().Be("Test Fix");
        result.ModifiedFiles.Should().NotBeNull();
        result.AvailableFixes.Should().NotBeNull();
        result.Diffs.Should().NotBeNull();
    }

    [Fact]
    public void BatchFixResultHasRequiredProperties()
    {
        // Arrange & Act
        var result = new BatchFixResult
        {
            Success = true,
            FixedDiagnostics = 10,
            ModifiedFiles = new List<string>(),
            FailedFixes = new List<FailedFixInfo>(),
            SummaryByDiagnosticId = new Dictionary<string, int>()
        };

        // Assert
        result.Success.Should().BeTrue();
        result.FixedDiagnostics.Should().Be(10);
        result.ModifiedFiles.Should().NotBeNull();
        result.FailedFixes.Should().NotBeNull();
        result.SummaryByDiagnosticId.Should().NotBeNull();
    }

    [Fact]
    public void ProjectValidationResultHasRequiredProperties()
    {
        // Arrange & Act
        var result = new ProjectValidationResult
        {
            Success = true,
            ProjectPath = "/path/to/project.csproj",
            PathExists = true,
            CanLoadWorkspace = true,
            ProjectCount = 3,
            Projects = new List<ProjectValidationDetails>(),
            Recommendations = new List<string>()
        };

        // Assert
        result.Success.Should().BeTrue();
        result.ProjectPath.Should().Be("/path/to/project.csproj");
        result.PathExists.Should().BeTrue();
        result.CanLoadWorkspace.Should().BeTrue();
        result.ProjectCount.Should().Be(3);
        result.Projects.Should().NotBeNull();
        result.Recommendations.Should().NotBeNull();
    }

    [Fact]
    public void ServerInfoHasRequiredProperties()
    {
        // Arrange
        var config = new McpServerConfiguration();

        // Act
        var serverInfo = ServerInfoTools.GetServerInfo(config);

        // Assert
        serverInfo.Name.Should().NotBeNullOrEmpty();
        serverInfo.Version.Should().NotBeNullOrEmpty();
        serverInfo.Description.Should().NotBeNullOrEmpty();
        serverInfo.Capabilities.Should().NotBeNull();
        serverInfo.Configuration.Should().NotBeNull();
    }

    [Fact]
    public void WorkspaceCacheStatsHasRequiredProperties()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var stats = WorkspaceTools.GetWorkspaceCacheStats(workspaceManager);

        // Assert
        stats.CacheEnabled.Should().BeTrue();
        stats.TotalWorkspaces.Should().BeGreaterThanOrEqualTo(0);
        stats.MaxWorkspaces.Should().BeGreaterThan(0);
        stats.WorkspacesByState.Should().NotBeNull();
    }
}
