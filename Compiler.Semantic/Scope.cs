namespace Compiler.Semantic;

internal class Scope
{
    public Scope? Parent { get; }

    public Scope(List<Symbol>? symbols = null)
    {
        _symbols = symbols ?? [];
    }

    public void Declare(Symbol sym)
    {
        if (_symbols.Contains(sym))
            throw new DuplicateDeclarationException(sym.Name, sym.Line, sym.Column);
        
        _symbols.Add(sym);
    }

    public Symbol? Resolve(string name)
    {
        return _symbols.FirstOrDefault(s => s.Name == name) ?? Parent?.Resolve(name);
    }

    public Scope CreateChild()
    {
        return new Scope(this);
    }
    
    private readonly List<Symbol> _symbols;

    private Scope(Scope parent) : this()
    {
        Parent = parent;
    }
}
