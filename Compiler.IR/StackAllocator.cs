using Compiler.Semantic;

namespace Compiler.IR;

internal class StackAllocator
{
    public int TotalSize { get; private set; }

    public NamedStackEntry Push(string name, int size)
    {
        TotalSize += Align(size);
        var entry = new NamedStackEntry(name, -TotalSize, size);
        _stack.Add(entry);
        return entry;
    }

    public AnonStackEntry PushAnon(TypeInfo type)
    {
        TotalSize += Align(type.Size);
        var entry = new AnonStackEntry(type, -TotalSize, type.Size);
        _stack.Add(entry);
        return entry;
    }

    private static int Align(int size) => (size + 7) / 8 * 8;

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<StackEntry> _stack = [];
}
