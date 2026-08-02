namespace cosmos_error;

public class ScriptFuncException : CosmosException
{
    public ScriptFuncException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
