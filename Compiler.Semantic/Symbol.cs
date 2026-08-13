namespace Compiler.Semantic;

public sealed record Symbol(string Name, TypeInfo Type, int Line, int Column);
