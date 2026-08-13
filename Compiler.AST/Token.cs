namespace Compiler.AST;

public enum TokenType
{
    Func,
    If,
    Else,
    While,
    Return,
    As,
    Identifier,
    Number,
    String,
    True,
    False,
    Null,
    LParen,
    RParen,
    LBrace,
    RBrace,
    Comma,
    Semicolon,
    Assign,
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
    Not,
    EOF,
}

public static class TokensMap
{
    public static readonly Dictionary<string, TokenType> Map = new()
    {
        ["func"] = TokenType.Func,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["while"] = TokenType.While,
        ["return"] = TokenType.Return,
        ["as"] = TokenType.As,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["null"] = TokenType.Null,
        ["("] = TokenType.LParen,
        [")"] = TokenType.RParen,
        ["{"] = TokenType.LBrace,
        ["}"] = TokenType.RBrace,
        [","] = TokenType.Comma,
        [";"] = TokenType.Semicolon,
        ["="] = TokenType.Assign,
        ["+"] = TokenType.Plus,
        ["-"] = TokenType.Minus,
        ["*"] = TokenType.Star,
        ["/"] = TokenType.Slash,
        ["=="] = TokenType.Eq,
        ["!="] = TokenType.NotEq,
        ["<"] = TokenType.Lt,
        [">"] = TokenType.Gt,
        ["<="] = TokenType.LtEq,
        [">="] = TokenType.GtEq,
        ["&&"] = TokenType.And,
        ["||"] = TokenType.Or,
        ["!"] = TokenType.Not,
    };
}

public sealed record Token
{
    public required TokenType Type { get; init; }
    public required string Value { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }
}
