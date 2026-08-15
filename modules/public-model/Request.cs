using System.Text.Json;
using cosmos_error;

namespace public_model;

/// <summary>
/// Represents a request sent to the Cosmos server, containing a handler name and optional arguments.
/// </summary>
public class Request
{
    /// <summary>
    /// Gets or sets the name of the request handler to invoke.
    /// </summary>
    public string request { get; set; }

    /// <summary>
    /// Gets or sets the list of string arguments passed to the handler.
    /// </summary>
    public List<string> args { get; set; }

    /// <summary>
    /// Initializes a new <see cref="Request"/> with empty values.
    /// </summary>
    public Request()
    {
        request = "";
        args = new List<string>();
    }

    /// <summary>
    /// Initializes a new <see cref="Request"/> with the specified handler name and arguments.
    /// </summary>
    /// <param name="request_">The name of the request handler. Must not be null or empty.</param>
    /// <param name="args_">The arguments to pass to the handler.</param>
    /// <exception cref="PublicModelException">Thrown when <paramref name="request_"/> is null or empty.</exception>
    public Request(string request_, params List<string> args_)
    {
        if (string.IsNullOrEmpty(request_))
            throw new PublicModelException(
                ErrorCode.EmptyRequestName,
                "Request name cannot be null or empty.");

        request = request_;
        args = args_;
    }

    /// <summary>
    /// Serializes this <see cref="Request"/> to a JSON string.
    /// </summary>
    /// <returns>A JSON representation of this request.</returns>
    /// <exception cref="PublicModelException">Thrown when serialization fails.</exception>
    public string Serialize()
    {
        try
        {
            return JsonSerializer.Serialize(this);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonSerializeFailed,
                $"Failed to serialize Request to JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="Request"/>.
    /// </summary>
    /// <param name="text">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="Request"/>, or <c>null</c> if <paramref name="text"/> is null or empty.</returns>
    /// <exception cref="PublicModelException">Thrown when deserialization fails.</exception>
    public static Request? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Request? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Request>(text);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Request: {ex.Message}");
        }

        return jsonStruct;
    }
}
