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

public sealed class UndefinedTypeException : SemanticException
{
    public string TypeName { get; }

    public UndefinedTypeException(string typeName, int line, int column)
        : base($"Undefined type '{typeName}'", line, column)
    {
        TypeName = typeName;
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
