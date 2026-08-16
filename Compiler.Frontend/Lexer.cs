using System.Text;
using Compiler.AST;

namespace Compiler.Frontend;

public sealed class Lexer
{
    private readonly string _code;
    private readonly List<Token> _tokens = [];

    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    public Lexer(string code)
    {
        _code = code;
    }

    public List<Token> Lex()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        return _tokens;
    }

    private void ScanToken()
    {
        var c = Advance();

        if (IsWhitespace(c))
        {
        }
        else if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else if (IsDigit(c))
        {
            ScanNumber();
        }
        else if (IsAlpha(c))
        {
            ScanIdentifier();
        }
        else if (c == '"')
        {
            ScanString();
        }
        else
        {
            ScanOperator(c);
        }
    }

    private void ScanNumber()
    {
        while (IsDigit(Peek())) Advance();

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();
            while (IsDigit(Peek())) Advance();
        }

        AddToken(TokenType.Number, _code[_start.._current]);
    }

    private void ScanIdentifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        var value = _code[_start.._current];
        AddToken(TokensMap.Map.GetValueOrDefault(value, TokenType.Identifier), value);
    }

    private void ScanString()
    {
        var value = new StringBuilder();

        while (Peek() != '"' && !IsAtEnd())
        {
            var c = Advance();

            if (c == '\n')
            {
                _line++;
                _column = 1;
            }

            if (c == '\\' && !IsAtEnd())
            {
                var escaped = Advance();
                value.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '"' => '"',
                    '\\' => '\\',
                    '0' => '\0',
                    _ => throw new Exception($"Unknown escape sequence '\\{escaped}' at line {_line}"),
                });
            }
            else
            {
                value.Append(c);
            }
        }

        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string at line {_line}");
        }

        Advance(); // closing quote
        AddToken(TokenType.String, value.ToString());
    }

    private void ScanOperator(char c)
    {
        var twoChar = c.ToString() + Peek();
        if (TokensMap.Map.TryGetValue(twoChar, out var twoCharType))
        {
            Advance();
            AddToken(twoCharType, twoChar);
            return;
        }

        if (!TokensMap.Map.TryGetValue(c.ToString(), out var type))
        {
            throw new Exception($"Unexpected character '{c}' at line {_line}");
        }

        AddToken(type, c.ToString());
    }

    private void AddToken(TokenType type, string value)
    {
        _tokens.Add(new Token { Type = type, Value = value, Line = _line, Column = _column - value.Length });
    }

    private char Advance()
    {
        var c = _current < _code.Length ? _code[_current] : '\0';
        _current++;
        _column++;
        return c;
    }

    private char Peek()
    {
        return _current < _code.Length ? _code[_current] : '\0';
    }

    private char PeekNext()
    {
        return _current + 1 < _code.Length ? _code[_current + 1] : '\0';
    }

    private bool IsAtEnd()
    {
        return _current >= _code.Length;
    }

    private static bool IsWhitespace(char c)
    {
        return c is ' ' or '\t' or '\r';
    }

    private static bool IsDigit(char c)
    {
        return c is >= '0' and <= '9';
    }

    private static bool IsAlpha(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    }

    private static bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }
}
