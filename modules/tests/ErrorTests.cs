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
        var ex = new ServerException(ErrorCode.UnknownRequestType, "unknown type");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.UnknownRequestType, ex.ErrorCode);
        Assert.Equal("unknown type", ex.Message);
    }

    [Fact]
    public void ServerException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ServerException(ErrorCode.UnknownRequestType, "bad type");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.UnknownRequestType, ex.ErrorCode);
    }

    [Fact]
    public void ServerException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ServerException(ErrorCode.UnknownRequestType, "server error");
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

    // ── ErrorCode - Server and Public Model values ──────────────────

    [Fact]
    public void ErrorCode_UnknownRequestType_HasExpectedValue()
    {
        Assert.Equal(5001, (int)ErrorCode.UnknownRequestType);
    }

    [Fact]
    public void ErrorCode_EmptyRequestName_HasExpectedValue()
    {
        Assert.Equal(3003, (int)ErrorCode.EmptyRequestName);
    }

    [Fact]
    public void ErrorCode_InvalidRequestType_HasExpectedValue()
    {
        Assert.Equal(3004, (int)ErrorCode.InvalidRequestType);
    }

    [Fact]
    public void ErrorCode_EmptyReplyRequestName_HasExpectedValue()
    {
        Assert.Equal(3005, (int)ErrorCode.EmptyReplyRequestName);
    }

    [Fact]
    public void ErrorCode_NullInput_HasExpectedValue()
    {
        Assert.Equal(3006, (int)ErrorCode.NullInput);
    }
}
