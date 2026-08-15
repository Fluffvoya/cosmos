# Cosmos

> A subsystem for controlling the whole computer.

Cosmos is a Windows desktop automation platform built with .NET 10 and WebView2. It features a custom scripting language (cm-script), a task scheduler, password manager, app launcher, ringtone player, and log viewer — all orchestrated through a single tabbed interface.

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Interface Guide](#interface-guide)
  - [Start Page](#start-page)
  - [Log Viewer](#log-viewer)
  - [Scheduler](#scheduler)
  - [Script Terminal](#script-terminal)
  - [Password Manager](#password-manager)
  - [Ringtone Player](#ringtone-player)
  - [App Launcher](#app-launcher)
  - [Settings](#settings)
- [cm-script Language Reference](#cm-script-language-reference)
  - [Statements](#statements)
  - [Comments](#comments)
  - [Argument Types](#argument-types)
  - [Built-in Functions](#built-in-functions)
  - [External Executables](#external-executables)
  - [Python Scripts](#python-scripts)
  - [IPC Protocol](#ipc-protocol)
  - [Full Example](#full-example)
- [Python SDK](#python-sdk)
- [Data Files](#data-files)
- [Error Codes](#error-codes)
- [License](#license)

---

## Features

- **Custom Scripting Language** — cm-script with a clean syntax for automation tasks
- **Task Scheduler** — Schedule scripts to run once, daily, or weekly
- **Password Manager** — AES-256-CBC encrypted credential storage with master password
- **App Launcher** — Register and launch applications with drag-and-drop reordering
- **Ringtone Player** — Play audio files as looped notifications
- **Log Viewer** — Filterable system log with copy-to-clipboard support
- **Python Integration** — Run Python scripts with bidirectional IPC via the `cosmos-client` SDK
- **WebView2 UI** — Modern frameless window with native DWM animations

---

## Prerequisites

- **Windows 10/11**
- **.NET 10.0 SDK** — [Download](https://dotnet.microsoft.com/download)
- **WebView2 Runtime** — Pre-installed on modern Windows; [download](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) if missing
- **Visual Studio 2022** (recommended) or the `dotnet` CLI

---

## Installation

### Option 1: Download from Release (Recommended)

Download the latest pre-built package from the [Releases](https://github.com/Fluffvoya/cosmos/releases) page. Extract the archive and run `app.exe` directly — no build required.

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/Fluffvoya/cosmos.git
cd cosmos

# Build
dotnet build cosmos.sln

# Run
dotnet run --project modules/app/app.csproj

# Build release
dotnet build cosmos.sln -c Release

# Run tests
dotnet test modules/tests/tests.csproj
```

The output binary is at `modules/app/bin/{Configuration}/net10.0-windows/app.exe`.

---

## Interface Guide

Cosmos uses a single frameless window with a tabbed layout. The tab strip can be positioned at the **top**, **left**, or **right** (configurable in Settings).

### Start Page

The landing page displays:

- **Clock** — Real-time display in 12-hour or 24-hour format (click to toggle)
- **Greeting** — Personalized message using your configured username
- **Quick Actions** — Cards linking to all features: Log, Scheduler, Script, Password Manager, Ringtone, App Launcher, and Settings

### Log Viewer

A filterable system log table.

| Column | Description |
|--------|-------------|
| Time | Timestamp of the log entry |
| Level | `Info`, `Warning`, or `Error` |
| Sender | Module that produced the log |
| Message | Log content |

- **Filter buttons** — Show All, Error only, Warning only, or Info only
- **Copy** — Click the copy icon on any row to copy its content to clipboard
- **Clear** — Remove all log entries

### Scheduler

Manage scheduled cm-script tasks.

**Adding a task:**

1. Click **Add Task**
2. Set the **time** (HH:MM format)
3. Choose **recurrence**: Once, Daily, or Weekly
4. For Weekly: select the days of the week
5. For Once: pick a specific date
6. Browse and select a `.cms` script file
7. Click **Save**

**Managing tasks:**

- **Run Now** — Execute the task immediately
- **Delete** — Remove the task
- **Last Status** — Shows the result of the most recent execution

Tasks persist to `~/.cosmos/tasks.json` and are polled every 15 seconds by the background scheduler.

### Script Terminal

An interactive REPL for cm-script.

- **Input** — Type cm-script commands at the `>` prompt and press Enter
- **History** — Use Up/Down arrow keys to navigate command history
- **Output** — Color-coded: white for info, green for success, red for error, yellow for warning, gray for echoed commands
- **Clear** — Reset the terminal output

Output is persisted to `~/.cosmos/script-output.json` (up to 500 lines) and survives tab switches.

### Password Manager

An encrypted credential store protected by a master password.

**First-time setup:**

1. Enter and confirm a master password
2. The master password hash (SHA-256 + salt) is stored in `~/.cosmos/password_hash.dat`

**Daily use:**

1. Enter your master password to unlock
2. Browse platforms in the left sidebar
3. Click a platform to view its accounts
4. Each account shows: username, password (hidden by default), and copy buttons

**Managing credentials:**

- **Add Platform** — Create a new platform entry (e.g., "GitHub", "AWS")
- **Add Account** — Add a username/password pair under a platform
- **Edit / Delete** — Modify or remove platforms and accounts
- **Password Generator** — Generate random passwords with configurable length and special character inclusion
- **Change Master Password** — Re-encrypts all data with the new password

All data is AES-256-CBC encrypted and stored in `~/.cosmos/password_data.enc`.

### Ringtone Player

Displays currently playing audio files.

- Each active ringtone appears as a horizontal bar with its filename
- Click the **close** button to stop playback
- The tab auto-opens when a `PlayRingtone` command is executed from cm-script

### App Launcher

A card-based application launcher.

- **Card Grid** — Each registered app shows its icon (extracted from the executable) and name
- **Search** — Filter applications by name
- **Add App** — Register a new application with name, executable path, and optional arguments
- **Remove** — Delete a registered application
- **Drag & Drop** — Reorder cards by dragging
- **Launch** — Click a card to start the application

Registered apps are stored in `~/.cosmos/launch-apps.json`.

### Settings

Accessed via the menu bar. Changes are saved immediately.

| Section | Setting | Description |
|---------|---------|-------------|
| **General** | Username | Display name shown on the Start page greeting |
| | Tab Position | Top, Left, or Right |
| | Tab Strip Width | Width in pixels (for Left/Right positions) |
| **Scripting** | Python Path | Path to the Python interpreter (supports `%VAR%` expansion) |
| **Startup** | Startup Script | Path to a `.cms` file to run when Cosmos starts |

---

## cm-script Language Reference

cm-script is a line-oriented scripting language. Each line is one statement. Blank lines are ignored. File extension: `.cms`.

### Statements

| Keyword | Alias | Description |
|---------|-------|-------------|
| `COSMOS` | `$` | Call a registered Cosmos function via the internal Router |
| `EXE` | `#` | Launch an external executable with stdin/stdout JSON IPC |
| `PYTHON` | — | Launch a Python script with stdin/stdout JSON IPC |

### Comments

Comments start with `!` and extend to the end of the line. Inline comments are supported:

```
! This is a full-line comment
COSMOS Log "Hello"  ! This is an inline comment
```

### Argument Types

Arguments are separated by whitespace. Three types are supported:

| Type | Format | Examples |
|------|--------|----------|
| Integer | Optional sign + digits | `42`, `-100`, `0` |
| Float | Digits with decimal point | `3.14`, `-2.5`, `.5` |
| String | Double-quoted or single-quoted text | `"hello world"`, `'hello world'` |

### Built-in Functions

These functions are registered in the Cosmos Router and can be called via `COSMOS` or `$`:

#### `Log` — Log an info message

```
COSMOS Log "Application started successfully"
$ Log "Ready"
```

#### `Warning` — Log a warning message

```
COSMOS Warning "Disk space running low"
$ Warning "Check configuration"
```

#### `Error` — Log an error message

```
COSMOS Error "Failed to connect to database"
```

#### `MessageBox` — Show a native dialog

```
COSMOS MessageBox "Alert" "Operation completed"
$ MessageBox "Confirm" "Are you sure?"
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | String | Dialog title |
| `message` | String | Dialog body text |

#### `MessageBar` — Show a toast notification

```
COSMOS MessageBar "Build finished" "info"
$ MessageBar "Low memory" "warning"
COSMOS MessageBar "Connection lost" "error"
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `message` | String | Notification text |
| `level` | String | `"info"`, `"warning"`, or `"error"` |

#### `GetUserName` — Get the configured username

```
COSMOS GetUserName
```

Returns the username configured in Settings. No arguments.

#### `PlayRingtone` — Play an audio file

```
COSMOS PlayRingtone "C:/Music/alert.mp3"
$ PlayRingtone "notification.wav"
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `audioPath` | String | Path to the audio file |

The audio loops until the user closes it from the Ringtone tab.

#### `OpenRegisteredApp` — Launch a registered application

```
COSMOS OpenRegisteredApp "Visual Studio Code"
$ OpenRegisteredApp "Terminal"
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `appName` | String | Name of the registered application |

### External Executables

Use `EXE` or `#` to launch an external program. The program communicates with Cosmos via stdin/stdout JSON IPC.

```
EXE mytool.exe --verbose --output result.txt
# helper.cmd /silent
$  ! This is NOT valid — $ needs a function name
```

The executable path is followed by optional string arguments passed to the process.

### Python Scripts

Use `PYTHON` to launch a Python script. The script communicates with Cosmos via stdin/stdout JSON IPC.

```
PYTHON cleanup.py --path /tmp
PYTHON analyze.py "input.csv"
```

The Python script path is followed by optional string arguments.

**Requirements:**
- Python must be installed and the path configured in Settings > Scripting > Python Path
- Use the `cosmos-client` SDK for easy IPC (see [Python SDK](#python-sdk))

### IPC Protocol

External processes (EXE and PYTHON) communicate with Cosmos through stdin/stdout using JSON messages.

**Request (Cosmos → Process):**

```json
{"request": "Log", "args": ["Hello from external process"]}
```

**Response (Process → Cosmos):**

```json
{"request": "Log", "message": "Acknowledged"}
```

Available request names match the built-in function names: `Log`, `Warning`, `Error`, `MessageBox`, `MessageBar`, etc.

### Full Example

```
! ===================================
! Cosmos Startup Script
! ===================================

! Log system status
COSMOS Log "=== System Startup ==="
$ Log "Initializing components..."

! Show welcome dialog
COSMOS MessageBox "Welcome" "Cosmos is ready!"

! Check configuration
COSMOS Warning "Verify Python path in Settings"

! Play startup sound
COSMOS PlayRingtone "C:/Sounds/startup.mp3"

! Launch external tools
EXE notepad.exe
# cmd.exe /c echo "Hello from CMD"

! Run Python maintenance
PYTHON maintenance.py --check-all

! Notify completion
$ MessageBar "All systems operational" "info"
$ Log "=== Startup Complete ==="
```

---

## Python SDK

The `cosmos-client` package provides a Python interface for IPC with Cosmos.

### Installation

```bash
pip install cosmos-client
```

### Usage

```python
from cosmos_client import Client

# Log a message
response = Client.Send(Client.Log("Hello from Python"))

# Show a warning
response = Client.Send(Client.Warning("Check this"))

# Show a message box
response = Client.Send(Client.MessageBox("Title", "Message content"))

# Show a toast notification
response = Client.Send(Client.MessageBar("Task done", "info"))

# Play a ringtone
response = Client.Send(Client.PlayRingtone("/path/to/audio.mp3"))

# Open a registered app
response = Client.Send(Client.OpenRegisteredApp("VS Code"))

# Get the username
response = Client.Send(Client.GetUserName())
```

### Custom Requests

```python
from cosmos_client import Client

# Send a custom request
response = Client.Send({"request": "Log", "args": ["custom message"]})
```

---

## Data Files

All Cosmos data is stored in `~/.cosmos/` (typically `C:\Users\<username>\.cosmos\`):

| File | Description |
|------|-------------|
| `settings.json` | Application settings (username, tab position, Python path, etc.) |
| `start-config.json` | Start page configuration (time format) |
| `tasks.json` | Scheduled tasks list |
| `script-output.json` | Persisted Script Terminal output (max 500 lines) |
| `launch-apps.json` | Registered applications for the App Launcher |
| `password_hash.dat` | Master password salt and SHA-256 hash |
| `password_data.enc` | AES-256-CBC encrypted credential data |

---

## Error Codes

| Range | Module | Examples |
|-------|--------|----------|
| 1xxx | Argument | `ArgumentNull`, `ArgumentFormatInvalid`, `ArgumentTypeMismatch`, `ArgumentOverflow` |
| 2xxx | Function Router | `FunctionNotFound`, `ArgumentCountMismatch`, `ArgumentTypeCheckFailed` |
| 3xxx | Data Model | `JsonDeserializeFailed`, `JsonSerializeFailed`, `EmptyRequestName` |
| 4xxx | cm-script | `SyntaxError`, `MissingFunctionName` |
| 5xxx | Script Functions | `EmptyArgumentValue`, `InvalidArgumentValue` |
| 6xxx | Process | `PythonNotFound`, `ScriptNotFound`, `PythonRuntimeError`, `ProcessCommunicationError` |
| 7xxx | Launcher | `AppNotFound`, `AppPathInvalid`, `DuplicateAppName`, `AppRegistryLoadFailed` |

---

## License

MIT License — Copyright (c) 2026 Hao Meng
