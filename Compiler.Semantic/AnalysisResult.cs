namespace Compiler.Semantic;

public sealed record AnalysisResult
{
    public List<SemanticException> Diagnostics { get; } = [];
}