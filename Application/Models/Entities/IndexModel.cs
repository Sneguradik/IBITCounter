using System.Text.Json.Serialization;

namespace Application.Models.Entities;

public class IndexModel
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonPropertyName("value")]
    public double Value { get; set; }
}