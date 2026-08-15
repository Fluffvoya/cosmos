# app-password Module

## Purpose

Manages encrypted password storage using AES-256-CBC with a master password. The master password is hashed with SHA256 + random salt and stored in `~/.cosmos/password_hash.dat`. Encrypted platform and account data is stored in `~/.cosmos/password_data.enc`.

## Namespace

`app_password`

## Classes

### `AppPasswordManager`

Provides the full lifecycle for password-based encrypted storage: setup, verification, password change, and data load/save.

```csharp
public class AppPasswordManager
{
    public AppPasswordManager()
    public AppPasswordManager(string settingsFolder)
    public bool IsSetup()
    public bool Setup(string password)
    public bool VerifyPassword(string password)
    public bool ChangePassword(string oldPassword, string newPassword)
    public PlatformData[] LoadData(string password)
    public bool SaveData(string password, PlatformData[] platforms)
}
```

#### Constructor

```csharp
public AppPasswordManager()
public AppPasswordManager(string settingsFolder)
```

- **Arguments:**
  - `settingsFolder` (optional) — The directory to store password files in. Defaults to `~/.cosmos/`.

#### `IsSetup()`

Checks whether a master password has been configured by testing for the existence of the hash file.

**Returns:** `true` if `~/.cosmos/password_hash.dat` exists.

#### `Setup(string password)`

Initializes the master password for the first time. Generates a random salt, hashes the password with SHA256, stores the salt + hash, and creates an empty encrypted data file.

**Returns:** `true` if setup succeeded, `false` on error.

#### `VerifyPassword(string password)`

Verifies a password against the stored hash using constant-time comparison to prevent timing attacks.

**Returns:** `true` if the password matches.

#### `ChangePassword(string oldPassword, string newPassword)`

Verifies the old password, loads existing data, re-hashes with a new salt, and re-encrypts all data with the new password.

**Returns:** `true` if the password was changed successfully, `false` if the old password is incorrect or an error occurs.

#### `LoadData(string password)`

Decrypts and returns all stored platform data. Uses PBKDF2 (SHA256, 10000 iterations) to derive the AES key from the password.

**Returns:** An array of `PlatformData`, or an empty array if decryption fails or no data exists.

#### `SaveData(string password, PlatformData[] platforms)`

Serializes the platform data to JSON (camelCase), encrypts with AES-256-CBC, and writes to the data file. A random IV is generated for each save.

**Returns:** `true` if save succeeded, `false` on error.

### `PlatformData`

Represents a platform (e.g., a website or service) with multiple accounts.

```csharp
public class PlatformData
{
    public string Name { get; set; }
    public AccountData[] Accounts { get; set; }
}
```

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the platform |
| `Accounts` | `AccountData[]` | Array of accounts associated with this platform |

### `AccountData`

Represents a single account with username and password.

```csharp
public class AccountData
{
    public string Username { get; set; }
    public string Password { get; set; }
}
```

| Property | Type | Description |
|----------|------|-------------|
| `Username` | `string` | The account username |
| `Password` | `string` | The account password (stored encrypted) |

## Dependencies

None. This is a leaf module. It uses only BCL types (`System.Security.Cryptography`, `System.Text.Json`, `System.IO`).
