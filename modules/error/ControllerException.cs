namespace cosmos_error;

public class ControllerException : CosmosException
{
    public ControllerException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
