using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp;

/// <summary>
/// MCP Server entry point for WPF to Avalonia Analyzer.
/// </summary>
public class Program
{
    private static ILogger<Program>? _logger;
    private static readonly CancellationTokenSource _shutdownTokenSource = new();

    public static async Task<int> Main(string[] args)
    {
        // Set up console cancellation (Ctrl+C, SIGTERM)
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            // Parse command-line arguments
            var configPath = ParseConfigPath(args);

            // Build and run the host
            var host = CreateHostBuilder(args, configPath).Build();

            // Get logger after host is built
            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            _logger.LogInformation("WpfToAvalonia MCP Server starting...");

            // Set up unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Run the server
            await host.RunAsync(_shutdownTokenSource.Token);

            _logger.LogInformation("WpfToAvalonia MCP Server stopped gracefully");
            return 0;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("WpfToAvalonia MCP Server shutdown requested");
            return 0;
        }
        catch (Exception ex)
        {
            var logger = _logger ?? CreateFallbackLogger();
            logger.LogCritical(ex, "Fatal error occurred during server startup");
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            _shutdownTokenSource.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, string? configPath)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Load configuration
                var configLoader = new ConfigurationLoader();
                var config = configLoader.LoadConfigurationAsync(configPath).GetAwaiter().GetResult();
                services.AddSingleton(config);

                // Configure logging
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();

                    // Parse log level
                    if (Enum.TryParse<LogLevel>(config.Logging.MinimumLevel, out var minLevel))
                    {
                        loggingBuilder.SetMinimumLevel(minLevel);
                    }

                    // Add console logging (for stderr, not stdio)
                    loggingBuilder.AddConsole(options =>
                    {
                        options.LogToStandardErrorThreshold = LogLevel.Trace;
                    });

                    // Add file logging if enabled
                    if (config.Logging.WriteToFile)
                    {
                        var logPath = config.Logging.LogFilePath
                            ?? Path.Combine(Directory.GetCurrentDirectory(), "logs", "mcp-server.log");

                        var logDir = Path.GetDirectoryName(logPath);
                        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                        {
                            Directory.CreateDirectory(logDir);
                        }

                        // Simple file logger implementation will be added later
                        // For now, we'll use console logging only
                    }
                });

                // Add MCP Server services
                services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly(); // Automatically discovers and registers tools

                // Add application services
                services.AddSingleton<MCPWorkspaceManager>();
                services.AddSingleton<MCPAnalysisService>();
                services.AddSingleton<MCPCodeFixService>();
                services.AddSingleton<WorkspaceFileWatcher>();
            });
    }

    private static string? ParseConfigPath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger?.LogInformation("Received SIGINT (Ctrl+C), initiating graceful shutdown...");
        e.Cancel = true; // Prevent immediate termination
        _shutdownTokenSource.Cancel();
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Received SIGTERM, initiating graceful shutdown...");
        _shutdownTokenSource.Cancel();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger?.LogCritical(exception, "Unhandled exception occurred. IsTerminating: {IsTerminating}", e.IsTerminating);

        if (e.IsTerminating)
        {
            Console.Error.WriteLine($"Fatal unhandled exception: {exception?.Message}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception occurred");
        e.SetObserved(); // Prevent process termination
    }

    private static ILogger<Program> CreateFallbackLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return loggerFactory.CreateLogger<Program>();
    }
}
