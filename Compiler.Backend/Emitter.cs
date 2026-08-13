using System.Text;
using Compiler.IR;

namespace Compiler.Backend;

public class Emitter
{
    public string EmitProgram(IrProgram program)
    {
        StringBuilder body = new();
        StackAllocator alloc = new();

        foreach (var irInstruction in program.Instructions)
        {
            EmitInstruction(body, irInstruction, alloc);
        }
        
        StringBuilder result = new();
        EmitPrologue(result, alloc.TotalSize);
        result.Append(body);
        EmitEpilogue(result);
        return result.ToString();
    }
    
    private void EmitInstruction(StringBuilder sb, IrInstruction instr, StackAllocator alloc)
    {
        switch (instr.Op)
        {
            case IrOp.Add:
                LoadOperand(sb, instr.Src1, alloc, "rax");
                LoadOperand(sb, instr.Src2, alloc, "rbx");
                sb.AppendLine("    add %rbx, %rax");
                StoreResult(sb, instr.Dest, alloc, "rax");
                break;

            case IrOp.Sub:
                LoadOperand(sb, instr.Src1, alloc, "rax");
                LoadOperand(sb, instr.Src2, alloc, "rbx");
                sb.AppendLine("    sub %rbx, %rax");
                StoreResult(sb, instr.Dest, alloc, "rax");
                break;

            case IrOp.Mul:
                LoadOperand(sb, instr.Src1, alloc, "rax");
                LoadOperand(sb, instr.Src2, alloc, "rbx");
                sb.AppendLine("    imul %rbx, %rax");
                StoreResult(sb, instr.Dest, alloc, "rax");
                break;

            case IrOp.Mov:
                LoadOperand(sb, instr.Src1, alloc, "rax");
                StoreResult(sb, instr.Dest, alloc, "rax");
                break;

            default:
                throw new NotImplementedException($"Unhandled IrOp: {instr.Op}");
        }
        
    }

    private void LoadOperand(StringBuilder sb, string operand, StackAllocator alloc, string reg)
    {
        if (int.TryParse(operand, out int i))
        {
            sb.AppendLine($"    mov ${i}, %{reg}");
        }
        else
        {
            int offset = alloc.GetOffset(operand);
            sb.AppendLine($"    mov -{offset}(%rbp), %{reg}");
        }
    }

    private void StoreResult(StringBuilder sb, string operand, StackAllocator alloc, string reg)
    {
        int offset = alloc.GetOffset(operand);
        sb.AppendLine($"    mov %{reg}, -{offset}(%rbp)");
    }

    private void EmitPrologue(StringBuilder sb, int stackSize)
    {
        sb.AppendLine(".text");
        sb.AppendLine(".globl main");
        sb.AppendLine("main: ");
        sb.AppendLine("    push %rbp");
        sb.AppendLine("    mov %rsp, %rbp");
        sb.AppendLine($"    sub ${stackSize}, %rsp");
    }

    private void EmitEpilogue(StringBuilder sb)
    {
        sb.AppendLine("    mov $60, %rax");
        sb.AppendLine("    syscall");
    }
}
