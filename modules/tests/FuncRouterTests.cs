using argument;
using bridge;
using cosmos_error;
using func_router;

namespace tests;

/// <summary>
/// Unit tests for the func-router module: Function and Router.
/// </summary>
public class FuncRouterTests
{
    // Mock IServer for testing
    private class MockServer : IServer
    {
        public string Execute(string requests) => "mock-reply";
    }

    // ── Function ───────────────────────────────────────────────────

    [Fact]
    public void Function_Call_WithCorrectArgs_InvokesAction()
    {
        var called = false;
        var fn = new Function((IServer s, List<object> a) => called = true, new List<ArgumentType>());

        fn.Call(new MockServer(), new List<object>());

        Assert.True(called);
    }

    [Fact]
    public void Function_Call_ReceivesArguments()
    {
        object? capturedNum = null;
        object? capturedStr = null;
        var fn = new Function((IServer s, List<object> args) =>
        {
            capturedNum = args[0];
            capturedStr = args[1];
        }, new List<ArgumentType> { ArgumentType.Number, ArgumentType.String });

        fn.Call(new MockServer(), new List<object> { 42L, "hello" });

        Assert.Equal(42L, capturedNum);
        Assert.Equal("hello", capturedStr);
    }

    [Fact]
    public void Function_Call_ReceivesServerInstance()
    {
        IServer? capturedServer = null;
        var server = new MockServer();
        var fn = new Function((IServer s, List<object> a) => capturedServer = s, new List<ArgumentType>());

        fn.Call(server, new List<object>());

        Assert.Same(server, capturedServer);
    }

    [Fact]
    public void Function_Call_ArgumentCountMismatch_Throws()
    {
        var fn = new Function((IServer s, List<object> a) => { }, new List<ArgumentType> { ArgumentType.Number });

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(new MockServer(), new List<object>()));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Function_Call_TypeMismatch_NumberExpected_Throws()
    {
        var fn = new Function((IServer s, List<object> a) => { }, new List<ArgumentType> { ArgumentType.Number });

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(new MockServer(), new List<object> { "not a number" }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    [Fact]
    public void Function_Call_TypeMismatch_FloatExpected_Throws()
    {
        var fn = new Function((IServer s, List<object> a) => { }, new List<ArgumentType> { ArgumentType.Float });

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(new MockServer(), new List<object> { 42L }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    [Fact]
    public void Function_Call_TypeMismatch_StringExpected_Throws()
    {
        var fn = new Function((IServer s, List<object> a) => { }, new List<ArgumentType> { ArgumentType.String });

        var ex = Assert.Throws<RouterException>(() =>
            fn.Call(new MockServer(), new List<object> { 42L }));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    [Fact]
    public void Function_Call_MultipleCorrectTypes_Succeeds()
    {
        var results = new List<object>();
        var fn = new Function((IServer s, List<object> args) => results.AddRange(args),
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Float, ArgumentType.String });

        fn.Call(new MockServer(), new List<object> { 1L, 2.5, "test" });

        Assert.Equal(3, results.Count);
        Assert.Equal(1L, results[0]);
        Assert.Equal(2.5, results[1]);
        Assert.Equal("test", results[2]);
    }

    // ── Router ─────────────────────────────────────────────────────

    [Fact]
    public void Router_Add_And_Call_ExecutesFunction()
    {
        var called = false;
        var router = new Router(new MockServer());
        var fn = new Function((IServer s, List<object> a) => called = true, new List<ArgumentType>());

        router.Add("myFunc", fn);
        router.Call("myFunc", new List<object>());

        Assert.True(called);
    }

    [Fact]
    public void Router_Call_NonExistentFunction_ThrowsFunctionNotFound()
    {
        var router = new Router(new MockServer());

        var ex = Assert.Throws<RouterException>(() =>
            router.Call("missing", new List<object>()));
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
    }

    [Fact]
    public void Router_Add_OverwritesExistingFunction()
    {
        var router = new Router(new MockServer());
        var firstCalled = false;
        var secondCalled = false;

        router.Add("f", new Function((IServer s, List<object> a) => firstCalled = true, new List<ArgumentType>()));
        router.Add("f", new Function((IServer s, List<object> a) => secondCalled = true, new List<ArgumentType>()));
        router.Call("f", new List<object>());

        Assert.False(firstCalled);
        Assert.True(secondCalled);
    }

    [Fact]
    public void Router_Call_PassesArgumentsToFunction()
    {
        long captured = 0;
        var router = new Router(new MockServer());
        router.Add("add", new Function((IServer s, List<object> args) => captured = (long)args[0] + (long)args[1],
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number }));

        router.Call("add", new List<object> { 3L, 4L });

        Assert.Equal(7L, captured);
    }

    [Fact]
    public void Router_Call_PassesServerToFunction()
    {
        IServer? capturedServer = null;
        var server = new MockServer();
        var router = new Router(server);
        router.Add("fn", new Function((IServer s, List<object> a) => capturedServer = s,
            new List<ArgumentType>()));

        router.Call("fn", new List<object>());

        Assert.Same(server, capturedServer);
    }
}
