using Compiler.AST;

namespace Compiler.Semantic;

public abstract record BoundNode;

public abstract record BoundExpression(TypeInfo Type) : BoundNode;

public abstract record BoundStatement : BoundNode;

public sealed record BoundLiteral(TypeInfo Type, object? Value) : BoundExpression(Type);

public sealed record BoundIdentifier(TypeInfo Type, string Name) : BoundExpression(Type);

public sealed record BoundBinary(TypeInfo Type, BoundExpression Left, BoundExpression Right, BinaryOperator Op)
    : BoundExpression(Type);

public sealed record BoundAssignment(TypeInfo Type, BoundExpression Target, BoundExpression Value) : BoundExpression(Type);

public sealed record BoundCall(TypeInfo Type, string Callee, List<BoundExpression> Args) : BoundExpression(Type);

public sealed record BoundSyscall(TypeInfo Type, List<BoundExpression> Args) : BoundExpression(Type);

public sealed record BoundPeek(BoundExpression Address, BoundExpression Offset) : BoundExpression(IntType.Int32);

public sealed record BoundPoke(BoundExpression Address, BoundExpression Offset, BoundExpression Value) : BoundExpression(VoidType.Instance);

public sealed record BoundVarDecl(TypeInfo Type, string Name, BoundExpression? Init) : BoundStatement;

public sealed record BoundParamDecl(TypeInfo Type, string Name) : BoundStatement;

public sealed record BoundFuncDecl(string Name, TypeInfo ReturnType, List<BoundParamDecl> Params, BoundBlock Body)
    : BoundStatement;

public sealed record BoundBlock(List<BoundStatement> Body) : BoundStatement;

public sealed record BoundReturn(BoundExpression? Value) : BoundStatement;

// Else is BoundBlock | BoundIf | null
public sealed record BoundIf(BoundExpression Condition, BoundBlock Then, BoundNode? Else) : BoundStatement;

public sealed record BoundWhile(BoundExpression Condition, BoundBlock Body) : BoundStatement;

public sealed record BoundExprStatement(BoundExpression Expression) : BoundStatement;

public sealed record BoundProgram(List<BoundStatement> Body) : BoundNode;

public sealed record BoundCast(TypeInfo Type, BoundExpression Value) : BoundExpression(Type);

public sealed record BoundBreak : BoundStatement;

public sealed record BoundContinue : BoundStatement;

public sealed record BoundStruct(string Name, List<BoundVarDecl> Fields, List<BoundFuncDecl> Methods) : BoundStatement;

public sealed record BoundMemberAccess(TypeInfo Type, BoundExpression Target, string Member) : BoundExpression(Type);