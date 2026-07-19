using cm_script;

var source = """
COSMOS test arg "arg" 1 
$test arg "arg" 1

EXE test arg "arg" 1 
#test arg "arg" 1 

LIB test.dll function arg "arg" 1 
@test function arg "arg" 1 

SCRIPT test.cm arg "arg" 1 
&test arg "arg" 1 

PYTHON test.py arg "arg" 1 

! this is a comment
""";

var lexer = new Lexer(source);

var PrintToken = (Token tk) =>
{
    Console.WriteLine($"token:{tk.tk}\ttoken type:{tk.tokenType}\tat {tk.line}:{tk.col}");
};

var tokens = lexer.Tokenize();

foreach (var tk in tokens)
{
    PrintToken(tk);
}