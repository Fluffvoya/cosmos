using System.Text.Json;
using cosmos_error;

namespace public_model;

public class Request
{
    public string request { get; set; }
    public List<string> args { get; set; }

    public Request()
    {
        request = "";
        args = new List<string>();
    }

    public Request(string request_, params List<string> args_)
    {
        if (string.IsNullOrEmpty(request_))
            throw new PublicModelException(
                ErrorCode.EmptyRequestName,
                "Request name cannot be null or empty.");

        request = request_;
        args = args_;
    }

    public string Serialize()
    {
        try
        {
            return JsonSerializer.Serialize(this);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonSerializeFailed,
                $"Failed to serialize Request to JSON: {ex.Message}");
        }
    }

    public static Request? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Request? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Request>(text);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Request: {ex.Message}");
        }

        return jsonStruct;
    }
}
