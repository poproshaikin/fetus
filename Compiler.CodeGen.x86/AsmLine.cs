namespace Compiler.CodeGen.x86;

internal abstract record AsmLine;

internal sealed record AsmInstr(string Mnemonic, params string[] Operands) : AsmLine;

internal sealed record AsmLabel(string Name) : AsmLine;

internal sealed record AsmDirective(string Text) : AsmLine;
