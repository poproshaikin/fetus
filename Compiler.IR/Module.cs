namespace Compiler.IR;

public class Module
{
    public IReadOnlyList<Instruction> Instructions => _instructions;

    internal Module(List<Instruction> instructions)
    {
        _instructions = instructions;
    }

    private readonly List<Instruction> _instructions;
}