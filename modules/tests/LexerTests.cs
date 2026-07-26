using cm_script;

namespace tests;

/// <summary>
/// Unit tests for the cm-script Lexer.
/// </summary>
public class LexerTests
{
    // ── Basic tokenization ─────────────────────────────────────────

    [Fact]
    public void Tokenize_EmptySource_ReturnsOnlyEOF()
    {
        var lexer = new Lexer("");
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].tokenType);
    }

    [Fact]
    public void Tokenize_SingleCOSMOS_ProducesCorrectTokens()
    {
        var lexer = new Lexer("COSMOS");
        var tokens = lexer.Tokenize();

        // COSMOS, EOF
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal("COSMOS", tokens[0].tk);
        Assert.Equal(TokenType.EOF, tokens[1].tokenType);
    }

    [Fact]
    public void Tokenize_DollarSign_ProducesST_Cosmos()
    {
        var lexer = new Lexer("$");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal("$", tokens[0].tk);
    }

    [Fact]
    public void Tokenize_EXE_Keyword()
    {
        var lexer = new Lexer("EXE");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Exe, tokens[0].tokenType);
    }

    [Fact]
    public void Tokenize_Hash_ProducesST_Exe()
    {
        var lexer = new Lexer("#");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Exe, tokens[0].tokenType);
        Assert.Equal("#", tokens[0].tk);
    }

    [Fact]
    public void Tokenize_LIB_Keyword()
    {
        var lexer = new Lexer("LIB");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Lib, tokens[0].tokenType);
    }

    [Fact]
    public void Tokenize_AtSign_ProducesST_Lib()
    {
        var lexer = new Lexer("@");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Lib, tokens[0].tokenType);
        Assert.Equal("@", tokens[0].tk);
    }

    [Fact]
    public void Tokenize_SCRIPT_Keyword()
    {
        var lexer = new Lexer("SCRIPT");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Script, tokens[0].tokenType);
    }

    [Fact]
    public void Tokenize_Ampersand_ProducesST_Script()
    {
        var lexer = new Lexer("&");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Script, tokens[0].tokenType);
        Assert.Equal("&", tokens[0].tk);
    }

    [Fact]
    public void Tokenize_PYTHON_Keyword()
    {
        var lexer = new Lexer("PYTHON");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ST_Python, tokens[0].tokenType);
    }

    // ── Identifiers ────────────────────────────────────────────────

    [Fact]
    public void Tokenize_Identifier()
    {
        var lexer = new Lexer("myFunc");
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].tokenType);
        Assert.Equal("myFunc", tokens[0].tk);
    }

    // ── String literals ────────────────────────────────────────────

    [Fact]
    public void Tokenize_DoubleQuotedString()
    {
        var lexer = new Lexer("\"hello world\"");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.String, tokens[0].tokenType);
        Assert.Equal("\"hello world\"", tokens[0].tk);
    }

    [Fact]
    public void Tokenize_StringPreservesQuotes()
    {
        var lexer = new Lexer("\"test\"");
        var tokens = lexer.Tokenize();

        // Quotes are included in the token text
        Assert.Equal("\"test\"", tokens[0].tk);
    }

    // ── Comments ───────────────────────────────────────────────────

    [Fact]
    public void Tokenize_Comment_IsSkipped()
    {
        var lexer = new Lexer("! this is a comment\nCOSMOS");
        var tokens = lexer.Tokenize();

        // Comment is skipped, COSMOS is tokenized, then EOF
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal(TokenType.EOF, tokens[1].tokenType);
    }

    // ── Newlines ───────────────────────────────────────────────────

    [Fact]
    public void Tokenize_Newline_ProducesNewLineToken()
    {
        var lexer = new Lexer("COSMOS\nEXE");
        var tokens = lexer.Tokenize();

        // COSMOS, NEWLINE, EXE, EOF
        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal(TokenType.NewLine, tokens[1].tokenType);
        Assert.Equal(TokenType.ST_Exe, tokens[2].tokenType);
        Assert.Equal(TokenType.EOF, tokens[3].tokenType);
    }

    [Fact]
    public void Tokenize_CRLF_ProducesSingleNewLineToken()
    {
        var lexer = new Lexer("A\r\nB");
        var tokens = lexer.Tokenize();

        // CRLF is treated as a single newline: Identifier A, NEWLINE, Identifier B, EOF
        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].tokenType);
        Assert.Equal("A", tokens[0].tk);
        Assert.Equal(TokenType.NewLine, tokens[1].tokenType);
        Assert.Equal(TokenType.Identifier, tokens[2].tokenType);
        Assert.Equal("B", tokens[2].tk);
        Assert.Equal(TokenType.EOF, tokens[3].tokenType);
    }

    // ── Full statement ─────────────────────────────────────────────

    [Fact]
    public void Tokenize_FullCOSMOSStatement()
    {
        var lexer = new Lexer("COSMOS func 1 2 \"hello\"");
        var tokens = lexer.Tokenize();

        // COSMOS, func, 1, 2, "hello", EOF
        Assert.Equal(6, tokens.Count);
        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal(TokenType.Identifier, tokens[1].tokenType);
        Assert.Equal("func", tokens[1].tk);
        Assert.Equal(TokenType.Identifier, tokens[2].tokenType);
        Assert.Equal("1", tokens[2].tk);
        Assert.Equal(TokenType.Identifier, tokens[3].tokenType);
        Assert.Equal("2", tokens[3].tk);
        Assert.Equal(TokenType.String, tokens[4].tokenType);
        Assert.Equal("\"hello\"", tokens[4].tk);
        Assert.Equal(TokenType.EOF, tokens[5].tokenType);
    }

    // ── Line and column tracking ───────────────────────────────────

    [Fact]
    public void Tokenize_TracksLineAndCol()
    {
        var lexer = new Lexer("A\nB");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].line);
        Assert.Equal(2, tokens[2].line); // B is on line 2
    }

    // ── Multiple statements ────────────────────────────────────────

    [Fact]
    public void Tokenize_MultipleStatements()
    {
        var lexer = new Lexer("COSMOS f1\nEXE f2");
        var tokens = lexer.Tokenize();

        // COSMOS, f1, NEWLINE, EXE, f2, EOF
        Assert.Equal(6, tokens.Count);
        Assert.Equal(TokenType.ST_Cosmos, tokens[0].tokenType);
        Assert.Equal(TokenType.NewLine, tokens[2].tokenType);
        Assert.Equal(TokenType.ST_Exe, tokens[3].tokenType);
    }
}
