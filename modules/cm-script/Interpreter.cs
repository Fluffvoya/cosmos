using argument;
using func_router;

namespace cm_script;

public class Interpreter
{
    private List<Token> _tokens;
    private int _pos = 0;
    private int _length;
    private Token _curr
    {
        get =>
            _pos < _length ?
            _tokens[_pos] :
            new Token("\0", TokenType.EOF, 1, 0);
    }

    private Router _router;

    public Interpreter(List<Token> tokens, Router router)
    {
        _tokens = tokens;
        _length = _tokens.Count;
        _router = router;
    }

    public void Interpret()
    {
        if (IsEnd()) return;

        while (_curr.tokenType == TokenType.NewLine) Advance();

        switch (_curr.tokenType)
        {
            case TokenType.ST_Cosmos:
                Advance();
                Cosmos();
                break;
            case TokenType.ST_Exe:
                Advance();
                Executable();
                break;
            case TokenType.ST_Lib:
                Advance();
                Library();
                break;
            case TokenType.ST_Script:
                Advance();
                Script();
                break;
            case TokenType.ST_Python:
                Advance();
                Python();
                break;

            case TokenType.Identifier:

            case TokenType.EOF:
            default: break;
        }

    }

    private void Cosmos()
    {
        var args = new List<object>();
        var func = _curr.tk;
        Advance();
        while (true)
        {
            var tkType = _curr.tokenType;
            if (tkType == TokenType.NewLine || tkType == TokenType.EOF)
                break;
            var tk = _curr.tk;
            var argType = ArgumentTypeJudge.Judge(tk);
            switch (argType)
            {
                case ArgumentType.Number:
                    object numArg = ArgumentConvert.ToNumber(tk);
                    args.Add(numArg);
                    break;
                case ArgumentType.Float:
                    object floatArg = ArgumentConvert.ToFloat(tk);
                    args.Add(floatArg);
                    break;
                case ArgumentType.String:
                    object strArg = ArgumentConvert.ToString_(tk);
                    args.Add(strArg);
                    break;
                default:
                    break;
            }
            Advance();
        }
        _router.Call(func, args);
    }

    private void Executable()
    {

    }

    private void Library()
    {

    }

    private void Script()
    {

    }

    private void Python()
    {

    }

    private void Advance()
    {
        if (_pos + 1 < _length) _pos++;
    }

    private bool IsEnd() => _curr.tokenType == TokenType.EOF;
}