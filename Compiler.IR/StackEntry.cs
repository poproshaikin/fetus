using Compiler.Semantic;

namespace Compiler.IR;

public abstract record StackEntry(int Offset, int Size);
public sealed record NamedStackEntry(string Name, int Offset, int Size) : StackEntry(Offset, Size);
public sealed record AnonStackEntry(TypeInfo Type, int Offset, int Size) : StackEntry(Offset, Size);