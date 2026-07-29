using public_model;

namespace client;

public class Client
{
    public static string CreateRequests(params List<Request> requests)
    {
        var requestsText = new Requests(requests);
        return requestsText.Serialize();
    }

    // transmit json message by stdout
    public static Request ShowWindow(string name, string message)
        => new Request("ShowWindow", RequestType.Action, name, message);

    public static Request GetUserName()
        => new Request("GetUserName", RequestType.Inquiry);
}
