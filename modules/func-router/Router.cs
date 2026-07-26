using cosmos_error;

namespace func_router;

public class Router
{
    private Dictionary<string, Function> _functions;

    public Router()
    {
        _functions = new Dictionary<string, Function>();
    }

    public void Add(string name, Function func)
    {
        _functions[name] = func;
    }

    public void Call(string func, List<object> args)
    {
        if (_functions.TryGetValue(func, out var value))
            value.Call(args);
        else
            throw new RouterException(ErrorCode.FunctionNotFound,
                $"Function not found: '{func}'");
    }
}
