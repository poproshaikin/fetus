namespace Compiler.IR;

public enum OpCode
{
    Mov,
    Add, Sub, Mul, Div,
    Label, Jump, JumpIfFalse, JumpIfTrue,
    CmpEq, CmpNotEq, CmpLt, CmpGt, CmpLtEq, CmpGtEq,
    Call, Syscall, Param, Ret,
    Peek, Poke
}