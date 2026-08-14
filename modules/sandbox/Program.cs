using bridge;
using client;
using cm_script;
using func_router;


public class SandboxServer : IServer
{
    public string Execute(string requests)
    {
        Console.WriteLine(requests);
        return "reply";
    }
}

public class Sandbox
{
    public static async Task Main(string[] args)
    {
        Function func = new Function((IServer server, List<object> args) =>
        {
            Console.WriteLine("func invoke");
            var requests = Client.CreateRequest(Client.MessageBox("a", "b"));
            Console.WriteLine("server:" + server.Execute(requests));
        });

        var source = "COSMOS func";
        var source2 = "PYTHON D:\\program\\a.py";

        var script = new Script(new SandboxServer(), "C:\\Users\\Fluffvoya\\AppData\\Local\\Python\\bin\\python.exe");
        script.AddFunction("func", func);

        await script.Run(source);
        Console.WriteLine("==============");
        await script.Run(source2);
    }
}
