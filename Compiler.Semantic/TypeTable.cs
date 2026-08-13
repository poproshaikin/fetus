using Compiler.AST;

namespace Compiler.Semantic;

public class TypeTable
{
    public TypeTable()
    {
        _table["int"] = IntType.Int32;
        _table["float"] = FloatType.Float32;
        _table["string"] = StringType.Instance;
        _table["bool"] = BoolType.Instance;
        _table["void"] = VoidType.Instance;
    }
    
    public void Add(string name, TypeInfo typeInfo)
    {
        _table.Add(name, typeInfo);
    }

    public TypeInfo? Get(string name)
    {
        return _table.GetValueOrDefault(name);
    }
    
    public TypeInfo GetOrThrow(Identifier identifier)
    {
        return 
            _table.GetValueOrDefault(identifier.Name) ?? 
            throw new UndefinedTypeException(identifier.Name, identifier.Line, identifier.Column);
    }
    
    private readonly Dictionary<string, TypeInfo> _table = [];
}
