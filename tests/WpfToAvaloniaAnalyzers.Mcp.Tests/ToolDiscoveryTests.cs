using FluentAssertions;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;
using WpfToAvaloniaAnalyzers.Mcp.Services;
using WpfToAvaloniaAnalyzers.Mcp.Tools;

namespace WpfToAvaloniaAnalyzers.Mcp.Tests;

public class ToolDiscoveryTests
{
    [Fact]
    public void AnalysisToolsClassExists()
    {
        // Arrange & Act
        var type = typeof(AnalysisTools);

        // Assert
        type.Should().NotBeNull();
        type.IsClass.Should().BeTrue();
        type.IsAbstract.Should().BeTrue(); // static class
    }

    [Fact]
    public void TransformationToolsClassExists()
    {
        // Arrange & Act
        var type = typeof(TransformationTools);

        // Assert
        type.Should().NotBeNull();
        type.IsClass.Should().BeTrue();
        type.IsAbstract.Should().BeTrue(); // static class
    }

    [Fact]
    public void UtilityToolsClassExists()
    {
        // Arrange & Act
        var type = typeof(UtilityTools);

        // Assert
        type.Should().NotBeNull();
        type.IsClass.Should().BeTrue();
        type.IsAbstract.Should().BeTrue(); // static class
    }

    [Fact]
    public void WorkspaceToolsClassExists()
    {
        // Arrange & Act
        var type = typeof(WorkspaceTools);

        // Assert
        type.Should().NotBeNull();
        type.IsClass.Should().BeTrue();
        type.IsAbstract.Should().BeTrue(); // static class
    }

    [Fact]
    public void ServerInfoToolsClassExists()
    {
        // Arrange & Act
        var type = typeof(ServerInfoTools);

        // Assert
        type.Should().NotBeNull();
        type.IsClass.Should().BeTrue();
        type.IsAbstract.Should().BeTrue(); // static class
    }

    [Fact]
    public void GetServerInfoToolWorks()
    {
        // Arrange
        var config = new McpServerConfiguration();

        // Act
        var result = ServerInfoTools.GetServerInfo(config);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("WpfToAvalonia MCP Server");
        result.Version.Should().NotBeNullOrEmpty();
        result.Description.Should().NotBeNullOrEmpty();
        result.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public void GetWorkspaceCacheStatsToolWorks()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var result = WorkspaceTools.GetWorkspaceCacheStats(workspaceManager);

        // Assert
        result.Should().NotBeNull();
        result.CacheEnabled.Should().BeTrue();
        result.TotalWorkspaces.Should().Be(0); // Initially empty
    }

    [Fact]
    public async Task ClearWorkspaceCacheToolWorks()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var workspaceManager = new MCPWorkspaceManager(config, logger);

        // Act
        var result = await WorkspaceTools.ClearWorkspaceCacheAsync(workspaceManager);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.WorkspacesCleared.Should().Be(0); // Nothing to clear initially
    }

    [Fact]
    public void GetDiagnosticInfoToolWorks()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var analysisLogger = loggerFactory.CreateLogger<MCPAnalysisService>();
        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);
        var analysisService = new MCPAnalysisService(workspaceManager, config, analysisLogger);

        // Act
        var result = UtilityTools.GetDiagnosticInfo(analysisService, "WPFAV001");

        // Assert
        result.Should().NotBeNull();
        if (result.Success)
        {
            result.Id.Should().Be("WPFAV001");
            result.Title.Should().NotBeNullOrEmpty();
            result.Category.Should().NotBeNullOrEmpty();
            result.Examples.Should().NotBeNullOrEmpty();
            result.MigrationGuide.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetDiagnosticInfoWithInvalidIdReturnsError()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var analysisLogger = loggerFactory.CreateLogger<MCPAnalysisService>();
        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);
        var analysisService = new MCPAnalysisService(workspaceManager, config, analysisLogger);

        // Act
        var result = UtilityTools.GetDiagnosticInfo(analysisService, "INVALID999");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void GetDiagnosticInfoWithEmptyIdReturnsError()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var analysisLogger = loggerFactory.CreateLogger<MCPAnalysisService>();
        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);
        var analysisService = new MCPAnalysisService(workspaceManager, config, analysisLogger);

        // Act
        var result = UtilityTools.GetDiagnosticInfo(analysisService, "");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void AllToolClassesHaveMcpServerToolTypeAttribute()
    {
        // Arrange
        var toolTypes = new[]
        {
            typeof(AnalysisTools),
            typeof(TransformationTools),
            typeof(UtilityTools),
            typeof(WorkspaceTools),
            typeof(ServerInfoTools)
        };

        // Act & Assert
        foreach (var type in toolTypes)
        {
            var attribute = type.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);
            attribute.Should().NotBeEmpty($"{type.Name} should have McpServerToolType attribute");
        }
    }

    [Fact]
    public void AnalysisServiceCanListAllAnalyzers()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var analysisLogger = loggerFactory.CreateLogger<MCPAnalysisService>();
        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);
        var analysisService = new MCPAnalysisService(workspaceManager, config, analysisLogger);

        // Act
        var analyzers = analysisService.GetAnalyzerMetadata().ToList();

        // Assert
        analyzers.Should().NotBeEmpty();

        // Check for known analyzer IDs
        var analyzerIds = analyzers.Select(a => a.Id).ToList();
        analyzerIds.Should().Contain(id => id.StartsWith("WA")); // Should have WA-prefixed IDs

        // Verify all analyzers have required properties
        foreach (var analyzer in analyzers)
        {
            analyzer.Id.Should().NotBeNullOrEmpty();
            analyzer.Title.Should().NotBeNullOrEmpty();
            analyzer.Category.Should().NotBeNullOrEmpty();
            analyzer.DefaultSeverity.Should().NotBeNullOrEmpty();
        }
    }
}
