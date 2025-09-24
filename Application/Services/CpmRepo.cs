using System.Globalization;
using Application.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Services;

public interface ICpmRepo
{
    Task<IEnumerable<CurrentPriceOfMarket>> GetLatest1MinAsync(DateTime from, CancellationToken cancellationToken = default);
}

public class CpmRepo(PosttradeDbContext dbContext, IOptions<BotConfig> conf) : ICpmRepo
{
    private string BuildQuery(DateTime time)
    {
        var fromUtc = time.ToUniversalTime();
        var ts = fromUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); 

        return $@"
            WITH p AS (
              SELECT {conf.Value.InstrumentId} AS inst,
                     ('{ts}'::timestamp) AS t0,
                     ('{ts}'::timestamp) + INTERVAL '1 minute' AS t1
            )
            -- сделки за указанную минуту
            SELECT i.trade_time, i.price, i.amount
            FROM ""Indiquote"" i
            JOIN p ON i.instrument_instrument_id = p.inst
            WHERE i.trade_time >= p.t0 AND i.trade_time < p.t1

            UNION ALL

            -- последнее значение ПЕРЕД этой минутой
            SELECT pv.trade_time, pv.price, pv.amount
            FROM p
            CROSS JOIN LATERAL (
              SELECT trade_time, price, amount
              FROM ""Indiquote""
              WHERE instrument_instrument_id = p.inst
                AND trade_time < p.t0
              ORDER BY trade_time DESC
              LIMIT 1
            ) pv

            ORDER BY trade_time DESC".ToString();
    }

    public async Task<IEnumerable<CurrentPriceOfMarket>> GetLatest1MinAsync(DateTime from, CancellationToken cancellationToken = default) =>
        await dbContext.Database
            .SqlQueryRaw<CurrentPriceOfMarket>(BuildQuery(from))
            .ToArrayAsync(cancellationToken);
}