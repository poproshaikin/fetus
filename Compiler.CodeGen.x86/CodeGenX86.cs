using Compiler.IR;

namespace Compiler.CodeGen.x86;

public class CodeGenX86 : CodeGen
{
    public override string EmitAssembly(Module module)
    {
        string result =
            """
            .text
            .global main


            """;

        foreach (var function in module.Functions)
        {
            result += EmitFunction(function) + "\n\n";
        }

        return result;
    }

    public string EmitFunction(Function function)
    {
        string result = 
            $"""
             {function.Name}:
             push %rbp
             mov %rsp, %rbp
             sub ${function.FrameSize}, %rsp


             """;

        foreach (var instruction in function.Instructions)
        {
            var emitted = function.Name == "main" && instruction.OpCode == OpCode.Ret
                ? EmitMainEpilogue(instruction)
                : EmitInstruction(instruction);
            result += emitted + "\n";
        }

        if (function.Name == "main")
            result +=
                """

                mov $60, %rax
                syscall
                """;
        else
            result +=
                """

                leave
                ret
                """;

        return result;
    }

    public string EmitInstruction(Instruction instruction)
    {
        return instruction.OpCode switch
        {
            OpCode.Add or OpCode.Mul or OpCode.Sub => EmitArithmetic(instruction),
            OpCode.Div => EmitDiv(instruction),
            OpCode.CmpEq or OpCode.CmpNotEq or OpCode.CmpLt or OpCode.CmpGt or OpCode.CmpLtEq or OpCode.CmpGtEq => EmitSetCc(instruction),
            OpCode.Mov => EmitMov(instruction),
            OpCode.Label => EmitLabel(instruction.Target!) + ": ",
            OpCode.Jump or OpCode.JumpIfFalse or OpCode.JumpIfTrue => EmitJump(instruction),
            OpCode.Param or OpCode.Call or OpCode.Ret => EmitCallingConvention(instruction),
            _ => throw new ArgumentOutOfRangeException(nameof(instruction.OpCode))
        };
    }

    private string EmitMainEpilogue(Instruction instruction)
    {
        return $"""
                {(instruction.Src1 != null ? $"mov {EmitOperand(instruction.Src1)}, %rdi" : "xor %rdi, %rdi")}
                mov $60, %rax
                syscall
                """;
    }

    private string EmitCallingConvention(Instruction instruction)
    {
        if (instruction.OpCode == OpCode.Param)
            return "push " + EmitOperand(instruction.Src1!);

        if (instruction.OpCode == OpCode.Call)
        {
            string call = 
                $"""
                 call {instruction.Callee}
                 add ${8 * instruction.ArgCount}, %rsp
                 """;

            if (instruction.Dest != null)
                call += $"\nmov %rax, {EmitStackEntry(instruction.Dest)}";

            return call;
        }

        if (instruction.OpCode == OpCode.Ret)
        {
            return $"""
                    {(instruction.Src1 != null ? $"mov {EmitOperand(instruction.Src1)}, %rax" : "")}
                    leave
                    ret
                    """;
        }
        
        throw new ArgumentOutOfRangeException(nameof(instruction.OpCode));
    }

    private string EmitJump(Instruction instruction)
    {
        if (instruction.OpCode == OpCode.Jump)
            return "jmp " + EmitLabel(instruction.Target!);
        
        // if condition jump
        string cond = EmitOperand(instruction.Src1!);
        // mov cond, %rax
        // cmp $0, %rax
        string target = EmitLabel(instruction.Target!);
        // jne label
        string mnemonic = instruction.OpCode == OpCode.JumpIfFalse ? "je" : "jne";

        return $"""
                mov {cond}, %rax
                cmp $0, %rax
                {mnemonic} {target}
                """;
    }

    private string EmitLabel(Label label)
    {
        return $"L{label.Id}";
    }

    private string EmitMov(Instruction mov)
    {
        string src1 = EmitOperand(mov.Src1!);
        string dest = EmitStackEntry(mov.Dest!);

        return $"""
                mov {src1}, %rax
                mov %rax, {dest}
                """;
    }

    private string EmitSetCc(Instruction setcc)
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

        return $"""
                mov {src1}, %rax
                mov {src2}, %rbx
                cmp %rbx, %rax
                {mnemonic} %al
                movzbq %al, %rax
                mov %rax, {dest}
                """;
    }

    private string EmitArithmetic(Instruction arithmetic)
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

        return $"""
                mov {src1}, %rax
                mov {src2}, %rbx
                {mnemonic} %rbx, %rax
                mov %rax, {dest}
                """;
    }
    
    private string EmitDiv(Instruction instruction)
    {
        string src1 = EmitOperand(instruction.Src1!);
        string src2 = EmitOperand(instruction.Src2!);
        string dest = EmitStackEntry(instruction.Dest!);

        return $"""
                mov {src1}, %rax
                mov {src2}, %rbx
                cqo
                idiv %rbx
                mov %rax, {dest}
                """;
    }

    private string EmitOperand(Operand operand) => operand switch
    {
        ConstOperand c => "$" + c.Value,
        EntryOperand e => EmitStackEntry(e.Entry),
        _ => throw new ArgumentOutOfRangeException(nameof(operand))
    };

    private string EmitStackEntry(StackEntry entry) => entry.Offset + "(%rbp)";
}