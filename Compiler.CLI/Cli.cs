using Compiler.AST;
using Compiler.CodeGen.x86;
using Compiler.Frontend;
using Compiler.IR;
using Compiler.Lowering;
using Compiler.Semantic;

namespace Compiler.CLI;

public class Cli
{
    public readonly string[] Args;
    
    public Cli(string[] args)
    {
        Args = args;
    }

    public void Execute()
    {
        var @params = ParseParams();
        var inputFiles = SelectInputFiles(@params);
        var outputFile = @params.FirstOrDefault(p => p.Option == "-o")?.Value ?? throw new Exception("Output path not provided");

        List<AstModule> modules = [];
        foreach (var inputFile in inputFiles.Select(p => p.Value!))
        {
            var content = File.ReadAllText(inputFile);
            var lexer = new Lexer(content).Lex();
            var parsed = new Parser(lexer).Parse();
            
            modules.Add(parsed);
        }

        var totalStatements = modules.SelectMany(m => m.Body);

        var bound = new SemanticAnalyzer(
            new AstModule
            {
                Body = totalStatements.ToList(),
                Line = 0,
                Column = 0
            }).Analyze();
        
        
        Console.WriteLine($"OK: {bound.Body.Count} top-level statements");

        var lowered = new Lowerer(bound).Lower();
        
        string asm = new CodeGenX86().Compile(lowered, outputFile);
        
    }

    private IEnumerable<Param> SelectInputFiles(List<Param> @params)
    {
        return @params.Where(p => p.Option is null);
    }

    private List<Param> ParseParams()
    {
        List<Param> result = [];
        
        // in1.fx in2.fx -o out
        for (int i = 0; i < Args.Length; i++)
        {
            if (Args[i].StartsWith('-') && Args.Length > i + 1)
                result.Add(new Param(Args[i], Args[++i]));
            else if (!Args[i].StartsWith('-'))
                result.Add(new Param(null, Args[i]));
            else 
                result.Add(new Param(Args[i], null));
        }

        return result;
    }

    private static string StdLib() =>
        """
            
        """;
}