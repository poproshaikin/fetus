namespace Compiler.Semantic;

public abstract class SemanticException : Exception
{
    public int Line { get; }
    public int Column { get; }

    protected SemanticException(string message, int line, int column)
        : base($"{message} at {line}:{column}")
    {
        Line = line;
        Column = column;
    }
}

public sealed class UndefinedSymbolException : SemanticException
{
    public string SymbolName { get; }

    public UndefinedSymbolException(string symbolName, int line, int column)
        : base($"Undefined symbol '{symbolName}'", line, column)
    {
        SymbolName = symbolName;
    }
}

public sealed class DuplicateDeclarationException : SemanticException
{
    public string SymbolName { get; }

    public DuplicateDeclarationException(string symbolName, int line, int column)
        : base($"'{symbolName}' is already declared in this scope", line, column)
    {
        SymbolName = symbolName;
    }
}

public sealed class TypeMismatchException : SemanticException
{
    public string Expected { get; }
    public string Actual { get; }

    public TypeMismatchException(string expected, string actual, int line, int column)
        : base($"Type mismatch: expected '{expected}', got '{actual}'", line, column)
    {
        Expected = expected;
        Actual = actual;
    }
}

public sealed class NotCallableException : SemanticException
{
    public string SymbolName { get; }

    public NotCallableException(string symbolName, int line, int column)
        : base($"'{symbolName}' is not callable", line, column)
    {
        SymbolName = symbolName;
    }
}

public sealed class NotAStructException : SemanticException
{
    public string TypeName { get; }

    public NotAStructException(string typeName, int line, int column)
        : base($"'{typeName}' is not a struct and has no members", line, column)
    {
        TypeName = typeName;
    }
}

public sealed class UndefinedMemberException : SemanticException
{
    public string MemberName { get; }
    public string TypeName { get; }

    public UndefinedMemberException(string memberName, string typeName, int line, int column)
        : base($"'{typeName}' has no member '{memberName}'", line, column)
    {
        MemberName = memberName;
        TypeName = typeName;
    }
}

public sealed class ArgumentCountMismatchException : SemanticException
{
    public int Expected { get; }
    public int Actual { get; }

    public ArgumentCountMismatchException(int expected, int actual, int line, int column)
        : base($"Expected {expected} argument(s), got {actual}", line, column)
    {
        Expected = expected;
        Actual = actual;
    }
}

public sealed class InvalidAssignmentTargetException : SemanticException
{
    public InvalidAssignmentTargetException(int line, int column)
        : base("Invalid assignment target", line, column)
    {
    }
}

public sealed class InvalidConditionException : SemanticException
{
    public InvalidConditionException(int line, int column)
        : base("Condition must be of type 'bool'", line, column)
    {
    }
}

public sealed class BreakOutsideLoopException : SemanticException
{
    public BreakOutsideLoopException(int line, int column)
        : base("'break' can only be used inside a loop", line, column)
    {
    }
}

public sealed class ContinueOutsideLoopException : SemanticException
{
    public ContinueOutsideLoopException(int line, int column)
        : base("'continue' can only be used inside a loop", line, column)
    {
    }
}

public sealed class UndefinedTypeException : SemanticException
{
    public string TypeName { get; }

    public UndefinedTypeException(string typeName, int line, int column)
        : base($"Undefined type '{typeName}'", line, column)
    {
        TypeName = typeName;
    }
}

public sealed class NestedFunctionException : SemanticException
{
    public string FunctionName { get; }

    public NestedFunctionException(string functionName, int line, int column)
        : base($"Nested function declarations are not allowed ('{functionName}')", line, column)
    {
        FunctionName = functionName;
    }
}

public sealed class NestedStructException : SemanticException
{
    public string StructName { get; }

    public NestedStructException(string structName, int line, int column)
        : base($"Struct declarations are only allowed at the top level ('{structName}')", line, column)
    {
        StructName = structName;
    }
}

public sealed class MissingReturnException : SemanticException
{
    public MissingReturnException(int line, int column)
        : base("Missing return statement", line, column)
    {
    }
}

public sealed class UnsupportedLiteralException : SemanticException
{
    public UnsupportedLiteralException(int line, int column)
        : base("Unsupported literal type", line, column)
    {
    }
}

public sealed class UnsupportedExpressionException : SemanticException
{
    public UnsupportedExpressionException(int line, int column)
        : base("Unsupported expression", line, column)
    {
    }
}

public sealed class UnsupportedStatementException : SemanticException
{
    public UnsupportedStatementException(int line, int column)
        : base("Unsupported statement", line, column)
    {
    }
}
