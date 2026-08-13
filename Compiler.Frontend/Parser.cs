using System.Globalization;
using Compiler.AST;

namespace Compiler.Frontend;

public sealed class ParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParseException(string message, int line, int column)
        : base($"{message} (line {line}, column {column})")
    {
        Line = line;
        Column = column;
    }
}

public sealed class Parser
{
    private static readonly Dictionary<TokenType, int> BinaryPrecedence = new()
    {
        [TokenType.Or] = 1,
        [TokenType.And] = 2,
        [TokenType.Eq] = 3,
        [TokenType.NotEq] = 3,
        [TokenType.Lt] = 4,
        [TokenType.Gt] = 4,
        [TokenType.LtEq] = 4,
        [TokenType.GtEq] = 4,
        [TokenType.Plus] = 5,
        [TokenType.Minus] = 5,
        [TokenType.Star] = 6,
        [TokenType.Slash] = 6,
    };

    private static readonly Dictionary<TokenType, BinaryOperator> BinaryOperatorMap = new()
    {
        [TokenType.Or] = BinaryOperator.Or,
        [TokenType.And] = BinaryOperator.And,
        [TokenType.Eq] = BinaryOperator.Eq,
        [TokenType.NotEq] = BinaryOperator.NotEq,
        [TokenType.Lt] = BinaryOperator.Lt,
        [TokenType.Gt] = BinaryOperator.Gt,
        [TokenType.LtEq] = BinaryOperator.LtEq,
        [TokenType.GtEq] = BinaryOperator.GtEq,
        [TokenType.Plus] = BinaryOperator.Plus,
        [TokenType.Minus] = BinaryOperator.Minus,
        [TokenType.Star] = BinaryOperator.Star,
        [TokenType.Slash] = BinaryOperator.Slash,
    };

    private readonly List<Token> _tokens;
    private int _i;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Program Parse()
    {
        _i = 0;
        var (line, column) = PositionOf(Current());
        var body = new List<Statement>();

        while (_i < _tokens.Count)
        {
            if (Match(TokenType.Semicolon))
            {
                Advance();
                continue;
            }
            body.Add(ParseStatement());
        }

        return new Program { Body = body, Line = line, Column = column };
    }

    private Statement ParseStatement()
    {
        return Current()?.Type switch
        {
            TokenType.Func => ParseFuncDecl(),
            TokenType.LBrace => ParseBlock(),
            TokenType.If => ParseIf(),
            TokenType.While => ParseWhile(),
            TokenType.Return => ParseReturn(),
            TokenType.Identifier when Peek()?.Type == TokenType.Identifier => ParseVarDecl(),
            _ => ParseExprStatement(),
        };
    }

    private VarDecl ParseVarDecl()
    {
        var (line, column) = PositionOf(Current());
        var typeName = ParseIdentifier();
        var name = ParseIdentifier();
        MatchOrThrow(TokenType.Assign);
        AdvanceOrThrow();
        var init = ParseExpression();

        MatchOrThrow(TokenType.Semicolon);
        Advance();

        return new VarDecl { TypeName = typeName, Identifier = name, Init = init, Line = line, Column = column };
    }

