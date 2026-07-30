namespace cosmos_error;

public class ServerException : CosmosException
{
    public ServerException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
