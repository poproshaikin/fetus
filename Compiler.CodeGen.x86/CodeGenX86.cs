using Compiler.IR;

namespace Compiler.CodeGen.x86;

public class CodeGenX86 : CodeGen
{
    public override string EmitAssembly(Module module)
    {
        var lines = new List<AsmLine>
        {
            new AsmDirective(".text"),
            new AsmDirective(".global main"),
        };

        foreach (var function in module.Functions)
            lines.AddRange(EmitFunction(function));

        if (_stringPool.Count > 0)
        {
            lines.Add(new AsmDirective(".section .rodata"));
            foreach (var (text, label) in _stringPool)
            {
                lines.Add(new AsmLabel(label));
                lines.Add(new AsmDirective($".asciz \"{text}\""));
            }
        }

        return Print(lines);
    }

    private readonly Dictionary<string, string> _stringPool = [];

    private List<AsmLine> EmitFunction(Function function)
    {
        var lines = new List<AsmLine>
        {
            new AsmLabel(function.Name),
            new AsmInstr("push", "%rbp"),
            new AsmInstr("mov", "%rsp", "%rbp"),
            new AsmInstr("sub", $"${function.FrameSize}", "%rsp"),
        };

        foreach (var instruction in function.Instructions)
        {
            lines.AddRange(function.Name == "main" && instruction.OpCode == OpCode.Ret
                ? EmitMainEpilogue(instruction)
                : EmitInstruction(instruction));
        }

        lines.AddRange(function.Name == "main"
            ? [new AsmInstr("mov", "$60", "%rax"), new AsmInstr("syscall")]
            : [new AsmInstr("leave"), new AsmInstr("ret")]);

        return lines;
    }

    private List<AsmLine> EmitInstruction(Instruction instruction)
    {
        return instruction.OpCode switch
        {
            OpCode.Add or OpCode.Mul or OpCode.Sub => EmitArithmetic(instruction),
            OpCode.Div => EmitDiv(instruction),
            OpCode.CmpEq or OpCode.CmpNotEq or OpCode.CmpLt or OpCode.CmpGt or OpCode.CmpLtEq or OpCode.CmpGtEq => EmitSetCc(instruction),
            OpCode.Mov => EmitMov(instruction),
            OpCode.Label => [new AsmLabel(EmitLabel(instruction.Target!))],
            OpCode.Jump or OpCode.JumpIfFalse or OpCode.JumpIfTrue => EmitJump(instruction),
            OpCode.Param or OpCode.Call or OpCode.Ret => EmitCallingConvention(instruction),
            OpCode.Syscall => EmitSyscall(instruction),
            OpCode.Peek => EmitPeek(instruction),
            OpCode.Poke => EmitPoke(instruction),
            _ => throw new ArgumentOutOfRangeException(nameof(instruction.OpCode))
        };
    }

    private List<AsmLine> EmitMainEpilogue(Instruction instruction)
    {
        return
        [
            instruction.Src1 != null
                ? new AsmInstr("mov", EmitOperand(instruction.Src1), "%rdi")
                : new AsmInstr("xor", "%rdi", "%rdi"),
            new AsmInstr("mov", "$60", "%rax"),
            new AsmInstr("syscall"),
        ];
    }
    
    private List<AsmLine> EmitPeek(Instruction instruction)
    {
        // address - src1
        // offset - src2
        //
        // mov address, %rax
        // mov offset, %rbx
        // add %rbx, %rax
        // movzbl (%rax), %ebx
        // mov %rbx, dest

        return
        [
            new AsmInstr("mov", EmitOperand(instruction.Src1!), "%rax"),
            new AsmInstr("mov", EmitOperand(instruction.Src2!), "%rbx"),
            new AsmInstr("add", "%rbx", "%rax"),
            new AsmInstr("movzbl", "(%rax)", "%ebx"),
            new AsmInstr("mov", "%rbx", EmitStackEntry(instruction.Dest!)),
        ];
    }

    private List<AsmLine> EmitPoke(Instruction instruction)
    {
        // args: 0=address, 1=offset, 2=value
        //
        // mov address, %rax
        // mov offset, %rbx
        // add %rbx, %rax
        // mov value, %rbx
        // movb %bl, (%rax)

        var args = instruction.Args!;
        return
        [
            new AsmInstr("mov", EmitOperand(args[0]), "%rax"),
            new AsmInstr("mov", EmitOperand(args[1]), "%rbx"),
            new AsmInstr("add", "%rbx", "%rax"),
            new AsmInstr("mov", EmitOperand(args[2]), "%rbx"),
            new AsmInstr("movb", "%bl", "(%rax)"),
        ];
    }

    private static readonly string[] SyscallRegisters = ["%rax", "%rdi", "%rsi", "%rdx", "%r10", "%r8", "%r9"];

    private List<AsmLine> EmitSyscall(Instruction syscall)
    {
        var lines = new List<AsmLine>();

        for (var i = 0; i < syscall.Args!.Count; i++)
            lines.Add(new AsmInstr("mov", EmitOperand(syscall.Args[i]), SyscallRegisters[i]));

        lines.Add(new AsmInstr("syscall"));
        lines.Add(new AsmInstr("mov", "%rax", EmitStackEntry(syscall.Dest!)));

        return lines;
    }