    private If ParseIf()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.If);
        AdvanceOrThrow();

        MatchOrThrow(TokenType.LParen);
        AdvanceOrThrow();
        var condition = ParseExpression();
        MatchOrThrow(TokenType.RParen);
        AdvanceOrThrow();

        var then = ParseBlock();

        Node? elseBranch = null;
        if (Match(TokenType.Else))
        {
            AdvanceOrThrow();
            elseBranch = Match(TokenType.If) ? ParseIf() : ParseBlock();
        }

        return new If { Condition = condition, Then = then, Else = elseBranch, Line = line, Column = column };
    }

    private While ParseWhile()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.While);
        AdvanceOrThrow();

        MatchOrThrow(TokenType.LParen);
        AdvanceOrThrow();
        var condition = ParseExpression();
        MatchOrThrow(TokenType.RParen);
        AdvanceOrThrow();

        var body = ParseBlock();

        return new While { Condition = condition, Body = body, Line = line, Column = column };
    }

    private Return ParseReturn()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.Return);
        AdvanceOrThrow();

        var value = Match(TokenType.Semicolon) ? null : ParseExpression();

        MatchOrThrow(TokenType.Semicolon);
        Advance();

        return new Return { Value = value, Line = line, Column = column };
    }

    private ExprStatement ParseExprStatement()
    {
        var (line, column) = PositionOf(Current());
        var expression = ParseAssignmentOrExpression();
        MatchOrThrow(TokenType.Semicolon);
        Advance();

        return new ExprStatement { Expression = expression, Line = line, Column = column };
    }

    private Expression ParseAssignmentOrExpression()
    {
        var (line, column) = PositionOf(Current());
        var expr = ParseExpression();
        if (!Match(TokenType.Assign)) return expr;

        if (expr is not Identifier identifier)
        {
            throw ThrowAt(Current(), "Invalid assignment target");
        }

        AdvanceOrThrow();
        var value = ParseExpression();

        return new Assignment { Target = identifier, Value = value, Line = line, Column = column };
    }

    private FuncDecl ParseFuncDecl()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.Func);
        AdvanceOrThrow();

        var returnType = ParseIdentifier();
        var name = ParseIdentifier();
        var parameters = ParseParamsList();

        MatchOrThrow(TokenType.LBrace);
        var body = ParseBlock();

        return new FuncDecl
        {
            Identifier = name, 
            ReturnType = returnType,
            Params = parameters, 
            Body = body, 
            Line = line, 
            Column = column
        };
    }

    private List<ParamDecl> ParseParamsList()
    { 
        MatchOrThrow(TokenType.LParen);
        var parameters = new List<ParamDecl>();

        AdvanceOrThrow();
        while (!Match(TokenType.RParen))
        {
            if (parameters.Count > 0)
            {
                MatchOrThrow(TokenType.Comma);
                AdvanceOrThrow();
            }

            var type = ParseIdentifier();
            var name = ParseIdentifier();

            parameters.Add(new ParamDecl
            {
                Type = type,
                Identifier = name,
                Line = type.Line,
                Column = type.Column
            });
        }

        AdvanceOrThrow();

        return parameters;
    }

    private Block ParseBlock()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.LBrace);
        AdvanceOrThrow();

        var body = new List<Statement>();
        while (!Match(TokenType.RBrace))
        {
            if (Match(TokenType.Semicolon))
            {
                Advance();
                continue;
            }
            body.Add(ParseStatement());
        }

        Advance();

        return new Block { Body = body, Line = line, Column = column };
    }

    private Expression ParseExpression()
    {
        return ParseBinary(0);
    }

    private Expression ParseBinary(int minPrecedence)
    {
        var left = ParsePrimary();

        while (true)
        {
            var type = Current()?.Type;
            if (type is null || !BinaryPrecedence.TryGetValue(type.Value, out var precedence) || precedence < minPrecedence)
                break;

            var op = BinaryOperatorMap[type.Value];
            AdvanceOrThrow();
            var right = ParseBinary(precedence + 1);

            left = new Binary { Operator = op, Left = left, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private Expression ParsePrimary()
    {
        switch (Current()?.Type)
        {
            case TokenType.String: return ParseStringLiteral();
            case TokenType.Number: return ParseNumberLiteral();
            case TokenType.True: return ParseBooleanLiteral(true);
            case TokenType.False: return ParseBooleanLiteral(false);
            case TokenType.Null: return ParseNullLiteral();
            case TokenType.Identifier: return ParseIdentifierOrCall();
            default:
                throw ThrowAt(Current(), $"Unexpected token '{Current()?.Type}' in expression");
        }
    }

    private Expression ParseIdentifierOrCall()
    {
        var identifier = ParseIdentifier();
        return Match(TokenType.LParen) ? ParseCall(identifier) : identifier;
    }

    private Call ParseCall(Identifier callee)
    {
        AdvanceOrThrow();
        var args = new List<Expression>();
        if (!Match(TokenType.RParen))
        {
            args.Add(ParseExpression());
            while (Match(TokenType.Comma))
            {
                AdvanceOrThrow();
                args.Add(ParseExpression());
            }
        }
        MatchOrThrow(TokenType.RParen);
        AdvanceOrThrow();

        return new Call { Callee = callee, Args = args, Line = callee.Line, Column = callee.Column };
    }

    private Literal ParseStringLiteral()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.String);
        var value = Current()!.Value;
        AdvanceOrThrow();
        return new Literal { Value = value, Line = line, Column = column };
    }

    private Literal ParseNumberLiteral()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.Number);
        var value = double.Parse(Current()!.Value, CultureInfo.InvariantCulture);
        AdvanceOrThrow();
        return new Literal { Value = value, Line = line, Column = column };
    }

    private Literal ParseBooleanLiteral(bool value)
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(value ? TokenType.True : TokenType.False);
        AdvanceOrThrow();
        return new Literal { Value = value, Line = line, Column = column };
    }

    private Literal ParseNullLiteral()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.Null);
        AdvanceOrThrow();
        return new Literal { Value = null, Line = line, Column = column };
    }

    private Identifier ParseIdentifier()
    {
        var (line, column) = PositionOf(Current());
        MatchOrThrow(TokenType.Identifier);
        var name = Current()!.Value;
        Advance();
        return new Identifier { Name = name, Line = line, Column = column };
    }

    private Token? Current()
    {
        return _i < _tokens.Count ? _tokens[_i] : null;
    }

    private Token? Peek(int n = 1)
    {
        return _i + n < _tokens.Count ? _tokens[_i + n] : null;
    }

    private bool Advance()
    {
        _i++;
        return _i < _tokens.Count;
    }

    private (int Line, int Column) PositionOf(Token? t)
    {
        if (t is not null) return (t.Line, t.Column);
        var last = _tokens.Count > 0 ? _tokens[^1] : null;
        return last is not null ? (last.Line, last.Column + last.Value.Length) : (1, 1);
    }

    private Exception ThrowAt(Token? t, string message)
    {
        var (line, column) = PositionOf(t);
        return new ParseException(message, line, column);
    }

    private void AdvanceOrThrow()
    {
        if (!Advance())
            throw ThrowAt(Current(), "Unexpected end of input");
    }

    private bool Match(TokenType expected)
    {
        var t = Current();
        return t is not null && t.Type == expected;
    }

    private void MatchOrThrow(TokenType expected)
    {
        var t = Current();
        if (!Match(expected))
            throw ThrowAt(t, $"Expected '{expected}' but got {(t is not null ? $"'{t.Type}'" : "end of input")}");
    }
}