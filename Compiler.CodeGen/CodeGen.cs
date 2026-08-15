using System.Diagnostics;
using Compiler.IR;

namespace Compiler.CodeGen;

public abstract class CodeGen
{
    public abstract string EmitAssembly(Module module);

    public string Compile(Module module, string outputPath)
    {
        var asm = EmitAssembly(module);

        var asmPath = Path.GetTempFileName() + ".s";
        File.WriteAllText(asmPath, asm);
        File.WriteAllText(outputPath + ".s", asm);

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "gcc",
            Arguments = $"-o {outputPath} {asmPath}",
            UseShellExecute = false
        });
        proc!.WaitForExit();
        
        return outputPath;
    }
}