    private List<AsmLine> EmitCallingConvention(Instruction instruction)
    {
        if (instruction.OpCode == OpCode.Param)
            return [new AsmInstr("push", EmitOperand(instruction.Src1!))];

        if (instruction.OpCode == OpCode.Call)
        {
            var lines = new List<AsmLine>
            {
                new AsmInstr("call", instruction.Callee!),
                new AsmInstr("add", $"${8 * instruction.ArgCount}", "%rsp"),
            };

            if (instruction.Dest != null)
                lines.Add(new AsmInstr("mov", "%rax", EmitStackEntry(instruction.Dest)));

            return lines;
        }

        if (instruction.OpCode == OpCode.Ret)
        {
            var lines = new List<AsmLine>();

            if (instruction.Src1 != null)
                lines.Add(new AsmInstr("mov", EmitOperand(instruction.Src1), "%rax"));

            lines.Add(new AsmInstr("leave"));
            lines.Add(new AsmInstr("ret"));
            return lines;
        }

        throw new ArgumentOutOfRangeException(nameof(instruction.OpCode));
    }

    private List<AsmLine> EmitJump(Instruction instruction)
    {
        if (instruction.OpCode == OpCode.Jump)
            return [new AsmInstr("jmp", EmitLabel(instruction.Target!))];

        string cond = EmitOperand(instruction.Src1!);
        string target = EmitLabel(instruction.Target!);
        string mnemonic = instruction.OpCode == OpCode.JumpIfFalse ? "je" : "jne";

        return
        [
            new AsmInstr("mov", cond, "%rax"),
            new AsmInstr("cmp", "$0", "%rax"),
            new AsmInstr(mnemonic, target),
        ];
    }

    private string EmitLabel(Label label) => $"L{label.Id}";

    private List<AsmLine> EmitMov(Instruction mov)
    {
        string src1 = EmitOperand(mov.Src1!);
        string dest = EmitStackEntry(mov.Dest!);

        return
        [
            new AsmInstr("mov", src1, "%rax"),
            new AsmInstr("mov", "%rax", dest),
        ];
    }

    private List<AsmLine> EmitSetCc(Instruction setcc)
    {
        string mnemonic = setcc.OpCode switch
        {
            OpCode.CmpEq => "sete",
            OpCode.CmpNotEq => "setne",
            OpCode.CmpLt => "setl",
            OpCode.CmpLtEq => "setle",
            OpCode.CmpGt => "setg",
            OpCode.CmpGtEq => "setge",
            _ => throw new ArgumentOutOfRangeException(nameof(setcc.OpCode))
        };

        string src1 = EmitOperand(setcc.Src1!);
        string src2 = EmitOperand(setcc.Src2!);
        string dest = EmitStackEntry(setcc.Dest!);

        return
        [
            new AsmInstr("mov", src1, "%rax"),
            new AsmInstr("mov", src2, "%rbx"),
            new AsmInstr("cmp", "%rbx", "%rax"),
            new AsmInstr(mnemonic, "%al"),
            new AsmInstr("movzbq", "%al", "%rax"),
            new AsmInstr("mov", "%rax", dest),
        ];
    }

    private List<AsmLine> EmitArithmetic(Instruction arithmetic)
    {
        var mnemonic = arithmetic.OpCode switch
        {
            OpCode.Add => "add",
            OpCode.Sub => "sub",
            OpCode.Mul => "imul",
            _ => throw new ArgumentOutOfRangeException(nameof(arithmetic.OpCode))
        };

        string src1 = EmitOperand(arithmetic.Src1!);
        string src2 = EmitOperand(arithmetic.Src2!);
        string dest = EmitStackEntry(arithmetic.Dest!);

        return
        [
            new AsmInstr("mov", src1, "%rax"),
            new AsmInstr("mov", src2, "%rbx"),
            new AsmInstr(mnemonic, "%rbx", "%rax"),
            new AsmInstr("mov", "%rax", dest),
        ];
    }

    private List<AsmLine> EmitDiv(Instruction instruction)
    {
        string src1 = EmitOperand(instruction.Src1!);
        string src2 = EmitOperand(instruction.Src2!);
        string dest = EmitStackEntry(instruction.Dest!);

        return
        [
            new AsmInstr("mov", src1, "%rax"),
            new AsmInstr("mov", src2, "%rbx"),
            new AsmInstr("cqo"),
            new AsmInstr("idiv", "%rbx"),
            new AsmInstr("mov", "%rax", dest),
        ];
    }

    private string EmitOperand(Operand operand) => operand switch
    {
        ConstOperand c => "$" + c.Value,
        EntryOperand e => EmitStackEntry(e.Entry),
        ConstStringOperand s => EmitConstString(s.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(operand))
    };

    private string EmitConstString(string value) => "$" + InternString(value);

    private string EmitStackEntry(StackEntry entry) => entry.Offset + "(%rbp)";

    private string InternString(string value)
    {
        if (_stringPool.TryGetValue(value, out var label)) 
            return label;
        
        label = $"Lstr{_stringPool.Count}";
        _stringPool[value] = label;

        return label;
    }

    private static string Print(IEnumerable<AsmLine> lines) => string.Join("\n", lines.Select(Print)) + "\n";

    private static string Print(AsmLine line) => line switch
    {
        AsmInstr { Operands.Length: 0 } i => i.Mnemonic,
        AsmInstr i => $"{i.Mnemonic} {string.Join(", ", i.Operands)}",
        AsmLabel l => $"{l.Name}:",
        AsmDirective d => d.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(line)),
    };
}