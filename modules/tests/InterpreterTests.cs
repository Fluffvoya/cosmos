using argument;
using bridge;
using cm_script;
using cosmos_error;
using func_router;

namespace tests;

/// <summary>
/// Unit tests for the cm-script Interpreter.
/// </summary>
public class InterpreterTests
{
    // Mock IServer for testing
    private class MockServer : IServer
    {
        public string Execute(string requests) => "mock-reply";
    }

    private static readonly MockServer _mockServer = new();

    private static Router CreateRouter(string funcName, List<ArgumentType> argTypes,
        Action<IServer, List<object>> action)
    {
        var router = new Router(_mockServer);
        router.Add(funcName, new Function(action, argTypes));
        return router;
    }

    private static Interpreter CreateInterpreter(List<Token> tokens, Router router)
        => new(tokens, router, _mockServer, "python");

    // ── COSMOS dispatch ────────────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_WithNumberArgs_CallsRouterFunction()
    {
        long capturedA = 0, capturedB = 0;
        var router = CreateRouter("add",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number },
            (IServer s, List<object> args) => { capturedA = (long)args[0]; capturedB = (long)args[1]; });

        var lexer = new Lexer("COSMOS add 3 4");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(3L, capturedA);
        Assert.Equal(4L, capturedB);
    }

    [Fact]
    public async Task Interpret_COSMOS_WithStringArg()
    {
        string? captured = null;
        var router = CreateRouter("greet",
            new List<ArgumentType> { ArgumentType.String },
            (IServer s, List<object> args) => captured = (string)args[0]);

        var lexer = new Lexer("COSMOS greet \"Hello\"");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal("Hello", captured);
    }

    [Fact]
    public async Task Interpret_COSMOS_WithMixedArgs()
    {
        var results = new List<object>();
        var router = CreateRouter("mixed",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Float, ArgumentType.String },
            (IServer s, List<object> args) => results.AddRange(args));

        var lexer = new Lexer("COSMOS mixed 1 2.5 \"test\"");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(3, results.Count);
        Assert.Equal(1L, results[0]);
        Assert.Equal(2.5, results[1]);
        Assert.Equal("test", results[2]);
    }

    [Fact]
    public async Task Interpret_COSMOS_NoArgs()
    {
        var called = false;
        var router = CreateRouter("noop", new List<ArgumentType>(),
            (IServer s, List<object> a) => called = true);

        var lexer = new Lexer("COSMOS noop");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.True(called);
    }

    [Fact]
    public async Task Interpret_COSMOS_FunctionNotFound_Throws()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("COSMOS missing 1");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<RouterException>(() => interpreter.Interpret());
    }

    [Fact]
    public async Task Interpret_COSMOS_TypeMismatch_Throws()
    {
        // Router expects Number, but the token is a quoted string.
        // Interpreter converts via ToString_() and passes a string object
        // to the router, which then throws RouterException on type check.
        var router = CreateRouter("fn",
            new List<ArgumentType> { ArgumentType.Number },
            (IServer s, List<object> a) => { });

        var lexer = new Lexer("COSMOS fn \"not_a_number\"");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<RouterException>(() => interpreter.Interpret());
    }

    // ── Dollar sign alias ──────────────────────────────────────────

    [Fact]
    public async Task Interpret_DollarSign_WorksLikeCOSMOS()
    {
        long captured = 0;
        var router = CreateRouter("f",
            new List<ArgumentType> { ArgumentType.Number },
            (IServer s, List<object> args) => captured = (long)args[0]);

        var lexer = new Lexer("$ f 99");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(99L, captured);
    }

    // ── Empty / EOF ────────────────────────────────────────────────

    [Fact]
    public async Task Interpret_EmptySource_DoesNotThrow()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        // Should not throw - empty input is a no-op
        await interpreter.Interpret();
    }

    [Fact]
    public async Task Interpret_IdentifierOnly_IsNoOp()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("someIdentifier");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        // Identifier at top level is a no-op
        await interpreter.Interpret();
    }

    // ── Stub keywords (LIB, SCRIPT) ───────────────────────────────

    [Fact]
    public async Task Interpret_LIB_DoesNotThrow()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("LIB something");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await interpreter.Interpret();
    }

    [Fact]
    public async Task Interpret_SCRIPT_DoesNotThrow()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("SCRIPT something");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await interpreter.Interpret();
    }

    // ── EXE keyword ────────────────────────────────────────────────

    [Fact]
    public async Task Interpret_EXE_NonexistentProgram_ThrowsProcessException()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("EXE something");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<ProcessException>(() => interpreter.Interpret());
    }

    [Fact]
    public async Task Interpret_EXE_NoProgramName_ThrowsException()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("EXE\n");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<Exception>(() => interpreter.Interpret());
    }

    [Fact]
    public async Task Interpret_EXE_AtEOF_ThrowsException()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("EXE");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<Exception>(() => interpreter.Interpret());
    }

    // ── PYTHON keyword ─────────────────────────────────────────────

    [Fact]
    public async Task Interpret_PYTHON_ThrowsWhenInterpreterNotFound()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("PYTHON something");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        // PythonProcess validates the interpreter path on disk,
        // so it throws ProcessException when the path is invalid.
        var ex = await Assert.ThrowsAsync<ProcessException>(() => interpreter.Interpret());
        Assert.Equal(ErrorCode.PythonNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Interpret_PYTHON_NoScriptName_Throws()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("PYTHON\n");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        // Python() throws when no script name follows the PYTHON keyword
        await Assert.ThrowsAsync<Exception>(() => interpreter.Interpret());
    }

    [Fact]
    public async Task Interpret_PYTHON_AtEOF_Throws()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("PYTHON");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        await Assert.ThrowsAsync<Exception>(() => interpreter.Interpret());
    }

    // ── Multi-line with comments ───────────────────────────────────

    [Fact]
    public async Task Interpret_MultiLineWithComment()
    {
        long result = 0;
        var router = CreateRouter("add",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number },
            (IServer s, List<object> args) => result = (long)args[0] + (long)args[1]);

        var source = "! comment line\nCOSMOS add 10 20";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(30L, result);
    }

    // ── Server interaction ─────────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_ServerIsAvailableInFunction()
    {
        IServer? capturedServer = null;
        var router = CreateRouter("check",
            new List<ArgumentType>(),
            (IServer s, List<object> a) => capturedServer = s);

        var lexer = new Lexer("COSMOS check");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.NotNull(capturedServer);
        Assert.IsType<MockServer>(capturedServer);
    }

    // ── Missing function name ──────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_NoFunctionName_ThrowsMissingFunctionName()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("COSMOS\n");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        var ex = await Assert.ThrowsAsync<InterpreterException>(() => interpreter.Interpret());
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    [Fact]
    public async Task Interpret_COSMOS_AtEOF_ThrowsMissingFunctionName()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("COSMOS");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        var ex = await Assert.ThrowsAsync<InterpreterException>(() => interpreter.Interpret());
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    [Fact]
    public async Task Interpret_COSMOS_OnlyNewlines_ThrowsMissingFunctionName()
    {
        var router = new Router(_mockServer);
        var lexer = new Lexer("COSMOS\n\n");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);

        var ex = await Assert.ThrowsAsync<InterpreterException>(() => interpreter.Interpret());
        Assert.Equal(ErrorCode.MissingFunctionName, ex.ErrorCode);
    }

    // ── Float arguments ────────────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_WithFloatArg()
    {
        double captured = 0;
        var router = CreateRouter("setFloat",
            new List<ArgumentType> { ArgumentType.Float },
            (IServer s, List<object> args) => captured = (double)args[0]);

        var lexer = new Lexer("COSMOS setFloat 3.14");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(3.14, captured, 1e-10);
    }

    [Fact]
    public async Task Interpret_COSMOS_WithNegativeFloatArg()
    {
        double captured = 0;
        var router = CreateRouter("setFloat",
            new List<ArgumentType> { ArgumentType.Float },
            (IServer s, List<object> args) => captured = (double)args[0]);

        var lexer = new Lexer("COSMOS setFloat -2.5");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(-2.5, captured, 1e-10);
    }

    // ── Multiple arguments ─────────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_WithMultipleStringArgs()
    {
        var results = new List<string>();
        var router = CreateRouter("concat",
            new List<ArgumentType> { ArgumentType.String, ArgumentType.String, ArgumentType.String },
            (IServer s, List<object> args) =>
            {
                results.Add((string)args[0]);
                results.Add((string)args[1]);
                results.Add((string)args[2]);
            });

        var lexer = new Lexer("COSMOS concat \"a\" \"b\" \"c\"");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(3, results.Count);
        Assert.Equal("a", results[0]);
        Assert.Equal("b", results[1]);
        Assert.Equal("c", results[2]);
    }

    // ── Negative number arguments ──────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_WithNegativeNumberArg()
    {
        long captured = 0;
        var router = CreateRouter("setNum",
            new List<ArgumentType> { ArgumentType.Number },
            (IServer s, List<object> args) => captured = (long)args[0]);

        var lexer = new Lexer("COSMOS setNum -42");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(-42L, captured);
    }

    // ── Zero arguments ─────────────────────────────────────────────

    [Fact]
    public async Task Interpret_COSMOS_WithZeroArg()
    {
        long captured = 1;
        var router = CreateRouter("setZero",
            new List<ArgumentType> { ArgumentType.Number },
            (IServer s, List<object> args) => captured = (long)args[0]);

        var lexer = new Lexer("COSMOS setZero 0");
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(0L, captured);
    }
    // Multi-line execution

    [Fact]
    public async Task Interpret_MultipleCOSMOSStatements_AllExecuted()
    {
        var callOrder = new List<string>();
        var router = new Router(_mockServer);
        router.Add("log", new Function(
            (IServer s, List<object> args) => callOrder.Add((string)args[0]),
            new List<ArgumentType> { ArgumentType.String }));

        var source = "COSMOS log \"first\"\nCOSMOS log \"second\"";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("first", callOrder[0]);
        Assert.Equal("second", callOrder[1]);
    }

    [Fact]
    public async Task Interpret_ThreeCOSMOSStatements_AllExecuted()
    {
        long total = 0;
        var router = new Router(_mockServer);
        router.Add("add", new Function(
            (IServer s, List<object> args) => total += (long)args[0],
            new List<ArgumentType> { ArgumentType.Number }));

        var source = "COSMOS add 10\nCOSMOS add 20\nCOSMOS add 30";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(60L, total);
    }

    [Fact]
    public async Task Interpret_MultiLineWithBlankLines_AllExecuted()
    {
        var callCount = 0;
        var router = new Router(_mockServer);
        router.Add("noop", new Function(
            (IServer s, List<object> a) => callCount++,
            new List<ArgumentType>()));

        var source = "COSMOS noop\n\n\nCOSMOS noop";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Interpret_MultiLineWithCommentsBetween_AllExecuted()
    {
        var callOrder = new List<string>();
        var router = new Router(_mockServer);
        router.Add("log", new Function(
            (IServer s, List<object> args) => callOrder.Add((string)args[0]),
            new List<ArgumentType> { ArgumentType.String }));

        var source = "COSMOS log \"a\"\n! comment\nCOSMOS log \"b\"";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("a", callOrder[0]);
        Assert.Equal("b", callOrder[1]);
    }

    [Fact]
    public async Task Interpret_MultiLineWithDollarAlias_AllExecuted()
    {
        var callCount = 0;
        var router = new Router(_mockServer);
        router.Add("noop", new Function(
            (IServer s, List<object> a) => callCount++,
            new List<ArgumentType>()));

        var source = "$ noop\n$ noop\n$ noop";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task Interpret_MixedKeywordsAndAliases_AllExecuted()
    {
        var callCount = 0;
        var router = new Router(_mockServer);
        router.Add("noop", new Function(
            (IServer s, List<object> a) => callCount++,
            new List<ArgumentType>()));

        var source = "COSMOS noop\n$ noop";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = CreateInterpreter(tokens, router);
        await interpreter.Interpret();

        Assert.Equal(2, callCount);
    }
}