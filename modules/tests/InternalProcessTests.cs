using argument;
using bridge;
using cosmos_error;
using func_router;
using process;

namespace tests;

/// <summary>
/// Unit tests for the process module: InternalProcess.
/// </summary>
public class InternalProcessTests
{
    // Mock IServer for testing
    private class MockServer : IServer
    {
        public string Execute(string requests) => "mock-reply";
    }

    // ── InternalProcess.Execute ────────────────────────────────────

    [Fact]
    public void Execute_CallsRouterFunction()
    {
        var called = false;
        var router = new Router(new MockServer());
        router.Add("testFunc", new Function(
            (IServer s, List<object> a) => called = true,
            new List<ArgumentType>()));

        var process = new InternalProcess(router, "testFunc", new List<object>());
        process.Execute();

        Assert.True(called);
    }

    [Fact]
    public void Execute_PassesArgumentsToRouter()
    {
        long capturedA = 0, capturedB = 0;
        var router = new Router(new MockServer());
        router.Add("add", new Function(
            (IServer s, List<object> args) => { capturedA = (long)args[0]; capturedB = (long)args[1]; },
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number }));

        var process = new InternalProcess(router, "add", new List<object> { 10L, 20L });
        process.Execute();

        Assert.Equal(10L, capturedA);
        Assert.Equal(20L, capturedB);
    }

    [Fact]
    public void Execute_PassesCorrectFunctionName()
    {
        var router = new Router(new MockServer());
        router.Add("myFunc", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType>()));

        // Verify the correct function is called by checking which one fires
        var myFuncCalled = false;
        var otherFuncCalled = false;
        router.Add("myFunc", new Function(
            (IServer s, List<object> a) => myFuncCalled = true,
            new List<ArgumentType>()));
        router.Add("otherFunc", new Function(
            (IServer s, List<object> a) => otherFuncCalled = true,
            new List<ArgumentType>()));

        var process = new InternalProcess(router, "myFunc", new List<object>());
        process.Execute();

        Assert.True(myFuncCalled);
        Assert.False(otherFuncCalled);
    }

    [Fact]
    public void Execute_PropagatesRouterException_OnFunctionNotFound()
    {
        var router = new Router(new MockServer());

        var process = new InternalProcess(router, "nonexistent", new List<object>());

        var ex = Assert.Throws<RouterException>(() => process.Execute());
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
    }

    [Fact]
    public void Execute_PropagatesRouterException_OnArgumentCountMismatch()
    {
        var router = new Router(new MockServer());
        router.Add("fn", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType> { ArgumentType.Number }));

        var process = new InternalProcess(router, "fn", new List<object>());

        var ex = Assert.Throws<RouterException>(() => process.Execute());
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Execute_PropagatesRouterException_OnTypeMismatch()
    {
        var router = new Router(new MockServer());
        router.Add("fn", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType> { ArgumentType.Number }));

        var process = new InternalProcess(router, "fn", new List<object> { "not a number" });

        var ex = Assert.Throws<RouterException>(() => process.Execute());
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    [Fact]
    public void Execute_WithMixedArgumentTypes()
    {
        var results = new List<object>();
        var router = new Router(new MockServer());
        router.Add("mixed", new Function(
            (IServer s, List<object> args) => results.AddRange(args),
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Float, ArgumentType.String }));

        var process = new InternalProcess(router, "mixed",
            new List<object> { 42L, 3.14, "hello" });
        process.Execute();

        Assert.Equal(3, results.Count);
        Assert.Equal(42L, results[0]);
        Assert.Equal(3.14, results[1]);
        Assert.Equal("hello", results[2]);
    }

    [Fact]
    public void Execute_WithNoArguments()
    {
        var called = false;
        var router = new Router(new MockServer());
        router.Add("noop", new Function(
            (IServer s, List<object> a) => called = true,
            new List<ArgumentType>()));

        var process = new InternalProcess(router, "noop", new List<object>());
        process.Execute();

        Assert.True(called);
    }

    [Fact]
    public void Execute_IsInstanceOfInternalProcess()
    {
        var router = new Router(new MockServer());
        router.Add("fn", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType>()));

        var process = new InternalProcess(router, "fn", new List<object>());

        Assert.IsType<InternalProcess>(process);
    }
}
