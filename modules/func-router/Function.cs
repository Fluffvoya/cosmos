using argument;
using cosmos_error;

namespace func_router;

public class Function
{
    public Action<List<object>> func;
    public List<ArgumentType> argsType;

    public Function(Action<List<object>> func_, params List<ArgumentType> argsType_)
    {
        func = func_;
        argsType = argsType_;
    }
    public void Call(List<object> args)
    {
        Check(args);
        func.Invoke(args);
    }

    // throw when fatal
    private void Check(List<object> args)
    {
        if (args.Count != argsType.Count)
            throw new RouterException(ErrorCode.ArgumentCountMismatch,
                $"Argument count mismatch: expected {argsType.Count}, got {args.Count}");
        var count = args.Count;
        for (int i = 0; i < count; i++)
        {
            switch (argsType[i])
            {
                case ArgumentType.Number:
                    if (args[i] is not long)
                        throw new RouterException(ErrorCode.ArgumentTypeCheckFailed,
                            $"Argument[{i}] type mismatch: expected Number, got {args[i].GetType().Name}");
                    break;
                case ArgumentType.Float:
                    if (args[i] is not double)
                        throw new RouterException(ErrorCode.ArgumentTypeCheckFailed,
                            $"Argument[{i}] type mismatch: expected Float, got {args[i].GetType().Name}");
                    break;
                case ArgumentType.String:
                    if (args[i] is not string)
                        throw new RouterException(ErrorCode.ArgumentTypeCheckFailed,
                            $"Argument[{i}] type mismatch: expected String, got {args[i].GetType().Name}");
                    break;
                default:
                    throw new RouterException(ErrorCode.ArgumentTypeCheckFailed,
                        $"Unknown argument type at index {i}: {argsType[i]}");
            }
        }

    }

}