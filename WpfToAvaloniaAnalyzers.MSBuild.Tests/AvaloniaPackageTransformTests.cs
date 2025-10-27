using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace WpfToAvaloniaAnalyzers.MSBuild.Tests;

public class AvaloniaPackageTransformTests
{
    [Fact]
    public async Task AddsAvaloniaPackageWhenMissing()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectPath = Path.Combine(tempDir.FullName, "Sample.csproj");
            await File.WriteAllTextAsync(projectPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

            var changed = await ProjectTransformer.EnsureAvaloniaPackageReferenceAsync(projectPath);

            Assert.True(changed);

            var contents = await File.ReadAllTextAsync(projectPath);
            Assert.Contains("PackageReference", contents);
            Assert.Contains("Include=\"Avalonia\"", contents);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task UsesDirectoryPackagesVersionWhenAvailable()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        try
        {
            var propsPath = Path.Combine(tempRoot.FullName, "Directory.Packages.props");
            await File.WriteAllTextAsync(propsPath,
@"<Project>
  <ItemGroup>
    <PackageVersion Include=""Avalonia"" Version=""11.2.0"" />
  </ItemGroup>
</Project>");

            var projectDir = Directory.CreateDirectory(Path.Combine(tempRoot.FullName, "src"));
            var projectPath = Path.Combine(projectDir.FullName, "Sample.csproj");
            await File.WriteAllTextAsync(projectPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

            await ProjectTransformer.EnsureAvaloniaPackageReferenceAsync(projectPath);

            var contents = await File.ReadAllTextAsync(projectPath);
            Assert.Contains(@"Version=""11.2.0""", contents);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}
