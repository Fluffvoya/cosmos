# script-func Module

## Purpose

Provides pre-built `Function` objects that wrap the `Client` API. These are the built-in functions available to cm-script via `Script.AddFunction`. Each function sends a `Request` through `IServer.Execute`.

## Namespace

`script_func`

## Classes

### `ScriptFunctions` (static)

All properties return a new `Function` instance on each access.

#### `MessageBox`

```csharp
public static Function MessageBox { get; }
```
- **Arguments:** `[String name, String message]`
- **Action:** Calls `Client.MessageBox(name, message)`, serializes the request, and sends it via `IServer.Execute`.

#### `Log`

```csharp
public static Function Log { get; }
```
- **Arguments:** `[String content]`
- **Action:** Calls `Client.Log(content)` and sends via `IServer.Execute`.

#### `Warning`

```csharp
public static Function Warning { get; }
```
- **Arguments:** `[String content]`
- **Action:** Calls `Client.Warning(content)` and sends via `IServer.Execute`.

#### `Error`

```csharp
public static Function Error { get; }
```
- **Arguments:** `[String content]`
- **Action:** Calls `Client.Error(content)` and sends via `IServer.Execute`.

#### `MessageBar`

```csharp
public static Function MessageBar { get; }
```
- **Arguments:** `[String message, String level]` — `level` must be `"info"`, `"warning"`, or `"error"`.
- **Action:** Calls `Client.MessageBar(message, level)`, serializes the request, and sends it via `IServer.Execute`. Throws `ScriptFuncException(InvalidArgumentValue)` if `level` is not a recognized value.

#### `GetUserName`

```csharp
public static Function GetUserName { get; }
```
- **Arguments:** none
- **Action:** Calls `Client.GetUserName()` and sends via `IServer.Execute`.

## Dependencies

- `argument` (ArgumentType — for Function type signatures)
- `bridge` (IServer)
- `client` (Client)
- `func-router` (Function)
- `public-model` (Request)
