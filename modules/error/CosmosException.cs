namespace cosmos_error;

public class CosmosException : Exception
{
    public ErrorCode ErrorCode { get; }

    public CosmosException(ErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
