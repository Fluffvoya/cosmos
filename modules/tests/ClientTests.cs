using System.Text.Json;
using client;
using cosmos_error;
using public_model;

namespace tests;

/// <summary>
/// Unit tests for the client module: Request, Requests, Reply, Replies, and Client.
/// </summary>
public class ClientTests
{
    // ── Request (default constructor) ──────────────────────────────

    [Fact]
    public void Request_DefaultConstructor_HasEmptyValues()
    {
        var req = new Request();

        Assert.Equal("", req.request);
        Assert.Equal("", req.requestType);
        Assert.Empty(req.args);
    }

    // ── Request (parameterized constructor) ────────────────────────

    [Fact]
    public void Request_ActionType_SetsRequestTypeToAction()
    {
        var req = new Request("DoSomething", RequestType.Action);

        Assert.Equal("DoSomething", req.request);
        Assert.Equal("action", req.requestType);
        Assert.Empty(req.args);
    }

    [Fact]
    public void Request_InquiryType_SetsRequestTypeToInquiry()
    {
        var req = new Request("AskSomething", RequestType.Inquiry);

        Assert.Equal("AskSomething", req.request);
        Assert.Equal("inquiry", req.requestType);
        Assert.Empty(req.args);
    }

    [Fact]
    public void Request_WithArgs_StoresArgsCorrectly()
    {
        var req = new Request("ShowWindow", RequestType.Action, "myWindow", "hello");

        Assert.Equal("ShowWindow", req.request);
        Assert.Equal("action", req.requestType);
        Assert.Equal(2, req.args.Count);
        Assert.Equal("myWindow", req.args[0]);
        Assert.Equal("hello", req.args[1]);
    }

    [Fact]
    public void Request_WithSingleArg_StoresSingleArg()
    {
        var req = new Request("Single", RequestType.Inquiry, "onlyArg");

        Assert.Single(req.args);
        Assert.Equal("onlyArg", req.args[0]);
    }

