namespace cm_script;

using func_router;
using bridge;

public class Script
{
    private Router _router;
    private IServer _server;

    public string python { get; set; }

    public Script(IServer server, string python)
    {
        _server = server;
        _router = new Router(_server);
        this.python = python;
    }

    public void AddFunction(string name, Function func)
        => _router.Add(name, func);

    public async Task RunScript(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, _router, _server, python);
        await interpreter.Interpret();
    }

}
