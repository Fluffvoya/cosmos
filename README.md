# Cosmos

A subsystem for controlling the whole computer.

## cm-script Language

cm-script is a line-oriented scripting language for invoking Cosmos functions, external executables, and Python scripts. Each line is one statement. Blank lines are ignored.

### Statements

| Keyword | Alias | Description |
|---------|-------|-------------|
| `COSMOS` | `$` | Call a registered Cosmos function |
| `EXE` | `#` | Launch an external executable |
| `PYTHON` | — | Launch a Python script |

### Comments

Comments start with `!` and extend to the end of the line:

```
! This is a comment
COSMOS Log "Hello"  ! Inline comment
```

### Arguments

Arguments are separated by whitespace. Three types are supported:

- **Integer** — `42`, `-100`, `0`
- **Float** — `3.14`, `-2.5`, `.5`
- **String** — double-quoted: `"hello world"`

Single quotes are **not** string delimiters — `'hello'` is treated as an identifier.

### Examples

```
! A startup script
COSMOS Log "System ready"
$ Warning "Check configuration"
COSMOS ShowMessage "Main" "All services started"

! Launch external tools
EXE mytool.exe --verbose
# helper.cmd /silent

! Run a Python script
PYTHON cleanup.py --path /tmp
```

### Notes

- Statements are executed top-to-bottom, one per line.
- Unrecognized identifiers at the top level are silently skipped (no-op).
- A `COSMOS` or `$` keyword must be followed by a function name; omitting it raises `MissingFunctionName`.
- An `EXE` or `PYTHON` keyword must be followed by a program/script path; omitting it raises an error.
- String arguments that are not wrapped in double quotes when expected will cause a type mismatch error at the router level.