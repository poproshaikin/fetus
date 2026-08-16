using Compiler.AST;

namespace Compiler.Semantic;

public class SemanticAnalyzer
{
    public SemanticAnalyzer(AstModule astModule)
    {
        _astModule = astModule;
    }

    public BoundProgram Analyze()
    {
        var body = new List<BoundStatement>();
        foreach (var statement in _astModule.Body)
            body.Add(BindStatement(statement));

        return new BoundProgram(body);
    }

    private readonly AstModule _astModule;
    private readonly TypeTable _typeTable = new();
    private readonly ConversionsTable _conversionsTable = new();
    private Scope _scope = new();
    private BlockContext _context = BlockContext.TopLevel;
    private bool _inBlock;

    private BoundStatement BindStatement(Statement stmt)
    {
        return stmt.Kind switch
        {
            NodeKind.VarDecl => BindVarDecl((VarDecl)stmt),
            NodeKind.FuncDecl => BindFuncDecl((FuncDecl)stmt),
            NodeKind.ExprStatement => BindExprStatement((ExprStatement)stmt),
            NodeKind.If => BindIf((If)stmt),
            NodeKind.While => BindWhile((While)stmt),
            NodeKind.Return => BindReturn((Return)stmt),
            NodeKind.Break when _context.InLoop => new BoundBreak(),
            NodeKind.Continue when _context.InLoop => new BoundContinue(),
            NodeKind.Break => throw new BreakOutsideLoopException(stmt.Line, stmt.Column),
            NodeKind.Continue => throw new ContinueOutsideLoopException(stmt.Line, stmt.Column),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private BoundReturn BindReturn(Return stmt)
    {
        var value = stmt.Value != null ? BindExpression(stmt.Value) : null;
        var actualType = value?.Type ?? VoidType.Instance;
        var expectedType = _context.ReturnType ?? VoidType.Instance;

        if (actualType != expectedType)
            throw new TypeMismatchException(expectedType.TypeName, actualType.TypeName, stmt.Line, stmt.Column);

        return new BoundReturn(value);
    }

    private BoundIf BindIf(If stmt)
    {
        var condition = BindExpression(stmt.Condition);
        if (condition.Type != BoolType.Instance)
            throw new InvalidConditionException(stmt.Condition.Line, stmt.Condition.Column);

        var then = BindBlockInChildScope(stmt.Then);

        BoundNode? boundElse = stmt.Else switch
        {
            null => null,
            Block elseBlock => BindBlockInChildScope(elseBlock),
            If elseIf => BindIf(elseIf),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return new BoundIf(condition, then, boundElse);
    }

    private BoundWhile BindWhile(While stmt)
    {
        var condition = BindExpression(stmt.Condition);
        if (condition.Type != BoolType.Instance)
            throw new InvalidConditionException(stmt.Condition.Line, stmt.Condition.Column);

        var savedScope = _scope;
        var savedContext = _context;

        _scope = _scope.CreateChild();
        _context = _context with { InLoop = true };
        var body = BindBlock(stmt.Body);

        _scope = savedScope;
        _context = savedContext;

        return new BoundWhile(condition, body);
    }

    private BoundExprStatement BindExprStatement(ExprStatement stmt)
    {
        var expr = stmt.Expression.Kind switch
        {
            NodeKind.Assignment => BindAssignment((Assignment)stmt.Expression),
            NodeKind.Call => BindExpression(stmt.Expression),
            _ => throw new UnsupportedStatementException(stmt.Line, stmt.Column),
        };

        return new BoundExprStatement(expr);
    }

    private BoundAssignment BindAssignment(Assignment stmt)
    {
        var targetSymbol = _scope.Resolve(stmt.Target.Name);
        if (targetSymbol == null)
            throw new UndefinedSymbolException(stmt.Target.Name, stmt.Line, stmt.Column);

        var targetType = targetSymbol.Type;
        var value = BindExpression(stmt.Value);

        if (value.Type != targetType)
            throw new TypeMismatchException(
                targetType.TypeName,
                value.Type.TypeName,
                stmt.Line,
                stmt.Column);

        return new BoundAssignment(targetType, stmt.Target.Name, value);
    }

    private BoundVarDecl BindVarDecl(VarDecl stmt)
    {
        var expectedType = _typeTable.GetOrThrow(stmt.TypeName);
        var init = stmt.Init != null ? BindExpression(stmt.Init) : null;

        if (init != null && expectedType != init.Type)
            throw new TypeMismatchException(stmt.TypeName.Name, init.Type.TypeName, stmt.Line, stmt.Column);

        _scope.Declare(new Symbol(stmt.Identifier.Name, expectedType, stmt.Line, stmt.Column));

        return new BoundVarDecl(expectedType, stmt.Identifier.Name, init);
    }

    private BoundFuncDecl BindFuncDecl(FuncDecl func)
    {
        if (_inBlock)
            throw new NestedFunctionException(func.Identifier.Name, func.Identifier.Line, func.Identifier.Column);

        if (_scope.Resolve(func.Identifier.Name) is not null)
            throw new DuplicateDeclarationException(func.Identifier.Name, func.Identifier.Line, func.Identifier.Column);

        var returnType = _typeTable.GetOrThrow(func.ReturnType);

        var savedScope = _scope;
        var savedContext = _context;

        _scope = _scope.CreateChild();

        var boundParams = new List<BoundParamDecl>();
        var paramTypes = new List<TypeInfo>();

        foreach (var param in func.Params)
        {
            var paramType = _typeTable.GetOrThrow(param.Type);
            paramTypes.Add(paramType);
            boundParams.Add(new BoundParamDecl(paramType, param.Identifier.Name));
            _scope.Declare(new Symbol(param.Identifier.Name, paramType, param.Line, param.Column));
        }

        var functionType = new FunctionType { ReturnType = returnType, ParamTypes = paramTypes };
        savedScope.Declare(new Symbol(func.Identifier.Name, functionType, func.Identifier.Line, func.Identifier.Column));

        _context = new BlockContext { InLoop = false, ReturnType = returnType };
        var body = BindBlock(func.Body);

        if (returnType != VoidType.Instance && !BlockDefinitelyReturns(func.Body))
            throw new MissingReturnException(func.Identifier.Line, func.Identifier.Column);

        _scope = savedScope;
        _context = savedContext;

        return new BoundFuncDecl(func.Identifier.Name, returnType, boundParams, body);
    }

    private static bool BlockDefinitelyReturns(Block block) => block.Body.Any(StatementDefinitelyReturns);

    private static bool StatementDefinitelyReturns(Statement stmt) => stmt switch
    {
        Return => true,
        If ifStmt when ifStmt.Else != null => BlockDefinitelyReturns(ifStmt.Then) && ElseDefinitelyReturns(ifStmt.Else),
        _ => false,
    };

    private static bool ElseDefinitelyReturns(Node elseNode) => elseNode switch
    {
        Block block => BlockDefinitelyReturns(block),
        If ifStmt => StatementDefinitelyReturns(ifStmt),
        _ => false,
    };

    private BoundBlock BindBlock(Block block)
    {
        var savedInBlock = _inBlock;
        _inBlock = true;

        var body = new List<BoundStatement>();
        foreach (var statement in block.Body)
            body.Add(BindStatement(statement));

        _inBlock = savedInBlock;
        return new BoundBlock(body);
    }

    private BoundBlock BindBlockInChildScope(Block block)
    {
        var saved = _scope;
        _scope = _scope.CreateChild();
        var bound = BindBlock(block);
        _scope = saved;
        return bound;
    }

    private BoundExpression BindExpression(Expression expression)
    {
        return expression.Kind switch
        {
            NodeKind.Literal => BindLiteral((Literal)expression),
            NodeKind.Binary => BindBinary((Binary)expression),
            NodeKind.Call => BindCall((Call)expression),
            NodeKind.Identifier => BindIdentifier((Identifier)expression),
            NodeKind.Cast => BindCast((Cast)expression),
            _ => throw new UnsupportedExpressionException(expression.Line, expression.Column),
        };
    }

    private BoundCast BindCast(Cast cast)
    {
        var targetType = _typeTable.GetOrThrow(cast.TargetType);
        var castee = BindExpression(cast.Value);

        var allowed = _conversionsTable.IsConvertibleTo(castee.Type.TypeName, targetType.TypeName);
        if (!allowed)
            throw new TypeMismatchException(targetType.TypeName, castee.Type.TypeName, cast.Line, cast.Column);
        
        return new BoundCast(targetType, castee);   
    }
        
    private BoundIdentifier BindIdentifier(Identifier identifier)
    {
        var symbol =
            _scope.Resolve(identifier.Name) ??
            throw new UndefinedSymbolException(identifier.Name, identifier.Line, identifier.Column);

        return new BoundIdentifier(symbol.Type, identifier.Name);
    }

    private BoundExpression BindCall(Call call)
    {
        if (call.Callee is not Identifier callee)
            throw new NotCallableException(call.Callee.Kind.ToString(), call.Line, call.Column);

        if (callee.Name == "syscall")
            return BindSyscall(call);

        if (callee.Name == "peek")
            return BindPeek(call);

        var symbol = _scope.Resolve(callee.Name) ?? throw new UndefinedSymbolException(callee.Name, callee.Line, callee.Column);
        if (symbol.Type is not FunctionType functionType)
            throw new NotCallableException(symbol.Name, symbol.Line, symbol.Column);

        if (call.Args.Count != functionType.ParamTypes.Count)
            throw new ArgumentCountMismatchException(functionType.ParamTypes.Count, call.Args.Count, call.Line, call.Column);

        var boundArgs = new List<BoundExpression>();
        for (var i = 0; i < call.Args.Count; i++)
        {
            var arg = BindExpression(call.Args[i]);
            var paramType = functionType.ParamTypes[i];
            if (arg.Type != paramType)
                throw new TypeMismatchException(paramType.TypeName, arg.Type.TypeName, call.Args[i].Line, call.Args[i].Column);

            boundArgs.Add(arg);
        }

        return new BoundCall(functionType.ReturnType, callee.Name, boundArgs);
    }

    private BoundPeek BindPeek(Call call)
    {
        var address = BindExpression(call.Args[0]);
        var offset = BindExpression(call.Args[1]);
        
        if (address.Type is not IntType and not StringType)
            throw new TypeMismatchException("int", address.Type.TypeName, call.Args[0].Line, call.Args[0].Column);        
        
        if (offset.Type is not IntType)
            throw new TypeMismatchException("int", offset.Type.TypeName, call.Args[1].Line, call.Args[1].Column);
        
        return new BoundPeek(address, offset);
    }

    private BoundSyscall BindSyscall(Call call)
    {
        if (call.Args.Count is < 1 or > 7)
            throw new ArgumentCountMismatchException(7, call.Args.Count, call.Line, call.Column);

        var boundArgs = new List<BoundExpression>();
        foreach (var arg in call.Args)
        {
            var bound = BindExpression(arg);
            if (bound.Type is not IntType and not StringType and not PtrType)
                throw new TypeMismatchException("convertible to ptr", bound.Type.TypeName, arg.Line, arg.Column);

            boundArgs.Add(bound);
        }

        return new BoundSyscall(IntType.Int32, boundArgs);
    }

    private BoundLiteral BindLiteral(Literal literal)
    {
        var type = InferLiteralType(literal);
        return new BoundLiteral(type, literal.Value);
    }

    private TypeInfo InferLiteralType(Literal literal)
    {
        if (literal.Value is string)
            return StringType.Instance;

        if (literal.Value is bool)
            return BoolType.Instance;

        if (literal.Value is double d)
            return d == Math.Truncate(d) ? IntType.Int32 : FloatType.Float32;

        throw new UnsupportedLiteralException(literal.Line, literal.Column);
    }

    private BoundBinary BindBinary(Binary binary)
    {
        var left = BindExpression(binary.Left);
        var right = BindExpression(binary.Right);

        var type = binary.Operator switch
        {
            BinaryOperator.Plus or BinaryOperator.Minus or BinaryOperator.Star or BinaryOperator.Slash =>
                InferArithmetic(left.Type, right.Type, binary),

            BinaryOperator.Lt or BinaryOperator.Gt or BinaryOperator.LtEq or BinaryOperator.GtEq =>
                InferComparison(left.Type, right.Type, binary),

            BinaryOperator.Eq or BinaryOperator.NotEq =>
                InferEquality(left.Type, right.Type, binary),

            BinaryOperator.And or BinaryOperator.Or =>
                InferLogical(left.Type, right.Type, binary),

            _ => throw new ArgumentOutOfRangeException(),
        };

        return new BoundBinary(type, left, right, binary.Operator);
    }

    private TypeInfo InferArithmetic(TypeInfo left, TypeInfo right, Binary binary)
    {
        RequireNumeric(left, binary);
        RequireNumeric(right, binary);
        return left is FloatType || right is FloatType ? FloatType.Float32 : IntType.Int32;
    }

    private TypeInfo InferComparison(TypeInfo left, TypeInfo right, Binary binary)
    {
        RequireNumeric(left, binary);
        RequireNumeric(right, binary);
        return BoolType.Instance;
    }

    private TypeInfo InferEquality(TypeInfo left, TypeInfo right, Binary binary)
    {
        if (left != right)
            throw new TypeMismatchException(left.TypeName, right.TypeName, binary.Line, binary.Column);

        return BoolType.Instance;
    }

    private TypeInfo InferLogical(TypeInfo left, TypeInfo right, Binary binary)
    {
        RequireType(left, BoolType.Instance, binary);
        RequireType(right, BoolType.Instance, binary);
        return BoolType.Instance;
    }

    private static void RequireNumeric(TypeInfo type, Binary binary)
    {
        if (type is not IntType and not FloatType and not PtrType)
            throw new TypeMismatchException("int/float", type.TypeName, binary.Line, binary.Column);
    }

    private static void RequireType(TypeInfo type, TypeInfo expected, Binary binary)
    {
        if (type != expected)
            throw new TypeMismatchException(expected.TypeName, type.TypeName, binary.Line, binary.Column);
    }

    private readonly record struct BlockContext(bool InLoop, TypeInfo ReturnType)
    {
        public static readonly BlockContext TopLevel = new(false, IntType.Int32);
    }
}