using System.Text;

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
        ["LIB"] = TokenType.ST_Lib,
        ["SCRIPT"] = TokenType.ST_Script,
        ["PYTHON"] = TokenType.ST_Python,

        ["$"] = TokenType.ST_Cosmos,
        ["#"] = TokenType.ST_Exe,
        ["@"] = TokenType.ST_Lib,
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
        return ret;
    }

    public Token NextToken()
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

    private bool IsEnd() => _pos >= _length - 1;

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
        // skip comment 
        if (_curr == '!')
        {
            while (_curr != '\0' && _curr != '\n' && _curr != '\r')
            {
                Advance();
                if (_pos >= _length - 1) break;
            }
        }

        while (_curr == '\0' || _curr == ' ' || _curr == '\t')
        {
            Advance();
            if (_pos >= _length - 1) break;
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
        var sb = new StringBuilder();
        while (_curr != ' ' && _curr != '\0' && _curr != '\n')
        {
            sb.Append(_curr);
            Advance();
        }
        var tk = sb.ToString();
        var tkType = Keywords.TryGetValue(tk, out var tkt) ? tkt : TokenType.Identifier;
        return new Token(tk, tkType, _line, startCol);
    }

}