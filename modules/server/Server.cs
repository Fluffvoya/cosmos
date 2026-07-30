using cosmos_error;
using public_model;

namespace server;

public class Server
{
    public static string CreateReplies(params List<Reply> replies)
    {
        Replies model = new Replies(replies);
        return model.Serialize();
    }

    public static List<Request> GetRequests(string text)
    {
        var requests = Requests.Deserialize(text);
        if (requests is null)
            return new List<Request>();

        return requests.requests;
    }

    public static List<string> GetRequestsName(string text)
    {
        List<string> rns = new List<string>();
        var requests = GetRequests(text);

        foreach (var item in requests)
            rns.Add(item.request);
        return rns;
    }

    public static List<RequestType> GetRequestType(string text)
        => GetRequestTypeFromRequests(GetRequests(text));

    public static List<RequestType> GetRequestTypeFromRequests(List<Request> requests)
    {
        var rns = new List<RequestType>();

        foreach (var item in requests)
        {
            var type = item.requestType switch
            {
                "action" => RequestType.Action,
                "inquiry" => RequestType.Inquiry,
                _ => RequestType.Unknown,
            };
            if (type == RequestType.Unknown)
                throw new ServerException(ErrorCode.UnknownRequestType, $"Unknown request type: {item.requestType}");
            rns.Add(type);
        }
        return rns;
    }


}
