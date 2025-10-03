using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Services.Exchanges.Dto;

public class OkxBitGetDto
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Garbage { get; set; } = new();
    
    public List<List<JsonElement>> Data { get; set; } = new();
}