using Compiler.CLI;

namespace Consos;

class Program
{
    static void Main(string[] args)
    {
        var cli = new Cli(args);
        cli.Execute();
    }
}