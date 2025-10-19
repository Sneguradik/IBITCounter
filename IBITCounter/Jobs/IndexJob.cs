using System.Text.Json;
using Application.Models.Entities;
using Application.Services;
using Application.Services.Exchanges;
using Quartz;

namespace IBITCounter.Jobs;

public class IndexJob(
    IIndexSender indexSender, 
    ICpmRepo cpmRepo, 
    CoefficientStorage coeff,
    ICsvReporter csvReporter,
    IServiceProvider serviceProvider, 
    ILogger<IndexJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("IndexJob started");
        var dt = DateTime.UtcNow;
        dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
        
        var cpm = await cpmRepo.GetLatest1MinAsync(dt, context.CancellationToken);
        logger
            .LogInformation("Got cpm from {TradeTime:yyyy-MM-dd HH:mm} (utc) to {DateTime:yyyy-MM-dd HH:mm} (utc)", 
                cpm.Last().TradeTime, cpm.First().TradeTime);

        var weightedAvg = await CountWeightedAvg(dt, context.CancellationToken);
        
        var cpmAvg = Counter.CountCpm(cpm, dt - TimeSpan.FromMinutes(1));
        
        var indexValue = Counter.Count( cpmAvg, coeff);
        
        await csvReporter.LogAsync(new Report()
        {
            Timestamp = dt,
            Cpm = cpmAvg,
            WeightedAverage = weightedAvg,
            Day = coeff.Day,
            Index = indexValue,
            Median = coeff.Median,
            Staking = coeff.StakingFee
        }, context.CancellationToken);

        try
        {
            await indexSender.SendCpmAsync([new CurrentPriceOfMarket()
            {
                Amount = 0,
                TradeTime = dt,
                Price = indexValue
            }], context.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            await indexSender.SendCpmAsync([new CurrentPriceOfMarket()
            {
                Amount = 0,
                TradeTime = dt,
                Price = indexValue
            }], context.CancellationToken);
        }
        
        logger.LogInformation("Index counted on {DateTime:yyyy-MM-dd HH:mm} - {IndexValue}", dt, indexValue);
    }

    private async Task<double> CountWeightedAvg(DateTime dt, CancellationToken ct = default)
    {
        logger.LogInformation("Started counting weighted avg");
        using var scope = serviceProvider.CreateScope();
        
        var binanceRepo = scope.ServiceProvider.GetRequiredKeyedService<IExchangeRepo>(BinanceRepo.Name);
        var byBitRepo = scope.ServiceProvider.GetRequiredKeyedService<IExchangeRepo>(ByBitRepo.Name);
        var bitGetRepo = scope.ServiceProvider.GetRequiredKeyedService<IExchangeRepo>(BitGetRepo.Name);
        
        var startTime = dt - TimeSpan.FromMinutes(2);
        var endTime = DateTime.UtcNow;
        
        var candleTasks = new List<Task<IEnumerable<Candle>>>()
        {
            binanceRepo.GetCandlesAsync("BTCUSDT", startTime, endTime, "1m", ct),
            byBitRepo.GetCandlesAsync("BTCUSDT", startTime, endTime, "1", ct),
            bitGetRepo.GetCandlesAsync("BTCUSDT",  startTime, endTime, "1min", ct),
        };
        var taskRes = await Task.WhenAll(candleTasks);
        
        logger.LogInformation("Received market data from exchanges");
        
        var searchDate = dt - TimeSpan.FromMinutes(1);
        
        var candles = new List<Candle>();
        
        foreach (var task in taskRes) candles.AddRange(task);
        
        if (candles.Count == 0) return 0;
        
        candles = candles.Where(x=>x.Timestamp == searchDate).ToList();
        
        logger.LogInformation($"Got {candles.Count} candles from {candles.FirstOrDefault()?.Timestamp:yyyy-MM-dd HH:mm}");
        
        var totalVolume = candles.Sum(x => x.Volume);

        return candles.Sum(x => x.Close * x.Volume/totalVolume);
    }
}