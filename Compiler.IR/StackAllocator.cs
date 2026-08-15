using Compiler.Semantic;

namespace Compiler.IR;

public class StackAllocator
{
    public int TotalSize => _stack.Sum(e => e.Size);

    public NamedStackEntry Push(string name, int size)
    {
        var entry = new NamedStackEntry(name, -(TotalSize + size), size);
        _stack.Add(entry);
        return entry;
    }

    public AnonStackEntry PushAnon(TypeInfo type)
    {
        var entry = new AnonStackEntry(type, -(TotalSize + type.Size), type.Size);
        _stack.Add(entry);
        return entry;
    }

    private List<StackEntry> _stack = [];
}
