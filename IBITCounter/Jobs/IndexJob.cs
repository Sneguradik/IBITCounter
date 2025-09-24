using Application.Models.Entities;
using Application.Services;
using Quartz;

namespace IBITCounter.Jobs;

public class IndexJob(IIndexSender indexSender, ICpmRepo cpmRepo, CoefficientStorage coeff, ILogger<IndexJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("IndexJob started");
        var dt = DateTime.UtcNow;
        dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
        var cpm = await cpmRepo.GetLatest1MinAsync(dt, context.CancellationToken);
        logger
            .LogInformation("Got cpm from {TradeTime:yyyy-MM-dd hh:mm} (utc) to {DateTime:yyyy-MM-dd hh:mm} (utc)", 
                cpm.Last().TradeTime, cpm.First().TradeTime);

        var indexValue = Counter.Count(Counter.CountCpm(cpm, dt), coeff);
        
        await indexSender.SendCpmAsync(new CurrentPriceOfMarket()
        {
            Amount = 0,
            TradeTime = dt,
            Price = indexValue
        },  context.CancellationToken);
        logger.LogInformation("Index counted on {DateTime:yyyy-MM-dd hh:mm} - {IndexValue}", dt, indexValue);
    }
}