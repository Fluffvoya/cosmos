using bridge;
using cosmos_error;

namespace func_router;

public class Router
{
    private Dictionary<string, Function> _functions;
    private IServer _server;

    public Router(IServer server)
    {
        _functions = new Dictionary<string, Function>();
        _server = server;
    }

    public void Add(string name, Function func)
    {
        _functions[name] = func;
    }

    public void Call(string func, List<object> args)
    {
        if (_functions.TryGetValue(func, out var value))
            value.Call(_server, args);
        else
            throw new RouterException(ErrorCode.FunctionNotFound,
                $"Function not found: '{func}'");
    }
}
