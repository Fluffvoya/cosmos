# cm-script Module

## Purpose

Implements the cm-script language: a line-oriented scripting language for invoking Cosmos functions, external executables, and Python scripts. Contains the lexer (tokenizer), token definitions, interpreter, and the `Script` facade class.

## Namespace

`cm_script`

## Language Syntax

Each line is one statement. Three statement types:

| Keyword | Alias | Description |
|---------|-------|-------------|
| `COSMOS` | `$` | Call a registered function via the router |
| `EXE` | `#` | Launch an external executable (stdin/stdout IPC) |
| `PYTHON` | — | Launch a Python script (stdin/stdout IPC) |

Comments start with `!` and extend to end of line. Blank lines are ignored.

**Example:**
```
! Startup script
COSMOS Log "Hello from Cosmos"
$ ShowMessage "Main" "Ready"
EXE mytool.exe --verbose
PYTHON helper.py
```

## Classes

### `TokenType` (enum)

| Value | Description |
|-------|-------------|
| `Unknown` | Unrecognized token |
| `ST_Cosmos` | `COSMOS` or `$` |
| `ST_Exe` | `EXE` or `#` |
| `ST_Python` | `PYTHON` |
| `Identifier` | Bare word (function name, number, path) |
| `String` | Double-quoted string literal (quotes included in `tk`) |
| `NewLine` | Line terminator |
| `EOF` | End of input |

### `Token`

```csharp
public class Token
{
    public string tk;              // Raw text
    public TokenType tokenType;
    public int line { get; }       // 1-based line number
    public int col { get; }        // 0-based column
}
```

### `Lexer`

Tokenizes a cm-script source string.

```csharp
public Lexer(string source)
public List<Token> Tokenize()
```

**Behavior:**
- `!` starts a line comment (consumed including trailing newline).
- Double quotes (`"`) delimit string literals. Unterminated strings throw `InterpreterException(SyntaxError)`.
- Single quotes are NOT string delimiters — `'hello'` is an identifier.
- `$`, `#`, `@`, `&` are single-character tokens. `$` → `ST_Cosmos`, `#` → `ST_Exe`.

### `Interpreter`

Executes a token stream.

```csharp
public Interpreter(List<Token> tokens, Router router, IServer server, string python)
public async Task Interpret()
```

**Dispatch:**
- `ST_Cosmos`: reads function name + arguments, converts via `ArgumentTypeJudge`/`ArgumentConvert`, calls `InternalProcess.Execute`.
- `ST_Exe`: reads program path + arguments, calls `ExecuteProcess.Execute`.
- `ST_Python`: reads script path + arguments, calls `PythonProcess.Execute`.
- Unrecognized identifiers at top level are silently skipped (no-op).

**Errors:**
- `MissingFunctionName` if nothing follows `COSMOS`.
- Bare `Exception` (not `CosmosException`) if nothing follows `EXE` or `PYTHON`.

### `Script`

Facade that ties together the lexer, interpreter, router, and server.

```csharp
public class Script
{
    public string python { get; set; }
    public Script(IServer server, string python)
    public void AddFunction(string name, Function func)
    public async Task Run(string source)
}
```

- `AddFunction` registers a function in the internal `Router`.
- `Run` tokenizes the source, creates an `Interpreter`, and calls `Interpret`.

## Dependencies

- `argument` (ArgumentTypeJudge, ArgumentConvert)
- `bridge` (IServer)
- `func-router` (Function, Router)
- `process` (InternalProcess, ExecuteProcess, PythonProcess)
- `error` (InterpreterException, ErrorCode)
