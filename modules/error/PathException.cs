namespace cosmos_error;

public class PathException : CosmosException
{
    public PathException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
