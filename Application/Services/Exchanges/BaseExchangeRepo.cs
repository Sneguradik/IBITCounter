using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Models.Entities;

namespace Application.Services.Exchanges;

public abstract class BaseExchangeRepo(HttpClient client) :  IExchangeRepo
{
    protected readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
    protected async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
    {
        var response = await client.SendAsync(request);
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(request.RequestUri);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(rawContent);
        }
        var content = JsonSerializer.Deserialize<T>(rawContent, JsonSerializerOptions);
        if (content == null) throw new JsonException("Error in serialization");
        return content;
    }
    
    public abstract Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, DateTime start, DateTime end, string interval,  CancellationToken ct = default);
}