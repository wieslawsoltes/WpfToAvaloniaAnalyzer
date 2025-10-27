using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WpfToAvaloniaAnalyzers.MSBuild;

public sealed class DirectoryPackagesProps
{
    private DirectoryPackagesProps(Dictionary<string, string> packageVersions)
    {
        PackageVersions = packageVersions;
    }

    public IReadOnlyDictionary<string, string> PackageVersions { get; }

    public bool TryGetVersion(string packageId, out string version)
    {
        if (packageId is null)
            throw new ArgumentNullException(nameof(packageId));

        return PackageVersions.TryGetValue(packageId, out version!);
    }

    public static DirectoryPackagesProps Load(string path)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var document = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            if (document.Root is null)
            {
                return new DirectoryPackagesProps(versions);
            }

            foreach (var element in document.Descendants("PackageVersion"))
            {
                var idAttribute = element.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(idAttribute))
                {
                    continue;
                }

                var versionAttribute = element.Attribute("Version")?.Value;
                if (string.IsNullOrWhiteSpace(versionAttribute))
                {
                    continue;
                }

                versions[idAttribute] = versionAttribute;
            }
        }
        catch (IOException)
        {
            // Ignore IO errors when loading the props file.
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission issues.
        }

        return new DirectoryPackagesProps(versions);
    }
}
