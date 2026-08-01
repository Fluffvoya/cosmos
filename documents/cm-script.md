# cm-script

The core of the system.

## Function Call

Basic statement in the cm-script.

## Interface Type

### Built-In

Some code wrote in the cosmos which can be called by the system.

Add `$` or `COSMOS` before to call.

```
$func [arg] ...

COSMOS func [arg] ...
```

### Execute

The program existed in the computer.

Add `#` or `EXE` before to call.

```
#[execute name] [arg] ...
EXE [execute name] [arg] ...
```

### Cosmos Script

Other script (not include self).

Add `&` or `SCRIPT` before to call.

```
&[script name] [function name] ...
SCRIPT [script name] [function name] ...
```

### Python

The python script(.py file).

Add `PYTHON` before to call.

```
PYTHON main.py [arg] ...
```
