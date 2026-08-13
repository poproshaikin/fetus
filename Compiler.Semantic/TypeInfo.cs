namespace Compiler.Semantic;

public enum TypeKind
{
    Int,
    Float,
    Bool,
    String,
    Void,
    Function,
}

public abstract record TypeInfo
{
    public abstract TypeKind Kind { get; }

    public abstract string GetTypeName();
}

// Bits is 32 | 64
public sealed record IntType : TypeInfo
{
    public static readonly IntType Int32 = new(32);
    public static readonly IntType Int64 = new(64);

    public override TypeKind Kind => TypeKind.Int;
    public int Bits { get; }
    public override string GetTypeName() => Bits == 64 ? "int64" : "int";

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
    public override string GetTypeName() => Bits == 64 ? "float64" : "float";

    private FloatType(int bits)
    {
        Bits = bits;
    }
}

public sealed record BoolType : TypeInfo
{
    public static readonly BoolType Instance = new();

    public override TypeKind Kind => TypeKind.Bool;
    public override string GetTypeName() => "bool";

    private BoolType()
    {
    }
}

public sealed record StringType : TypeInfo
{
    public static readonly StringType Instance = new();

    public override TypeKind Kind => TypeKind.String;
    public override string GetTypeName() => "string";

    private StringType()
    {
    }
}

public sealed record VoidType : TypeInfo
{
    public static readonly VoidType Instance = new();

    public override TypeKind Kind => TypeKind.Void;
    public override string GetTypeName() => "void";

    private VoidType()
    {
    }
}

public sealed record FunctionType : TypeInfo
{
    public override TypeKind Kind => TypeKind.Function;
    public required TypeInfo ReturnType { get; init; }
    public required List<TypeInfo> ParamTypes { get; init; }

    public override string GetTypeName() =>
        $"func({string.Join(", ", ParamTypes.Select(p => p.GetTypeName()))}) -> {ReturnType.GetTypeName()}";

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
