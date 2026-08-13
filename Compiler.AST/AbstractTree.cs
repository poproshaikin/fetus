using System.Reflection;

namespace Compiler.AST;

public enum NodeKind
{
    Program,
    VarDecl,
    FuncDecl,
    Block,
    If,
    While,
    ExprStatement,
    Assignment,
    Binary,
    Call,
    Identifier,
    Literal,
    ParamDecl,
    Return
}

public abstract record Node
{
    public abstract NodeKind Kind { get; }
    public required int Line { get; init; }
    public required int Column { get; init; }
}

public abstract record Statement : Node;

public abstract record Expression : Node;

public enum BinaryOperator
{
    Plus,
    Minus,
    Star,
    Slash,
    Eq,
    NotEq,
    Lt,
    Gt,
    LtEq,
    GtEq,
    And,
    Or,
}

public sealed record Program : Node
{
    public override NodeKind Kind => NodeKind.Program;
    public required List<Statement> Body { get; init; }
}

public sealed record VarDecl : Statement
{
    public override NodeKind Kind => NodeKind.VarDecl;
    public required Identifier TypeName { get; init; }
    public required Identifier Identifier { get; init; }
    public required Expression? Init { get; init; }
}

public sealed record FuncDecl : Statement
{
    public override NodeKind Kind => NodeKind.FuncDecl;
    public required Identifier Identifier { get; init; }
    public required Identifier ReturnType { get; init; }
    public required List<ParamDecl> Params { get; init; }
    public required Block Body { get; init; }
}

public sealed record ParamDecl : Statement
{
    public override NodeKind Kind => NodeKind.ParamDecl;
    public required Identifier Identifier { get; init; }
    public required Identifier Type { get; init; }
}

public sealed record Block : Statement
{
    public override NodeKind Kind => NodeKind.Block;
    public required List<Statement> Body { get; init; }
}

public sealed record Return : Statement
{
    public override NodeKind Kind => NodeKind.Return;
    public required Expression? Value { get; init; }
}

// Else is Block | If | null
public sealed record If : Statement
{
    public override NodeKind Kind => NodeKind.If;
    public required Expression Condition { get; init; }
    public required Block Then { get; init; }
    public required Node? Else { get; init; }
}

public sealed record While : Statement
{
    public override NodeKind Kind => NodeKind.While;
    public required Expression Condition { get; init; }
    public required Block Body { get; init; }
}

public sealed record ExprStatement : Statement
{
    public override NodeKind Kind => NodeKind.ExprStatement;
    public required Expression Expression { get; init; }
}

public sealed record Assignment : Expression
{
    public override NodeKind Kind => NodeKind.Assignment;
    public required Identifier Target { get; init; }
    public required Expression Value { get; init; }
}

public sealed record Binary : Expression
{
    public override NodeKind Kind => NodeKind.Binary;
    public required BinaryOperator Operator { get; init; }
    public required Expression Left { get; init; }
    public required Expression Right { get; init; }
}

public sealed record Call : Expression
{
    public override NodeKind Kind => NodeKind.Call;
    public required Expression Callee { get; init; }
    public required List<Expression> Args { get; init; }
}

public sealed record Identifier : Expression
{
    public override NodeKind Kind => NodeKind.Identifier;
    public required string Name { get; init; }
}

// Value is string | number | boolean | null
public sealed record Literal : Expression
{
    public override NodeKind Kind => NodeKind.Literal;
    public required object? Value { get; init; }
}
