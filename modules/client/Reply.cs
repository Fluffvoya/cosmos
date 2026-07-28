namespace client;

public class Reply
{
    public string requestName { get; set; }
    public string message { get; set; }

    public Reply(string requestName, string message)
    {
        this.requestName = requestName;
        this.message = message;
    }
}