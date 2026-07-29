using System.Text.Json;
using cosmos_error;

namespace client;

public enum RequestType
{
    Action,
    Inquiry,// expect a reply
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
        request = request_;
        var requestTypeStr = requestType_ switch
        {
            RequestType.Action => "action",
            RequestType.Inquiry => "inquiry",
            _ => ""
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
            throw new ClientException(
                ErrorCode.JsonSerializeFailed,
                $"Failed to serialize Requests to JSON: {ex.Message}");
        }
    }
}