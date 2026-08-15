namespace Compiler.IR;

public sealed record Label(int Id);

public sealed record Instruction(
    OpCode OpCode, 
    StackEntry? Dest,
    Operand? Src1,
    Operand? Src2,
    Label? Target = null,
    string? Callee = null,
    int? ArgCount = null);