using System.Text.Json;
using cosmos_error;

namespace public_model;

public class Response
{
    public string request { get; set; }
    public string message { get; set; }

    public Response()
    {
        request = "";
        message = "";
    }

    public Response(string request, string message)
    {
        if (string.IsNullOrEmpty(request))
            throw new PublicModelException(
                ErrorCode.EmptyResponseRequestName,
                "Response request name cannot be null or empty.");

        this.request = request;
        this.message = message;
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
                $"Failed to serialize Response to JSON: {ex.Message}");
        }
    }

    public static Response? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Response? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Response>(text);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Response: {ex.Message}");
        }

        return jsonStruct;
    }
}
