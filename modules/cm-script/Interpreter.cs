using argument;
using bridge;
using cosmos_error;
using func_router;
using process;

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
    private IServer _server;
    private string _python;

    public Interpreter(List<Token> tokens, Router router, IServer server, string python)
    {
        _tokens = tokens;
        _length = _tokens.Count;
        _router = router;
        _server = server;
        _python = python;
    }

    public async Task Interpret()
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
                await Executable();
                break;
            case TokenType.ST_Script:
                Advance();
                Script();
                break;
            case TokenType.ST_Python:
                Advance();
                await Python();
                break;

            case TokenType.Identifier:

            case TokenType.EOF:
            default: break;
        }

    }

    private void Cosmos()
    {
        var args = new List<object>();

        if (_curr.tokenType == TokenType.NewLine || _curr.tokenType == TokenType.EOF)
            throw new InterpreterException(ErrorCode.MissingFunctionName,
                $"Expected function name after COSMOS keyword at line {_curr.line}");

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
        var process = new InternalProcess(_router, func, args);
        process.Execute();
    }

    private async Task Executable()
    {

        if (_curr.tokenType == TokenType.NewLine || _curr.tokenType == TokenType.EOF)
            throw new Exception();

        string program = _curr.tk;
        List<string> args = new List<string>();
        Advance();

        while (true)
        {
            if (_curr.tokenType == TokenType.NewLine || _curr.tokenType == TokenType.EOF)
                break;
            args.Add(_curr.tk);
            Advance();
        }
        var process = new ExecuteProcess(program, args, _server);
        await process.Execute();
    }

    private void Script()
    {

    }

    private async Task Python()
    {
        if (_curr.tokenType == TokenType.NewLine || _curr.tokenType == TokenType.EOF)
            throw new Exception();

        string script = _curr.tk;
        List<string> args = new List<string>();
        Advance();

        while (true)
        {
            if (_curr.tokenType == TokenType.NewLine || _curr.tokenType == TokenType.EOF)
                break;
            args.Add(_curr.tk);
            Advance();
        }
        var process = new PythonProcess(_python, script, args, _server);
        await process.Execute();
    }

    private void Advance()
    {
        if (_pos + 1 < _length) _pos++;
    }

    private bool IsEnd() => _curr.tokenType == TokenType.EOF;
}