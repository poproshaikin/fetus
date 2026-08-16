namespace Compiler.Semantic;

internal class ConversionsTable
{
    public ConversionsTable()
    {
        _table["string"] = new Conversion("ptr");
        _table["int"] = new Conversion("ptr");
        _table["ptr"] = new Conversion("string", "int");

    }

    public bool IsConvertibleTo(string convertee, string target)
    {
        return _table.TryGetValue(convertee, out var value) && value.TargetTypes.Contains(target);
    }

    private sealed record Conversion(params string[] TargetTypes);

    private readonly Dictionary<string, Conversion> _table = [];
}