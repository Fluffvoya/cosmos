namespace cosmos_error;

public class PublicModelException : CosmosException
{
    public PublicModelException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}