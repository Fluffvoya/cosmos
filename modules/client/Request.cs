namespace client;

public enum RequestType
{
    Action,
    Inquiry,// expect a reply
}

public class Request
{
    public string name { get; set; }
    public string requestType { get; set; }
    public List<string> args { get; set; }

    public Request()
    {
        name = "";
        requestType = "";
        args = new List<string>();
    }

    public Request(string name_, RequestType requestType_, params List<string> args_)
    {
        name = name_;
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