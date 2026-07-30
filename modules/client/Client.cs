using public_model;

namespace client;

public class Client
{
    public static string CreateRequests(params List<Request> requests)
    {
        var requestsText = new Requests(requests);
        return requestsText.Serialize();
    }

    public static List<Reply> GetReplies(string reply)
    {
        var replies = Replies.Deserialize(reply);
        if (replies is null)
            return new List<Reply>();
        return replies.replies;
    }

    public static List<string> GetRepliesMessage(string reply)
    {
        List<string> rns = new List<string>();
        var replies = GetReplies(reply);
        foreach (var item in replies)
            rns.Add(item.request);
        return rns;
    }


    public static Request ShowWindow(string name, string message)
        => new Request("ShowWindow", RequestType.Action, name, message);

    public static Request GetUserName()
        => new Request("GetUserName", RequestType.Inquiry);
}
