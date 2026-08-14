using System.Text.Json;
using client;
using public_model;
using server;

namespace tests;

/// <summary>
/// Unit tests for the server module: Server class methods.
/// </summary>
public class ServerTests
{
    // ── Server.CreateResponse ──────────────────────────────────────

    [Fact]
    public void Server_CreateResponse_ReturnsValidJson()
    {
        var json = Server.CreateResponse(new Response("GetUserName", "Alice"));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("GetUserName", root.GetProperty("request").GetString());
        Assert.Equal("Alice", root.GetProperty("message").GetString());
    }

    [Fact]
    public void Server_CreateResponse_EmptyMessage_IsAllowed()
    {
        var json = Server.CreateResponse(new Response("Test", ""));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Test", root.GetProperty("request").GetString());
        Assert.Equal("", root.GetProperty("message").GetString());
    }

    // ── Server.GetRequest ──────────────────────────────────────────

    [Fact]
    public void Server_GetRequest_EmptyString_ReturnsNull()
    {
        var result = Server.GetRequest("");

        Assert.Null(result);
    }

    [Fact]
    public void Server_GetRequest_ValidJson_ReturnsRequest()
    {
        var json = """{"request":"MessageBox","args":["win1","hello"]}""";

        var result = Server.GetRequest(json);

        Assert.NotNull(result);
        Assert.Equal("MessageBox", result.request);
        Assert.Equal(2, result.args.Count);
    }

    // ── Server.GetRequestName ──────────────────────────────────────

    [Fact]
    public void Server_GetRequestName_EmptyString_ReturnsNull()
    {
        var result = Server.GetRequestName("");

        Assert.Null(result);
    }

    [Fact]
    public void Server_GetRequestName_ValidJson_ReturnsName()
    {
        var json = """{"request":"MessageBox","args":["win1"]}""";

        var result = Server.GetRequestName(json);

        Assert.Equal("MessageBox", result);
    }

    // ── Round-trip: CreateResponse -> Client.GetResponse ────────────

    [Fact]
    public void Server_CreateResponse_ClientGetResponse_RoundTrips()
    {
        var original = new Response("GetUserName", "Alice");

        var json = Server.CreateResponse(original);
        var deserialized = Client.GetResponse(json);

        Assert.NotNull(deserialized);
        Assert.Equal("GetUserName", deserialized.request);
        Assert.Equal("Alice", deserialized.message);
    }

    // ── Round-trip: Client.CreateRequest -> Server.GetRequest ──────

    [Fact]
    public void Client_CreateRequest_ServerGetRequest_RoundTrips()
    {
        var json = Client.CreateRequest(Client.MessageBox("win1", "hello"));

        var result = Server.GetRequest(json);

        Assert.NotNull(result);
        Assert.Equal("MessageBox", result.request);
        Assert.Equal(2, result.args.Count);
        Assert.Equal("win1", result.args[0]);
        Assert.Equal("hello", result.args[1]);
    }
}
