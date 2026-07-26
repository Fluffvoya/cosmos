namespace cosmos_error;

public class CosmosArgumentException : CosmosException
{
    public CosmosArgumentException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
