namespace Compiler.IR;

public abstract record Operand;
public sealed record EntryOperand(StackEntry Entry) : Operand;
public sealed record ConstOperand(long Value) : Operand;

