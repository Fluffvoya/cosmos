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

    // Argument 模块 - overflow (1xxx)
    ArgumentOverflow = 1004,

    // Public Model 模块 (3xxx)
    JsonDeserializeFailed = 3001,
    JsonSerializeFailed = 3002,
    EmptyRequestName = 3003,
    EmptyResponseRequestName = 3005,
    NullInput = 3006,

    // cm-script 模块 (4xxx)
    SyntaxError = 4001,
    MissingFunctionName = 4002,

    // Script-func module (5xxx)
    EmptyArgumentValue = 5001,

    // Process module (6xxx)
    PythonNotFound = 6001,
    ScriptNotFound = 6002,
    PythonProcessCrashed = 6003,
    PythonRuntimeError = 6004,
    ProcessCommunicationError = 6005,

}
