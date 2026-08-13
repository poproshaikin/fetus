namespace Compiler.IR;

public class IrProgram
{
    public IReadOnlyList<IrInstruction> Instructions => _instructions;
    
    internal IrProgram(List<IrInstruction> instructions)
    {
        _instructions = instructions;
    }
    
    private readonly List<IrInstruction> _instructions;
}