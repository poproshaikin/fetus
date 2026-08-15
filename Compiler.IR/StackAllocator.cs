using Compiler.Semantic;

namespace Compiler.IR;

internal class StackAllocator
{
    public int TotalSize => _stack.Count * 8;

    public NamedStackEntry Push(string name, int size)
    {
        var entry = new NamedStackEntry(name, -TotalSize - 8, size);
        _stack.Add(entry);
        return entry;
    }

    public AnonStackEntry PushAnon(TypeInfo type)
    {
        var entry = new AnonStackEntry(type, -TotalSize - 8, type.Size);
        _stack.Add(entry);
        return entry;
    }

    private List<StackEntry> _stack = [];
}
