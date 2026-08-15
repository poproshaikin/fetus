namespace Compiler.IR;

public class Module
{
    public IReadOnlyList<Function> Functions => _functions;                                                                                                                                                                      
    
    internal Module(List<Function> functions) => _functions = functions;                                                                                                                                                         
    
    private readonly List<Function> _functions;   
}