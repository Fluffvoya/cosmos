using System.Text.Json;

namespace bridge;

public class Requests
{
    public Requests(params List<Request> requests_)
    {
        requests = requests_;
    }

    public List<Request> requests { get; set; }
    public void Emit()
    {
        string jsonText = JsonSerializer.Serialize(this);
        Console.WriteLine(jsonText);
    }
}