namespace process;

using func_router;

public class InternalProcess : IProcess
{

    private Router _router;
    private string _func;
    private List<object> _args;

    public InternalProcess(Router router, string func, List<object> args)
    {
        _router = router;
        _func = func;
        _args = args;
    }

    public void Execute()
    {
        _router.Call(_func, _args);
    }
}