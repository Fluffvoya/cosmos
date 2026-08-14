# server Module

## Purpose

Factory methods for creating `Response` objects and parsing `Request` objects. The counterpart to the `client` module — used by the host side to handle incoming requests.

## Namespace

`server`

## Classes

### `Server`

Static factory class. All methods are static.

```csharp
public static string CreateResponse(Response response)
```
Serializes a `Response` to JSON. Delegates to `Response.Serialize()`.

```csharp
public static Request? GetRequest(string text)
```
Deserializes a JSON string to a `Request`. Returns `null` for empty input.

```csharp
public static string? GetRequestName(string text)
```
Deserializes a `Request` and returns only the `request` (function name) field. Returns `null` for empty or invalid input.

## Dependencies

- `public-model` (Request, Response)
