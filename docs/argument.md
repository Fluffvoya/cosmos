# argument Module

## Purpose

Classifies and converts string tokens from cm-script into typed values (`long`, `double`, `string`). Used by the interpreter to parse function arguments before dispatching them through the router.

## Namespace

`argument`

## Classes

### `ArgumentType` (enum)

Three supported argument types:

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Number` | Integer literal (e.g. `42`, `-100`) |
| 1 | `Float` | Floating-point literal (e.g. `3.14`, `.5`, `-2.5`) |
| 2 | `String` | Quoted string literal (e.g. `"hello"`, `'world'`) |

### `ArgumentTypeJudge`

Classifies a raw token string into its `ArgumentType`.

```csharp
public static ArgumentType Judge(string tk)
```

**Rules (evaluated in order):**
1. `null` or empty → throws `CosmosArgumentException(ArgumentNull)`
2. Wrapped in matching single or double quotes → `String`
3. Parses as `long` via `Integer` style → `Number`
4. Parses as `float` via `Float` style → `Float`
5. Otherwise → throws `CosmosArgumentException(ArgumentFormatInvalid)`

### `ArgumentConvert`

Converts a classified token to its typed value.

```csharp
public static long   ToNumber(string tk)    // → long; throws ArgumentTypeMismatch if not Number
public static double ToFloat(string tk)     // → double; throws ArgumentTypeMismatch if not Float
public static string ToString_(string tk)   // → string with quotes stripped; throws ArgumentTypeMismatch if not String
```

**Overflow handling:**
- `ToNumber`: throws `ArgumentOverflow` if the value exceeds `Int64` bounds.
- `ToFloat`: throws `ArgumentOverflow` if the result is `Infinity` or exceeds `Double` bounds.

## Dependencies

- `error` (CosmosArgumentException, ErrorCode)
