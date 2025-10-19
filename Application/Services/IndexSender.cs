using System.Text.Json;
using Application.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public interface IIndexSender
{
    Task SendCpmAsync(IEnumerable<CurrentPriceOfMarket> cpm, CancellationToken cancellationToken = default);
}

public class IndexSender(HttpClient httpClient, IOptions<BotConfig> conf, ILogger<IndexSender> logger) : IIndexSender
{
    public const string BaseAddress = "https://indexapi.spbexchange.ru";
    public async Task SendCpmAsync(IEnumerable<CurrentPriceOfMarket> cpm, CancellationToken cancellationToken = default)
    {
      
        var msg = new HttpRequestMessage()
        {
            RequestUri = new Uri(conf.Value.Endpoint,  UriKind.Relative),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonSerializer.Serialize(cpm
                .Select(x=>new IndexModel(){Timestamp = x.TradeTime.ToUniversalTime(), Value = Math.Round(x.Price, 2)})
                .ToArray())),
        };
        msg.Content.Headers.ContentType = new("application/json");
        var response = await httpClient.SendAsync(msg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(content);
            throw new Exception(response.ReasonPhrase);
        }
        
        logger.LogInformation($"Sent current price of market ");
    }
}