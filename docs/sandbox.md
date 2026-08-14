# sandbox Module

## Purpose

Developer testing harness. Provides a minimal `IServer` implementation and a `Main` entry point for manually exercising the cm-script engine without the full WinForms application.

## Namespace

Global (no namespace)

## Classes

### `SandboxServer`

```csharp
public class SandboxServer : IServer
{
    public string Execute(string requests)
}
```

Prints the incoming request JSON to the console and returns the literal string `"reply"`.

### `Sandbox`

```csharp
public static async Task Main(string[] args)
```

Runs two test scenarios:
1. `COSMOS func` — calls a registered function that sends a `MessageBox` request through `SandboxServer`.
2. `PYTHON D:\program\a.py` — launches a Python script through the interpreter.

## Dependencies

- `bridge` (IServer)
- `client` (Client)
- `cm-script` (Script)
- `func-router` (Function)
