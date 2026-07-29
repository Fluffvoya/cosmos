using System.Text.Json;
using cosmos_error;

namespace public_model;

public class Replies
{
    public List<Reply> replies { get; set; }

    public Replies(params List<Reply> replies)
    {
        this.replies = replies ?? throw new PublicModelException(
            ErrorCode.NullInput,
            "Replies list cannot be null.");
    }

    public static Replies Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new PublicModelException(
                ErrorCode.NullInput,
                "Input text cannot be null or empty.");

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

        return jsonStruct ?? new Replies();
    }
}