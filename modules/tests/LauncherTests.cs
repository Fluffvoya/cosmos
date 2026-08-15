using app_launcher;
using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the app-launcher module: RegisteredApp model and AppRegistry.
/// </summary>
public class LauncherTests : IDisposable
{
    private readonly string _testFolder;

    public LauncherTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"cosmos_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testFolder);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);
        }
        catch { }
    }

    // ── RegisteredApp properties ──────────────────────────────────

    [Fact]
    public void RegisteredApp_DefaultConstructor_PropertiesAreEmpty()
    {
        var app = new RegisteredApp();
        Assert.Equal(string.Empty, app.Name);
        Assert.Equal(string.Empty, app.Path);
        Assert.Null(app.Arguments);
    }

    [Fact]
    public void RegisteredApp_CanSetProperties()
    {
        var app = new RegisteredApp
        {
            Name = "Notepad",
            Path = @"C:\Windows\notepad.exe",
            Arguments = "test.txt"
        };

        Assert.Equal("Notepad", app.Name);
        Assert.Equal(@"C:\Windows\notepad.exe", app.Path);
        Assert.Equal("test.txt", app.Arguments);
    }

    // ── RegisteredApp.Create ──────────────────────────────────────

    [Fact]
    public void Create_SetsNameAndPath()
    {
        var app = RegisteredApp.Create("Notepad", @"C:\Windows\notepad.exe");
        Assert.Equal("Notepad", app.Name);
        Assert.Equal(@"C:\Windows\notepad.exe", app.Path);
        Assert.Null(app.Arguments);
    }

    [Fact]
    public void Create_WithArguments_SetsArguments()
    {
        var app = RegisteredApp.Create("Notepad", @"C:\Windows\notepad.exe", "test.txt");
        Assert.Equal("test.txt", app.Arguments);
    }

    // ── AppRegistry CRUD ──────────────────────────────────────────

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyList()
    {
        var apps = AppRegistry.Load();
        Assert.NotNull(apps);
        Assert.Empty(apps);
    }

    [Fact]
    public void Add_And_GetByName()
    {
        var app = RegisteredApp.Create("TestApp", @"C:\test.exe");
        AppRegistry.Add(app);

        var found = AppRegistry.GetByName("TestApp");
        Assert.NotNull(found);
        Assert.Equal("TestApp", found.Name);
        Assert.Equal(@"C:\test.exe", found.Path);

        // Cleanup
        AppRegistry.Remove("TestApp");
    }

    [Fact]
    public void Add_DuplicateName_ThrowsLauncherException()
    {
        var app = RegisteredApp.Create("DupApp", @"C:\test.exe");
        AppRegistry.Add(app);

        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Add(app));
        Assert.Equal(ErrorCode.DuplicateAppName, ex.ErrorCode);

        // Cleanup
        AppRegistry.Remove("DupApp");
    }

    [Fact]
    public void Add_DuplicateNameCaseInsensitive_ThrowsLauncherException()
    {
        var app1 = RegisteredApp.Create("MyApp", @"C:\test1.exe");
        var app2 = RegisteredApp.Create("myapp", @"C:\test2.exe");
        AppRegistry.Add(app1);

        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Add(app2));
        Assert.Equal(ErrorCode.DuplicateAppName, ex.ErrorCode);

        // Cleanup
        AppRegistry.Remove("MyApp");
    }

    [Fact]
    public void Remove_ExistingApp_Succeeds()
    {
        var app = RegisteredApp.Create("ToRemove", @"C:\test.exe");
        AppRegistry.Add(app);
        AppRegistry.Remove("ToRemove");

        var found = AppRegistry.GetByName("ToRemove");
        Assert.Null(found);
    }

    [Fact]
    public void Remove_NonExistentApp_ThrowsLauncherException()
    {
        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Remove("NonExistent"));
        Assert.Equal(ErrorCode.AppNotFound, ex.ErrorCode);
    }

    [Fact]
    public void GetByName_NonExistent_ReturnsNull()
    {
        var found = AppRegistry.GetByName("DoesNotExist");
        Assert.Null(found);
    }

    [Fact]
    public void GetAll_ReturnsAllApps()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\app1.exe");
        var app2 = RegisteredApp.Create("App2", @"C:\app2.exe");
        AppRegistry.Add(app1);
        AppRegistry.Add(app2);

        var all = AppRegistry.GetAll();
        Assert.True(all.Count >= 2);

        // Cleanup
        AppRegistry.Remove("App1");
        AppRegistry.Remove("App2");
    }

    // ── AppRegistry.Search ────────────────────────────────────────

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var app = RegisteredApp.Create("SearchTest", @"C:\test.exe");
        AppRegistry.Add(app);

        var results = AppRegistry.Search("");
        Assert.NotEmpty(results);

        // Cleanup
        AppRegistry.Remove("SearchTest");
    }

    [Fact]
    public void Search_MatchingQuery_ReturnsMatches()
    {
        var app = RegisteredApp.Create("NotePad", @"C:\Windows\notepad.exe");
        AppRegistry.Add(app);

        var results = AppRegistry.Search("note");
        Assert.Single(results);
        Assert.Equal("NotePad", results[0].Name);

        // Cleanup
        AppRegistry.Remove("NotePad");
    }

    [Fact]
    public void Search_NonMatchingQuery_ReturnsEmpty()
    {
        var app = RegisteredApp.Create("UniqueApp", @"C:\test.exe");
        AppRegistry.Add(app);

        var results = AppRegistry.Search("zzzznonexistent");
        Assert.Empty(results);

        // Cleanup
        AppRegistry.Remove("UniqueApp");
    }

    // ── LauncherException ─────────────────────────────────────────

    [Fact]
    public void LauncherException_HasCorrectErrorCode()
    {
        var ex = new LauncherException(ErrorCode.AppNotFound, "not found");
        Assert.Equal(ErrorCode.AppNotFound, ex.ErrorCode);
        Assert.Equal("not found", ex.Message);
    }

    [Fact]
    public void LauncherException_CosmosException_Inheritance()
    {
        var ex = new LauncherException(ErrorCode.DuplicateAppName, "dup");
        Assert.IsAssignableFrom<CosmosException>(ex);
    }

    // ── Client.OpenRegisteredApp ──────────────────────────────────

    [Fact]
    public void Client_OpenRegisteredApp_CreatesCorrectRequest()
    {
        var request = client.Client.OpenRegisteredApp("MyApp");
        Assert.Equal("OpenRegisteredApp", request.request);
        Assert.Single(request.args);
        Assert.Equal("MyApp", request.args[0]);
    }

    // ── ErrorCode launcher range ──────────────────────────────────

    [Fact]
    public void ErrorCode_LauncherRange_ValuesAreCorrect()
    {
        Assert.Equal(7001, (int)ErrorCode.AppNotFound);
        Assert.Equal(7002, (int)ErrorCode.AppPathInvalid);
        Assert.Equal(7003, (int)ErrorCode.DuplicateAppName);
        Assert.Equal(7004, (int)ErrorCode.AppRegistryLoadFailed);
    }
}
