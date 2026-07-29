namespace cosmos_error;

public class ClientException : CosmosException
{
    public ClientException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
