# process Module

## Purpose

Executes external processes (native executables and Python scripts) with stdin/stdout-based IPC. Each process communicates with the host by writing `Request` JSON to stdout and reading `Response` JSON from stdin.

## Namespace

`process`

## Classes

### `InternalProcess`

Executes a registered function through the `Router`. Not a real subprocess — calls the function in-process.

```csharp
public InternalProcess(Router router, string func, List<object> args)
public void Execute()
```

Delegates to `Router.Call(func, args)`. Propagates `RouterException` on lookup failure or argument mismatch.

### `ExecuteProcess`

Launches an external executable and bridges its stdin/stdout to the `IServer`.

```csharp
public ExecuteProcess(string program, List<string> args, IServer server)
public async Task Execute()
```

**Behavior:**
1. Validates the program file exists. Throws `ProcessException(ProcessCommunicationError)` if not.
2. Starts the process with redirected stdin/stdout/stderr, no window.
3. Reads lines from stdout. Each line is a `Request` JSON.
4. Passes each request to `IServer.Execute()` and writes the response to stdin.
5. When stdout closes (process exits), checks the exit code.
6. Non-zero exit code throws `ProcessException(ProcessCommunicationError)` with stderr content.

**Error scenarios:**
- Program not found → `ProcessException`
- Failed to start → `ProcessException`
- Read/write failure → `ProcessException`
- Non-zero exit → `ProcessException` with exit code and stderr

### `PythonProcess`

Launches a Python script via the configured Python interpreter. Same IPC protocol as `ExecuteProcess`.

```csharp
public PythonProcess(string python, string script, List<string> args, IServer server)
public async Task Execute()
```

**Validation (before launch):**
- Python interpreter not found → `ProcessException(PythonNotFound)`
- Script file not found → `ProcessException(ScriptNotFound)`

**Exit behavior:**
- Non-zero exit code throws `PythonRuntimeException(PythonRuntimeError)` instead of `ProcessException`. This distinguishes Python runtime errors from host-side communication errors.
- The `PythonRuntimeException.ExitCode` property carries the process exit code.

## Dependencies

- `bridge` (IServer)
- `func-router` (Router — for InternalProcess)
- `error` (ProcessException, PythonRuntimeException, ErrorCode)
