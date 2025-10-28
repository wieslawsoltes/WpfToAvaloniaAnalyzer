using System.Diagnostics;
using System.Text;
using WpfToAvaloniaAnalyzers.Cli;

namespace WpfToAvaloniaAnalyzers.Cli.Tests;

public class CliIntegrationTests
{
    [Fact]
    public void FixAllMode_AppliesRoutedEventDiagnostics()
    {
        var repoRoot = FindRepositoryRoot();
        var sampleRoot = Path.Combine(repoRoot, "samples", "WpfToAvaloniaAnalyzers.Sample.Wpf");
        if (!Directory.Exists(sampleRoot))
        {
            throw new InvalidOperationException($"Could not locate sample project at '{sampleRoot}'.");
        }

        var projectPath = Path.Combine(sampleRoot, "WpfToAvaloniaAnalyzers.Sample.Wpf.csproj");
        var sourcePath = Path.Combine(sampleRoot, "Samples", "RoutedEventSamples.cs");

        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);

        var originalContent = File.Exists(sourcePath)
            ? File.ReadAllText(sourcePath)
            : SampleSource;

        File.WriteAllText(sourcePath, SampleSource, Utf8NoBom);

        try
        {
            RunDotnet(repoRoot, "restore", projectPath);
            var cliProject = Path.Combine(repoRoot, "src", "WpfToAvaloniaAnalyzers.Cli", "WpfToAvaloniaAnalyzers.Cli.csproj");
            RunDotnet(repoRoot, "build", cliProject);

            var output = RunDotnet(
                repoRoot,
                "run",
                "--no-build",
                "--project", cliProject,
                "--",
                "--path", projectPath,
                "--diagnostic", "WA015",
                "--diagnostic", "WA020",
                "--mode", "fixall");

            Assert.Contains("Fixed 2 diagnostics", output, StringComparison.Ordinal);

            var contents = File.ReadAllText(sourcePath);
            Assert.Contains("global::Avalonia.Interactivity.RoutedEvent<global::Avalonia.Interactivity.RoutedEventArgs> FooEvent", contents);
            Assert.Contains("global::Avalonia.Interactivity.RoutedEvent.Register<RoutedEventSamples, global::Avalonia.Interactivity.RoutedEventArgs>(\"Foo\"", contents);
            Assert.Contains("global::Avalonia.Interactivity.RoutedEvent BarEvent = Button.ClickEvent;", contents);
            Assert.DoesNotContain("AddOwner", contents, StringComparison.Ordinal);
        }
        finally
        {
            File.WriteAllText(sourcePath, originalContent, Utf8NoBom);
        }
    }

    private static string RunDotnet(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        return stdout + stderr;
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "WpfToAvaloniaAnalyzers.sln")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private const string SampleSource = """
using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

public class RoutedEventSamples : Control
{
    public static readonly RoutedEvent FooEvent = EventManager.RegisterRoutedEvent(
        "Foo",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RoutedEventSamples));

    public static readonly RoutedEvent BarEvent = Button.ClickEvent.AddOwner(typeof(RoutedEventSamples));
}
""";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
