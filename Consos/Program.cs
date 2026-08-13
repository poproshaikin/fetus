// See https://aka.ms/new-console-template for more information

using Compiler.Backend;
using Compiler.Frontend;
using Compiler.IR;
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
var analysis = new SemanticAnalyzer().Analyze(ast);

Console.WriteLine(string.Join(',', analysis.Diagnostics.Select(ex => ex.Message).ToArray()));
