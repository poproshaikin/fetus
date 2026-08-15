using Compiler.Semantic;

namespace Compiler.IR;

public class ModuleBuilder
{
    public NamedStackEntry Push(string name, TypeInfo type) => _stack.Push(name, type.Size);

    public AnonStackEntry PushAnon(TypeInfo type) => _stack.PushAnon(type);

    public AnonStackEntry LoadConst(TypeInfo type, long value)
    {
        var dest = _stack.PushAnon(type);
        _instructions.Add(new Instruction(OpCode.Mov, dest, new ConstOperand(value), null));
        return dest;
    }

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

    public void Mov(StackEntry dest, StackEntry src)
    {
        _instructions.Add(new Instruction(OpCode.Mov, dest, new EntryOperand(src), null));
    }

    public AnonStackEntry CmpEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpEq, op1, op2);
    public AnonStackEntry CmpNotEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpNotEq, op1, op2);
    public AnonStackEntry CmpLt(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpLt, op1, op2);
    public AnonStackEntry CmpGt(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpGt, op1, op2);
    public AnonStackEntry CmpLtEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpLtEq, op1, op2);
    public AnonStackEntry CmpGtEq(StackEntry op1, StackEntry op2) => Cmp(OpCode.CmpGtEq, op1, op2);

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

    public void JumpIfTrue(StackEntry condition, Label target)
    {
        _instructions.Add(new Instruction(OpCode.JumpIfTrue, null, new EntryOperand(condition), null, target));
    }

    public void SetParam(StackEntry value)
    {
        _instructions.Add(new Instruction(OpCode.Param, null, new EntryOperand(value), null));
    }
    
    public AnonStackEntry? Call(string callee, TypeInfo returnType, int argCount)
    {
        var dest = returnType == VoidType.Instance ? null : _stack.PushAnon(returnType);
        _instructions.Add(new Instruction(OpCode.Call, dest, null, null, Callee: callee, ArgCount: argCount));
        return dest;
    }

    public void Return(StackEntry? value)
    {
        _instructions.Add(new Instruction(OpCode.Ret, null, value != null ? new EntryOperand(value) : null, null));
    }

    public NamedStackEntry PushParam(string name, TypeInfo type, int index)
    {
        return new NamedStackEntry(name, 16 + index * 8, type.Size);
    }
    
    public void EnterFunction()
    {
        _stack = new StackAllocator();
        _instructions = [];
    }

    public void EndFunction(string name, TypeInfo returnType)
    {
        _functions.Add(new Function(name, returnType, _stack.TotalSize, _instructions));
    }

    public Module Build() => new(_functions);

    private StackAllocator _stack = new();                                                                                                                                                  
    private List<Instruction> _instructions = [];                                                                                                                                                             
    private readonly List<Function> _functions = [];    
    private int _labelCounter = 0;

    private AnonStackEntry Cmp(OpCode op, StackEntry op1, StackEntry op2)
    {
        var dest = _stack.PushAnon(BoolType.Instance);
        _instructions.Add(new Instruction(op, dest, new EntryOperand(op1), new EntryOperand(op2)));
        return dest;
    }
}