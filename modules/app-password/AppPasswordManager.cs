using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace app_password;

/// <summary>
/// Shared JSON serializer options using camelCase to match frontend conventions.
/// </summary>
internal static class JsonOptions
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Manages password storage with encryption.
/// Master password is hashed with SHA256 + salt and stored in ~/.cosmos/password_hash.dat
/// Password data is encrypted using AES-256-CBC with the master password as key.
/// </summary>
public class AppPasswordManager
{
    // Storage paths
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cosmos");

    private static readonly string HashFile = Path.Combine(SettingsFolder, "password_hash.dat");
    private static readonly string DataFile = Path.Combine(SettingsFolder, "password_data.enc");

    // Hash configuration
    private const int SaltSize = 32;
    private const int HashSize = 32;

    /// <summary>
    /// Check if master password is already set up.
    /// </summary>
    /// <returns>True if hash file exists.</returns>
    public bool IsSetup()
    {
        return File.Exists(HashFile);
    }

    /// <summary>
    /// Set up master password for the first time.
    /// Hashes the password and stores it.
    /// </summary>
    /// <param name="password">The master password to set.</param>
    /// <returns>True if setup succeeded.</returns>
    public bool Setup(string password)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);

            // Generate random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash password with salt
            byte[] hash = HashPassword(password, salt);

            // Store salt + hash
            byte[] storedData = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, storedData, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, storedData, SaltSize, HashSize);

            File.WriteAllBytes(HashFile, storedData);

            // Initialize empty data file
            SaveData(password, Array.Empty<PlatformData>());

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to setup password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verify a password against the stored hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <returns>True if password matches.</returns>
    public bool VerifyPassword(string password)
    {
        try
        {
            if (!File.Exists(HashFile))
                return false;

            byte[] storedData = File.ReadAllBytes(HashFile);
            if (storedData.Length != SaltSize + HashSize)
                return false;

            // Extract salt and hash
            byte[] salt = new byte[SaltSize];
            byte[] storedHash = new byte[HashSize];
            Buffer.BlockCopy(storedData, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(storedData, SaltSize, storedHash, 0, HashSize);

            // Hash input password with same salt
            byte[] inputHash = HashPassword(password, salt);

            // Compare hashes
            return CryptographicEquals(storedHash, inputHash);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to verify password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Change the master password.
    /// Verifies old password, then updates to new password.
    /// </summary>
    /// <param name="oldPassword">The current master password.</param>
    /// <param name="newPassword">The new master password.</param>
    /// <returns>True if password was changed successfully.</returns>
    public bool ChangePassword(string oldPassword, string newPassword)
    {
        try
        {
            // Verify old password
            if (!VerifyPassword(oldPassword))
                return false;

            // Load existing data with old password
            var platforms = LoadData(oldPassword);

            // Generate new salt and hash
            byte[] newSalt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newSalt);
            }

            byte[] newHash = HashPassword(newPassword, newSalt);

            // Store new salt + hash
            byte[] storedData = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(newSalt, 0, storedData, 0, SaltSize);
            Buffer.BlockCopy(newHash, 0, storedData, SaltSize, HashSize);

            File.WriteAllBytes(HashFile, storedData);

            // Re-encrypt data with new password
            SaveData(newPassword, platforms);

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to change password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load and decrypt password data.
    /// </summary>
    /// <param name="password">The master password for decryption.</param>
    /// <returns>List of platform data, or empty list if decryption fails.</returns>
    public PlatformData[] LoadData(string password)
    {
        try
        {
            if (!File.Exists(DataFile))
                return Array.Empty<PlatformData>();

            byte[] encryptedData = File.ReadAllBytes(DataFile);
            if (encryptedData.Length < 16) // Minimum: IV
                return Array.Empty<PlatformData>();

            // Extract IV (first 16 bytes)
            byte[] iv = new byte[16];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);

            // Extract ciphertext
            byte[] ciphertext = new byte[encryptedData.Length - 16];
            Buffer.BlockCopy(encryptedData, 16, ciphertext, 0, ciphertext.Length);

            // Derive key from password
            byte[] key = DeriveKey(password);

            // Decrypt
            string json = DecryptAes(ciphertext, key, iv);

            // Deserialize using camelCase to match frontend conventions
            var platforms = JsonSerializer.Deserialize<PlatformData[]>(json, JsonOptions.CamelCase);
            return platforms ?? Array.Empty<PlatformData>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load data: {ex.Message}");
            return Array.Empty<PlatformData>();
        }
    }

    /// <summary>
    /// Encrypt and save password data.
    /// </summary>
    /// <param name="password">The master password for encryption.</param>
    /// <param name="platforms">The platform data to save.</param>
    /// <returns>True if save succeeded.</returns>
    public bool SaveData(string password, PlatformData[] platforms)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);

            // Serialize to JSON using camelCase to match frontend conventions
            string json = JsonSerializer.Serialize(platforms, JsonOptions.CamelCase);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            // Generate random IV
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            // Derive key from password
            byte[] key = DeriveKey(password);

            // Encrypt
            byte[] ciphertext = EncryptAes(plaintext, key, iv);

            // Combine IV + ciphertext
            byte[] encryptedData = new byte[16 + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, encryptedData, 0, 16);
            Buffer.BlockCopy(ciphertext, 0, encryptedData, 16, ciphertext.Length);

            File.WriteAllBytes(DataFile, encryptedData);

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Hash password with salt using SHA256.
    /// </summary>
    private byte[] HashPassword(string password, byte[] salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] combined = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(combined);
    }

    /// <summary>
    /// Derive a 256-bit key from password using PBKDF2.
    /// </summary>
    private byte[] DeriveKey(string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes("cosmos-password-manager-salt");
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, 10000, HashAlgorithmName.SHA256, 32);
    }

    /// <summary>
    /// Encrypt data using AES-256-CBC.
    /// </summary>
    private byte[] EncryptAes(byte[] plaintext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    /// <summary>
    /// Decrypt data using AES-256-CBC.
    /// </summary>
    private string DecryptAes(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Constant-time comparison to prevent timing attacks.
    /// </summary>
    private bool CryptographicEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}

/// <summary>
/// Represents a platform with multiple accounts.
/// </summary>
public class PlatformData
{
    public string Name { get; set; } = string.Empty;
    public AccountData[] Accounts { get; set; } = Array.Empty<AccountData>();
}

/// <summary>
/// Represents an account with username and password.
/// </summary>
public class AccountData
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
