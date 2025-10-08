using System.Globalization;
using System.Text.Json;
using Application.Models.Entities;
using Application.Services.Exchanges.Dto;
using Microsoft.Extensions.Logging;

namespace Application.Services.Exchanges;

public class ByBitRepo(HttpClient client, ILogger<ByBitRepo> logger) : BaseExchangeRepo(client)
{
    public const string BaseAddress = "https://api.bybit.com";
    public const string Name = "ByBit";
    private Candle ConvertCandle(IEnumerable<JsonElement> candle)
    {
        var enumerable = candle as JsonElement[] ?? candle.ToArray();
        return new Candle()
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(enumerable[0].GetString())).UtcDateTime,
            Open = Convert.ToDouble(enumerable[1].GetString(), CultureInfo.InvariantCulture),
            High = Convert.ToDouble(enumerable[2].GetString(), CultureInfo.InvariantCulture),
            Low = Convert.ToDouble(enumerable[3].GetString(), CultureInfo.InvariantCulture),
            Close = Convert.ToDouble(enumerable[4].GetString(), CultureInfo.InvariantCulture),
            Volume = Convert.ToDouble(enumerable[6].GetString(), CultureInfo.InvariantCulture),
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
                RequestUri = new Uri($"/v5/market/kline?category=linear&symbol={symbol}&interval={interval}&start={startUnixTime}&end={endUnixTime}&limit=1000", UriKind.Relative),
                Method = HttpMethod.Get
            };
            
            var response = await SendRequestAsync<ByBitDto>(msg);
            return !response.Result.List.Any() ? [] : response.Result.List.Select(ConvertCandle);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return [];
        }
        
    }
}