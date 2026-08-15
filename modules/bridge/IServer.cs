namespace bridge;

/// <summary>
/// Defines the contract for executing serialized request payloads.
/// Implementations handle routing requests to the appropriate handler and returning a response.
/// </summary>
public interface IServer
{
    /// <summary>
    /// Executes a serialized JSON request and returns the serialized response.
    /// </summary>
    /// <param name="requests">A JSON string containing the request name and arguments.</param>
    /// <returns>A JSON string containing the response from the handler.</returns>
    string Execute(string requests);
}