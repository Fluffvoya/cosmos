namespace cosmos_error;

/// <summary>
/// Represents an error originating from the Python runtime
/// e.g. Python script crash, Python runtime exception
/// </summary>
public class PythonRuntimeException : CosmosException
{
    public int ExitCode { get; }

    public PythonRuntimeException(ErrorCode errorCode, string message, int exitCode = -1)
        : base(errorCode, message)
    {
        ExitCode = exitCode;
    }
}
