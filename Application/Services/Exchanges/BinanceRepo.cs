using System.Globalization;
using System.Text.Json;
using Application.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services.Exchanges;

public class BinanceRepo(HttpClient client, ILogger<BinanceRepo> logger) : BaseExchangeRepo(client)
{
    public const string BaseAddress = "https://api.binance.com";
    public const string Name = "Binance";
    private static Candle ConvertCandle(IEnumerable<JsonElement> candle)
    {
        var enumerable = candle as JsonElement[] ?? candle.ToArray();
        return new Candle()
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(enumerable[0].GetInt64()).UtcDateTime,
            Open = Convert.ToDouble(enumerable[1].GetString(), CultureInfo.InvariantCulture),
            High = Convert.ToDouble(enumerable[2].GetString(), CultureInfo.InvariantCulture),
            Low = Convert.ToDouble(enumerable[3].GetString(), CultureInfo.InvariantCulture),
            Close = Convert.ToDouble(enumerable[4].GetString(), CultureInfo.InvariantCulture),
            Volume = Convert.ToDouble(enumerable[7].GetString(), CultureInfo.InvariantCulture),
        };
    }

    public override async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, DateTime start, DateTime end, string interval, CancellationToken ct = default)
    {
        try
        {
            var startUnixTime = new DateTimeOffset(start).ToUnixTimeMilliseconds();
            var endUnixTime = new DateTimeOffset(end).ToUnixTimeMilliseconds();
            
            var msg = new HttpRequestMessage()
            {
                RequestUri = new Uri($"/api/v3/klines?symbol={symbol}&interval={interval}&startTime={startUnixTime}&endTime={endUnixTime}&limit=1000", UriKind.Relative),
                Method = HttpMethod.Get
            };
            
            var response = await SendRequestAsync<JsonElement[][]>(msg);
            return response.Length == 0 ? [] : response.Select(ConvertCandle);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return [];
        }
        
    }
    
}