# public-model Module

## Purpose

Defines the `Request` and `Response` data models used for JSON-based communication between cm-script processes and the host application.

## Namespace

`public_model`

## Classes

### `Request`

Represents a function call request from a cm-script to the host.

```csharp
public class Request
{
    public string request { get; set; }       // Function name (e.g. "MessageBox")
    public List<string> args { get; set; }    // String arguments
}
```

**Constructors:**
- `Request()` — initializes with empty `request` and empty `args`.
- `Request(string request_, params List<string> args_)` — sets the function name and arguments. Throws `PublicModelException(EmptyRequestName)` if `request_` is null or empty.

**Methods:**
- `string Serialize()` — converts to JSON string. Throws `PublicModelException(JsonSerializeFailed)` on failure.
- `static Request? Deserialize(string text)` — parses JSON to `Request`. Returns `null` for empty input. Throws `PublicModelException(JsonDeserializeFailed)` on invalid JSON.

### `Response`

Represents the host's reply to a cm-script request.

```csharp
public class Response
{
    public string request { get; set; }   // The original function name
    public string message { get; set; }   // Response payload
}
```

**Constructors:**
- `Response()` — initializes with empty strings.
- `Response(string request, string message)` — sets both fields. Throws `PublicModelException(EmptyResponseRequestName)` if `request` is null or empty. Empty `message` is allowed.

**Methods:**
- `string Serialize()` — converts to JSON. Throws `PublicModelException(JsonSerializeFailed)` on failure.
- `static Response? Deserialize(string text)` — parses JSON. Returns `null` for empty input. Throws `PublicModelException(JsonDeserializeFailed)` on invalid JSON.

## Dependencies

- `error` (CosmosException, ErrorCode via PublicModelException)
