using app_launcher;
using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the app-launcher module: RegisteredApp model and AppRegistry.
/// Each test uses an isolated temp folder to avoid polluting the real user data.
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

    [Fact]
    public void Create_DefaultCategory_IsGeneral()
    {
        var app = RegisteredApp.Create("Notepad", @"C:\Windows\notepad.exe");
        Assert.Equal("General", app.Category);
    }

    [Fact]
    public void Create_WithCategory_SetsCategory()
    {
        var app = RegisteredApp.Create("VSCode", @"C:\vscode.exe", null, "Development");
        Assert.Equal("Development", app.Category);
    }

    [Fact]
    public void RegisteredApp_DefaultConstructor_CategoryIsGeneral()
    {
        var app = new RegisteredApp();
        Assert.Equal("General", app.Category);
    }

    // ── AppRegistry CRUD ──────────────────────────────────────────

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyList()
    {
        var apps = AppRegistry.Load(_testFolder);
        Assert.NotNull(apps);
        Assert.Empty(apps);
    }

    [Fact]
    public void Add_And_GetByName()
    {
        var app = RegisteredApp.Create("TestApp", @"C:\test.exe");
        AppRegistry.Add(app, _testFolder);

        var found = AppRegistry.GetByName("TestApp", _testFolder);
        Assert.NotNull(found);
        Assert.Equal("TestApp", found.Name);
        Assert.Equal(@"C:\test.exe", found.Path);
    }

    [Fact]
    public void Add_DuplicateName_ThrowsLauncherException()
    {
        var app = RegisteredApp.Create("DupApp", @"C:\test.exe");
        AppRegistry.Add(app, _testFolder);

        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Add(app, _testFolder));
        Assert.Equal(ErrorCode.DuplicateAppName, ex.ErrorCode);
    }

    [Fact]
    public void Add_DuplicateNameCaseInsensitive_ThrowsLauncherException()
    {
        var app1 = RegisteredApp.Create("MyApp", @"C:\test1.exe");
        var app2 = RegisteredApp.Create("myapp", @"C:\test2.exe");
        AppRegistry.Add(app1, _testFolder);

        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Add(app2, _testFolder));
        Assert.Equal(ErrorCode.DuplicateAppName, ex.ErrorCode);
    }

    [Fact]
    public void Remove_ExistingApp_Succeeds()
    {
        var app = RegisteredApp.Create("ToRemove", @"C:\test.exe");
        AppRegistry.Add(app, _testFolder);
        AppRegistry.Remove("ToRemove", _testFolder);

        var found = AppRegistry.GetByName("ToRemove", _testFolder);
        Assert.Null(found);
    }

    [Fact]
    public void Remove_NonExistentApp_ThrowsLauncherException()
    {
        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Remove("NonExistent", _testFolder));
        Assert.Equal(ErrorCode.AppNotFound, ex.ErrorCode);
    }

    [Fact]
    public void GetByName_NonExistent_ReturnsNull()
    {
        var found = AppRegistry.GetByName("DoesNotExist", _testFolder);
        Assert.Null(found);
    }

    [Fact]
    public void GetAll_ReturnsAllApps()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\app1.exe");
        var app2 = RegisteredApp.Create("App2", @"C:\app2.exe");
        AppRegistry.Add(app1, _testFolder);
        AppRegistry.Add(app2, _testFolder);

        var all = AppRegistry.GetAll(_testFolder);
        Assert.Equal(2, all.Count);
    }

    // ── AppRegistry.Search ────────────────────────────────────────

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var app = RegisteredApp.Create("SearchTest", @"C:\test.exe");
        AppRegistry.Add(app, _testFolder);

        var results = AppRegistry.Search("", _testFolder);
        Assert.Single(results);
    }

    [Fact]
    public void Search_MatchingQuery_ReturnsMatches()
    {
        var app = RegisteredApp.Create("NotePad", @"C:\Windows\notepad.exe");
        AppRegistry.Add(app, _testFolder);

        var results = AppRegistry.Search("note", _testFolder);
        Assert.Single(results);
        Assert.Equal("NotePad", results[0].Name);
    }

    [Fact]
    public void Search_NonMatchingQuery_ReturnsEmpty()
    {
        var app = RegisteredApp.Create("UniqueApp", @"C:\test.exe");
        AppRegistry.Add(app, _testFolder);

        var results = AppRegistry.Search("zzzznonexistent", _testFolder);
        Assert.Empty(results);
    }

    // ── AppRegistry.GetAllCategories ─────────────────────────────

    [Fact]
    public void GetAllCategories_EmptyRegistry_ReturnsEmptyList()
    {
        var categories = AppRegistry.GetAllCategories(_testFolder);
        Assert.NotNull(categories);
        Assert.Empty(categories);
    }

    [Fact]
    public void GetAllCategories_ReturnsDistinctSortedCategories()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\a.exe", null, "Development");
        var app2 = RegisteredApp.Create("App2", @"C:\b.exe", null, "Games");
        var app3 = RegisteredApp.Create("App3", @"C:\c.exe", null, "Development");
        var app4 = RegisteredApp.Create("App4", @"C:\d.exe"); // defaults to "General"
        AppRegistry.Add(app1, _testFolder);
        AppRegistry.Add(app2, _testFolder);
        AppRegistry.Add(app3, _testFolder);
        AppRegistry.Add(app4, _testFolder);

        var categories = AppRegistry.GetAllCategories(_testFolder);
        Assert.Equal(3, categories.Count);
        Assert.Equal("Development", categories[0]);
        Assert.Equal("Games", categories[1]);
        Assert.Equal("General", categories[2]);
    }

    // ── AppRegistry.GetByCategory ────────────────────────────────

    [Fact]
    public void GetByCategory_ReturnsMatchingApps()
    {
        var app1 = RegisteredApp.Create("VSCode", @"C:\vscode.exe", null, "Development");
        var app2 = RegisteredApp.Create("Notepad", @"C:\notepad.exe", null, "Utilities");
        var app3 = RegisteredApp.Create("PyCharm", @"C:\pycharm.exe", null, "Development");
        AppRegistry.Add(app1, _testFolder);
        AppRegistry.Add(app2, _testFolder);
        AppRegistry.Add(app3, _testFolder);

        var devApps = AppRegistry.GetByCategory("Development", _testFolder);
        Assert.Equal(2, devApps.Count);
        Assert.All(devApps, a => Assert.Equal("Development", a.Category));
    }

    [Fact]
    public void GetByCategory_All_ReturnsAllApps()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\a.exe", null, "Development");
        var app2 = RegisteredApp.Create("App2", @"C:\b.exe", null, "Games");
        AppRegistry.Add(app1, _testFolder);
        AppRegistry.Add(app2, _testFolder);

        var allApps = AppRegistry.GetByCategory("All", _testFolder);
        Assert.Equal(2, allApps.Count);
    }

    [Fact]
    public void GetByCategory_Null_ReturnsAllApps()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\a.exe", null, "Development");
        AppRegistry.Add(app1, _testFolder);

        var allApps = AppRegistry.GetByCategory(null, _testFolder);
        Assert.Single(allApps);
    }

    [Fact]
    public void GetByCategory_CaseInsensitive()
    {
        var app = RegisteredApp.Create("App1", @"C:\a.exe", null, "Development");
        AppRegistry.Add(app, _testFolder);

        var result = AppRegistry.GetByCategory("development", _testFolder);
        Assert.Single(result);
    }

    [Fact]
    public void GetByCategory_NoMatch_ReturnsEmpty()
    {
        var app = RegisteredApp.Create("App1", @"C:\a.exe", null, "Development");
        AppRegistry.Add(app, _testFolder);

        var result = AppRegistry.GetByCategory("Games", _testFolder);
        Assert.Empty(result);
    }

    // ── AppRegistry.Update ────────────────────────────────────────

    [Fact]
    public void Update_ExistingApp_Succeeds()
    {
        var app = RegisteredApp.Create("OldName", @"C:\old.exe", "arg1", "General");
        AppRegistry.Add(app, _testFolder);

        var updated = RegisteredApp.Create("NewName", @"C:\new.exe", "arg2", "Development");
        AppRegistry.Update("OldName", updated, _testFolder);

        var found = AppRegistry.GetByName("NewName", _testFolder);
        Assert.NotNull(found);
        Assert.Equal("NewName", found.Name);
        Assert.Equal(@"C:\new.exe", found.Path);
        Assert.Equal("arg2", found.Arguments);
        Assert.Equal("Development", found.Category);

        // Old name should be gone
        Assert.Null(AppRegistry.GetByName("OldName", _testFolder));
    }

    [Fact]
    public void Update_SameName_Succeeds()
    {
        var app = RegisteredApp.Create("MyApp", @"C:\old.exe", null, "General");
        AppRegistry.Add(app, _testFolder);

        var updated = RegisteredApp.Create("MyApp", @"C:\new.exe", null, "Games");
        AppRegistry.Update("MyApp", updated, _testFolder);

        var found = AppRegistry.GetByName("MyApp", _testFolder);
        Assert.NotNull(found);
        Assert.Equal(@"C:\new.exe", found.Path);
        Assert.Equal("Games", found.Category);
    }

    [Fact]
    public void Update_NameConflict_ThrowsLauncherException()
    {
        var app1 = RegisteredApp.Create("App1", @"C:\a1.exe");
        var app2 = RegisteredApp.Create("App2", @"C:\a2.exe");
        AppRegistry.Add(app1, _testFolder);
        AppRegistry.Add(app2, _testFolder);

        var updated = RegisteredApp.Create("App2", @"C:\a1new.exe");
        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Update("App1", updated, _testFolder));
        Assert.Equal(ErrorCode.DuplicateAppName, ex.ErrorCode);
    }

    [Fact]
    public void Update_NotFound_ThrowsLauncherException()
    {
        var updated = RegisteredApp.Create("Ghost", @"C:\ghost.exe");
        var ex = Assert.Throws<LauncherException>(() => AppRegistry.Update("Ghost", updated, _testFolder));
        Assert.Equal(ErrorCode.AppNotFound, ex.ErrorCode);
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
