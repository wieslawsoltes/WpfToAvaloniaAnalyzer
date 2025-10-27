using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace WpfToAvaloniaAnalyzers.MSBuild.Tests;

public class ProjectPropertyAlignmentTransformTests
{
    [Fact]
    public async Task NormalizesTargetFrameworkAndProperties()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectPath = Path.Combine(tempDir.FullName, "Sample.csproj");
            await File.WriteAllTextAsync(projectPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>disable</Nullable>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
</Project>");

            var context = new ProjectTransformContext(projectPath, new[] { new PackageRequest("Avalonia", "11.3.7") });
            var result = await ProjectTransformer.ApplyTransformsAsync(context);

            Assert.True(result.ProjectChanged);
            Assert.Contains("ProjectPropertyAlignment", result.AppliedTransforms);

            var contents = await File.ReadAllTextAsync(projectPath);
            Assert.Contains("<TargetFramework>net8.0</TargetFramework>", contents);
            Assert.Contains("<Nullable>enable</Nullable>", contents);
            Assert.Contains("<PublishTrimmed>false</PublishTrimmed>", contents);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
