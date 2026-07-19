namespace cm_script;

public enum TokenType
{
    Unknown,
    // keywords
    ST_Cosmos,
    ST_Exe,
    ST_Lib,
    ST_Script,
    ST_Python,
    // identifier
    Identifier,
    // new line
    NewLine,
    // end of file
    EOF,
}

public class Token
{
    public string tk = "";
    public TokenType tokenType = TokenType.Unknown;
    public int line { get; }
    public int col { get; }
    public Token(string tk_, TokenType type, int line, int col)
    {
        tk = tk_;
        tokenType = type;
        this.line = line;
        this.col = col;
    }
}
