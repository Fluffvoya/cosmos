# func-router Module

## Purpose

Type-safe function registry and dispatcher. Maps function names to callable `Function` objects, validates argument count and types before invocation, and passes the `IServer` instance to each call.

## Namespace

`func_router`

## Classes

### `Function`

Wraps an `Action<IServer, List<object>>` with a typed argument signature.

```csharp
public class Function
{
    public Action<IServer, List<object>> func { get; }
    public List<ArgumentType> argsType { get; }
}
```

**Constructor:**
```csharp
public Function(Action<IServer, List<object>> func_, params List<ArgumentType> argsType_)
```

**Methods:**

```csharp
public void Call(IServer server, List<object> args)
```
Validates that `args.Count == argsType.Count` and each argument's CLR type matches the expected `ArgumentType`:
- `Number` → `long`
- `Float` → `double`
- `String` → `string`

Throws:
- `RouterException(ArgumentCountMismatch)` if the count doesn't match.
- `RouterException(ArgumentTypeCheckFailed)` if a type doesn't match.

### `Router`

Maps string function names to `Function` objects.

```csharp
public class Router
{
    public Router(IServer server)
    public void Add(string name, Function func)
    public void Call(string func, List<object> args)
}
```

**`Add`**: Registers or overwrites a function under the given name.

**`Call`**: Looks up the function by name and invokes `Function.Call`. Throws `RouterException(FunctionNotFound)` if the name is not registered.

## Dependencies

- `argument` (ArgumentType)
- `bridge` (IServer)
- `error` (RouterException, ErrorCode)
