namespace Compiler.Semantic;

public enum TypeKind
{
    Int,
    Float,
    Bool,
    String,
    Void,
    Function,
    Ptr
}

public abstract record TypeInfo
{
    public abstract TypeKind Kind { get; }

    public abstract string TypeName { get; }
    public abstract int Size { get; }
}

public sealed record IntType : TypeInfo
{
    public static readonly IntType Int32 = new(32);

    public override TypeKind Kind => TypeKind.Int;
    public int Bits { get; }
    public override string TypeName => "int";
    public override int Size => Bits / 8;

    private IntType(int bits)
    {
        Bits = bits;
    }
}

// Bits is 32 | 64
public sealed record FloatType : TypeInfo
{
    public static readonly FloatType Float32 = new(32);
    public static readonly FloatType Float64 = new(64);

    public override TypeKind Kind => TypeKind.Float;
    public int Bits { get; }
    public override string TypeName => Bits == 64 ? "float64" : "float";
    public override int Size => Bits / 8;

    private FloatType(int bits)
    {
        Bits = bits;
    }
}

public sealed record BoolType : TypeInfo
{
    public static readonly BoolType Instance = new();

    public override TypeKind Kind => TypeKind.Bool;
    public override string TypeName => "bool";
    public override int Size => 1;

    private BoolType()
    {
    }
}

public sealed record StringType : TypeInfo
{
    public static readonly StringType Instance = new();

    public override TypeKind Kind => TypeKind.String;
    public override string TypeName => "string";
    public override int Size => 8;

    private StringType()
    {
    }
}

public sealed record VoidType : TypeInfo
{
    public static readonly VoidType Instance = new();

    public override TypeKind Kind => TypeKind.Void;
    public override string TypeName => "void";
    public override int Size => throw new InvalidOperationException("void has no size");

    private VoidType()
    {
    }
}

public sealed record PtrType : TypeInfo
{
    public static readonly PtrType Instance = new();
    
    public override TypeKind Kind => TypeKind.Ptr;

    public override string TypeName => "ptr";
    
    public override int Size => 8;
    
    private PtrType()
    {
    }
}

public sealed record FunctionType : TypeInfo
{
    public override TypeKind Kind => TypeKind.Function;
    public required TypeInfo ReturnType { get; init; }
    public required List<TypeInfo> ParamTypes { get; init; }

    public override string TypeName =>
        $"func({string.Join(", ", ParamTypes.Select(p => p.TypeName))}) -> {ReturnType.TypeName}";
    public override int Size => throw new InvalidOperationException("function type has no size");

    public bool Equals(FunctionType? other) =>
        other is not null
        && ReturnType.Equals(other.ReturnType)
        && ParamTypes.SequenceEqual(other.ParamTypes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ReturnType);
        foreach (var param in ParamTypes) hash.Add(param);
        return hash.ToHashCode();
    }
}

public static class BuiltinTypes
{
    public static readonly Dictionary<string, TypeInfo> Map = new()
    {
        ["int"] = IntType.Int32,
        ["float"] = FloatType.Float32,
        ["bool"] = BoolType.Instance,
        ["string"] = StringType.Instance,
        ["void"] = VoidType.Instance,
    };
}
