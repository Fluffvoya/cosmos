using System.Text.Json;
using bridge;
using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the controller module: Request, Requests, Reply, Replies, and Controller.
/// </summary>
public class ControllerTests
{
    // ── Request (default constructor) ──────────────────────────────

    [Fact]
    public void Request_DefaultConstructor_HasEmptyValues()
    {
        var req = new Request();

        Assert.Equal("", req.name);
        Assert.Equal("", req.requestType);
        Assert.Empty(req.args);
    }

    // ── Request (parameterized constructor) ────────────────────────

    [Fact]
    public void Request_ActionType_SetsRequestTypeToAction()
    {
        var req = new Request("DoSomething", RequestType.Action);

        Assert.Equal("DoSomething", req.name);
        Assert.Equal("action", req.requestType);
        Assert.Empty(req.args);
    }

    [Fact]
    public void Request_InquiryType_SetsRequestTypeToInquiry()
    {
        var req = new Request("AskSomething", RequestType.Inquiry);

        Assert.Equal("AskSomething", req.name);
        Assert.Equal("inquiry", req.requestType);
        Assert.Empty(req.args);
    }

    [Fact]
    public void Request_WithArgs_StoresArgsCorrectly()
    {
        var req = new Request("ShowWindow", RequestType.Action, "myWindow", "hello");

        Assert.Equal("ShowWindow", req.name);
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
        Assert.Equal("A", requests.requests[0].name);
        Assert.Equal("B", requests.requests[1].name);
    }

