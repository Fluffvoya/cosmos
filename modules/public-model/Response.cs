using System.Text.Json;
using cosmos_error;

namespace public_model;

/// <summary>
/// Represents a response returned from the Cosmos server, containing the originating request name and a message.
/// </summary>
public class Response
{
    /// <summary>
    /// Gets or sets the name of the request that produced this response.
    /// </summary>
    public string request { get; set; }

    /// <summary>
    /// Gets or sets the response message payload.
    /// </summary>
    public string message { get; set; }

    /// <summary>
    /// Initializes a new <see cref="Response"/> with empty values.
    /// </summary>
    public Response()
    {
        request = "";
        message = "";
    }

    /// <summary>
    /// Initializes a new <see cref="Response"/> with the specified request name and message.
    /// </summary>
    /// <param name="request">The originating request name. Must not be null or empty.</param>
    /// <param name="message">The response message payload.</param>
    /// <exception cref="PublicModelException">Thrown when <paramref name="request"/> is null or empty.</exception>
    public Response(string request, string message)
    {
        if (string.IsNullOrEmpty(request))
            throw new PublicModelException(
                ErrorCode.EmptyResponseRequestName,
                "Response request name cannot be null or empty.");

        this.request = request;
        this.message = message;
    }

    /// <summary>
    /// Serializes this <see cref="Response"/> to a JSON string.
    /// </summary>
    /// <returns>A JSON representation of this response.</returns>
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
                $"Failed to serialize Response to JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="Response"/>.
    /// </summary>
    /// <param name="text">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="Response"/>, or <c>null</c> if <paramref name="text"/> is null or empty.</returns>
    /// <exception cref="PublicModelException">Thrown when deserialization fails.</exception>
    public static Response? Deserialize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        Response? jsonStruct = null;
        try
        {
            jsonStruct = JsonSerializer.Deserialize<Response>(text);
        }
        catch (Exception ex)
        {
            throw new PublicModelException(
                ErrorCode.JsonDeserializeFailed,
                $"Failed to deserialize JSON to Response: {ex.Message}");
        }

        return jsonStruct;
    }
}
