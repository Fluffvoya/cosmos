namespace cosmos_error;

public class InterpreterException : CosmosException
{
    public InterpreterException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
