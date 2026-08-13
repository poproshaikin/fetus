using Compiler.AST;

namespace Compiler.Semantic;

public class SemanticAnalyzer
{
    public AnalysisResult Analyze(Program program)
    {
        var result = new AnalysisResult();

        foreach (var statement in program.Body)
            AnalyzeStatement(statement, _globalScope, result.Diagnostics);

        return result;
    }

    private void AnalyzeStatement(Statement stmt, Scope scope, List<SemanticException> diagnostics)
    {
        // try
        // {
            AnalyzeStatement(stmt, scope, BlockContext.TopLevel);
        // }
        // catch (SemanticException e)
        // {
        //     diagnostics.Add(e);
        // }
    }

    private void AnalyzeStatement(Statement stmt, Scope scope, BlockContext context)
    {
        switch (stmt.Kind)
        {
            case NodeKind.VarDecl: AnalyzeVarDecl((VarDecl)stmt, scope); break;
            case NodeKind.FuncDecl: AnalyzeFuncDecl((FuncDecl)stmt, scope); break;
            case NodeKind.ExprStatement: AnalyzeExprStatement((ExprStatement)stmt, scope); break;
            case NodeKind.If: AnalyzeIf((If)stmt, scope, context); break;
            case NodeKind.While: AnalyzeWhile((While)stmt, scope, context); break;
            case NodeKind.Return: AnalyzeReturn((Return)stmt, scope, context); break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AnalyzeReturn(Return stmt, Scope scope, BlockContext context)
    {
        var actualType = stmt.Value != null ? InferType(stmt.Value, scope) : VoidType.Instance;
        var expectedType = context.ReturnType ?? VoidType.Instance;

        if (actualType != expectedType)
            throw new TypeMismatchException(expectedType.GetTypeName(), actualType.GetTypeName(), stmt.Line, stmt.Column);
    }

    private void AnalyzeIf(If stmt, Scope scope, BlockContext context)
    {
        if (InferType(stmt.Condition, scope) != BoolType.Instance)
            throw new InvalidConditionException(stmt.Condition.Line, stmt.Condition.Column);

        AnalyzeBlock(stmt.Then, scope.CreateChild(), context);

        switch (stmt.Else)
        {
            case null:
                break;
            case Block elseBlock:
                AnalyzeBlock(elseBlock, scope.CreateChild(), context);
                break;
            case If elseIf:
                AnalyzeIf(elseIf, scope, context);
                break;
        }
    }
    
    private void AnalyzeWhile(While stmt, Scope scope, BlockContext context)
    {
        if (InferType(stmt.Condition, scope) != BoolType.Instance)
            throw new InvalidConditionException(stmt.Condition.Line, stmt.Condition.Column);

        AnalyzeBlock(stmt.Body, scope.CreateChild(), context with { InLoop = true });
    }

    private void AnalyzeExprStatement(ExprStatement stmt, Scope scope)
    {
        switch (stmt.Expression.Kind)
        {
            case NodeKind.Assignment: AnalyzeAssignment((Assignment)stmt.Expression, scope); break;
            case NodeKind.Call: InferType(stmt.Expression, scope); break;
            default:
                throw new UnsupportedStatementException(stmt.Line, stmt.Column);
        }
    }

    private void AnalyzeAssignment(Assignment stmt, Scope scope)
    {
        var targetSymbol = scope.Resolve(stmt.Target.Name);
        if (targetSymbol == null)
            throw new UndefinedSymbolException(stmt.Target.Name, stmt.Line, stmt.Column);

        var targetType = targetSymbol.Type;
        var actualType = InferType(stmt.Value, scope);

        if (actualType != targetType)
            throw new TypeMismatchException(
                targetType.GetTypeName(),
                actualType.GetTypeName(),
                stmt.Line,
                stmt.Column);
    }

    private void AnalyzeVarDecl(VarDecl stmt, Scope scope)
    {
        var expectedType = _typeTable.GetOrThrow(stmt.TypeName);
        var inferredType = stmt.Init != null ? InferType(stmt.Init, scope) : null;
        
        if (inferredType != null && expectedType != inferredType)
            throw new TypeMismatchException(stmt.TypeName.Name, inferredType.GetTypeName(), stmt.Line, stmt.Column);
            
        scope.Declare(new Symbol(stmt.Identifier.Name, expectedType, stmt.Line, stmt.Column));
    }

    private void AnalyzeFuncDecl(FuncDecl func, Scope scope)
    {
        if (scope.Resolve(func.Identifier.Name) is not null)
            throw new DuplicateDeclarationException(func.Identifier.Name, func.Identifier.Line, func.Identifier.Column);
        
        var returnType = _typeTable.GetOrThrow(func.ReturnType);
        var funcScope = scope.CreateChild();
        var paramTypes = new List<TypeInfo>();

        foreach (var param in func.Params)
        {
            var paramType = _typeTable.GetOrThrow(param.Type);
            paramTypes.Add(paramType);
            funcScope.Declare(new Symbol(param.Identifier.Name, paramType, param.Line, param.Column));
        }

        var functionType = new FunctionType { ReturnType = returnType, ParamTypes = paramTypes };
        scope.Declare(new Symbol(func.Identifier.Name, functionType, func.Identifier.Line, func.Identifier.Column));

        AnalyzeBlock(func.Body, funcScope, new BlockContext { InLoop = false, ReturnType = returnType });
    }

    private void AnalyzeBlock(Block block, Scope scope, BlockContext blockContext)
    {
        if (block.Body.Count == 0)
            return;
        
        foreach (var statement in block.Body)
        {
            AnalyzeStatement(statement, scope, blockContext);
        }

        if (blockContext.ReturnType != null)
        {
            var last = block.Body.Last();
            if (last is not Return @return)
                throw new MissingReturnException(block.Line, block.Column);

            var actualType = @return.Value != null ? InferType(@return.Value, scope) : VoidType.Instance;
            if (blockContext.ReturnType != actualType)
                throw new TypeMismatchException(
                    blockContext.ReturnType?.GetTypeName() ?? "void",
                    actualType.GetTypeName(),
                    @return.Line,
                    @return.Column);
        }
    }

    private TypeInfo InferType(Expression expression, Scope scope)
    {
        return expression.Kind switch
        {
            NodeKind.Literal => InferLiteral((Literal)expression),
            NodeKind.Binary => InferBinary((Binary)expression, scope),
            NodeKind.Call => InferCall((Call)expression, scope),
            NodeKind.Identifier => InferIdentifier((Identifier)expression, scope),
            _ => throw new UnsupportedExpressionException(expression.Line, expression.Column),
        };
    }

    private TypeInfo InferIdentifier(Identifier identifier, Scope scope)
    {
        var symbol = 
            scope.Resolve(identifier.Name) ??
            throw new UndefinedSymbolException(identifier.Name, identifier.Line, identifier.Column);

        return symbol.Type;
    }

    private TypeInfo InferCall(Call call, Scope scope)
    {
        if (call.Callee is not Identifier callee)
            throw new NotCallableException(call.Callee.Kind.ToString(), call.Line, call.Column);

        var symbol = scope.Resolve(callee.Name) ?? throw new UndefinedSymbolException(callee.Name, callee.Line, callee.Column);
        if (symbol.Type is not FunctionType functionType)
            throw new NotCallableException(symbol.Name, symbol.Line, symbol.Column);

        if (call.Args.Count != functionType.ParamTypes.Count)
            throw new ArgumentCountMismatchException(functionType.ParamTypes.Count, call.Args.Count, call.Line, call.Column);

        for (var i = 0; i < call.Args.Count; i++)
        {
            var argType = InferType(call.Args[i], scope);
            var paramType = functionType.ParamTypes[i];
            if (argType != paramType)
                throw new TypeMismatchException(paramType.GetTypeName(), argType.GetTypeName(), call.Args[i].Line, call.Args[i].Column);
        }

        return functionType.ReturnType;
    }

    private TypeInfo InferLiteral(Literal literal)
    {
        if (literal.Value is string)
            return StringType.Instance;

        if (literal.Value is bool)
            return BoolType.Instance;

        if (literal.Value is double d)
            return d == Math.Truncate(d) ? IntType.Int32 : FloatType.Float32;

        throw new UnsupportedLiteralException(literal.Line, literal.Column);
    }

    private TypeInfo InferBinary(Binary binary, Scope scope)
    {
        var left = InferType(binary.Left, scope);
        var right = InferType(binary.Right, scope);

        return binary.Operator switch
        {
            BinaryOperator.Plus or BinaryOperator.Minus or BinaryOperator.Star or BinaryOperator.Slash =>
                InferArithmetic(left, right, binary),

            BinaryOperator.Lt or BinaryOperator.Gt or BinaryOperator.LtEq or BinaryOperator.GtEq =>
                InferComparison(left, right, binary),

            BinaryOperator.Eq or BinaryOperator.NotEq =>
                InferEquality(left, right, binary),

            BinaryOperator.And or BinaryOperator.Or =>
                InferLogical(left, right, binary),

            _ => throw new ArgumentOutOfRangeException(),
        };
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
            throw new TypeMismatchException(left.GetTypeName(), right.GetTypeName(), binary.Line, binary.Column);

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
        if (type is not IntType and not FloatType)
            throw new TypeMismatchException("int/float", type.GetTypeName(), binary.Line, binary.Column);
    }

    private static void RequireType(TypeInfo type, TypeInfo expected, Binary binary)
    {
        if (type != expected)
            throw new TypeMismatchException(expected.GetTypeName(), type.GetTypeName(), binary.Line, binary.Column);
    }
    
    private readonly Scope _globalScope = new();
    private readonly TypeTable _typeTable = new();

    private readonly record struct BlockContext(bool InLoop, TypeInfo? ReturnType)
    {
        public static readonly BlockContext TopLevel = new(false, null);
    }
}