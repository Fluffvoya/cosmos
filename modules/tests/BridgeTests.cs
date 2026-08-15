using bridge;

namespace tests;

/// <summary>
/// Unit tests for the bridge module: IServer interface contract.
/// Verifies that mock implementations correctly satisfy the interface.
/// </summary>
public class BridgeTests
{
    // ── IServer contract ───────────────────────────────────────────

    private class TestServer : IServer
    {
        public string? LastRequest { get; private set; }
        public string ResponseToReturn { get; set; } = "ok";

        public string Execute(string requests)
        {
            LastRequest = requests;
            return ResponseToReturn;
        }
    }

    [Fact]
    public void Execute_ReceivesRequestString()
    {
        var server = new TestServer();

        server.Execute("""{"request":"Log","args":["hello"]}""");

        Assert.NotNull(server.LastRequest);
        Assert.Contains("Log", server.LastRequest);
    }

    [Fact]
    public void Execute_ReturnsResponseString()
    {
        var server = new TestServer { ResponseToReturn = """{"request":"Log","message":"done"}""" };

        var result = server.Execute("any");

        Assert.Equal("""{"request":"Log","message":"done"}""", result);
    }

    [Fact]
    public void Execute_CalledMultipleTimes_RecordsEachRequest()
    {
        var server = new TestServer();

        server.Execute("first");
        server.Execute("second");

        Assert.Equal("second", server.LastRequest);
    }
}
