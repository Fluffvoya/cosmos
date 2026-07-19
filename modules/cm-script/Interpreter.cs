using System.Runtime.InteropServices;

namespace cm_script;

class Interpreter
{
    private List<Token> _tokens;
    private int _pos = 0;
    private int _length;
    private Token _curr;

    public Interpreter(List<Token> tokens)
    {
        _tokens = tokens;
        _length = _tokens.Count;
        if (_length >= 1) _curr = _tokens[0];
        else _curr = new Token("\0", TokenType.EOF, 1, 0);
    }

    public void Interpret()
    {
        if (IsEnd()) return;

        while (Current().tokenType == TokenType.NewLine) Advance();

        switch (Current().tokenType)
        {
            case TokenType.ST_Cosmos:
                {

                }
        }

    }

    private void Advance()
    {
        if (_pos + 1 < _length) _pos++;
    }

    private Token Current() => _pos < _length ? _tokens[_pos] : new Token("\0", TokenType.EOF, 1, 0);

    private bool IsEnd() => Current().tokenType == TokenType.EOF;
}