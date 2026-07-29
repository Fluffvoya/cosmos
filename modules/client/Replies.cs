using System.Text.Json;
using cosmos_error;

namespace client;

public class Replies
{
    public List<Reply> replies { get; set; }

    public Replies(params List<Reply> replies)
    {
        this.replies = replies;
    }

    public static Replies Deserialize(string text)
    {
        var rns = new Replies();
        Replies? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Replies>(text);
        }
        catch (Exception ex)
        {
            throw new ClientException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Replies: {ex.Message}");
        }

        if (jsonStruct is not null)
            rns = jsonStruct;
        return rns;
    }
}