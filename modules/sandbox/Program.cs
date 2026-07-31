using bridge;
using client;
using cm_script;
using func_router;


public class SandboxServer : IServer
{
    public string Execute(string requests)
    {
        Console.WriteLine("client:" + requests);
        return "reply";
    }
}

public class Sandbox
{
    public static async Task Main(string[] args)
    {
        IServer server = new SandboxServer();
        Router router = new Router(server);
        Function func = new Function((IServer server, List<object> args) =>
        {
            Console.WriteLine("func invoke");
            var requests = Client.CreateRequests(Client.ShowWindow("a", "b"));
            Console.WriteLine("server:" + server.Execute(requests));
        });
        router.Add("func", func);

        var source = "COSMOS func";
        var source2 = "PYTHON D:\\program\\a.py";

        Lexer lexer = new Lexer(source2);
        var tokens = lexer.Tokenize();
        Interpreter interpreter = new Interpreter(tokens, router, server, "python");
        await interpreter.Interpret();
    }
}