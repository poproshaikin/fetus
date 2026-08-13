using Compiler.Semantic;

namespace Compiler.IR;

public class ModuleBuilder
{
    
    
    public AnonStackEntry Add(TypeInfo type, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(type);
        _instructions.Add(new Instruction(OpCode.Add, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }

    public AnonStackEntry Sub(TypeInfo type, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(type);
        _instructions.Add(new Instruction(OpCode.Sub, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }
    
    public AnonStackEntry Mul(TypeInfo type, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(type);
        _instructions.Add(new Instruction(OpCode.Mul, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }

    public AnonStackEntry Div(TypeInfo type, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(type);
        _instructions.Add(new Instruction(OpCode.Div, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }

    public void Mov(NamedStackEntry dest, StackEntry src)
    {
        _instructions.Add(new Instruction(OpCode.Mov, dest, new EntryOperand(src), null));
    }

    public AnonStackEntry CmpEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpEq, op1, op2);
    public AnonStackEntry CmpNotEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpNotEq, op1, op2);
    public AnonStackEntry CmpLt(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpLt, op1, op2);
    public AnonStackEntry CmpGt(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpGt, op1, op2);
    public AnonStackEntry CmpLtEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpLtEq, op1, op2);
    public AnonStackEntry CmpGtEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpGtEq, op1, op2);

    private AnonStackEntry Cmp(OpCode op, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(BoolType.Instance);
        _instructions.Add(new Instruction(op, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }

    public Label NewLabel() => new(_labelCounter++);

    public void MarkLabel(Label label)
    {
        _instructions.Add(new Instruction(OpCode.Label, null, null, null, label));
    }

    public void Jump(Label target)
    {
        _instructions.Add(new Instruction(OpCode.Jump, null, null, null, target));
    }

    public void JumpIfFalse(StackEntry condition, Label target)
    {
        _instructions.Add(new Instruction(OpCode.JumpIfFalse, null, new EntryOperand(condition), null, target));
    }

    public Module Build() => new(_instructions);

    private readonly StackAllocator _stack = new();
    private readonly List<Instruction> _instructions = [];
    private int _labelCounter = 0;
}