    [Fact]
    public void Request_EmptyRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Request("", RequestType.Action));
        Assert.Equal(ErrorCode.EmptyRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Request_NullRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Request(null!, RequestType.Action));
        Assert.Equal(ErrorCode.EmptyRequestName, ex.ErrorCode);
    }

    // ── Requests ───────────────────────────────────────────────────

    [Fact]
    public void Requests_Constructor_StoresRequestList()
    {
        var list = new List<Request>
        {
            new Request("A", RequestType.Action),
            new Request("B", RequestType.Inquiry)
        };
        var requests = new Requests(list);

        Assert.Equal(2, requests.requests.Count);
        Assert.Equal("A", requests.requests[0].request);
        Assert.Equal("B", requests.requests[1].request);
    }

    [Fact]
    public void Requests_Constructor_NullList_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Requests(null!));
        Assert.Equal(ErrorCode.NullInput, ex.ErrorCode);
    }

    [Fact]
    public void Requests_Serialize_ReturnsValidJson()
    {
        var list = new List<Request> { new Request("Test", RequestType.Action, "arg1") };
        var requests = new Requests(list);
        var json = requests.Serialize();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var requestsArray = root.GetProperty("requests");
        Assert.Equal(1, requestsArray.GetArrayLength());

        var first = requestsArray[0];
        Assert.Equal("Test", first.GetProperty("request").GetString());
        Assert.Equal("action", first.GetProperty("requestType").GetString());
        Assert.Equal("arg1", first.GetProperty("args")[0].GetString());
    }

    [Fact]
    public void Requests_Serialize_MultipleRequests_SerializesAll()
    {
        var list = new List<Request>
        {
            new Request("First", RequestType.Action, "a"),
            new Request("Second", RequestType.Inquiry, "b", "c")
        };
        var requests = new Requests(list);
        var json = requests.Serialize();

        using var doc = JsonDocument.Parse(json);
        var requestsArray = doc.RootElement.GetProperty("requests");

        Assert.Equal(2, requestsArray.GetArrayLength());
        Assert.Equal("First", requestsArray[0].GetProperty("request").GetString());
        Assert.Equal("Second", requestsArray[1].GetProperty("request").GetString());
        Assert.Equal(2, requestsArray[1].GetProperty("args").GetArrayLength());
    }

    [Fact]
    public void Requests_Serialize_EmptyRequests_ReturnsEmptyArray()
    {
        var requests = new Requests(new List<Request>());
        var json = requests.Serialize();

        using var doc = JsonDocument.Parse(json);
        var requestsArray = doc.RootElement.GetProperty("requests");

        Assert.Equal(0, requestsArray.GetArrayLength());
    }

    // ── Reply ──────────────────────────────────────────────────────

    [Fact]
    public void Reply_Constructor_StoresFields()
    {
        var reply = new Reply("GetUserName", "Alice");

        Assert.Equal("GetUserName", reply.requestName);
        Assert.Equal("Alice", reply.message);
    }

    [Fact]
    public void Reply_Constructor_EmptyRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Reply("", "msg"));
        Assert.Equal(ErrorCode.EmptyReplyRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Reply_Constructor_NullRequestName_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Reply(null!, "msg"));
        Assert.Equal(ErrorCode.EmptyReplyRequestName, ex.ErrorCode);
    }

    [Fact]
    public void Reply_Constructor_EmptyMessage_IsAllowed()
    {
        var reply = new Reply("validName", "");

        Assert.Equal("validName", reply.requestName);
        Assert.Equal("", reply.message);
    }

    // ── Replies ────────────────────────────────────────────────────

    [Fact]
    public void Replies_Constructor_StoresReplyList()
    {
        var list = new List<Reply> { new Reply("A", "msg1"), new Reply("B", "msg2") };
        var replies = new Replies(list);

        Assert.Equal(2, replies.replies.Count);
        Assert.Equal("msg1", replies.replies[0].message);
        Assert.Equal("msg2", replies.replies[1].message);
    }

    [Fact]
    public void Replies_Constructor_NullList_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => new Replies(null!));
        Assert.Equal(ErrorCode.NullInput, ex.ErrorCode);
    }

    [Fact]
    public void Replies_Deserialize_ValidJson_ReturnsReplies()
    {
        var json = """{"replies":[{"requestName":"GetUserName","message":"Alice"}]}""";

        var result = Replies.Deserialize(json);

        Assert.NotNull(result);
        Assert.Single(result.replies);
        Assert.Equal("GetUserName", result.replies[0].requestName);
        Assert.Equal("Alice", result.replies[0].message);
    }

    [Fact]
    public void Replies_Deserialize_MultipleReplies_ReturnsAll()
    {
        var json = """{"replies":[{"requestName":"A","message":"msg1"},{"requestName":"B","message":"msg2"}]}""";

        var result = Replies.Deserialize(json);

        Assert.Equal(2, result.replies.Count);
        Assert.Equal("msg1", result.replies[0].message);
        Assert.Equal("msg2", result.replies[1].message);
    }

    [Fact]
    public void Replies_Deserialize_EmptyRepliesArray_ReturnsEmptyList()
    {
        var json = """{"replies":[]}""";

        var result = Replies.Deserialize(json);

        Assert.NotNull(result);
        Assert.Empty(result.replies);
    }

    [Fact]
    public void Replies_Deserialize_InvalidJson_ThrowsPublicModelException()
    {
        var invalidJson = "this is not json";

        var ex = Assert.Throws<PublicModelException>(() => Replies.Deserialize(invalidJson));
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
    }

    [Fact]
    public void Replies_Deserialize_EmptyString_ThrowsPublicModelException()
    {
        var ex = Assert.Throws<PublicModelException>(() => Replies.Deserialize(""));
        Assert.Equal(ErrorCode.NullInput, ex.ErrorCode);
    }

    // ── Client.ShowWindow ──────────────────────────────────────────

    [Fact]
    public void Client_ShowWindow_ReturnsCorrectRequest()
    {
        var req = Client.ShowWindow("myWindow", "Hello, World!");

        Assert.Equal("ShowWindow", req.request);
        Assert.Equal("action", req.requestType);
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
        Assert.Equal("inquiry", req.requestType);
        Assert.Empty(req.args);
    }

    // ── Client.CreateRequests ──────────────────────────────────────

    [Fact]
    public void Client_CreateRequests_SingleRequest_ReturnsValidJson()
    {
        var json = Client.CreateRequests(Client.ShowWindow("win1", "hello"));

        using var doc = JsonDocument.Parse(json);
        var requestsArray = doc.RootElement.GetProperty("requests");

        Assert.Equal(1, requestsArray.GetArrayLength());
        Assert.Equal("ShowWindow", requestsArray[0].GetProperty("request").GetString());
        Assert.Equal("action", requestsArray[0].GetProperty("requestType").GetString());
    }

    [Fact]
    public void Client_CreateRequests_MultipleRequests_ReturnsAll()
    {
        var json = Client.CreateRequests(
            Client.ShowWindow("win1", "hello"),
            Client.GetUserName());

        using var doc = JsonDocument.Parse(json);
        var requestsArray = doc.RootElement.GetProperty("requests");

        Assert.Equal(2, requestsArray.GetArrayLength());
        Assert.Equal("ShowWindow", requestsArray[0].GetProperty("request").GetString());
        Assert.Equal("GetUserName", requestsArray[1].GetProperty("request").GetString());
    }

    [Fact]
    public void Client_CreateRequests_EmptyList_ReturnsEmptyArray()
    {
        var json = Client.CreateRequests();

        using var doc = JsonDocument.Parse(json);
        var requestsArray = doc.RootElement.GetProperty("requests");

        Assert.Equal(0, requestsArray.GetArrayLength());
    }

    // ── Serialization round-trip ───────────────────────────────────

    [Fact]
    public void Request_SerializeAndDeserialize_RoundTrips()
    {
        var original = new Request("ShowWindow", RequestType.Action, "win1", "hello");
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Request>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.request, deserialized.request);
        Assert.Equal(original.requestType, deserialized.requestType);
        Assert.Equal(original.args, deserialized.args);
    }

    [Fact]
    public void Reply_SerializeAndDeserialize_RoundTrips()
    {
        var original = new Reply("GetUserName", "Bob");
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Reply>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.requestName, deserialized.requestName);
        Assert.Equal(original.message, deserialized.message);
    }
}
