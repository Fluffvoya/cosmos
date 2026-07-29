using System.Text.Json;
using cosmos_error;

namespace public_model;

public class Reply
{
    public string request { get; set; }
    public string message { get; set; }

    public Reply(string request, string message)
    {
        if (string.IsNullOrEmpty(request))
            throw new PublicModelException(
                ErrorCode.EmptyReplyRequestName,
                "Reply request name cannot be null or empty.");

        this.request = request;
        this.message = message;
    }
}

public class Replies
{
    public List<Reply> replies { get; set; }

    public Replies(params List<Reply> replies)
    {
        this.replies = replies ?? throw new PublicModelException(
            ErrorCode.NullInput,
            "Replies list cannot be null.");
    }

    public static Replies? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Replies? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Replies>(text);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Replies: {ex.Message}");
        }

        return jsonStruct;
    }
}