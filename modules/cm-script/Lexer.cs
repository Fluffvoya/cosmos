using System.Text;
using cosmos_error;

namespace cm_script;

public class Lexer
{
    private int _pos = 0;
    private int _line = 1;
    private int _col = 0;
    private char _curr = '\0';

    private readonly string _source;
    private readonly int _length;
    private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
    {
        ["COSMOS"] = TokenType.ST_Cosmos,
        ["EXE"] = TokenType.ST_Exe,
        ["SCRIPT"] = TokenType.ST_Script,
        ["PYTHON"] = TokenType.ST_Python,

        ["$"] = TokenType.ST_Cosmos,
        ["#"] = TokenType.ST_Exe,
        ["&"] = TokenType.ST_Script,
    };
    public Lexer(string source)
    {
        _source = source;
        _length = source.Length;
        if (_length >= 1)
            _curr = _source[0];
    }

    public List<Token> Tokenize()
    {
        var token = NextToken();
        var ret = new List<Token>();
        while (token.tokenType != TokenType.EOF)
        {
            ret.Add(token);
            token = NextToken();
        }
        ret.Add(new Token("\0", TokenType.EOF, 1, 0));
        return ret;
    }

    private Token NextToken()
    {
        SkipBlank();
        if (IsEnd())
            return new Token("\0", TokenType.EOF, _line, _col);
        if (_curr == '\n' || _curr == '\r')
        {
            _line++;
            Advance();
            var startCol = _col;
            _col = 0;
            if (_curr == '\n') Advance();
            return new Token("\n", TokenType.NewLine, _line - 1, startCol);
        }
        return ReadIdentifier();
    }

    private bool IsEnd() => _pos >= _length;

    private void Advance()
    {
        if (_pos + 1 < _length)
        {
            _pos++;
            _curr = _source[_pos];
            _col++;
        }
        else
        {
            _pos = _length;
            _curr = '\0';
        }
    }

    private char Peek() => _pos + 1 < _length ? _source[_pos + 1] : '\0';

    private void SkipBlank()
    {
        // skip comment (including the trailing newline)
        if (_curr == '!')
        {
            while (_curr != '\0' && _curr != '\n' && _curr != '\r')
            {
                Advance();
                if (_pos >= _length) break;
            }
            // Consume the trailing newline after the comment
            if (_curr == '\n' || _curr == '\r')
            {
                _line++;
                Advance();
                _col = 0;
                if (_curr == '\n') Advance();
            }
        }

        while (_curr == '\0' || _curr == ' ' || _curr == '\t')
        {
            Advance();
            if (_pos >= _length) break;
        }
    }

    private Token ReadIdentifier()
    {
        var startCol = _col;
        if (_curr == '$' || _curr == '#' || _curr == '@' || _curr == '&')
        {
            string tk_ = _curr.ToString();
            Advance();
            var tkType_ = Keywords.TryGetValue(tk_, out var tkt_) ? tkt_ : TokenType.Unknown;
            return new Token(tk_, tkType_, _line, startCol);
        }
        // Handle quoted string literals - read everything between quotes as one token (quotes included)
        if (_curr == '"')
        {
            var sb = new StringBuilder();
            sb.Append(_curr); // opening quote
            Advance();
            while (_curr != '"' && _curr != '\0' && _curr != '\n' && _curr != '\r')
            {
                sb.Append(_curr);
                Advance();
            }
            if (_curr == '"')
            {
                sb.Append(_curr); // closing quote
                Advance();
            }
            else
            {
                throw new InterpreterException(ErrorCode.SyntaxError,
                    $"Unterminated string literal at line {_line}, col {startCol}");
            }
            return new Token(sb.ToString(), TokenType.String, _line, startCol);
        }
        var sbId = new StringBuilder();
        while (_curr != ' ' && _curr != '\0' && _curr != '\n' && _curr != '\r')
        {
            sbId.Append(_curr);
            Advance();
        }
        var tk = sbId.ToString();
        var tkType = Keywords.TryGetValue(tk, out var tkt) ? tkt : TokenType.Identifier;
        return new Token(tk, tkType, _line, startCol);
    }

}