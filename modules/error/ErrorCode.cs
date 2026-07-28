namespace cosmos_error;

public enum ErrorCode
{
    // Argument 模块 (1xxx)
    ArgumentNull = 1001,
    ArgumentFormatInvalid = 1002,
    ArgumentTypeMismatch = 1003,

    // Router 模块 (2xxx)
    FunctionNotFound = 2001,
    ArgumentCountMismatch = 2002,
    ArgumentTypeCheckFailed = 2003,

    // Client 模块 (3xxx)
    JsonDeserializeFailed = 3001,
}
