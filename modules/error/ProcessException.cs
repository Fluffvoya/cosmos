namespace cosmos_error;

/// <summary>
/// Represents a program-level process error (not a Python internal error)
/// e.g. Python interpreter not found, script file not found, process communication failure
/// </summary>
public class ProcessException : CosmosException
{
    public ProcessException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
