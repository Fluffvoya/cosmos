using argument;
using bridge;
using client;
using cosmos_error;
using func_router;
using public_model;
using script_func;

namespace tests;

/// <summary>
/// Unit tests for the script-func module: Function objects wrapping Client interfaces.
/// </summary>
public class ScriptFuncTests
{
    // Mock IServer that captures the request JSON sent via Execute
    private class CapturingServer : IServer
    {
        public string? LastRequestJson { get; private set; }

        public string Execute(string requests)
        {
            LastRequestJson = requests;
            // Return a valid Response JSON so Client.GetResponse can parse it
            return """{"request":"test","message":"ok"}""";
        }
    }

    // ── MessageBox ─────────────────────────────────────────────────

    [Fact]
    public void MessageBox_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.MessageBox;

        fn.Call(server, new List<object> { "myWindow", "Hello!" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("MessageBox", request.request);
        Assert.Equal(2, request.args.Count);
        Assert.Equal("myWindow", request.args[0]);
        Assert.Equal("Hello!", request.args[1]);
    }

    [Fact]
    public void MessageBox_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.MessageBox;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { "onlyOne" }));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void MessageBox_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.MessageBox;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { 42L, "message" }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    // ── Log ────────────────────────────────────────────────────────

    [Fact]
    public void Log_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Log;

        fn.Call(server, new List<object> { "Something happened" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Log", request.request);
        Assert.Single(request.args);
        Assert.Equal("Something happened", request.args[0]);
    }

    [Fact]
    public void Log_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Log;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object>()));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Log_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Log;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { 42L }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    // ── Warning ────────────────────────────────────────────────────

    [Fact]
    public void Warning_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Warning;

        fn.Call(server, new List<object> { "Something looks off" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Warning", request.request);
        Assert.Single(request.args);
        Assert.Equal("Something looks off", request.args[0]);
    }

    [Fact]
    public void Warning_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Warning;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object>()));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Warning_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Warning;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { 42L }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    // ── Error ──────────────────────────────────────────────────────

    [Fact]
    public void Error_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Error;

        fn.Call(server, new List<object> { "Something went wrong" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Error", request.request);
        Assert.Single(request.args);
        Assert.Equal("Something went wrong", request.args[0]);
    }

    [Fact]
    public void Error_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Error;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object>()));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Error_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Error;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { 42L }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    // ── GetUserName ────────────────────────────────────────────────

    [Fact]
    public void GetUserName_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.GetUserName;

        fn.Call(server, new List<object>());

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("GetUserName", request.request);
        Assert.Empty(request.args);
    }

    [Fact]
    public void GetUserName_Function_WithArgs_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.GetUserName;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { "unexpected" }));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    // ── MessageBar ──────────────────────────────────────────────────

    [Fact]
    public void MessageBar_Function_InvalidLevel_ThrowsScriptFuncException()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.MessageBar;

        var ex = Assert.Throws<ScriptFuncException>(() =>
            fn.Call(server, new List<object> { "hello", "critical" }));
        Assert.Equal(ErrorCode.InvalidArgumentValue, ex.ErrorCode);
    }

    [Fact]
    public void MessageBar_Function_ValidLevel_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.MessageBar;

        fn.Call(server, new List<object> { "hello", "warning" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("MessageBar", request.request);
        Assert.Equal("hello", request.args[0]);
        Assert.Equal("warning", request.args[1]);
    }

    // ── Router integration ─────────────────────────────────────────

    [Fact]
    public void Functions_CanBeAddedToRouter_AndCalled()
    {
        var server = new CapturingServer();
        var router = new Router(server);

        router.Add("MessageBox", ScriptFunctions.MessageBox);
        router.Add("Log", ScriptFunctions.Log);
        router.Add("Warning", ScriptFunctions.Warning);
        router.Add("Error", ScriptFunctions.Error);
        router.Add("GetUserName", ScriptFunctions.GetUserName);

        router.Call("MessageBox", new List<object> { "win1", "msg1" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("MessageBox", request.request);
    }

    [Fact]
    public void Warning_Function_CanBeAddedToRouter_AndCalled()
    {
        var server = new CapturingServer();
        var router = new Router(server);

        router.Add("Warning", ScriptFunctions.Warning);
        router.Call("Warning", new List<object> { "test warning" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Warning", request.request);
        Assert.Equal("test warning", request.args[0]);
    }

    [Fact]
    public void Error_Function_CanBeAddedToRouter_AndCalled()
    {
        var server = new CapturingServer();
        var router = new Router(server);

        router.Add("Error", ScriptFunctions.Error);
        router.Call("Error", new List<object> { "test error" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Error", request.request);
        Assert.Equal("test error", request.args[0]);
    }
}
