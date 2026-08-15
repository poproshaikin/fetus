using Compiler.IR;

namespace Compiler.Lowering;

internal class Scope
{
    public Scope? Parent { get; }

    public Scope(List<NamedStackEntry>? entries = null)
    {
        _entries = entries ?? [];
    }

    public void Declare(NamedStackEntry entry)
    {
        _entries.Add(entry);
    }

    public NamedStackEntry? Resolve(string name)
    {
        return _entries.FirstOrDefault(e => e.Name == name) ?? Parent?.Resolve(name);
    }

    public Scope CreateChild() => new(this);

    private Scope(Scope parent) : this()
    {
        Parent = parent;
    }

    private readonly List<NamedStackEntry> _entries;
}