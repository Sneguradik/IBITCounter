using Application.Models.Entities;

namespace Application.Services.Exchanges;

public interface IExchangeRepo
{
    Task<IEnumerable<Candle>>  GetCandlesAsync(string symbol, DateTime start, DateTime end, string interval, CancellationToken ct = default);
}