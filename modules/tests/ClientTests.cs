using System.Text.Json;
using client;
using cosmos_error;
using public_model;

namespace tests;

/// <summary>
/// Unit tests for the client module: Request, Response, and Client.
/// </summary>
public class ClientTests
{
    // ── Request (default constructor) ──────────────────────────────

    [Fact]
    public void Request_DefaultConstructor_HasEmptyValues()
    {
        var req = new Request();

        Assert.Equal("", req.request);
        Assert.Empty(req.args);
    }

    // ── Request (parameterized constructor) ────────────────────────

    [Fact]
    public void Request_Constructor_SetsRequestAndArgs()
    {
        var req = new Request("ShowWindow", "myWindow", "hello");

        Assert.Equal("ShowWindow", req.request);
        Assert.Equal(2, req.args.Count);
        Assert.Equal("myWindow", req.args[0]);
        Assert.Equal("hello", req.args[1]);
    }

    [Fact]
    public void Request_WithSingleArg_StoresSingleArg()
    {
        var req = new Request("Single", "onlyArg");

        Assert.Single(req.args);
        Assert.Equal("onlyArg", req.args[0]);
    }

    [Fact]
    public void Request_WithNoArgs_HasEmptyArgs()
    {
        var req = new Request("NoArgs");

        Assert.Empty(req.args);
    }

