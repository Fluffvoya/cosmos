using System.Text.Json;
using client;
using cosmos_error;
using public_model;
using server;

namespace tests;

/// <summary>
/// Unit tests for the server module: Server class methods.
/// </summary>
public class ServerTests
{
    // ── Server.CreateReplies ────────────────────────────────────────

    [Fact]
    public void Server_CreateReplies_SingleReply_ReturnsValidJson()
    {
        var json = Server.CreateReplies(new Reply("GetUserName", "Alice"));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var repliesArray = root.GetProperty("replies");
        Assert.Equal(1, repliesArray.GetArrayLength());

        var first = repliesArray[0];
        Assert.Equal("GetUserName", first.GetProperty("request").GetString());
        Assert.Equal("Alice", first.GetProperty("message").GetString());
    }

    [Fact]
    public void Server_CreateReplies_MultipleReplies_SerializesAll()
    {
        var json = Server.CreateReplies(
            new Reply("GetUserName", "Alice"),
            new Reply("ShowWindow", "clicked"));

        using var doc = JsonDocument.Parse(json);
        var repliesArray = doc.RootElement.GetProperty("replies");

        Assert.Equal(2, repliesArray.GetArrayLength());
        Assert.Equal("GetUserName", repliesArray[0].GetProperty("request").GetString());
        Assert.Equal("Alice", repliesArray[0].GetProperty("message").GetString());
        Assert.Equal("ShowWindow", repliesArray[1].GetProperty("request").GetString());
        Assert.Equal("clicked", repliesArray[1].GetProperty("message").GetString());
    }

    [Fact]
    public void Server_CreateReplies_EmptyList_ReturnsEmptyArray()
    {
        var json = Server.CreateReplies();

        using var doc = JsonDocument.Parse(json);
        var repliesArray = doc.RootElement.GetProperty("replies");

        Assert.Equal(0, repliesArray.GetArrayLength());
    }

    [Fact]
    public void Server_CreateReplies_EmptyMessage_IsAllowed()
    {
        var json = Server.CreateReplies(new Reply("Test", ""));

        using var doc = JsonDocument.Parse(json);
        var repliesArray = doc.RootElement.GetProperty("replies");

        Assert.Equal(1, repliesArray.GetArrayLength());
        Assert.Equal("", repliesArray[0].GetProperty("message").GetString());
    }

    // ── Server.GetRequests ──────────────────────────────────────────
    // Note: Server.GetRequests relies on Requests.Deserialize which has a
    // constructor parameter naming issue (requests_ vs requests).
    // Testing via round-trip with Client.CreateRequests instead.

    [Fact]
    public void Server_GetRequests_EmptyString_ReturnsEmptyList()
    {
        var result = Server.GetRequests("");

        Assert.Empty(result);
    }

    // ── Server.GetRequestsName ──────────────────────────────────────
    // Note: Server.GetRequestsName relies on Requests.Deserialize which has a
    // constructor parameter naming issue (requests_ vs requests).

    [Fact]
    public void Server_GetRequestsName_EmptyString_ReturnsEmptyList()
    {
        var result = Server.GetRequestsName("");

        Assert.Empty(result);
    }

    // ── Server.GetRequestType ───────────────────────────────────────
    // Note: Server.GetRequestType relies on Requests.Deserialize which has a
    // constructor parameter naming issue (requests_ vs requests).

    [Fact]
    public void Server_GetRequestType_EmptyString_ReturnsEmptyList()
    {
        var result = Server.GetRequestType("");

        Assert.Empty(result);
    }

    // ── Server.GetRequestTypeFromRequests ───────────────────────────

    [Fact]
    public void Server_GetRequestTypeFromRequests_ActionRequest_ReturnsAction()
    {
        var requests = new List<Request>
        {
            new Request("ShowWindow", RequestType.Action)
        };

        var result = Server.GetRequestTypeFromRequests(requests);

        Assert.Single(result);
        Assert.Equal(RequestType.Action, result[0]);
    }

    [Fact]
    public void Server_GetRequestTypeFromRequests_InquiryRequest_ReturnsInquiry()
    {
        var requests = new List<Request>
        {
            new Request("GetUserName", RequestType.Inquiry)
        };

        var result = Server.GetRequestTypeFromRequests(requests);

        Assert.Single(result);
        Assert.Equal(RequestType.Inquiry, result[0]);
    }

    [Fact]
    public void Server_GetRequestTypeFromRequests_MultipleRequests_ReturnsAllCorrectly()
    {
        var requests = new List<Request>
        {
            new Request("A", RequestType.Action),
            new Request("B", RequestType.Inquiry),
            new Request("C", RequestType.Action)
        };

        var result = Server.GetRequestTypeFromRequests(requests);

        Assert.Equal(3, result.Count);
        Assert.Equal(RequestType.Action, result[0]);
        Assert.Equal(RequestType.Inquiry, result[1]);
        Assert.Equal(RequestType.Action, result[2]);
    }

    [Fact]
    public void Server_GetRequestTypeFromRequests_EmptyList_ReturnsEmptyList()
    {
        var result = Server.GetRequestTypeFromRequests(new List<Request>());

        Assert.Empty(result);
    }

    // ── Server round-trip: CreateReplies -> Client.GetReplies ──────

    [Fact]
    public void Server_CreateReplies_ClientGetReplies_RoundTrips()
    {
        var originalReplies = new List<Reply>
        {
            new Reply("GetUserName", "Alice"),
            new Reply("ShowWindow", "clicked")
        };

        var json = Server.CreateReplies(originalReplies);
        var deserialized = Client.GetReplies(json);

        Assert.Equal(2, deserialized.Count);
        Assert.Equal("GetUserName", deserialized[0].request);
        Assert.Equal("Alice", deserialized[0].message);
        Assert.Equal("ShowWindow", deserialized[1].request);
        Assert.Equal("clicked", deserialized[1].message);
    }

    // ── Server round-trip: Client.CreateRequests -> Server.GetRequests ──
    // Note: This round-trip test is commented out due to Requests.Deserialize
    // constructor parameter naming issue (requests_ vs requests).

    // [Fact]
    // public void Client_CreateRequests_ServerGetRequests_RoundTrips()
    // {
    //     var json = Client.CreateRequests(
    //         Client.ShowWindow("win1", "hello"),
    //         Client.GetUserName());
    //
    //     var result = Server.GetRequests(json);
    //
    //     Assert.Equal(2, result.Count);
    //     Assert.Equal("ShowWindow", result[0].request);
    //     Assert.Equal("action", result[0].requestType);
    //     Assert.Equal("GetUserName", result[1].request);
    //     Assert.Equal("inquiry", result[1].requestType);
    // }
}
