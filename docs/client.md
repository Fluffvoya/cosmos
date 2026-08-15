# client Module

## Purpose

Factory methods for creating `Request` objects and parsing `Response` objects. Used by cm-script functions to build requests that are sent to the host via `IServer.Execute`.

## Namespace

`client`

## Classes

### `Client`

Static factory class. All methods are static.

```csharp
public static Request MessageBox(string name, string message)
```
Creates a `MessageBox` request with two arguments: the window name and the message text.

```csharp
public static Request Log(string content)
```
Creates a `Log` request with one argument.

```csharp
public static Request Warning(string content)
```
Creates a `Warning` request with one argument.

```csharp
public static Request Error(string content)
```
Creates an `Error` request with one argument.

```csharp
public static Request MessageBar(string message, string level)
```
Creates a `MessageBar` request with two arguments: the message text and the level (`"info"`, `"warning"`, or `"error"`).

```csharp
public static Request GetUserName()
```
Creates a `GetUserName` request with no arguments.

```csharp
public static Request OpenRegisteredApp(string appName)
```
Creates an `OpenRegisteredApp` request with one argument: the registered application name.

```csharp
public static string CreateRequest(Request request)
```
Serializes a `Request` to JSON. Delegates to `Request.Serialize()`.

```csharp
public static Response? GetResponse(string text)
```
Deserializes a JSON string to a `Response`. Returns `null` for empty input.

```csharp
public static string? GetResponseMessage(string text)
```
Deserializes a `Response` and returns only the `message` field. Returns `null` for empty or invalid input.

## Dependencies

- `public-model` (Request, Response)
