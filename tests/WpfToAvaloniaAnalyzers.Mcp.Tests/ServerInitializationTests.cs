using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp.Tests;

public class ServerInitializationTests
{
    [Fact]
    public void CanCreateHostBuilder()
    {
        // Arrange & Act
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var config = new McpServerConfiguration();
                services.AddSingleton(config);
                services.AddSingleton<MCPWorkspaceManager>();
                services.AddSingleton<MCPAnalysisService>();
                services.AddSingleton<MCPCodeFixService>();
                services.AddSingleton<WorkspaceFileWatcher>();
            });

        var host = hostBuilder.Build();

        // Assert
        host.Should().NotBeNull();
    }

    [Fact]
    public void CanResolveAllRequiredServices()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var config = new McpServerConfiguration();

        serviceCollection.AddSingleton(config);
        serviceCollection.AddLogging(builder => builder.AddConsole());
        serviceCollection.AddSingleton<MCPWorkspaceManager>();
        serviceCollection.AddSingleton<MCPAnalysisService>();
        serviceCollection.AddSingleton<MCPCodeFixService>();
        serviceCollection.AddSingleton<WorkspaceFileWatcher>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act & Assert
        var workspaceManager = serviceProvider.GetService<MCPWorkspaceManager>();
        workspaceManager.Should().NotBeNull();

        var analysisService = serviceProvider.GetService<MCPAnalysisService>();
        analysisService.Should().NotBeNull();

        var codeFixService = serviceProvider.GetService<MCPCodeFixService>();
        codeFixService.Should().NotBeNull();

        var fileWatcher = serviceProvider.GetService<WorkspaceFileWatcher>();
        fileWatcher.Should().NotBeNull();
    }

    [Fact]
    public void DefaultConfigurationIsValid()
    {
        // Arrange & Act
        var config = new McpServerConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.WorkspaceCache.Should().NotBeNull();
        config.Timeouts.Should().NotBeNull();
        config.Parallelism.Should().NotBeNull();
        config.Logging.Should().NotBeNull();
        config.Security.Should().NotBeNull();

        // Check defaults
        config.WorkspaceCache.Enabled.Should().BeTrue();
        config.WorkspaceCache.MaxCachedWorkspaces.Should().BeGreaterThan(0);
        config.Timeouts.WorkspaceLoadTimeoutSeconds.Should().BeGreaterThan(0);
        config.Timeouts.AnalysisTimeoutSeconds.Should().BeGreaterThan(0);
        config.Timeouts.CodeFixTimeoutSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConfigurationLoaderCanLoadDefaultConfig()
    {
        // Arrange
        var loader = new ConfigurationLoader();

        // Act
        var config = loader.LoadConfigurationAsync(null).GetAwaiter().GetResult();

        // Assert
        config.Should().NotBeNull();
        config.WorkspaceCache.Should().NotBeNull();
        config.Timeouts.Should().NotBeNull();
    }

    [Fact]
    public void WorkspaceManagerHasCorrectInitialState()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MCPWorkspaceManager>();

        // Act
        var manager = new MCPWorkspaceManager(config, logger);

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public void AnalysisServiceCanBeCreated()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var analysisLogger = loggerFactory.CreateLogger<MCPAnalysisService>();

        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);

        // Act
        var analysisService = new MCPAnalysisService(workspaceManager, config, analysisLogger);

        // Assert
        analysisService.Should().NotBeNull();
    }

    [Fact]
    public void CodeFixServiceCanBeCreated()
    {
        // Arrange
        var config = new McpServerConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var workspaceLogger = loggerFactory.CreateLogger<MCPWorkspaceManager>();
        var codeFixLogger = loggerFactory.CreateLogger<MCPCodeFixService>();

        var workspaceManager = new MCPWorkspaceManager(config, workspaceLogger);

        // Act
        var codeFixService = new MCPCodeFixService(workspaceManager, config, codeFixLogger);

        // Assert
        codeFixService.Should().NotBeNull();
    }

    [Fact]
    public void FileWatcherCanBeCreated()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<WorkspaceFileWatcher>();

        // Act
        using var fileWatcher = new WorkspaceFileWatcher(logger);

        // Assert
        fileWatcher.Should().NotBeNull();
    }

    [Fact]
    public void FileWatcherDisposesCleanly()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<WorkspaceFileWatcher>();
        var fileWatcher = new WorkspaceFileWatcher(logger);

        // Act
        Action dispose = () => fileWatcher.Dispose();

        // Assert
        dispose.Should().NotThrow();
    }

    [Fact]
    public void AnalysisServiceCanListAnalyzers()
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
        analyzers.Should().HaveCountGreaterThan(0);
        analyzers.Should().AllSatisfy(a =>
        {
            a.Id.Should().NotBeNullOrEmpty();
            a.Title.Should().NotBeNullOrEmpty();
            a.Category.Should().NotBeNullOrEmpty();
        });
    }
}
