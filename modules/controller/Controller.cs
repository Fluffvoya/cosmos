namespace controller;

public class Controller
{
    public static void Emit(params List<Request> requests)
    {
        var message = new Requests(requests);
        message.Emit();
    }

    public static void Receive(out List<Reply> replies)
    {
        var message = Console.ReadLine();
        var repliesJson = new Replies();
        if (message is not null)
            repliesJson = Replies.Deserialize(message);
        replies = repliesJson.replies;
    }

    public static void ReceiveMessageOnly(out List<string> messages)
    {
        Receive(out var replies);
        messages = new List<string>();
        foreach (var reply in replies)
        {
            messages.Add(reply.message);
        }
    }

    // transmit json message by stdout
    public static Request ShowWindow(string name, string message)
        => new Request("ShowWindow", RequestType.Action, name, message);

    public static Request GetUserName()
        => new Request("GetUserName", RequestType.Inquiry);
}
