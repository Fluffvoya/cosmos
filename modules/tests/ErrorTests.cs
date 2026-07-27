using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the error module: ErrorCode enum, CosmosException,
/// CosmosArgumentException, and RouterException.
/// </summary>
public class ErrorTests
{
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

    [Fact]
    public void CosmosArgumentException_InheritsCosmosException()
    {
        var ex = new CosmosArgumentException(ErrorCode.ArgumentFormatInvalid, "bad format");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ArgumentFormatInvalid, ex.ErrorCode);
        Assert.Equal("bad format", ex.Message);
    }

    [Fact]
    public void RouterException_InheritsCosmosException()
    {
        var ex = new RouterException(ErrorCode.FunctionNotFound, "missing func");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
        Assert.Equal("missing func", ex.Message);
    }

    [Fact]
    public void CosmosException_CanBeCaughtAsBaseException()
    {
        // CosmosArgumentException is a subclass of CosmosException
        CosmosException ex = new CosmosArgumentException(ErrorCode.ArgumentNull, "null arg");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.ArgumentNull, ex.ErrorCode);
    }

    [Fact]
    public void RouterException_CanBeCaughtAsCosmosException()
    {
        // RouterException is a subclass of CosmosException
        CosmosException ex = new RouterException(ErrorCode.FunctionNotFound, "not found");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
    }

    // ── ControllerException (new) ──────────────────────────────────

    [Fact]
    public void ErrorCode_JsonDeserializeFailed_HasExpectedValue()
    {
        Assert.Equal(3001, (int)ErrorCode.JsonDeserializeFailed);
    }

    [Fact]
    public void ControllerException_InheritsCosmosException()
    {
        var ex = new ControllerException(ErrorCode.JsonDeserializeFailed, "json error");

        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
        Assert.Equal("json error", ex.Message);
    }

    [Fact]
    public void ControllerException_CanBeCaughtAsCosmosException()
    {
        CosmosException ex = new ControllerException(ErrorCode.JsonDeserializeFailed, "bad json");
        Assert.IsAssignableFrom<CosmosException>(ex);
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
    }

    [Fact]
    public void ControllerException_CanBeCaughtAsBaseException()
    {
        Exception ex = new ControllerException(ErrorCode.JsonDeserializeFailed, "parse fail");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("parse fail", ex.Message);
    }
}
