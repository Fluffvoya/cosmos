using argument;
using cm_script;
using cosmos_error;
using func_router;

namespace tests;

/// <summary>
/// Unit tests for the cm-script Interpreter.
/// </summary>
public class InterpreterTests
{
    private static Router CreateRouter(string funcName, List<ArgumentType> argTypes,
        Action<List<object>> action)
    {
        var router = new Router();
        router.Add(funcName, new Function(action, argTypes));
        return router;
    }

    // ── COSMOS dispatch ────────────────────────────────────────────

    [Fact]
    public void Interpret_COSMOS_WithNumberArgs_CallsRouterFunction()
    {
        long capturedA = 0, capturedB = 0;
        var router = CreateRouter("add",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number },
            args => { capturedA = (long)args[0]; capturedB = (long)args[1]; });

        var lexer = new Lexer("COSMOS add 3 4");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.Equal(3L, capturedA);
        Assert.Equal(4L, capturedB);
    }

    [Fact]
    public void Interpret_COSMOS_WithStringArg()
    {
        string? captured = null;
        var router = CreateRouter("greet",
            new List<ArgumentType> { ArgumentType.String },
            args => captured = (string)args[0]);

        var lexer = new Lexer("COSMOS greet \"Hello\"");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.Equal("Hello", captured);
    }

    [Fact]
    public void Interpret_COSMOS_WithMixedArgs()
    {
        var results = new List<object>();
        var router = CreateRouter("mixed",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Float, ArgumentType.String },
            args => results.AddRange(args));

        var lexer = new Lexer("COSMOS mixed 1 2.5 \"test\"");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.Equal(3, results.Count);
        Assert.Equal(1L, results[0]);
        Assert.Equal(2.5, results[1]);
        Assert.Equal("test", results[2]);
    }

    [Fact]
    public void Interpret_COSMOS_NoArgs()
    {
        var called = false;
        var router = CreateRouter("noop", new List<ArgumentType>(),
            _ => called = true);

        var lexer = new Lexer("COSMOS noop");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.True(called);
    }

    [Fact]
    public void Interpret_COSMOS_FunctionNotFound_Throws()
    {
        var router = new Router();
        var lexer = new Lexer("COSMOS missing 1");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        Assert.Throws<RouterException>(() => interpreter.Interpret());
    }

    [Fact]
    public void Interpret_COSMOS_TypeMismatch_Throws()
    {
        // Router expects Number, but the token is a quoted string.
        // Interpreter converts via ToString_() and passes a string object
        // to the router, which then throws RouterException on type check.
        var router = CreateRouter("fn",
            new List<ArgumentType> { ArgumentType.Number },
            _ => { });

        var lexer = new Lexer("COSMOS fn \"not_a_number\"");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        Assert.Throws<RouterException>(() => interpreter.Interpret());
    }

    // ── Dollar sign alias ──────────────────────────────────────────

    [Fact]
    public void Interpret_DollarSign_WorksLikeCOSMOS()
    {
        long captured = 0;
        var router = CreateRouter("f",
            new List<ArgumentType> { ArgumentType.Number },
            args => captured = (long)args[0]);

        var lexer = new Lexer("$ f 99");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.Equal(99L, captured);
    }

    // ── Empty / EOF ────────────────────────────────────────────────

    [Fact]
    public void Interpret_EmptySource_DoesNotThrow()
    {
        var router = new Router();
        var lexer = new Lexer("");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        // Should not throw - empty input is a no-op
        interpreter.Interpret();
    }

    [Fact]
    public void Interpret_IdentifierOnly_IsNoOp()
    {
        var router = new Router();
        var lexer = new Lexer("someIdentifier");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        // Identifier at top level is a no-op
        interpreter.Interpret();
    }

    // ── Stub keywords (EXE, LIB, SCRIPT, PYTHON) ──────────────────

    [Fact]
    public void Interpret_EXE_DoesNotThrow()
    {
        var router = new Router();
        var lexer = new Lexer("EXE something");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        // EXE is a stub - should not throw
        interpreter.Interpret();
    }

    [Fact]
    public void Interpret_LIB_DoesNotThrow()
    {
        var router = new Router();
        var lexer = new Lexer("LIB something");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        interpreter.Interpret();
    }

    [Fact]
    public void Interpret_SCRIPT_DoesNotThrow()
    {
        var router = new Router();
        var lexer = new Lexer("SCRIPT something");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        interpreter.Interpret();
    }

    [Fact]
    public void Interpret_PYTHON_DoesNotThrow()
    {
        var router = new Router();
        var lexer = new Lexer("PYTHON something");
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);

        interpreter.Interpret();
    }

    // ── Multi-line with comments ───────────────────────────────────

    [Fact]
    public void Interpret_MultiLineWithComment()
    {
        long result = 0;
        var router = CreateRouter("add",
            new List<ArgumentType> { ArgumentType.Number, ArgumentType.Number },
            args => result = (long)args[0] + (long)args[1]);

        var source = "! comment line\nCOSMOS add 10 20";
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var interpreter = new Interpreter(tokens, router);
        interpreter.Interpret();

        Assert.Equal(30L, result);
    }
}
