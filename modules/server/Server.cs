using public_model;

namespace server;

public class Server
{
    public static string CreateResponse(Response response)
    {
        return response.Serialize();
    }

    public static Request? GetRequest(string text)
    {
        return Request.Deserialize(text);
    }

    public static string? GetRequestName(string text)
    {
        var request = GetRequest(text);
        return request?.request;
    }
}
