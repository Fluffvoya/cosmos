# bridge Module

## Purpose

Defines the `IServer` interface — the core abstraction for request/response communication between cm-script and the host application.

## Namespace

`bridge`

## Interfaces

### `IServer`

```csharp
public interface IServer
{
    string Execute(string requests);
}
```

Receives a serialized `Request` JSON string, processes it, and returns a serialized `Response` JSON string. Implementations include:

- **`MainWindow`** (app module) — routes requests to the WebView2 frontend or intercepts them for built-in operations (ShowMessage, Log, Warning, Error).
- **`SandboxServer`** (sandbox module) — debug implementation that prints to console.

## Dependencies

None. This is a leaf module.