    [Fact]
    public void Requests_Emit_WritesValidJsonToStdout()
    {
        var originalOut = Console.Out;
        try
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            var list = new List<Request> { new Request("Test", RequestType.Action, "arg1") };
            var requests = new Requests(list);
            requests.Emit();

            var json = writer.ToString().Trim();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var requestsArray = root.GetProperty("requests");
            Assert.Equal(1, requestsArray.GetArrayLength());

            var first = requestsArray[0];
            Assert.Equal("Test", first.GetProperty("name").GetString());
            Assert.Equal("action", first.GetProperty("requestType").GetString());
            Assert.Equal("arg1", first.GetProperty("args")[0].GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Requests_Emit_MultipleRequests_SerializesAll()
    {
        var originalOut = Console.Out;
        try
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            var list = new List<Request>
            {
                new Request("First", RequestType.Action, "a"),
                new Request("Second", RequestType.Inquiry, "b", "c")
            };
            var requests = new Requests(list);
            requests.Emit();

            var json = writer.ToString().Trim();
            using var doc = JsonDocument.Parse(json);
            var requestsArray = doc.RootElement.GetProperty("requests");

            Assert.Equal(2, requestsArray.GetArrayLength());
            Assert.Equal("First", requestsArray[0].GetProperty("name").GetString());
            Assert.Equal("Second", requestsArray[1].GetProperty("name").GetString());
            Assert.Equal(2, requestsArray[1].GetProperty("args").GetArrayLength());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
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
    public void Reply_Constructor_WithEmptyStrings()
    {
        var reply = new Reply("", "");

        Assert.Equal("", reply.requestName);
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
    public void Replies_Deserialize_InvalidJson_ThrowsControllerException()
    {
        var invalidJson = "this is not json";

        var ex = Assert.Throws<ControllerException>(() => Replies.Deserialize(invalidJson));
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
    }

    [Fact]
    public void Replies_Deserialize_EmptyString_ThrowsControllerException()
    {
        var ex = Assert.Throws<ControllerException>(() => Replies.Deserialize(""));
        Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
    }

    // ── Controller.ShowWindow ──────────────────────────────────────

    [Fact]
    public void Controller_ShowWindow_ReturnsCorrectRequest()
    {
        var req = Controller.ShowWindow("myWindow", "Hello, World!");

        Assert.Equal("ShowWindow", req.name);
        Assert.Equal("action", req.requestType);
        Assert.Equal(2, req.args.Count);
        Assert.Equal("myWindow", req.args[0]);
        Assert.Equal("Hello, World!", req.args[1]);
    }

    [Fact]
    public void Controller_ShowWindow_EmptyArgs_ReturnsEmptyStrings()
    {
        var req = Controller.ShowWindow("", "");

        Assert.Equal("ShowWindow", req.name);
        Assert.Equal("", req.args[0]);
        Assert.Equal("", req.args[1]);
    }

    // ── Controller.GetUserName ─────────────────────────────────────

    [Fact]
    public void Controller_GetUserName_ReturnsCorrectRequest()
    {
        var req = Controller.GetUserName();

        Assert.Equal("GetUserName", req.name);
        Assert.Equal("inquiry", req.requestType);
        Assert.Empty(req.args);
    }

    // ── Controller.Emit ────────────────────────────────────────────

    [Fact]
    public void Controller_Emit_WritesValidJsonToStdout()
    {
        var originalOut = Console.Out;
        try
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            Controller.Emit(
                new Request("ShowWindow", RequestType.Action, "win", "msg"),
                new Request("GetUserName", RequestType.Inquiry)
            );

            var json = writer.ToString().Trim();
            using var doc = JsonDocument.Parse(json);
            var requestsArray = doc.RootElement.GetProperty("requests");

            Assert.Equal(2, requestsArray.GetArrayLength());
            Assert.Equal("ShowWindow", requestsArray[0].GetProperty("name").GetString());
            Assert.Equal("GetUserName", requestsArray[1].GetProperty("name").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Controller_Emit_SingleRequest_WritesSingleRequest()
    {
        var originalOut = Console.Out;
        try
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            Controller.Emit(new Request("Test", RequestType.Action));

            var json = writer.ToString().Trim();
            using var doc = JsonDocument.Parse(json);
            var requestsArray = doc.RootElement.GetProperty("requests");

            Assert.Equal(1, requestsArray.GetArrayLength());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ── Controller.Receive ─────────────────────────────────────────

    [Fact]
    public void ControllerReceive_ValidJson_ReturnsReplies()
    {
        var originalIn = Console.In;
        try
        {
            var json = """{"replies":[{"requestName":"GetUserName","message":"Alice"}]}""";
            Console.SetIn(new StringReader(json));

            Controller.Receive(out var replies);

            Assert.Single(replies);
            Assert.Equal("GetUserName", replies[0].requestName);
            Assert.Equal("Alice", replies[0].message);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void ControllerReceive_NullInput_ReturnsEmptyReplies()
    {
        var originalIn = Console.In;
        try
        {
            // Empty stream yields null from Console.ReadLine()
            Console.SetIn(new StringReader(""));

            Controller.Receive(out var replies);

            Assert.NotNull(replies);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void ControllerReceive_InvalidJson_ThrowsControllerException()
    {
        var originalIn = Console.In;
        try
        {
            Console.SetIn(new StringReader("not valid json!!!"));

            var ex = Assert.Throws<ControllerException>(() => Controller.Receive(out _));
            Assert.Equal(ErrorCode.JsonDeserializeFailed, ex.ErrorCode);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    // ── Controller.ReceiveMessageOnly ──────────────────────────────

    [Fact]
    public void ControllerReceiveMessageOnly_ReturnsOnlyMessages()
    {
        var originalIn = Console.In;
        try
        {
            var json = """{"replies":[{"requestName":"A","message":"hello"},{"requestName":"B","message":"world"}]}""";
            Console.SetIn(new StringReader(json));

            Controller.ReceiveMessageOnly(out var messages);

            Assert.Equal(2, messages.Count);
            Assert.Equal("hello", messages[0]);
            Assert.Equal("world", messages[1]);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void ControllerReceiveMessageOnly_EmptyReplies_ReturnsEmptyList()
    {
        var originalIn = Console.In;
        try
        {
            var json = """{"replies":[]}""";
            Console.SetIn(new StringReader(json));

            Controller.ReceiveMessageOnly(out var messages);

            Assert.Empty(messages);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    // ── Serialization round-trip ───────────────────────────────────

    [Fact]
    public void Request_SerializeAndDeserialize_RoundTrips()
    {
        var original = new Request("ShowWindow", RequestType.Action, "win1", "hello");
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Request>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.name, deserialized.name);
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
