namespace cosmos_error;

public class RouterException : CosmosException
{
    public RouterException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
