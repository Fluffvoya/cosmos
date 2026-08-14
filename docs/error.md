# error Module

## Purpose

Defines the exception hierarchy and error code registry for the entire Cosmos application. All domain-specific exceptions inherit from `CosmosException`, which carries an `ErrorCode` enum value for programmatic error handling.

## Namespace

`cosmos_error`

## Classes

### `ErrorCode` (enum)

Numeric error codes grouped by module. Used as the first argument to every exception constructor.

| Range | Module | Codes |
|-------|--------|-------|
| 1xxx | argument | `ArgumentNull` (1001), `ArgumentFormatInvalid` (1002), `ArgumentTypeMismatch` (1003), `ArgumentOverflow` (1004) |
| 2xxx | func-router | `FunctionNotFound` (2001), `ArgumentCountMismatch` (2002), `ArgumentTypeCheckFailed` (2003) |
| 3xxx | public-model | `JsonDeserializeFailed` (3001), `JsonSerializeFailed` (3002), `EmptyRequestName` (3003), `EmptyResponseRequestName` (3005), `NullInput` (3006) |
| 4xxx | cm-script | `SyntaxError` (4001), `MissingFunctionName` (4002) |
| 5xxx | script-func | `EmptyArgumentValue` (5001) |
| 6xxx | process | `PythonNotFound` (6001), `ScriptNotFound` (6002), `PythonProcessCrashed` (6003), `PythonRuntimeError` (6004), `ProcessCommunicationError` (6005) |

### `CosmosException`

Base exception class. All Cosmos domain exceptions derive from this.

```csharp
public class CosmosException : Exception
{
    public ErrorCode ErrorCode { get; }
    public CosmosException(ErrorCode errorCode, string message) : base(message) { ... }
}
```

### Exception Subclasses

Each subclass inherits `CosmosException` with no additional members unless noted.

| Class | Typical Usage |
|-------|---------------|
| `CosmosArgumentException` | Invalid or malformed argument tokens |
| `RouterException` | Function lookup failure, argument count/type mismatch |
| `ClientException` | Client-side serialization errors |
| `InterpreterException` | Syntax errors and missing function names in cm-script |
| `ServerException` | Server-side communication failures |
| `PublicModelException` | Request/Response serialization or validation failures |
| `ScriptFuncException` | Built-in script function argument errors |
| `ProcessException` | External process failures (exe not found, communication error) |
| `PythonRuntimeException` | Python script runtime errors. **Additional property:** `int ExitCode` (defaults to -1) |

## Dependencies

None. This is a leaf module with no project references.
