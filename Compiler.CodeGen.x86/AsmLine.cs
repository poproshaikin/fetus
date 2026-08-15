namespace Compiler.CodeGen.x86;

public abstract record AsmLine;

public sealed record AsmInstr(string Mnemonic, params string[] Operands) : AsmLine;

public sealed record AsmLabel(string Name) : AsmLine;

public sealed record AsmDirective(string Text) : AsmLine;
