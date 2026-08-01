using argument;
using bridge;
using cm_script;
using cosmos_error;
using func_router;

namespace tests;

/// <summary>
/// Unit tests for the cm-script Script class.
/// </summary>
public class ScriptTests
{
    // Mock IServer for testing
    private class MockServer : IServer
    {
        public string Execute(string requests) => "mock-reply";
    }

    private static readonly MockServer _mockServer = new();

    private static Script CreateScript() => new(_mockServer, "python");

    // ── Constructor ────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var script = new Script(_mockServer, "python");

        Assert.NotNull(script);
        Assert.IsType<Script>(script);
    }

    // ── AddFunction ────────────────────────────────────────────────

    [Fact]
    public void AddFunction_RegistersFunction_DoesNotThrow()
    {
        var script = CreateScript();

        script.AddFunction("test", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType>()));
    }

    [Fact]
    public async Task AddFunction_WithNoArgs_FunctionIsCallable()
    {
        var called = false;
        var script = CreateScript();

        script.AddFunction("noop", new Function(
            (IServer s, List<object> a) => called = true,
            new List<ArgumentType>()));

        await script.RunScript("COSMOS noop");

        Assert.True(called);
    }

    [Fact]
    public async Task AddFunction_WithNumberArgs_FunctionReceivesArgs()
    {
        long capturedA = 0, capturedB = 0;
        var script = CreateScript();

        script.AddFunction("add", new Function(
            (IServer s, List<object> args) => { capturedA = (long)args[0]; capturedB = (long)args[1]; },
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number }));

        await script.RunScript("COSMOS add 3 4");

        Assert.Equal(3L, capturedA);
        Assert.Equal(4L, capturedB);
    }

    [Fact]
    public async Task AddFunction_WithStringArg_FunctionReceivesArg()
    {
        string? captured = null;
        var script = CreateScript();

        script.AddFunction("greet", new Function(
            (IServer s, List<object> args) => captured = (string)args[0],
            new List<ArgumentType> { ArgumentType.String }));

        await script.RunScript("COSMOS greet \"Hello\"");

        Assert.Equal("Hello", captured);
    }

    [Fact]
    public async Task AddFunction_WithMixedArgs_FunctionReceivesAllArgs()
    {
        var results = new List<object>();
        var script = CreateScript();

        script.AddFunction("mixed", new Function(
            (IServer s, List<object> args) => results.AddRange(args),
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Float, ArgumentType.String }));

        await script.RunScript("COSMOS mixed 1 2.5 \"test\"");

        Assert.Equal(3, results.Count);
        Assert.Equal(1L, results[0]);
        Assert.Equal(2.5, results[1]);
        Assert.Equal("test", results[2]);
    }

    [Fact]
    public async Task AddFunction_MultipleFunctions_EachCallableSeparately()
    {
        var func1Called = false;
        var func2Called = false;
        var script = CreateScript();

        script.AddFunction("f1", new Function(
            (IServer s, List<object> a) => func1Called = true,
            new List<ArgumentType>()));
        script.AddFunction("f2", new Function(
            (IServer s, List<object> a) => func2Called = true,
            new List<ArgumentType>()));

        // Interpreter processes one statement per RunScript call
        await script.RunScript("COSMOS f1");
        await script.RunScript("COSMOS f2");

        Assert.True(func1Called);
        Assert.True(func2Called);
    }

    [Fact]
    public async Task AddFunction_OverwritesExistingFunction_LastOneWins()
    {
        var firstCalled = false;
        var secondCalled = false;
        var script = CreateScript();

        script.AddFunction("f", new Function(
            (IServer s, List<object> a) => firstCalled = true,
            new List<ArgumentType>()));
        script.AddFunction("f", new Function(
            (IServer s, List<object> a) => secondCalled = true,
            new List<ArgumentType>()));

        await script.RunScript("COSMOS f");

        Assert.False(firstCalled);
        Assert.True(secondCalled);
    }

    // ── RunScript ──────────────────────────────────────────────────

    [Fact]
    public async Task RunScript_EmptySource_DoesNotThrow()
    {
        var script = CreateScript();

        await script.RunScript("");
    }

    [Fact]
    public async Task RunScript_WhitespaceOnly_DoesNotThrow()
    {
        var script = CreateScript();

        await script.RunScript("   \n  \n  ");
    }

    [Fact]
    public async Task RunScript_CommentOnly_DoesNotThrow()
    {
        var script = CreateScript();

        await script.RunScript("! this is a comment");
    }

    [Fact]
    public async Task RunScript_IdentifierOnly_IsNoOp()
    {
        var script = CreateScript();

        // A bare identifier at top level is a no-op
        await script.RunScript("someIdentifier");
    }

    [Fact]
    public async Task RunScript_DollarSignAlias_WorksLikeCOSMOS()
    {
        long captured = 0;
        var script = CreateScript();

        script.AddFunction("f", new Function(
            (IServer s, List<object> args) => captured = (long)args[0],
            new List<ArgumentType> { ArgumentType.Number }));

        await script.RunScript("$ f 99");

        Assert.Equal(99L, captured);
    }

    [Fact]
    public async Task RunScript_MultiLineWithComments_ExecutesCorrectly()
    {
        long result = 0;
        var script = CreateScript();

        script.AddFunction("add", new Function(
            (IServer s, List<object> args) => result = (long)args[0] + (long)args[1],
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number }));

        await script.RunScript("! comment line\nCOSMOS add 10 20");

        Assert.Equal(30L, result);
    }

    [Fact]
    public async Task RunScript_FunctionCalledViaRouter_ReceivesServerInstance()
    {
        IServer? capturedServer = null;
        var script = CreateScript();

        script.AddFunction("check", new Function(
            (IServer s, List<object> a) => capturedServer = s,
            new List<ArgumentType>()));

        await script.RunScript("COSMOS check");

        Assert.NotNull(capturedServer);
        Assert.Same(_mockServer, capturedServer);
    }

    // ── RunScript error propagation ────────────────────────────────

    [Fact]
    public async Task RunScript_UnregisteredFunction_ThrowsRouterException()
    {
        var script = CreateScript();

        var ex = await Assert.ThrowsAsync<RouterException>(() => script.RunScript("COSMOS missing"));
        Assert.Equal(ErrorCode.FunctionNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task RunScript_ArgumentCountMismatch_ThrowsRouterException()
    {
        var script = CreateScript();

        script.AddFunction("fn", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType> { ArgumentType.Number }));

        var ex = await Assert.ThrowsAsync<RouterException>(() => script.RunScript("COSMOS fn"));
        Assert.Equal(ErrorCode.ArgumentCountMismatch, ex.ErrorCode);
    }

    [Fact]
    public async Task RunScript_TypeMismatch_ThrowsRouterException()
    {
        var script = CreateScript();

        script.AddFunction("fn", new Function(
            (IServer s, List<object> a) => { },
            new List<ArgumentType> { ArgumentType.Number }));

        var ex = await Assert.ThrowsAsync<RouterException>(() => script.RunScript("COSMOS fn \"not_a_number\""));
        Assert.Equal(ErrorCode.ArgumentTypeCheckFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task RunScript_MissingFunctionName_ThrowsInterpreterException()
    {
        var script = CreateScript();

        var ex = await Assert.ThrowsAsync<InterpreterException>(() => script.RunScript("COSMOS\n"));
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    [Fact]
    public async Task RunScript_COSMOSAtEOF_ThrowsInterpreterException()
    {
        var script = CreateScript();

        var ex = await Assert.ThrowsAsync<InterpreterException>(() => script.RunScript("COSMOS"));
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    // ── RunScript with EXE/PYTHON keywords ─────────────────────────

    [Fact]
    public async Task RunScript_EXE_NonexistentProgram_ThrowsProcessException()
    {
        var script = CreateScript();

        await Assert.ThrowsAsync<ProcessException>(() => script.RunScript("EXE something"));
    }

    [Fact]
    public async Task RunScript_PYTHON_InvalidInterpreter_ThrowsProcessException()
    {
        var script = CreateScript();

        var ex = await Assert.ThrowsAsync<ProcessException>(() => script.RunScript("PYTHON something"));
        Assert.Equal(ErrorCode.PythonNotFound, ex.ErrorCode);
    }

    // ── RunScript with LIB/SCRIPT stubs ────────────────────────────

    [Fact]
    public async Task RunScript_LIB_DoesNotThrow()
    {
        var script = CreateScript();

        await script.RunScript("LIB something");
    }

    [Fact]
    public async Task RunScript_SCRIPT_DoesNotThrow()
    {
        var script = CreateScript();

        await script.RunScript("SCRIPT something");
    }

    // ── Multiple RunScript calls on same Script instance ───────────

    [Fact]
    public async Task RunScript_CalledMultipleTimes_FunctionsPersist()
    {
        long total = 0;
        var script = CreateScript();

        script.AddFunction("add", new Function(
            (IServer s, List<object> args) => total += (long)args[0],
            new List<ArgumentType> { ArgumentType.Number }));

        await script.RunScript("COSMOS add 10");
        await script.RunScript("COSMOS add 20");

        Assert.Equal(30L, total);
    }

    [Fact]
    public async Task RunScript_AddFunctionBetweenCalls_NewFunctionAvailable()
    {
        var func1Called = false;
        var func2Called = false;
        var script = CreateScript();

        script.AddFunction("f1", new Function(
            (IServer s, List<object> a) => func1Called = true,
            new List<ArgumentType>()));

        await script.RunScript("COSMOS f1");

        script.AddFunction("f2", new Function(
            (IServer s, List<object> a) => func2Called = true,
            new List<ArgumentType>()));

        await script.RunScript("COSMOS f2");

        Assert.True(func1Called);
        Assert.True(func2Called);
    }
}
