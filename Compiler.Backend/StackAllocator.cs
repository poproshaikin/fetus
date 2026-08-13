namespace Compiler.Backend;

public class StackAllocator
{
    public int TotalSize => _offsets.Count * RegSize;

    public int GetOffset(string name)
    {
        if (!_offsets.TryGetValue(name, out var offset))
        {
            offset = _nextOffset += 8;
            _offsets[name] = offset;
        }

        return offset;
    }

    private int _nextOffset = 0;
    private Dictionary<string, int> _offsets = [];
    private const int RegSize = 8;
}
