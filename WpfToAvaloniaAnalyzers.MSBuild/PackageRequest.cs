using System;

namespace WpfToAvaloniaAnalyzers.MSBuild;

public sealed class PackageRequest
{
    public PackageRequest(string id, string? version)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Package id must be provided.", nameof(id));

        Id = id;
        Version = version;
    }

    public string Id { get; }

    public string? Version { get; }
}
