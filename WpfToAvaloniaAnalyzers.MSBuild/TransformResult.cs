namespace WpfToAvaloniaAnalyzers.MSBuild;

public sealed class TransformResult
{
    public TransformResult(bool changed, string? message = null)
    {
        Changed = changed;
        Message = message;
    }

    public bool Changed { get; }

    public string? Message { get; }
}
