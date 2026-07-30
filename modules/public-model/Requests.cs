using System.Text.Json;
using cosmos_error;

namespace public_model;

public enum RequestType
{
    Action,
    Inquiry,// expect a reply
    Unknown,
}

public class Request
{
    public string request { get; set; }
    public string requestType { get; set; }
    public List<string> args { get; set; }

    public Request()
    {
        request = "";
        requestType = "";
        args = new List<string>();
    }

    public Request(string request_, RequestType requestType_, params List<string> args_)
    {
        if (string.IsNullOrEmpty(request_))
            throw new PublicModelException(
                ErrorCode.EmptyRequestName,
                "Request name cannot be null or empty.");

        request = request_;
        var requestTypeStr = requestType_ switch
        {
            RequestType.Action => "action",
            RequestType.Inquiry => "inquiry",
            _ => throw new PublicModelException(
                ErrorCode.InvalidRequestType,
                $"Invalid request type: {requestType_}")
        };
        requestType = requestTypeStr;
        args = args_;
    }
}

public class Requests
{
    public Requests(params List<Request> requests_)
    {
        requests = requests_;
    }

    public List<Request> requests { get; set; }
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
                $"Failed to serialize Requests to JSON: {ex.Message}");
        }
    }

    public static Requests? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Requests? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Requests>(text);
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