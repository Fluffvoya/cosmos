using cm_script;
using func_router;
using argument;

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

Console.WriteLine("-----------------------------------------------");

var router = new Router();
var func = new Function((List<object> args) =>
{
    Console.WriteLine($"{(long)args[0] + (long)args[1]}\n{(string)args[2]}");
}, ArgumentType.Number, ArgumentType.Number

, ArgumentType.String);

router.Add("func", func);
string wrongCode = "COSMOS func \"1\"";
string correct = "COSMOS func 1 2 \"Hello world\"";
var lexer1 = new Lexer(correct);
var lexer2 = new Lexer(wrongCode);
var ctks = lexer1.Tokenize();
var interpreter = new Interpreter(ctks, router);
try
{
    interpreter.Interpret();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}