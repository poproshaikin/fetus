// See https://aka.ms/new-console-template for more information

using Compiler.CodeGen.x86;
using Compiler.Frontend;
using Compiler.IR;
using Compiler.Lowering;
using Compiler.Semantic;

// var b = new IrBuilder();
// var t1 = b.Mul("3", "2");
// var t2 = b.Add("5", t1);
// b.Mov("x", t2);
//
// var program = b.Build();
// var code = new Emitter().EmitProgram(program);
//
// foreach (var programInstruction in program.Instructions)
// {
//     Console.WriteLine(programInstruction.Op + " " + programInstruction.Dest + " " + programInstruction.Src1 + " " + programInstruction.Src2);
// }
//
// Console.WriteLine(code);
//
// File.WriteAllText("out.s", code);

var code = File.ReadAllText("in.fx");
var lexed = new Lexer(code).Lex();
var ast = new Parser(lexed).Parse();
var bound = new SemanticAnalyzer(ast).Analyze();

Console.WriteLine($"OK: {bound.Body.Count} top-level statements");

var lowered = new Lowerer(bound).Lower();

foreach (var loweredFunction in lowered.Functions)
{
    Console.WriteLine($"{loweredFunction.ReturnType.TypeName} {loweredFunction.Name}(frame={loweredFunction.FrameSize}):");
    foreach (var instruction in loweredFunction.Instructions)
    {
        var line = FormatInstruction(instruction);
        Console.WriteLine(instruction.OpCode == OpCode.Label ? $"  {line}" : $"      {line}");
    }
    Console.WriteLine();
}

string asm = new CodeGenX86().Compile(lowered, "out");

Console.WriteLine(asm);



string FormatInstruction(Instruction i)
{
    if (i.OpCode == OpCode.Label)
        return $"{FormatLabel(i.Target)}:";

    var parts = new List<string>();
    if (i.Dest != null) parts.Add(FormatEntry(i.Dest));
    if (i.Src1 != null) parts.Add(FormatOperand(i.Src1));
    if (i.Src2 != null) parts.Add(FormatOperand(i.Src2));
    if (i.Callee != null) parts.Add(i.Callee);

    var line = $"{i.OpCode,-12} {string.Join(", ", parts)}";
    if (i.Target != null)
        line += $" -> {FormatLabel(i.Target)}";

    return line;
}

string FormatEntry(StackEntry entry) => entry switch
{
    NamedStackEntry n => n.Name,
    AnonStackEntry a => $"{a.Offset}",
    _ => entry.ToString()!,
};

string FormatOperand(Operand op) => op switch
{
    EntryOperand e => FormatEntry(e.Entry),
    ConstOperand c => c.Value.ToString(),
    _ => op.ToString()!,
};

string FormatLabel(Label? label) => label == null ? "?" : $"L{label.Id}";
