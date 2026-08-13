namespace Compiler.IR;

public enum IrOp
{
    Add,
    Sub,
    Mul,
    Div,
    Mov
};

public struct IrInstruction
{
    public IrOp Op;
    public string Dest;
    public string Src1;
    public string Src2;
}
