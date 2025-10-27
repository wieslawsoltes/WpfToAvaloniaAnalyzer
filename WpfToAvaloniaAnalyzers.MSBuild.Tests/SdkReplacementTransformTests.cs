using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace WpfToAvaloniaAnalyzers.MSBuild.Tests;

public class SdkReplacementTransformTests
{
    [Fact]
    public async Task ConvertsWindowsDesktopSdkToNetSdk()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectPath = Path.Combine(tempDir.FullName, "Sample.csproj");
            await File.WriteAllTextAsync(projectPath,
@"<Project Sdk=""Microsoft.NET.Sdk.WindowsDesktop"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>");

            var context = new ProjectTransformContext(
                projectPath,
                new[] { new PackageRequest("Avalonia", "11.3.7") });

            var result = await ProjectTransformer.ApplyTransformsAsync(context);

            Assert.True(result.ProjectChanged);
            Assert.Contains("SdkReplacement", result.AppliedTransforms);

            var contents = await File.ReadAllTextAsync(projectPath);
            Assert.Contains(@"Project Sdk=""Microsoft.NET.Sdk""", contents);
            Assert.DoesNotContain("UseWPF", contents);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
