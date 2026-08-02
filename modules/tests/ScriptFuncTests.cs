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

    // ── ShowWindow ─────────────────────────────────────────────────

    [Fact]
    public void ShowWindow_Function_Call_SendsCorrectRequest()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.ShowWindow;

        fn.Call(server, new List<object> { "myWindow", "Hello!" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("ShowWindow", request.request);
        Assert.Equal(2, request.args.Count);
        Assert.Equal("myWindow", request.args[0]);
        Assert.Equal("Hello!", request.args[1]);
    }

    [Fact]
    public void ShowWindow_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.ShowWindow;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { "onlyOne" }));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void ShowWindow_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.ShowWindow;

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

        fn.Call(server, new List<object> { "Error", "script", "Something went wrong" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("Log", request.request);
        Assert.Equal(3, request.args.Count);
        Assert.Equal("Error", request.args[0]);
        Assert.Equal("script", request.args[1]);
        Assert.Equal("Something went wrong", request.args[2]);
    }

    [Fact]
    public void Log_Function_WrongArgCount_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Log;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { "Error", "script" }));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Log_Function_WrongArgType_Throws()
    {
        var server = new CapturingServer();
        var fn = ScriptFunctions.Log;

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(server, new List<object> { 42L, "script", "content" }));
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

    // ── Router integration ─────────────────────────────────────────

    [Fact]
    public void Functions_CanBeAddedToRouter_AndCalled()
    {
        var server = new CapturingServer();
        var router = new Router(server);

        router.Add("ShowWindow", ScriptFunctions.ShowWindow);
        router.Add("Log", ScriptFunctions.Log);
        router.Add("GetUserName", ScriptFunctions.GetUserName);

        router.Call("ShowWindow", new List<object> { "win1", "msg1" });

        Assert.NotNull(server.LastRequestJson);
        var request = Request.Deserialize(server.LastRequestJson);
        Assert.NotNull(request);
        Assert.Equal("ShowWindow", request.request);
    }
}
