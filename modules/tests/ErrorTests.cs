using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the error module: ErrorCode enum, CosmosException,
/// CosmosArgumentException, RouterException, ClientException, and InterpreterException.
/// </summary>
public class ErrorTests
{
    // ── ErrorCode values ───────────────────────────────────────────

    [Fact]
    public void ErrorCode_ArgumentNull_HasExpectedValue()
    {
        Assert.Equal(1001, (int)ErrorCode.ArgumentNull);
    }

    [Fact]
    public void ErrorCode_ArgumentFormatInvalid_HasExpectedValue()
    {
        Assert.Equal(1002, (int)ErrorCode.ArgumentFormatInvalid);
    }

    [Fact]
    public void ErrorCode_ArgumentTypeMismatch_HasExpectedValue()
    {
        Assert.Equal(1003, (int)ErrorCode.ArgumentTypeMismatch);
    }

    [Fact]
    public void ErrorCode_ArgumentOverflow_HasExpectedValue()
    {
        Assert.Equal(1004, (int)ErrorCode.ArgumentOverflow);
    }

    [Fact]
    public void ErrorCode_FunctionNotFound_HasExpectedValue()
    {
        Assert.Equal(2001, (int)ErrorCode.FunctionNotFound);
    }

    [Fact]
    public void ErrorCode_ArgumentCountMismatch_HasExpectedValue()
    {
        Assert.Equal(2002, (int)ErrorCode.ArgumentCountMismatch);
    }

    [Fact]
    public void ErrorCode_ArgumentTypeCheckFailed_HasExpectedValue()
    {
        Assert.Equal(2003, (int)ErrorCode.ArgumentTypeCheckFailed);
    }

    [Fact]
    public void ErrorCode_JsonDeserializeFailed_HasExpectedValue()
    {
        Assert.Equal(3001, (int)ErrorCode.JsonDeserializeFailed);
    }

    [Fact]
    public void ErrorCode_JsonSerializeFailed_HasExpectedValue()
    {
        Assert.Equal(3002, (int)ErrorCode.JsonSerializeFailed);
    }

    [Fact]
    public void ErrorCode_SyntaxError_HasExpectedValue()
    {
        Assert.Equal(4001, (int)ErrorCode.SyntaxError);
    }

    [Fact]
    public void ErrorCode_MissingFunctionName_HasExpectedValue()
    {
        Assert.Equal(4002, (int)ErrorCode.MissingFunctionName);
    }

    // ── CosmosException ────────────────────────────────────────────

    [Fact]
    public void CosmosException_StoresErrorCodeAndMessage()
    {
        var ex = new CosmosException(ErrorCode.ArgumentNull, "test message");

        Assert.Equal(ErrorCode.ArgumentNull, ex.ErrorCode);
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void CosmosException_IsException()
    {
        var ex = new CosmosException(ErrorCode.FunctionNotFound, "not found");

        Assert.IsAssignableFrom<Exception>(ex);
    }

    // ── CosmosArgumentException ─────────────────────────────────────

    [Fact]
    public void CosmosArgumentException_InheritsCosmosException()
    {
        var ex = new CosmosArgumentException(ErrorCode.ArgumentFormatInvalid, "bad format");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ArgumentFormatInvalid, ex.ErrorCode);
        Assert.Equal("bad format", ex.Message);
    }

