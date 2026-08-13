namespace Compiler.IR;

public enum OpCode
{
    Mov,
    Add, Sub, Mul, Div,
    Label, Jump, JumpIfFalse,
    CmpEq, CmpNotEq, CmpLt, CmpGt, CmpLtEq, CmpGtEq
}