    [Fact]
    public void Request_EmptyRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Request(""));
        Assert.Equal(ErrorCode.EmptyRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Request_NullRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Request(null!));
        Assert.Equal(ErrorCode.EmptyRequestName, ex.ErrorCode);
    }

    // ── Request serialization ──────────────────────────────────────

    [Fact]
    public void Request_Serialize_ReturnsValidJson()
    {
        var req = new Request("Test", "arg1");
        var json = req.Serialize();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Test", root.GetProperty("request").GetString());
        Assert.Equal("arg1", root.GetProperty("args")[0].GetString());
    }

    [Fact]
    public void Request_Serialize_NoArgs_HasEmptyArgsArray()
    {
        var req = new Request("Test");
        var json = req.Serialize();

        using var doc = JsonDocument.Parse(json);
        var argsArray = doc.RootElement.GetProperty("args");

        Assert.Equal(0, argsArray.GetArrayLength());
    }

    // ── Request deserialization ────────────────────────────────────

    [Fact]
    public void Request_Deserialize_ValidJson_ReturnsRequest()
    {
        var json = """{"request":"ShowWindow","args":["win1","hello"]}""";

        var result = Request.Deserialize(json);

        Assert.NotNull(result);
        Assert.Equal("ShowWindow", result.request);
        Assert.Equal(2, result.args.Count);
        Assert.Equal("win1", result.args[0]);
        Assert.Equal("hello", result.args[1]);
    }

    [Fact]
    public void Request_Deserialize_EmptyString_ReturnsNull()
    {
        var result = Request.Deserialize("");

        Assert.Null(result);
    }

    [Fact]
    public void Request_Deserialize_InvalidJson_ThrowsPublicModelException()
    {
        Assert.Throws<PublicModelException>(() => Request.Deserialize("not valid json"));
    }

    // ── Response ──────────────────────────────────────────────────

    [Fact]
    public void Response_Constructor_StoresFields()
    {
        var resp = new Response("GetUserName", "Alice");

        Assert.Equal("GetUserName", resp.request);
        Assert.Equal("Alice", resp.message);
    }

    [Fact]
    public void Response_Constructor_EmptyRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Response("", "msg"));
        Assert.Equal(ErrorCode.EmptyResponseRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Response_Constructor_NullRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Response(null!, "msg"));
        Assert.Equal(ErrorCode.EmptyResponseRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Response_Constructor_EmptyMessage_IsAllowed()
    {
        var resp = new Response("validName", "");

        Assert.Equal("validName", resp.request);
        Assert.Equal("", resp.message);
    }

    // ── Response serialization ─────────────────────────────────────

    [Fact]
    public void Response_Serialize_ReturnsValidJson()
    {
        var resp = new Response("GetUserName", "Alice");
        var json = resp.Serialize();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("GetUserName", root.GetProperty("request").GetString());
        Assert.Equal("Alice", root.GetProperty("message").GetString());
    }

    // ── Response deserialization ───────────────────────────────────

    [Fact]
    public void Response_Deserialize_ValidJson_ReturnsResponse()
    {
        var json = """{"request":"GetUserName","message":"Alice"}""";

        var result = Response.Deserialize(json);

        Assert.NotNull(result);
        Assert.Equal("GetUserName", result.request);
        Assert.Equal("Alice", result.message);
    }

    [Fact]
    public void Response_Deserialize_EmptyString_ReturnsNull()
    {
        var result = Response.Deserialize("");

        Assert.Null(result);
    }

    [Fact]
    public void Response_Deserialize_InvalidJson_ThrowsPublicModelException()
    {
        Assert.Throws<PublicModelException>(() => Response.Deserialize("not valid json"));
    }

    // ── Client.ShowWindow ──────────────────────────────────────────

    [Fact]
    public void Client_ShowWindow_ReturnsCorrectRequest()
    {
        var req = Client.ShowWindow("myWindow", "Hello, World!");

        Assert.Equal("ShowWindow", req.request);
        Assert.Equal(2, req.args.Count);
        Assert.Equal("myWindow", req.args[0]);
        Assert.Equal("Hello, World!", req.args[1]);
    }

    [Fact]
    public void Client_ShowWindow_EmptyArgs_ReturnsEmptyStrings()
    {
        var req = Client.ShowWindow("", "");

        Assert.Equal("ShowWindow", req.request);
        Assert.Equal("", req.args[0]);
        Assert.Equal("", req.args[1]);
    }

    // ── Client.GetUserName ─────────────────────────────────────────

    [Fact]
    public void Client_GetUserName_ReturnsCorrectRequest()
    {
        var req = Client.GetUserName();

        Assert.Equal("GetUserName", req.request);
        Assert.Empty(req.args);
    }

    // ── Client.Log ─────────────────────────────────────────────────

    [Fact]
    public void Client_Log_ReturnsCorrectRequest()
    {
        var req = Client.Log("Hello, World!");

        Assert.Equal("Log", req.request);
        Assert.Single(req.args);
        Assert.Equal("Hello, World!", req.args[0]);
    }

    [Fact]
    public void Client_Log_EmptyContent_ReturnsEmptyString()
    {
        var req = Client.Log("");

        Assert.Equal("Log", req.request);
        Assert.Single(req.args);
        Assert.Equal("", req.args[0]);
    }

    [Fact]
    public void Client_Log_WithSpecialCharacters_StoresCorrectly()
    {
        var content = "Line1\nLine2\tTabbed";
        var req = Client.Log(content);

        Assert.Equal("Log", req.request);
        Assert.Single(req.args);
        Assert.Equal(content, req.args[0]);
    }

    [Fact]
    public void Client_Log_WithLongContent_StoresCorrectly()
    {
        var content = new string('A', 10000);
        var req = Client.Log(content);

        Assert.Equal("Log", req.request);
        Assert.Single(req.args);
        Assert.Equal(content, req.args[0]);
    }

    // ── Client.CreateRequest ───────────────────────────────────────

    [Fact]
    public void Client_CreateRequest_ReturnsValidJson()
    {
        var json = Client.CreateRequest(Client.ShowWindow("win1", "hello"));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("ShowWindow", root.GetProperty("request").GetString());
        Assert.Equal("win1", root.GetProperty("args")[0].GetString());
    }

    // ── Client.GetResponse ─────────────────────────────────────────

    [Fact]
    public void Client_GetResponse_ValidJson_ReturnsResponse()
    {
        var json = """{"request":"GetUserName","message":"Alice"}""";

        var result = Client.GetResponse(json);

        Assert.NotNull(result);
        Assert.Equal("GetUserName", result.request);
        Assert.Equal("Alice", result.message);
    }

    [Fact]
    public void Client_GetResponse_EmptyString_ReturnsNull()
    {
        var result = Client.GetResponse("");

        Assert.Null(result);
    }

    [Fact]
    public void Client_GetResponse_InvalidJson_ThrowsPublicModelException()
    {
        Assert.Throws<PublicModelException>(() => Client.GetResponse("not valid json"));
    }

    // ── Client.GetResponseMessage ──────────────────────────────────

    [Fact]
    public void Client_GetResponseMessage_ValidJson_ReturnsMessage()
    {
        var json = """{"request":"GetUserName","message":"Alice"}""";

        var result = Client.GetResponseMessage(json);

        Assert.Equal("Alice", result);
    }

    [Fact]
    public void Client_GetResponseMessage_EmptyString_ReturnsNull()
    {
        var result = Client.GetResponseMessage("");

        Assert.Null(result);
    }

    [Fact]
    public void Client_GetResponseMessage_InvalidJson_ThrowsPublicModelException()
    {
        Assert.Throws<PublicModelException>(() => Client.GetResponseMessage("not valid json"));
    }

    // ── Serialization round-trip ───────────────────────────────────

    [Fact]
    public void Request_SerializeAndDeserialize_RoundTrips()
    {
        var original = new Request("ShowWindow", "win1", "hello");
        var json = original.Serialize();
        var deserialized = Request.Deserialize(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.request, deserialized.request);
        Assert.Equal(original.args, deserialized.args);
    }

    [Fact]
    public void Response_SerializeAndDeserialize_RoundTrips()
    {
        var original = new Response("GetUserName", "Bob");
        var json = original.Serialize();
        var deserialized = Response.Deserialize(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.request, deserialized.request);
        Assert.Equal(original.message, deserialized.message);
    }
}
