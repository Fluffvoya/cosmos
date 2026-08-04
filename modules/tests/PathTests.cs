using cosmos_error;
using path;

namespace tests;

/// <summary>
/// Unit tests for the path module: CosmosPath class and PathException.
/// </summary>
public class PathTests
{
    // ── Constructor ──────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidPath_StoresAbsolutePath()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        Assert.Equal("/usr/local/cosmos", cp.AbsolutePath);
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsPathException()
    {
        var ex = Assert.Throws<PathException>(() => new CosmosPath(null!));

        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsPathException()
    {
        var ex = Assert.Throws<PathException>(() => new CosmosPath(""));

        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void Constructor_ThrowsPathException_InheritsCosmosException()
    {
        var ex = Assert.Throws<PathException>(() => new CosmosPath(""));

        Assert.IsAssignableFrom<CosmosException>(ex);
    }

    // ── Resolve - cosmos:// scheme ───────────────────────────────

    [Fact]
    public void Resolve_CosmosScheme_ReplacesWithAbsolutePath()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var result = cp.Resolve("cosmos://scripts/main.cs");

        Assert.Equal(Path.Combine("/usr/local/cosmos", "scripts/main.cs"), result);
    }

    [Fact]
    public void Resolve_CosmosSchemeWithNestedPath_ResolvesCorrectly()
    {
        var cp = new CosmosPath("/home/user/cosmos");

        var result = cp.Resolve("cosmos://a/b/c/file.txt");

        Assert.Equal(Path.Combine("/home/user/cosmos", "a/b/c/file.txt"), result);
    }

    [Fact]
    public void Resolve_CosmosSchemeOnly_ResolvesToBasePath()
    {
        var cp = new CosmosPath("/cosmos/root");

        var result = cp.Resolve("cosmos://");

        Assert.Equal(Path.Combine("/cosmos/root", ""), result);
    }

    // ── Resolve - regular paths ──────────────────────────────────

    [Fact]
    public void Resolve_AbsolutePath_ReturnsUnchanged()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var result = cp.Resolve("/tmp/test.txt");

        Assert.Equal("/tmp/test.txt", result);
    }

    [Fact]
    public void Resolve_RelativePath_ReturnsUnchanged()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var result = cp.Resolve("scripts/main.cs");

        Assert.Equal("scripts/main.cs", result);
    }

    [Fact]
    public void Resolve_WindowsAbsolutePath_ReturnsUnchanged()
    {
        var cp = new CosmosPath("C:\\cosmos");

        var result = cp.Resolve("D:\\data\\file.txt");

        Assert.Equal("D:\\data\\file.txt", result);
    }

    // ── Resolve - edge cases ─────────────────────────────────────

    [Fact]
    public void Resolve_NullPath_ThrowsPathException()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var ex = Assert.Throws<PathException>(() => cp.Resolve(null!));

        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void Resolve_EmptyPath_ThrowsPathException()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var ex = Assert.Throws<PathException>(() => cp.Resolve(""));

        Assert.Equal(ErrorCode.PathNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void Resolve_PathWithSpaces_PreservedCorrectly()
    {
        var cp = new CosmosPath("/usr/local/cosmos");

        var result = cp.Resolve("cosmos://my scripts/file.cs");

        Assert.Equal(Path.Combine("/usr/local/cosmos", "my scripts/file.cs"), result);
    }
}