    [Fact]
    public void CosmosArgumentException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new CosmosArgumentException(ErrorCode.ArgumentNull, "null arg");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ArgumentNull, ex.ErrorCode);
    }

    // ── RouterException ────────────────────────────────────────────

    [Fact]
    public void RouterException_InheritsCosmosException()
    {
        var ex = new RouterException(ErrorCode.FunctionNotFound, "missing func");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
        Assert.Equal("missing func", ex.Message);
    }

    [Fact]
    public void RouterException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new RouterException(ErrorCode.FunctionNotFound, "not found");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
    }

    // ── ClientException ────────────────────────────────────────────

    [Fact]
    public void ClientException_InheritsCosmosException()
    {
        var ex = new ClientException(ErrorCode.JsonDeserializeFailed, "json error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
        Assert.Equal("json error", ex.Message);
    }

    [Fact]
    public void ClientException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ClientException(ErrorCode.JsonDeserializeFailed, "bad json");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
    }

    [Fact]
    public void ClientException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ClientException(ErrorCode.JsonDeserializeFailed, "parse fail");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("parse fail", ex.Message);
    }

    // ── InterpreterException ───────────────────────────────────────

    [Fact]
    public void InterpreterException_InheritsCosmosException()
    {
        var ex = new InterpreterException(ErrorCode.SyntaxError, "syntax error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.SyntaxError, ex.ErrorCode);
        Assert.Equal("syntax error", ex.Message);
    }

    [Fact]
    public void InterpreterException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new InterpreterException(ErrorCode.MissingFunctionName, "missing name");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    [Fact]
    public void InterpreterException_CanBeCaughtAsBaseException()
    {
        Exception ex = new InterpreterException(ErrorCode.SyntaxError, "bad syntax");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("bad syntax", ex.Message);
    }

    // ── ServerException ────────────────────────────────────────────

    [Fact]
    public void ServerException_InheritsCosmosException()
    {
        var ex = new ServerException(ErrorCode.ProcessCommunicationError, "comm error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ProcessCommunicationError, ex.ErrorCode);
        Assert.Equal("comm error", ex.Message);
    }

    [Fact]
    public void ServerException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ServerException(ErrorCode.ProcessCommunicationError, "bad comm");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ProcessCommunicationError, ex.ErrorCode);
    }

    [Fact]
    public void ServerException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ServerException(ErrorCode.ProcessCommunicationError, "server error");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("server error", ex.Message);
    }

    // ── PublicModelException ───────────────────────────────────────

    [Fact]
    public void PublicModelException_InheritsCosmosException()
    {
        var ex = new PublicModelException(ErrorCode.JsonDeserializeFailed, "json error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
        Assert.Equal("json error", ex.Message);
    }

    [Fact]
    public void PublicModelException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new PublicModelException(ErrorCode.EmptyRequestName, "empty name");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.EmptyRequestName, ex.ErrorCode);
    }

    [Fact]
    public void PublicModelException_CanBeCaughtAsBaseException()
    {
        Exception ex = new PublicModelException(ErrorCode.NullInput, "null input");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("null input", ex.Message);
    }

    // ── ErrorCode - Public Model values ────────────────────────────

    [Fact]
    public void ErrorCode_EmptyRequestName_HasExpectedValue()
    {
        Assert.Equal(3003, (int)ErrorCode.EmptyRequestName);
    }

    [Fact]
    public void ErrorCode_EmptyResponseRequestName_HasExpectedValue()
    {
        Assert.Equal(3005, (int)ErrorCode.EmptyResponseRequestName);
    }

    [Fact]
    public void ErrorCode_NullInput_HasExpectedValue()
    {
        Assert.Equal(3006, (int)ErrorCode.NullInput);
    }

    // ── ErrorCode - Script-func values ─────────────────────────────

    [Fact]
    public void ErrorCode_EmptyArgumentValue_HasExpectedValue()
    {
        Assert.Equal(5001, (int)ErrorCode.EmptyArgumentValue);
    }

    // ── ErrorCode - Process values ─────────────────────────────────

    [Fact]
    public void ErrorCode_PythonNotFound_HasExpectedValue()
    {
        Assert.Equal(6001, (int)ErrorCode.PythonNotFound);
    }

    [Fact]
    public void ErrorCode_ScriptNotFound_HasExpectedValue()
    {
        Assert.Equal(6002, (int)ErrorCode.ScriptNotFound);
    }

    [Fact]
    public void ErrorCode_PythonProcessCrashed_HasExpectedValue()
    {
        Assert.Equal(6003, (int)ErrorCode.PythonProcessCrashed);
    }

    [Fact]
    public void ErrorCode_PythonRuntimeError_HasExpectedValue()
    {
        Assert.Equal(6004, (int)ErrorCode.PythonRuntimeError);
    }

    [Fact]
    public void ErrorCode_ProcessCommunicationError_HasExpectedValue()
    {
        Assert.Equal(6005, (int)ErrorCode.ProcessCommunicationError);
    }

    // ── ScriptFuncException ────────────────────────────────────────

    [Fact]
    public void ScriptFuncException_InheritsCosmosException()
    {
        var ex = new ScriptFuncException(ErrorCode.EmptyArgumentValue, "empty arg");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.EmptyArgumentValue, ex.ErrorCode);
        Assert.Equal("empty arg", ex.Message);
    }

    [Fact]
    public void ScriptFuncException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ScriptFuncException(ErrorCode.EmptyArgumentValue, "null arg");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.EmptyArgumentValue, ex.ErrorCode);
    }

    [Fact]
    public void ScriptFuncException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ScriptFuncException(ErrorCode.EmptyArgumentValue, "script func error");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("script func error", ex.Message);
    }

    // ── ProcessException ───────────────────────────────────────────

    [Fact]
    public void ProcessException_InheritsCosmosException()
    {
        var ex = new ProcessException(ErrorCode.ProcessCommunicationError, "comm error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ProcessCommunicationError, ex.ErrorCode);
        Assert.Equal("comm error", ex.Message);
    }

    [Fact]
    public void ProcessException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ProcessException(ErrorCode.PythonNotFound, "not found");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.PythonNotFound, ex.ErrorCode);
    }

    [Fact]
    public void ProcessException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ProcessException(ErrorCode.ScriptNotFound, "script missing");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("script missing", ex.Message);
    }

    // ── PathException ────────────────────────────────────────────────

    [Fact]
    public void PathException_InheritsCosmosException()
    {
        var ex = new PathException(ErrorCode.PathNullOrEmpty, "path error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
        Assert.Equal("path error", ex.Message);
    }

    [Fact]
    public void PathException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new PathException(ErrorCode.PathNullOrEmpty, "null path");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void PathException_CanBeCaughtAsBaseException()
    {
        Exception ex = new PathException(ErrorCode.PathNullOrEmpty, "empty path");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("empty path", ex.Message);
    }

    // ── ErrorCode - Path values ──────────────────────────────────

    [Fact]
    public void ErrorCode_PathNullOrEmpty_HasExpectedValue()
    {
        Assert.Equal(7001, (int)ErrorCode.PathNullOrEmpty);
    }

    // ── PythonRuntimeException ─────────────────────────────────────

    [Fact]
    public void PythonRuntimeException_InheritsCosmosException()
    {
        var ex = new PythonRuntimeException(ErrorCode.PythonRuntimeError, "runtime error", 1);

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.PythonRuntimeError, ex.ErrorCode);
        Assert.Equal("runtime error", ex.Message);
        Assert.Equal(1, ex.ExitCode);
    }

    [Fact]
    public void PythonRuntimeException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new PythonRuntimeException(ErrorCode.PythonRuntimeError, "error", 2);
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.PythonRuntimeError, ex.ErrorCode);
    }

    [Fact]
    public void PythonRuntimeException_CanBeCaughtAsBaseException()
    {
        Exception ex = new PythonRuntimeException(ErrorCode.PythonProcessCrashed, "crashed");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("crashed", ex.Message);
    }

    [Fact]
    public void PythonRuntimeException_DefaultExitCode_IsNegativeOne()
    {
        var ex = new PythonRuntimeException(ErrorCode.PythonRuntimeError, "error");
        Assert.Equal(-1, ex.ExitCode);
    }

    [Fact]
    public void PythonRuntimeException_StoresExitCode()
    {
        var ex = new PythonRuntimeException(ErrorCode.PythonRuntimeError, "error", 42);
        Assert.Equal(42, ex.ExitCode);
    }
}
