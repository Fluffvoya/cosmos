# cosmos-client

Python client SDK for the Cosmos host application.

## Installation

```bash
pip install cosmos-client
```

## Usage

```python
from cosmos_client import Client

# Build and send a request
response = Client.Send(Client.Log("Hello from Python"))
print(response.message)

# Get the user name
response = Client.Send(Client.GetUserName())
print(response.message)
```

## API

### Request Builders (static methods)

| Method | Description |
|---|---|
| `Client.Log(content)` | Create a Log request |
| `Client.Warning(content)` | Create a Warning request |
| `Client.Error(content)` | Create an Error request |
| `Client.GetUserName()` | Create a GetUserName request |
| `Client.MessageBox(name, message)` | Create a MessageBox request |
| `Client.MessageBar(message, level)` | Create a MessageBar request |
| `Client.PlayRingtone(audio_path)` | Create a PlayRingtone request |
| `Client.OpenRegisteredApp(app_name)` | Create an OpenRegisteredApp request |

### Serialization Helpers (static methods)

| Method | Description |
|---|---|
| `Client.CreateRequest(request)` | Serialize a Request to JSON string |
| `Client.GetResponse(text)` | Deserialize JSON string to Response |
| `Client.GetResponseMessage(text)` | Deserialize and extract message |

### I/O

| Method | Description |
|---|---|
| `Client.Send(request)` | Send request via stdout, read response from stdin |

## Communication Protocol

The client communicates with the Cosmos host via stdin/stdout using JSON:

- **Request**: `{"request": "Log", "args": ["content"]}`
- **Response**: `{"request": "Log", "message": "reply text"}`
