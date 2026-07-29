using cosmos_error;

namespace public_model;

public class Reply
{
    public string requestName { get; set; }
    public string message { get; set; }

    public Reply(string requestName, string message)
    {
        if (string.IsNullOrEmpty(requestName))
            throw new PublicModelException(
                ErrorCode.EmptyReplyRequestName,
                "Reply request name cannot be null or empty.");

        this.requestName = requestName;
        this.message = message;
    }
}