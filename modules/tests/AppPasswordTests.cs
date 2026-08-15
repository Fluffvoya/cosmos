using app_password;

namespace tests;

/// <summary>
/// Unit tests for the app-password module: AppPasswordManager.
/// Uses a temp directory via the constructor to isolate tests from the real filesystem.
/// </summary>
public class AppPasswordTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppPasswordManager _manager;

    public AppPasswordTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cosmos-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _manager = new AppPasswordManager(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ── IsSetup ────────────────────────────────────────────────────

    [Fact]
    public void IsSetup_NoHashFile_ReturnsFalse()
    {
        Assert.False(_manager.IsSetup());
    }

    [Fact]
    public void IsSetup_AfterSetup_ReturnsTrue()
    {
        _manager.Setup("password123");

        Assert.True(_manager.IsSetup());
    }

    // ── Setup ──────────────────────────────────────────────────────

    [Fact]
    public void Setup_CreatesHashFile()
    {
        _manager.Setup("mypassword");

        Assert.True(File.Exists(Path.Combine(_tempDir, "password_hash.dat")));
    }

    [Fact]
    public void Setup_CreatesDataFile()
    {
        _manager.Setup("mypassword");

        Assert.True(File.Exists(Path.Combine(_tempDir, "password_data.enc")));
    }

    [Fact]
    public void Setup_ReturnsTrue()
    {
        var result = _manager.Setup("mypassword");

        Assert.True(result);
    }

    // ── VerifyPassword ─────────────────────────────────────────────

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        _manager.Setup("secret123");

        Assert.True(_manager.VerifyPassword("secret123"));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        _manager.Setup("secret123");

        Assert.False(_manager.VerifyPassword("wrongpassword"));
    }

    [Fact]
    public void VerifyPassword_NoSetup_ReturnsFalse()
    {
        Assert.False(_manager.VerifyPassword("anything"));
    }

    // ── ChangePassword ─────────────────────────────────────────────

    [Fact]
    public void ChangePassword_OldPasswordCorrect_ReturnsTrue()
    {
        _manager.Setup("old");

        var result = _manager.ChangePassword("old", "new");

        Assert.True(result);
    }

    [Fact]
    public void ChangePassword_OldPasswordWrong_ReturnsFalse()
    {
        _manager.Setup("old");

        var result = _manager.ChangePassword("wrong", "new");

        Assert.False(result);
    }

    [Fact]
    public void ChangePassword_NewPasswordWorksAfterChange()
    {
        _manager.Setup("old");

        _manager.ChangePassword("old", "new");

        Assert.False(_manager.VerifyPassword("old"));
        Assert.True(_manager.VerifyPassword("new"));
    }

    // ── SaveData / LoadData ────────────────────────────────────────

    [Fact]
    public void SaveData_And_LoadData_RoundTrips()
    {
        _manager.Setup("password");
        var platforms = new[]
        {
            new PlatformData
            {
                Name = "GitHub",
                Accounts = new[] { new AccountData { Username = "user1", Password = "pass1" } }
            }
        };

        _manager.SaveData("password", platforms);
        var loaded = _manager.LoadData("password");

        Assert.Single(loaded);
        Assert.Equal("GitHub", loaded[0].Name);
        Assert.Single(loaded[0].Accounts);
        Assert.Equal("user1", loaded[0].Accounts[0].Username);
        Assert.Equal("pass1", loaded[0].Accounts[0].Password);
    }

    [Fact]
    public void LoadData_WrongPassword_ReturnsEmpty()
    {
        _manager.Setup("password");
        _manager.SaveData("password", new[] { new PlatformData { Name = "Test" } });

        var loaded = _manager.LoadData("wrongpassword");

        Assert.Empty(loaded);
    }

    [Fact]
    public void LoadData_NoDataFile_ReturnsEmpty()
    {
        _manager.Setup("password");

        var loaded = _manager.LoadData("password");

        Assert.Empty(loaded);
    }

    [Fact]
    public void SaveData_MultiplePlatforms_AllPersisted()
    {
        _manager.Setup("password");
        var platforms = new[]
        {
            new PlatformData { Name = "GitHub", Accounts = new[] { new AccountData { Username = "u1", Password = "p1" } } },
            new PlatformData { Name = "GitLab", Accounts = new[] { new AccountData { Username = "u2", Password = "p2" } } }
        };

        _manager.SaveData("password", platforms);
        var loaded = _manager.LoadData("password");

        Assert.Equal(2, loaded.Length);
        Assert.Equal("GitHub", loaded[0].Name);
        Assert.Equal("GitLab", loaded[1].Name);
    }

    [Fact]
    public void SaveData_EmptyPlatforms_PersistsEmptyArray()
    {
        _manager.Setup("password");

        _manager.SaveData("password", Array.Empty<PlatformData>());
        var loaded = _manager.LoadData("password");

        Assert.Empty(loaded);
    }

    // ── Data survives password change ──────────────────────────────

    [Fact]
    public void ChangePassword_DataPreserved()
    {
        _manager.Setup("old");
        var platforms = new[]
        {
            new PlatformData { Name = "GitHub", Accounts = new[] { new AccountData { Username = "u", Password = "p" } } }
        };
        _manager.SaveData("old", platforms);

        _manager.ChangePassword("old", "new");
        var loaded = _manager.LoadData("new");

        Assert.Single(loaded);
        Assert.Equal("GitHub", loaded[0].Name);
    }
}
