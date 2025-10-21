using System.Text.Json.Serialization;

namespace Records.Models;

public class RecordDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Dataset { get; set; } = string.Empty;

    public Dictionary<string, object> Fields { get; set; } = new();

    public string SearchVector { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
