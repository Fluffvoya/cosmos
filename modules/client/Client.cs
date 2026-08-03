using public_model;

namespace client;

public class Client
{
    public static string CreateRequest(Request request)
    {
        return request.Serialize();
    }

    public static Response? GetResponse(string text)
    {
        return Response.Deserialize(text);
    }

    public static string? GetResponseMessage(string text)
    {
        var response = GetResponse(text);
        return response?.message;
    }

    public static Request ShowWindow(string name, string message)
        => new Request("ShowWindow", name, message);

    public static Request Log(string content)
        => new Request("Log", content);

    public static Request Warning(string content)
        => new Request("Warning", content);

    public static Request Error(string content)
        => new Request("Error", content);

    public static Request GetUserName()
        => new Request("GetUserName");
}
