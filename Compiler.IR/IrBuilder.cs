namespace Compiler.IR;

public class IrBuilder
{
    public string NewTemp() => $"t{_tempCounter++}";

    public string Add(string src1, string src2)
    {
        string dest = NewTemp();
        _instructions.Add(new IrInstruction { Op = IrOp.Add, Dest = dest, Src1 = src1, Src2 = src2 });
        return dest;
    }

    public string Mul(string src1, string src2)
    {
        string dest = NewTemp();
        _instructions.Add(new IrInstruction { Op = IrOp.Mul, Dest = dest, Src1 = src1, Src2 = src2 });
        return dest;
    }

    public void Mov(string dest, string src)
    {
        _instructions.Add(new IrInstruction { Op = IrOp.Mov, Dest = dest, Src1 = src });
    }

    public IrProgram Build() => new(_instructions);

    private readonly List<IrInstruction> _instructions = [];
    private int _tempCounter = 0;
}