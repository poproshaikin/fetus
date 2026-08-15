
using Compiler.Semantic;

namespace Compiler.IR;

public class Function
{
    public string Name { get; }
    public List<Instruction> Instructions { get; }
    public TypeInfo ReturnType { get; }
    public int FrameSize { get; }

    public Function(string name, TypeInfo returnType, int frameSize, List<Instruction> instructions)
    {
        Name = name;
        ReturnType = returnType;
        Instructions = instructions;
        FrameSize = frameSize;
    }
}