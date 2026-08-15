namespace Compiler.Semantic;

internal sealed record Symbol(string Name, TypeInfo Type, int Line, int Column);
